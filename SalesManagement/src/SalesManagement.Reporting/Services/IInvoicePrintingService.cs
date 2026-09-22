using SalesManagement.Reporting.Models;

namespace SalesManagement.Reporting.Services;

public interface IInvoicePrintingService
{
    byte[] GenerateEscPosTicket(PrintInvoiceModel model, bool openCashDrawer = true, bool cutPaper = true);
    string GenerateHtmlReceipt(PrintInvoiceModel model);
    string GenerateHtmlA4Invoice(PrintInvoiceModel model);
    void PrintToThermalPrinter(string printerName, PrintInvoiceModel model, bool openDrawer = true);
}
