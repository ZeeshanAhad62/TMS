using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class IncidentListItemDto
{
    public int Id { get; set; }
    public string IncidentCode { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Location { get; set; }
    public IncidentType IncidentType { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public decimal? EstimatedRepairCost { get; set; }
    public int ClaimCount { get; set; }
    public int PhotoCount { get; set; }
    public decimal ClaimsOutstanding { get; set; }
}

public class IncidentDetailDto : IncidentUpsertDto
{
    public int Id { get; set; }
    public string IncidentCode { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? TripCode { get; set; }

    // Aggregated across this incident's claims.
    public decimal TotalClaimed { get; set; }
    public decimal TotalApproved { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal ClaimsOutstanding { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<InsuranceClaimDto> Claims { get; set; } = new();
    public List<IncidentPhotoDto> Photos { get; set; } = new();
}

public class IncidentUpsertDto
{
    [Required]
    public int VehicleId { get; set; }

    public int? DriverId { get; set; }
    public int? TripId { get; set; }

    [Required]
    public DateTime OccurredAt { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }
    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    public IncidentType IncidentType { get; set; } = IncidentType.Collision;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Minor;
    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;

    public string? Description { get; set; }
    public bool ThirdPartyInvolved { get; set; }
    public string? ThirdPartyDetails { get; set; }

    [MaxLength(100)]
    public string? PoliceReportNumber { get; set; }

    [Range(0, 999999999)]
    public decimal? EstimatedRepairCost { get; set; }

    public string? Notes { get; set; }
}

public class IncidentPhotoDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class InsuranceClaimDto
{
    public int Id { get; set; }
    public string ClaimCode { get; set; } = string.Empty;
    public int IncidentId { get; set; }
    public string? InsurerName { get; set; }
    public string? PolicyNumber { get; set; }
    public string? ClaimNumber { get; set; }
    public DateOnly ClaimDate { get; set; }
    public decimal AmountClaimed { get; set; }
    public decimal? AmountApproved { get; set; }
    public decimal? AmountReceived { get; set; }
    public decimal? Deductible { get; set; }
    public decimal Outstanding { get; set; }
    public InsuranceClaimStatus Status { get; set; }
    public DateOnly? SettledDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class InsuranceClaimUpsertDto
{
    [MaxLength(200)]
    public string? InsurerName { get; set; }
    [MaxLength(100)]
    public string? PolicyNumber { get; set; }
    [MaxLength(100)]
    public string? ClaimNumber { get; set; }

    [Required]
    public DateOnly ClaimDate { get; set; }

    [Range(0, 999999999)]
    public decimal AmountClaimed { get; set; }
    [Range(0, 999999999)]
    public decimal? AmountApproved { get; set; }
    [Range(0, 999999999)]
    public decimal? AmountReceived { get; set; }
    [Range(0, 999999999)]
    public decimal? Deductible { get; set; }

    public InsuranceClaimStatus Status { get; set; } = InsuranceClaimStatus.Draft;
    public DateOnly? SettledDate { get; set; }
    public string? Notes { get; set; }
}
