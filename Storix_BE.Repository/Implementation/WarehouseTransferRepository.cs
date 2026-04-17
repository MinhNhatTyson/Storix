using Microsoft.EntityFrameworkCore;
using Storix_BE.Domain.Context;
using Storix_BE.Domain.Models;
using Storix_BE.Repository.Interfaces;
using Storix_BE.Service.Interfaces;

namespace Storix_BE.Repository.Implementation
{
    public class WarehouseTransferRepository : IWarehouseTransferRepository
    {
        private const int ManagerRoleId = 3;
        private const int StaffRoleId = 4;

        private readonly StorixDbContext _context;

        public WarehouseTransferRepository(StorixDbContext context)
        {
            _context = context;
        }

        public async Task<TransferOrderDetailDto> CreateDraftAsync(int companyId, int createdBy, CreateTransferOrderRequest request)
        {
            ValidateCompanyAndUser(companyId, createdBy);
            ValidateWarehouses(request.SourceWarehouseId, request.DestinationWarehouseId);

            await EnsureManagerAsync(createdBy, companyId);
            await EnsureWarehouseAccessibleAsync(request.SourceWarehouseId, companyId);
            await EnsureWarehouseAccessibleAsync(request.DestinationWarehouseId, companyId);

            if (request.CarrierUserId is > 0)
                await EnsureStaffAssignedToWarehouseAsync(request.CarrierUserId.Value, request.SourceWarehouseId, companyId);

            var entity = new TransferOrder
            {
                SourceWarehouseId = request.SourceWarehouseId,
                DestinationWarehouseId = request.DestinationWarehouseId,
                CreatedBy = createdBy,
                Status = TransferStatuses.Draft,
                CreatedAt = UtcNow()
            };

            _context.TransferOrders.Add(entity);
            await _context.SaveChangesAsync();

            if (request.CarrierUserId is > 0)
                await AddActivityAsync(createdBy, $"CARRIER:{request.CarrierUserId.Value}", entity.Id);

            await AddActivityAsync(createdBy, "TRANSFER_CREATED_DRAFT", entity.Id);

            return request.SubmitAfterCreate
                ? await SubmitAsync(companyId, createdBy, entity.Id)
                : await GetByIdAsync(companyId, entity.Id);
        }

        public async Task<TransferOrderDetailDto> UpdateDraftAsync(int companyId, int actorUserId, int transferOrderId, UpdateTransferOrderRequest request)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            EnsureCanEdit(order.Status);
            await EnsureManagerAndOwnerAsync(order, actorUserId, companyId);

            ValidateWarehouses(request.SourceWarehouseId, request.DestinationWarehouseId);
            await EnsureWarehouseAccessibleAsync(request.SourceWarehouseId, companyId);
            await EnsureWarehouseAccessibleAsync(request.DestinationWarehouseId, companyId);

            order.SourceWarehouseId = request.SourceWarehouseId;
            order.DestinationWarehouseId = request.DestinationWarehouseId;
            await _context.SaveChangesAsync();

            if (request.CarrierUserId is > 0)
                await AddActivityAsync(actorUserId, $"CARRIER:{request.CarrierUserId.Value}", order.Id);

