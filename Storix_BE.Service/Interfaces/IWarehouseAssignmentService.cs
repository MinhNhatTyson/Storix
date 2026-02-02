using Storix_BE.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storix_BE.Service.Interfaces
{
    public interface IWarehouseAssignmentService
    {
        Task<List<WarehouseAssignment>> GetAssignmentsByCompanyAsync(int companyId, int callerRoleId);
        Task<List<WarehouseAssignment>> GetAssignmentsByWarehouseAsync(int companyId, int callerRoleId, int warehouseId);
        Task<WarehouseAssignment> AssignWarehouseAsync(int companyId, int callerRoleId, AssignWarehouseRequest request);
        Task<bool> UnassignWarehouseAsync(int companyId, int callerRoleId, int userId, int warehouseId);
        Task<int> CountAssignmentsByUserAsync(int userId);
        Task<int> UpdateRoleInAssignmentsAsync(int userId, string roleInWarehouse);
    }

    public sealed record AssignWarehouseRequest(int UserId, int WarehouseId);
}
