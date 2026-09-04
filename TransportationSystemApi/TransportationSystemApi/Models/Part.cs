namespace TransportationSystemApi.Models;

// Part master. On-hand quantity and stock value are derived at read time
// (PartMapper) from the Movements log, not stored.
public class Part
{
    public int Id { get; set; }

    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "pcs";
    public decimal ReorderLevel { get; set; }
    public decimal? StandardCost { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<StockMovement> Movements { get; set; } = new();
}
