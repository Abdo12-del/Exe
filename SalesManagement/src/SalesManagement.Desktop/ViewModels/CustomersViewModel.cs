using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class CustomersViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;

    private string _searchQuery = string.Empty;
    private bool _filterDebtOnly;
    private CustomerDto? _selectedCustomer;

    // Debt Payment Modal / Subform
    private decimal _paymentAmount;
    private string _paymentNotes = "تسديد دين";

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<PartnerStatementItemDto> StatementItems { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadCustomersAsync();
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
                _ = LoadCustomersAsync();
            }
        }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value) && value != null)
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

    public IAsyncRelayCommand LoadCustomersCommand { get; }
    public IAsyncRelayCommand RecordPaymentCommand { get; }

    public CustomersViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
        LoadCustomersCommand = new AsyncRelayCommand(LoadCustomersAsync);
        RecordPaymentCommand = new AsyncRelayCommand(RecordPaymentAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            IsBusy = true;
            var list = await _customerService.GetAllAsync(SearchQuery, FilterDebtOnly);
            Customers.Clear();
            foreach (var c in list)
            {
                Customers.Add(c);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في جلب الزبائن: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadStatementAsync(int customerId)
    {
        try
        {
            var st = await _customerService.GetCustomerStatementAsync(customerId);
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
        if (SelectedCustomer == null)
        {
            StatusMessage = "يرجى تحديد زبون أولاً.";
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
            await _customerService.RecordDebtPaymentAsync(
                SelectedCustomer.Id,
                PaymentAmount,
                PaymentMethod.Cash,
                1, // CashRegisterId
                1, // UserId
                PaymentNotes
            );

            StatusMessage = $"تم تسجيل تسديد مبلغ {PaymentAmount:N2} دج للزبون {SelectedCustomer.Name} بنجاح وإدخاله في الصندوق.";
            PaymentAmount = 0;
            await LoadCustomersAsync();
            if (SelectedCustomer != null)
            {
                await LoadStatementAsync(SelectedCustomer.Id);
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
