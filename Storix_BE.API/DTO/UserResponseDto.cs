using System;

namespace Storix_BE.API.DTO
{
    public sealed record UserResponseDto(
        int Id,
        int? CompanyId,
        string? FullName,
        string? Email,
        string? Phone,
        int? RoleId,
        string? RoleName,
        int? WarehouseId,
        string? WarehouseName,
        string? Status,
        DateTime? CreatedAt,
        DateTime? UpdatedAt);
}
