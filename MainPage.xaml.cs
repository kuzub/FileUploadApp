using FileUploadApp.Services;
using FileUploadApp.ViewModels;

namespace FileUploadApp;

public partial class MainPage : ContentPage
{
    private readonly IAuthenticationService _authenticationService;
    private readonly MainViewModel _viewModel;

    public MainPage(IAuthenticationService authenticationService, MainViewModel viewModel)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

//    protected override async void OnAppearing()
//    {
//        base.OnAppearing();

//        // Check if user is authenticated
//        var isAuthenticated = await _authenticationService.IsUserAuthenticatedAsync();

//        if (!isAuthenticated)
//        {
//            // Navigate to login page if token is expired or not found
//            await Shell.Current.GoToAsync("//LoginPage");
//        }
//    }
//}
}