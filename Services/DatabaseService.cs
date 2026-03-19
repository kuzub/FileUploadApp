using SQLite;
using FileUploadApp.Models.DB;

namespace FileUploadApp.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _database;

    public async Task InitializeAsync()
    {
        if (_database != null)
            return;

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "UploadResults.db3");
        _database = new SQLiteAsyncConnection(databasePath);

        await _database.CreateTableAsync<UploadResult>();
    }

    public async Task<int> SaveUploadResultAsync(UploadResult uploadResult)
    {
        await InitializeAsync();

        if (uploadResult.Id != 0)
        {
            return await _database!.UpdateAsync(uploadResult);
        }
        else
        {
            return await _database!.InsertAsync(uploadResult);
        }
    }

    public async Task<List<UploadResult>> GetAllUploadResultsAsync()
    {
        await InitializeAsync();
        return await _database!.Table<UploadResult>()
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync();
    }

    public async Task<UploadResult?> GetUploadResultByReceiptNoAsync(string receiptNo)
    {
        await InitializeAsync();
        return await _database!.Table<UploadResult>()
            .Where(x => x.ReceiptNo == receiptNo)
            .FirstOrDefaultAsync();
    }

    public async Task<int> DeleteUploadResultAsync(int id)
    {
        await InitializeAsync();
        return await _database!.DeleteAsync<UploadResult>(id);
    }

    public async Task<int> DeleteAllUploadResultsAsync()
    {
        await InitializeAsync();
        return await _database!.DeleteAllAsync<UploadResult>();
    }
}