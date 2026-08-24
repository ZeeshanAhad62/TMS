using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class TripListItemDto
{
    public int Id { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TripStatus Status { get; set; }
}

public class TripDetailDto : TripUpsertDto
{
    public int Id { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
}

public class TripUpsertDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public int DriverId { get; set; }

    [Required, MaxLength(150)]
    public string Origin { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Scheduled;
    public string? Notes { get; set; }
    public decimal? Revenue { get; set; }
}
