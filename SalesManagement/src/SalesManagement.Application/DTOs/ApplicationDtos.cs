using SalesManagement.Domain.Enums;

namespace SalesManagement.Application.DTOs;

// --- Auth DTOs ---
public record LoginRequest(string Username, string Password);
public record LoginResponse(bool Success, string Message, UserDto? User = null);
public record UserDto(int Id, string Username, string FullName, string RoleName, string RoleDisplayNameAr, List<string> Permissions);

// --- Catalog DTOs ---
public record ProductDto(
    int Id,
    string Sku,
    string Barcode,
    string NameAr,
    string? NameFr,
    int CategoryId,
    string CategoryName,
    int? BrandId,
    string? BrandName,
    int UnitId,
    string UnitName,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal WholesalePrice,
    decimal MinimumStock,
    decimal TaxPercent,
    decimal CurrentStock,
    bool IsActive
);

public record CreateProductDto(
    string Sku,
    string Barcode,
    string NameAr,
    string? NameFr,
    int CategoryId,
    int? BrandId,
    int UnitId,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal WholesalePrice,
    decimal MinimumStock,
    decimal TaxPercent,
    string? Description
);

public record CategoryDto(int Id, string Code, string NameAr, string? NameFr, int? ParentId);
public record BrandDto(int Id, string Name, string? OriginCountry);
public record UnitDto(int Id, string Code, string NameAr, bool AllowDecimal);

// --- Partner DTOs ---
public record CustomerDto(
    int Id,
    string Code,
    string Name,
    string? CompanyName,
    string? Phone,
    string? Address,
    string? TaxNumber,
    decimal CreditLimit,
    decimal CurrentDebt,
    string? Notes,
    bool IsActive
);

public record SupplierDto(
    int Id,
    string Code,
    string Name,
    string? CompanyName,
    string? Phone,
    string? Address,
    string? TaxNumber,
    decimal CurrentDebt,
    string? Notes,
    bool IsActive
);

public record PartnerStatementItemDto(
    DateTime Date,
    string DocumentType,
    string ReferenceNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal BalanceAfter
);

// --- Sale & POS DTOs ---
public record CreateSaleItemDto(int ProductId, int UnitId, decimal Quantity, decimal UnitSalePrice, decimal DiscountAmount);
public record CreateSaleDto(
    int CustomerId,
    int WarehouseId,
    int CashRegisterId,
    int UserId,
    decimal DiscountAmount,
    decimal PaidAmount,
    PaymentMethod PaymentMethod,
    string? Notes,
    List<CreateSaleItemDto> Items
);

public record SaleDto(
    long Id,
    string InvoiceNumber,
    DateTime Timestamp,
    int CustomerId,
    string CustomerName,
    string CashierName,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal NetProfit,
    decimal PaidAmount,
    decimal RemainingDebt,
    PaymentStatus PaymentStatus,
    PaymentMethod PaymentMethod,
    List<SaleItemDetailDto> Items
);

public record SaleItemDetailDto(
    long Id,
    int ProductId,
    string ProductName,
    string Barcode,
    decimal Quantity,
    decimal UnitSalePrice,
    decimal TotalLineAmount,
    decimal LineProfit
);

// --- Purchase DTOs ---
public record CreatePurchaseItemDto(int ProductId, decimal Quantity, decimal UnitBuyPrice, decimal DiscountAmount);
public record CreatePurchaseDto(
    int SupplierId,
    string? SupplierInvoiceNumber,
    int WarehouseId,
    int? CashRegisterId,
    int UserId,
    decimal DiscountAmount,
    decimal PaidAmount,
    PaymentMethod PaymentMethod,
    string? Notes,
    List<CreatePurchaseItemDto> Items
);

// --- Cash Register & Z-Report DTOs ---
public record CashRegisterStatusDto(
    int Id,
    string Name,
    bool IsOpen,
    decimal CurrentBalance,
    decimal OpeningFloat,
    DateTime? OpenedAt
);

// --- Expense DTOs ---
public record ExpenseCategoryDto(int Id, string Name, string? Description);
public record ExpenseDto(int Id, string ExpenseNumber, string CategoryName, DateTime Date, decimal Amount, string PaymentMethod, string? Beneficiary, string? Notes);
public record CreateExpenseDto(int CategoryId, decimal Amount, PaymentMethod PaymentMethod, int? CashRegisterId, int UserId, string? Beneficiary, string? Notes);

// --- Inventory Count DTOs ---
public record InventoryCountItemDto(int ProductId, string ProductName, string Barcode, decimal ExpectedQuantity, decimal CountedQuantity, decimal Difference);
public record PostInventoryCountDto(int WarehouseId, int UserId, string Notes, List<InventoryCountItemDto> Items);

public record ZReportDto(
    DateTime Date,
    string CashRegisterName,
    string CashierName,
    decimal OpeningFloat,
    decimal CashSalesTotal,
    decimal CreditSalesTotal,
    decimal CustomerDebtCollections,
    decimal SupplierCashPayments,
    decimal CashExpensesTotal,
    decimal ExpectedCashInDrawer,
    decimal ActualCashInDrawer,
    decimal CashDifference,
    int InvoicesCount,
    decimal TotalGrossSales,
    decimal TotalNetProfit
);

// --- Dashboard & Analytics DTOs ---
public record DashboardSummaryDto(
    decimal TodaySales,
    int TodayInvoicesCount,
    decimal TodayNetProfit,
    decimal MonthSales,
    decimal TotalInventoryCostValue,
    decimal TotalInventoryRetailValue,
    decimal TotalCustomerDebts,
    decimal TotalSupplierDebts,
    int LowStockProductsCount,
    List<TopProductDto> TopSellingProducts,
    List<DailyTrendDto> Last7DaysTrend
);

public record TopProductDto(int ProductId, string ProductName, decimal TotalQuantitySold, decimal TotalRevenue);
public record DailyTrendDto(string DateLabel, decimal SalesAmount, decimal ProfitAmount);
