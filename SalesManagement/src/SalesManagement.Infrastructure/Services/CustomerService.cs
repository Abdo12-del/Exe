using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public CustomerService(AppDbContext context, IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<List<CustomerDto>> GetAllAsync(string? search = null, bool withDebtOnly = false)
    {
        var query = _context.Customers.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c => c.Name.Contains(s) || (c.Phone != null && c.Phone.Contains(s)) || (c.CompanyName != null && c.CompanyName.Contains(s)));
        }

        if (withDebtOnly)
        {
            query = query.Where(c => c.CurrentDebt > 0);
        }

        var list = await query.OrderBy(c => c.Name).ToListAsync();

        return list.Select(c => new CustomerDto(
            c.Id, c.Code, c.Name, c.CompanyName, c.Phone, c.Address, c.TaxNumber,
            c.CreditLimit, c.CurrentDebt, c.Notes, c.IsActive
        )).ToList();
    }

    public async Task<CustomerDto> CreateAsync(CustomerDto dto, int userId)
    {
        string code = string.IsNullOrWhiteSpace(dto.Code) 
            ? $"CUST-{Guid.NewGuid().ToString("N")[..6].ToUpper()}" 
            : dto.Code;

        var customer = new Customer
        {
            Code = code,
            Name = dto.Name.Trim(),
            CompanyName = dto.CompanyName?.Trim(),
            Phone = dto.Phone?.Trim(),
            Address = dto.Address?.Trim(),
            TaxNumber = dto.TaxNumber?.Trim(),
            CreditLimit = dto.CreditLimit > 0 ? dto.CreditLimit : 50000,
            CurrentDebt = 0,
            Notes = dto.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "CreateCustomer", "Customer", customer.Id.ToString(), null, customer);

        return new CustomerDto(customer.Id, customer.Code, customer.Name, customer.CompanyName, customer.Phone, customer.Address, customer.TaxNumber, customer.CreditLimit, customer.CurrentDebt, customer.Notes, customer.IsActive);
    }

    public async Task<CustomerDto> UpdateAsync(int id, CustomerDto dto, int userId)
    {
        var c = await _context.Customers.FindAsync(id);
        if (c == null) throw new DomainException("الزبون غير موجود.");

        var old = new { c.Name, c.Phone, c.CreditLimit };

        c.Name = dto.Name.Trim();
        c.CompanyName = dto.CompanyName?.Trim();
        c.Phone = dto.Phone?.Trim();
        c.Address = dto.Address?.Trim();
        c.TaxNumber = dto.TaxNumber?.Trim();
        c.CreditLimit = dto.CreditLimit;
        c.Notes = dto.Notes;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "UpdateCustomer", "Customer", c.Id.ToString(), old, c);

        return new CustomerDto(c.Id, c.Code, c.Name, c.CompanyName, c.Phone, c.Address, c.TaxNumber, c.CreditLimit, c.CurrentDebt, c.Notes, c.IsActive);
    }

    public async Task<CustomerPayment> RecordDebtPaymentAsync(int customerId, decimal amount, PaymentMethod method, int? cashRegisterId, int userId, string? note)
    {
        if (amount <= 0) throw new DomainException("مبلغ التسديد يجب أن يكون أكبر من الصفر.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) throw new DomainException("الزبون غير موجود.");

            decimal previousDebt = customer.CurrentDebt;
            customer.CurrentDebt = Math.Max(0, customer.CurrentDebt - amount);

            string paymentNum = $"REG-CUST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var payment = new CustomerPayment
            {
                PaymentNumber = paymentNum,
                CustomerId = customer.Id,
                CashRegisterId = cashRegisterId,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Amount = amount,
                PreviousDebt = previousDebt,
                RemainingDebt = customer.CurrentDebt,
                PaymentMethod = method,
                Notes = note
            };

            _context.CustomerPayments.Add(payment);

            // If cash and register provided, add cash to drawer
            if (cashRegisterId.HasValue && method == PaymentMethod.Cash)
            {
                var register = await _context.CashRegisters.FindAsync(cashRegisterId.Value);
                if (register != null)
                {
                    register.CurrentBalance += amount;
                    _context.CashTransactions.Add(new CashTransaction
                    {
                        CashRegisterId = register.Id,
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        TransactionType = CashTransactionType.CustomerDebtPayment,
                        Amount = amount,
                        BalanceAfter = register.CurrentBalance,
                        ReferenceDocumentType = "CustomerPayment",
                        Notes = $"تسديد دين الزبون: {customer.Name}"
                    });
                }
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            await _auditService.LogAsync(userId, user?.Username ?? "Unknown", "CustomerDebtPayment", "CustomerPayment", payment.Id.ToString(), null, new { customerId, amount, remainingDebt = customer.CurrentDebt });

            await _unitOfWork.CommitAsync();

            return payment;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PartnerStatementItemDto>> GetCustomerStatementAsync(int customerId)
    {
        var sales = await _context.Sales
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId && !s.IsCancelled)
            .OrderBy(s => s.Timestamp)
            .ToListAsync();

        var payments = await _context.CustomerPayments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderBy(p => p.Timestamp)
            .ToListAsync();

        var items = new List<PartnerStatementItemDto>();

        foreach (var s in sales)
        {
            if (s.RemainingDebt > 0 || s.TotalAmount > 0)
            {
                items.Add(new PartnerStatementItemDto(
                    s.Timestamp,
                    "فاتورة مبيعات",
                    s.InvoiceNumber,
                    $"فاتورة بيع بإجمالي {s.TotalAmount:N2} دج (مسدد: {s.PaidAmount:N2} دج)",
                    s.TotalAmount,
                    s.PaidAmount,
                    0
                ));
            }
        }

        foreach (var p in payments)
        {
            items.Add(new PartnerStatementItemDto(
                p.Timestamp,
                "تسديد دين",
                p.PaymentNumber,
                $"دفعة نقدية مسددة: {p.Notes ?? "تسديد على الحساب"}",
                0,
                p.Amount,
                p.RemainingDebt
            ));
        }

        return items.OrderBy(x => x.Date).ToList();
    }
}
