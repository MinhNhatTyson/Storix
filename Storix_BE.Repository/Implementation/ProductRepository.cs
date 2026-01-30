using Microsoft.EntityFrameworkCore;
using Storix_BE.Domain.Context;
using Storix_BE.Domain.Models;
using Storix_BE.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storix_BE.Repository.Implementation
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly StorixDbContext _context;

        public ProductRepository(StorixDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Type)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id, int companyId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Type)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        }

        public async Task<Product?> GetBySkuAsync(string sku, int companyId)
        {
            if (string.IsNullOrWhiteSpace(sku)) return null;
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Type)
                .FirstOrDefaultAsync(p => p.Sku == sku && p.CompanyId == companyId);
        }

        public async Task<List<Product>> GetProductsByCompanyIdAsync(int companyId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Type)
                .Where(p => p.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            product.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            if (product.TypeId.HasValue)
            {
                var type = await _context.Types.FindAsync(product.TypeId.Value);
                if (type == null)
                    throw new InvalidOperationException($"Product type with id {product.TypeId.Value} not found.");
                product.Type = type;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Reload to ensure navigation is populated for the returned entity
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Type)
                .FirstOrDefaultAsync(p => p.Id == product.Id) ?? product;
        }

        public async Task<int> UpdateAsync(Product product)
        {
            // Validate existing product exists
            var existing = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existing == null)
                throw new InvalidOperationException($"Product with id {product.Id} not found.");

            // If TypeId changed / provided, validate and attach
            if (product.TypeId.HasValue)
            {
                var type = await _context.Types.FindAsync(product.TypeId.Value);
                if (type == null)
                    throw new InvalidOperationException($"Product type with id {product.TypeId.Value} not found.");
                existing.TypeId = product.TypeId;
                existing.Type = type;
            }
            else
            {
                existing.TypeId = null;
                existing.Type = null;
            }

            // Patch other fields
            existing.CompanyId = product.CompanyId;
            existing.Sku = product.Sku;
            existing.Name = product.Name;
            existing.Category = product.Category;
            existing.Unit = product.Unit;
            existing.Weight = product.Weight;
            existing.Description = product.Description;
            existing.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            _context.Products.Update(existing);
            return await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoveAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ProductType>> GetAllProductTypesAsync()
        {
            return await _context.Types
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .ToListAsync();
        }
    }
}
