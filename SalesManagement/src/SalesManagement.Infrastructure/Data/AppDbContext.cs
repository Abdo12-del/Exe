using Microsoft.EntityFrameworkCore;
using SalesManagement.Domain.Entities;

namespace SalesManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseProduct> WarehouseProducts => Set<WarehouseProduct>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<PurchasePayment> PurchasePayments => Set<PurchasePayment>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();
    public DbSet<InventoryCountItem> InventoryCountItems => Set<InventoryCountItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Security
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasIndex(r => r.Name).IsUnique();
            entity.HasMany(r => r.Permissions)
                  .WithMany(p => p.Roles)
                  .UsingEntity("role_permissions");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasIndex(p => p.Code).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        // Catalog
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasIndex(c => c.Code).IsUnique();
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("brands");
            entity.HasIndex(b => b.Name).IsUnique();
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("units");
            entity.HasIndex(u => u.Code).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasIndex(p => p.Barcode).IsUnique();
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.PurchasePrice).HasPrecision(18, 4);
            entity.Property(p => p.SalePrice).HasPrecision(18, 4);
            entity.Property(p => p.WholesalePrice).HasPrecision(18, 4);
            entity.Property(p => p.MinimumStock).HasPrecision(18, 4);
            entity.Property(p => p.TaxPercent).HasPrecision(5, 2);
        });

        // Warehouse & Stock
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasIndex(w => w.Code).IsUnique();
        });

        modelBuilder.Entity<WarehouseProduct>(entity =>
        {
            entity.ToTable("warehouse_products");
            entity.HasKey(wp => new { wp.WarehouseId, wp.ProductId });
            entity.Property(wp => wp.CurrentQuantity).HasPrecision(18, 4);
            entity.Property(wp => wp.ReservedQuantity).HasPrecision(18, 4);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.Property(sm => sm.QuantityChange).HasPrecision(18, 4);
            entity.Property(sm => sm.ResultingQuantity).HasPrecision(18, 4);
            entity.Property(sm => sm.UnitCost).HasPrecision(18, 4);
        });

        // Partners
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.CreditLimit).HasPrecision(18, 4);
            entity.Property(c => c.CurrentDebt).HasPrecision(18, 4);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasIndex(s => s.Code).IsUnique();
            entity.Property(s => s.CurrentDebt).HasPrecision(18, 4);
        });

        // Cash Register
        modelBuilder.Entity<CashRegister>(entity =>
        {
            entity.ToTable("cash_registers");
            entity.HasIndex(cr => cr.Code).IsUnique();
            entity.Property(cr => cr.CurrentBalance).HasPrecision(18, 4);
            entity.Property(cr => cr.OpeningFloat).HasPrecision(18, 4);
        });

        modelBuilder.Entity<CashTransaction>(entity =>
        {
            entity.ToTable("cash_transactions");
            entity.Property(ct => ct.Amount).HasPrecision(18, 4);
            entity.Property(ct => ct.BalanceAfter).HasPrecision(18, 4);
        });

        // Sales
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("sales");
            entity.HasIndex(s => s.InvoiceNumber).IsUnique();
            entity.Property(s => s.Subtotal).HasPrecision(18, 4);
            entity.Property(s => s.DiscountAmount).HasPrecision(18, 4);
            entity.Property(s => s.TaxAmount).HasPrecision(18, 4);
            entity.Property(s => s.TotalAmount).HasPrecision(18, 4);
            entity.Property(s => s.TotalCost).HasPrecision(18, 4);
            entity.Property(s => s.NetProfit).HasPrecision(18, 4);
            entity.Property(s => s.PaidAmount).HasPrecision(18, 4);
            entity.Property(s => s.RemainingDebt).HasPrecision(18, 4);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.ToTable("sale_items");
            entity.Property(si => si.Quantity).HasPrecision(18, 4);
            entity.Property(si => si.UnitSalePrice).HasPrecision(18, 4);
            entity.Property(si => si.UnitCostPrice).HasPrecision(18, 4);
            entity.Property(si => si.DiscountAmount).HasPrecision(18, 4);
            entity.Property(si => si.TaxPercent).HasPrecision(5, 2);
            entity.Property(si => si.TotalLineAmount).HasPrecision(18, 4);
            entity.Property(si => si.LineProfit).HasPrecision(18, 4);
        });

        modelBuilder.Entity<SalePayment>(entity =>
        {
            entity.ToTable("sale_payments");
            entity.Property(sp => sp.Amount).HasPrecision(18, 4);
        });

        // Purchases
        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.ToTable("purchases");
            entity.HasIndex(p => p.PurchaseNumber).IsUnique();
            entity.Property(p => p.Subtotal).HasPrecision(18, 4);
            entity.Property(p => p.DiscountAmount).HasPrecision(18, 4);
            entity.Property(p => p.TaxAmount).HasPrecision(18, 4);
            entity.Property(p => p.TotalAmount).HasPrecision(18, 4);
            entity.Property(p => p.PaidAmount).HasPrecision(18, 4);
            entity.Property(p => p.RemainingDebt).HasPrecision(18, 4);
        });

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.ToTable("purchase_items");
            entity.Property(pi => pi.Quantity).HasPrecision(18, 4);
            entity.Property(pi => pi.UnitBuyPrice).HasPrecision(18, 4);
            entity.Property(pi => pi.DiscountAmount).HasPrecision(18, 4);
            entity.Property(pi => pi.TaxPercent).HasPrecision(5, 2);
            entity.Property(pi => pi.TotalLineAmount).HasPrecision(18, 4);
        });

        // Customer / Supplier Payments
        modelBuilder.Entity<CustomerPayment>(entity =>
        {
            entity.ToTable("customer_payments");
            entity.Property(cp => cp.Amount).HasPrecision(18, 4);
            entity.Property(cp => cp.PreviousDebt).HasPrecision(18, 4);
            entity.Property(cp => cp.RemainingDebt).HasPrecision(18, 4);
        });

        modelBuilder.Entity<SupplierPayment>(entity =>
        {
            entity.ToTable("supplier_payments");
            entity.Property(sp => sp.Amount).HasPrecision(18, 4);
            entity.Property(sp => sp.PreviousDebt).HasPrecision(18, 4);
            entity.Property(sp => sp.RemainingDebt).HasPrecision(18, 4);
        });

        // Expenses
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("expenses");
            entity.Property(e => e.Amount).HasPrecision(18, 4);
        });

        // Settings
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.ToTable("settings");
            entity.HasKey(s => s.SettingKey);
        });
    }
}
