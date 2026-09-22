namespace SalesManagement.Reporting.Models;

public class PrintInvoiceModel
{
    public string StoreName { get; set; } = "مؤسسة النور للتجارة والتوزيع";
    public string StoreAddress { get; set; } = "شارع الاستقلال رقم 45، الخروب - قسنطينة";
    public string StorePhone { get; set; } = "0550 12 34 56";
    public string TaxId { get; set; } = "000216091234567";

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string CashierName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "نقداً";

    public List<PrintInvoiceItem> Items { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal RemainingDebt { get; set; }

    public string FooterMessage { get; set; } = "شكراً لزيارتكم! البضاعة المباعة ترد أو تستبدل خلال 48 ساعة.";
}

public class PrintInvoiceItem
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}
