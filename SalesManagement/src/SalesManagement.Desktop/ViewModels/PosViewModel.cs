using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class PosCartItemViewModel : ViewModelBase
{
    private decimal _quantity = 1;
    private decimal _unitPrice;
    private decimal _discount;

    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal AvailableStock { get; set; }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(TotalLine));
            }
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
            {
                OnPropertyChanged(nameof(TotalLine));
            }
        }
    }

    public decimal Discount
    {
        get => _discount;
        set
        {
            if (SetProperty(ref _discount, value))
            {
                OnPropertyChanged(nameof(TotalLine));
            }
        }
    }

    public decimal TotalLine => Math.Max(0, (Quantity * UnitPrice) - Discount);
}

public class PosViewModel : ViewModelBase
{
    private readonly ISaleService _saleService;
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;
    private readonly ICashRegisterService _cashRegisterService;
    private readonly Reporting.Services.IInvoicePrintingService _printingService;

    private string _barcodeInput = string.Empty;
    private string _searchProductQuery = string.Empty;
    private CustomerDto? _selectedCustomer;
    private PosCartItemViewModel? _selectedCartItem;
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    private decimal _invoiceDiscount;
    private decimal _receivedCash;
    private string _customerDebtNote = string.Empty;

    // Parked Cart holder
    private List<PosCartItemViewModel>? _parkedCart;

    public ObservableCollection<PosCartItemViewModel> CartItems { get; } = new();
    public ObservableCollection<ProductDto> SearchProductsResults { get; } = new();
    public ObservableCollection<CustomerDto> CustomersList { get; } = new();

    public string BarcodeInput
    {
        get => _barcodeInput;
        set => SetProperty(ref _barcodeInput, value);
    }

