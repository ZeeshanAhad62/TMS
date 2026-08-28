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
        var items = await _db.WorkOrderItems
            .Where(i => i.WorkOrderId == workOrderId)
            .OrderBy(i => i.Id)
            .ToListAsync();
        return items.Select(WorkOrderMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrderItemDto>> Create(int workOrderId, WorkOrderItemUpsertDto dto)
    {
        if (!await _db.WorkOrders.AnyAsync(w => w.Id == workOrderId)) return NotFound("Work order not found.");

        var item = new WorkOrderItem { WorkOrderId = workOrderId };
        WorkOrderMapper.ApplyUpsert(item, dto);

        _db.WorkOrderItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(WorkOrderMapper.ToDto(item));
    }

    [HttpPut("{itemId:int}")]
    public async Task<IActionResult> Update(int workOrderId, int itemId, WorkOrderItemUpsertDto dto)
    {
        var item = await _db.WorkOrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.WorkOrderId == workOrderId);
        if (item is null) return NotFound();

        WorkOrderMapper.ApplyUpsert(item, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> Delete(int workOrderId, int itemId)
    {
        var item = await _db.WorkOrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.WorkOrderId == workOrderId);
        if (item is null) return NotFound();

        _db.WorkOrderItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
