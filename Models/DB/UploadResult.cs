using SQLite;

namespace FileUploadApp.Models.DB;

[Table("UploadResults")]
public class UploadResult
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string? ReceiptNo { get; set; }

    public decimal? Price { get; set; }

    public string? FileName { get; set; }

    public DateTime UploadedAt { get; set; }
}