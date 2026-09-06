using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/rate-contracts")]
public class RateContractsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public RateContractsController(FleetDbContext db)
    {
        _db = db;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private IQueryable<RateContract> WithRefs() =>
        _db.RateContracts.Include(c => c.Customer).Include(c => c.Route);

    [HttpGet]
    public async Task<ActionResult<List<RateContractListItemDto>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] int? routeId,
        [FromQuery] bool? activeOnly,
        [FromQuery] bool? currentOnly)
    {
        var query = WithRefs().AsNoTracking().AsQueryable();

        if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId.Value);
        if (routeId.HasValue) query = query.Where(c => c.RouteId == routeId.Value);
        if (activeOnly == true) query = query.Where(c => c.IsActive);

        var contracts = await query
            .OrderByDescending(c => c.ValidFrom).ThenByDescending(c => c.Id)
            .ToListAsync();

        var result = contracts.Select(c => RouteRateMapper.ToListItemDto(c, Today));
        if (currentOnly == true) result = result.Where(c => c.IsCurrentlyValid);
        return result.ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RateContractDetailDto>> GetById(int id)
    {
        var contract = await WithRefs().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound();
        return RouteRateMapper.ToDetailDto(contract, Today);
    }

    [HttpPost]
    public async Task<ActionResult<RateContractDetailDto>> Create(RateContractUpsertDto dto)
    {
        var problem = await ValidateAsync(dto);
        if (problem is not null) return BadRequest(problem);

        var contract = new RateContract();
        RouteRateMapper.ApplyUpsert(contract, dto);

        _db.RateContracts.Add(contract);
        await _db.SaveChangesAsync();

        contract.ContractCode = $"RC-{contract.Id:D5}";
        await _db.SaveChangesAsync();

        await LoadRefs(contract);
        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, RouteRateMapper.ToDetailDto(contract, Today));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RateContractUpsertDto dto)
    {
        var contract = await _db.RateContracts.FindAsync(id);
        if (contract is null) return NotFound();

        var problem = await ValidateAsync(dto);
        if (problem is not null) return BadRequest(problem);

        RouteRateMapper.ApplyUpsert(contract, dto);
        contract.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _db.RateContracts.FindAsync(id);
        if (contract is null) return NotFound();

        _db.RateContracts.Remove(contract);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Resolve the rate that applies to a trip: the best customer contract for
    // the route on the given date, else the route's standard rate.
    [HttpGet("quote")]
    public async Task<ActionResult<RateQuoteDto>> Quote(
        [FromQuery] int routeId,
        [FromQuery] int? customerId,
        [FromQuery] VehicleType? vehicleType,
        [FromQuery] DateOnly? onDate)
    {
        var route = await _db.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == routeId);
        if (route is null) return NotFound("Route not found.");

        var contracts = await _db.RateContracts.AsNoTracking()
            .Where(c => c.RouteId == routeId)
            .ToListAsync();

        return RouteRateMapper.ResolveQuote(route, contracts, customerId, vehicleType, onDate ?? Today);
    }

    private async Task<string?> ValidateAsync(RateContractUpsertDto dto)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == dto.CustomerId))
            return "Customer not found.";
        if (!await _db.Routes.AnyAsync(r => r.Id == dto.RouteId))
            return "Route not found.";
        if (dto.ValidTo is DateOnly to && to < dto.ValidFrom)
            return "Valid-to date cannot be before valid-from.";
        if (dto.Rate < 0)
            return "Rate cannot be negative.";
        return null;
    }

    private async Task LoadRefs(RateContract c)
    {
        await _db.Entry(c).Reference(x => x.Customer).LoadAsync();
        await _db.Entry(c).Reference(x => x.Route).LoadAsync();
    }
}
