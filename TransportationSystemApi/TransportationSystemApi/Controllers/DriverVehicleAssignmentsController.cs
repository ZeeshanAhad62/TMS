using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/drivers/{driverId:int}/assignments")]
public class DriverVehicleAssignmentsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public DriverVehicleAssignmentsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DriverVehicleAssignmentDto>>> GetAll(int driverId)
    {
        if (!await _db.Drivers.AnyAsync(d => d.Id == driverId)) return NotFound();
        var records = await _db.DriverVehicleAssignments
            .Include(a => a.Vehicle)
            .Where(a => a.DriverId == driverId)
            .OrderByDescending(a => a.StartDate)
            .ToListAsync();
        return records.Select(DriverMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<DriverVehicleAssignmentDto>> Create(int driverId, DriverVehicleAssignmentUpsertDto dto)
    {
        if (!await _db.Drivers.AnyAsync(d => d.Id == driverId)) return NotFound("Driver not found.");
        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle is null) return NotFound("Vehicle not found.");

        var record = new DriverVehicleAssignment
        {
            DriverId = driverId,
            VehicleId = dto.VehicleId,
            Vehicle = vehicle,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            Notes = dto.Notes
        };

        _db.DriverVehicleAssignments.Add(record);
        await _db.SaveChangesAsync();
        return Ok(DriverMapper.ToDto(record));
    }

    [HttpPut("{assignmentId:int}")]
    public async Task<IActionResult> Update(int driverId, int assignmentId, DriverVehicleAssignmentUpsertDto dto)
    {
        var record = await _db.DriverVehicleAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.DriverId == driverId);
        if (record is null) return NotFound();

        if (!await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId)) return NotFound("Vehicle not found.");

        record.VehicleId = dto.VehicleId;
        record.StartDate = dto.StartDate;
        record.EndDate = dto.EndDate;
        record.Status = dto.Status;
        record.Notes = dto.Notes;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{assignmentId:int}")]
    public async Task<IActionResult> Delete(int driverId, int assignmentId)
    {
        var record = await _db.DriverVehicleAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId && a.DriverId == driverId);
        if (record is null) return NotFound();

        _db.DriverVehicleAssignments.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
