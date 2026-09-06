using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

// Read-only. Admin-only: the log records everyone's changes, including role
// edits, so viewing it is an oversight function.
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public AuditLogsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogPageDto>> GetAll(
        [FromQuery] string? entity,
        [FromQuery] string? action,
        [FromQuery] int? userId,
        [FromQuery] string? entityId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(a => a.EntityName == entity);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a =>
                a.EntityName.Contains(search) ||
                (a.UserName != null && a.UserName.Contains(search)) ||
                (a.ChangesJson != null && a.ChangesJson.Contains(search)));

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new AuditLogPageDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = rows.Select(ToListItem).ToList()
        };
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AuditLogDetailDto>> GetById(long id)
    {
        var row = await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (row is null) return NotFound();

        var dto = new AuditLogDetailDto
        {
            Id = row.Id, Timestamp = row.Timestamp, UserId = row.UserId, UserName = row.UserName,
            Action = row.Action, EntityName = row.EntityName, EntityId = row.EntityId,
            Summary = Summarize(row), ChangesJson = row.ChangesJson
        };
        return dto;
    }

    [HttpGet("facets")]
    public async Task<ActionResult<AuditLogFacetsDto>> Facets()
    {
        return new AuditLogFacetsDto
        {
            EntityNames = await _db.AuditLogs.AsNoTracking().Select(a => a.EntityName).Distinct().OrderBy(n => n).ToListAsync(),
            Actions = await _db.AuditLogs.AsNoTracking().Select(a => a.Action).Distinct().OrderBy(n => n).ToListAsync()
        };
    }

    private static AuditLogListItemDto ToListItem(AuditLog a) => new()
    {
        Id = a.Id, Timestamp = a.Timestamp, UserId = a.UserId, UserName = a.UserName,
        Action = a.Action, EntityName = a.EntityName, EntityId = a.EntityId,
        Summary = Summarize(a)
    };

    private static string Summarize(AuditLog a)
    {
        if (a.Action == "Insert") return "created";
        if (a.Action == "Delete") return "deleted";
        if (a.Action == "Update" && !string.IsNullOrWhiteSpace(a.ChangesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(a.ChangesJson);
                var fields = doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();
                return fields.Count == 0 ? "updated" : "changed " + string.Join(", ", fields);
            }
            catch (JsonException) { /* fall through */ }
        }
        return a.Action.ToLowerInvariant();
    }
}
