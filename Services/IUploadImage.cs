using FileUploadApp.Models;

namespace FileUploadApp.Services;

public interface IUploadImage
{
    Task<UploadResult?> UploadImageAsync(byte[] imageData, string fileName, 
        string accessToken, CancellationToken cancellationToken);
}