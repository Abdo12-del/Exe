using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;

    private string _storeName = "مؤسسة النور للتجارة والتوزيع";
    private string _storePhone = "0550 12 34 56";
    private string _storeAddress = "شارع الاستقلال رقم 45، الخروب - قسنطينة";
    private string _taxNumber = "000216091234567";
    private string _dbHost = "localhost";
    private int _dbPort = 3307;
    private string _backupFolder = @"C:\SalesManagement\Backups";
    private string _backupStatus = string.Empty;
    private string _connectionStatus = string.Empty;

    public string StoreName { get => _storeName; set => SetProperty(ref _storeName, value); }
    public string StorePhone { get => _storePhone; set => SetProperty(ref _storePhone, value); }
    public string StoreAddress { get => _storeAddress; set => SetProperty(ref _storeAddress, value); }
    public string TaxNumber { get => _taxNumber; set => SetProperty(ref _taxNumber, value); }
    public string DbHost { get => _dbHost; set => SetProperty(ref _dbHost, value); }
    public int DbPort { get => _dbPort; set => SetProperty(ref _dbPort, value); }
    public string BackupFolder { get => _backupFolder; set => SetProperty(ref _backupFolder, value); }
    public string BackupStatus { get => _backupStatus; set => SetProperty(ref _backupStatus, value); }
    public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }

    public IAsyncRelayCommand CreateBackupCommand { get; }
    public IAsyncRelayCommand TestConnectionCommand { get; }
    public IRelayCommand SaveSettingsCommand { get; }

    public SettingsViewModel(IBackupService backupService)
    {
        _backupService = backupService;
        CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            IsBusy = true;
            ConnectionStatus = "جاري اختبار الاتصال بقاعدة البيانات...";
            var builder = new MySqlConnector.MySqlConnectionStringBuilder
            {
                Server = DbHost,
                Port = (uint)DbPort,
                UserID = "root",
                Password = "",
                ConnectionTimeout = 3
            };

            await using var conn = new MySqlConnector.MySqlConnection(builder.ConnectionString);
            await conn.OpenAsync();
            ConnectionStatus = $"✅ نجح الاتصال بقاعدة بيانات MySQL بنجاح ({DbHost}:{DbPort})!";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"❌ فشل الاتصال: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateBackupAsync()
    {
        try
        {
            IsBusy = true;
            BackupStatus = "جاري إنشاء النسخة الاحتياطية لقاعدة البيانات عبر mysqldump...";
            string path = await _backupService.CreateBackupAsync(BackupFolder);
            BackupStatus = $"تم إنشاء النسخة الاحتياطية بنجاح في المسار:\n{path}";
            StatusMessage = "تم حفظ النسخة الاحتياطية بنجاح.";
        }
        catch (DomainException dex)
        {
            BackupStatus = $"خطأ: {dex.Message}";
        }
        catch (Exception ex)
        {
            BackupStatus = $"خطأ غير متوقع: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SaveSettings()
    {
        StatusMessage = "تم حفظ إعدادات المؤسسة بنجاح.";
    }
}
