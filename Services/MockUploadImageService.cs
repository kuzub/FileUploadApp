using FileUploadApp.Models;

namespace FileUploadApp.Services;

public class MockUploadImageService : IUploadImage
{
    private readonly Random _random = new();

    public async Task<UploadResult?> UploadImageAsync(byte[] imageData, string fileName, 
        string accessToken, CancellationToken cancellationToken)
    {
        // Validate inputs
        if (imageData == null || imageData.Length == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            // Simulate network delay
            await Task.Delay(_random.Next(1000, 3000), cancellationToken);

            // Check if cancellation was requested
            cancellationToken.ThrowIfCancellationRequested();

            // Simulate 90% success rate
            bool simulatedSuccess = _random.Next(1, 11) <= 9;

            if (simulatedSuccess)
            {
                // Generate mock receipt number and random price
                string receiptNo = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                decimal price = Math.Round((decimal)(_random.NextDouble() * 1000 + 10), 2);

                return new UploadResult
                {
                    ReceiptNo = receiptNo,
                    Price = price
                };
            }
            else
            {
                // Simulate failure by returning null
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}