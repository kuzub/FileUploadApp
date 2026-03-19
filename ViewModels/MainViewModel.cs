using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileUploadApp.Models;
using FileUploadApp.Services;
using System.Collections.ObjectModel;

namespace FileUploadApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUploadImage _uploadImageService;
    private readonly IDatabaseService _databaseService;
    private bool _isBusy;
    private string _selectedImagesText = string.Empty;
    private bool _hasSelectedImages;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(
        IAuthenticationService authenticationService, 
        IUploadImage uploadImageService,
        IDatabaseService databaseService)
    {
        _authenticationService = authenticationService;
        _uploadImageService = uploadImageService;
        _databaseService = databaseService;
        SelectedImages = new ObservableCollection<ImageFile>();
        PickImagesCommand = new Command(async () => await PickImagesAsync(), () => !IsBusy);
        UploadCommand = new Command(async () => await UploadImagesAsync(), () => !IsBusy && HasSelectedImages);
        DeleteImageCommand = new Command<ImageFile>((image) => DeleteImage(image));
        
        // Initialize database
        Task.Run(async () => await _databaseService.InitializeAsync());
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
                    var uploadResult = await _uploadImageService.UploadImageAsync(
                        imageData, 
                        imageFile.Name, 
                        accessToken, 
                        cts.Token);

                    if (uploadResult != null && !string.IsNullOrWhiteSpace(uploadResult.ReceiptNo))
                    {
                        // Save to SQLite database
                        var uploadResultDB = new Models.DB.UploadResult()
                        {
                            ReceiptNo = uploadResult.ReceiptNo,
                            Price = uploadResult.Price,
                            FileName = imageFile.Name,
                            UploadedAt = DateTime.UtcNow
                        };

                        await _databaseService.SaveUploadResultAsync(uploadResultDB);

                        successCount++;
                        uploadResults.Add($"✓ {imageFile.Name}\n  Receipt: {uploadResult.ReceiptNo}\n  Price: ${uploadResult.Price:F2}");
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
            var message = $"Successfully uploaded: {successCount}/{SelectedImages.Count}";
            if (uploadResults.Any())
            {
                message += $"\n\n{string.Join("\n\n", uploadResults)}";
            }
            if (failedImages.Any())
            {
                message += $"\n\nFailed:\n{string.Join("\n", failedImages)}";
            }

            await Application.Current?.MainPage?.DisplayAlert(
                "Upload Complete", 
                message, 
                "OK");

            // Clear selected images after successful upload
            if (successCount > 0)
            {
                SelectedImages.Clear();
                UpdateSelectedImagesText();
                HasSelectedImages = false;
            }
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert("Error", $"Upload failed: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void DeleteImage(ImageFile image)
    {
        if (image != null)
        {
            SelectedImages.Remove(image);
            UpdateSelectedImagesText();
            HasSelectedImages = SelectedImages.Any();
        }
    }

    private void UpdateSelectedImagesText()
    {
        if (!SelectedImages.Any())
        {
            SelectedImagesText = string.Empty;
        }
        else if (SelectedImages.Count == 1)
        {
            SelectedImagesText = $"1 image selected";
        }
        else
        {
            SelectedImagesText = $"{SelectedImages.Count} images selected";
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}