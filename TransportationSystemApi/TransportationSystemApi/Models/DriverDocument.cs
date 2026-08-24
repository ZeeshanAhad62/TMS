namespace TransportationSystemApi.Models;

public class DriverDocument
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public Driver? Driver { get; set; }

    public DriverDocumentCategory Category { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
