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
        Task<Warehouse> CreateWarehouseAsync(int callerCompanyId, CreateWarehouseRequest request);
    }

    public sealed record AssignWarehouseRequest(int UserId, int WarehouseId);
    public sealed record CreateWarehouseRequest(
        string? Name,
        string? Address,
        string? Description,
        int? Width,
        int? Height,
        int? Length,
        int? GridSize,
        string? Status,
        string? Image,
        IEnumerable<ZoneDto>? Zones,
        IEnumerable<NodeDto>? Nodes,
        IEnumerable<EdgeDto>? Edges
    );

    public sealed record ZoneDto(
        string Id,
        string? Name,
        double? X,
        double? Y,
        double? Width,
        double? Height,
        string? Color,
        IEnumerable<ShelfDto>? Shelves
    );

    public sealed record ShelfDto(
        string Id,
        string? Code,
        double? X,
        double? Y,
        double? Width,
        double? Height,
        IEnumerable<AccessNodeDto>? AccessNodes,
        IEnumerable<LevelDto>? Levels
    );

    public sealed record AccessNodeDto(
        string Id,
        string? Side,
        double? X,
        double? Y,
        double? Radius
    );

    public sealed record LevelDto(
        string Id,
        string? Name,
        IEnumerable<BinDto>? Bins
    );

    public sealed record BinDto(
        string Id,
        string? Name
    );

    public sealed record NodeDto(
        string Id,
        double? X,
        double? Y,
        double? Radius,
        string? Color,
        string? Type
    );

    public sealed record EdgeDto(
        string Id,
        string From,
        string To,
        double? Distance
    );
}
