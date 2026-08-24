using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class DriverListItemDto
{
    public int Id { get; set; }
    public string DriverCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public DriverStatus Status { get; set; }
    public DateOnly? LicenseExpiryDate { get; set; }
    public bool HasExpiringDocument { get; set; }
}

public class DriverDetailDto : DriverUpsertDto
{
    public int Id { get; set; }
    public string DriverCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<DriverDocumentDto> Documents { get; set; } = new();
    public List<DriverVehicleAssignmentDto> Assignments { get; set; } = new();
}

public class DriverUpsertDto
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }

    [Required, MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    public string? LicenseType { get; set; }
    public DateOnly? LicenseExpiryDate { get; set; }

    public DriverStatus Status { get; set; } = DriverStatus.Active;
}

public class DriverDocumentDto
{
    public int Id { get; set; }
    public DriverDocumentCategory Category { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class DriverVehicleAssignmentDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public AssignmentStatus Status { get; set; }
    public string? Notes { get; set; }
}

public class DriverVehicleAssignmentUpsertDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public string? Notes { get; set; }
}
