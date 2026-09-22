using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Infrastructure.Data;
using SalesManagement.Infrastructure.Services;
using Xunit;

namespace SalesManagement.Tests;

public class CashRegisterTests
{
    private (AppDbContext Context, Mock<IAuditService> AuditMock) CreateInMemorySetup(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AppDbContext(options);
        var auditMock = new Mock<IAuditService>();

        return (context, auditMock);
    }

    [Fact]
    public async Task CloseRegister_ShouldCalculateZReportCorrectly()
    {
        // Arrange
        var (context, auditMock) = CreateInMemorySetup(nameof(CloseRegister_ShouldCalculateZReportCorrectly));

        var reg = new CashRegister
        {
            Id = 1,
            Name = "الصندوق الرئيسي",
            IsOpen = true,
            OpeningFloat = 10000,
            CurrentBalance = 15500,
            OpenedAt = DateTime.UtcNow.AddHours(-8)
        };
        context.CashRegisters.Add(reg);

        // Add cash transactions
        context.CashTransactions.AddRange(
            new CashTransaction { CashRegisterId = 1, Timestamp = DateTime.UtcNow.AddHours(-7), TransactionType = CashTransactionType.SaleReceipt, Amount = 8000, Notes = "مبيعات" },
            new CashTransaction { CashRegisterId = 1, Timestamp = DateTime.UtcNow.AddHours(-6), TransactionType = CashTransactionType.CustomerDebtPayment, Amount = 2000, Notes = "تحصيل ديون" },
            new CashTransaction { CashRegisterId = 1, Timestamp = DateTime.UtcNow.AddHours(-4), TransactionType = CashTransactionType.SupplierDebtPayment, Amount = -3500, Notes = "تسديد مورد" },
            new CashTransaction { CashRegisterId = 1, Timestamp = DateTime.UtcNow.AddHours(-2), TransactionType = CashTransactionType.ExpensePayment, Amount = -1000, Notes = "مصاريف نقل" }
        );

        // Add sale record
        context.Sales.Add(new Sale
        {
            InvoiceNumber = "INV-001",
            CashRegisterId = 1,
            Timestamp = DateTime.UtcNow.AddHours(-7),
            TotalAmount = 8000,
            PaidAmount = 8000,
            RemainingDebt = 0,
            NetProfit = 1600,
            PaymentMethod = PaymentMethod.Cash
        });

        await context.SaveChangesAsync();

        var service = new CashRegisterService(context, auditMock.Object);

        // Expected cash: 10,000 + 8,000 + 2,000 - 3,500 - 1,000 = 15,500 DZD
        decimal countedCash = 15400; // 100 DZD deficit

        // Act
        var zreport = await service.CloseRegisterAsync(1, countedCash, 1);

        // Assert
        zreport.Should().NotBeNull();
        zreport.OpeningFloat.Should().Be(10000);
        zreport.TotalCashSales.Should().Be(8000);
        zreport.CustomerDebtCollections.Should().Be(2000);
        zreport.SupplierDebtPayments.Should().Be(3500);
        zreport.CashExpenses.Should().Be(1000);
        zreport.ExpectedCashInDrawer.Should().Be(15500);
        zreport.ActualCashCounted.Should().Be(15400);
        zreport.CashDifference.Should().Be(-100); // 100 DZD shortage

        // Register is now closed
        var closedRegister = await context.CashRegisters.FindAsync(1);
        closedRegister!.IsOpen.Should().BeFalse();
        closedRegister.CurrentBalance.Should().Be(0);
    }
}
