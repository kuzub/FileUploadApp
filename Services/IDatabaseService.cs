using FileUploadApp.Models.DB;

namespace FileUploadApp.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<int> SaveUploadResultAsync(UploadResult uploadResult);
    Task<List<UploadResult>> GetAllUploadResultsAsync();
    Task<UploadResult?> GetUploadResultByReceiptNoAsync(string receiptNo);
    Task<int> DeleteUploadResultAsync(int id);
    Task<int> DeleteAllUploadResultsAsync();
}