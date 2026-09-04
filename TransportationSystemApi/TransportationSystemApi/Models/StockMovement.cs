namespace TransportationSystemApi.Models;

// Receipt (+Quantity), Issue (-Quantity) or Adjust (+/-Quantity as entered)
// against a Part. ReferenceId is a soft link (no FK) -- today only WorkOrder,
// kept generic so a future PurchaseOrder reference (module 11) doesn't need
// a schema change.
public class StockMovement
{
    public int Id { get; set; }

    public int PartId { get; set; }
    public Part? Part { get; set; }

    public PartMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public DateOnly Date { get; set; }

    public StockMovementReferenceType ReferenceType { get; set; } = StockMovementReferenceType.Manual;
    public int? ReferenceId { get; set; }
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
