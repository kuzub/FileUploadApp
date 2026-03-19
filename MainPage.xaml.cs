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
}