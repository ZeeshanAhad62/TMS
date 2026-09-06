using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

// ----- Routes -----

public class RouteListItemDto
{
    public int Id { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public decimal? StandardRate { get; set; }
    public bool IsActive { get; set; }
    public int ActiveContractCount { get; set; }
}

public class RouteDetailDto : RouteUpsertDto
{
    public int Id { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RateContractListItemDto> Contracts { get; set; } = new();
}

public class RouteUpsertDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [Required, MaxLength(150)]
    public string Origin { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Destination { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal? DistanceKm { get; set; }

    [Range(0, 10000)]
    public decimal? EstimatedDurationHours { get; set; }

    [Range(0, 999999999)]
    public decimal? StandardRate { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

// ----- Rate contracts -----

public class RateContractListItemDto
{
    public int Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string RouteLabel { get; set; } = string.Empty;
    public VehicleType? VehicleType { get; set; }
    public decimal Rate { get; set; }
    public RateBasis RateBasis { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyValid { get; set; }
}

public class RateContractDetailDto : RateContractUpsertDto
{
    public int Id { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RouteCode { get; set; } = string.Empty;
    public string RouteLabel { get; set; } = string.Empty;
    public bool IsCurrentlyValid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RateContractUpsertDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int RouteId { get; set; }

    public VehicleType? VehicleType { get; set; }

    [Range(0, 999999999)]
    public decimal Rate { get; set; }

    public RateBasis RateBasis { get; set; } = RateBasis.PerTrip;

    [Required]
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

// ----- Quote (trip-revenue resolution) -----

public class RateQuoteDto
{
    public RateQuoteSource Source { get; set; }
    public decimal? Rate { get; set; }
    public RateBasis? RateBasis { get; set; }
    public decimal? DistanceKm { get; set; }
    // Rate resolved to a single trip amount (Rate, or Rate x DistanceKm for PerKm).
    public decimal? Amount { get; set; }
    public string? ContractCode { get; set; }
    public string? RouteLabel { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
}
