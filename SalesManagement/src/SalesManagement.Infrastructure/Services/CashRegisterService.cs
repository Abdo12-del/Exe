using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class CashRegisterService : ICashRegisterService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public CashRegisterService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<CashRegisterStatusDto> GetStatusAsync(int registerId)
    {
        var reg = await _context.CashRegisters.FindAsync(registerId);
        if (reg == null) throw new DomainException("الصندوق غير موجود.");

        return new CashRegisterStatusDto(reg.Id, reg.Name, reg.IsOpen, reg.CurrentBalance, reg.OpeningFloat, reg.OpenedAt);
    }

    public async Task OpenRegisterAsync(int registerId, decimal openingFloat, int userId)
    {
        var reg = await _context.CashRegisters.FindAsync(registerId);
        if (reg == null) throw new DomainException("الصندوق غير موجود.");

        if (reg.IsOpen) throw new DomainException("الصندوق مفتوح بالفعل.");

        reg.IsOpen = true;
        reg.OpeningFloat = openingFloat;
        reg.CurrentBalance = openingFloat;
        reg.OpenedAt = DateTime.UtcNow;
        reg.ClosedAt = null;
        reg.OpenedByUserId = userId;

        _context.CashTransactions.Add(new CashTransaction
        {
            CashRegisterId = reg.Id,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            TransactionType = CashTransactionType.OpeningFloat,
            Amount = openingFloat,
            BalanceAfter = openingFloat,
            Notes = "رصيد بداية اليومية (Fond de Caisse)"
        });

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "OpenRegister", "CashRegister", reg.Id.ToString(), null, new { openingFloat });
    }

    public async Task<ZReportDto> CloseRegisterAsync(int registerId, decimal actualCashCounted, int userId)
    {
        var reg = await _context.CashRegisters.FindAsync(registerId);
        if (reg == null) throw new DomainException("الصندوق غير موجود.");
        if (!reg.IsOpen) throw new DomainException("الصندوق مغلق بالفعل.");

        var openTime = reg.OpenedAt ?? DateTime.UtcNow.Date;

        // Transactions since opening
        var transactions = await _context.CashTransactions
            .Where(ct => ct.CashRegisterId == registerId && ct.Timestamp >= openTime)
            .ToListAsync();

        var sales = await _context.Sales
            .Where(s => s.CashRegisterId == registerId && s.Timestamp >= openTime && !s.IsCancelled)
            .ToListAsync();

        decimal cashSalesTotal = transactions
            .Where(t => t.TransactionType == CashTransactionType.SaleReceipt)
            .Sum(t => t.Amount);

        decimal creditSalesTotal = sales
            .Sum(s => s.RemainingDebt);

        decimal custCollections = transactions
            .Where(t => t.TransactionType == CashTransactionType.CustomerDebtPayment)
            .Sum(t => t.Amount);

        decimal supplierPayments = Math.Abs(transactions
            .Where(t => t.TransactionType == CashTransactionType.SupplierDebtPayment)
            .Sum(t => t.Amount));

        decimal cashExpenses = Math.Abs(transactions
            .Where(t => t.TransactionType == CashTransactionType.ExpensePayment)
            .Sum(t => t.Amount));

        decimal expectedCash = reg.OpeningFloat + cashSalesTotal + custCollections - supplierPayments - cashExpenses;
        decimal diff = actualCashCounted - expectedCash;

        // Close register
        reg.IsOpen = false;
        reg.ClosedAt = DateTime.UtcNow;
        reg.CurrentBalance = 0;

        _context.CashTransactions.Add(new CashTransaction
        {
            CashRegisterId = reg.Id,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            TransactionType = CashTransactionType.ClosingBalance,
            Amount = -actualCashCounted,
            BalanceAfter = 0,
            Notes = $"إغلاق الصندوق Z-Report (فارق: {diff:N2} دج)"
        });

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "CloseRegister", "CashRegister", reg.Id.ToString(), null, new { expectedCash, actualCashCounted, diff });

        return new ZReportDto(
            DateTime.UtcNow,
            reg.Name,
            user?.FullName ?? "Admin",
            reg.OpeningFloat,
            cashSalesTotal,
            creditSalesTotal,
            custCollections,
            supplierPayments,
            cashExpenses,
            expectedCash,
            actualCashCounted,
            diff,
            sales.Count,
            sales.Sum(s => s.TotalAmount),
            sales.Sum(s => s.NetProfit)
        );
    }

    public async Task AddCashTransactionAsync(int registerId, CashTransactionType type, decimal amount, string notes, int userId)
    {
        var reg = await _context.CashRegisters.FindAsync(registerId);
        if (reg == null) throw new DomainException("الصندوق غير موجود.");
        if (!reg.IsOpen) throw new ClosedCashRegisterException(registerId);

        decimal delta = (type == CashTransactionType.CashOutWithdrawal || type == CashTransactionType.ExpensePayment)
            ? -Math.Abs(amount)
            : Math.Abs(amount);

        reg.CurrentBalance += delta;

        _context.CashTransactions.Add(new CashTransaction
        {
            CashRegisterId = registerId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            TransactionType = type,
            Amount = delta,
            BalanceAfter = reg.CurrentBalance,
            Notes = notes
        });

        await _context.SaveChangesAsync();
    }
}
