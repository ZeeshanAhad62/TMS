using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

// Stand-in for the future Bookings module: lets Fleet Master show booking
// history / utilization until that module exists.
[ApiController]
[Route("api/vehicles/{vehicleId:int}/bookings")]
public class VehicleBookingsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public VehicleBookingsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<BookingRecordDto>>> GetAll(int vehicleId)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();
        var records = await _db.BookingRecords
            .Where(b => b.VehicleId == vehicleId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();
        return records.Select(VehicleMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<BookingRecordDto>> Create(int vehicleId, BookingRecordUpsertDto dto)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();

        var record = new BookingRecord
        {
            VehicleId = vehicleId,
            TripReference = dto.TripReference,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            Notes = dto.Notes
        };

        _db.BookingRecords.Add(record);
        await _db.SaveChangesAsync();
        return Ok(VehicleMapper.ToDto(record));
    }

    [HttpPut("{bookingId:int}")]
    public async Task<IActionResult> Update(int vehicleId, int bookingId, BookingRecordUpsertDto dto)
    {
        var record = await _db.BookingRecords.FirstOrDefaultAsync(b => b.Id == bookingId && b.VehicleId == vehicleId);
        if (record is null) return NotFound();

        record.TripReference = dto.TripReference;
        record.StartDate = dto.StartDate;
        record.EndDate = dto.EndDate;
        record.Status = dto.Status;
        record.Notes = dto.Notes;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{bookingId:int}")]
    public async Task<IActionResult> Delete(int vehicleId, int bookingId)
    {
        var record = await _db.BookingRecords.FirstOrDefaultAsync(b => b.Id == bookingId && b.VehicleId == vehicleId);
        if (record is null) return NotFound();

        _db.BookingRecords.Remove(record);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
