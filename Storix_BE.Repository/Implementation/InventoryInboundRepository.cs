using ClosedXML.Excel;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Storix_BE.Domain.Context;
using Storix_BE.Domain.Models;
using Storix_BE.Repository.DTO;
using Storix_BE.Repository.Interfaces;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Implementation
{
    public class InventoryInboundRepository : IInventoryInboundRepository
    {
        private readonly StorixDbContext _context;

        public InventoryInboundRepository(StorixDbContext context)
        {
            _context = context;
        }

        public async Task<InboundRequest> CreateInventoryInboundTicketRequest(InboundRequest request, IEnumerable<ProductPrice>? productPrices = null)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Request must contain at least one product item
            if (request.InboundOrderItems == null || !request.InboundOrderItems.Any())
            {
                throw new InvalidOperationException("InboundRequest must contain at least one InboundOrderItem describing product and expected quantity.");
            }

            // Basic per-item validation
            var invalidItem = request.InboundOrderItems.FirstOrDefault(i => i.ProductId == null || i.ExpectedQuantity == null || i.ExpectedQuantity <= 0);
            if (invalidItem != null)
            {
                throw new InvalidOperationException("All InboundOrderItems must specify a ProductId and ExpectedQuantity > 0.");
            }

            // Verify products exist
            var productIds = request.InboundOrderItems.Select(i => i.ProductId!.Value).Distinct().ToList();
            var existingProductIds = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            var missing = productIds.Except(existingProductIds).ToList();
            if (missing.Any())
            {
                throw new InvalidOperationException($"Products not found: {string.Join(',', missing)}");
            }

            // Optional: validate referenced warehouse/supplier/requestedBy exist when provided
            if (request.WarehouseId.HasValue)
            {
                var wh = await _context.Warehouses.FindAsync(request.WarehouseId.Value).ConfigureAwait(false);
                if (wh == null) throw new InvalidOperationException($"Warehouse with id {request.WarehouseId.Value} not found.");
            }
            if (request.SupplierId.HasValue)
            {
                var sup = await _context.Suppliers.FindAsync(request.SupplierId.Value).ConfigureAwait(false);
                if (sup == null) throw new InvalidOperationException($"Supplier with id {request.SupplierId.Value} not found.");
            }

            // Set defaults
            request.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                request.Status = "Pending";
            }

            // Ensure child items are correctly linked to the request
            foreach (var item in request.InboundOrderItems)
            {
                item.InboundRequest = request;
            }

            // Persist within a transaction — also persist productPrices if provided
            await using var tx = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                _context.InboundRequests.Add(request);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                if (productPrices != null)
                {
                    var pricesList = productPrices.ToList();
                    if (pricesList.Any())
                    {
                        // Ensure Date is set
                        var nowDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        foreach (var p in pricesList)
                        {
                            if (p.Date == null)
                                p.Date = nowDate;
                        }

                        _context.ProductPrices.AddRange(pricesList);
                        await _context.SaveChangesAsync().ConfigureAwait(false);
                    }
                }

                await tx.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }

            return request;
        }

        public async Task<InboundRequest> UpdateInventoryInboundTicketRequestStatus(int ticketRequestId, int approverId, string status)
        {
            if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status is required.", nameof(status));

            var inbound = await _context.InboundRequests
                .FirstOrDefaultAsync(r => r.Id == ticketRequestId)
                .ConfigureAwait(false);

            if (inbound == null)
            {
                throw new InvalidOperationException($"InboundRequest with id {ticketRequestId} not found.");
            }

            inbound.Status = status;
            inbound.ApprovedBy = approverId;
            inbound.ApprovedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _context.SaveChangesAsync().ConfigureAwait(false);

            return inbound;
        }

        public async Task<InboundOrder> CreateInboundOrderFromRequestAsync(int inboundRequestId, int createdBy, int? staffId)
        {
            var inboundRequest = await _context.InboundRequests
                .Include(r => r.InboundOrderItems)
                .FirstOrDefaultAsync(r => r.Id == inboundRequestId)
                .ConfigureAwait(false);

            if (inboundRequest == null)
                throw new InvalidOperationException($"InboundRequest with id {inboundRequestId} not found.");

            if (!string.Equals(inboundRequest.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("InboundRequest must be in 'Approved' status to create an InboundOrder (ticket).");

            // Build new InboundOrder inheriting fields except CreatedAt, Status, CreatedBy
            var inboundOrder = new InboundOrder
            {
                WarehouseId = inboundRequest.WarehouseId,
                SupplierId = inboundRequest.SupplierId,
                CreatedBy = createdBy,
                StaffId = staffId, 
                Status = "Created",
                InboundRequestId = inboundRequest.Id,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
                ReferenceCode = $"INB-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
            };

            // Copy items: ExpectedQuantity from request items. Do not link InboundOrderId yet.
            foreach (var reqItem in inboundRequest.InboundOrderItems)
            {
                var orderItem = new InboundOrderItem
                {
                    ProductId = reqItem.ProductId,
                    ExpectedQuantity = reqItem.ExpectedQuantity,
                    ReceivedQuantity = reqItem.ReceivedQuantity // usually null/0 initially
                };
                inboundOrder.InboundOrderItems.Add(orderItem);
            }

            await using var tx = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                _context.InboundOrders.Add(inboundOrder);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                await tx.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }

            return inboundOrder;
        }

        public async Task<InboundOrder> UpdateInboundOrderItemsAsync(int inboundOrderId, IEnumerable<InboundOrderItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                .FirstOrDefaultAsync(o => o.Id == inboundOrderId)
                .ConfigureAwait(false);

            if (order == null)
                throw new InvalidOperationException($"InboundOrder with id {inboundOrderId} not found.");

            // Validate items: each must have ProductId and at least one quantity to update
            foreach (var i in items)
            {
                if (i.ProductId == null || i.ProductId <= 0)
                    throw new InvalidOperationException("Each item must have a valid ProductId.");
            }

            // Update existing items or add new ones. We will not remove items that are not present in the payload.
            foreach (var incoming in items)
            {
                if (incoming.Id > 0)
                {
                    var existing = order.InboundOrderItems.FirstOrDefault(x => x.Id == incoming.Id);
                    if (existing == null)
                        throw new InvalidOperationException($"InboundOrderItem with id {incoming.Id} not found in order {inboundOrderId}.");

                    // Update allowed fields
                    existing.ExpectedQuantity = incoming.ExpectedQuantity;
                    existing.ReceivedQuantity = incoming.ReceivedQuantity;
                    existing.ProductId = incoming.ProductId;
                }
                else
                {
                    // Try to find by ProductId first
                    var existingByProduct = order.InboundOrderItems.FirstOrDefault(x => x.ProductId == incoming.ProductId);
                    if (existingByProduct != null)
                    {
                        existingByProduct.ExpectedQuantity = incoming.ExpectedQuantity;
                        existingByProduct.ReceivedQuantity = incoming.ReceivedQuantity;
                    }
                    else
                    {
                        var newItem = new InboundOrderItem
                        {
                            ProductId = incoming.ProductId,
                            ExpectedQuantity = incoming.ExpectedQuantity,
                            ReceivedQuantity = incoming.ReceivedQuantity,
                            InboundOrder = order
                        };
                        order.InboundOrderItems.Add(newItem);
                    }
                }
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            return order;
        }
        public async Task<List<InboundRequest>> GetAllInboundRequestsAsync(int companyId)
        {
            return await _context.InboundRequests
                .Include(r => r.InboundOrderItems)
                .Include(r => r.Supplier)
                .Include(r => r.Warehouse)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Where(r=> r.RequestedByNavigation.CompanyId == companyId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        
        public async Task<List<InboundOrder>> GetAllInboundOrdersAsync(int companyId)
        {
            return await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Supplier)
                .Include(o => o.Warehouse)
                .Include(o => o.CreatedByNavigation)
                .Where(o=> o.CreatedByNavigation.CompanyId == companyId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        public async Task<InboundRequest> GetInboundRequestByIdAsync(int companyId, int id)
        {
            var request = await _context.InboundRequests
                .Include(r => r.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(r => r.Supplier)
                .Include(r => r.Warehouse)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Where(r=> r.RequestedByNavigation.CompanyId == companyId)
                .FirstOrDefaultAsync(r => r.Id == id)
                .ConfigureAwait(false);

            if (request == null)
                throw new InvalidOperationException($"InboundRequest with id {id} not found.");

            return request;
        }
        
        public async Task<InboundOrder> GetInboundOrderByIdAsync(int companyId, int id)
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Supplier)
                .Include(o => o.Warehouse)
                .Include(o => o.CreatedByNavigation)
                .Include(o => o.InboundRequest)
                .Where(o=> o.CreatedByNavigation.CompanyId == companyId)
                .FirstOrDefaultAsync(o => o.Id == id)
                .ConfigureAwait(false);

            if (order == null)
                throw new InvalidOperationException($"InboundOrder with id {id} not found.");

            return order;
        }
        public async Task<bool> InboundRequestCodeExistsAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return await _context.InboundRequests.AnyAsync(r => r.Code == code).ConfigureAwait(false);
        }
        public async Task<List<InboundOrder>> GetInboundOrdersByStaffAsync(int companyId, int staffId)
        {
            if (companyId <= 0) throw new ArgumentException("Invalid company id.", nameof(companyId));
            if (staffId <= 0) throw new ArgumentException("Invalid staff id.", nameof(staffId));

            var query = _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Supplier)
                .Include(o => o.Warehouse)
                .Include(o => o.CreatedByNavigation)
                .Include(o => o.Staff)
                .Where(o => o.StaffId == staffId && o.Warehouse != null && o.Warehouse.CompanyId == companyId)
                .OrderByDescending(o => o.CreatedAt);

            return await query.ToListAsync().ConfigureAwait(false);
        }
        public async Task<List<InboundRequestExportDto>> GetInboundRequestsForExportAsync(int companyId)
        {
            return await _context.InboundRequests
                .Include(r => r.Warehouse)
                .Include(r => r.Supplier)
                .Include(r => r.RequestedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Select(r => new InboundRequestExportDto
                {
                    Id = r.Id,
                    Code = r.Code,
                    Warehouse = r.Warehouse != null ? r.Warehouse.Name : null,
                    Supplier = r.Supplier != null ? r.Supplier.Name : null,
                    RequestedBy = r.RequestedByNavigation != null ? r.RequestedByNavigation.FullName : null,
                    ApprovedBy = r.ApprovedByNavigation != null ? r.ApprovedByNavigation.FullName : null,
                    Status = r.Status,
                    TotalPrice = r.TotalPrice,
                    OrderDiscount = r.OrderDiscount,
                    FinalPrice = r.FinalPrice,
                    ExpectedDate = r.ExpectedDate,
                    Note = r.Note,
                    CreatedAt = r.CreatedAt,
                    ApprovedAt = r.ApprovedAt,
                    ItemCount = r.InboundOrderItems != null ? r.InboundOrderItems.Count : 0
                })
                .Where(dto => dto.RequestedBy != null) // keep same scoping as other methods (RequestedByNavigation.CompanyId)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<List<InboundOrderExportDto>> GetInboundOrdersForExportAsync(int companyId)
        {
            return await _context.InboundOrders
                .Include(o => o.Warehouse)
                .Include(o => o.Supplier)
                .Include(o => o.CreatedByNavigation)
                .Include(o => o.Staff)
                .Include(o => o.InboundRequest)
                .Select(o => new InboundOrderExportDto
                {
                    Id = o.Id,
                    ReferenceCode = o.ReferenceCode,
                    Warehouse = o.Warehouse != null ? o.Warehouse.Name : null,
                    Supplier = o.Supplier != null ? o.Supplier.Name : null,
                    CreatedBy = o.CreatedByNavigation != null ? o.CreatedByNavigation.FullName : null,
                    Staff = o.Staff != null ? o.Staff.FullName : null,
                    Status = o.Status,
                    TotalPrice = o.InboundRequest != null ? o.InboundRequest.FinalPrice : null,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.InboundOrderItems != null ? o.InboundOrderItems.Count : 0
                })
                .Where(dto => dto.CreatedBy != null) // keep same scoping as other methods (CreatedByNavigation.CompanyId)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public byte[] ExportInboundRequestsToCsv(List<InboundRequestExportDto> requests)
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(requests);
                writer.Flush();
            }

            return memoryStream.ToArray();
        }

        public byte[] ExportInboundRequestsToExcel(List<InboundRequestExportDto> requests)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("InboundRequests");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Code";
            worksheet.Cell(1, 3).Value = "Warehouse";
            worksheet.Cell(1, 4).Value = "Supplier";
            worksheet.Cell(1, 5).Value = "Requested By";
            worksheet.Cell(1, 6).Value = "Approved By";
            worksheet.Cell(1, 7).Value = "Status";
            worksheet.Cell(1, 8).Value = "Total Price";
            worksheet.Cell(1, 9).Value = "Order Discount";
            worksheet.Cell(1, 10).Value = "Final Price";
            worksheet.Cell(1, 11).Value = "Expected Date";
            worksheet.Cell(1, 12).Value = "Note";
            worksheet.Cell(1, 13).Value = "Created At";
            worksheet.Cell(1, 14).Value = "Approved At";
            worksheet.Cell(1, 15).Value = "Item Count";

            for (int i = 0; i < requests.Count; i++)
            {
                var row = i + 2;
                var r = requests[i];

                worksheet.Cell(row, 1).Value = r.Id;
                worksheet.Cell(row, 2).Value = r.Code;
                worksheet.Cell(row, 3).Value = r.Warehouse;
                worksheet.Cell(row, 4).Value = r.Supplier;
                worksheet.Cell(row, 5).Value = r.RequestedBy;
                worksheet.Cell(row, 6).Value = r.ApprovedBy;
                worksheet.Cell(row, 7).Value = r.Status;
                worksheet.Cell(row, 8).Value = r.TotalPrice;
                worksheet.Cell(row, 9).Value = r.OrderDiscount;
                worksheet.Cell(row, 10).Value = r.FinalPrice;
                worksheet.Cell(row, 11).Value = r.ExpectedDate?.ToString();
                worksheet.Cell(row, 12).Value = r.Note;
                worksheet.Cell(row, 13).Value = r.CreatedAt;
                worksheet.Cell(row, 14).Value = r.ApprovedAt;
                worksheet.Cell(row, 15).Value = r.ItemCount;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportInboundOrdersToCsv(List<InboundOrderExportDto> orders)
        {
            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(orders);
                writer.Flush();
            }

            return memoryStream.ToArray();
        }

        public byte[] ExportInboundOrdersToExcel(List<InboundOrderExportDto> orders)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("InboundOrders");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Reference Code";
            worksheet.Cell(1, 3).Value = "Warehouse";
            worksheet.Cell(1, 4).Value = "Supplier";
            worksheet.Cell(1, 5).Value = "Created By";
            worksheet.Cell(1, 6).Value = "Staff";
            worksheet.Cell(1, 7).Value = "Status";
            worksheet.Cell(1, 8).Value = "Total Price";
            worksheet.Cell(1, 9).Value = "Created At";
            worksheet.Cell(1, 10).Value = "Item Count";

            for (int i = 0; i < orders.Count; i++)
            {
                var row = i + 2;
                var o = orders[i];

                worksheet.Cell(row, 1).Value = o.Id;
                worksheet.Cell(row, 2).Value = o.ReferenceCode;
                worksheet.Cell(row, 3).Value = o.Warehouse;
                worksheet.Cell(row, 4).Value = o.Supplier;
                worksheet.Cell(row, 5).Value = o.CreatedBy;
                worksheet.Cell(row, 6).Value = o.Staff;
                worksheet.Cell(row, 7).Value = o.Status;
                worksheet.Cell(row, 8).Value = o.TotalPrice;
                worksheet.Cell(row, 9).Value = o.CreatedAt;
                worksheet.Cell(row, 10).Value = o.ItemCount;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
