using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/drivers/{driverId:int}/documents")]
public class DriverDocumentsController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public DriverDocumentsController(FleetDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(int driverId, [FromForm] DriverDocumentUploadRequest request)
    {
        var (category, file) = (request.Category, request.File);

        var driver = await _db.Drivers.FindAsync(driverId);
        if (driver is null) return NotFound("Driver not found.");

        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > MaxFileSizeBytes) return BadRequest("File exceeds the 10 MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Unsupported file type '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "drivers", driverId.ToString());
        Directory.CreateDirectory(folder);

        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folder, storedFileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var doc = new DriverDocument
        {
            DriverId = driverId,
            Category = category,
            FileName = file.FileName,
            ContentType = file.ContentType,
            StoragePath = Path.Combine("uploads", "drivers", driverId.ToString(), storedFileName),
            FileSizeBytes = file.Length
        };

        _db.DriverDocuments.Add(doc);
        await _db.SaveChangesAsync();

        return Ok(DriverMapper.ToDto(doc));
    }

    [HttpGet("{documentId:int}/download")]
    public async Task<IActionResult> Download(int driverId, int documentId)
    {
        var doc = await _db.DriverDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.DriverId == driverId);
        if (doc is null) return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, doc.StoragePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound("File missing on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, doc.ContentType, doc.FileName);
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> Delete(int driverId, int documentId)
    {
        var doc = await _db.DriverDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.DriverId == driverId);
        if (doc is null) return NotFound();

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var fullPath = Path.Combine(webRoot, doc.StoragePath);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

        _db.DriverDocuments.Remove(doc);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class DriverDocumentUploadRequest
{
    public DriverDocumentCategory Category { get; set; }
    public IFormFile File { get; set; } = null!;
}
