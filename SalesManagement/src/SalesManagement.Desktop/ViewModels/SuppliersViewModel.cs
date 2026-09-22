using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class SuppliersViewModel : ViewModelBase
{
    private readonly ISupplierService _supplierService;
    private readonly IPurchaseService _purchaseService;
    private readonly IProductService _productService;

    private string _searchQuery = string.Empty;
    private bool _filterDebtOnly;
    private SupplierDto? _selectedSupplier;

    // Supplier Payment
    private decimal _paymentAmount;
    private string _paymentNotes = "تسديد مستحقات مورد";

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<PartnerStatementItemDto> StatementItems { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadSuppliersAsync();
            }
        }
    }

    public bool FilterDebtOnly
    {
        get => _filterDebtOnly;
        set
        {
            if (SetProperty(ref _filterDebtOnly, value))
            {
                _ = LoadSuppliersAsync();
            }
        }
    }

    public SupplierDto? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value) && value != null)
            {
                PaymentAmount = value.CurrentDebt;
                _ = LoadStatementAsync(value.Id);
            }
        }
    }

    public decimal PaymentAmount
    {
        get => _paymentAmount;
        set => SetProperty(ref _paymentAmount, value);
    }

    public string PaymentNotes
    {
        get => _paymentNotes;
        set => SetProperty(ref _paymentNotes, value);
    }

    public IAsyncRelayCommand LoadSuppliersCommand { get; }
    public IAsyncRelayCommand RecordPaymentCommand { get; }

    public SuppliersViewModel(ISupplierService supplierService, IPurchaseService purchaseService, IProductService productService)
    {
        _supplierService = supplierService;
        _purchaseService = purchaseService;
        _productService = productService;

        LoadSuppliersCommand = new AsyncRelayCommand(LoadSuppliersAsync);
        RecordPaymentCommand = new AsyncRelayCommand(RecordPaymentAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadSuppliersAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        try
        {
            IsBusy = true;
            var list = await _supplierService.GetAllAsync(SearchQuery, FilterDebtOnly);
            Suppliers.Clear();
            foreach (var s in list)
            {
                Suppliers.Add(s);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في جلب الموردين: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadStatementAsync(int supplierId)
    {
        try
        {
            var st = await _supplierService.GetSupplierStatementAsync(supplierId);
            StatementItems.Clear();
            foreach (var item in st)
            {
                StatementItems.Add(item);
            }
        }
        catch { }
    }

    private async Task RecordPaymentAsync()
    {
        if (SelectedSupplier == null)
        {
            StatusMessage = "يرجى اختيار مورد أولاً.";
            return;
        }

        if (PaymentAmount <= 0)
        {
            StatusMessage = "مبلغ التسديد يجب أن يكون أكبر من الصفر.";
            return;
        }

        try
        {
            IsBusy = true;
            await _supplierService.RecordDebtPaymentAsync(
                SelectedSupplier.Id,
                PaymentAmount,
                PaymentMethod.Cash,
                1, // CashRegisterId
                1, // UserId
                PaymentNotes
            );

            StatusMessage = $"تم تسديد مبلغ {PaymentAmount:N2} دج للمورد {SelectedSupplier.Name} وخصمه من الصندوق.";
            PaymentAmount = 0;
            await LoadSuppliersAsync();
            if (SelectedSupplier != null)
            {
                await LoadStatementAsync(SelectedSupplier.Id);
            }
        }
        catch (DomainException dex)
        {
            StatusMessage = dex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
