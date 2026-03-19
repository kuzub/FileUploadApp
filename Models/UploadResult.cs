namespace FileUploadApp.Models;

public record UploadResult
{
    public string? ReceiptNo { get; init; }
    public decimal? Price { get; set; }
}