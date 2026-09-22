using CommunityToolkit.Mvvm.Input;
using SalesManagement.Application.DTOs;
using SalesManagement.Application.Interfaces;

namespace SalesManagement.Desktop.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private string _username = "admin";
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public IAsyncRelayCommand LoginCommand { get; }
    public event Action<UserSessionDto>? LoginSucceeded;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
    }

    private async Task ExecuteLoginAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "يرجى إدخال اسم المستخدم وكلمة المرور.";
            return;
        }

        try
        {
            IsBusy = true;
            var session = await _authService.AuthenticateAsync(Username, Password);
            if (session == null)
            {
                ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة.";
                return;
            }

            LoginSucceeded?.Invoke(session);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في تسجيل الدخول: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
