namespace TransportationSystemApi.Models;

// A single part / material line on a WorkOrder. Line total is Quantity * UnitCost
// (computed in the DTO, not stored).
public class WorkOrderItem
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; }

    // Optional link to a stocked Part -- when set, this line issues stock
    // (see PartsController / WorkOrderItemsController) and StockMovementId
    // tracks the StockMovement row that issue created, so editing or
    // deleting this line keeps on-hand qty in sync.
    public int? PartId { get; set; }
    public Part? Part { get; set; }
    public int? StockMovementId { get; set; }
}
