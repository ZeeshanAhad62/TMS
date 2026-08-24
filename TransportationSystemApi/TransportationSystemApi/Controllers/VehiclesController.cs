using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public VehiclesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleListItemDto>>> GetAll(
        [FromQuery] VehicleType? vehicleType,
        [FromQuery] OperationalStatus? status,
        [FromQuery] string? search)
    {
        var query = _db.Vehicles.AsNoTracking().AsQueryable();

        if (vehicleType.HasValue)
            query = query.Where(v => v.VehicleType == vehicleType.Value);

        if (status.HasValue)
            query = query.Where(v => v.CurrentStatus == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v =>
                v.RegistrationNumber.Contains(search) ||
                v.VehicleCode.Contains(search) ||
                (v.Make != null && v.Make.Contains(search)) ||
                (v.Model != null && v.Model.Contains(search)));
        }

        var vehicles = await query.OrderBy(v => v.VehicleCode).ToListAsync();
        return vehicles.Select(VehicleMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleDetailDto>> GetById(int id)
    {
        var vehicle = await LoadFullVehicle(id);
        if (vehicle is null) return NotFound();
        return VehicleMapper.ToDetailDto(vehicle);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDetailDto>> Create(VehicleUpsertDto dto)
    {
        if (await _db.Vehicles.AnyAsync(v => v.RegistrationNumber == dto.RegistrationNumber))
            return Conflict($"A vehicle with registration number '{dto.RegistrationNumber}' already exists.");

        var vehicle = new Vehicle();
        VehicleMapper.ApplyUpsert(vehicle, dto);

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        vehicle.VehicleCode = $"VEH-{vehicle.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, VehicleMapper.ToDetailDto(vehicle));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VehicleUpsertDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();

        if (await _db.Vehicles.AnyAsync(v => v.Id != id && v.RegistrationNumber == dto.RegistrationNumber))
            return Conflict($"A vehicle with registration number '{dto.RegistrationNumber}' already exists.");

        VehicleMapper.ApplyUpsert(vehicle, dto);
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    internal async Task<Vehicle?> LoadFullVehicle(int id)
    {
        return await _db.Vehicles
            .Include(v => v.Documents)
            .Include(v => v.AlertRules)
            .Include(v => v.Tyres).ThenInclude(t => t.ReplacementHistory)
            .Include(v => v.MaintenanceRecords)
            .Include(v => v.Trips).ThenInclude(t => t.Driver)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id);
    }
}
