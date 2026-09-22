using SalesManagement.Domain.Enums;

namespace SalesManagement.Domain.Entities;

public class CashRegister
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AssignedWarehouseId { get; set; }
    public Warehouse AssignedWarehouse { get; set; } = null!;
    public decimal CurrentBalance { get; set; }
    public bool IsOpen { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? OpenedByUserId { get; set; }
    public User? OpenedByUser { get; set; }
    public decimal OpeningFloat { get; set; }

    public ICollection<CashTransaction> Transactions { get; set; } = new List<CashTransaction>();
}

public class CashTransaction
{
    public long Id { get; set; }
    public int CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public CashTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceDocumentType { get; set; }
    public long? ReferenceDocumentId { get; set; }
    public string? Notes { get; set; }
}

public class ExpenseCategory
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string? NameFr { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

public class Expense
{
    public long Id { get; set; }
    public string ExpenseNumber { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public int? CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class InventoryCount
{
    public long Id { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }
    public InventoryCountStatus Status { get; set; } = InventoryCountStatus.Draft;
    public string? Notes { get; set; }

    public ICollection<InventoryCountItem> Items { get; set; } = new List<InventoryCountItem>();
}

public class InventoryCountItem
{
    public long Id { get; set; }
    public long InventoryCountId { get; set; }
    public InventoryCount InventoryCount { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal SystemQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal Difference { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DifferenceValue { get; set; }
    public string? Notes { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ComputerName { get; set; }
    public string? IpAddress { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public class Setting
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
