using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Application.DTOs;
using SalesManagement.Desktop.ViewModels;

namespace SalesManagement.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        _viewModel.LoginSucceeded += OnLoginSucceeded;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = UserPasswordBox.Password;
        }
    }

    private void OnLoginSucceeded(UserSessionDto user)
    {
        var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
        var mainVm = App.ServiceProvider.GetRequiredService<MainViewModel>();
        mainVm.SetUser(user);
        mainWindow.DataContext = mainVm;

        mainWindow.Show();
        Close();
    }
}
