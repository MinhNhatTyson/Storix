using Storix_BE.Repository.Interfaces;
using Storix_BE.Service.Interfaces;

namespace Storix_BE.Service.Implementation
{
    public class WarehouseTransferService : IWarehouseTransferService
    {
        private readonly IWarehouseTransferRepository _repository;

        public WarehouseTransferService(IWarehouseTransferRepository repository)
        {
            _repository = repository;
        }

        public Task<TransferOrderDetailDto> CreateDraftAsync(int companyId, int createdBy, CreateTransferOrderRequest request)
            => _repository.CreateDraftAsync(companyId, createdBy, request);

        public Task<TransferOrderDetailDto> UpdateDraftAsync(int companyId, int actorUserId, int transferOrderId, UpdateTransferOrderRequest request)
            => _repository.UpdateDraftAsync(companyId, actorUserId, transferOrderId, request);

        public Task<TransferOrderDetailDto> AddItemAsync(int companyId, int actorUserId, int transferOrderId, AddTransferOrderItemRequest request)
            => _repository.AddItemAsync(companyId, actorUserId, transferOrderId, request);

        public Task<TransferOrderDetailDto> UpdateItemAsync(int companyId, int actorUserId, int transferOrderId, int itemId, UpdateTransferOrderItemRequest request)
            => _repository.UpdateItemAsync(companyId, actorUserId, transferOrderId, itemId, request);

        public Task<TransferOrderDetailDto> RemoveItemAsync(int companyId, int actorUserId, int transferOrderId, int itemId)
            => _repository.RemoveItemAsync(companyId, actorUserId, transferOrderId, itemId);

        public Task<List<TransferStaffSuggestionDto>> SuggestStaffAsync(int companyId, int actorUserId, int transferOrderId)
            => _repository.SuggestStaffAsync(companyId, actorUserId, transferOrderId);

        public Task<TransferOrderDetailDto> AssignCarrierAsync(int companyId, int actorUserId, int transferOrderId, int carrierUserId)
            => _repository.AssignCarrierAsync(companyId, actorUserId, transferOrderId, carrierUserId);

        public Task<TransferOrderDetailDto> SubmitAsync(int companyId, int actorUserId, int transferOrderId)
            => _repository.SubmitAsync(companyId, actorUserId, transferOrderId);

        public Task<TransferOrderDetailDto> ApproveAsync(int companyId, int actorUserId, int transferOrderId, int? receiverStaffId = null)
            => _repository.ApproveAsync(companyId, actorUserId, transferOrderId, receiverStaffId);

        public Task<TransferOrderDetailDto> RejectAsync(int companyId, int actorUserId, int transferOrderId, string reason)
            => _repository.RejectAsync(companyId, actorUserId, transferOrderId, reason);

        public Task<TransferOrderDetailDto> StartPickingAsync(int companyId, int actorUserId, int transferOrderId)
            => _repository.StartPickingAsync(companyId, actorUserId, transferOrderId);

        public Task<TransferOrderDetailDto> MarkPackedAsync(int companyId, int actorUserId, int transferOrderId)
            => _repository.MarkPackedAsync(companyId, actorUserId, transferOrderId);

        public Task<TransferOrderDetailDto> ShipAsync(int companyId, int actorUserId, int transferOrderId)
            => _repository.ShipAsync(companyId, actorUserId, transferOrderId);

        public Task<TransferOrderDetailDto> ReceiveAsync(int companyId, int actorUserId, int transferOrderId, ReceiveTransferOrderRequest request)
            => _repository.ReceiveAsync(companyId, actorUserId, transferOrderId, request);

        public Task<TransferOrderDetailDto> QualityCheckAsync(int companyId, int actorUserId, int transferOrderId, TransferQualityCheckRequest request)
            => _repository.QualityCheckAsync(companyId, actorUserId, transferOrderId, request);

        public Task<TransferOrderDetailDto> CancelAsync(int companyId, int actorUserId, int transferOrderId, string? reason)
            => _repository.CancelAsync(companyId, actorUserId, transferOrderId, reason);

        public Task<List<TransferOrderListDto>> GetAllAsync(int companyId, int? sourceWarehouseId, int? destinationWarehouseId, string? status)
            => _repository.GetAllAsync(companyId, sourceWarehouseId, destinationWarehouseId, status);

        public Task<TransferOrderDetailDto> GetByIdAsync(int companyId, int transferOrderId)
            => _repository.GetByIdAsync(companyId, transferOrderId);

        public Task<List<TransferAvailabilityDto>> CheckAvailabilityAsync(int companyId, int transferOrderId)
            => _repository.CheckAvailabilityAsync(companyId, transferOrderId);
    }
}
