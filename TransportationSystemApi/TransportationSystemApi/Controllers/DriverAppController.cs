using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;
using TransportationSystemApi.Services;

namespace TransportationSystemApi.Controllers;

// Scoped surface for the mobile driver app (a static PWA-style client under
// wwwroot/driver-app/). Every endpoint operates on the calling driver's own
// data, resolved from the token's driverId claim. StaffOnlyFilter keeps a
// driver-app token off every other controller.
[ApiController]
[Route("api/driver-app")]
[Authorize(Policy = "DriverApp")]
public class DriverAppController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly JwtTokenService _tokens;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedPodExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxPodBytes = 10 * 1024 * 1024;

    public DriverAppController(FleetDbContext db, JwtTokenService tokens, IWebHostEnvironment env)
    {
        _db = db;
        _tokens = tokens;
        _env = env;
    }

    private int DriverId => int.Parse(User.FindFirst("driverId")!.Value);

    // ----- Auth -----

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<DriverAppLoginResponseDto>> Login(DriverAppLoginRequestDto dto)
    {
        var code = dto.DriverCode.Trim();
        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.DriverCode == code);

        var ok = driver is { AppLoginEnabled: true, AppPassword: not null }
                 && FixedTimeEquals(driver.AppPassword!, dto.Password);
        if (driver is null || !ok)
            return Unauthorized("Invalid driver code or password, or app access is not enabled.");

        driver.AppLastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var (token, expiresAt) = _tokens.CreateDriverToken(driver);
        return new DriverAppLoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Driver = ToProfile(driver)
        };
    }

    [HttpGet("me")]
    public async Task<ActionResult<DriverAppProfileDto>> Me()
    {
        var driver = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == DriverId);
        if (driver is null) return NotFound();
        return ToProfile(driver);
    }

    // ----- My trips -----

    [HttpGet("trips")]
    public async Task<ActionResult<List<DriverAppTripListItemDto>>> MyTrips([FromQuery] TripStatus? status)
    {
        var query = _db.Trips.AsNoTracking()
            .Include(t => t.Vehicle)
            .Include(t => t.Consignments)
            .Where(t => t.DriverId == DriverId);

        if (status.HasValue) query = query.Where(t => t.Status == status.Value);

        var trips = await query.OrderByDescending(t => t.StartDate).ThenByDescending(t => t.Id).ToListAsync();
        return trips.Select(ToListItem).ToList();
    }

    [HttpGet("trips/{id:int}")]
    public async Task<ActionResult<DriverAppTripDetailDto>> MyTrip(int id)
    {
        var trip = await _db.Trips.AsNoTracking()
            .Include(t => t.Vehicle)
            .Include(t => t.Customer)
            .Include(t => t.Expenses)
            .Include(t => t.Consignments)
            .FirstOrDefaultAsync(t => t.Id == id && t.DriverId == DriverId);
        if (trip is null) return NotFound();

        var list = ToListItem(trip);
        return new DriverAppTripDetailDto
        {
            Id = list.Id,
            TripCode = list.TripCode,
            VehicleCode = list.VehicleCode,
            VehicleRegistrationNumber = list.VehicleRegistrationNumber,
            Origin = list.Origin,
            Destination = list.Destination,
            StartDate = list.StartDate,
            EndDate = list.EndDate,
            Status = list.Status,
            ConsignmentCount = list.ConsignmentCount,
            PendingPodCount = list.PendingPodCount,
            CustomerName = trip.Customer?.Name,
            Notes = trip.Notes,
            ExpensesTotal = trip.Expenses.Sum(e => e.Amount),
            Expenses = trip.Expenses
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Select(e => new DriverAppExpenseDto
                {
                    Id = e.Id, Category = e.Category, Amount = e.Amount, Date = e.Date, Notes = e.Notes
                }).ToList(),
            Consignments = trip.Consignments
                .OrderBy(c => c.Id)
                .Select(ToConsignmentDto).ToList()
        };
    }

    [HttpPost("trips/{id:int}/status")]
    public async Task<IActionResult> SetTripStatus(int id, DriverAppTripStatusDto dto)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id && t.DriverId == DriverId);
        if (trip is null) return NotFound();

        var allowed = (trip.Status, dto.Status) switch
        {
            (TripStatus.Scheduled, TripStatus.Active) => true,
            (TripStatus.Active, TripStatus.Completed) => true,
            _ => false
        };
        if (!allowed)
            return BadRequest($"A driver can only start (Scheduled -> Active) or finish (Active -> Completed) a trip. Current status: {trip.Status}.");

        trip.Status = dto.Status;
        if (dto.Status == TripStatus.Completed && trip.EndDate is null)
            trip.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        trip.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("trips/{id:int}/expenses")]
    public async Task<ActionResult<DriverAppExpenseDto>> LogExpense(int id, DriverAppExpenseCreateDto dto)
    {
        if (!await _db.Trips.AnyAsync(t => t.Id == id && t.DriverId == DriverId)) return NotFound();

        var expense = new TripExpense
        {
            TripId = id,
            Category = dto.Category,
            Amount = dto.Amount,
            Date = dto.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            PaidBy = ExpensePaidBy.Driver,
            Notes = dto.Notes
        };
        _db.TripExpenses.Add(expense);
        await _db.SaveChangesAsync();

        return Ok(new DriverAppExpenseDto
        {
            Id = expense.Id, Category = expense.Category, Amount = expense.Amount,
            Date = expense.Date, Notes = expense.Notes
        });
    }

    [HttpPost("trips/{id:int}/fuel")]
    public async Task<IActionResult> LogFuel(int id, DriverAppFuelCreateDto dto)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id && t.DriverId == DriverId);
        if (trip is null) return NotFound();

        var entry = new FuelEntry
        {
            VehicleId = trip.VehicleId,
            DriverId = DriverId,
            TripId = trip.Id,
            Date = dto.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            OdometerReading = dto.OdometerReading,
            Litres = dto.Litres,
            RatePerLitre = dto.RatePerLitre,
            TotalCost = decimal.Round(dto.Litres * dto.RatePerLitre, 2),
            FuelType = FuelType.Diesel,
            PaymentMode = FuelPaymentMode.Cash,
            IsTankFull = dto.IsTankFull,
            StationName = dto.StationName
        };
        _db.FuelEntries.Add(entry);
        await _db.SaveChangesAsync();

        entry.FuelEntryCode = $"FE-{entry.Id:D5}";
        await _db.SaveChangesAsync();

        return Ok(new { entry.Id, entry.FuelEntryCode, entry.TotalCost });
    }

    // ----- Consignments on my trips -----

    [HttpGet("trips/{id:int}/consignments")]
    public async Task<ActionResult<List<DriverAppConsignmentDto>>> TripConsignments(int id)
    {
        if (!await _db.Trips.AnyAsync(t => t.Id == id && t.DriverId == DriverId)) return NotFound();

        var list = await _db.Consignments.AsNoTracking()
            .Where(c => c.TripId == id).OrderBy(c => c.Id).ToListAsync();
        return list.Select(ToConsignmentDto).ToList();
    }

    [HttpPost("consignments/{cid:int}/deliver")]
    public async Task<IActionResult> DeliverConsignment(int cid, DriverAppDeliverDto dto)
    {
        var c = await MyConsignment(cid);
        if (c is null) return NotFound();
        if (c.Status == ConsignmentStatus.Cancelled) return BadRequest("This consignment is cancelled.");

        c.Status = ConsignmentStatus.Delivered;
        c.DeliveredAt = DateTime.UtcNow;
        c.ReceivedBy = dto.ReceivedBy.Trim();
        c.DeliveryNotes = dto.DeliveryNotes;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("consignments/{cid:int}/pod")]
    [RequestSizeLimit(MaxPodBytes)]
    public async Task<IActionResult> UploadPod(int cid, [FromForm] PodUploadRequest request)
    {
        var c = await MyConsignment(cid);
        if (c is null) return NotFound();

        var file = request.File;
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > MaxPodBytes) return BadRequest("File exceeds the 10 MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPodExtensions.Contains(extension))
            return BadRequest($"Unsupported file type '{extension}'. Allowed: {string.Join(", ", AllowedPodExtensions)}");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "consignments", cid.ToString());
        Directory.CreateDirectory(folder);

        if (!string.IsNullOrEmpty(c.PodStoragePath))
        {
            var old = Path.Combine(webRoot, c.PodStoragePath);
            if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
        }

        var storedFileName = $"{Guid.NewGuid()}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, storedFileName)))
        {
            await file.CopyToAsync(stream);
        }

        c.PodFileName = file.FileName;
        c.PodContentType = file.ContentType;
        c.PodStoragePath = Path.Combine("uploads", "consignments", cid.ToString(), storedFileName);
        c.PodFileSizeBytes = file.Length;
        c.PodUploadedAt = DateTime.UtcNow;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { c.Id, hasPod = true, c.PodFileName });
    }

    // ----- helpers -----

    private async Task<Consignment?> MyConsignment(int cid) =>
        await _db.Consignments
            .Include(c => c.Trip)
            .FirstOrDefaultAsync(c => c.Id == cid && c.Trip != null && c.Trip.DriverId == DriverId);

    private static DriverAppProfileDto ToProfile(Driver d) => new()
    {
        Id = d.Id,
        DriverCode = d.DriverCode,
        FullName = d.FullName,
        PhoneNumber = d.PhoneNumber,
        LicenseNumber = d.LicenseNumber,
        LicenseExpiryDate = d.LicenseExpiryDate
    };

    private static DriverAppTripListItemDto ToListItem(Trip t) => new()
    {
        Id = t.Id,
        TripCode = t.TripCode,
        VehicleCode = t.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = t.Vehicle?.RegistrationNumber ?? string.Empty,
        Origin = t.Origin,
        Destination = t.Destination,
        StartDate = t.StartDate,
        EndDate = t.EndDate,
        Status = t.Status,
        ConsignmentCount = t.Consignments.Count,
        PendingPodCount = t.Consignments.Count(c => c.Status == ConsignmentStatus.Delivered && c.PodStoragePath == null)
    };

    private static DriverAppConsignmentDto ToConsignmentDto(Consignment c) => new()
    {
        Id = c.Id,
        ConsignmentCode = c.ConsignmentCode,
        LrNumber = c.LrNumber,
        ConsignorName = c.ConsignorName,
        ConsigneeName = c.ConsigneeName,
        Status = c.Status,
        HasPod = !string.IsNullOrEmpty(c.PodStoragePath)
    };

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
