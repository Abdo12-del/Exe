using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;

namespace SalesManagement.Desktop.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;

    private decimal _todaySales;
    private int _todayInvoices;
    private decimal _todayProfit;
    private decimal _monthSales;
    private decimal _inventoryCost;
    private decimal _inventoryRetail;
    private decimal _totalCustomerDebts;
    private decimal _totalSupplierDebts;
    private int _lowStockCount;

    public decimal TodaySales { get => _todaySales; set => SetProperty(ref _todaySales, value); }
    public int TodayInvoices { get => _todayInvoices; set => SetProperty(ref _todayInvoices, value); }
    public decimal TodayProfit { get => _todayProfit; set => SetProperty(ref _todayProfit, value); }
    public decimal MonthSales { get => _monthSales; set => SetProperty(ref _monthSales, value); }
    public decimal InventoryCost { get => _inventoryCost; set => SetProperty(ref _inventoryCost, value); }
    public decimal InventoryRetail { get => _inventoryRetail; set => SetProperty(ref _inventoryRetail, value); }
    public decimal TotalCustomerDebts { get => _totalCustomerDebts; set => SetProperty(ref _totalCustomerDebts, value); }
    public decimal TotalSupplierDebts { get => _totalSupplierDebts; set => SetProperty(ref _totalSupplierDebts, value); }
    public int LowStockCount { get => _lowStockCount; set => SetProperty(ref _lowStockCount, value); }

    public ObservableCollection<TopProductDto> TopProducts { get; } = new();
    public ObservableCollection<DailyTrendDto> SalesTrend { get; } = new();

    public IAsyncRelayCommand RefreshCommand { get; }

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var summary = await _dashboardService.GetDashboardSummaryAsync();

            TodaySales = summary.TodaySales;
            TodayInvoices = summary.TodayInvoicesCount;
            TodayProfit = summary.TodayNetProfit;
            MonthSales = summary.MonthSales;
            InventoryCost = summary.TotalInventoryCostValue;
            InventoryRetail = summary.TotalInventoryRetailValue;
            TotalCustomerDebts = summary.TotalCustomerDebts;
            TotalSupplierDebts = summary.TotalSupplierDebts;
            LowStockCount = summary.LowStockProductsCount;

            TopProducts.Clear();
            foreach (var p in summary.TopSellingProducts)
            {
                TopProducts.Add(p);
            }

            SalesTrend.Clear();
            foreach (var t in summary.Last7DaysTrend)
            {
                SalesTrend.Add(t);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"فشل تحميل البيانات: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