    public string SearchProductQuery
    {
        get => _searchProductQuery;
        set
        {
            if (SetProperty(ref _searchProductQuery, value))
            {
                _ = SearchProductsAsync();
            }
        }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public PosCartItemViewModel? SelectedCartItem
    {
        get => _selectedCartItem;
        set => SetProperty(ref _selectedCartItem, value);
    }

    public PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (SetProperty(ref _selectedPaymentMethod, value))
            {
                RecalculateTotals();
            }
        }
    }

    public decimal InvoiceDiscount
    {
        get => _invoiceDiscount;
        set
        {
            if (SetProperty(ref _invoiceDiscount, value))
            {
                RecalculateTotals();
            }
        }
    }

    public decimal ReceivedCash
    {
        get => _receivedCash;
        set
        {
            if (SetProperty(ref _receivedCash, value))
            {
                RecalculateTotals();
            }
        }
    }

    public string CustomerDebtNote
    {
        get => _customerDebtNote;
        set => SetProperty(ref _customerDebtNote, value);
    }

    // Calculated Totals
    public decimal Subtotal => CartItems.Sum(x => x.TotalLine);
    public decimal TotalAmount => Math.Max(0, Subtotal - InvoiceDiscount);
    public decimal ChangeAmount => SelectedPaymentMethod == PaymentMethod.Cash ? Math.Max(0, ReceivedCash - TotalAmount) : 0;
    public decimal RemainingDebt => SelectedPaymentMethod == PaymentMethod.Credit ? TotalAmount : (SelectedPaymentMethod == PaymentMethod.Cash && ReceivedCash < TotalAmount ? TotalAmount - ReceivedCash : 0);

    // Commands
    public IAsyncRelayCommand ProcessBarcodeCommand { get; }
    public IRelayCommand<ProductDto> AddProductToCartCommand { get; }
    public IRelayCommand RemoveSelectedCartItemCommand { get; }
    public IRelayCommand IncreaseQuantityCommand { get; }
    public IRelayCommand DecreaseQuantityCommand { get; }
    public IRelayCommand ClearCartCommand { get; }
    public IRelayCommand ParkCartCommand { get; }
    public IRelayCommand RecallCartCommand { get; }
    public IAsyncRelayCommand CheckoutAndPrintCommand { get; }

    public PosViewModel(
        ISaleService saleService,
        IProductService productService,
        ICustomerService customerService,
        ICashRegisterService cashRegisterService,
        Reporting.Services.IInvoicePrintingService printingService)
    {
        _saleService = saleService;
        _productService = productService;
        _customerService = customerService;
        _cashRegisterService = cashRegisterService;
        _printingService = printingService;

        ProcessBarcodeCommand = new AsyncRelayCommand(ExecuteProcessBarcodeAsync);
        AddProductToCartCommand = new RelayCommand<ProductDto>(ExecuteAddProductToCart);
        RemoveSelectedCartItemCommand = new RelayCommand(ExecuteRemoveSelectedCartItem);
        IncreaseQuantityCommand = new RelayCommand(ExecuteIncreaseQuantity);
        DecreaseQuantityCommand = new RelayCommand(ExecuteDecreaseQuantity);
        ClearCartCommand = new RelayCommand(ExecuteClearCart);
        ParkCartCommand = new RelayCommand(ExecuteParkCart);
        RecallCartCommand = new RelayCommand(ExecuteRecallCart);
        CheckoutAndPrintCommand = new AsyncRelayCommand(ExecuteCheckoutAndPrintAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var custs = await _customerService.GetAllAsync();
            CustomersList.Clear();
            foreach (var c in custs)
            {
                CustomersList.Add(c);
            }
            SelectedCustomer = CustomersList.FirstOrDefault(c => c.Name.Contains("زبون نقدي") || c.Name.Contains("عادي")) 
                               ?? CustomersList.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في تحميل الزبائن: {ex.Message}";
        }
    }

    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchProductQuery) || SearchProductQuery.Length < 2)
        {
            SearchProductsResults.Clear();
            return;
        }

        try
        {
            var results = await _productService.GetAllAsync(SearchProductQuery);
            SearchProductsResults.Clear();
            foreach (var r in results.Take(15))
            {
                SearchProductsResults.Add(r);
            }
        }
        catch { }
    }

    private async Task ExecuteProcessBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;
        string barcode = BarcodeInput.Trim();
        BarcodeInput = string.Empty;

        try
        {
            var product = await _productService.GetByBarcodeAsync(barcode);
            if (product == null)
            {
                System.Media.SystemSounds.Hand.Play();
                StatusMessage = $"المنتج ذو الباركود {barcode} غير مسجل في النظام.";
                return;
            }

            AddOrIncrementCart(product.Id, product.Barcode, product.NameAr, product.SalePrice, product.TotalStock);
            System.Media.SystemSounds.Beep.Play();
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
    }

    private void ExecuteAddProductToCart(ProductDto? p)
    {
        if (p == null) return;
        AddOrIncrementCart(p.Id, p.Barcode, p.NameAr, p.SalePrice, p.CurrentStock);
    }

    private void AddOrIncrementCart(int id, string barcode, string name, decimal price, decimal stock)
    {
        var existing = CartItems.FirstOrDefault(x => x.ProductId == id);
        if (existing != null)
        {
            existing.Quantity += 1;
        }
        else
        {
            var item = new PosCartItemViewModel
            {
                ProductId = id,
                Barcode = barcode,
                ProductName = name,
                UnitPrice = price,
                Quantity = 1,
                Discount = 0,
                AvailableStock = stock
            };
            CartItems.Add(item);
            SelectedCartItem = item;
        }

        RecalculateTotals();
    }

    private void ExecuteIncreaseQuantity()
    {
        if (SelectedCartItem != null)
        {
            SelectedCartItem.Quantity += 1;
            RecalculateTotals();
        }
    }

    private void ExecuteDecreaseQuantity()
    {
        if (SelectedCartItem != null)
        {
            if (SelectedCartItem.Quantity > 1)
            {
                SelectedCartItem.Quantity -= 1;
            }
            else
            {
                CartItems.Remove(SelectedCartItem);
                SelectedCartItem = CartItems.LastOrDefault();
            }
            RecalculateTotals();
        }
    }

    private void ExecuteRemoveSelectedCartItem()
    {
        if (SelectedCartItem != null)
        {
            CartItems.Remove(SelectedCartItem);
            SelectedCartItem = CartItems.LastOrDefault();
            RecalculateTotals();
        }
    }

    private void ExecuteClearCart()
    {
        CartItems.Clear();
        InvoiceDiscount = 0;
        ReceivedCash = 0;
        CustomerDebtNote = string.Empty;
        RecalculateTotals();
    }

    private void ExecuteParkCart()
    {
        if (!CartItems.Any()) return;
        _parkedCart = CartItems.ToList();
        ExecuteClearCart();
        StatusMessage = "تم تعليق السلة بنجاح (يمكن استرجاعها بـ F6).";
    }

    private void ExecuteRecallCart()
    {
        if (_parkedCart == null || !_parkedCart.Any())
        {
            StatusMessage = "لا توجد سلة معلقة لاسترجاعها.";
            return;
        }

        ExecuteClearCart();
        foreach (var item in _parkedCart)
        {
            CartItems.Add(item);
        }
        _parkedCart = null;
        RecalculateTotals();
        StatusMessage = "تم استرجاع السلة المعلقة.";
    }

    public void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(RemainingDebt));
    }

    private async Task ExecuteCheckoutAndPrintAsync()
    {
        if (!CartItems.Any())
        {
            StatusMessage = "السلة فارغة. يرجى إضافة منتجات أولاً.";
            return;
        }

        if (SelectedPaymentMethod == PaymentMethod.Credit && (SelectedCustomer == null || SelectedCustomer.Name.Contains("زبون نقدي")))
        {
            StatusMessage = "تنبيه: لا يمكن تسجيل عملية بيع بالكريدي (دين) على حساب زبون نقدي عام! يرجى تحديد الزبون بالاسم.";
            return;
        }

        try
        {
            IsBusy = true;

            decimal paid = SelectedPaymentMethod == PaymentMethod.Cash ? Math.Min(TotalAmount, ReceivedCash) : (SelectedPaymentMethod == PaymentMethod.Credit ? 0 : TotalAmount);

            var saleDto = new CreateSaleDto(
                CustomerId: SelectedCustomer?.Id ?? 1,
                WarehouseId: 1, // Default main warehouse
                CashRegisterId: 1, // Default primary register
                UserId: 1, // Active logged in cashier
                DiscountAmount: InvoiceDiscount,
                PaidAmount: paid,
                PaymentMethod: SelectedPaymentMethod,
                Notes: CustomerDebtNote,
                Items: CartItems.Select(i => new CreateSaleItemDto(i.ProductId, 1, i.Quantity, i.UnitPrice, i.Discount)).ToList()
            );

            var sale = await _saleService.CreateSaleAsync(saleDto);

            // Print Thermal Receipt
            try
            {
                var printModel = new Reporting.Models.PrintInvoiceModel
                {
                    InvoiceNumber = sale.InvoiceNumber,
                    Date = sale.Timestamp,
                    CashierName = "الكاشير",
                    CustomerName = SelectedCustomer?.Name ?? "زبون عادي",
                    PaymentMethod = SelectedPaymentMethod == PaymentMethod.Cash ? "نقداً" : (SelectedPaymentMethod == PaymentMethod.Credit ? "كريدي" : "بطاقة بنكية"),
                    Subtotal = Subtotal,
                    Discount = InvoiceDiscount,
                    Total = TotalAmount,
                    PaidAmount = paid,
                    ChangeAmount = ChangeAmount,
                    RemainingDebt = RemainingDebt,
                    Items = CartItems.Select((ci, idx) => new Reporting.Models.PrintInvoiceItem
                    {
                        Number = idx + 1,
                        Name = ci.ProductName,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.UnitPrice,
                        Total = ci.TotalLine
                    }).ToList()
                };

                _printingService.PrintToThermalPrinter(string.Empty, printModel, openDrawer: true);
            }
            catch { }

            StatusMessage = $"تم تسجيل وطباعة الفاتورة {sale.InvoiceNumber} بنجاح! الصرف: {ChangeAmount:N2} دج";
            System.Media.SystemSounds.Asterisk.Play();

            // Clear Cart for next sale
            ExecuteClearCart();
        }
        catch (DomainException dex)
        {
            StatusMessage = dex.Message;
            System.Media.SystemSounds.Hand.Play();
        }
        catch (Exception ex)
        {
            StatusMessage = $"حدث خطأ أثناء حفظ الفاتورة: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
