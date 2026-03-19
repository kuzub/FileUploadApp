using FileUploadApp.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FileUploadApp.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService _authenticationService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
    }

    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
                ((Command)LoginCommand).ChangeCanExecute();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand LoginCommand { get; }

    private async Task LoginAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _authenticationService.AuthenticateUserAsync(Username, Password);

            if (result != null)
            {
                // Clear password for security
                Password = string.Empty;
                
                // Navigate back to main page with parameter indicating successful login
                await Shell.Current.GoToAsync("//MainPage?fromLogin=true");
            }
            else
            {
                ErrorMessage = "Invalid credentials. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}