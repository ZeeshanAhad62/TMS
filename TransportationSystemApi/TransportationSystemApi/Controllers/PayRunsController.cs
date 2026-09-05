using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/payruns")]
public class PayRunsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public PayRunsController(FleetDbContext db)
    {
        _db = db;
    }

    private IQueryable<PayRun> WithChildren() =>
        _db.PayRuns.Include(p => p.Driver).Include(p => p.Lines);

    [HttpGet]
    public async Task<ActionResult<List<PayRunListItemDto>>> GetAll(
        [FromQuery] int? driverId,
        [FromQuery] PayRunStatus? status,
        [FromQuery] string? search)
    {
        var query = WithChildren().AsNoTracking().AsQueryable();

        if (driverId.HasValue)
            query = query.Where(p => p.DriverId == driverId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.PayRunCode.Contains(search) ||
                (p.Driver != null && p.Driver.FullName.Contains(search)));
        }

        var runs = await query
            .OrderByDescending(p => p.PeriodEnd).ThenByDescending(p => p.Id)
            .ToListAsync();
        return runs.Select(PayrollMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PayRunDetailDto>> GetById(int id)
    {
        var run = await WithChildren().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (run is null) return NotFound();
        return await ToDetail(run);
    }

    [HttpPost]
    public async Task<ActionResult<PayRunDetailDto>> Create(PayRunUpsertDto dto)
    {
        var driver = await _db.Drivers.FindAsync(dto.DriverId);
        if (driver is null) return NotFound("Driver not found.");
        if (dto.PeriodEnd < dto.PeriodStart)
            return BadRequest("Period end cannot be before period start.");

        var run = new PayRun();
        PayrollMapper.ApplyUpsert(run, dto);

        _db.PayRuns.Add(run);
        await _db.SaveChangesAsync();

        run.PayRunCode = $"PR-{run.Id:D5}";
        await _db.SaveChangesAsync();

        run.Driver = driver;
        return CreatedAtAction(nameof(GetById), new { id = run.Id }, await ToDetail(run));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PayRunUpsertDto dto)
    {
        var run = await _db.PayRuns.FindAsync(id);
        if (run is null) return NotFound();
        if (dto.PeriodEnd < dto.PeriodStart)
            return BadRequest("Period end cannot be before period start.");
        if (!await _db.Drivers.AnyAsync(d => d.Id == dto.DriverId))
            return NotFound("Driver not found.");

        PayrollMapper.ApplyUpsert(run, dto);
        run.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var run = await _db.PayRuns.FindAsync(id);
        if (run is null) return NotFound();

        _db.PayRuns.Remove(run); // Lines cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Builds a Draft pay run with lines derived from the driver's pay
    // configuration and their completed trips whose StartDate falls in the
    // period. Does not mutate advance rows -- AdvanceRecovery is a suggested
    // figure only (the driver's outstanding advances, capped at gross pay);
    // recovery against individual advances is recorded on the advances tab.
    [HttpPost("generate")]
    public async Task<ActionResult<PayRunDetailDto>> Generate(GeneratePayRunDto dto)
    {
        var driver = await _db.Drivers.FindAsync(dto.DriverId);
        if (driver is null) return NotFound("Driver not found.");
        if (dto.PeriodEnd < dto.PeriodStart)
            return BadRequest("Period end cannot be before period start.");

        var rate = driver.PayRate ?? 0m;
        if (driver.PayType != DriverPayType.PerKm && rate <= 0m)
            return BadRequest("Set the driver's pay rate before generating a pay run.");

        var trips = await _db.Trips.AsNoTracking()
            .Where(t => t.DriverId == driver.Id
                        && t.Status == TripStatus.Completed
                        && t.StartDate >= dto.PeriodStart
                        && t.StartDate <= dto.PeriodEnd)
            .OrderBy(t => t.StartDate).ThenBy(t => t.Id)
            .ToListAsync();

        var lines = new List<PayRunLine>();
        switch (driver.PayType)
        {
            case DriverPayType.PerTrip:
                lines.AddRange(trips.Select(t => new PayRunLine
                {
                    TripId = t.Id,
                    Description = $"Trip {t.TripCode}: {t.Origin} - {t.Destination}",
                    Basis = PayRunLineBasis.PerTrip,
                    Quantity = 1,
                    Rate = rate,
                    Amount = rate
                }));
                break;

            case DriverPayType.Percentage:
                lines.AddRange(trips.Select(t =>
                {
                    var revenue = t.Revenue ?? 0m;
                    return new PayRunLine
                    {
                        TripId = t.Id,
                        Description = $"Trip {t.TripCode}: {revenue:N0} revenue @ {rate:0.##}%",
                        Basis = PayRunLineBasis.Percentage,
                        Quantity = revenue,
                        Rate = rate,
                        Amount = Math.Round(revenue * rate / 100m, 2)
                    };
                }));
                break;

            case DriverPayType.Monthly:
                lines.Add(new PayRunLine
                {
                    Description = $"Monthly pay {dto.PeriodStart:dd-MMM-yyyy} to {dto.PeriodEnd:dd-MMM-yyyy}",
                    Basis = PayRunLineBasis.Monthly,
                    Quantity = 1,
                    Rate = rate,
                    Amount = rate
                });
                break;

            case DriverPayType.PerKm:
                // Trips carry no distance data, so a per-km line cannot be
                // auto-valued -- seed a zero line for the user to fill in.
                lines.Add(new PayRunLine
                {
                    Description = "Per-km pay - enter distance in Quantity (trips carry no odometer data)",
                    Basis = PayRunLineBasis.PerKm,
                    Quantity = 0,
                    Rate = rate,
                    Amount = 0
                });
                break;
        }

        var gross = lines.Sum(l => l.Amount);

        decimal advanceRecovery = 0m;
        if (dto.RecoverAdvances)
        {
            var outstanding = await _db.DriverAdvances
                .Where(a => a.DriverId == driver.Id)
                .SumAsync(a => (decimal?)(a.Amount - a.RecoveredAmount)) ?? 0m;
            advanceRecovery = Math.Max(0m, Math.Min(outstanding, gross));
        }

        var run = new PayRun
        {
            DriverId = driver.Id,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            Status = PayRunStatus.Draft,
            AllowancesTotal = 0m,
            AdvanceRecovery = advanceRecovery,
            Lines = lines
        };

        _db.PayRuns.Add(run);
        await _db.SaveChangesAsync();

        run.PayRunCode = $"PR-{run.Id:D5}";
        await _db.SaveChangesAsync();

        run.Driver = driver;
        return CreatedAtAction(nameof(GetById), new { id = run.Id }, await ToDetail(run));
    }

    private async Task<PayRunDetailDto> ToDetail(PayRun run)
    {
        var tripIds = run.Lines.Where(l => l.TripId.HasValue).Select(l => l.TripId!.Value).Distinct().ToList();
        var tripCodes = tripIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Trips.AsNoTracking()
                .Where(t => tripIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.TripCode);

        var outstanding = await _db.DriverAdvances
            .Where(a => a.DriverId == run.DriverId)
            .SumAsync(a => (decimal?)(a.Amount - a.RecoveredAmount)) ?? 0m;

        return PayrollMapper.ToDetailDto(run, Math.Max(0m, outstanding), tripCodes);
    }
}
