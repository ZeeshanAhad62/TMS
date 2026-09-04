using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/workorders")]
public class WorkOrdersController : ControllerBase
{
    private readonly FleetDbContext _db;

    public WorkOrdersController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkOrderListItemDto>>> GetAll(
        [FromQuery] int? vehicleId,
        [FromQuery] WorkOrderStatus? status,
        [FromQuery] MaintenanceType? type,
        [FromQuery] string? search)
    {
        var query = _db.WorkOrders.AsNoTracking()
            .Include(w => w.Vehicle)
            .Include(w => w.Items).ThenInclude(i => i.Part)
            .AsQueryable();

        if (vehicleId.HasValue)
            query = query.Where(w => w.VehicleId == vehicleId.Value);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        if (type.HasValue)
            query = query.Where(w => w.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w =>
                w.WorkOrderCode.Contains(search) ||
                (w.Workshop != null && w.Workshop.Contains(search)) ||
                (w.Description != null && w.Description.Contains(search)));
        }

        var workOrders = await query.OrderByDescending(w => w.ReportedDate).ThenByDescending(w => w.Id).ToListAsync();
        return workOrders.Select(WorkOrderMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkOrderDetailDto>> GetById(int id)
    {
        var workOrder = await LoadFull(id);
        if (workOrder is null) return NotFound();
        return WorkOrderMapper.ToDetailDto(workOrder);
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderDetailDto>> Create(WorkOrderUpsertDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle is null) return NotFound("Vehicle not found.");

        var workOrder = new WorkOrder { Vehicle = vehicle };
        WorkOrderMapper.ApplyUpsert(workOrder, dto);
        NormalizeCompletion(workOrder);

        _db.WorkOrders.Add(workOrder);
        await _db.SaveChangesAsync();

        workOrder.WorkOrderCode = $"WO-{workOrder.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = workOrder.Id }, WorkOrderMapper.ToDetailDto(workOrder));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, WorkOrderUpsertDto dto)
    {
        var workOrder = await _db.WorkOrders.FindAsync(id);
        if (workOrder is null) return NotFound();

        if (!await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId)) return NotFound("Vehicle not found.");

        WorkOrderMapper.ApplyUpsert(workOrder, dto);
        NormalizeCompletion(workOrder);
        workOrder.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var workOrder = await _db.WorkOrders.FindAsync(id);
        if (workOrder is null) return NotFound();

        // WorkOrderItems cascades at the DB level, but any StockMovement an
        // item issued does not -- delete those explicitly first so on-hand
        // qty is restored instead of the movement being orphaned.
        var stockMovementIds = await _db.WorkOrderItems
            .Where(i => i.WorkOrderId == id && i.StockMovementId != null)
            .Select(i => i.StockMovementId!.Value)
            .ToListAsync();
        if (stockMovementIds.Count > 0)
        {
            await _db.StockMovements.Where(m => stockMovementIds.Contains(m.Id)).ExecuteDeleteAsync();
        }

        _db.WorkOrders.Remove(workOrder);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // A completed work order should always carry a completion date; a
    // re-opened one should not.
    private static void NormalizeCompletion(WorkOrder w)
    {
        if (w.Status == WorkOrderStatus.Completed)
            w.CompletedDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        else
            w.CompletedDate = null;
    }

    private async Task<WorkOrder?> LoadFull(int id)
    {
        return await _db.WorkOrders
            .Include(w => w.Vehicle)
            .Include(w => w.Items).ThenInclude(i => i.Part)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.Id == id);
    }
}
