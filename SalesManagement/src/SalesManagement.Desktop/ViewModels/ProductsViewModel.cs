using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;
using SalesManagement.Domain.Exceptions;

namespace SalesManagement.Desktop.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private string _searchQuery = string.Empty;
    private ProductDto? _selectedProduct;

    // Form inputs for Add/Edit
    private string _code = string.Empty;
    private string _barcode = string.Empty;
    private string _nameAr = string.Empty;
    private string _nameFr = string.Empty;
    private decimal _purchasePrice;
    private decimal _salePrice;
    private decimal _wholesalePrice;
    private decimal _minStockAlert = 5;
    private decimal _initialStock = 10;
    private bool _isEditing;

    public ObservableCollection<ProductDto> Products { get; } = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadProductsAsync();
            }
        }
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value != null)
            {
                PopulateForm(value);
            }
        }
    }

    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string Barcode { get => _barcode; set => SetProperty(ref _barcode, value); }
    public string NameAr { get => _nameAr; set => SetProperty(ref _nameAr, value); }
    public string NameFr { get => _nameFr; set => SetProperty(ref _nameFr, value); }
    public decimal PurchasePrice { get => _purchasePrice; set => SetProperty(ref _purchasePrice, value); }
    public decimal SalePrice { get => _salePrice; set => SetProperty(ref _salePrice, value); }
    public decimal WholesalePrice { get => _wholesalePrice; set => SetProperty(ref _wholesalePrice, value); }
    public decimal MinStockAlert { get => _minStockAlert; set => SetProperty(ref _minStockAlert, value); }
    public decimal InitialStock { get => _initialStock; set => SetProperty(ref _initialStock, value); }
    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

    public IAsyncRelayCommand LoadProductsCommand { get; }
    public IAsyncRelayCommand SaveProductCommand { get; }
    public IRelayCommand NewProductCommand { get; }
    public IAsyncRelayCommand GenerateBarcodeCommand { get; }

    public ProductsViewModel(IProductService productService)
    {
        _productService = productService;
        LoadProductsCommand = new AsyncRelayCommand(LoadProductsAsync);
        SaveProductCommand = new AsyncRelayCommand(SaveProductAsync);
        NewProductCommand = new RelayCommand(ClearForm);
        GenerateBarcodeCommand = new AsyncRelayCommand(GenerateEan13Async);
    }

    public override async Task InitializeAsync()
    {
        await LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            IsBusy = true;
            var list = await _productService.GetAllAsync(SearchQuery);
            Products.Clear();
            foreach (var item in list)
            {
                Products.Add(item);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في جلب المنتجات: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PopulateForm(ProductDto p)
    {
        IsEditing = true;
        Code = p.Sku;
        Barcode = p.Barcode;
        NameAr = p.NameAr;
        NameFr = p.NameFr ?? string.Empty;
        PurchasePrice = p.PurchasePrice;
        SalePrice = p.SalePrice;
        WholesalePrice = p.WholesalePrice;
        MinStockAlert = p.MinimumStock;
        InitialStock = p.CurrentStock;
    }

    private void ClearForm()
    {
        IsEditing = false;
        SelectedProduct = null;
        Code = $"PRD-{new Random().Next(1000, 9999)}";
        Barcode = string.Empty;
        NameAr = string.Empty;
        NameFr = string.Empty;
        PurchasePrice = 0;
        SalePrice = 0;
        WholesalePrice = 0;
        MinStockAlert = 5;
        InitialStock = 0;
    }

    private Task GenerateEan13Async()
    {
        // 613 Algerian Country Code Prefix
        string prefix = "613" + DateTime.UtcNow.ToString("MMdd");
        string body = new Random().Next(10000, 99999).ToString();
        string raw12 = (prefix + body)[..12];

        // Calculate check digit
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = raw12[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        int check = (10 - (sum % 10)) % 10;
        Barcode = raw12 + check;
        return Task.CompletedTask;
    }

    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(NameAr))
        {
            StatusMessage = "يرجى إدخال اسم المنتج بالعربية.";
            return;
        }

        if (SalePrice <= 0)
        {
            StatusMessage = "سعر البيع يجب أن يكون أكبر من الصفر.";
            return;
        }

        try
        {
            IsBusy = true;
            var dto = new CreateProductDto(
                Sku: Code,
                Barcode: Barcode,
                NameAr: NameAr,
                NameFr: NameFr,
                CategoryId: 1, // Default Category
                BrandId: 1,    // Default Brand
                UnitId: 1,     // Default Unit
                PurchasePrice: PurchasePrice,
                SalePrice: SalePrice,
                WholesalePrice: WholesalePrice,
                MinimumStock: MinStockAlert,
                TaxPercent: 0,
                Description: null
            );

            if (IsEditing && SelectedProduct != null)
            {
                await _productService.UpdateAsync(SelectedProduct.Id, dto, 1);
                StatusMessage = "تم تحديث بيانات المنتج بنجاح.";
            }
            else
            {
                await _productService.CreateAsync(dto, 1);
                StatusMessage = "تم إضافة المنتج الجديد بنجاح.";
            }

            ClearForm();
            await LoadProductsAsync();
        }
        catch (DomainException dex)
        {
            StatusMessage = dex.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في الحفظ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
