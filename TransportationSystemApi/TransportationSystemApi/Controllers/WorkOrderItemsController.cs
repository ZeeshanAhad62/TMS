using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/workorders/{workOrderId:int}/items")]
public class WorkOrderItemsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public WorkOrderItemsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkOrderItemDto>>> GetAll(int workOrderId)
    {
        if (!await _db.WorkOrders.AnyAsync(w => w.Id == workOrderId)) return NotFound();
        var items = await _db.WorkOrderItems.Include(i => i.Part)
            .Where(i => i.WorkOrderId == workOrderId)
            .OrderBy(i => i.Id)
            .ToListAsync();
        return items.Select(WorkOrderMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderItemDto>> Create(int workOrderId, WorkOrderItemUpsertDto dto)
    {
        var workOrder = await _db.WorkOrders.FindAsync(workOrderId);
        if (workOrder is null) return NotFound("Work order not found.");

        if (dto.PartId.HasValue && !await _db.Parts.AnyAsync(p => p.Id == dto.PartId))
            return BadRequest("Part not found.");

        var item = new WorkOrderItem { WorkOrderId = workOrderId };
        WorkOrderMapper.ApplyUpsert(item, dto);
        item.PartId = dto.PartId;

        _db.WorkOrderItems.Add(item);
        await _db.SaveChangesAsync();

        if (dto.PartId.HasValue)
        {
            var movement = IssueMovementFor(item, workOrder);
            _db.StockMovements.Add(movement);
            await _db.SaveChangesAsync();
            item.StockMovementId = movement.Id;
            await _db.SaveChangesAsync();
        }

        var created = await _db.WorkOrderItems.Include(i => i.Part).FirstAsync(i => i.Id == item.Id);
        return Ok(WorkOrderMapper.ToDto(created));
    }

    [HttpPut("{itemId:int}")]
    public async Task<IActionResult> Update(int workOrderId, int itemId, WorkOrderItemUpsertDto dto)
    {
        var item = await _db.WorkOrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.WorkOrderId == workOrderId);
        if (item is null) return NotFound();

        if (dto.PartId.HasValue && !await _db.Parts.AnyAsync(p => p.Id == dto.PartId))
            return BadRequest("Part not found.");

        WorkOrderMapper.ApplyUpsert(item, dto);

        var existingMovement = item.StockMovementId.HasValue
            ? await _db.StockMovements.FindAsync(item.StockMovementId.Value)
            : null;

        if (!dto.PartId.HasValue)
        {
            // Part removed from the line -- drop the issue it created, if any.
            if (existingMovement is not null) _db.StockMovements.Remove(existingMovement);
            item.PartId = null;
            item.StockMovementId = null;
        }
        else if (existingMovement is not null)
        {
            // Still linked -- keep the movement in step with the line.
            existingMovement.PartId = dto.PartId.Value;
            existingMovement.Quantity = dto.Quantity;
            existingMovement.UnitCost = dto.UnitCost;
            item.PartId = dto.PartId;
        }
        else
        {
            // Newly linked to a part -- create the issue now.
            item.PartId = dto.PartId;
            var workOrder = await _db.WorkOrders.FindAsync(workOrderId);
            var movement = IssueMovementFor(item, workOrder!);
            _db.StockMovements.Add(movement);
            await _db.SaveChangesAsync();
            item.StockMovementId = movement.Id;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> Delete(int workOrderId, int itemId)
    {
        var item = await _db.WorkOrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.WorkOrderId == workOrderId);
        if (item is null) return NotFound();

        if (item.StockMovementId.HasValue)
        {
            var movement = await _db.StockMovements.FindAsync(item.StockMovementId.Value);
            if (movement is not null) _db.StockMovements.Remove(movement);
        }

        _db.WorkOrderItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static StockMovement IssueMovementFor(WorkOrderItem item, WorkOrder workOrder) => new()
    {
        PartId = item.PartId!.Value,
        MovementType = PartMovementType.Issue,
        Quantity = item.Quantity,
        UnitCost = item.UnitCost,
        Date = workOrder.ReportedDate,
        ReferenceType = StockMovementReferenceType.WorkOrder,
        ReferenceId = workOrder.Id,
        Notes = $"Issued for {workOrder.WorkOrderCode}"
    };
}
