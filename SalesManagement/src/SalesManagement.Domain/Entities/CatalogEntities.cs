namespace SalesManagement.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameFr { get; set; }
    public string? NameEn { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OriginCountry { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Unit
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameFr { get; set; }
    public bool AllowDecimal { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameFr { get; set; }
    public string? NameEn { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal MinimumStock { get; set; } = 5.0m;
    public decimal TaxPercent { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WarehouseProduct> WarehouseProducts { get; set; } = new List<WarehouseProduct>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public decimal GetTotalStock() => WarehouseProducts.Sum(wp => wp.CurrentQuantity);
    public bool IsLowStock() => GetTotalStock() <= MinimumStock;
}
