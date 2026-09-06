using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/consignments")]
public class ConsignmentsController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedPodExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxPodBytes = 10 * 1024 * 1024;

    public ConsignmentsController(FleetDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<List<ConsignmentListItemDto>>> GetAll(
        [FromQuery] int? tripId,
        [FromQuery] ConsignmentStatus? status,
        [FromQuery] bool? pendingPodOnly,
        [FromQuery] string? search)
    {
        var query = _db.Consignments.Include(c => c.Trip).AsNoTracking().AsQueryable();

        if (tripId.HasValue) query = query.Where(c => c.TripId == tripId.Value);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        if (pendingPodOnly == true)
            query = query.Where(c => c.Status == ConsignmentStatus.Delivered && c.PodStoragePath == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.ConsignmentCode.Contains(search) ||
                (c.LrNumber != null && c.LrNumber.Contains(search)) ||
                c.ConsignorName.Contains(search) ||
                c.ConsigneeName.Contains(search));
        }

        var list = await query.OrderByDescending(c => c.BookingDate).ThenByDescending(c => c.Id).ToListAsync();
        return list.Select(ConsignmentMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConsignmentDetailDto>> GetById(int id)
    {
        var c = await _db.Consignments.Include(x => x.Trip).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        return ConsignmentMapper.ToDetailDto(c);
    }

    [HttpPost]
    public async Task<ActionResult<ConsignmentDetailDto>> Create(ConsignmentUpsertDto dto)
    {
        var trip = await _db.Trips.FindAsync(dto.TripId);
        if (trip is null) return NotFound("Trip not found.");

        var c = new Consignment();
        ConsignmentMapper.ApplyUpsert(c, dto);

        _db.Consignments.Add(c);
        await _db.SaveChangesAsync();

        c.ConsignmentCode = $"CN-{c.Id:D5}";
        await _db.SaveChangesAsync();

        c.Trip = trip;
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, ConsignmentMapper.ToDetailDto(c));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ConsignmentUpsertDto dto)
    {
        var c = await _db.Consignments.FindAsync(id);
        if (c is null) return NotFound();
        if (!await _db.Trips.AnyAsync(t => t.Id == dto.TripId))
            return NotFound("Trip not found.");

        ConsignmentMapper.ApplyUpsert(c, dto);
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Consignments.FindAsync(id);
        if (c is null) return NotFound();

        DeletePodFile(c);
        _db.Consignments.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Mark delivered: sets Status + delivery fields together.
    [HttpPost("{id:int}/deliver")]
    public async Task<IActionResult> Deliver(int id, DeliverConsignmentDto dto)
    {
        var c = await _db.Consignments.FindAsync(id);
        if (c is null) return NotFound();
        if (c.Status == ConsignmentStatus.Cancelled)
            return BadRequest("This consignment is cancelled.");

        c.Status = ConsignmentStatus.Delivered;
        c.DeliveredAt = DateTime.SpecifyKind(dto.DeliveredAt ?? DateTime.UtcNow, DateTimeKind.Utc);
        c.ReceivedBy = dto.ReceivedBy.Trim();
        c.DeliveryNotes = dto.DeliveryNotes;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ----- POD (proof-of-delivery) file -----

    [HttpPost("{id:int}/pod")]
    [RequestSizeLimit(MaxPodBytes)]
    public async Task<IActionResult> UploadPod(int id, [FromForm] PodUploadRequest request)
    {
        var c = await _db.Consignments.FindAsync(id);
        if (c is null) return NotFound();

        var file = request.File;
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > MaxPodBytes) return BadRequest("File exceeds the 10 MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPodExtensions.Contains(extension))
            return BadRequest($"Unsupported file type '{extension}'. Allowed: {string.Join(", ", AllowedPodExtensions)}");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "consignments", id.ToString());
        Directory.CreateDirectory(folder);

        DeletePodFile(c); // replace any existing POD

        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folder, storedFileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        c.PodFileName = file.FileName;
        c.PodContentType = file.ContentType;
        c.PodStoragePath = Path.Combine("uploads", "consignments", id.ToString(), storedFileName);
        c.PodFileSizeBytes = file.Length;
        c.PodUploadedAt = DateTime.UtcNow;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ConsignmentMapper.ToDetailDto(c));
    }

    [HttpGet("{id:int}/pod/download")]
    public async Task<IActionResult> DownloadPod(int id)
    {
        var c = await _db.Consignments.FindAsync(id);
        if (c is null || string.IsNullOrEmpty(c.PodStoragePath)) return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, c.PodStoragePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound("File missing on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, c.PodContentType ?? "application/octet-stream", c.PodFileName ?? $"{c.ConsignmentCode}-pod");
    }

    [HttpDelete("{id:int}/pod")]
    public async Task<IActionResult> DeletePod(int id)
    {
        var c = await _db.Consignments.FindAsync(id);
        if (c is null) return NotFound();
        if (string.IsNullOrEmpty(c.PodStoragePath)) return NoContent();

        DeletePodFile(c);
        c.PodFileName = null;
        c.PodContentType = null;
        c.PodStoragePath = null;
        c.PodFileSizeBytes = null;
        c.PodUploadedAt = null;
        c.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private void DeletePodFile(Consignment c)
    {
        if (string.IsNullOrEmpty(c.PodStoragePath)) return;
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, c.PodStoragePath);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }
}

public class PodUploadRequest
{
    public IFormFile File { get; set; } = null!;
}
