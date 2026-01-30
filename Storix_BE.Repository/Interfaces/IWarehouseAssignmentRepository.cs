using Storix_BE.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Interfaces
{
    public interface IWarehouseAssignmentRepository
    {
        Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId);
        Task<WarehouseAssignment?> GetAssignmentAsync(int userId, int warehouseId);
        Task<List<WarehouseAssignment>> GetAssignmentsByCompanyIdAsync(int companyId);
        Task<List<WarehouseAssignment>> GetAssignmentsByWarehouseIdAsync(int warehouseId);
        Task<int> AddAssignmentAsync(WarehouseAssignment assignment);
        Task<bool> RemoveAssignmentAsync(WarehouseAssignment assignment);
    }
}
