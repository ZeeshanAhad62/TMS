using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/incidents/{incidentId:int}/claims")]
public class InsuranceClaimsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public InsuranceClaimsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<InsuranceClaimDto>>> GetAll(int incidentId)
    {
        if (!await _db.Incidents.AnyAsync(i => i.Id == incidentId)) return NotFound();
        var claims = await _db.InsuranceClaims.AsNoTracking()
            .Where(c => c.IncidentId == incidentId)
            .OrderBy(c => c.Id)
            .ToListAsync();
        return claims.Select(IncidentMapper.ToClaimDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<InsuranceClaimDto>> Create(int incidentId, InsuranceClaimUpsertDto dto)
    {
        if (!await _db.Incidents.AnyAsync(i => i.Id == incidentId)) return NotFound("Incident not found.");

        var problem = Validate(dto);
        if (problem is not null) return BadRequest(problem);

        var claim = new InsuranceClaim { IncidentId = incidentId };
        IncidentMapper.ApplyUpsert(claim, dto);

        _db.InsuranceClaims.Add(claim);
        await _db.SaveChangesAsync();

        claim.ClaimCode = $"CLM-{claim.Id:D5}";
        await _db.SaveChangesAsync();

        return Ok(IncidentMapper.ToClaimDto(claim));
    }

    [HttpPut("{claimId:int}")]
    public async Task<IActionResult> Update(int incidentId, int claimId, InsuranceClaimUpsertDto dto)
    {
        var claim = await _db.InsuranceClaims.FirstOrDefaultAsync(c => c.Id == claimId && c.IncidentId == incidentId);
        if (claim is null) return NotFound();

        var problem = Validate(dto);
        if (problem is not null) return BadRequest(problem);

        IncidentMapper.ApplyUpsert(claim, dto);
        claim.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{claimId:int}")]
    public async Task<IActionResult> Delete(int incidentId, int claimId)
    {
        var claim = await _db.InsuranceClaims.FirstOrDefaultAsync(c => c.Id == claimId && c.IncidentId == incidentId);
        if (claim is null) return NotFound();

        _db.InsuranceClaims.Remove(claim);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string? Validate(InsuranceClaimUpsertDto dto)
    {
        if (dto.AmountApproved is decimal a && a > dto.AmountClaimed)
            return "Approved amount cannot exceed the claimed amount.";
        if (dto.AmountReceived is decimal r && dto.AmountApproved is decimal ap && r > ap)
            return "Received amount cannot exceed the approved amount.";
        if (dto.Status == InsuranceClaimStatus.Settled && dto.SettledDate is null)
            return "A settled claim needs a settlement date.";
        return null;
    }
}
