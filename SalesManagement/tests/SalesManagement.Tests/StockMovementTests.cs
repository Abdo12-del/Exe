using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Infrastructure.Data;
using SalesManagement.Infrastructure.Services;
using Xunit;

namespace SalesManagement.Tests;

public class StockMovementTests
{
    private (AppDbContext Context, IUnitOfWork UnitOfWork, Mock<IAuditService> AuditMock) CreateInMemorySetup(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AppDbContext(options);
        var uow = new UnitOfWork(context);
        var auditMock = new Mock<IAuditService>();

        return (context, uow, auditMock);
    }

    [Fact]
    public async Task CreatePurchase_ShouldIncrementStock_AndUpdateSupplierDebt()
    {
        // Arrange
        var (context, uow, auditMock) = CreateInMemorySetup(nameof(CreatePurchase_ShouldIncrementStock_AndUpdateSupplierDebt));

        var supplier = new Supplier { Id = 1, Code = "SUP-01", Name = "شركة التوزيع الجزائرية", CurrentDebt = 10000, IsActive = true };
        var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "مستودع البضائع", IsActive = true };
        var product = new Product { Id = 1, Code = "PRD-01", NameAr = "طماطم معلبة 800غ", PurchasePrice = 140, SalePrice = 170, IsActive = true };
        var wp = new WarehouseProduct { WarehouseId = 1, ProductId = 1, CurrentQuantity = 10 };

        context.Suppliers.Add(supplier);
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.WarehouseProducts.Add(wp);
        await context.SaveChangesAsync();

        var purchaseService = new PurchaseService(context, uow, auditMock.Object);

        // Buy 50 items @ 150 DZD (total = 7500 DZD), pay 2500 DZD cash, 5000 DZD credit
        var dto = new CreatePurchaseDto(
            SupplierId: 1,
            WarehouseId: 1,
            CashRegisterId: null,
            UserId: 1,
            SupplierInvoiceNumber: "FAC-2026-99",
            PaymentMethod: PaymentMethod.Credit,
            DiscountAmount: 0,
            PaidAmount: 2500,
            Notes: "شحنة تجريبية",
            Items: new List<CreatePurchaseItemDto>
            {
                new CreatePurchaseItemDto(ProductId: 1, Quantity: 50, UnitBuyPrice: 150, DiscountAmount: 0)
            }
        );

        // Act
        var purchase = await purchaseService.CreatePurchaseAsync(dto);

        // Assert
        purchase.Should().NotBeNull();
        purchase.TotalAmount.Should().Be(7500);

        // Stock must have increased by 50
        var updatedWp = await context.WarehouseProducts.FirstAsync(x => x.WarehouseId == 1 && x.ProductId == 1);
        updatedWp.CurrentQuantity.Should().Be(60); // 10 + 50

        // Product purchase price updated
        var updatedProd = await context.Products.FindAsync(1);
        updatedProd!.PurchasePrice.Should().Be(150);

        // Supplier debt increased by 7500 (since Credit method)
        var updatedSupplier = await context.Suppliers.FindAsync(1);
        updatedSupplier!.CurrentDebt.Should().Be(17500); // 10000 + 7500
    }

    [Fact]
    public async Task ProcessSaleReturn_ShouldReturnStock_AndRefundCash()
    {
        // Arrange
        var (context, uow, auditMock) = CreateInMemorySetup(nameof(ProcessSaleReturn_ShouldReturnStock_AndRefundCash));

        var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "المستودع الرئيسي", IsActive = true };
        var register = new CashRegister { Id = 1, Name = "Caisse 01", IsOpen = true, CurrentBalance = 5000 };
        var product = new Product { Id = 1, Code = "PRD-01", NameAr = "علبة شاي 250غ", PurchasePrice = 180, SalePrice = 220, IsActive = true };
        var wp = new WarehouseProduct { WarehouseId = 1, ProductId = 1, CurrentQuantity = 10 };

        var sale = new Sale
        {
            Id = 100,
            InvoiceNumber = "INV-2026-001",
            WarehouseId = 1,
            CashRegisterId = 1,
            UserId = 1,
            TotalAmount = 440,
            PaidAmount = 440,
            RemainingDebt = 0,
            Items = new List<SaleItem>
            {
                new SaleItem { ProductId = 1, Quantity = 2, UnitPrice = 220, CostPrice = 180, TotalLineAmount = 440 }
            }
        };

        context.Warehouses.Add(warehouse);
        context.CashRegisters.Add(register);
        context.Products.Add(product);
        context.WarehouseProducts.Add(wp);
        context.Sales.Add(sale);
        await context.SaveChangesAsync();

        var saleService = new SaleService(context, uow, auditMock.Object);

        var returnedItems = new List<(int productId, decimal quantity)>
        {
            (1, 1)
        };

        // Act
        var returnObj = await saleService.ProcessSaleReturnAsync(100, returnedItems, RefundMethod.Cash, "إرجاع علبة شاي", 1);

        // Assert
        returnObj.Should().NotBeNull();
        returnObj.TotalRefundAmount.Should().Be(220);

        // Stock restored (+1)
        var updatedWp = await context.WarehouseProducts.FirstAsync(x => x.WarehouseId == 1 && x.ProductId == 1);
        updatedWp.CurrentQuantity.Should().Be(11); // 10 + 1

        // Cash register refunded (-220)
        var updatedRegister = await context.CashRegisters.FindAsync(1);
        updatedRegister!.CurrentBalance.Should().Be(4780); // 5000 - 220
    }
}
