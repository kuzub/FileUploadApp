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
            var uploadResultItems = new List<UploadResultItem>();

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

                        // Add success result
                        uploadResultItems.Add(new UploadResultItem
                        {
                            FileName = imageFile.Name,
                            IsSuccess = true,
                            ReceiptNo = uploadResult.ReceiptNo,
                            Price = uploadResult.Price
                        });
                    }
                    else
                    {
                        // Add failure result
                        uploadResultItems.Add(new UploadResultItem
                        {
                            FileName = imageFile.Name,
                            IsSuccess = false,
                            ErrorMessage = "Upload failed - no response from server"
                        });
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
                    // Add failure result with error
                    uploadResultItems.Add(new UploadResultItem
                    {
                        FileName = imageFile.Name,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Clear selected images after upload attempt
            SelectedImages.Clear();
            UpdateSelectedImagesText();
            HasSelectedImages = false;

            // Navigate to results page with upload results
            var navigationParameter = new Dictionary<string, object>
            {
                { "results", uploadResultItems }
            };

            await Shell.Current.GoToAsync("UploadResultsPage", navigationParameter);
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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}