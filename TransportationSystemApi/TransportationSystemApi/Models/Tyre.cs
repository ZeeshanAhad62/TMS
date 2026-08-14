namespace TransportationSystemApi.Models;

public class Tyre
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public TyrePosition Position { get; set; }
    public string? BrandAndSize { get; set; }
    public DateOnly? InstallationDate { get; set; }
    public decimal? InstallationOdometer { get; set; }
    public string? CurrentCondition { get; set; } // tread depth / rating scale
    public DateOnly? LastRotationDate { get; set; }

    public List<TyreReplacementHistory> ReplacementHistory { get; set; } = new();
}
