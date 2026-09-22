using System.Text;
using SalesManagement.Reporting.Models;

namespace SalesManagement.Reporting.Services;

public class InvoicePrintingService : IInvoicePrintingService
{
    private readonly IBarcodeGeneratorService _barcodeService;

    public InvoicePrintingService(IBarcodeGeneratorService barcodeService)
    {
        _barcodeService = barcodeService;
    }

    public byte[] GenerateEscPosTicket(PrintInvoiceModel model, bool openCashDrawer = true, bool cutPaper = true)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // ESC @: Initialize printer
        bw.Write(new byte[] { 0x1B, 0x40 });

        // Optional: Open Cash Drawer (ESC p m t1 t2)
        if (openCashDrawer)
        {
            bw.Write(new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA });
        }

        // Align Center
        bw.Write(new byte[] { 0x1B, 0x61, 0x01 });

        // Bold & Double Height for Store Name
        bw.Write(new byte[] { 0x1B, 0x45, 0x01 }); // Bold On
        bw.Write(new byte[] { 0x1D, 0x21, 0x11 }); // Double width + double height
        WriteText(bw, $"{model.StoreName}\n");

        // Normal text
        bw.Write(new byte[] { 0x1D, 0x21, 0x00 });
        bw.Write(new byte[] { 0x1B, 0x45, 0x00 }); // Bold Off

        if (!string.IsNullOrEmpty(model.StoreAddress))
            WriteText(bw, $"{model.StoreAddress}\n");
        if (!string.IsNullOrEmpty(model.StorePhone))
            WriteText(bw, $"هاتف: {model.StorePhone}\n");
        if (!string.IsNullOrEmpty(model.TaxId))
            WriteText(bw, $"NIF: {model.TaxId}\n");

        WriteText(bw, "================================================\n");

        // Align Right (Arabic flow)
        bw.Write(new byte[] { 0x1B, 0x61, 0x02 });

        WriteText(bw, $"رقم الفاتورة: {model.InvoiceNumber}\n");
        WriteText(bw, $"التاريخ: {model.Date:yyyy-MM-dd HH:mm:ss}\n");
        WriteText(bw, $"الكاشير: {model.CashierName}\n");
        if (!string.IsNullOrEmpty(model.CustomerName))
            WriteText(bw, $"الزبون: {model.CustomerName}\n");
        WriteText(bw, $"طريقة الدفع: {model.PaymentMethod}\n");

        WriteText(bw, "------------------------------------------------\n");
        WriteText(bw, string.Format("{0,-20} {1,6} {2,9} {3,10}\n", "التعيين", "الكمية", "السعر", "الإجمالي"));
        WriteText(bw, "------------------------------------------------\n");

        foreach (var item in model.Items)
        {
            string name = item.Name.Length > 20 ? item.Name[..19] + "." : item.Name;
            WriteText(bw, string.Format("{0,-20} {1,6:N0} {2,9:N2} {3,10:N2}\n", name, item.Quantity, item.UnitPrice, item.Total));
        }

        WriteText(bw, "================================================\n");

        // Totals (Align Left/Right)
        bw.Write(new byte[] { 0x1B, 0x45, 0x01 }); // Bold On
        WriteText(bw, string.Format("{0,-28} {1,18:N2} دج\n", "المجموع الجزئي:", model.Subtotal));
        if (model.Discount > 0)
        {
            WriteText(bw, string.Format("{0,-28} -{1,17:N2} دج\n", "التخفيض:", model.Discount));
        }
        WriteText(bw, string.Format("{0,-28} {1,18:N2} دج\n", "الصافي للدفع (Net):", model.Total));
        bw.Write(new byte[] { 0x1B, 0x45, 0x00 }); // Bold Off

        WriteText(bw, "------------------------------------------------\n");
        WriteText(bw, string.Format("{0,-28} {1,18:N2} دج\n", "المبلغ المستلم:", model.PaidAmount));
        if (model.ChangeAmount > 0)
        {
            WriteText(bw, string.Format("{0,-28} {1,18:N2} دج\n", "المتبقي للزبون (الصرف):", model.ChangeAmount));
        }
        if (model.RemainingDebt > 0)
        {
            bw.Write(new byte[] { 0x1B, 0x45, 0x01 });
            WriteText(bw, string.Format("{0,-28} {1,18:N2} دج\n", "المتبقي كدين (كريدي):", model.RemainingDebt));
            bw.Write(new byte[] { 0x1B, 0x45, 0x00 });
        }

        // Align Center for footer
        bw.Write(new byte[] { 0x1B, 0x61, 0x01 });
        WriteText(bw, "\n");
        WriteText(bw, $"{model.FooterMessage}\n");
        WriteText(bw, "*** تاجر برو - نظام إدارة المبيعات ***\n\n\n\n");

        // Cut Paper (GS V 0)
        if (cutPaper)
        {
            bw.Write(new byte[] { 0x1D, 0x56, 0x00 });
        }

        return ms.ToArray();
    }

    public string GenerateHtmlReceipt(PrintInvoiceModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; width: 80mm; margin: 0 auto; padding: 10px; font-size: 13px; color: #000; }");
        sb.AppendLine(".center { text-align: center; }");
        sb.AppendLine(".bold { font-weight: bold; }");
        sb.AppendLine(".title { font-size: 18px; margin-bottom: 4px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 8px; margin-bottom: 8px; }");
        sb.AppendLine("th, td { padding: 4px 2px; text-align: right; }");
        sb.AppendLine("th { border-bottom: 1px dashed #000; }");
        sb.AppendLine(".line { border-bottom: 1px dashed #000; margin: 8px 0; }");
        sb.AppendLine(".double-line { border-bottom: 2px solid #000; margin: 8px 0; }");
        sb.AppendLine(".total-row { font-size: 15px; font-weight: bold; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<div class='center bold title'>{model.StoreName}</div>");
        if (!string.IsNullOrEmpty(model.StoreAddress)) sb.AppendLine($"<div class='center'>{model.StoreAddress}</div>");
        if (!string.IsNullOrEmpty(model.StorePhone)) sb.AppendLine($"<div class='center'>هاتف: {model.StorePhone}</div>");
        if (!string.IsNullOrEmpty(model.TaxId)) sb.AppendLine($"<div class='center'>NIF: {model.TaxId}</div>");

        sb.AppendLine("<div class='double-line'></div>");
        sb.AppendLine($"<div><strong>رقم الفاتورة:</strong> {model.InvoiceNumber}</div>");
        sb.AppendLine($"<div><strong>التاريخ:</strong> {model.Date:yyyy-MM-dd HH:mm}</div>");
        sb.AppendLine($"<div><strong>الكاشير:</strong> {model.CashierName}</div>");
        if (!string.IsNullOrEmpty(model.CustomerName)) sb.AppendLine($"<div><strong>الزبون:</strong> {model.CustomerName}</div>");
        sb.AppendLine($"<div><strong>طريقة الدفع:</strong> {model.PaymentMethod}</div>");

        sb.AppendLine("<table><thead><tr><th>المنتج</th><th>الكمية</th><th>السعر</th><th>المجموع</th></tr></thead><tbody>");
        foreach (var item in model.Items)
        {
            sb.AppendLine($"<tr><td>{item.Name}</td><td>{item.Quantity:N0}</td><td>{item.UnitPrice:N2}</td><td class='bold'>{item.Total:N2}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<div class='line'></div>");
        sb.AppendLine($"<div style='display:flex;justify-content:space-between'><span>المجموع الجزئي:</span><span>{model.Subtotal:N2} دج</span></div>");
        if (model.Discount > 0)
            sb.AppendLine($"<div style='display:flex;justify-content:space-between'><span>التخفيض:</span><span>-{model.Discount:N2} دج</span></div>");
        sb.AppendLine($"<div class='total-row' style='display:flex;justify-content:space-between;margin:4px 0'><span>الصافي للدفع:</span><span>{model.Total:N2} دج</span></div>");
        sb.AppendLine("<div class='line'></div>");
        sb.AppendLine($"<div style='display:flex;justify-content:space-between'><span>المبلغ المدفوع:</span><span>{model.PaidAmount:N2} دج</span></div>");
        if (model.ChangeAmount > 0)
            sb.AppendLine($"<div style='display:flex;justify-content:space-between;font-weight:bold'><span>المتبقي للزبون (الصرف):</span><span>{model.ChangeAmount:N2} دج</span></div>");
        if (model.RemainingDebt > 0)
            sb.AppendLine($"<div style='display:flex;justify-content:space-between;font-weight:bold;color:#b91c1c'><span>المتبقي كدين (كريدي):</span><span>{model.RemainingDebt:N2} دج</span></div>");

        sb.AppendLine("<div class='double-line'></div>");
        sb.AppendLine($"<div class='center' style='margin-top:10px'>{model.FooterMessage}</div>");
        sb.AppendLine("<div class='center' style='font-size:11px;color:#555;margin-top:4px'>تاجر برو - Taajer PRO</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public string GenerateHtmlA4Invoice(PrintInvoiceModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html dir='rtl' lang='ar'><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; width: 210mm; margin: 20mm auto; color: #1e293b; }");
        sb.AppendLine(".header-table { width: 100%; margin-bottom: 24px; border-bottom: 2px solid #1e3a8a; padding-bottom: 12px; }");
        sb.AppendLine(".store-name { font-size: 24px; font-weight: bold; color: #1e3a8a; }");
        sb.AppendLine(".invoice-title { font-size: 26px; font-weight: bold; text-align: left; color: #0d9488; }");
        sb.AppendLine(".meta-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; margin-bottom: 20px; }");
        sb.AppendLine("table.items { width: 100%; border-collapse: collapse; margin-bottom: 20px; }");
        sb.AppendLine("table.items th { background: #1e3a8a; color: white; padding: 10px; text-align: right; }");
        sb.AppendLine("table.items td { border: 1px solid #e2e8f0; padding: 8px 10px; }");
        sb.AppendLine("table.items tr:nth-child(even) { background: #f8fafc; }");
        sb.AppendLine(".totals-table { width: 320px; margin-right: auto; margin-bottom: 30px; border-collapse: collapse; }");
        sb.AppendLine(".totals-table td { padding: 8px; border-bottom: 1px solid #e2e8f0; }");
        sb.AppendLine(".totals-table .grand-total { font-size: 18px; font-weight: bold; background: #e0f2fe; color: #0369a1; }");
        sb.AppendLine(".signatures { display: flex; justify-content: space-between; margin-top: 50px; padding: 0 40px; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<table class='header-table'><tr>");
        sb.AppendLine($"<td><div class='store-name'>{model.StoreName}</div><div>{model.StoreAddress}</div><div>الهاتف: {model.StorePhone}</div><div>NIF: {model.TaxId}</div></td>");
        sb.AppendLine($"<td class='invoice-title'>فاتورة بيع تجارية<div style='font-size:14px;color:#64748b;margin-top:4px'>FACTURE N°: {model.InvoiceNumber}</div></td>");
        sb.AppendLine("</tr></table>");

        sb.AppendLine("<table style='width:100%;margin-bottom:20px'><tr>");
        sb.AppendLine($"<td style='width:50%'><div class='meta-box'><strong>بيانات الفاتورة:</strong><br/>التاريخ: {model.Date:yyyy-MM-dd}<br/>الكاشير المسؤول: {model.CashierName}<br/>طريقة التسديد: {model.PaymentMethod}</div></td>");
        sb.AppendLine($"<td style='width:50%'><div class='meta-box'><strong>الزبون / المشتري:</strong><br/>الاسم: {(string.IsNullOrEmpty(model.CustomerName) ? "زبون عادي" : model.CustomerName)}<br/>حالة الدفع: {(model.RemainingDebt == 0 ? "مسددة بالكامل" : "آجلة / جزئية")}</div></td>");
        sb.AppendLine("</tr></table>");

        sb.AppendLine("<table class='items'><thead><tr><th>#</th><th>التعيين / المنتج</th><th>الكمية</th><th>السعر الإفرادي (دج)</th><th>المجموع (دج)</th></tr></thead><tbody>");
        int count = 1;
        foreach (var item in model.Items)
        {
            sb.AppendLine($"<tr><td>{count++}</td><td><strong>{item.Name}</strong></td><td>{item.Quantity:N0}</td><td>{item.UnitPrice:N2}</td><td><strong>{item.Total:N2}</strong></td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<table class='totals-table'>");
        sb.AppendLine($"<tr><td>المجموع الجزئي (HT):</td><td><strong>{model.Subtotal:N2} دج</strong></td></tr>");
        if (model.Discount > 0) sb.AppendLine($"<tr><td>التخفيض الممنوح:</td><td><strong>-{model.Discount:N2} دج</strong></td></tr>");
        sb.AppendLine($"<tr class='grand-total'><td>المجموع الصافي للدفع:</td><td><strong>{model.Total:N2} دج</strong></td></tr>");
        sb.AppendLine($"<tr><td>المبلغ المسدد:</td><td><strong>{model.PaidAmount:N2} دج</strong></td></tr>");
        if (model.RemainingDebt > 0) sb.AppendLine($"<tr style='color:#dc2626'><td>المتبقي كدين (كريدي):</td><td><strong>{model.RemainingDebt:N2} دج</strong></td></tr>");
        sb.AppendLine("</table>");

        sb.AppendLine("<div class='signatures'><div>توقيع وختم المؤسسة:</div><div>توقيع واستلام الزبون:</div></div>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public void PrintToThermalPrinter(string printerName, PrintInvoiceModel model, bool openDrawer = true)
    {
        // When printerName is provided, send raw ESC/POS bytes to Windows printer spooler
        // Or if printerName is empty/default, use default Windows Thermal printer
        var bytes = GenerateEscPosTicket(model, openDrawer, cutPaper: true);
        RawPrinterHelper.SendBytesToPrinter(printerName, bytes);
    }

    private static void WriteText(BinaryWriter bw, string text)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(1256); // Windows-1256 Arabic
            bw.Write(encoding.GetBytes(text));
        }
        catch
        {
            bw.Write(Encoding.UTF8.GetBytes(text));
        }
    }
}

// Windows Spooler Raw P/Invoke Helper for Direct ESC/POS printing
public static class RawPrinterHelper
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
    public class DOCINFOA
    {
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string? pDocName;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string? pOutputFile;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)]
        public string? pDataType;
    }

    [System.Runtime.InteropServices.DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi, ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
    public static extern bool OpenPrinter([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [System.Runtime.InteropServices.DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi, ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [System.Runtime.InteropServices.In, System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStruct)] DOCINFOA di);

    [System.Runtime.InteropServices.DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [System.Runtime.InteropServices.DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
    public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public static bool SendBytesToPrinter(string szPrinterName, byte[] bytes)
    {
        if (string.IsNullOrEmpty(szPrinterName)) return false;

        var di = new DOCINFOA { pDocName = "Taajer_PRO_Receipt", pDataType = "RAW" };
        if (OpenPrinter(szPrinterName.Normalize(), out IntPtr hPrinter, IntPtr.Zero))
        {
            if (StartDocPrinter(hPrinter, 1, di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    IntPtr pUnmanagedBytes = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(bytes.Length);
                    System.Runtime.InteropServices.Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
                    WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int _);
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pUnmanagedBytes);
                    EndPagePrinter(hPrinter);
                }
                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
            return true;
        }
        return false;
    }
}
