using Storix_BE.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Interfaces
{
    public interface IReportingRepository
    {
        Task<Report> CreateReportAsync(Report report);
        Task UpdateReportAsync(Report report);

        Task<Report?> GetReportByIdAsync(int companyId, int reportId);

        Task<List<Report>> ListReportsAsync(
            int companyId,
            string? reportType,
            int? warehouseId,
            DateTime? from,
            DateTime? to,
            int skip,
            int take);

        Task<OutboundKpiBasicReportData> GetOutboundKpiBasicAsync(int companyId, int? warehouseId, DateTime from, DateTime to);
    }

    public sealed record OutboundKpiBasicPoint(DateTime Day, int Count, double? AvgLeadTimeHours);

    public sealed record OutboundKpiBasicStaffThroughput(int StaffId, string? StaffName, int CompletedCount, double? AvgLeadTimeHours);

    public sealed record OutboundKpiBasicReportData(
        DateTime TimeFrom,
        DateTime TimeTo,
        int? WarehouseId,
        int TotalCompleted,
        double? OverallAvgLeadTimeHours,
        IReadOnlyList<OutboundKpiBasicPoint> ByDay,
        IReadOnlyList<OutboundKpiBasicStaffThroughput> ByStaff);
}

