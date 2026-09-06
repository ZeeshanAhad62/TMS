using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class IncidentMapper
{
    // What is still expected on a claim: the approved amount (or, before an
    // assessment, the claimed amount) less whatever has been received.
    // A rejected claim is worth nothing.
    public static decimal ClaimOutstanding(InsuranceClaim c)
    {
        if (c.Status == InsuranceClaimStatus.Rejected) return 0m;
        var expected = c.AmountApproved ?? c.AmountClaimed;
        return Math.Max(0m, expected - (c.AmountReceived ?? 0m));
    }

    public static decimal ClaimsOutstanding(IEnumerable<InsuranceClaim> claims) => claims.Sum(ClaimOutstanding);

    public static IncidentListItemDto ToListItemDto(Incident i) => new()
    {
        Id = i.Id,
        IncidentCode = i.IncidentCode,
        VehicleId = i.VehicleId,
        VehicleCode = i.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = i.Vehicle?.RegistrationNumber ?? string.Empty,
        DriverName = i.Driver?.FullName,
        OccurredAt = i.OccurredAt,
        Location = i.Location,
        IncidentType = i.IncidentType,
        Severity = i.Severity,
        Status = i.Status,
        EstimatedRepairCost = i.EstimatedRepairCost,
        ClaimCount = i.Claims.Count,
        PhotoCount = i.Photos.Count,
        ClaimsOutstanding = ClaimsOutstanding(i.Claims)
    };

    public static IncidentDetailDto ToDetailDto(Incident i, string? tripCode) => new()
    {
        Id = i.Id,
        IncidentCode = i.IncidentCode,
        VehicleId = i.VehicleId,
        VehicleCode = i.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = i.Vehicle?.RegistrationNumber ?? string.Empty,
        DriverName = i.Driver?.FullName,
        DriverId = i.DriverId,
        TripId = i.TripId,
        TripCode = tripCode,
        OccurredAt = i.OccurredAt,
        Location = i.Location,
        Latitude = i.Latitude,
        Longitude = i.Longitude,
        IncidentType = i.IncidentType,
        Severity = i.Severity,
        Status = i.Status,
        Description = i.Description,
        ThirdPartyInvolved = i.ThirdPartyInvolved,
        ThirdPartyDetails = i.ThirdPartyDetails,
        PoliceReportNumber = i.PoliceReportNumber,
        EstimatedRepairCost = i.EstimatedRepairCost,
        Notes = i.Notes,
        TotalClaimed = i.Claims.Sum(c => c.AmountClaimed),
        TotalApproved = i.Claims.Sum(c => c.AmountApproved ?? 0m),
        TotalReceived = i.Claims.Sum(c => c.AmountReceived ?? 0m),
        ClaimsOutstanding = ClaimsOutstanding(i.Claims),
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
        Claims = i.Claims.OrderBy(c => c.Id).Select(ToClaimDto).ToList(),
        Photos = i.Photos.OrderByDescending(p => p.UploadedAt).Select(ToPhotoDto).ToList()
    };

    public static void ApplyUpsert(Incident i, IncidentUpsertDto dto)
    {
        i.VehicleId = dto.VehicleId;
        i.DriverId = dto.DriverId;
        i.TripId = dto.TripId;
        i.OccurredAt = DateTime.SpecifyKind(dto.OccurredAt, DateTimeKind.Utc);
        i.Location = dto.Location;
        i.Latitude = dto.Latitude;
        i.Longitude = dto.Longitude;
        i.IncidentType = dto.IncidentType;
        i.Severity = dto.Severity;
        i.Status = dto.Status;
        i.Description = dto.Description;
        i.ThirdPartyInvolved = dto.ThirdPartyInvolved;
        i.ThirdPartyDetails = dto.ThirdPartyDetails;
        i.PoliceReportNumber = dto.PoliceReportNumber;
        i.EstimatedRepairCost = dto.EstimatedRepairCost;
        i.Notes = dto.Notes;
    }

    public static IncidentPhotoDto ToPhotoDto(IncidentPhoto p) => new()
    {
        Id = p.Id,
        FileName = p.FileName,
        ContentType = p.ContentType,
        FileSizeBytes = p.FileSizeBytes,
        Caption = p.Caption,
        UploadedAt = p.UploadedAt,
        DownloadUrl = $"/api/incidents/{p.IncidentId}/photos/{p.Id}/download"
    };

    public static InsuranceClaimDto ToClaimDto(InsuranceClaim c) => new()
    {
        Id = c.Id,
        ClaimCode = c.ClaimCode,
        IncidentId = c.IncidentId,
        InsurerName = c.InsurerName,
        PolicyNumber = c.PolicyNumber,
        ClaimNumber = c.ClaimNumber,
        ClaimDate = c.ClaimDate,
        AmountClaimed = c.AmountClaimed,
        AmountApproved = c.AmountApproved,
        AmountReceived = c.AmountReceived,
        Deductible = c.Deductible,
        Outstanding = ClaimOutstanding(c),
        Status = c.Status,
        SettledDate = c.SettledDate,
        Notes = c.Notes,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    public static void ApplyUpsert(InsuranceClaim c, InsuranceClaimUpsertDto dto)
    {
        c.InsurerName = dto.InsurerName;
        c.PolicyNumber = dto.PolicyNumber;
        c.ClaimNumber = dto.ClaimNumber;
        c.ClaimDate = dto.ClaimDate;
        c.AmountClaimed = dto.AmountClaimed;
        c.AmountApproved = dto.AmountApproved;
        c.AmountReceived = dto.AmountReceived;
        c.Deductible = dto.Deductible;
        c.Status = dto.Status;
        c.SettledDate = dto.SettledDate;
        c.Notes = dto.Notes;
    }
}
