using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Entities;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class SalesHistoryViewModel : ViewModelBase
{
    private readonly ISaleService _saleService;
    private readonly Reporting.Services.IInvoicePrintingService _printingService;
    private DateTime _fromDate = DateTime.Today.AddDays(-7);
    private DateTime _toDate = DateTime.Today.AddDays(1);
    private SaleDto? _selectedSale;

    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }

    public ObservableCollection<SaleDto> Sales { get; } = new();

    public SaleDto? SelectedSale
    {
        get => _selectedSale;
        set => SetProperty(ref _selectedSale, value);
    }

    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand ReturnSaleCommand { get; }
    public IRelayCommand ReprintReceiptCommand { get; }

    public SalesHistoryViewModel(ISaleService saleService, Reporting.Services.IInvoicePrintingService printingService)
    {
        _saleService = saleService;
        _printingService = printingService;
        SearchCommand = new AsyncRelayCommand(LoadSalesAsync);
        ReturnSaleCommand = new AsyncRelayCommand(ExecuteReturnSaleAsync);
        ReprintReceiptCommand = new RelayCommand(ExecuteReprintReceipt);
    }

    public override async Task InitializeAsync()
    {
        await LoadSalesAsync();
    }

    private async Task LoadSalesAsync()
    {
        try
        {
            IsBusy = true;
            var list = await _saleService.GetSalesHistoryAsync(FromDate, ToDate);
            Sales.Clear();
            foreach (var s in list)
            {
                Sales.Add(s);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في جلب المبيعات: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecuteReprintReceipt()
    {
        if (SelectedSale == null)
        {
            StatusMessage = "يرجى تحديد فاتورة لإعادة طباعتها.";
            return;
        }

        try
        {
            var printModel = new Reporting.Models.PrintInvoiceModel
            {
                InvoiceNumber = SelectedSale.InvoiceNumber,
                Date = SelectedSale.Timestamp,
                CashierName = SelectedSale.CashierName,
                CustomerName = SelectedSale.CustomerName,
                PaymentMethod = SelectedSale.PaymentMethod.ToString(),
                Subtotal = SelectedSale.Subtotal,
                Discount = SelectedSale.DiscountAmount,
                Total = SelectedSale.TotalAmount,
                PaidAmount = SelectedSale.PaidAmount,
                RemainingDebt = SelectedSale.RemainingDebt,
                Items = SelectedSale.Items.Select((it, idx) => new Reporting.Models.PrintInvoiceItem
                {
                    Number = idx + 1,
                    Name = it.ProductName,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitSalePrice,
                    Total = it.TotalLineAmount
                }).ToList()
            };

            _printingService.PrintToThermalPrinter(string.Empty, printModel, openDrawer: false);
            StatusMessage = $"تم إرسال الفاتورة {SelectedSale.InvoiceNumber} إلى الطابعة.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء الطباعة: {ex.Message}";
        }
    }

    private async Task ExecuteReturnSaleAsync()
    {
        if (SelectedSale == null)
        {
            StatusMessage = "يرجى تحديد فاتورة لإجراء الإرجاع.";
            return;
        }

        try
        {
            IsBusy = true;
            var returnedItems = SelectedSale.Items.Select(i => (i.ProductId, i.Quantity)).ToList();

            await _saleService.ProcessSaleReturnAsync(SelectedSale.Id, returnedItems, RefundMethod.Cash, "إرجاع بضاعة من الزبون", 1);
            StatusMessage = $"تم إرجاع الفاتورة {SelectedSale.InvoiceNumber} واسترداد المبلغ وإعادة المنتجات للمخزون بنجاح.";
            await LoadSalesAsync();
        }
        catch (DomainException dex)
        {
            StatusMessage = dex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في إرجاع الفاتورة: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
