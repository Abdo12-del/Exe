using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class InventoryCountService : IInventoryCountService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public InventoryCountService(AppDbContext context, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<List<InventoryCountItemDto>> PrepareInventorySheetAsync(int warehouseId)
    {
        var warehouseProducts = await _context.WarehouseProducts
            .AsNoTracking()
            .Include(wp => wp.Product)
            .Where(wp => wp.WarehouseId == warehouseId && wp.Product.IsActive)
            .OrderBy(wp => wp.Product.NameAr)
            .ToListAsync();

        return warehouseProducts.Select(wp => new InventoryCountItemDto(
            wp.ProductId,
            wp.Product.NameAr,
            wp.Product.Barcode,
            wp.CurrentQuantity,
            wp.CurrentQuantity, // Initially matches theoretical
            0
        )).ToList();
    }

    public async Task<InventoryCount> PostInventoryAdjustmentAsync(PostInventoryCountDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            throw new DomainException("لا توجد أصناف في ورقة الجرد.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var warehouse = await _context.Warehouses.FindAsync(dto.WarehouseId);
            if (warehouse == null) throw new DomainException("المستودع المحدد غير موجود.");

            string countNum = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var invCount = new InventoryCount
            {
                CountNumber = countNum,
                WarehouseId = dto.WarehouseId,
                CreatedByUserId = dto.UserId,
                CreatedAt = DateTime.UtcNow,
                Status = InventoryCountStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                Notes = dto.Notes
            };

            _context.InventoryCounts.Add(invCount);
            await _context.SaveChangesAsync();

            var countItems = new List<InventoryCountItem>();

            foreach (var item in dto.Items)
            {
                var wp = await _context.WarehouseProducts
                    .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId && x.ProductId == item.ProductId);

                if (wp == null) continue;

                decimal diff = item.CountedQuantity - wp.CurrentQuantity;

                countItems.Add(new InventoryCountItem
                {
                    InventoryCountId = invCount.Id,
                    ProductId = item.ProductId,
                    TheoreticalQuantity = wp.CurrentQuantity,
                    PhysicalQuantity = item.CountedQuantity,
                    Difference = diff,
                    Notes = diff != 0 ? $"تسوية جردية: فارق {diff:+0.##;-0.##;0}" : "مطابق"
                });

                // Apply stock movement if discrepancy exists
                if (diff != 0)
                {
                    wp.CurrentQuantity = item.CountedQuantity;

                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = item.ProductId,
                        WarehouseId = dto.WarehouseId,
                        MovementType = MovementType.InventoryAdjustment,
                        QuantityChange = diff,
                        ResultingQuantity = item.CountedQuantity,
                        UnitCost = 0,
                        ReferenceDocumentType = "InventoryCount",
                        ReferenceDocumentId = invCount.Id,
                        UserId = dto.UserId,
                        Notes = $"تسوية جردية ({countNum})"
                    });
                }
            }

            _context.InventoryCountItems.AddRange(countItems);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(dto.UserId);
            await _auditService.LogAsync(dto.UserId, user?.Username ?? "Unknown", "PostInventoryCount", "InventoryCount", invCount.Id.ToString(), null, new { countNum, itemsCount = countItems.Count });

            await _unitOfWork.CommitAsync();

            return invCount;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
