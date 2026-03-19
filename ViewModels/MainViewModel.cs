using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileUploadApp.Models;
using FileUploadApp.Services;

namespace FileUploadApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUploadImage _uploadImageService;
    private bool _isBusy;
    private string _selectedImagesText = string.Empty;
    private bool _hasSelectedImages;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(IAuthenticationService authenticationService, IUploadImage uploadImageService)
    {
        _authenticationService = authenticationService;
        _uploadImageService = uploadImageService;
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Check if returning from login
        if (query.ContainsKey("fromLogin") && query["fromLogin"].ToString() == "true")
        {
            // Automatically retry upload after successful login
            if (HasSelectedImages)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(500); // Small delay to ensure navigation is complete
                    await UploadImagesAsync();
                });
            }
        }
    }

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

            // Validate token before uploading
            var isAuthenticated = await _authenticationService.IsUserAuthenticatedAsync();
            
            if (!isAuthenticated)
            {
                await Application.Current?.MainPage?.DisplayAlert(
                    "Authentication Required", 
                    "Your session has expired. Please log in again.", 
                    "OK");
                
                // Navigate to login page (images will be preserved in SelectedImages collection)
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            // Get access token for upload
            var tokenResult = await _authenticationService.RetrieveTokenAsync();
            var accessToken = tokenResult?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                await Application.Current?.MainPage?.DisplayAlert(
                    "Error", 
                    "Failed to retrieve access token.", 
                    "OK");
                return;
            }

            // Upload images
            var successCount = 0;
            var failedImages = new List<string>();
            var uploadResults = new List<string>();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5 minute timeout for all uploads

            foreach (var imageFile in SelectedImages.ToList())
            {
                try
                {
                    // Read image file as byte array
                    byte[] imageData = await File.ReadAllBytesAsync(imageFile.Path, cts.Token);

                    // Upload image using the service
                    var result = await _uploadImageService.UploadImageAsync(
                        imageData, 
                        imageFile.Name, 
                        accessToken, 
                        cts.Token);

                    if (result != null && !string.IsNullOrWhiteSpace(result.ReceiptNo))
                    {
                        successCount++;
                        uploadResults.Add($"✓ {imageFile.Name}\n  Receipt: {result.ReceiptNo}\n  Price: ${result.Price:F2}");
                    }
                    else
                    {
                        failedImages.Add(imageFile.Name);
                    }
                }
                catch (OperationCanceledException)
                {
                    await Application.Current?.MainPage?.DisplayAlert(
                        "Upload Cancelled", 
                        "Upload operation was cancelled due to timeout.", 
                        "OK");
                    return;
                }
                catch (Exception ex)
                {
                    failedImages.Add($"{imageFile.Name} ({ex.Message})");
                }
            }

            // Show results
            if (successCount > 0 && failedImages.Count == 0)
            {
                var resultsMessage = string.Join("\n\n", uploadResults);
                await Application.Current?.MainPage?.DisplayAlert(
                    "Success", 
                    $"{successCount} image(s) uploaded successfully!\n\n{resultsMessage}", 
                    "OK");
                
                // Clear selected images after successful upload
                SelectedImages.Clear();
                HasSelectedImages = false;
                SelectedImagesText = string.Empty;
            }
            else if (successCount > 0 && failedImages.Count > 0)
            {
                var resultsMessage = string.Join("\n\n", uploadResults);
                var failedMessage = string.Join("\n", failedImages.Select(f => $"✗ {f}"));
                await Application.Current?.MainPage?.DisplayAlert(
                    "Partial Success", 
                    $"{successCount} succeeded, {failedImages.Count} failed\n\n{resultsMessage}\n\nFailed:\n{failedMessage}", 
                    "OK");
                
                // Remove successfully uploaded images
                var imagesToRemove = SelectedImages
                    .Where(img => !failedImages.Any(f => f.StartsWith(img.Name)))
                    .ToList();
                
                foreach (var img in imagesToRemove)
                {
                    SelectedImages.Remove(img);
                }
                
                UpdateSelectedImagesText();
                HasSelectedImages = SelectedImages.Count > 0;
            }
            else
            {
                var failedMessage = string.Join("\n", failedImages.Select(f => $"✗ {f}"));
                await Application.Current?.MainPage?.DisplayAlert(
                    "Upload Failed", 
                    $"All uploads failed:\n\n{failedMessage}", 
                    "OK");
            }
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