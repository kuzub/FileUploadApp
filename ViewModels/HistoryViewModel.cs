using FileUploadApp.Models.DB;
using FileUploadApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileUploadApp.ViewModels;

public class HistoryViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly INotificationService _notificationService;
    private bool _isBusy;
    private bool _isRefreshing;
    private bool _hasResults;
    private string _emptyMessage = "No upload history available.";

    public HistoryViewModel(IDatabaseService databaseService, INotificationService notificationService)
    {
        _databaseService = databaseService;
        _notificationService = notificationService;
        UploadResults = new ObservableCollection<UploadResult>();

        LoadHistoryCommand = new Command(async () => await LoadHistoryAsync());
        DeleteItemCommand = new Command<UploadResult>(async (item) => await DeleteItemAsync(item));
        ClearAllCommand = new Command(async () => await ClearAllAsync(), () => HasResults);
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    public ObservableCollection<UploadResult> UploadResults { get; }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing != value)
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasResults
    {
        get => _hasResults;
        set
        {
            if (_hasResults != value)
            {
                _hasResults = value;
                OnPropertyChanged();
                ((Command)ClearAllCommand).ChangeCanExecute();
            }
        }
    }

    public string EmptyMessage
    {
        get => _emptyMessage;
        set
        {
            if (_emptyMessage != value)
            {
                _emptyMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand LoadHistoryCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand RefreshCommand { get; }

    // ✅ Core logic extracted to private method
    private async Task LoadDataAsync()
    {
        await _databaseService.InitializeAsync();
        var results = await _databaseService.GetAllUploadResultsAsync();

        UploadResults.Clear();
        foreach (var result in results)
        {
            UploadResults.Add(result);
        }

        HasResults = UploadResults.Any();
    }

    // ✅ LoadHistoryAsync uses IsBusy
    public async Task LoadHistoryAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowAlertAsync(
                "Error", 
                $"Failed to load history: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ✅ RefreshAsync uses IsRefreshing (managed by RefreshView)
    private async Task RefreshAsync()
    {
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowAlertAsync(
                "Error", 
                $"Failed to refresh history: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task DeleteItemAsync(UploadResult? item)
    {
        if (item == null || IsBusy)
            return;

        var confirm = await _notificationService.ShowConfirmationAsync(
            "Confirm Delete",
            $"Are you sure you want to delete this result?\n\nReceipt: {item.ReceiptNo}\nFile: {item.FileName}",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;

            await _databaseService.DeleteUploadResultAsync(item.Id);
            UploadResults.Remove(item);
            HasResults = UploadResults.Any();

            await _notificationService.ShowAlertAsync(
                "Success",
                "Item deleted successfully.");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowAlertAsync(
                "Error",
                $"Failed to delete item: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClearAllAsync()
    {
        if (IsBusy || !HasResults)
            return;

        var confirm = await _notificationService.ShowConfirmationAsync(
            "Confirm Clear All",
            $"Are you sure you want to delete all {UploadResults.Count} upload results?\n\nThis action cannot be undone.",
            "Delete All",
            "Cancel");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;

            await _databaseService.DeleteAllUploadResultsAsync();
            UploadResults.Clear();
            HasResults = false;

            await _notificationService.ShowAlertAsync(
                "Success",
                "All history cleared successfully.");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowAlertAsync(
                "Error",
                $"Failed to clear history: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}