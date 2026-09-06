using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/purchase-orders/{poId:int}/lines")]
public class PurchaseOrderLinesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public PurchaseOrderLinesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<PurchaseOrderLineDto>>> GetAll(int poId)
    {
        if (!await _db.PurchaseOrders.AnyAsync(p => p.Id == poId)) return NotFound();
        var lines = await _db.PurchaseOrderLines.Include(l => l.Part)
            .Where(l => l.PurchaseOrderId == poId)
            .OrderBy(l => l.Id)
            .ToListAsync();
        return lines.Select(PurchaseOrderMapper.ToLineDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderLineDto>> Create(int poId, PurchaseOrderLineUpsertDto dto)
    {
        if (!await _db.PurchaseOrders.AnyAsync(p => p.Id == poId)) return NotFound("Purchase order not found.");
        if (dto.PartId.HasValue && !await _db.Parts.AnyAsync(p => p.Id == dto.PartId.Value))
            return BadRequest("Part not found.");

        var line = new PurchaseOrderLine { PurchaseOrderId = poId };
        PurchaseOrderMapper.ApplyUpsert(line, dto);

        _db.PurchaseOrderLines.Add(line);
        await _db.SaveChangesAsync();

        var created = await _db.PurchaseOrderLines.Include(l => l.Part).FirstAsync(l => l.Id == line.Id);
        return Ok(PurchaseOrderMapper.ToLineDto(created));
    }

    [HttpPut("{lineId:int}")]
    public async Task<IActionResult> Update(int poId, int lineId, PurchaseOrderLineUpsertDto dto)
    {
        var line = await _db.PurchaseOrderLines.FirstOrDefaultAsync(l => l.Id == lineId && l.PurchaseOrderId == poId);
        if (line is null) return NotFound();
        if (dto.PartId.HasValue && !await _db.Parts.AnyAsync(p => p.Id == dto.PartId.Value))
            return BadRequest("Part not found.");
        if (dto.Quantity < line.QuantityReceived)
            return BadRequest($"Quantity cannot be below the {line.QuantityReceived} already received.");

        PurchaseOrderMapper.ApplyUpsert(line, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{lineId:int}")]
    public async Task<IActionResult> Delete(int poId, int lineId)
    {
        var line = await _db.PurchaseOrderLines.FirstOrDefaultAsync(l => l.Id == lineId && l.PurchaseOrderId == poId);
        if (line is null) return NotFound();
        if (line.QuantityReceived > 0)
            return BadRequest("Goods have been received against this line; it cannot be deleted.");

        _db.PurchaseOrderLines.Remove(line);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
