using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ScottPlot;
using Storix_BE.Domain.Models;
using Storix_BE.Repository.Interfaces;
using Storix_BE.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Storix_BE.Service.Implementation
{
    public class ReportingService : IReportingService
    {
        private readonly IReportingRepository _repo;
        private readonly Cloudinary _cloudinary;

        public ReportingService(IReportingRepository repo, Cloudinary cloudinary)
        {
            _repo = repo;
            _cloudinary = cloudinary;
        }

        public async Task<ReportDetailDto> CreateReportAsync(int companyId, int createdByUserId, CreateReportRequest payload)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid companyId.", nameof(companyId));
            if (createdByUserId <= 0) throw new ArgumentException("Invalid createdByUserId.", nameof(createdByUserId));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (string.IsNullOrWhiteSpace(payload.ReportType)) throw new ArgumentException("ReportType is required.", nameof(payload.ReportType));
            if (payload.TimeTo < payload.TimeFrom) throw new ArgumentException("TimeTo must be >= TimeFrom.");

            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            var report = new Report
            {
                CompanyId = companyId,
                CreatedByUserId = createdByUserId,
                ReportType = payload.ReportType,
                WarehouseId = payload.WarehouseId,
                TimeFrom = payload.TimeFrom,
                TimeTo = payload.TimeTo,
                Status = "Running",
                CreatedAt = now
            };

            report = await _repo.CreateReportAsync(report).ConfigureAwait(false);

            try
            {
                if (!string.Equals(payload.ReportType, ReportTypes.OutboundKpiBasic, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Unsupported report type '{payload.ReportType}'.");
                }

                var kpi = await _repo.GetOutboundKpiBasicAsync(companyId, payload.WarehouseId, payload.TimeFrom, payload.TimeTo)
                    .ConfigureAwait(false);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var summaryObj = new
                {
                    totalCompleted = kpi.TotalCompleted,
                    overallAvgLeadTimeHours = kpi.OverallAvgLeadTimeHours
                };

                report.SummaryJson = JsonSerializer.Serialize(summaryObj, jsonOptions);
                report.DataJson = JsonSerializer.Serialize(kpi, jsonOptions);
                report.SchemaVersion = "1";

                report.Status = "Succeeded";
                report.CompletedAt = now;
                report.ErrorMessage = null;
                await _repo.UpdateReportAsync(report).ConfigureAwait(false);

                return new ReportDetailDto(
                    report.Id,
                    report.ReportType,
                    report.CompanyId,
                    report.WarehouseId,
                    report.Status,
                    report.TimeFrom,
                    report.TimeTo,
                    report.CreatedAt,
                    report.CompletedAt,
                    report.ErrorMessage,
                    new ReportResultDto(TryParseJson(report.SummaryJson), TryParseJson(report.DataJson), report.SchemaVersion),
                    report.PdfUrl == null ? null : new ReportPdfArtifactDto(report.PdfUrl, report.PdfFileName, report.PdfContentHash, report.PdfGeneratedAt));
            }
            catch (Exception ex)
            {
                report.Status = "Failed";
                report.CompletedAt = now;
                report.ErrorMessage = ex.Message;
                await _repo.UpdateReportAsync(report).ConfigureAwait(false);
                throw;
            }
        }

        public async Task<ReportDetailDto?> GetReportAsync(int companyId, int reportId)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid companyId.", nameof(companyId));
            if (reportId <= 0) throw new ArgumentException("Invalid reportId.", nameof(reportId));

            var report = await _repo.GetReportByIdAsync(companyId, reportId).ConfigureAwait(false);
            if (report == null) return null;

            var resultDto = (string.IsNullOrWhiteSpace(report.SummaryJson) && string.IsNullOrWhiteSpace(report.DataJson) && string.IsNullOrWhiteSpace(report.SchemaVersion))
                ? null
                : new ReportResultDto(TryParseJson(report.SummaryJson), TryParseJson(report.DataJson), report.SchemaVersion);

            var pdfDto = string.IsNullOrWhiteSpace(report.PdfUrl)
                ? null
                : new ReportPdfArtifactDto(report.PdfUrl, report.PdfFileName, report.PdfContentHash, report.PdfGeneratedAt);

            return new ReportDetailDto(
                report.Id,
                report.ReportType,
                report.CompanyId,
                report.WarehouseId,
                report.Status,
                report.TimeFrom,
                report.TimeTo,
                report.CreatedAt,
                report.CompletedAt,
                report.ErrorMessage,
                resultDto,
                pdfDto);
        }

        public async Task<ReportPdfArtifactDto> ExportReportPdfAsync(int companyId, int reportId)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid companyId.", nameof(companyId));
            if (reportId <= 0) throw new ArgumentException("Invalid reportId.", nameof(reportId));

            var report = await _repo.GetReportByIdAsync(companyId, reportId).ConfigureAwait(false);
            if (report == null)
                throw new InvalidOperationException("Report not found.");

            if (!string.Equals(report.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Report is not ready for export. Current status: '{report.Status}'.");

            if (string.IsNullOrWhiteSpace(report.DataJson))
                throw new InvalidOperationException("Report result data is missing.");

            if (!string.Equals(report.ReportType, ReportTypes.OutboundKpiBasic, StringComparison.Ordinal))
                throw new InvalidOperationException($"PDF export is not implemented for report type '{report.ReportType}'.");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<OutboundKpiBasicReportData>(report.DataJson, jsonOptions);
            if (data == null)
                throw new InvalidOperationException("Failed to deserialize report data.");

            var pdfBytes = GenerateOutboundKpiBasicPdf(report, data);

            var fileName = $"report_{report.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var contentHash = ComputeSha256Hex(pdfBytes);

            await using var stream = new MemoryStream(pdfBytes);
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = "reports",
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams).ConfigureAwait(false);
            if (uploadResult.Error != null)
                throw new InvalidOperationException(uploadResult.Error.Message);

            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            report.PdfUrl = uploadResult.SecureUrl?.ToString();
            report.PdfFileName = fileName;
            report.PdfContentHash = contentHash;
            report.PdfGeneratedAt = now;
            await _repo.UpdateReportAsync(report).ConfigureAwait(false);

            return new ReportPdfArtifactDto(report.PdfUrl, report.PdfFileName, report.PdfContentHash, report.PdfGeneratedAt);
        }

        private static string ComputeSha256Hex(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static JsonElement? TryParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch
            {
                // If stored JSON is invalid/corrupt, don't break the whole response.
                return null;
            }
        }

        private static byte[] GenerateOutboundKpiBasicPdf(Report report, OutboundKpiBasicReportData data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var completedChart = BuildCompletedCountByDayChart(data);
            var leadTimeChart = BuildAvgLeadTimeByDayChart(data);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"Report: {report.ReportType}").FontSize(16).SemiBold();
                        col.Item().Text($"ReportId: {report.Id}   CompanyId: {report.CompanyId}   WarehouseId: {(report.WarehouseId?.ToString() ?? "All")}");
                        col.Item().Text($"Range: {data.TimeFrom:yyyy-MM-dd} -> {data.TimeTo:yyyy-MM-dd}");
                        col.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).Padding(8).Column(box =>
                            {
                                box.Item().Text("Total completed").SemiBold();
                                box.Item().Text(data.TotalCompleted.ToString()).FontSize(14);
                            });

                            row.RelativeItem().Border(1).Padding(8).Column(box =>
                            {
                                box.Item().Text("Avg lead time (hours)").SemiBold();
                                box.Item().Text(data.OverallAvgLeadTimeHours?.ToString("0.##") ?? "-").FontSize(14);
                            });
                        });

                        if (completedChart != null)
                        {
                            col.Item().Text("Completed count by day").SemiBold();
                            col.Item().Image(completedChart);
                        }
                        else
                        {
                            col.Item().Text("No completed orders in selected range.");
                        }

                        if (leadTimeChart != null)
                        {
                            col.Item().Text("Average lead time (hours) by day").SemiBold();
                            col.Item().Image(leadTimeChart);
                        }

                        col.Item().Text("Top staff throughput").SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(110);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Staff");
                                header.Cell().Element(CellStyle).AlignRight().Text("Completed");
                                header.Cell().Element(CellStyle).AlignRight().Text("Avg lead time (h)");
                            });

                            foreach (var s in data.ByStaff.Take(10))
                            {
                                table.Cell().Element(CellStyle).Text($"{s.StaffName ?? "(unknown)"} (#{s.StaffId})");
                                table.Cell().Element(CellStyle).AlignRight().Text(s.CompletedCount.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(s.AvgLeadTimeHours?.ToString("0.##") ?? "-");
                            }

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated at ");
                        x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")).SemiBold();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static byte[]? BuildCompletedCountByDayChart(OutboundKpiBasicReportData data)
        {
            if (data.ByDay == null || data.ByDay.Count == 0) return null;

            var days = data.ByDay.Select(x => x.Day).ToList();
            var positions = Enumerable.Range(0, days.Count).Select(i => (double)i).ToArray();
            var values = data.ByDay.Select(x => (double)x.Count).ToArray();

            var plot = new Plot();
            plot.Add.Bars(positions, values);
            plot.Axes.Margins(bottom: 0);
            plot.Title("Completed orders");
            plot.YLabel("Count");

            var ticks = new ScottPlot.TickGenerators.NumericManual();
            for (var i = 0; i < days.Count; i++)
                ticks.AddMajor(i, days[i].ToString("MM-dd"));
            plot.Axes.Bottom.TickGenerator = ticks;
            plot.Axes.Bottom.TickLabelStyle.Rotation = 45;

            return plot.GetImageBytes(800, 350, ScottPlot.ImageFormat.Png);
        }

        private static byte[]? BuildAvgLeadTimeByDayChart(OutboundKpiBasicReportData data)
        {
            if (data.ByDay == null || data.ByDay.Count == 0) return null;

            var days = data.ByDay.Select(x => x.Day).ToList();
            var positions = Enumerable.Range(0, days.Count).Select(i => (double)i).ToArray();
            var values = data.ByDay.Select(x => x.AvgLeadTimeHours ?? 0).ToArray();

            var plot = new Plot();
            plot.Add.Scatter(positions, values);
            plot.Title("Average lead time");
            plot.YLabel("Hours");

            var ticks = new ScottPlot.TickGenerators.NumericManual();
            for (var i = 0; i < days.Count; i++)
                ticks.AddMajor(i, days[i].ToString("MM-dd"));
            plot.Axes.Bottom.TickGenerator = ticks;
            plot.Axes.Bottom.TickLabelStyle.Rotation = 45;

            return plot.GetImageBytes(800, 350, ScottPlot.ImageFormat.Png);
        }

        public async Task<List<ReportRequestListItemDto>> ListReportsAsync(
            int companyId,
            string? reportType,
            int? warehouseId,
            DateTime? from,
            DateTime? to,
            int skip,
            int take)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid companyId.", nameof(companyId));

            var items = await _repo.ListReportsAsync(companyId, reportType, warehouseId, from, to, skip, take)
                .ConfigureAwait(false);

            return items.Select(r => new ReportRequestListItemDto(
                r.Id,
                r.ReportType,
                r.WarehouseId,
                r.Status,
                r.TimeFrom,
                r.TimeTo,
                r.CreatedAt,
                r.CompletedAt,
                r.ErrorMessage)).ToList();
        }
    }
}

