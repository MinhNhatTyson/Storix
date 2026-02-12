using Storix_BE.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Interfaces
{
    public interface IStockCountRepository
    {
        Task<IReadOnlyList<Inventory>> ListInventoryProductsAsync(int companyId, int warehouseId, IEnumerable<int>? productIds = null);

        Task<StockCountsTicket> CreateTicketAsync(
            int companyId,
            int warehouseId,
            int createdByUserId,
            string? name,
            string? type,
            string? description,
            IEnumerable<int>? productIds);

        Task<List<StockCountsTicket>> ListTicketsAsync(int companyId, int? warehouseId, string? status);

        Task<StockCountsTicket> GetTicketByIdAsync(int companyId, int ticketId);

        Task<StockCountItem> UpdateCountedQuantityAsync(
            int companyId,
            int itemId,
            int countedQuantity,
            string? description = null,
            bool? status = null);

        Task ApplyApprovalAsync(int companyId, int ticketId, int performedByUserId);
    }
}

