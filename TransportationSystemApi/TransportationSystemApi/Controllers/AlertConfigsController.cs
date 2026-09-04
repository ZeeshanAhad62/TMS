using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/compliance/config")]
public class AlertConfigsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public AlertConfigsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AlertConfigDto>>> GetAll()
    {
        var configs = await _db.AlertConfigs.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync();
        return configs.Select(AlertConfigMapper.ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AlertConfigDto>> GetById(int id)
    {
        var config = await _db.AlertConfigs.FindAsync(id);
        if (config is null) return NotFound();
        return AlertConfigMapper.ToDto(config);
    }

    [HttpPost]
    public async Task<ActionResult<AlertConfigDto>> Create(AlertConfigUpsertDto dto)
    {
        var config = new AlertConfig();
        AlertConfigMapper.ApplyUpsert(config, dto);

        _db.AlertConfigs.Add(config);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = config.Id }, AlertConfigMapper.ToDto(config));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AlertConfigUpsertDto dto)
    {
        var config = await _db.AlertConfigs.FindAsync(id);
        if (config is null) return NotFound();

        AlertConfigMapper.ApplyUpsert(config, dto);
        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var config = await _db.AlertConfigs.FindAsync(id);
        if (config is null) return NotFound();

        _db.AlertConfigs.Remove(config);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
