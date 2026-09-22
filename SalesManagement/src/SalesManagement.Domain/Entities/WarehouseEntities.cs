using SalesManagement.Domain.Enums;

namespace SalesManagement.Domain.Entities;

public class Warehouse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ManagerName { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WarehouseProduct> WarehouseProducts { get; set; } = new List<WarehouseProduct>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

public class WarehouseProduct
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal CurrentQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }

    public decimal AvailableQuantity => CurrentQuantity - ReservedQuantity;
}

public class StockMovement
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public MovementType MovementType { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal ResultingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceDocumentType { get; set; }
    public long? ReferenceDocumentId { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
