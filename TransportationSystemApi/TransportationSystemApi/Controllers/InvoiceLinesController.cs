using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/invoices/{invoiceId:int}/lines")]
public class InvoiceLinesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public InvoiceLinesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceLineDto>>> GetAll(int invoiceId)
    {
        if (!await _db.Invoices.AnyAsync(i => i.Id == invoiceId)) return NotFound();
        var lines = await _db.InvoiceLines
            .Where(l => l.InvoiceId == invoiceId)
            .OrderBy(l => l.Id)
            .ToListAsync();
        return lines.Select(InvoiceMapper.ToLineDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceLineDto>> Create(int invoiceId, InvoiceLineUpsertDto dto)
    {
        if (!await _db.Invoices.AnyAsync(i => i.Id == invoiceId)) return NotFound("Invoice not found.");
        if (dto.TripId.HasValue && !await _db.Trips.AnyAsync(t => t.Id == dto.TripId.Value))
            return NotFound("Trip not found.");

        var line = new InvoiceLine { InvoiceId = invoiceId };
        InvoiceMapper.ApplyUpsert(line, dto);

        _db.InvoiceLines.Add(line);
        await _db.SaveChangesAsync();
        return Ok(InvoiceMapper.ToLineDto(line));
    }

    [HttpPut("{lineId:int}")]
    public async Task<IActionResult> Update(int invoiceId, int lineId, InvoiceLineUpsertDto dto)
    {
        var line = await _db.InvoiceLines.FirstOrDefaultAsync(l => l.Id == lineId && l.InvoiceId == invoiceId);
        if (line is null) return NotFound();
        if (dto.TripId.HasValue && !await _db.Trips.AnyAsync(t => t.Id == dto.TripId.Value))
            return NotFound("Trip not found.");

        InvoiceMapper.ApplyUpsert(line, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{lineId:int}")]
    public async Task<IActionResult> Delete(int invoiceId, int lineId)
    {
        var line = await _db.InvoiceLines.FirstOrDefaultAsync(l => l.Id == lineId && l.InvoiceId == invoiceId);
        if (line is null) return NotFound();

        _db.InvoiceLines.Remove(line);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
