using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;
using SalesManagement.Infrastructure.Data;
using SalesManagement.Infrastructure.Services;
using Xunit;

namespace SalesManagement.Tests;

public class SaleTransactionTests
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
    public async Task CreateSale_ShouldDeductStock_AndAddCashToRegister_WhenCashPayment()
    {
        // Arrange
        var (context, uow, auditMock) = CreateInMemorySetup(nameof(CreateSale_ShouldDeductStock_AndAddCashToRegister_WhenCashPayment));

        var warehouse = new Warehouse { Id = 1, Code = "WH-MAIN", Name = "المستودع الرئيسي", IsActive = true };
        var register = new CashRegister { Id = 1, Name = "Caisse 01", IsOpen = true, CurrentBalance = 10000, OpeningFloat = 10000 };
        var product = new Product
        {
            Id = 1,
            Code = "PRD-01",
            Barcode = "61300001",
            NameAr = "حليب كونديلا 1 لتر",
            PurchasePrice = 110,
            SalePrice = 135,
            MinStockAlert = 5,
            IsActive = true
        };
        var warehouseProduct = new WarehouseProduct
        {
            WarehouseId = 1,
            ProductId = 1,
            CurrentQuantity = 50,
            ReservedQuantity = 0
        };

        context.Warehouses.Add(warehouse);
        context.CashRegisters.Add(register);
        context.Products.Add(product);
        context.WarehouseProducts.Add(warehouseProduct);
        await context.SaveChangesAsync();

        var saleService = new SaleService(context, uow, auditMock.Object);

        var dto = new CreateSaleDto(
            CustomerId: 1,
            WarehouseId: 1,
            CashRegisterId: 1,
            UserId: 1,
            DiscountAmount: 0,
            PaidAmount: 270, // 2 x 135
            PaymentMethod: PaymentMethod.Cash,
            Notes: "بيع نقدي تجريبي",
            Items: new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto(ProductId: 1, UnitId: 1, Quantity: 2, UnitSalePrice: 135, DiscountAmount: 0)
            }
        );

        // Act
        var result = await saleService.CreateSaleAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(270);
        result.NetProfit.Should().Be((135 - 110) * 2); // 50 DZD net profit

        // Verify stock deducted
        var updatedWp = await context.WarehouseProducts.FirstAsync(wp => wp.WarehouseId == 1 && wp.ProductId == 1);
        updatedWp.CurrentQuantity.Should().Be(48); // 50 - 2

        // Verify Stock Movement recorded
        var movement = await context.StockMovements.FirstOrDefaultAsync(m => m.ReferenceDocumentId == result.Id);
        movement.Should().NotBeNull();
        movement!.MovementType.Should().Be(MovementType.Sale);
        movement.QuantityChange.Should().Be(-2);

        // Verify Cash Register incremented
        var updatedRegister = await context.CashRegisters.FindAsync(1);
        updatedRegister!.CurrentBalance.Should().Be(10270); // 10000 + 270
    }

    [Fact]
    public async Task CreateSale_ShouldThrowInsufficientStockException_WhenRequestedExceedsAvailable()
    {
        // Arrange
        var (context, uow, auditMock) = CreateInMemorySetup(nameof(CreateSale_ShouldThrowInsufficientStockException_WhenRequestedExceedsAvailable));

        var warehouse = new Warehouse { Id = 1, Code = "WH-MAIN", Name = "المستودع الرئيسي", IsActive = true };
        var register = new CashRegister { Id = 1, Name = "Caisse 01", IsOpen = true, CurrentBalance = 10000 };
        var product = new Product { Id = 2, Code = "PRD-02", Barcode = "61300002", NameAr = "سكر 1 كغ", PurchasePrice = 80, SalePrice = 95, IsActive = true };
        var warehouseProduct = new WarehouseProduct { WarehouseId = 1, ProductId = 2, CurrentQuantity = 3, ReservedQuantity = 0 };

        context.Warehouses.Add(warehouse);
        context.CashRegisters.Add(register);
        context.Products.Add(product);
        context.WarehouseProducts.Add(warehouseProduct);
        await context.SaveChangesAsync();

        var saleService = new SaleService(context, uow, auditMock.Object);

        var dto = new CreateSaleDto(
            CustomerId: 1,
            WarehouseId: 1,
            CashRegisterId: 1,
            UserId: 1,
            DiscountAmount: 0,
            PaidAmount: 475,
            PaymentMethod: PaymentMethod.Cash,
            Notes: null,
            Items: new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto(ProductId: 2, UnitId: 1, Quantity: 5, UnitSalePrice: 95, DiscountAmount: 0) // Wants 5, only 3 available
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() => saleService.CreateSaleAsync(dto));
    }

    [Fact]
    public async Task CreateSale_ShouldUpdateCustomerDebt_WhenCreditSale()
    {
        // Arrange
        var (context, uow, auditMock) = CreateInMemorySetup(nameof(CreateSale_ShouldUpdateCustomerDebt_WhenCreditSale));

        var customer = new Customer { Id = 10, Code = "C-10", Name = "محمد بوعلام", CreditLimit = 20000, CurrentDebt = 5000, IsActive = true };
        var warehouse = new Warehouse { Id = 1, Code = "WH-MAIN", Name = "المستودع الرئيسي", IsActive = true };
        var register = new CashRegister { Id = 1, Name = "Caisse 01", IsOpen = true, CurrentBalance = 10000 };
        var product = new Product { Id = 3, Code = "PRD-03", Barcode = "61300003", NameAr = "زيت إيليو 5 لتر", PurchasePrice = 600, SalePrice = 650, IsActive = true };
        var warehouseProduct = new WarehouseProduct { WarehouseId = 1, ProductId = 3, CurrentQuantity = 20, ReservedQuantity = 0 };

        context.Customers.Add(customer);
        context.Warehouses.Add(warehouse);
        context.CashRegisters.Add(register);
        context.Products.Add(product);
        context.WarehouseProducts.Add(warehouseProduct);
        await context.SaveChangesAsync();

        var saleService = new SaleService(context, uow, auditMock.Object);

        var dto = new CreateSaleDto(
            CustomerId: 10,
            WarehouseId: 1,
            CashRegisterId: 1,
            UserId: 1,
            DiscountAmount: 0,
            PaidAmount: 0,
            PaymentMethod: PaymentMethod.Credit,
            Notes: "بيع على الحساب",
            Items: new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto(ProductId: 3, UnitId: 1, Quantity: 2, UnitSalePrice: 650, DiscountAmount: 0) // Total: 1300
            }
        );

        // Act
        var result = await saleService.CreateSaleAsync(dto);

        // Assert
        result.RemainingDebt.Should().Be(1300);
        result.PaymentStatus.Should().Be(PaymentStatus.Unpaid);

        var updatedCustomer = await context.Customers.FindAsync(10);
        updatedCustomer!.CurrentDebt.Should().Be(6300); // 5000 + 1300
    }

    [Fact]
    public async Task CreateSale_ShouldThrowCreditLimitExceededException_WhenDebtExceedsLimit()
    {
        // Arrange
        var (context, uow, auditMock) = CreateInMemorySetup(nameof(CreateSale_ShouldThrowCreditLimitExceededException_WhenDebtExceedsLimit));

        var customer = new Customer { Id = 11, Code = "C-11", Name = "كمال دريدي", CreditLimit = 5000, CurrentDebt = 4500, IsActive = true };
        var warehouse = new Warehouse { Id = 1, Code = "WH-MAIN", Name = "المستودع الرئيسي", IsActive = true };
        var register = new CashRegister { Id = 1, Name = "Caisse 01", IsOpen = true, CurrentBalance = 10000 };
        var product = new Product { Id = 4, Code = "PRD-04", Barcode = "61300004", NameAr = "سميد سيم 25 كغ", PurchasePrice = 1000, SalePrice = 1100, IsActive = true };
        var warehouseProduct = new WarehouseProduct { WarehouseId = 1, ProductId = 4, CurrentQuantity = 10, ReservedQuantity = 0 };

        context.Customers.Add(customer);
        context.Warehouses.Add(warehouse);
        context.CashRegisters.Add(register);
        context.Products.Add(product);
        context.WarehouseProducts.Add(warehouseProduct);
        await context.SaveChangesAsync();

        var saleService = new SaleService(context, uow, auditMock.Object);

        var dto = new CreateSaleDto(
            CustomerId: 11,
            WarehouseId: 1,
            CashRegisterId: 1,
            UserId: 1,
            DiscountAmount: 0,
            PaidAmount: 0,
            PaymentMethod: PaymentMethod.Credit,
            Notes: "طلب كريدي يتجاوز السقف",
            Items: new List<CreateSaleItemDto>
            {
                new CreateSaleItemDto(ProductId: 4, UnitId: 1, Quantity: 1, UnitSalePrice: 1100, DiscountAmount: 0) // 4500 + 1100 = 5600 > 5000
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<CreditLimitExceededException>(() => saleService.CreateSaleAsync(dto));
    }
}
