namespace TransportationSystemApi.Models;

// One data-change record, written by AuditSaveChangesInterceptor. Append-only;
// no navigation to User so the row outlives the account.
public class AuditLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int? UserId { get; set; }
    public string? UserName { get; set; }

    public string Action { get; set; } = string.Empty; // Insert / Update / Delete
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    // Update: { "Field": { "old": .., "new": .. }, ... }
    // Insert / Delete: { "Field": value, ... } snapshot.
    public string? ChangesJson { get; set; }
}
