using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Storix_BE.Service.Interfaces
{
    public interface IStockCountService
    {
        Task<IReadOnlyList<StockCountInventoryProductDto>> ListInventoryProductsAsync(int companyId, int warehouseId, IEnumerable<int>? productIds = null);

        Task<StockCountTicketDetailDto> CreateTicketAsync(int companyId, int createdByUserId, CreateStockCountTicketRequest request);

        Task<List<StockCountTicketListItemDto>> ListTicketsAsync(int companyId, int? warehouseId, string? status);

        Task<StockCountTicketDetailDto> GetTicketByIdAsync(int companyId, int ticketId);

        Task<StockCountItemDto> UpdateCountedQuantityAsync(int companyId, int itemId, UpdateStockCountItemRequest request);

        Task<RunStockCountResultDto> RunAsync(int companyId, int createdByUserId, int ticketId);

        Task ApproveAsync(int companyId, int performedByUserId, int ticketId);
    }

    public sealed record StockCountInventoryProductDto(
        int ProductId,
        string? Sku,
        string? Name,
        int Quantity);

    public sealed record CreateStockCountTicketRequest(
        int WarehouseId,
        string? Name,
        string? Type,
        string? Description,
        IEnumerable<int>? ProductIds);

    public sealed record StockCountTicketListItemDto(
        int Id,
        int? WarehouseId,
        string? Name,
        string? Type,
        string? Status,
        DateTime? CreatedAt,
        DateTime? ExecutedDay,
        DateTime? FinishedDay,
        int ItemCount);

    public sealed record StockCountItemDto(
        int Id,
        int? ProductId,
        string? Sku,
        string? ProductName,
        int? SystemQuantity,
        int? CountedQuantity,
        int? Discrepancy,
        bool? Status,
        string? Description);

    public sealed record StockCountTicketDetailDto(
        int Id,
        int? WarehouseId,
        string? Name,
        string? Type,
        string? Status,
        DateTime? CreatedAt,
        DateTime? ExecutedDay,
        DateTime? FinishedDay,
        string? Description,
        IReadOnlyList<StockCountItemDto> Items);

    public sealed record UpdateStockCountItemRequest(
        int CountedQuantity,
        string? Description,
        bool? Status);

    public sealed record RunStockCountResultDto(
        int ReportId,
        JsonElement Summary,
        JsonElement Data);
}

