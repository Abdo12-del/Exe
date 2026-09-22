using SalesManagement.Application.DTOs;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;

namespace SalesManagement.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    Task<int> SaveChangesAsync();
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string username, string password);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(string? searchQuery = null, int? categoryId = null, bool lowStockOnly = false);
    Task<ProductDto?> GetByBarcodeAsync(string barcode);
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(CreateProductDto dto, int userId);
    Task<ProductDto> UpdateAsync(int id, CreateProductDto dto, int userId);
    Task<bool> DeleteAsync(int id, int userId);
}

public interface ISaleService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleDto dto);
    Task<List<SaleDto>> GetSalesHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null, int? customerId = null, string? search = null);
    Task<SaleDto?> GetSaleByIdAsync(long saleId);
    Task<SalesReturn> ProcessSaleReturnAsync(long saleId, List<(int productId, decimal quantity)> returnedItems, RefundMethod refundMethod, string? reason, int userId);
}

public interface IPurchaseService
{
    Task<Purchase> CreatePurchaseAsync(CreatePurchaseDto dto);
    Task<List<Purchase>> GetPurchasesHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null, int? supplierId = null);
}

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(string? search = null, bool withDebtOnly = false);
    Task<CustomerDto> CreateAsync(CustomerDto dto, int userId);
    Task<CustomerDto> UpdateAsync(int id, CustomerDto dto, int userId);
    Task<CustomerPayment> RecordDebtPaymentAsync(int customerId, decimal amount, PaymentMethod method, int? cashRegisterId, int userId, string? note);
    Task<List<PartnerStatementItemDto>> GetCustomerStatementAsync(int customerId);
}

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync(string? search = null, bool withDebtOnly = false);
    Task<SupplierDto> CreateAsync(SupplierDto dto, int userId);
    Task<SupplierDto> UpdateAsync(int id, SupplierDto dto, int userId);
    Task<SupplierPayment> RecordDebtPaymentAsync(int supplierId, decimal amount, PaymentMethod method, int? cashRegisterId, int userId, string? note);
    Task<List<PartnerStatementItemDto>> GetSupplierStatementAsync(int supplierId);
}

public interface ICashRegisterService
{
    Task<CashRegisterStatusDto> GetStatusAsync(int registerId);
    Task OpenRegisterAsync(int registerId, decimal openingFloat, int userId);
    Task<ZReportDto> CloseRegisterAsync(int registerId, decimal actualCashCounted, int userId);
    Task AddCashTransactionAsync(int registerId, CashTransactionType type, decimal amount, string notes, int userId);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
}

public interface IBackupService
{
    Task<string> CreateBackupAsync(string targetDirectory);
    Task<bool> RestoreBackupAsync(string backupFilePath);
}

public interface IExpenseService
{
    Task<List<ExpenseCategoryDto>> GetCategoriesAsync();
    Task<List<ExpenseDto>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<Expense> CreateExpenseAsync(CreateExpenseDto dto);
    Task<ExpenseCategory> CreateCategoryAsync(string name, string? description);
}

public interface IInventoryCountService
{
    Task<List<InventoryCountItemDto>> PrepareInventorySheetAsync(int warehouseId);
    Task<InventoryCount> PostInventoryAdjustmentAsync(PostInventoryCountDto dto);
}

public interface IAuditService
{
    Task LogAsync(int? userId, string username, string action, string entityName, string? entityId, object? oldValues, object? newValues);
}
