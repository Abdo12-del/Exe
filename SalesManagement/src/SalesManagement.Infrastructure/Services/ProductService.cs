using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public ProductService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<ProductDto>> GetAllAsync(string? searchQuery = null, int? categoryId = null, bool lowStockOnly = false)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Unit)
            .Include(p => p.WarehouseProducts)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var q = searchQuery.Trim();
            query = query.Where(p => p.NameAr.Contains(q) || p.Barcode.Contains(q) || p.Sku.Contains(q));
        }

        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query.ToListAsync();

        var result = products.Select(p => new ProductDto(
            p.Id,
            p.Sku,
            p.Barcode,
            p.NameAr,
            p.NameFr,
            p.CategoryId,
            p.Category.NameAr,
            p.BrandId,
            p.Brand?.Name,
            p.UnitId,
            p.Unit.NameAr,
            p.PurchasePrice,
            p.SalePrice,
            p.WholesalePrice,
            p.MinimumStock,
            p.TaxPercent,
            p.GetTotalStock(),
            p.IsActive
        )).ToList();

        if (lowStockOnly)
        {
            result = result.Where(p => p.CurrentStock <= p.MinimumStock).ToList();
        }

        return result;
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode)
    {
        var p = await _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Unit)
            .Include(x => x.WarehouseProducts)
            .FirstOrDefaultAsync(x => x.Barcode == barcode && x.IsActive);

        if (p == null) return null;

        return new ProductDto(
            p.Id, p.Sku, p.Barcode, p.NameAr, p.NameFr,
            p.CategoryId, p.Category.NameAr,
            p.BrandId, p.Brand?.Name,
            p.UnitId, p.Unit.NameAr,
            p.PurchasePrice, p.SalePrice, p.WholesalePrice,
            p.MinimumStock, p.TaxPercent, p.GetTotalStock(), p.IsActive
        );
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Unit)
            .Include(x => x.WarehouseProducts)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return null;

        return new ProductDto(
            p.Id, p.Sku, p.Barcode, p.NameAr, p.NameFr,
            p.CategoryId, p.Category.NameAr,
            p.BrandId, p.Brand?.Name,
            p.UnitId, p.Unit.NameAr,
            p.PurchasePrice, p.SalePrice, p.WholesalePrice,
            p.MinimumStock, p.TaxPercent, p.GetTotalStock(), p.IsActive
        );
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, int userId)
    {
        var product = new Product
        {
            Sku = dto.Sku,
            Barcode = dto.Barcode,
            NameAr = dto.NameAr,
            NameFr = dto.NameFr,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            UnitId = dto.UnitId,
            PurchasePrice = dto.PurchasePrice,
            SalePrice = dto.SalePrice,
            WholesalePrice = dto.WholesalePrice,
            MinimumStock = dto.MinimumStock,
            TaxPercent = dto.TaxPercent,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "CreateProduct", "Product", product.Id.ToString(), null, product);

        return (await GetByIdAsync(product.Id))!;
    }

    public async Task<ProductDto> UpdateAsync(int id, CreateProductDto dto, int userId)
    {
        var p = await _context.Products.FindAsync(id);
        if (p == null) throw new InvalidOperationException("المنتج غير موجود");

        var oldValues = new { p.NameAr, p.Barcode, p.PurchasePrice, p.SalePrice };

        p.Sku = dto.Sku;
        p.Barcode = dto.Barcode;
        p.NameAr = dto.NameAr;
        p.NameFr = dto.NameFr;
        p.CategoryId = dto.CategoryId;
        p.BrandId = dto.BrandId;
        p.UnitId = dto.UnitId;
        p.PurchasePrice = dto.PurchasePrice;
        p.SalePrice = dto.SalePrice;
        p.WholesalePrice = dto.WholesalePrice;
        p.MinimumStock = dto.MinimumStock;
        p.TaxPercent = dto.TaxPercent;
        p.Description = dto.Description;
        p.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "UpdateProduct", "Product", p.Id.ToString(), oldValues, p);

        return (await GetByIdAsync(p.Id))!;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var p = await _context.Products.FindAsync(id);
        if (p == null) return false;

        p.IsActive = false; // Soft delete
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "DeactivateProduct", "Product", id.ToString(), null, null);
        return true;
    }
}
