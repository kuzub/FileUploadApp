using FileUploadApp.ViewModels;

namespace FileUploadApp.Views;

public partial class UploadResultsPage : ContentPage
{
    public UploadResultsPage(UploadResultsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}