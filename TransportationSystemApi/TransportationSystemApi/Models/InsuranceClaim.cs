namespace TransportationSystemApi.Models;

// A claim raised against an insurer for an incident. An incident can carry
// several (own-damage, third-party, ...). NetReceivable is derived at read
// time (IncidentMapper), not stored.
public class InsuranceClaim
{
    public int Id { get; set; }

    // System-generated identity: CLM-00001
    public string ClaimCode { get; set; } = string.Empty;

    public int IncidentId { get; set; }
    public Incident? Incident { get; set; }

    public string? InsurerName { get; set; }
    public string? PolicyNumber { get; set; }
    public string? ClaimNumber { get; set; } // the insurer's own reference
    public DateOnly ClaimDate { get; set; }

    public decimal AmountClaimed { get; set; }
    public decimal? AmountApproved { get; set; }
    public decimal? AmountReceived { get; set; }
    public decimal? Deductible { get; set; }

    public InsuranceClaimStatus Status { get; set; } = InsuranceClaimStatus.Draft;
    public DateOnly? SettledDate { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
