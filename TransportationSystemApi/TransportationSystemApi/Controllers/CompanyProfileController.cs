using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/company-profile")]
[Authorize]
public class CompanyProfileController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".svg" };
    private const long MaxLogoSizeBytes = 2 * 1024 * 1024;

    public CompanyProfileController(FleetDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<CompanyProfileDto>> Get()
    {
        var profile = await _db.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync();
        if (profile is null) return NotFound();
        return ToDto(profile);
    }

    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<CompanyProfileDto>> Update(CompanyProfileUpsertDto dto)
    {
        var profile = await _db.CompanyProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            profile = new CompanyProfile();
            _db.CompanyProfiles.Add(profile);
        }

        profile.CompanyName = dto.CompanyName;
        profile.Address = dto.Address;
        profile.ContactEmail = dto.ContactEmail;
        profile.ContactPhone = dto.ContactPhone;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(profile);
    }

    [HttpPost("logo")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [RequestSizeLimit(MaxLogoSizeBytes)]
    public async Task<ActionResult<CompanyProfileDto>> UploadLogo(IFormFile file)
    {
        var profile = await _db.CompanyProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            profile = new CompanyProfile { CompanyName = "My Company" };
            _db.CompanyProfiles.Add(profile);
        }

        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > MaxLogoSizeBytes) return BadRequest("Logo exceeds the 2 MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return BadRequest($"Unsupported file type '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "uploads", "company");
        Directory.CreateDirectory(folder);

        var storedFileName = $"logo-{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folder, storedFileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        profile.LogoPath = Path.Combine("uploads", "company", storedFileName);
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToDto(profile);
    }

    private static CompanyProfileDto ToDto(CompanyProfile p) => new()
    {
        Id = p.Id,
        CompanyName = p.CompanyName,
        LogoUrl = p.LogoPath is null ? null : $"/{p.LogoPath.Replace('\\', '/')}",
        Address = p.Address,
        ContactEmail = p.ContactEmail,
        ContactPhone = p.ContactPhone
    };
}
