using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class DriverMapper
{
    private const int ExpiringSoonThresholdDays = 30;

    public static DriverListItemDto ToListItemDto(Driver d) => new()
    {
        Id = d.Id,
        DriverCode = d.DriverCode,
        FullName = d.FullName,
        PhoneNumber = d.PhoneNumber,
        LicenseNumber = d.LicenseNumber,
        Status = d.Status,
        LicenseExpiryDate = d.LicenseExpiryDate,
        HasExpiringDocument = IsExpiringOrExpired(d.LicenseExpiryDate)
    };

    public static DriverDetailDto ToDetailDto(Driver d) => new()
    {
        Id = d.Id,
        DriverCode = d.DriverCode,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        FullName = d.FullName,
        PhoneNumber = d.PhoneNumber,
        Email = d.Email,
        DateOfBirth = d.DateOfBirth,
        Address = d.Address,
        LicenseNumber = d.LicenseNumber,
        LicenseType = d.LicenseType,
        LicenseExpiryDate = d.LicenseExpiryDate,
        Status = d.Status,

        Documents = d.Documents.Select(ToDto).ToList(),
        Assignments = d.Assignments.OrderByDescending(a => a.StartDate).Select(ToDto).ToList()
    };

    public static void ApplyUpsert(Driver d, DriverUpsertDto dto)
    {
        d.FullName = dto.FullName;
        d.PhoneNumber = dto.PhoneNumber;
        d.Email = dto.Email;
        d.DateOfBirth = dto.DateOfBirth;
        d.Address = dto.Address;
        d.LicenseNumber = dto.LicenseNumber;
        d.LicenseType = dto.LicenseType;
        d.LicenseExpiryDate = dto.LicenseExpiryDate;
        d.Status = dto.Status;
    }

    public static DriverDocumentDto ToDto(DriverDocument d) => new()
    {
        Id = d.Id,
        Category = d.Category,
        FileName = d.FileName,
        ContentType = d.ContentType,
        FileSizeBytes = d.FileSizeBytes,
        UploadedAt = d.UploadedAt,
        DownloadUrl = $"/api/drivers/{d.DriverId}/documents/{d.Id}/download"
    };

    public static DriverVehicleAssignmentDto ToDto(DriverVehicleAssignment a) => new()
    {
        Id = a.Id,
        VehicleId = a.VehicleId,
        VehicleCode = a.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = a.Vehicle?.RegistrationNumber ?? string.Empty,
        StartDate = a.StartDate,
        EndDate = a.EndDate,
        Status = a.Status,
        Notes = a.Notes
    };

    private static bool IsExpiringOrExpired(DateOnly? expiry) =>
        expiry.HasValue && expiry.Value <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ExpiringSoonThresholdDays);
}
