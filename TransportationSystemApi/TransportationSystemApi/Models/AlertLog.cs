using TransportationSystemApi.Dtos;

namespace TransportationSystemApi.Models;

// One row per alert email actually sent. Also the dedupe record: the
// (EntityType, EntityId, DocumentType, ExpiryDate, Severity) tuple is
// unique, so the daily scan only re-alerts when severity escalates or
// the document is renewed (new ExpiryDate).
public class AlertLog
{
    public int Id { get; set; }
    public ComplianceEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public ComplianceSeverity Severity { get; set; }
    public string RecipientEmails { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
