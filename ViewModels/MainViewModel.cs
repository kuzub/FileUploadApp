using FileUploadApp.Models;
using FileUploadApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileUploadApp.ViewModels;

public class MainViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUploadImage _uploadImageService;
    private readonly IDatabaseService _databaseService;
    private readonly INotificationService _notificationService;
    private bool _isBusy;
    private string _selectedImagesText = string.Empty;
    private bool _hasSelectedImages;

    public MainViewModel(
        IAuthenticationService authenticationService, 
        IUploadImage uploadImageService,
        IDatabaseService databaseService,
        INotificationService notificationService)
    {
        _authenticationService = authenticationService;
        _uploadImageService = uploadImageService;
        _databaseService = databaseService;
        _notificationService = notificationService;
        SelectedImages = new ObservableCollection<ImageFile>();
        PickImagesCommand = new Command(async () => await PickImagesAsync(), () => !IsBusy);
        UploadCommand = new Command(async () => await UploadImagesAsync(), () => !IsBusy && HasSelectedImages);
        DeleteImageCommand = new Command<ImageFile>(async (image) => await DeleteImageAsync(image));
        ShowHistoryCommand = new Command(async () => await ShowHistoryAsync());
        
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
    public ICommand ShowHistoryCommand { get; }

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
            await _notificationService.ShowAlertAsync("Error", $"Failed to pick images: {ex.Message}");
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
                await _notificationService.ShowAlertAsync(
                    "Authentication Required", 
                    "Your session has expired. Please log in again.");
                
                // Navigate to login page (images will be preserved in SelectedImages collection)
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            // Get access token for upload
            var tokenResult = await _authenticationService.RetrieveTokenAsync();
            var accessToken = tokenResult?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                await _notificationService.ShowAlertAsync(
                    "Error", 
                    "Failed to retrieve access token.");
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5 minute timeout for all uploads

            // Create upload tasks for all images
            var uploadTasks = SelectedImages.ToList().Select(async imageFile =>
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

                        // Return success result
                        return new UploadResultItem
                        {
                            FileName = imageFile.Name,
                            IsSuccess = true,
                            ReceiptNo = uploadResult.ReceiptNo,
                            Price = uploadResult.Price
                        };
                    }
                    else
                    {
                        // Return failure result
                        return new UploadResultItem
                        {
                            FileName = imageFile.Name,
                            IsSuccess = false,
                            ErrorMessage = "Upload failed - no response from server"
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    return new UploadResultItem
                    {
                        FileName = imageFile.Name,
                        IsSuccess = false,
                        ErrorMessage = "Upload cancelled due to timeout"
                    };
                }
                catch (Exception ex)
                {
                    // Return failure result with error
                    return new UploadResultItem
                    {
                        FileName = imageFile.Name,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                }
            });

            // Execute all uploads in parallel
            var uploadResultItems = await Task.WhenAll(uploadTasks);

            // Check if any upload was cancelled
            if (uploadResultItems.Any(r => !r.IsSuccess && r.ErrorMessage.Contains("timeout")))
            {
                await _notificationService.ShowAlertAsync(
                    "Upload Cancelled", 
                    "Upload operation was cancelled due to timeout.");
            }

            // Clear selected images after upload attempt
            SelectedImages.Clear();
            UpdateSelectedImagesText();
            HasSelectedImages = false;

            // Navigate to results page with upload results
            var navigationParameter = new Dictionary<string, object>
            {
                { "results", uploadResultItems.ToList() }
            };

            await Shell.Current.GoToAsync("UploadResultsPage", navigationParameter);
        }
        catch (Exception ex)
        {
            await _notificationService.ShowAlertAsync("Error", $"Upload failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteImageAsync(ImageFile imageFile)
    {
        if (imageFile is null)
            return;

        bool confirmed = await _notificationService.ShowConfirmationAsync(
            "Confirm Delete",
            $"Are you sure you want to remove '{imageFile.Name}' from the selection?",
            "Delete",
            "Cancel");

        if (confirmed)
        {
            SelectedImages.Remove(imageFile);
            UpdateSelectedImagesText();
            HasSelectedImages = SelectedImages.Any();
        }
    }

    private async Task ShowHistoryAsync()
    {
        await Shell.Current.GoToAsync("//HistoryPage");
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
}