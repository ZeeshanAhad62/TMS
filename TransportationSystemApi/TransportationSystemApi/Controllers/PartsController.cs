using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/parts")]
public class PartsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public PartsController(FleetDbContext db)
    {
        _db = db;
    }

    private IQueryable<Part> BaseQuery() => _db.Parts.Include(p => p.Movements).AsNoTracking();

    [HttpGet]
    public async Task<ActionResult<List<PartListItemDto>>> GetAll([FromQuery] string? search)
    {
        var query = BaseQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.PartNumber.Contains(search) ||
                p.Name.Contains(search));
        }

        var parts = await query.OrderBy(p => p.PartNumber).ToListAsync();
        return parts.Select(PartMapper.ToListItemDto).ToList();
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<PartListItemDto>>> GetLowStock()
    {
        var parts = await BaseQuery().ToListAsync();
        return parts.Select(PartMapper.ToListItemDto).Where(p => p.BelowReorder).OrderBy(p => p.PartNumber).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartDetailDto>> GetById(int id)
    {
        var part = await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);
        if (part is null) return NotFound();
        return PartMapper.ToDetailDto(part);
    }

    [HttpPost]
    public async Task<ActionResult<PartDetailDto>> Create(PartUpsertDto dto)
    {
        if (await _db.Parts.AnyAsync(p => p.PartNumber == dto.PartNumber))
            return BadRequest("A part with this part number already exists.");

        var part = new Part();
        PartMapper.ApplyUpsert(part, dto);

        _db.Parts.Add(part);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = part.Id }, PartMapper.ToDetailDto(part));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PartUpsertDto dto)
    {
        var part = await _db.Parts.FindAsync(id);
        if (part is null) return NotFound();

        if (await _db.Parts.AnyAsync(p => p.Id != id && p.PartNumber == dto.PartNumber))
            return BadRequest("A part with this part number already exists.");

        PartMapper.ApplyUpsert(part, dto);
        part.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var part = await _db.Parts.FindAsync(id);
        if (part is null) return NotFound();

        _db.Parts.Remove(part);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/movements")]
    public async Task<ActionResult<List<StockMovementDto>>> GetMovements(int id)
    {
        if (!await _db.Parts.AnyAsync(p => p.Id == id)) return NotFound();

        var movements = await _db.StockMovements.AsNoTracking()
            .Where(m => m.PartId == id)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
            .ToListAsync();
        return movements.Select(PartMapper.ToDto).ToList();
    }

    // Manual movements only -- always Manual reference type. Movements tied
    // to a work-order line are created/edited/removed through that line
    // instead (see WorkOrderItemsController), so they stay in sync with it.
    [HttpPost("{id:int}/movements")]
    public async Task<ActionResult<StockMovementDto>> AddMovement(int id, StockMovementUpsertDto dto)
    {
        if (!await _db.Parts.AnyAsync(p => p.Id == id)) return NotFound();

        if (dto.MovementType is PartMovementType.Receipt or PartMovementType.Issue && dto.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero for a receipt or issue.");
        if (dto.MovementType == PartMovementType.Adjust && dto.Quantity == 0)
            return BadRequest("Adjustment quantity cannot be zero.");

        var movement = new StockMovement { PartId = id, ReferenceType = StockMovementReferenceType.Manual };
        PartMapper.ApplyUpsert(movement, dto);

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();

        return Ok(PartMapper.ToDto(movement));
    }

    [HttpDelete("{id:int}/movements/{movementId:int}")]
    public async Task<IActionResult> DeleteMovement(int id, int movementId)
    {
        var movement = await _db.StockMovements.FirstOrDefaultAsync(m => m.Id == movementId && m.PartId == id);
        if (movement is null) return NotFound();

        if (await _db.WorkOrderItems.AnyAsync(i => i.StockMovementId == movementId))
            return BadRequest("This movement was created from a work order line; edit or delete that line instead.");

        _db.StockMovements.Remove(movement);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
