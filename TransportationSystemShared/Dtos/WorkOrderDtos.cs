using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class WorkOrderListItemDto
{
    public int Id { get; set; }
    public string WorkOrderCode { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public MaintenanceType Type { get; set; }
    public WorkOrderPriority Priority { get; set; }
    public WorkOrderStatus Status { get; set; }
    public DateOnly ReportedDate { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string? Workshop { get; set; }
    public decimal TotalCost { get; set; }
}

public class WorkOrderDetailDto : WorkOrderUpsertDto
{
    public int Id { get; set; }
    public string WorkOrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;

    public decimal PartsCost { get; set; }
    public decimal TotalCost { get; set; }

    public List<WorkOrderItemDto> Items { get; set; } = new();
}

public class WorkOrderUpsertDto
{
    [Required]
    public int VehicleId { get; set; }

    public MaintenanceType Type { get; set; } = MaintenanceType.GeneralService;
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;

    [Required]
    public DateOnly ReportedDate { get; set; }
    public DateOnly? ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }

    public decimal? Odometer { get; set; }

    [MaxLength(150)]
    public string? Workshop { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public decimal? LabourCost { get; set; }
}

public class WorkOrderItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class WorkOrderItemUpsertDto
{
    [Required, MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal UnitCost { get; set; }
}
