namespace TransportationSystemApi.Models;

// An accident / theft / damage event on a vehicle. TripId is a soft link only
// (no FK); DriverId is nullable (ON DELETE SET NULL).
public class Incident
{
    public int Id { get; set; }

    // System-generated identity: INC-00001
    public string IncidentCode { get; set; } = string.Empty;

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }

    public int? TripId { get; set; }

    public DateTime OccurredAt { get; set; }
    public string? Location { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public IncidentType IncidentType { get; set; } = IncidentType.Collision;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Minor;
    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;

    public string? Description { get; set; }
    public bool ThirdPartyInvolved { get; set; }
    public string? ThirdPartyDetails { get; set; }
    public string? PoliceReportNumber { get; set; }
    public decimal? EstimatedRepairCost { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<InsuranceClaim> Claims { get; set; } = new();
    public List<IncidentPhoto> Photos { get; set; } = new();
}
