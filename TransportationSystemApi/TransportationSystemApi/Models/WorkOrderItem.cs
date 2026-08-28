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
}
