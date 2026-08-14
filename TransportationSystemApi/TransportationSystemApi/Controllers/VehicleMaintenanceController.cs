using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/vehicles/{vehicleId:int}/maintenance")]
public class VehicleMaintenanceController : ControllerBase
{
    private readonly FleetDbContext _db;

    public VehicleMaintenanceController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MaintenanceRecordDto>>> GetAll(int vehicleId)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();
        var records = await _db.MaintenanceRecords
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();
        return records.Select(VehicleMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceRecordDto>> Create(int vehicleId, MaintenanceRecordUpsertDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null) return NotFound();

        var record = new MaintenanceRecord
        {
            VehicleId = vehicleId,
            Type = dto.Type,
            Date = dto.Date,
            Odometer = dto.Odometer,
            Description = dto.Description,
            ServiceVendor = dto.ServiceVendor,
            Cost = dto.Cost
        };

        _db.MaintenanceRecords.Add(record);

        // Keep the vehicle's quick-reference maintenance fields in sync with the log.
        switch (dto.Type)
        {
            case MaintenanceType.OilChange:
                if (vehicle.LastOilChangeDate is null || dto.Date >= vehicle.LastOilChangeDate)
                {
                    vehicle.LastOilChangeDate = dto.Date;
                    vehicle.LastOilChangeOdometer = dto.Odometer;
                }
                break;
            case MaintenanceType.GeneralService:
                if (vehicle.LastServiceDate is null || dto.Date >= vehicle.LastServiceDate)
                    vehicle.LastServiceDate = dto.Date;
                break;
            case MaintenanceType.BatteryReplacement:
                if (vehicle.BatteryReplacementDate is null || dto.Date >= vehicle.BatteryReplacementDate)
                    vehicle.BatteryReplacementDate = dto.Date;
                break;
        }

        await _db.SaveChangesAsync();
        return Ok(VehicleMapper.ToDto(record));
    }

    [HttpDelete("{recordId:int}")]
    public async Task<IActionResult> Delete(int vehicleId, int recordId)
    {
        var record = await _db.MaintenanceRecords.FirstOrDefaultAsync(m => m.Id == recordId && m.VehicleId == vehicleId);
        if (record is null) return NotFound();

        _db.MaintenanceRecords.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
