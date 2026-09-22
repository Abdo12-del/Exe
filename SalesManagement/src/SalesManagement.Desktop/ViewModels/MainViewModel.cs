using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Application.DTOs;

namespace SalesManagement.Desktop.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private UserSessionDto? _currentUser;
    private ViewModelBase? _currentView;
    private string _currentTitle = "لوحة التحكم";

    public UserSessionDto? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string CurrentTitle
    {
        get => _currentTitle;
        set => SetProperty(ref _currentTitle, value);
    }

    public IRelayCommand NavigateDashboardCommand { get; }
    public IRelayCommand NavigatePosCommand { get; }
    public IRelayCommand NavigateProductsCommand { get; }
    public IRelayCommand NavigateSalesHistoryCommand { get; }
    public IRelayCommand NavigateCustomersCommand { get; }
    public IRelayCommand NavigateSuppliersCommand { get; }
    public IRelayCommand NavigateCashRegisterCommand { get; }
    public IRelayCommand NavigateExpensesCommand { get; }
    public IRelayCommand NavigateSettingsCommand { get; }

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        NavigateDashboardCommand = new RelayCommand(() => NavigateTo<DashboardViewModel>("لوحة القيادة والمؤشرات"));
        NavigatePosCommand = new RelayCommand(() => NavigateTo<PosViewModel>("شاشة البيع ونقطة الدفع (POS)"));
        NavigateProductsCommand = new RelayCommand(() => NavigateTo<ProductsViewModel>("إدارة المنتجات والمخزون"));
        NavigateSalesHistoryCommand = new RelayCommand(() => NavigateTo<SalesHistoryViewModel>("سجل المبيعات والفواتير"));
        NavigateCustomersCommand = new RelayCommand(() => NavigateTo<CustomersViewModel>("إدارة الزبائن والديون"));
        NavigateSuppliersCommand = new RelayCommand(() => NavigateTo<SuppliersViewModel>("إدارة الموردين والمشتريات"));
        NavigateCashRegisterCommand = new RelayCommand(() => NavigateTo<CashRegisterViewModel>("إدارة الصندوق واليومية (Z-Report)"));
        NavigateExpensesCommand = new RelayCommand(() => NavigateTo<ExpensesViewModel>("إدارة المصاريف والنفقات التشغيلية"));
        NavigateSettingsCommand = new RelayCommand(() => NavigateTo<SettingsViewModel>("إعدادات النظام والنسخ الاحتياطي"));
    }

    public void SetUser(UserSessionDto user)
    {
        CurrentUser = user;
        NavigateTo<DashboardViewModel>("لوحة القيادة والمؤشرات");
    }

    public void NavigateTo<TViewModel>(string title) where TViewModel : ViewModelBase
    {
        CurrentTitle = title;
        var vm = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentView = vm;
        _ = vm.InitializeAsync();
    }
}
