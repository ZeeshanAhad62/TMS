using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

// Top-level Tyre asset module (api/tyres). Distinct from the nested
// TyreDto/TyreUpsertDto in ChildDtos.cs, which stay as the vehicle editor's
// quick "add a tyre to this vehicle" tab and are unaffected by this module.

public class TyreListItemDto
{
    public int Id { get; set; }
    public string? SerialNumber { get; set; }
    public string? BrandAndSize { get; set; }
    public string? Pattern { get; set; }
    public TyreStatus Status { get; set; }
    public int? VehicleId { get; set; }
    public string? VehicleRegistrationNumber { get; set; }
    public TyrePosition Position { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal DistanceRun { get; set; }
    public decimal? CostPerKm { get; set; }
    public int RetreadCount { get; set; }
}

public class TyreDetailDto : TyreListItemDto
{
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? InstallationDate { get; set; }
    public decimal? InstallationOdometer { get; set; }
    public decimal TotalDistanceRunCarried { get; set; }
    public string? CurrentCondition { get; set; }
    public DateOnly? LastRotationDate { get; set; }
    public DateOnly? LastRetreadDate { get; set; }
    public List<TyreEventDto> Events { get; set; } = new();
}

public class TyreCreateDto
{
    [MaxLength(100)]
    public string? SerialNumber { get; set; }
    public string? BrandAndSize { get; set; }
    [MaxLength(100)]
    public string? Pattern { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    [Range(0, 999999999)]
    public decimal? PurchaseCost { get; set; }
    public string? CurrentCondition { get; set; }
}

public class TyreEventDto
{
    public int Id { get; set; }
    public TyreEventType EventType { get; set; }
    public DateOnly EventDate { get; set; }
    public int? VehicleId { get; set; }
    public TyrePosition? Position { get; set; }
    public decimal? Odometer { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
}

public class TyreEventUpsertDto
{
    [Required]
    public TyreEventType EventType { get; set; }

    [Required]
    public DateOnly EventDate { get; set; }

    public int? VehicleId { get; set; }
    public TyrePosition? Position { get; set; }

    [Range(0, 99999999)]
    public decimal? Odometer { get; set; }

    [Range(0, 999999999)]
    public decimal? Cost { get; set; }

    public string? Notes { get; set; }
}
