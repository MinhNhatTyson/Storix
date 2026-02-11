using Microsoft.EntityFrameworkCore;
using Storix_BE.Domain.Context;
using Storix_BE.Domain.Models;
using Storix_BE.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Implementation
{
    public class ReportingRepository : IReportingRepository
    {
        private readonly StorixDbContext _context;

        public ReportingRepository(StorixDbContext context)
        {
            _context = context;
        }

        public async Task<Report> CreateReportAsync(Report report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            _context.Reports.Add(report);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            return report;
        }

        public async Task UpdateReportAsync(Report report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            _context.Reports.Update(report);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<Report?> GetReportByIdAsync(int companyId, int reportId)
        {
            return await _context.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reportId && r.CompanyId == companyId)
                .ConfigureAwait(false);
        }

        public async Task<List<Report>> ListReportsAsync(
            int companyId,
            string? reportType,
            int? warehouseId,
            DateTime? from,
            DateTime? to,
            int skip,
            int take)
        {
            var query = _context.Reports
                .AsNoTracking()
                .Where(r => r.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(reportType))
            {
                query = query.Where(r => r.ReportType == reportType);
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(r => r.WarehouseId == warehouseId.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= to.Value);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip < 0 ? 0 : skip)
                .Take(take <= 0 ? 50 : take)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<OutboundKpiBasicReportData> GetOutboundKpiBasicAsync(int companyId, int? warehouseId, DateTime from, DateTime to)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid companyId.", nameof(companyId));
            if (to < from) throw new ArgumentException("TimeTo must be >= TimeFrom.");

            var baseQuery = _context.OutboundOrders
                .AsNoTracking()
                .Where(o => o.Warehouse != null && o.Warehouse.CompanyId == companyId);

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                baseQuery = baseQuery.Where(o => o.WarehouseId == warehouseId.Value);
            }

            // CompletedAt source priority:
            // 1) Status history entry where NewStatus == "Completed"
            // 2) InventoryTransaction.CreatedAt for Outbound as a proxy (older data)
            var filtered = baseQuery
                .Select(o => new
                {
                    o.Id,
                    o.WarehouseId,
                    o.StaffId,
                    StaffName = o.Staff != null ? o.Staff.FullName : null,
                    o.CreatedAt,
                    CompletedAt =
                        _context.OutboundOrderStatusHistories
                            .Where(h => h.OutboundOrderId == o.Id && h.NewStatus == "Completed")
                            .Max(h => (DateTime?)h.ChangedAt)
                        ?? _context.InventoryTransactions
                            .Where(t => t.ReferenceId == o.Id && t.TransactionType == "Outbound")
                            .Max(t => (DateTime?)t.CreatedAt),
                    o.Status
                })
                .Where(x =>
                    x.CompletedAt.HasValue &&
                    x.CompletedAt.Value >= from &&
                    x.CompletedAt.Value <= to);

            var rows = await filtered.ToListAsync().ConfigureAwait(false);

            static double? AvgHours(IEnumerable<(DateTime CreatedAt, DateTime CompletedAt)> items)
            {
                var list = items.ToList();
                if (!list.Any()) return null;
                var avg = list.Average(x => (x.CompletedAt - x.CreatedAt).TotalHours);
                return double.IsFinite(avg) ? avg : null;
            }

            var completedRows = rows
                .Where(r => r.CreatedAt.HasValue && r.CompletedAt.HasValue)
                .Select(r => new
                {
                    r.Id,
                    r.StaffId,
                    r.StaffName,
                    CreatedAt = r.CreatedAt!.Value,
                    CompletedAt = r.CompletedAt!.Value
                })
                .ToList();

            var byDay = completedRows
                .GroupBy(r => r.CompletedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var avg = AvgHours(g.Select(x => (x.CreatedAt, x.CompletedAt)));
                    return new OutboundKpiBasicPoint(g.Key, g.Count(), avg);
                })
                .ToList();

            var byStaff = completedRows
                .Where(r => r.StaffId.HasValue && r.StaffId.Value > 0)
                .GroupBy(r => new { StaffId = r.StaffId!.Value, r.StaffName })
                .Select(g =>
                {
                    var avg = AvgHours(g.Select(x => (x.CreatedAt, x.CompletedAt)));
                    return new OutboundKpiBasicStaffThroughput(g.Key.StaffId, g.Key.StaffName, g.Count(), avg);
                })
                .OrderByDescending(x => x.CompletedCount)
                .ThenBy(x => x.StaffId)
                .ToList();

            var overallAvg = AvgHours(completedRows.Select(x => (x.CreatedAt, x.CompletedAt)));

            return new OutboundKpiBasicReportData(
                from,
                to,
                warehouseId,
                completedRows.Count,
                overallAvg,
                byDay,
                byStaff);
        }
    }
}

