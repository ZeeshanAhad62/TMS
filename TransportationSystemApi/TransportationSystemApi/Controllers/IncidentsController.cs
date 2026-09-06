using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxFileBytes = 10 * 1024 * 1024;

    public IncidentsController(FleetDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private IQueryable<Incident> WithChildren() =>
        _db.Incidents
            .Include(i => i.Vehicle)
            .Include(i => i.Driver)
            .Include(i => i.Claims)
            .Include(i => i.Photos);

    [HttpGet]
    public async Task<ActionResult<List<IncidentListItemDto>>> GetAll(
        [FromQuery] int? vehicleId,
        [FromQuery] int? driverId,
        [FromQuery] IncidentStatus? status,
        [FromQuery] IncidentSeverity? severity,
        [FromQuery] IncidentType? type,
        [FromQuery] string? search)
    {
        var query = WithChildren().AsNoTracking().AsQueryable();

        if (vehicleId.HasValue) query = query.Where(i => i.VehicleId == vehicleId.Value);
        if (driverId.HasValue) query = query.Where(i => i.DriverId == driverId.Value);
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (severity.HasValue) query = query.Where(i => i.Severity == severity.Value);
        if (type.HasValue) query = query.Where(i => i.IncidentType == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.IncidentCode.Contains(search) ||
                (i.Location != null && i.Location.Contains(search)) ||
                (i.PoliceReportNumber != null && i.PoliceReportNumber.Contains(search)) ||
                (i.Vehicle != null && i.Vehicle.RegistrationNumber.Contains(search)));
        }

        var incidents = await query.OrderByDescending(i => i.OccurredAt).ThenByDescending(i => i.Id).ToListAsync();
        return incidents.Select(IncidentMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidentDetailDto>> GetById(int id)
    {
        var incident = await WithChildren().AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null) return NotFound();

        string? tripCode = null;
        if (incident.TripId is int tid)
            tripCode = await _db.Trips.Where(t => t.Id == tid).Select(t => t.TripCode).FirstOrDefaultAsync();

        return IncidentMapper.ToDetailDto(incident, tripCode);
    }

    [HttpPost]
    public async Task<ActionResult<IncidentDetailDto>> Create(IncidentUpsertDto dto)
    {
        var problem = await ValidateAsync(dto);
        if (problem is not null) return problem;

        var incident = new Incident();
        IncidentMapper.ApplyUpsert(incident, dto);

        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();

        incident.IncidentCode = $"INC-{incident.Id:D5}";
        await _db.SaveChangesAsync();

        await _db.Entry(incident).Reference(i => i.Vehicle).LoadAsync();
        await _db.Entry(incident).Reference(i => i.Driver).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = incident.Id }, IncidentMapper.ToDetailDto(incident, null));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, IncidentUpsertDto dto)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null) return NotFound();

        var problem = await ValidateAsync(dto);
        if (problem is not null) return problem;

        IncidentMapper.ApplyUpsert(incident, dto);
        incident.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var incident = await _db.Incidents.Include(i => i.Photos).FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null) return NotFound();

        foreach (var photo in incident.Photos) DeleteFile(photo.StoragePath);
        var folder = Path.Combine(WebRoot(), "uploads", "incidents", id.ToString());
        if (Directory.Exists(folder)) try { Directory.Delete(folder, true); } catch { /* best effort */ }

        _db.Incidents.Remove(incident); // Claims + Photo rows cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ----- Photos -----

    [HttpPost("{id:int}/photos")]
    [RequestSizeLimit(MaxFileBytes)]
    public async Task<ActionResult<IncidentPhotoDto>> UploadPhoto(int id, [FromForm] IncidentPhotoUploadRequest request)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null) return NotFound();

        var file = request.File;
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > MaxFileBytes) return BadRequest("File exceeds the 10 MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Unsupported file type '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}");

        var folder = Path.Combine(WebRoot(), "uploads", "incidents", id.ToString());
        Directory.CreateDirectory(folder);

        var storedFileName = $"{Guid.NewGuid()}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, storedFileName)))
        {
            await file.CopyToAsync(stream);
        }

        var photo = new IncidentPhoto
        {
            IncidentId = id,
            FileName = file.FileName,
            ContentType = file.ContentType,
            StoragePath = Path.Combine("uploads", "incidents", id.ToString(), storedFileName),
            FileSizeBytes = file.Length,
            Caption = string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption.Trim()
        };
        _db.IncidentPhotos.Add(photo);
        await _db.SaveChangesAsync();

        return Ok(IncidentMapper.ToPhotoDto(photo));
    }

    [HttpGet("{id:int}/photos/{photoId:int}/download")]
    public async Task<IActionResult> DownloadPhoto(int id, int photoId)
    {
        var photo = await _db.IncidentPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.IncidentId == id);
        if (photo is null) return NotFound();

        var fullPath = Path.Combine(WebRoot(), photo.StoragePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound("File missing on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, photo.ContentType, photo.FileName);
    }

    [HttpDelete("{id:int}/photos/{photoId:int}")]
    public async Task<IActionResult> DeletePhoto(int id, int photoId)
    {
        var photo = await _db.IncidentPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.IncidentId == id);
        if (photo is null) return NotFound();

        DeleteFile(photo.StoragePath);
        _db.IncidentPhotos.Remove(photo);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ----- helpers -----

    private string WebRoot() => _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

    private void DeleteFile(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath)) return;
        var full = Path.Combine(WebRoot(), storagePath);
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }

    private async Task<ObjectResult?> ValidateAsync(IncidentUpsertDto dto)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId))
            return NotFound("Vehicle not found.");
        if (dto.DriverId is int did && !await _db.Drivers.AnyAsync(d => d.Id == did))
            return NotFound("Driver not found.");
        if (dto.TripId is int tid && !await _db.Trips.AnyAsync(t => t.Id == tid))
            return NotFound("Trip not found.");
        return null;
    }
}

public class IncidentPhotoUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string? Caption { get; set; }
}
