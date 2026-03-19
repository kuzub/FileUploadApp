using FileUploadApp.Services;

namespace FileUploadApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IAuthenticationService _authenticationService;
        int count = 0;

        public MainPage(IAuthenticationService authenticationService)
        {
            InitializeComponent();
            _authenticationService = authenticationService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Check if user is authenticated
            var isAuthenticated = await _authenticationService.IsUserAuthenticatedAsync();

            if (!isAuthenticated)
            {
                // Navigate to login page if token is expired or not found
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
