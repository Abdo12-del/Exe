using Microsoft.EntityFrameworkCore;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Infrastructure.Data;

namespace SalesManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        var todaySalesList = await _context.Sales
            .AsNoTracking()
            .Where(s => s.Timestamp >= today && !s.IsCancelled)
            .ToListAsync();

        decimal todaySales = todaySalesList.Sum(s => s.TotalAmount);
        int todayInvoices = todaySalesList.Count;
        decimal todayProfit = todaySalesList.Sum(s => s.NetProfit);

        decimal monthSales = await _context.Sales
            .AsNoTracking()
            .Where(s => s.Timestamp >= firstDayOfMonth && !s.IsCancelled)
            .SumAsync(s => s.TotalAmount);

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.WarehouseProducts)
            .Where(p => p.IsActive)
            .ToListAsync();

        decimal totalCost = products.Sum(p => p.PurchasePrice * p.GetTotalStock());
        decimal totalRetail = products.Sum(p => p.SalePrice * p.GetTotalStock());
        int lowStockCount = products.Count(p => p.IsLowStock());

        decimal totalCustomerDebts = await _context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .SumAsync(c => c.CurrentDebt);

        decimal totalSupplierDebts = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .SumAsync(s => s.CurrentDebt);

        // Top selling products
        var topProductsRaw = await _context.SaleItems
            .AsNoTracking()
            .Where(si => !si.Sale.IsCancelled)
            .GroupBy(si => new { si.ProductId, si.Product.NameAr })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.NameAr,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.TotalLineAmount)
            ))
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(6)
            .ToListAsync();

        // Last 7 days trend
        var last7Days = new List<DailyTrendDto>();
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var nextDay = day.AddDays(1);

            var daySales = await _context.Sales
                .AsNoTracking()
                .Where(s => s.Timestamp >= day && s.Timestamp < nextDay && !s.IsCancelled)
                .ToListAsync();

            last7Days.Add(new DailyTrendDto(
                $"{day.Day}/{day.Month}",
                daySales.Sum(s => s.TotalAmount),
                daySales.Sum(s => s.NetProfit)
            ));
        }

        return new DashboardSummaryDto(
            todaySales,
            todayInvoices,
            todayProfit,
            monthSales,
            totalCost,
            totalRetail,
            totalCustomerDebts,
            totalSupplierDebts,
            lowStockCount,
            topProductsRaw,
            last7Days
        );
    }
}
