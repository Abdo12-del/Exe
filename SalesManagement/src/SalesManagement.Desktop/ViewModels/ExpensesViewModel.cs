using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class ExpensesViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;

    private ExpenseCategoryDto? _selectedCategory;
    private decimal _amount;
    private string _title = string.Empty;
    private string _notes = string.Empty;

    public ObservableCollection<ExpenseCategoryDto> Categories { get; } = new();
    public ObservableCollection<ExpenseDto> Expenses { get; } = new();

    public ExpenseCategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public decimal TotalExpenses => Expenses.Sum(e => e.Amount);

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand SaveExpenseCommand { get; }

    public ExpensesViewModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        SaveExpenseCommand = new AsyncRelayCommand(SaveExpenseAsync);
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
            var cats = await _expenseService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);
            SelectedCategory = Categories.FirstOrDefault();

            var list = await _expenseService.GetExpensesAsync();
            Expenses.Clear();
            foreach (var e in list) Expenses.Add(e);

            OnPropertyChanged(nameof(TotalExpenses));
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في تحميل المصاريف: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveExpenseAsync()
    {
        if (SelectedCategory == null)
        {
            StatusMessage = "يرجى تحديد فئة المصروف أولاً.";
            return;
        }

        if (Amount <= 0)
        {
            StatusMessage = "مبلغ المصروف يجب أن يكون أكبر من الصفر.";
            return;
        }

        try
        {
            IsBusy = true;
            var dto = new CreateExpenseDto(
                SelectedCategory.Id,
                Amount,
                1, // CashRegisterId (deduct from register)
                1, // UserId
                Title,
                Notes
            );

            await _expenseService.CreateExpenseAsync(dto);
            StatusMessage = $"تم تسجيل المصروف بقيمة {Amount:N2} دج وخصمه من الصندوق بنجاح.";

            Amount = 0;
            Title = string.Empty;
            Notes = string.Empty;

            await LoadDataAsync();
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
