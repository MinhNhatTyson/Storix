# Storix_BE - Warehouse Management System

Hệ thống quản lý kho hàng được xây dựng với ASP.NET Core 8.0 theo kiến trúc Clean Architecture.

## Cấu trúc dự án (Project-based)

Dự án được tổ chức theo Clean Architecture với 4 layers chính:

```
Storix_BE.sln
├── Storix_BE.API/          # Presentation Layer
├── Storix_BE.Application/  # Application Layer (Business Logic)
├── Storix_BE.Domain/       # Domain Layer (Core Entities)
└── Storix_BE.Infrastructure/ # Infrastructure Layer (Data Access)
```

## Dependencies Flow

```
API → Application → Domain
  ↓         ↓
Infrastructure → Domain
```

**Quy tắc dependencies:**
- **Domain**: Không phụ thuộc vào layer nào
- **Application**: Chỉ phụ thuộc vào Domain
- **Infrastructure**: Phụ thuộc vào Domain và Application
- **API**: Phụ thuộc vào Application và Infrastructure

## Cấu trúc chi tiết

### Storix_BE.Domain
- **Entities/**: Domain entities (Product, Warehouse, Inventory, etc.)
- **Interfaces/**: Repository và service interfaces
- **Common/**: Base classes, enums, constants, exceptions

### Storix_BE.Application
- **Services/**: Application services (business logic)
- **DTOs/**: Data Transfer Objects
- **Mappings/**: AutoMapper profiles
- **Interfaces/**: Service interfaces

### Storix_BE.Infrastructure
- **Data/**: DbContext, configurations, migrations
- **Repositories/**: Repository implementations
- **Services/**: External service implementations

### Storix_BE.API
- **Controllers/**: API controllers
- **Program.cs**: Startup và dependency injection configuration

## Cách chạy

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run API
cd Storix_BE.API
dotnet run
```

API sẽ chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`
Swagger UI: `https://localhost:5001/swagger`

## Công nghệ sử dụng

- .NET 8.0
- ASP.NET Core Web API
- Clean Architecture
- Swagger/OpenAPI

## Phát triển tiếp theo

1. Thêm Entity Framework Core vào Infrastructure
2. Tạo các entities cho warehouse management (Product, Warehouse, Inventory, etc.)
3. Implement repositories và services
4. Thêm validation và error handling
5. Thêm authentication và authorization
