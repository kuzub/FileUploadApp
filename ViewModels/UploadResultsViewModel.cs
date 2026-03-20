using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileUploadApp.ViewModels;

public class UploadResultsViewModel : BaseViewModel, IQueryAttributable
{
    private bool _isBusy;
    private int _totalUploaded;
    private int _totalFailed;

    public UploadResultsViewModel()
    {
        UploadResults = new ObservableCollection<UploadResultItem>();
        CloseCommand = new Command(async () => await CloseAsync());
    }

    public ObservableCollection<UploadResultItem> UploadResults { get; }

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

    public int TotalUploaded
    {
        get => _totalUploaded;
        set
        {
            if (_totalUploaded != value)
            {
                _totalUploaded = value;
                OnPropertyChanged();
            }
        }
    }

    public int TotalFailed
    {
        get => _totalFailed;
        set
        {
            if (_totalFailed != value)
            {
                _totalFailed = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand CloseCommand { get; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("results"))
        {
            var results = query["results"] as List<UploadResultItem>;
            if (results != null)
            {
                UploadResults.Clear();
                foreach (var result in results)
                {
                    UploadResults.Add(result);
                }

                TotalUploaded = results.Count(r => r.IsSuccess);
                TotalFailed = results.Count(r => !r.IsSuccess);
            }
        }
    }

    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}

public class UploadResultItem
{
    public string? FileName { get; set; }
    public bool IsSuccess { get; set; }
    public string? ReceiptNo { get; set; }
    public decimal? Price { get; set; }
    public string? ErrorMessage { get; set; }

    public string StatusIcon => IsSuccess ? "✓" : "✗";
    public string StatusColor => IsSuccess ? "#4CAF50" : "#F44336";
    public string DisplayText => IsSuccess 
        ? $"{FileName}\nReceipt: {ReceiptNo}\nPrice: ${Price:F2}" 
        : $"{FileName}\n{ErrorMessage}";
}