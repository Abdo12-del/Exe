using SalesManagement.Domain.Enums;

namespace SalesManagement.Domain.Entities;

public class Purchase
{
    public long Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public string? SupplierInvoiceNumber { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingDebt { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    public string? Notes { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    public ICollection<PurchasePayment> Payments { get; set; } = new List<PurchasePayment>();
}

public class PurchaseItem
{
    public long Id { get; set; }
    public long PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitBuyPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TotalLineAmount { get; set; }
}

public class PurchasePayment
{
    public long Id { get; set; }
    public long PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public int? CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseReturn
{
    public long Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public long OriginalPurchaseId { get; set; }
    public Purchase OriginalPurchase { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public decimal TotalRefundAmount { get; set; }
    public RefundMethod RefundMethod { get; set; } = RefundMethod.SupplierDebtDeduction;
    public string? Reason { get; set; }

    public ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}

public class PurchaseReturnItem
{
    public long Id { get; set; }
    public long PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal QuantityReturned { get; set; }
    public decimal UnitBuyPrice { get; set; }
    public decimal TotalLineAmount { get; set; }
}
