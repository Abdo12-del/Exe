using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class CashRegisterViewModel : ViewModelBase
{
    private readonly ICashRegisterService _cashRegisterService;

    private bool _isOpen;
    private decimal _currentBalance;
    private decimal _openingFloatInput = 10000;
    private decimal _actualCashCounted;
    private decimal _cashInAmount;
    private string _cashInReason = "تغذية الصندوق";
    private decimal _cashOutAmount;
    private string _cashOutReason = "مصاريف يومية";
    private ZReportDto? _lastZReport;

    public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }
    public decimal CurrentBalance { get => _currentBalance; set => SetProperty(ref _currentBalance, value); }
    public decimal OpeningFloatInput { get => _openingFloatInput; set => SetProperty(ref _openingFloatInput, value); }
    public decimal ActualCashCounted { get => _actualCashCounted; set => SetProperty(ref _actualCashCounted, value); }
    public decimal CashInAmount { get => _cashInAmount; set => SetProperty(ref _cashInAmount, value); }
    public string CashInReason { get => _cashInReason; set => SetProperty(ref _cashInReason, value); }
    public decimal CashOutAmount { get => _cashOutAmount; set => SetProperty(ref _cashOutAmount, value); }
    public string CashOutReason { get => _cashOutReason; set => SetProperty(ref _cashOutReason, value); }
    public ZReportDto? LastZReport { get => _lastZReport; set => SetProperty(ref _lastZReport, value); }

    public IAsyncRelayCommand LoadStatusCommand { get; }
    public IAsyncRelayCommand OpenRegisterCommand { get; }
    public IAsyncRelayCommand CloseRegisterCommand { get; }
    public IAsyncRelayCommand CashInCommand { get; }
    public IAsyncRelayCommand CashOutCommand { get; }

    public CashRegisterViewModel(ICashRegisterService cashRegisterService)
    {
        _cashRegisterService = cashRegisterService;

        LoadStatusCommand = new AsyncRelayCommand(LoadStatusAsync);
        OpenRegisterCommand = new AsyncRelayCommand(OpenRegisterAsync);
        CloseRegisterCommand = new AsyncRelayCommand(CloseRegisterAsync);
        CashInCommand = new AsyncRelayCommand(AddCashInAsync);
        CashOutCommand = new AsyncRelayCommand(AddCashOutAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadStatusAsync();
    }

    private async Task LoadStatusAsync()
    {
        try
        {
            IsBusy = true;
            var status = await _cashRegisterService.GetStatusAsync(1);
            IsOpen = status.IsOpen;
            CurrentBalance = status.CurrentBalance;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في فحص حالة الصندوق: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenRegisterAsync()
    {
        try
        {
            IsBusy = true;
            await _cashRegisterService.OpenRegisterAsync(1, OpeningFloatInput, 1);
            StatusMessage = $"تم فتح الصندوق برصيد أولي: {OpeningFloatInput:N2} دج بنجاح.";
            await LoadStatusAsync();
        }
        catch (DomainException dex)
        {
            StatusMessage = dex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في فتح الصندوق: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CloseRegisterAsync()
    {
        try
        {
            IsBusy = true;
            LastZReport = await _cashRegisterService.CloseRegisterAsync(1, ActualCashCounted, 1);
            StatusMessage = $"تم إغلاق الصندوق وطباعة تقرير Z-Report بنجاح! الفارق في الصندوق: {LastZReport.CashDifference:N2} دج";
            await LoadStatusAsync();
        }
        catch (DomainException dex)
        {
            StatusMessage = dex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في إغلاق الصندوق: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddCashInAsync()
    {
        if (CashInAmount <= 0) return;
        try
        {
            IsBusy = true;
            await _cashRegisterService.AddCashTransactionAsync(1, CashTransactionType.CashInDeposit, CashInAmount, CashInReason, 1);
            StatusMessage = $"تم إيداع مبلغ {CashInAmount:N2} دج في الصندوق.";
            CashInAmount = 0;
            await LoadStatusAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    private async Task AddCashOutAsync()
    {
        if (CashOutAmount <= 0) return;
        try
        {
            IsBusy = true;
            await _cashRegisterService.AddCashTransactionAsync(1, CashTransactionType.CashOutWithdrawal, CashOutAmount, CashOutReason, 1);
            StatusMessage = $"تم سحب مبلغ {CashOutAmount:N2} دج من الصندوق.";
            CashOutAmount = 0;
            await LoadStatusAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }
}
