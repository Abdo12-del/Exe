using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace SalesManagement.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var loginWindow = App.ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
        Close();
    }
}