            await AddActivityAsync(actorUserId, "TRANSFER_UPDATED_DRAFT", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> AddItemAsync(int companyId, int actorUserId, int transferOrderId, AddTransferOrderItemRequest request)
        {
            ValidatePositive(request.ProductId, nameof(request.ProductId));
            ValidatePositive(request.Quantity, nameof(request.Quantity));

            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            EnsureCanEdit(order.Status);
            await EnsureManagerAndOwnerAsync(order, actorUserId, companyId);
            await EnsureProductInCompanyAsync(request.ProductId, companyId);

            var existing = await _context.TransferOrderItems
                .FirstOrDefaultAsync(i => i.TransferOrderId == order.Id && i.ProductId == request.ProductId);

            if (existing == null)
            {
                _context.TransferOrderItems.Add(new TransferOrderItem
                {
                    TransferOrderId = order.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }
            else
            {
                existing.Quantity = (existing.Quantity ?? 0) + request.Quantity;
            }

            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, "TRANSFER_ITEM_ADDED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> UpdateItemAsync(int companyId, int actorUserId, int transferOrderId, int itemId, UpdateTransferOrderItemRequest request)
        {
            ValidatePositive(itemId, nameof(itemId));
            ValidatePositive(request.ProductId, nameof(request.ProductId));
            ValidatePositive(request.Quantity, nameof(request.Quantity));

            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            EnsureCanEdit(order.Status);
            await EnsureManagerAndOwnerAsync(order, actorUserId, companyId);
            await EnsureProductInCompanyAsync(request.ProductId, companyId);

            var item = await GetTransferItemAsync(order.Id, itemId);
            item.ProductId = request.ProductId;
            item.Quantity = request.Quantity;

            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, "TRANSFER_ITEM_UPDATED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> RemoveItemAsync(int companyId, int actorUserId, int transferOrderId, int itemId)
        {
            ValidatePositive(itemId, nameof(itemId));

            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            EnsureCanEdit(order.Status);
            await EnsureManagerAndOwnerAsync(order, actorUserId, companyId);

            var item = await GetTransferItemAsync(order.Id, itemId);
            _context.TransferOrderItems.Remove(item);

            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, "TRANSFER_ITEM_REMOVED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<List<TransferStaffSuggestionDto>> SuggestStaffAsync(int companyId, int actorUserId, int transferOrderId)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureManagerAndOwnerAsync(order, actorUserId, companyId);

            var sourceWarehouseId = order.SourceWarehouseId ?? 0;
            if (sourceWarehouseId <= 0)
                throw new InvalidOperationException("Transfer source warehouse is invalid.");

            var assignedStaff = await _context.WarehouseAssignments
                .AsNoTracking()
                .Where(a => a.WarehouseId == sourceWarehouseId
                    && a.User != null
                    && (a.User.RoleId ?? 0) == StaffRoleId
                    && (a.User.CompanyId ?? 0) == companyId)
                .Select(a => new
                {
                    a.UserId,
                    a.User!.FullName,
                    a.User.Email
                })
                .Distinct()
                .ToListAsync();

            if (!assignedStaff.Any())
                return new List<TransferStaffSuggestionDto>();

            var staffIds = assignedStaff.Select(x => x.UserId).ToList();
            var assignmentCounts = await _context.WarehouseAssignments
                .AsNoTracking()
                .Where(a => staffIds.Contains(a.UserId))
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var activeTaskCounts = await GetActiveTransferTaskCountsAsync(companyId, staffIds);

            return assignedStaff
                .Select(staff =>
                {
                    var activeTaskCount = activeTaskCounts.TryGetValue(staff.UserId, out var active) ? active : 0;
                    var assignedWarehouseCount = assignmentCounts.TryGetValue(staff.UserId, out var assigned) ? assigned : 0;
                    var score = Math.Max(0, 100 - (activeTaskCount * 15)) + Math.Min(assignedWarehouseCount, 5);
                    var reason = activeTaskCount == 0
                        ? "Rảnh, phù hợp để nhận phiếu chuyển mới."
                        : $"Đang xử lý {activeTaskCount} phiếu chuyển.";

                    return new TransferStaffSuggestionDto(
                        staff.UserId,
                        staff.FullName,
                        staff.Email,
                        assignedWarehouseCount,
                        activeTaskCount,
                        score,
                        reason);
                })
                .OrderByDescending(x => x.SuggestionScore)
                .ThenBy(x => x.ActiveTransferTaskCount)
                .ThenBy(x => x.FullName)
                .ToList();
        }

        public async Task<TransferOrderDetailDto> AssignCarrierAsync(int companyId, int actorUserId, int transferOrderId, int carrierUserId)
        {
            ValidatePositive(carrierUserId, nameof(carrierUserId));

            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            EnsureCanEdit(order.Status);
            await EnsureManagerAndOwnerAsync(order, actorUserId, companyId);
            await EnsureStaffAssignedToWarehouseAsync(carrierUserId, order.SourceWarehouseId ?? 0, companyId);

            await AddActivityAsync(actorUserId, $"CARRIER:{carrierUserId}", order.Id);
            await AddActivityAsync(actorUserId, "TRANSFER_CARRIER_ASSIGNED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> SubmitAsync(int companyId, int actorUserId, int transferOrderId)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureManagerAndOwnerAsync(order, actorUserId, companyId);

            if (!IsInStatuses(order.Status, TransferStatuses.Draft, TransferStatuses.Rejected))
                throw new InvalidOperationException("Only DRAFT/REJECTED can be submitted.");

            if (!await _context.TransferOrderItems.AnyAsync(x => x.TransferOrderId == order.Id))
                throw new InvalidOperationException("Transfer must contain at least one item.");

            order.Status = TransferStatuses.PendingApproval;
            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, "TRANSFER_SUBMITTED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> ApproveAsync(int companyId, int actorUserId, int transferOrderId, int? receiverStaffId = null)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureManagerAsync(actorUserId, companyId);

            if (!string.Equals(order.Status, TransferStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only PENDING_APPROVAL can be approved.");

            var items = await GetTransferItemsAsync(order.Id);
            if (!items.Any()) throw new InvalidOperationException("Transfer must contain at least one item.");

            var inventories = await GetSourceInventoriesAsync(order, items);
            ValidateStockForApproval(items, inventories);
            if (receiverStaffId is > 0)
                await EnsureStaffAssignedToWarehouseAsync(receiverStaffId.Value, order.DestinationWarehouseId ?? 0, companyId);

            await ApproveTransferAsync(order, actorUserId, receiverStaffId, items, inventories);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> RejectAsync(int companyId, int actorUserId, int transferOrderId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("reason is required", nameof(reason));

            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureManagerAsync(actorUserId, companyId);

            if (!string.Equals(order.Status, TransferStatuses.PendingApproval, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only PENDING_APPROVAL can be rejected.");

            order.Status = TransferStatuses.Rejected;
            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, $"TRANSFER_REJECTED:{reason}", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> StartPickingAsync(int companyId, int actorUserId, int transferOrderId)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureStaffAssignedToWarehouseAsync(actorUserId, order.SourceWarehouseId ?? 0, companyId);
            EnsureStatus(order, TransferStatuses.Approved);

            order.Status = TransferStatuses.Picking;
            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, "TRANSFER_PICKING_STARTED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> MarkPackedAsync(int companyId, int actorUserId, int transferOrderId)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureStaffAssignedToWarehouseAsync(actorUserId, order.SourceWarehouseId ?? 0, companyId);
            EnsureStatus(order, TransferStatuses.Picking);

            order.Status = TransferStatuses.Packed;
            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, "TRANSFER_PACKED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> ShipAsync(int companyId, int actorUserId, int transferOrderId)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureStaffAssignedToWarehouseAsync(actorUserId, order.SourceWarehouseId ?? 0, companyId);
            EnsureStatus(order, TransferStatuses.Packed);

            var items = await GetTransferItemsAsync(order.Id);
            var inventories = await GetSourceInventoriesAsync(order, items);
            var now = UtcNow();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var line in items)
                {
                    var inv = inventories.FirstOrDefault(i => i.ProductId == line.ProductId);
                    var qty = line.Quantity ?? 0;

                    if (inv == null || (inv.ReservedQuantity ?? 0) < qty || (inv.Quantity ?? 0) < qty)
                        throw new InvalidOperationException($"OUT_OF_STOCK_ON_SHIP ProductId={line.ProductId}");

                    inv.ReservedQuantity = (inv.ReservedQuantity ?? 0) - qty;
                    inv.Quantity = (inv.Quantity ?? 0) - qty;
                    inv.LastUpdated = now;
                }

                order.Status = TransferStatuses.InTransit;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            await AddActivityAsync(actorUserId, "TRANSFER_SHIPPED", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> ReceiveAsync(int companyId, int actorUserId, int transferOrderId, ReceiveTransferOrderRequest request)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureStaffAssignedToWarehouseAsync(actorUserId, order.DestinationWarehouseId ?? 0, companyId);
            EnsureStatus(order, TransferStatuses.InTransit);

            var lines = request.Items?.ToList() ?? new List<ReceiveTransferItemRequest>();
            if (!lines.Any()) throw new InvalidOperationException("receive items required");

            var reqByProduct = (await GetTransferItemsAsync(order.Id))
                .Where(i => i.ProductId.HasValue)
                .ToDictionary(i => i.ProductId!.Value, i => i.Quantity ?? 0);

            await ReceiveTransferAsync(order, actorUserId, lines, reqByProduct);
            await AddActivityAsync(actorUserId, string.IsNullOrWhiteSpace(request.Note) ? "TRANSFER_RECEIVED" : $"TRANSFER_RECEIVED:{request.Note}", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> QualityCheckAsync(int companyId, int actorUserId, int transferOrderId, TransferQualityCheckRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureStaffAssignedToWarehouseAsync(actorUserId, order.SourceWarehouseId ?? 0, companyId);

            var items = request.Items?.ToList() ?? new List<TransferQualityCheckItemRequest>();
            if (!items.Any()) throw new InvalidOperationException("quality items required");

            var reqByProduct = (await GetTransferItemsAsync(order.Id))
                .Where(i => i.ProductId.HasValue)
                .ToDictionary(i => i.ProductId!.Value, i => i.Quantity ?? 0);

            ValidateQualityItems(items, reqByProduct);
            await ApplyQualityCheckAsync(order, actorUserId, request.Note, items);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<TransferOrderDetailDto> CancelAsync(int companyId, int actorUserId, int transferOrderId, string? reason)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            await EnsureManagerAsync(actorUserId, companyId);

            if (!IsInStatuses(order.Status, TransferStatuses.Draft, TransferStatuses.PendingApproval, TransferStatuses.Approved))
                throw new InvalidOperationException("Only DRAFT/PENDING_APPROVAL/APPROVED can be cancelled.");

            order.Status = TransferStatuses.Cancelled;
            await _context.SaveChangesAsync();
            await AddActivityAsync(actorUserId, string.IsNullOrWhiteSpace(reason) ? "TRANSFER_CANCELLED" : $"TRANSFER_CANCELLED:{reason}", order.Id);
            return await GetByIdAsync(companyId, order.Id);
        }

        public async Task<List<TransferOrderListDto>> GetAllAsync(int companyId, int? sourceWarehouseId, int? destinationWarehouseId, string? status)
        {
            var query = _context.TransferOrders
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.TransferOrderItems)
                .Where(t => t.SourceWarehouse != null
                    && t.DestinationWarehouse != null
                    && t.SourceWarehouse.CompanyId == companyId
                    && t.DestinationWarehouse.CompanyId == companyId);

            if (sourceWarehouseId.HasValue)
                query = query.Where(t => t.SourceWarehouseId == sourceWarehouseId.Value);

            if (destinationWarehouseId.HasValue)
                query = query.Where(t => t.DestinationWarehouseId == destinationWarehouseId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status != null && t.Status.ToUpper() == status.Trim().ToUpper());

            var data = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
            return data.Select(MapList).ToList();
        }

        public async Task<TransferOrderDetailDto> GetByIdAsync(int companyId, int transferOrderId)
        {
            var order = await _context.TransferOrders
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.OutboundOrder)
                    .ThenInclude(o => o.OutboundOrderItems)
                        .ThenInclude(i => i.Product)
                .Include(t => t.InboundOrder)
                    .ThenInclude(i => i.InboundOrderItems)
                        .ThenInclude(i => i.Product)
                .Include(t => t.TransferOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(t => t.Id == transferOrderId);

            if (order == null)
                throw new InvalidOperationException("Transfer order not found.");

            if ((order.SourceWarehouse?.CompanyId ?? 0) != companyId || (order.DestinationWarehouse?.CompanyId ?? 0) != companyId)
                throw new InvalidOperationException("Transfer order out of company scope.");

            var timeline = await _context.ActivityLogs
                .Where(a => a.Entity == "TransferOrder" && a.EntityId == order.Id)
                .OrderBy(a => a.Timestamp)
                .Select(a => new TransferOrderTimelineDto(a.Id, a.Action, a.Timestamp, a.UserId, a.User != null ? a.User.FullName : null))
                .ToListAsync();

            return MapDetail(order, timeline);
        }

        public async Task<List<TransferAvailabilityDto>> CheckAvailabilityAsync(int companyId, int transferOrderId)
        {
            var order = await GetOrderInCompanyAsync(companyId, transferOrderId);
            var items = await _context.TransferOrderItems
                .Include(i => i.Product)
                .Where(i => i.TransferOrderId == order.Id)
                .ToListAsync();

            var productIds = items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
            var inventories = await _context.Inventories
                .Where(i => i.WarehouseId == order.SourceWarehouseId && i.ProductId.HasValue && productIds.Contains(i.ProductId.Value))
                .ToListAsync();

            return items.Select(i =>
            {
                var inv = inventories.FirstOrDefault(x => x.ProductId == i.ProductId);
                var available = (inv?.Quantity ?? 0) - (inv?.ReservedQuantity ?? 0);
                var required = i.Quantity ?? 0;
                return new TransferAvailabilityDto(i.ProductId ?? 0, i.Product?.Name, required, available, available >= required);
            }).ToList();
        }

        private async Task<TransferOrder> GetOrderInCompanyAsync(int companyId, int transferOrderId)
        {
            ValidateCompanyId(companyId);
            ValidatePositive(transferOrderId, nameof(transferOrderId));

            var order = await _context.TransferOrders
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .FirstOrDefaultAsync(t => t.Id == transferOrderId);

            if (order == null)
                throw new InvalidOperationException("Transfer order not found.");

            if ((order.SourceWarehouse?.CompanyId ?? 0) != companyId || (order.DestinationWarehouse?.CompanyId ?? 0) != companyId)
                throw new InvalidOperationException("Transfer order out of company scope.");

            return order;
        }

        private async Task EnsureManagerAndOwnerAsync(TransferOrder order, int actorUserId, int companyId)
        {
            await EnsureManagerAsync(actorUserId, companyId);
            if (order.CreatedBy != actorUserId)
                throw new InvalidOperationException("Only creator manager can modify this transfer.");
        }

        private async Task EnsureManagerAsync(int userId, int companyId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new InvalidOperationException("User not found.");
            if ((user.CompanyId ?? 0) != companyId) throw new InvalidOperationException("User out of company scope.");
            if ((user.RoleId ?? 0) != ManagerRoleId) throw new InvalidOperationException("Only Manager(roleId=3).");
        }

        private async Task EnsureStaffAssignedToWarehouseAsync(int userId, int warehouseId, int companyId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new InvalidOperationException("User not found.");
            if ((user.CompanyId ?? 0) != companyId) throw new InvalidOperationException("User out of company scope.");
            if ((user.RoleId ?? 0) != StaffRoleId) throw new InvalidOperationException("Only Staff(roleId=4).");

            var assigned = await _context.WarehouseAssignments.AsNoTracking().AnyAsync(a => a.WarehouseId == warehouseId && a.UserId == userId);
            if (!assigned) throw new InvalidOperationException("Staff is not assigned to warehouse.");
        }

        private async Task EnsureWarehouseAccessibleAsync(int warehouseId, int companyId)
        {
            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
            if (warehouse == null) throw new InvalidOperationException($"Warehouse {warehouseId} not found.");
            if ((warehouse.CompanyId ?? 0) != companyId) throw new InvalidOperationException($"Warehouse {warehouseId} out of company scope.");
            if (string.Equals(warehouse.Status, "inactive", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"Warehouse {warehouseId} inactive.");
        }

        private async Task EnsureProductInCompanyAsync(int productId, int companyId)
        {
            var ok = await _context.Products.AsNoTracking().AnyAsync(p => p.Id == productId && p.CompanyId == companyId);
            if (!ok) throw new InvalidOperationException($"Product {productId} not found in company.");
        }

        private async Task<List<TransferOrderItem>> GetTransferItemsAsync(int orderId)
        {
            return await _context.TransferOrderItems
                .Where(x => x.TransferOrderId == orderId)
                .ToListAsync();
        }

        private async Task<TransferOrderItem> GetTransferItemAsync(int orderId, int itemId)
        {
            var item = await _context.TransferOrderItems.FirstOrDefaultAsync(x => x.Id == itemId && x.TransferOrderId == orderId);
            if (item == null) throw new InvalidOperationException("Transfer item not found.");
            return item;
        }

        private async Task<List<Inventory>> GetSourceInventoriesAsync(TransferOrder order, IEnumerable<TransferOrderItem> items)
        {
            var productIds = items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
            return await _context.Inventories
                .Where(i => i.WarehouseId == order.SourceWarehouseId && i.ProductId.HasValue && productIds.Contains(i.ProductId.Value))
                .ToListAsync();
        }

        private static void ValidateStockForApproval(IEnumerable<TransferOrderItem> items, List<Inventory> inventories)
        {
            foreach (var line in items)
            {
                var inv = inventories.FirstOrDefault(i => i.ProductId == line.ProductId);
                var qty = line.Quantity ?? 0;
                var available = (inv?.Quantity ?? 0) - (inv?.ReservedQuantity ?? 0);

                if (qty <= 0 || inv == null || available < qty)
                    throw new InvalidOperationException($"OUT_OF_STOCK ProductId={line.ProductId}, available={available}, required={qty}");
            }
        }

        private static void ValidateQualityItems(IEnumerable<TransferQualityCheckItemRequest> items, Dictionary<int, int> reqByProduct)
        {
            foreach (var line in items)
            {
                if (!reqByProduct.ContainsKey(line.ProductId))
                    throw new InvalidOperationException($"INVALID_PRODUCT {line.ProductId}");

                if (line.OkQuantity < 0 || line.BadQuantity < 0)
                    throw new InvalidOperationException("INVALID_QUANTITY");

                if (line.OkQuantity + line.BadQuantity > reqByProduct[line.ProductId])
                    throw new InvalidOperationException($"QUALITY_OVERFLOW ProductId={line.ProductId}");
            }
        }

        private async Task AddActivityAsync(int userId, string action, int transferOrderId)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                Entity = "TransferOrder",
                EntityId = transferOrderId,
                Timestamp = UtcNow()
            });
            await _context.SaveChangesAsync();
        }

        private async Task<int?> ResolveLinkedOutboundIdAsync(int transferOrderId)
        {
            var action = await _context.ActivityLogs
                .Where(a => a.Entity == "TransferOrder" && a.EntityId == transferOrderId && a.Action != null && a.Action.StartsWith("LINK_OUTBOUND:"))
                .OrderByDescending(a => a.Timestamp)
                .Select(a => a.Action)
                .FirstOrDefaultAsync();

            return ParseLinkedId(action);
        }

        private static OutboundOrder BuildOutboundOrder(TransferOrder order, int actorUserId, int? carrierId, DateTime now, IEnumerable<TransferOrderItem> items)
        {
            var outbound = new OutboundOrder
            {
                WarehouseId = order.SourceWarehouseId,
                Destination = order.DestinationWarehouse?.Name,
                CreatedBy = actorUserId,
                StaffId = carrierId,
                Status = "Picking",
                Note = $"AUTO_FROM_TRANSFER#{order.Id}",
                CreatedAt = now
            };

            foreach (var line in items)
            {
                outbound.OutboundOrderItems.Add(new OutboundOrderItem
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    PricingMethod = "LastPurchasePrice"
                });
            }

            return outbound;
        }

        private static InboundOrder BuildInboundOrder(TransferOrder order, int actorUserId, int? receiverStaffId, DateTime now, IEnumerable<TransferOrderItem> items)
        {
            var inbound = new InboundOrder
            {
                WarehouseId = order.DestinationWarehouseId,
                CreatedBy = actorUserId,
                StaffId = receiverStaffId,
                Status = "Created",
                ReferenceCode = $"AUTO_FROM_TRANSFER#{order.Id}",
                CreatedAt = now
            };

            foreach (var line in items)
            {
                inbound.InboundOrderItems.Add(new InboundOrderItem
                {
                    ProductId = line.ProductId,
                    ExpectedQuantity = line.Quantity,
                    ReceivedQuantity = 0
                });
            }

            return inbound;
        }

        private async Task<int?> ResolveCarrierUserIdAsync(int transferOrderId)
        {
            var action = await _context.ActivityLogs
                .Where(a => a.Entity == "TransferOrder" && a.EntityId == transferOrderId && a.Action != null && a.Action.StartsWith("CARRIER:"))
                .OrderByDescending(a => a.Timestamp)
                .Select(a => a.Action)
                .FirstOrDefaultAsync();

            return ParseLinkedId(action);
        }

        private async Task<int> GetActiveTransferTaskCountAsync(int companyId, int staffUserId)
        {
            var counts = await GetActiveTransferTaskCountsAsync(companyId, new[] { staffUserId });
            return counts.TryGetValue(staffUserId, out var count) ? count : 0;
        }

        private async Task<Dictionary<int, int>> GetActiveTransferTaskCountsAsync(int companyId, IEnumerable<int> staffUserIds)
        {
            var staffIds = staffUserIds.Distinct().ToList();
            if (!staffIds.Any()) return new Dictionary<int, int>();

            var activeStatuses = new[]
            {
                TransferStatuses.Approved,
                TransferStatuses.Picking,
                TransferStatuses.Packed,
                TransferStatuses.InTransit,
                TransferStatuses.QualityChecked,
                TransferStatuses.QualityIssue
            };

            var carrierActions = staffIds.Select(id => $"CARRIER:{id}").ToList();
            var orderIdsByStaff = await _context.ActivityLogs
                .AsNoTracking()
                .Where(a => a.Entity == "TransferOrder" && a.Action != null && carrierActions.Contains(a.Action))
                .Select(a => new { a.EntityId, a.Action })
                .ToListAsync();

            var orderIds = orderIdsByStaff.Select(x => x.EntityId).Distinct().ToList();
            if (!orderIds.Any())
                return staffIds.ToDictionary(id => id, _ => 0);

            var orders = await _context.TransferOrders
                .AsNoTracking()
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Where(t => orderIds.Contains(t.Id)
                    && activeStatuses.Contains((t.Status ?? string.Empty).ToUpper())
                    && (t.SourceWarehouse!.CompanyId ?? 0) == companyId
                    && (t.DestinationWarehouse!.CompanyId ?? 0) == companyId)
                .Select(t => new { t.Id })
                .ToListAsync();

            var activeOrderIds = orders.Select(x => x.Id).ToHashSet();
            return orderIdsByStaff
                .Where(x => activeOrderIds.Contains(x.EntityId))
                .GroupBy(x => ParseCarrierUserId(x.Action))
                .Where(g => g.Key.HasValue)
                .ToDictionary(g => g.Key!.Value, g => g.Count());
        }

        private async Task ApproveTransferAsync(TransferOrder order, int actorUserId, int? receiverStaffId, List<TransferOrderItem> items, List<Inventory> inventories)
        {
            var now = UtcNow();
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var line in items)
                {
                    var inv = inventories.First(i => i.ProductId == line.ProductId);
                    var qty = line.Quantity ?? 0;

                    inv.Quantity = (inv.Quantity ?? 0) - qty;
                    inv.LastUpdated = now;

                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        WarehouseId = order.SourceWarehouseId,
                        ProductId = line.ProductId,
                        QuantityChange = -qty,
                        TransactionType = "TransferApproveOut",
                        ReferenceId = order.Id,
                        PerformedBy = actorUserId,
                        CreatedAt = now
                    });
                }

                var carrierId = await ResolveCarrierUserIdAsync(order.Id);
                var outbound = BuildOutboundOrder(order, actorUserId, carrierId, now, items);
                var inbound = BuildInboundOrder(order, actorUserId, receiverStaffId, now, items);

                _context.OutboundOrders.Add(outbound);
                _context.InboundOrders.Add(inbound);
                order.OutboundOrder = outbound;
                order.InboundOrder = inbound;
                order.OutboundOrderId = null;
                order.InboundOrderId = null;
                order.Status = TransferStatuses.Approved;
                await _context.SaveChangesAsync();

                order.OutboundOrderId = outbound.Id;
                order.InboundOrderId = inbound.Id;
                await _context.SaveChangesAsync();

                await AddActivityAsync(actorUserId, $"LINK_OUTBOUND:{outbound.Id}", order.Id);
                await AddActivityAsync(actorUserId, $"LINK_INBOUND:{inbound.Id}", order.Id);
                await AddActivityAsync(actorUserId, "TRANSFER_APPROVED", order.Id);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task ReceiveTransferAsync(TransferOrder order, int actorUserId, List<ReceiveTransferItemRequest> lines, Dictionary<int, int> reqByProduct)
        {
            var now = UtcNow();
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var line in lines)
                {
                    if (!reqByProduct.ContainsKey(line.ProductId)) throw new InvalidOperationException($"INVALID_PRODUCT {line.ProductId}");
                    if (line.ReceivedQuantity < 0) throw new InvalidOperationException("INVALID_RECEIVED_QUANTITY");

                    var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.WarehouseId == order.DestinationWarehouseId && i.ProductId == line.ProductId);
                    if (inv == null)
                    {
                        inv = new Inventory
                        {
                            WarehouseId = order.DestinationWarehouseId,
                            ProductId = line.ProductId,
                            Quantity = 0,
                            ReservedQuantity = 0,
                            LastUpdated = now
                        };
                        _context.Inventories.Add(inv);
                    }

                    inv.Quantity = (inv.Quantity ?? 0) + line.ReceivedQuantity;
                    inv.LastUpdated = now;
                }

                order.Status = TransferStatuses.Completed;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task ApplyQualityCheckAsync(TransferOrder order, int actorUserId, string? note, List<TransferQualityCheckItemRequest> items)
        {
            var outboundId = await ResolveLinkedOutboundIdAsync(order.Id);
            if (outboundId == null)
                throw new InvalidOperationException("Linked outbound order not found.");

            var outbound = await _context.OutboundOrders.Include(o => o.OutboundOrderItems).FirstOrDefaultAsync(o => o.Id == outboundId.Value);
            if (outbound == null)
                throw new InvalidOperationException("Linked outbound order not found.");

            var status = items.Any(i => i.BadQuantity > 0) ? TransferStatuses.QualityIssue : TransferStatuses.QualityChecked;
            order.Status = status;
            outbound.Status = items.Any(i => i.BadQuantity > 0) ? "IssueReported" : "QualityCheck";
            await _context.SaveChangesAsync();

            await AddActivityAsync(actorUserId, string.IsNullOrWhiteSpace(note) ? "QUALITY_CHECK" : $"QUALITY_CHECK:{note}", order.Id);
            foreach (var line in items)
            {
                var itemNote = string.IsNullOrWhiteSpace(line.Note)
                    ? $"QUALITY_ITEM:{line.ProductId}:{line.OkQuantity}:{line.BadQuantity}"
                    : $"QUALITY_ITEM:{line.ProductId}:{line.OkQuantity}:{line.BadQuantity}:{line.Note}";
                await AddActivityAsync(actorUserId, itemNote, order.Id);
            }

            await AddActivityAsync(actorUserId, $"OUTBOUND_QUALITY_UPDATED:{outbound.Id}", order.Id);
        }

        private static TransferOrderListDto MapList(TransferOrder t)
        {
            var totalItems = t.TransferOrderItems?.Count ?? 0;
            var totalQuantity = t.TransferOrderItems?.Sum(x => x.Quantity ?? 0) ?? 0;
            return new TransferOrderListDto(t.Id, t.SourceWarehouseId, t.SourceWarehouse?.Name, t.DestinationWarehouseId, t.DestinationWarehouse?.Name, t.CreatedBy, t.CreatedByNavigation?.FullName, t.Status, t.CreatedAt, totalItems, totalQuantity);
        }

        private static TransferOrderDetailDto MapDetail(TransferOrder t, IEnumerable<TransferOrderTimelineDto> timeline)
        {
            var items = (t.TransferOrderItems ?? new List<TransferOrderItem>())
                .Select(i => new TransferOrderItemDto(i.Id, i.ProductId, i.Product?.Name, i.Quantity))
                .ToList();

            var outbound = t.OutboundOrder == null ? null : new TransferOrderLinkedOutboundDto(
                t.OutboundOrder.Id,
                t.OutboundOrder.WarehouseId,
                t.OutboundOrder.Destination,
                t.OutboundOrder.StaffId,
                t.OutboundOrder.Status,
                t.OutboundOrder.Note,
                t.OutboundOrder.CreatedAt,
                (t.OutboundOrder.OutboundOrderItems ?? new List<OutboundOrderItem>())
                    .Select(x => new OutboundOrderItemDto(x.Id, x.ProductId, x.Product?.Name, x.Quantity, x.PricingMethod))
                    .ToList());

            var inbound = t.InboundOrder == null ? null : new TransferOrderLinkedInboundDto(
                t.InboundOrder.Id,
                t.InboundOrder.WarehouseId,
                t.InboundOrder.StaffId,
                t.InboundOrder.Status,
                t.InboundOrder.ReferenceCode,
                t.InboundOrder.CreatedAt,
                (t.InboundOrder.InboundOrderItems ?? new List<InboundOrderItem>())
                    .Select(x => new InboundOrderItemDto(x.Id, x.ProductId, x.Product?.Name, x.ExpectedQuantity, x.ReceivedQuantity))
                    .ToList());

            return new TransferOrderDetailDto(
                t.Id,
                t.SourceWarehouseId,
                t.SourceWarehouse?.Name,
                t.DestinationWarehouseId,
                t.DestinationWarehouse?.Name,
                t.CreatedBy,
                t.CreatedByNavigation?.FullName,
                t.Status,
                t.CreatedAt,
                t.OutboundOrderId,
                t.InboundOrderId,
                outbound,
                inbound,
                items,
                timeline);
        }

        private static bool IsInStatuses(string? status, params string[] statuses)
        {
            return statuses.Any(s => string.Equals(status, s, StringComparison.OrdinalIgnoreCase));
        }

        private static int? ParseLinkedId(string? action)
        {
            if (string.IsNullOrWhiteSpace(action)) return null;
            var parts = action.Split(':');
            if (parts.Length != 2) return null;
            return int.TryParse(parts[1], out var id) ? id : null;
        }

        private static int? ParseCarrierUserId(string? action)
        {
            if (string.IsNullOrWhiteSpace(action)) return null;
            var parts = action.Split(':');
            if (parts.Length != 2) return null;
            return int.TryParse(parts[1], out var id) ? id : null;
        }

        private static void ValidateCompanyAndUser(int companyId, int userId)
        {
            ValidateCompanyId(companyId);
            ValidatePositive(userId, nameof(userId));
        }

        private static void ValidateCompanyId(int companyId)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid company id", nameof(companyId));
        }

        private static void ValidatePositive(int value, string name)
        {
            if (value <= 0) throw new ArgumentException($"{name} must be positive", name);
        }

        private static void ValidateWarehouses(int sourceWarehouseId, int destinationWarehouseId)
        {
            if (sourceWarehouseId <= 0) throw new ArgumentException("SourceWarehouseId must be positive", nameof(sourceWarehouseId));
            if (destinationWarehouseId <= 0) throw new ArgumentException("DestinationWarehouseId must be positive", nameof(destinationWarehouseId));
            if (sourceWarehouseId == destinationWarehouseId) throw new InvalidOperationException("Source and destination must be different.");
        }

        private static DateTime UtcNow() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }
}
