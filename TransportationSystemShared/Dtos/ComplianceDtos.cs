namespace TransportationSystemApi.Dtos;

public enum ComplianceEntityType
{
    Vehicle,
    Driver
}

// Only "needs attention" bands are surfaced; anything further out than the
// scan window is not returned.
public enum ComplianceSeverity
{
    Expired,   // past the expiry date
    Critical,  // 0-7 days left
    Warning,   // 8-30 days left
    Upcoming   // 31-60 days left (or up to the requested window)
}

public class ComplianceItemDto
{
    public ComplianceEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }   // negative once expired
    public ComplianceSeverity Severity { get; set; }
}

public class ComplianceSummaryDto
{
    public int Expired { get; set; }
    public int Critical { get; set; }
    public int Warning { get; set; }
    public int Upcoming { get; set; }
    public int Total { get; set; }
    public int VehicleItems { get; set; }
    public int DriverItems { get; set; }
}
