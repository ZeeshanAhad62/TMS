using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/vehicles/{vehicleId:int}/alerts")]
public class VehicleAlertsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public VehicleAlertsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AlertRuleDto>>> GetAll(int vehicleId)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();
        var rules = await _db.AlertRules.Where(a => a.VehicleId == vehicleId).ToListAsync();
        return rules.Select(VehicleMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<AlertRuleDto>> Create(int vehicleId, AlertRuleUpsertDto dto)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();

        var rule = new AlertRule
        {
            VehicleId = vehicleId,
            DocumentCategory = dto.DocumentCategory,
            ThresholdDays = dto.ThresholdDays,
            Channel = dto.Channel,
            RecipientRole = dto.RecipientRole,
            Status = dto.Status
        };

        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync();
        return Ok(VehicleMapper.ToDto(rule));
    }

    [HttpPut("{alertId:int}")]
    public async Task<IActionResult> Update(int vehicleId, int alertId, AlertRuleUpsertDto dto)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(a => a.Id == alertId && a.VehicleId == vehicleId);
        if (rule is null) return NotFound();

        rule.DocumentCategory = dto.DocumentCategory;
        rule.ThresholdDays = dto.ThresholdDays;
        rule.Channel = dto.Channel;
        rule.RecipientRole = dto.RecipientRole;
        rule.Status = dto.Status;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{alertId:int}")]
    public async Task<IActionResult> Delete(int vehicleId, int alertId)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(a => a.Id == alertId && a.VehicleId == vehicleId);
        if (rule is null) return NotFound();

        _db.AlertRules.Remove(rule);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
