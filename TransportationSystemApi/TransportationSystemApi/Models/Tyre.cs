namespace TransportationSystemApi.Models;

public class Tyre
{
    public int Id { get; set; }

    // Null while in stock (not currently fitted to any vehicle).
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public TyrePosition Position { get; set; }
    public string? BrandAndSize { get; set; }
    public DateOnly? InstallationDate { get; set; }

    // Odometer reading when this tyre was fitted for its CURRENT stint --
    // the baseline DistanceRun is measured from. Cleared on removal.
    public decimal? InstallationOdometer { get; set; }
    public string? CurrentCondition { get; set; } // tread depth / rating scale
    public DateOnly? LastRotationDate { get; set; }

    public List<TyreReplacementHistory> ReplacementHistory { get; set; } = new();

    // ----- Full asset tracking (module 6) -----
    public string? SerialNumber { get; set; }
    public string? Pattern { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public TyreStatus Status { get; set; } = TyreStatus.InStock;

    // Distance run during completed stints (before the current fit), carried
    // forward across remove/rotate/scrap so total lifetime distance survives
    // a re-fit. Added to (current odometer - InstallationOdometer) at read time.
    public decimal TotalDistanceRunCarried { get; set; }

    public List<TyreEvent> Events { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
