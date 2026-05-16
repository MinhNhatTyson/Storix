using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storix_BE.Service.Interfaces
{

    public sealed record StartBarcodeSessionRequest(int StaffId);

    public sealed record ScanBarcodeRequest(string Sku);

    public sealed record FinalizeBarcodeSessionRequest(
        IEnumerable<BarcodeQcOverride> QcOverrides);
    public sealed record BarcodeQcOverride(
        int ProductId,
        int ReceivedQuantity,
        int PassedQuantity,
        string? FailureReason,
        string? Notes);

    public sealed record BarcodeScanLineDto(
        int ProductId,
        string? ProductName,
        string? Sku,
        int ExpectedQuantity,
        int ScannedQuantity,
        bool IsComplete,
        bool IsOverScanned);

    public sealed record BarcodeScanSessionDto(
        Guid SessionId,
        int InboundOrderId,
        int StaffId,
        DateTime StartedAt,
        bool IsFinalized,
        DateTime? FinalizedAt,
        IReadOnlyList<BarcodeScanLineDto> Lines,
        bool AllComplete);

    public sealed record ScanResultDto(
        bool Success,
        string? WarningMessage,
        BarcodeScanLineDto UpdatedLine,
        BarcodeScanSessionDto Session);

    public interface IBarcodeScanService
    {
        Task<BarcodeScanSessionDto> StartSessionAsync(int companyId, int inboundOrderId,
            StartBarcodeSessionRequest request);

        Task<BarcodeScanSessionDto?> GetSessionAsync(int companyId, int inboundOrderId);
        Task<ScanResultDto> ScanAsync(int companyId, int inboundOrderId, ScanBarcodeRequest request);
        Task<InboundQualityCheckResultDto> FinalizeSessionAsync(int companyId, int inboundOrderId,
            FinalizeBarcodeSessionRequest request);
        Task DiscardSessionAsync(int companyId, int inboundOrderId);
    }
}