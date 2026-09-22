using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public PurchaseService(AppDbContext context, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<Purchase> CreatePurchaseAsync(CreatePurchaseDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            throw new DomainException("فاتورة الشراء لا تحتوي على أي مواد.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId);
            if (supplier == null) throw new DomainException("الممون المحدد غير موجود.");

            var warehouse = await _context.Warehouses.FindAsync(dto.WarehouseId);
            if (warehouse == null) throw new DomainException("المستودع المحدد غير موجود.");

            decimal subtotal = 0;
            var purchaseItems = new List<PurchaseItem>();
            var stockMovements = new List<StockMovement>();

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null) continue;

                // Update product purchase price to latest acquisition cost
                if (itemDto.UnitBuyPrice > 0)
                {
                    product.PurchasePrice = itemDto.UnitBuyPrice;
                }

                // Increment stock in warehouse
                var wp = await _context.WarehouseProducts
                    .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId && x.ProductId == product.Id);

                if (wp == null)
                {
                    wp = new WarehouseProduct
                    {
                        WarehouseId = dto.WarehouseId,
                        ProductId = product.Id,
                        CurrentQuantity = itemDto.Quantity,
                        ReservedQuantity = 0
                    };
                    _context.WarehouseProducts.Add(wp);
                }
                else
                {
                    wp.CurrentQuantity += itemDto.Quantity;
                }

                decimal lineTotal = (itemDto.Quantity * itemDto.UnitBuyPrice) - itemDto.DiscountAmount;
                subtotal += (itemDto.Quantity * itemDto.UnitBuyPrice);

                purchaseItems.Add(new PurchaseItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitBuyPrice = itemDto.UnitBuyPrice,
                    DiscountAmount = itemDto.DiscountAmount,
                    TotalLineAmount = lineTotal
                });

                stockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    WarehouseId = dto.WarehouseId,
                    MovementType = MovementType.Purchase,
                    QuantityChange = itemDto.Quantity,
                    ResultingQuantity = wp.CurrentQuantity,
                    UnitCost = itemDto.UnitBuyPrice,
                    ReferenceDocumentType = "Purchase",
                    UserId = dto.UserId,
                    Notes = $"توريد بضاعة من الممون: {supplier.Name}"
                });
            }

            decimal totalAmount = Math.Max(0, subtotal - dto.DiscountAmount);
            decimal paid = Math.Min(totalAmount, dto.PaidAmount);
            if (dto.PaymentMethod == PaymentMethod.Credit)
            {
                paid = 0;
            }
            decimal remainingDebt = Math.Max(0, totalAmount - paid);

            // Update supplier debt (what we owe them)
            if (remainingDebt > 0)
            {
                supplier.CurrentDebt += remainingDebt;
            }

            string purchaseNumber = $"PUR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var purchase = new Purchase
            {
                PurchaseNumber = purchaseNumber,
                SupplierInvoiceNumber = dto.SupplierInvoiceNumber,
                Timestamp = DateTime.UtcNow,
                UserId = dto.UserId,
                SupplierId = dto.SupplierId,
                WarehouseId = dto.WarehouseId,
                Subtotal = subtotal,
                DiscountAmount = dto.DiscountAmount,
                TotalAmount = totalAmount,
                PaidAmount = paid,
                RemainingDebt = remainingDebt,
                PaymentStatus = remainingDebt == 0 ? PaymentStatus.Paid : (paid > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid),
                Notes = dto.Notes,
                Items = purchaseItems
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            // Link movements
            foreach (var sm in stockMovements)
            {
                sm.ReferenceDocumentId = purchase.Id;
                _context.StockMovements.Add(sm);
            }

            // Register Payment
            if (paid > 0)
            {
                _context.PurchasePayments.Add(new PurchasePayment
                {
                    PurchaseId = purchase.Id,
                    CashRegisterId = dto.CashRegisterId,
                    UserId = dto.UserId,
                    Timestamp = DateTime.UtcNow,
                    PaymentMethod = dto.PaymentMethod,
                    Amount = paid,
                    Notes = $"تسديد فاتورة شراء {purchaseNumber}"
                });

                if (dto.CashRegisterId.HasValue && dto.PaymentMethod == PaymentMethod.Cash)
                {
                    var register = await _context.CashRegisters.FindAsync(dto.CashRegisterId.Value);
                    if (register != null)
                    {
                        register.CurrentBalance -= paid;
                        _context.CashTransactions.Add(new CashTransaction
                        {
                            CashRegisterId = register.Id,
                            UserId = dto.UserId,
                            Timestamp = DateTime.UtcNow,
                            TransactionType = CashTransactionType.SupplierDebtPayment,
                            Amount = -paid,
                            BalanceAfter = register.CurrentBalance,
                            ReferenceDocumentType = "Purchase",
                            ReferenceDocumentId = purchase.Id,
                            Notes = $"مدفوعات مشتريات نقدية للممون: {supplier.Name}"
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(dto.UserId);
            await _auditService.LogAsync(dto.UserId, user?.Username ?? "Unknown", "CreatePurchase", "Purchase", purchase.Id.ToString(), null, new { purchaseNumber, totalAmount, supplierId = supplier.Id });

            await _unitOfWork.CommitAsync();

            return purchase;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Purchase>> GetPurchasesHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null, int? supplierId = null)
    {
        var query = _context.Purchases
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.User)
            .Include(p => p.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();

        if (fromDate.HasValue) query = query.Where(p => p.Timestamp >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(p => p.Timestamp <= toDate.Value);
        if (supplierId.HasValue && supplierId > 0) query = query.Where(p => p.SupplierId == supplierId.Value);

        return await query.OrderByDescending(p => p.Timestamp).ToListAsync();
    }
}
