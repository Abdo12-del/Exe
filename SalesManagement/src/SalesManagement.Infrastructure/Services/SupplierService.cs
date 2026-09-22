using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public SupplierService(AppDbContext context, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<List<SupplierDto>> GetAllAsync(string? search = null, bool withDebtOnly = false)
    {
        var query = _context.Suppliers.AsNoTracking().Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(s => s.Name.Contains(q) || (s.CompanyName != null && s.CompanyName.Contains(q)) || (s.Phone != null && s.Phone.Contains(q)));
        }

        if (withDebtOnly)
        {
            query = query.Where(s => s.CurrentDebt > 0);
        }

        var list = await query.OrderBy(s => s.Name).ToListAsync();

        return list.Select(s => new SupplierDto(
            s.Id, s.Code, s.Name, s.CompanyName, s.Phone, s.Address, s.TaxNumber,
            s.CurrentDebt, s.Notes, s.IsActive
        )).ToList();
    }

    public async Task<SupplierDto> CreateAsync(SupplierDto dto, int userId)
    {
        string code = string.IsNullOrWhiteSpace(dto.Code) 
            ? $"SUPP-{Guid.NewGuid().ToString("N")[..6].ToUpper()}" 
            : dto.Code;

        var supplier = new Supplier
        {
            Code = code,
            Name = dto.Name.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Phone = dto.Phone?.Trim(),
            Address = dto.Address?.Trim(),
            TaxNumber = dto.TaxNumber?.Trim(),
            CurrentDebt = dto.CurrentDebt,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "CreateSupplier", "Supplier", supplier.Id.ToString(), null, supplier);

        return new SupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.CompanyName, supplier.Phone, supplier.Address, supplier.TaxNumber, supplier.CurrentDebt, supplier.Notes, supplier.IsActive);
    }

    public async Task<SupplierDto> UpdateAsync(int id, SupplierDto dto, int userId)
    {
        var s = await _context.Suppliers.FindAsync(id);
        if (s == null) throw new DomainException("الممون غير موجود.");

        var old = new { s.Name, s.Phone, s.CurrentDebt };

        s.Name = dto.Name.Trim();
        s.CompanyName = dto.CompanyName?.Trim();
        s.Phone = dto.Phone?.Trim();
        s.Address = dto.Address?.Trim();
        s.TaxNumber = dto.TaxNumber?.Trim();
        s.CurrentDebt = dto.CurrentDebt;
        s.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "UpdateSupplier", "Supplier", s.Id.ToString(), old, s);

        return new SupplierDto(s.Id, s.Code, s.Name, s.CompanyName, s.Phone, s.Address, s.TaxNumber, s.CurrentDebt, s.Notes, s.IsActive);
    }

    public async Task<SupplierPayment> RecordDebtPaymentAsync(int supplierId, decimal amount, PaymentMethod method, int? cashRegisterId, int userId, string? note)
    {
        if (amount <= 0) throw new DomainException("مبلغ التسديد يجب أن يكون أكبر من الصفر.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            if (supplier == null) throw new DomainException("الممون غير موجود.");

            decimal previousDebt = supplier.CurrentDebt;
            supplier.CurrentDebt = Math.Max(0, supplier.CurrentDebt - amount);

            string paymentNum = $"REG-SUPP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var payment = new SupplierPayment
            {
                PaymentNumber = paymentNum,
                SupplierId = supplier.Id,
                CashRegisterId = cashRegisterId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Amount = amount,
                PreviousDebt = previousDebt,
                RemainingDebt = supplier.CurrentDebt,
                PaymentMethod = method,
                Notes = note
            };

            _context.SupplierPayments.Add(payment);

            // Deduct cash from drawer if cash payment
            if (cashRegisterId.HasValue && method == PaymentMethod.Cash)
            {
                var register = await _context.CashRegisters.FindAsync(cashRegisterId.Value);
                if (register != null)
                {
                    register.CurrentBalance -= amount;
                    _context.CashTransactions.Add(new CashTransaction
                    {
                        CashRegisterId = register.Id,
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        TransactionType = CashTransactionType.SupplierDebtPayment,
                        Amount = -amount,
                        BalanceAfter = register.CurrentBalance,
                        ReferenceDocumentType = "SupplierPayment",
                        Notes = $"تسديد مستحقات للممون: {supplier.Name}"
                    });
                }
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "SupplierDebtPayment", "SupplierPayment", payment.Id.ToString(), null, new { supplierId, amount, remainingDebt = supplier.CurrentDebt });

            await _unitOfWork.CommitAsync();

            return payment;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PartnerStatementItemDto>> GetSupplierStatementAsync(int supplierId)
    {
        var purchases = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();

        var payments = await _context.SupplierPayments
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();

        var items = new List<PartnerStatementItemDto>();

        foreach (var p in purchases)
        {
            items.Add(new PartnerStatementItemDto(
                p.Timestamp,
                "فاتورة شراء",
                p.PurchaseNumber,
                $"فاتورة توريد بضاعة بقيمة {p.TotalAmount:N2} دج (مسدد: {p.PaidAmount:N2} دج)",
                p.TotalAmount,
                p.PaidAmount,
                0
            ));
        }

        foreach (var pm in payments)
        {
            items.Add(new PartnerStatementItemDto(
                pm.Timestamp,
                "تسديد مستحقات",
                pm.PaymentNumber,
                $"دفعة مسددة للمورد: {pm.Notes ?? "تسديد نقدي"}",
                0,
                pm.Amount,
                pm.RemainingDebt
            ));
        }

        return items.OrderBy(x => x.Date).ToList();
    }
}
