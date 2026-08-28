using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class WorkOrderMapper
{
    public static decimal PartsCost(WorkOrder w) =>
        w.Items.Sum(i => i.Quantity * i.UnitCost);

    public static decimal TotalCost(WorkOrder w) =>
        (w.LabourCost ?? 0m) + PartsCost(w);

    public static WorkOrderListItemDto ToListItemDto(WorkOrder w) => new()
    {
        Id = w.Id,
        WorkOrderCode = w.WorkOrderCode,
        VehicleCode = w.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = w.Vehicle?.RegistrationNumber ?? string.Empty,
        Type = w.Type,
        Priority = w.Priority,
        Status = w.Status,
        ReportedDate = w.ReportedDate,
        ScheduledDate = w.ScheduledDate,
        CompletedDate = w.CompletedDate,
        Workshop = w.Workshop,
        TotalCost = TotalCost(w)
    };

    public static WorkOrderDetailDto ToDetailDto(WorkOrder w) => new()
    {
        Id = w.Id,
        WorkOrderCode = w.WorkOrderCode,
        CreatedAt = w.CreatedAt,
        UpdatedAt = w.UpdatedAt,
        VehicleCode = w.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = w.Vehicle?.RegistrationNumber ?? string.Empty,
        VehicleId = w.VehicleId,
        Type = w.Type,
        Priority = w.Priority,
        Status = w.Status,
        ReportedDate = w.ReportedDate,
        ScheduledDate = w.ScheduledDate,
        CompletedDate = w.CompletedDate,
        Odometer = w.Odometer,
        Workshop = w.Workshop,
        Description = w.Description,
        Notes = w.Notes,
        LabourCost = w.LabourCost,
        PartsCost = PartsCost(w),
        TotalCost = TotalCost(w),
        Items = w.Items.OrderBy(i => i.Id).Select(ToDto).ToList()
    };

    public static void ApplyUpsert(WorkOrder w, WorkOrderUpsertDto dto)
    {
        w.VehicleId = dto.VehicleId;
        w.Type = dto.Type;
        w.Priority = dto.Priority;
        w.Status = dto.Status;
        w.ReportedDate = dto.ReportedDate;
        w.ScheduledDate = dto.ScheduledDate;
        w.CompletedDate = dto.CompletedDate;
        w.Odometer = dto.Odometer;
        w.Workshop = dto.Workshop;
        w.Description = dto.Description;
        w.Notes = dto.Notes;
        w.LabourCost = dto.LabourCost;
    }

    public static WorkOrderItemDto ToDto(WorkOrderItem i) => new()
    {
        Id = i.Id,
        Description = i.Description,
        Quantity = i.Quantity,
        UnitCost = i.UnitCost,
        LineTotal = i.Quantity * i.UnitCost
    };

    public static void ApplyUpsert(WorkOrderItem i, WorkOrderItemUpsertDto dto)
    {
        i.Description = dto.Description;
        i.Quantity = dto.Quantity;
        i.UnitCost = dto.UnitCost;
    }
}
