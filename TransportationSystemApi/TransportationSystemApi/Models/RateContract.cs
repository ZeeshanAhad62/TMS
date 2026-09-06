namespace TransportationSystemApi.Models;

// A negotiated rate for a customer on a route (optionally narrowed to a
// vehicle type), valid over a date window. When several match a trip, the
// most specific one valid on the trip's start date wins (see
// RateContractsController.Quote).
public class RateContract
{
    public int Id { get; set; }

    // System-generated identity: RC-00001
    public string ContractCode { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int RouteId { get; set; }
    public RouteMaster? Route { get; set; }

    // null = applies to any vehicle type
    public VehicleType? VehicleType { get; set; }

    public decimal Rate { get; set; }
    public RateBasis RateBasis { get; set; } = RateBasis.PerTrip;

    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
