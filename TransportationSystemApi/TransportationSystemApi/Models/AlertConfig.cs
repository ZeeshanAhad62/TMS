using TransportationSystemApi.Dtos;

namespace TransportationSystemApi.Models;

// Which expiring documents the daily compliance job should email about.
// A null EntityType/DocumentType is a wildcard (matches everything).
public class AlertConfig
{
    public int Id { get; set; }
    public ComplianceEntityType? EntityType { get; set; }
    public string? DocumentType { get; set; }
    public int ThresholdDays { get; set; }
    public string RecipientEmails { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
