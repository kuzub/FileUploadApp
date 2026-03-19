namespace FileUploadApp.Models;

public record TokenResult
{
    public string? AccessToken { get; init; }
    public DateTime Expiration { get; init; }
}
