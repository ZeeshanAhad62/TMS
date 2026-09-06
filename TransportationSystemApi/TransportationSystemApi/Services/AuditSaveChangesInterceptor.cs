using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TransportationSystemApi.Data;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Services;

// Writes one dbo.AuditLog row per inserted / updated / deleted entity on every
// SaveChanges. Two-phase: snapshot in SavingChanges (Ids not yet assigned for
// inserts), then build + persist the audit rows in SavedChanges once real Ids
// exist. A re-entrancy flag stops the audit write from auditing itself.
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _http;

    private static readonly HashSet<string> Skip = new()
    {
        nameof(AuditLog), nameof(LoginHistory), nameof(VehiclePosition), nameof(GeofenceEvent)
    };
    private const int MaxValueChars = 512;

    private readonly List<PendingEntry> _pending = new();
    private bool _writing;

    public AuditSaveChangesInterceptor(IHttpContextAccessor http)
    {
        _http = http;
    }

    private sealed record PendingEntry(EntityEntry Entry, string Action, Dictionary<string, object?>? OldValues, Dictionary<string, object?>? Snapshot);

    // ----- snapshot -----

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void Capture(DbContext? context)
    {
        _pending.Clear();
        if (_writing || context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var name = entry.Metadata.ClrType.Name;
            if (Skip.Contains(name)) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            switch (entry.State)
            {
                case EntityState.Modified:
                    var changed = entry.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)).ToList();
                    if (changed.Count == 0) break;
                    _pending.Add(new PendingEntry(entry, "Update",
                        changed.ToDictionary(p => p.Metadata.Name, p => Trim(p.OriginalValue)), null));
                    break;

                case EntityState.Added:
                    _pending.Add(new PendingEntry(entry, "Insert", null, null));
                    break;

                case EntityState.Deleted:
                    _pending.Add(new PendingEntry(entry, "Delete", null,
                        entry.Properties.ToDictionary(p => p.Metadata.Name, p => Trim(p.OriginalValue))));
                    break;
            }
        }
    }

    // ----- write -----

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        WriteAudit(eventData.Context, async: false).GetAwaiter().GetResult();
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await WriteAudit(eventData.Context, async: true, cancellationToken);
        return result;
    }

    private async Task WriteAudit(DbContext? context, bool async, CancellationToken ct = default)
    {
        if (_writing || context is null || _pending.Count == 0) return;

        var (userId, userName) = CurrentUser();
        var now = DateTime.UtcNow;
        var logs = new List<AuditLog>(_pending.Count);

        foreach (var p in _pending)
        {
            var entityName = p.Entry.Metadata.ClrType.Name;
            var entityId = PrimaryKey(p.Entry);

            string? json = p.Action switch
            {
                "Update" => JsonSerializer.Serialize(
                    p.OldValues!.ToDictionary(kv => kv.Key, kv => new
                    {
                        old = kv.Value,
                        @new = Trim(p.Entry.Property(kv.Key).CurrentValue)
                    })),
                "Insert" => JsonSerializer.Serialize(
                    p.Entry.Properties.ToDictionary(x => x.Metadata.Name, x => Trim(x.CurrentValue))),
                "Delete" => JsonSerializer.Serialize(p.Snapshot),
                _ => null
            };

            logs.Add(new AuditLog
            {
                Timestamp = now,
                UserId = userId,
                UserName = userName,
                Action = p.Action,
                EntityName = entityName,
                EntityId = entityId,
                ChangesJson = json
            });
        }

        _pending.Clear();

        _writing = true;
        try
        {
            context.Set<AuditLog>().AddRange(logs);
            if (async) await context.SaveChangesAsync(ct);
            else context.SaveChanges();
        }
        finally
        {
            _writing = false;
        }
    }

    private (int? UserId, string? UserName) CurrentUser()
    {
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return (null, null);

        int? id = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
        var name = user.FindFirst(ClaimTypes.Name)?.Value
                   ?? user.FindFirst("driverCode")?.Value;
        return (id, name);
    }

    private static string? PrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return null;
        var parts = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "");
        return string.Join(",", parts);
    }

    private static object? Trim(object? value)
    {
        if (value is string s && s.Length > MaxValueChars) return s[..MaxValueChars] + "…";
        return value;
    }
}
