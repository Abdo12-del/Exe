using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public ExpenseService(AppDbContext context, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<List<ExpenseCategoryDto>> GetCategoriesAsync()
    {
        return await _context.ExpenseCategories
            .AsNoTracking()
            .Select(c => new ExpenseCategoryDto(c.Id, c.NameAr, c.Description))
            .ToListAsync();
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Expenses
            .AsNoTracking()
            .Include(e => e.Category)
            .AsQueryable();

        if (fromDate.HasValue) query = query.Where(e => e.Timestamp >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.Timestamp <= toDate.Value);

        var list = await query.OrderByDescending(e => e.Timestamp).ToListAsync();

        return list.Select(e => new ExpenseDto(
            e.Id,
            e.ExpenseNumber,
            e.Category.NameAr,
            e.Timestamp,
            e.Amount,
            e.Title,
            e.Notes
        )).ToList();
    }

    public async Task<Expense> CreateExpenseAsync(CreateExpenseDto dto)
    {
        if (dto.Amount <= 0)
            throw new DomainException("مبلغ المصروف يجب أن يكون أكبر من الصفر.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var category = await _context.ExpenseCategories.FindAsync(dto.CategoryId);
            if (category == null) throw new DomainException("فئة المصروف المحددة غير موجودة.");

            string expNum = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var expense = new Expense
            {
                ExpenseNumber = expNum,
                CategoryId = dto.CategoryId,
                CashRegisterId = dto.CashRegisterId,
                UserId = dto.UserId,
                Timestamp = DateTime.UtcNow,
                Amount = dto.Amount,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? category.NameAr : dto.Title.Trim(),
                Notes = dto.Notes
            };

            _context.Expenses.Add(expense);

            // Deduct cash from drawer when a cash register is specified
            if (dto.CashRegisterId.HasValue)
            {
                var reg = await _context.CashRegisters.FindAsync(dto.CashRegisterId.Value);
                if (reg != null)
                {
                    reg.CurrentBalance -= dto.Amount;
                    _context.CashTransactions.Add(new CashTransaction
                    {
                        CashRegisterId = reg.Id,
                        UserId = dto.UserId,
                        Timestamp = DateTime.UtcNow,
                        TransactionType = CashTransactionType.ExpensePayment,
                        Amount = -dto.Amount,
                        BalanceAfter = reg.CurrentBalance,
                        ReferenceDocumentType = "Expense",
                        Notes = $"مصروف: {category.NameAr} - {dto.Notes}"
                    });
                }
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(dto.UserId);
            await _auditService.LogAsync(dto.UserId, user?.Username ?? "Unknown", "CreateExpense", "Expense", expense.Id.ToString(), null, new { expNum, dto.Amount, category = category.NameAr });

            await _unitOfWork.CommitAsync();

            return expense;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<ExpenseCategory> CreateCategoryAsync(string name, string? description)
    {
        var category = new ExpenseCategory { NameAr = name.Trim(), Description = description?.Trim() };
        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }
}
