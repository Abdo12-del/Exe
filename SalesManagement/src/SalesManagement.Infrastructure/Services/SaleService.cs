using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public SaleService(AppDbContext context, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            throw new DomainException("سلة المبيعات فارغة. يرجى إضافة منتج واحد على الأقل.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // 1. Validate Cash Register
            var register = await _context.CashRegisters.FindAsync(dto.CashRegisterId);
            if (register == null || !register.IsOpen)
                throw new ClosedCashRegisterException(dto.CashRegisterId);

            // 2. Validate Customer
            var customer = await _context.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
                throw new DomainException("الزبون المحدد غير موجود.");

            // 3. Process items and validate stock
            decimal subtotal = 0;
            decimal totalCost = 0;
            var saleItems = new List<SaleItem>();
            var stockMovements = new List<StockMovement>();

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null || !product.IsActive)
                    throw new DomainException($"المنتج رقم {itemDto.ProductId} غير متوفر.");

                var wp = await _context.WarehouseProducts
                    .FirstOrDefaultAsync(x => x.WarehouseId == dto.WarehouseId && x.ProductId == product.Id);

                decimal currentStock = wp?.CurrentQuantity ?? 0;
                if (currentStock < itemDto.Quantity)
                {
                    throw new InsufficientStockException(product.Id, product.NameAr, itemDto.Quantity, currentStock);
                }

                // Deduct stock
                wp!.CurrentQuantity -= itemDto.Quantity;

                decimal lineTotal = (itemDto.Quantity * itemDto.UnitSalePrice) - itemDto.DiscountAmount;
                decimal lineCost = itemDto.Quantity * product.PurchasePrice;
                decimal lineProfit = lineTotal - lineCost;

                subtotal += (itemDto.Quantity * itemDto.UnitSalePrice);
                totalCost += lineCost;

                var saleItem = new SaleItem
                {
                    ProductId = product.Id,
                    UnitId = itemDto.UnitId,
                    Quantity = itemDto.Quantity,
                    UnitSalePrice = itemDto.UnitSalePrice,
                    UnitCostPrice = product.PurchasePrice,
                    DiscountAmount = itemDto.DiscountAmount,
                    TaxPercent = product.TaxPercent,
                    TotalLineAmount = lineTotal,
                    LineProfit = lineProfit
                };
                saleItems.Add(saleItem);

                // Movement
                stockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    WarehouseId = dto.WarehouseId,
                    MovementType = MovementType.Sale,
                    QuantityChange = -itemDto.Quantity,
                    ResultingQuantity = wp.CurrentQuantity,
                    UnitCost = product.PurchasePrice,
                    ReferenceDocumentType = "Sale",
                    UserId = dto.UserId,
                    Notes = $"فاتورة بيع"
                });
            }

            decimal totalAmount = Math.Max(0, subtotal - dto.DiscountAmount);
            decimal netProfit = totalAmount - totalCost;

            decimal paid = Math.Min(totalAmount, dto.PaidAmount);
            if (dto.PaymentMethod == PaymentMethod.Credit)
            {
                paid = 0;
            }
            decimal remainingDebt = Math.Max(0, totalAmount - paid);

            // 4. Validate Credit Limit
            if (remainingDebt > 0)
            {
                if (customer.Id == 1) // Walk-in customer cannot have debt
                    throw new DomainException("لا يمكن البيع بالدين للزبون العابر. يرجى اختيار زبون مسجل.");

                if (customer.CurrentDebt + remainingDebt > customer.CreditLimit)
                {
                    throw new CreditLimitExceededException(customer.Id, customer.Name, customer.CurrentDebt, customer.CreditLimit, remainingDebt);
                }

                customer.CurrentDebt += remainingDebt;
            }

            // 5. Generate Invoice Number
            string invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var sale = new Sale
            {
                InvoiceNumber = invoiceNumber,
                Timestamp = DateTime.UtcNow,
                UserId = dto.UserId,
                CustomerId = dto.CustomerId,
                WarehouseId = dto.WarehouseId,
                CashRegisterId = dto.CashRegisterId,
                Subtotal = subtotal,
                DiscountAmount = dto.DiscountAmount,
                TaxAmount = 0,
                TotalAmount = totalAmount,
                TotalCost = totalCost,
                NetProfit = netProfit,
                PaidAmount = paid,
                RemainingDebt = remainingDebt,
                PaymentStatus = remainingDebt == 0 ? PaymentStatus.Paid : (paid > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid),
                PaymentMethod = dto.PaymentMethod,
                Notes = dto.Notes,
                Items = saleItems
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync(); // get sale.Id

            // Link movements
            foreach (var sm in stockMovements)
            {
                sm.ReferenceDocumentId = sale.Id;
                _context.StockMovements.Add(sm);
            }

            // 6. Cash Register Transaction & Payment
            if (paid > 0)
            {
                var salePayment = new SalePayment
                {
                    SaleId = sale.Id,
                    CashRegisterId = dto.CashRegisterId,
                    UserId = dto.UserId,
                    Timestamp = DateTime.UtcNow,
                    PaymentMethod = dto.PaymentMethod,
                    Amount = paid,
                    Notes = $"قبض فاتورة {invoiceNumber}"
                };
                _context.SalePayments.Add(salePayment);

                if (dto.PaymentMethod == PaymentMethod.Cash || dto.PaymentMethod == PaymentMethod.Mixed)
                {
                    register.CurrentBalance += paid;
                    var cashTrans = new CashTransaction
                    {
                        CashRegisterId = register.Id,
                        UserId = dto.UserId,
                        Timestamp = DateTime.UtcNow,
                        TransactionType = CashTransactionType.SaleReceipt,
                        Amount = paid,
                        BalanceAfter = register.CurrentBalance,
                        ReferenceDocumentType = "Sale",
                        ReferenceDocumentId = sale.Id,
                        Notes = $"مبيعات نقدية - فاتورة {invoiceNumber}"
                    };
                    _context.CashTransactions.Add(cashTrans);
                }
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(dto.UserId);
            await _auditService.LogAsync(dto.UserId, user?.Username ?? "Unknown", "CreateSale", "Sale", sale.Id.ToString(), null, new { invoiceNumber, totalAmount, netProfit });

            await _unitOfWork.CommitAsync();

            return (await GetSaleByIdAsync(sale.Id))!;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<SaleDto>> GetSalesHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null, int? customerId = null, string? search = null)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.User)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Where(s => !s.IsCancelled);

        if (fromDate.HasValue) query = query.Where(s => s.Timestamp >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(s => s.Timestamp <= toDate.Value);
        if (customerId.HasValue && customerId.Value > 0) query = query.Where(s => s.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.InvoiceNumber.Contains(s) || x.Customer.Name.Contains(s));
        }

        var list = await query.OrderByDescending(s => s.Timestamp).ToListAsync();

        return list.Select(s => new SaleDto(
            s.Id,
            s.InvoiceNumber,
            s.Timestamp,
            s.CustomerId,
            s.Customer.Name,
            s.User.FullName,
            s.Subtotal,
            s.DiscountAmount,
            s.TaxAmount,
            s.TotalAmount,
            s.NetProfit,
            s.PaidAmount,
            s.RemainingDebt,
            s.PaymentStatus,
            s.PaymentMethod,
            s.Items.Select(it => new SaleItemDetailDto(
                it.Id,
                it.ProductId,
                it.Product.NameAr,
                it.Product.Barcode,
                it.Quantity,
                it.UnitSalePrice,
                it.TotalLineAmount,
                it.LineProfit
            )).ToList()
        )).ToList();
    }

    public async Task<SaleDto?> GetSaleByIdAsync(long saleId)
    {
        var s = await _context.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == saleId);

        if (s == null) return null;

        return new SaleDto(
            s.Id,
            s.InvoiceNumber,
            s.Timestamp,
            s.CustomerId,
            s.Customer.Name,
            s.User.FullName,
            s.Subtotal,
            s.DiscountAmount,
            s.TaxAmount,
            s.TotalAmount,
            s.NetProfit,
            s.PaidAmount,
            s.RemainingDebt,
            s.PaymentStatus,
            s.PaymentMethod,
            s.Items.Select(it => new SaleItemDetailDto(
                it.Id,
                it.ProductId,
                it.Product.NameAr,
                it.Product.Barcode,
                it.Quantity,
                it.UnitSalePrice,
                it.TotalLineAmount,
                it.LineProfit
            )).ToList()
        );
    }

    public async Task<SalesReturn> ProcessSaleReturnAsync(long saleId, List<(int productId, decimal quantity)> returnedItems, RefundMethod refundMethod, string? reason, int userId)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null) throw new DomainException("الفاتورة الأصلية غير موجودة.");

            decimal totalRefund = 0;
            var returnItems = new List<SalesReturnItem>();

            foreach (var ret in returnedItems)
            {
                var originalItem = sale.Items.FirstOrDefault(i => i.ProductId == ret.productId);
                if (originalItem == null || ret.quantity > originalItem.Quantity)
                    throw new DomainException($"كمية الإرجاع غير صالحة للمنتج رقم {ret.productId}.");

                decimal lineRefund = ret.quantity * originalItem.UnitSalePrice;
                totalRefund += lineRefund;

                returnItems.Add(new SalesReturnItem
                {
                    ProductId = ret.productId,
                    QuantityReturned = ret.quantity,
                    RefundUnitPrice = originalItem.UnitSalePrice,
                    TotalLineRefund = lineRefund,
                    ReturnToStock = true
                });

                // Return stock to warehouse
                var wp = await _context.WarehouseProducts
                    .FirstOrDefaultAsync(x => x.WarehouseId == sale.WarehouseId && x.ProductId == ret.productId);
                if (wp != null)
                {
                    wp.CurrentQuantity += ret.quantity;
                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = ret.productId,
                        WarehouseId = sale.WarehouseId,
                        MovementType = MovementType.SaleReturn,
                        QuantityChange = ret.quantity,
                        ResultingQuantity = wp.CurrentQuantity,
                        UnitCost = originalItem.UnitCostPrice,
                        ReferenceDocumentType = "SaleReturn",
                        UserId = userId,
                        Notes = $"إرجاع بضاعة من فاتورة {sale.InvoiceNumber}"
                    });
                }
            }

            var salesReturn = new SalesReturn
            {
                ReturnNumber = $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                OriginalSaleId = sale.Id,
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                CustomerId = sale.CustomerId,
                WarehouseId = sale.WarehouseId,
                CashRegisterId = sale.CashRegisterId,
                TotalRefundAmount = totalRefund,
                RefundMethod = refundMethod,
                Reason = reason,
                Items = returnItems
            };

            _context.SalesReturns.Add(salesReturn);

            // Handle refund payment
            if (refundMethod == RefundMethod.Cash)
            {
                var register = await _context.CashRegisters.FindAsync(sale.CashRegisterId);
                if (register != null)
                {
                    register.CurrentBalance -= totalRefund;
                    _context.CashTransactions.Add(new CashTransaction
                    {
                        CashRegisterId = register.Id,
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        TransactionType = CashTransactionType.SaleRefund,
                        Amount = -totalRefund,
                        BalanceAfter = register.CurrentBalance,
                        ReferenceDocumentType = "SaleReturn",
                        Notes = $"استرجاع مبلغ لمرتجع مبيعات"
                    });
                }
            }
            else if (refundMethod == RefundMethod.CustomerCreditDeduction)
            {
                sale.Customer.CurrentDebt = Math.Max(0, sale.Customer.CurrentDebt - totalRefund);
            }

            await _context.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return salesReturn;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
