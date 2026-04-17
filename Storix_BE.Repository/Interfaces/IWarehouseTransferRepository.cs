using Storix_BE.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Interfaces
{
    public interface IWarehouseTransferRepository
    {
        Task<TransferOrderDetailDto> CreateDraftAsync(int companyId, int createdBy, CreateTransferOrderRequest request);
        Task<TransferOrderDetailDto> UpdateDraftAsync(int companyId, int actorUserId, int transferOrderId, UpdateTransferOrderRequest request);
        Task<TransferOrderDetailDto> AddItemAsync(int companyId, int actorUserId, int transferOrderId, AddTransferOrderItemRequest request);
        Task<TransferOrderDetailDto> UpdateItemAsync(int companyId, int actorUserId, int transferOrderId, int itemId, UpdateTransferOrderItemRequest request);
        Task<TransferOrderDetailDto> RemoveItemAsync(int companyId, int actorUserId, int transferOrderId, int itemId);
        Task<List<TransferStaffSuggestionDto>> SuggestStaffAsync(int companyId, int actorUserId, int transferOrderId);
        Task<TransferOrderDetailDto> AssignCarrierAsync(int companyId, int actorUserId, int transferOrderId, int carrierUserId);
        Task<TransferOrderDetailDto> SubmitAsync(int companyId, int actorUserId, int transferOrderId);
        Task<TransferOrderDetailDto> ApproveAsync(int companyId, int actorUserId, int transferOrderId, int? receiverStaffId = null);
        Task<TransferOrderDetailDto> RejectAsync(int companyId, int actorUserId, int transferOrderId, string reason);
        Task<TransferOrderDetailDto> StartPickingAsync(int companyId, int actorUserId, int transferOrderId);
        Task<TransferOrderDetailDto> MarkPackedAsync(int companyId, int actorUserId, int transferOrderId);
        Task<TransferOrderDetailDto> ShipAsync(int companyId, int actorUserId, int transferOrderId);
        Task<TransferOrderDetailDto> ReceiveAsync(int companyId, int actorUserId, int transferOrderId, ReceiveTransferOrderRequest request);
        Task<TransferOrderDetailDto> QualityCheckAsync(int companyId, int actorUserId, int transferOrderId, TransferQualityCheckRequest request);
        Task<TransferOrderDetailDto> CancelAsync(int companyId, int actorUserId, int transferOrderId, string? reason);
        Task<List<TransferOrderListDto>> GetAllAsync(int companyId, int? sourceWarehouseId, int? destinationWarehouseId, string? status);
        Task<TransferOrderDetailDto> GetByIdAsync(int companyId, int transferOrderId);
        Task<List<TransferAvailabilityDto>> CheckAvailabilityAsync(int companyId, int transferOrderId);
    }
}
