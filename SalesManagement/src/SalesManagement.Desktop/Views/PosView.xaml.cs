using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SalesManagement.Desktop.ViewModels;

namespace SalesManagement.Desktop.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        FocusBarcodeBox();
    }

    public void FocusBarcodeBox()
    {
        TxtBarcode.Focus();
        TxtBarcode.SelectAll();
    }

    private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is PosViewModel vm)
            {
                vm.ProcessBarcodeCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    private void UserControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not PosViewModel vm) return;

        switch (e.Key)
        {
            case Key.F2:
                FocusBarcodeBox();
                e.Handled = true;
                break;
            case Key.F4:
                vm.ParkCartCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F6:
                vm.RecallCartCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F12:
                vm.CheckoutAndPrintCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                vm.ClearCartCommand.Execute(null);
                FocusBarcodeBox();
                e.Handled = true;
                break;
            case Key.Add:
            case Key.OemPlus:
                vm.IncreaseQuantityCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Subtract:
            case Key.OemMinus:
                vm.DecreaseQuantityCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete:
                vm.RemoveSelectedCartItemCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
