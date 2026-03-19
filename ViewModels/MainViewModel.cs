using FileUploadApp.Models;
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
        SelectedImages = new ObservableCollection<ImageFile>();
        PickImagesCommand = new Command(async () => await PickImagesAsync(), () => !IsBusy);
        UploadCommand = new Command(async () => await UploadImagesAsync(), () => !IsBusy && HasSelectedImages);
        DeleteImageCommand = new Command<ImageFile>(async (image) => await DeleteImageAsync(image));
    }

    public ObservableCollection<ImageFile> SelectedImages { get; }

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
                ((Command)UploadCommand).ChangeCanExecute();
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
                ((Command)UploadCommand).ChangeCanExecute();
            }
        }
    }

    public ICommand PickImagesCommand { get; }
    public ICommand UploadCommand { get; }
    public ICommand DeleteImageCommand { get; }

    private async Task PickImagesAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            var options = new PickOptions
            {
                PickerTitle = "Select files",
                FileTypes = FilePickerFileType.Images,
            };

            var results = await FilePicker.Default.PickMultipleAsync(options);

            if (results != null && results.Any())
            {
                SelectedImages.Clear();

                foreach (var result in results)
                {
                    var imageFile = new ImageFile(result.FileName, result.FullPath);
                    SelectedImages.Add(imageFile);
                }

                UpdateSelectedImagesText();
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

    private async Task UploadImagesAsync()
    {
        if (IsBusy || !HasSelectedImages)
            return;

        try
        {
            IsBusy = true;

            // TODO: Implement your upload logic here
            // Example: await _uploadService.UploadFilesAsync(SelectedImages);

            await Task.Delay(2000); // Simulating upload
            
            await Application.Current?.MainPage?.DisplayAlert("Success", $"{SelectedImages.Count} image(s) uploaded successfully!", "OK");
            
            // Clear selected images after successful upload
            SelectedImages.Clear();
            HasSelectedImages = false;
            SelectedImagesText = string.Empty;
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Error", $"Failed to upload images: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteImageAsync(ImageFile? imageFile)
    {
        if (imageFile == null)
            return;

        bool confirmed = await Application.Current?.MainPage?.DisplayAlert(
            "Confirm Delete",
            $"Are you sure you want to remove '{imageFile.Name}' from the selection?",
            "Delete",
            "Cancel");

        if (confirmed)
        {
            SelectedImages.Remove(imageFile);
            UpdateSelectedImagesText();
            HasSelectedImages = SelectedImages.Count > 0;
        }
    }

    private void UpdateSelectedImagesText()
    {
        SelectedImagesText = SelectedImages.Count > 0 
            ? $"{SelectedImages.Count} image(s) selected" 
            : string.Empty;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}