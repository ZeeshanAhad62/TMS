using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/payruns/{payRunId:int}/lines")]
public class PayRunLinesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public PayRunLinesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<PayRunLineDto>>> GetAll(int payRunId)
    {
        if (!await _db.PayRuns.AnyAsync(p => p.Id == payRunId)) return NotFound();

        var lines = await _db.PayRunLines.AsNoTracking()
            .Where(l => l.PayRunId == payRunId)
            .OrderBy(l => l.Id)
            .ToListAsync();

        var tripCodes = await TripCodesFor(lines);
        return lines.Select(l => PayrollMapper.ToDto(l, Code(tripCodes, l.TripId))).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<PayRunLineDto>> Create(int payRunId, PayRunLineUpsertDto dto)
    {
        if (!await _db.PayRuns.AnyAsync(p => p.Id == payRunId)) return NotFound("Pay run not found.");
        if (dto.TripId.HasValue && !await _db.Trips.AnyAsync(t => t.Id == dto.TripId.Value))
            return NotFound("Trip not found.");

        var line = new PayRunLine { PayRunId = payRunId };
        PayrollMapper.ApplyUpsert(line, dto);

        _db.PayRunLines.Add(line);
        await _db.SaveChangesAsync();

        var tripCodes = await TripCodesFor(new[] { line });
        return Ok(PayrollMapper.ToDto(line, Code(tripCodes, line.TripId)));
    }

    [HttpPut("{lineId:int}")]
    public async Task<IActionResult> Update(int payRunId, int lineId, PayRunLineUpsertDto dto)
    {
        var line = await _db.PayRunLines.FirstOrDefaultAsync(l => l.Id == lineId && l.PayRunId == payRunId);
        if (line is null) return NotFound();
        if (dto.TripId.HasValue && !await _db.Trips.AnyAsync(t => t.Id == dto.TripId.Value))
            return NotFound("Trip not found.");

        PayrollMapper.ApplyUpsert(line, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{lineId:int}")]
    public async Task<IActionResult> Delete(int payRunId, int lineId)
    {
        var line = await _db.PayRunLines.FirstOrDefaultAsync(l => l.Id == lineId && l.PayRunId == payRunId);
        if (line is null) return NotFound();

        _db.PayRunLines.Remove(line);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Dictionary<int, string>> TripCodesFor(IEnumerable<PayRunLine> lines)
    {
        var ids = lines.Where(l => l.TripId.HasValue).Select(l => l.TripId!.Value).Distinct().ToList();
        if (ids.Count == 0) return new();
        return await _db.Trips.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.TripCode);
    }

    private static string? Code(Dictionary<int, string> codes, int? tripId) =>
        tripId is int id && codes.TryGetValue(id, out var code) ? code : null;
}
