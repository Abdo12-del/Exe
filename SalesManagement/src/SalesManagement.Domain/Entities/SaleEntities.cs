using SalesManagement.Domain.Enums;

namespace SalesManagement.Domain.Entities;

public class Sale
{
    public long Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal NetProfit { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingDebt { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
}

public class SaleItem
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitSalePrice { get; set; }
    public decimal UnitCostPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TotalLineAmount { get; set; }
    public decimal LineProfit { get; set; }
}

public class SalePayment
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class SalesReturn
{
    public long Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public long OriginalSaleId { get; set; }
    public Sale OriginalSale { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public decimal TotalRefundAmount { get; set; }
    public RefundMethod RefundMethod { get; set; } = RefundMethod.Cash;
    public string? Reason { get; set; }

    public ICollection<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
}

public class SalesReturnItem
{
    public long Id { get; set; }
    public long SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal QuantityReturned { get; set; }
    public decimal RefundUnitPrice { get; set; }
    public decimal TotalLineRefund { get; set; }
    public bool ReturnToStock { get; set; } = true;
}
