namespace TransportationSystemApi.Models;

// A scene / damage photo for an incident. The file lives on disk under
// wwwroot/uploads/incidents/{incidentId}/ (see IncidentsController).
public class IncidentPhoto
{
    public int Id { get; set; }

    public int IncidentId { get; set; }
    public Incident? Incident { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
