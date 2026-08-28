namespace TransportationSystemApi.Models;

// A workshop job card. Standalone (like Trip) -- references a Vehicle but is
// not a nested child of it. Parts / materials used on the job live in the
// WorkOrderItems child collection; labour is a single field on the header.
public class WorkOrder
{
    public int Id { get; set; }

    // System-generated identity
    public string WorkOrderCode { get; set; } = string.Empty;

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public MaintenanceType Type { get; set; }
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;

    public DateOnly ReportedDate { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }

    public decimal? Odometer { get; set; }
    public string? Workshop { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public decimal? LabourCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<WorkOrderItem> Items { get; set; } = new();
}
