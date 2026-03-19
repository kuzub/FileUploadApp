using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileUploadApp.Models.DB;
using FileUploadApp.Services;

namespace FileUploadApp.ViewModels;

public class HistoryViewModel : INotifyPropertyChanged
{
    private readonly IDatabaseService _databaseService;
    private bool _isBusy;
    private bool _hasResults;
    private string _emptyMessage = "No upload history available.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public HistoryViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
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

    public async Task LoadHistoryAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            await _databaseService.InitializeAsync();
            var results = await _databaseService.GetAllUploadResultsAsync();

            UploadResults.Clear();
            foreach (var result in results)
            {
                UploadResults.Add(result);
            }

            HasResults = UploadResults.Any();
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert(
                "Error", 
                $"Failed to load history: {ex.Message}", 
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteItemAsync(UploadResult? item)
    {
        if (item == null || IsBusy)
            return;

        var confirm = await Application.Current?.MainPage?.DisplayAlert(
            "Confirm Delete",
            $"Are you sure you want to delete this result?\n\nReceipt: {item.ReceiptNo}\nFile: {item.FileName}",
            "Delete",
            "Cancel");

        if (confirm != true)
            return;

        try
        {
            IsBusy = true;

            await _databaseService.DeleteUploadResultAsync(item.Id);
            UploadResults.Remove(item);
            HasResults = UploadResults.Any();

            await Application.Current?.MainPage?.DisplayAlert(
                "Success",
                "Item deleted successfully.",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert(
                "Error",
                $"Failed to delete item: {ex.Message}",
                "OK");
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

        var confirm = await Application.Current?.MainPage?.DisplayAlert(
            "Confirm Clear All",
            $"Are you sure you want to delete all {UploadResults.Count} upload results?\n\nThis action cannot be undone.",
            "Delete All",
            "Cancel");

        if (confirm != true)
            return;

        try
        {
            IsBusy = true;

            await _databaseService.DeleteAllUploadResultsAsync();
            UploadResults.Clear();
            HasResults = false;

            await Application.Current?.MainPage?.DisplayAlert(
                "Success",
                "All history cleared successfully.",
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current?.MainPage?.DisplayAlert(
                "Error",
                $"Failed to clear history: {ex.Message}",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        await LoadHistoryAsync();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}