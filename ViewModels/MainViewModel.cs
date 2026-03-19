using FileUploadApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FileUploadApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService _authenticationService;
    private bool _isBusy;
    private string _selectedImagesText = string.Empty;
    private bool _hasSelectedImages;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        SelectedImagePaths = new ObservableCollection<string>();
        PickImagesCommand = new Command(async () => await PickImagesAsync(), () => !IsBusy);
    }

    public ObservableCollection<string> SelectedImagePaths { get; }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
                ((Command)PickImagesCommand).ChangeCanExecute();
            }
        }
    }

    public string SelectedImagesText
    {
        get => _selectedImagesText;
        set
        {
            if (_selectedImagesText != value)
            {
                _selectedImagesText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasSelectedImages
    {
        get => _hasSelectedImages;
        set
        {
            if (_hasSelectedImages != value)
            {
                _hasSelectedImages = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand PickImagesCommand { get; }

    private async Task PickImagesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "image/*" } },
                    { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" } },
                    { DevicePlatform.iOS, new[] { "public.image" } },
                    { DevicePlatform.macOS, new[] { "public.image" } }
                });

            var options = new PickOptions
            {
                FileTypes = customFileType,
                PickerTitle = "Select images"
            };

            var results = await FilePicker.Default.PickMultipleAsync(options);

            if (results != null && results.Any())
            {
                SelectedImagePaths.Clear();

                foreach (var result in results)
                {
                    SelectedImagePaths.Add(result.FullPath);
                }

                SelectedImagesText = $"{SelectedImagePaths.Count} image(s) selected";
                HasSelectedImages = true;
            }
        }
        catch (Exception ex)
        {
            // In a real app, you might want to use a messaging service or event
            await Application.Current?.MainPage?.DisplayAlert("Error", $"Failed to pick images: {ex.Message}", "OK");
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