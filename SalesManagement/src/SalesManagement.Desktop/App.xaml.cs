using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Application.Interfaces;
using SalesManagement.Desktop.Services;
using SalesManagement.Desktop.ViewModels;
using SalesManagement.Desktop.Views;
using SalesManagement.Infrastructure.Data;
using SalesManagement.Infrastructure.Services;
using SalesManagement.Reporting.Services;

namespace SalesManagement.Desktop;

public partial class App : System.Windows.Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        // Set Arabic Culture as default
        var culture = new CultureInfo("ar-DZ");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Build Configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        // Ensure Portable MySQL & Database Initialized
        var portableManager = new PortableMySqlManager(Configuration);
        await portableManager.EnsureDatabaseReadyAsync();

        // Configure Dependency Injection
        var services = new ServiceCollection();

        string connectionString = Configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Port=3307;Database=sales_management;User=root;Password=;CharSet=utf8mb4;";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        // Application & Infrastructure Services
        services.AddSingleton<IConfiguration>(Configuration);
        services.AddSingleton(portableManager);
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IInventoryCountService, InventoryCountService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBackupService, MySqlBackupService>();
        services.AddSingleton<IBarcodeGeneratorService, BarcodeGeneratorService>();
        services.AddSingleton<IInvoicePrintingService, InvoicePrintingService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<SalesHistoryViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<CashRegisterViewModel>();
        services.AddTransient<ExpensesViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Windows
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();

        // Show Login Window
        var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        PortableMySqlManager.ShutdownPortableServer();
    }
}
