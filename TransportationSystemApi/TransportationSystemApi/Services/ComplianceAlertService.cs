using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Services;

// Scans vehicles/drivers via ComplianceScanner, matches each expiring item
// against the active AlertConfigs, and emails the ones not already logged
// for that (entity, document, expiry, severity) combo. Scoped so it can be
// resolved both from the daily hosted service and from a manual "run now"
// controller action.
public class ComplianceAlertService
{
    private readonly FleetDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<ComplianceAlertService> _logger;

    public ComplianceAlertService(FleetDbContext db, IEmailSender email, ILogger<ComplianceAlertService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<AlertRunResultDto> RunScanAsync(CancellationToken ct = default)
    {
        var result = new AlertRunResultDto { RanAt = DateTime.UtcNow };

        var configs = await _db.AlertConfigs.AsNoTracking().Where(c => c.IsActive).ToListAsync(ct);
        if (configs.Count == 0) return result;

        var vehicles = await _db.Vehicles.AsNoTracking().ToListAsync(ct);
        var drivers = await _db.Drivers.AsNoTracking().ToListAsync(ct);
        var maxWindow = configs.Max(c => c.ThresholdDays);
        var items = ComplianceScanner.Scan(vehicles, drivers, maxWindow);
        result.ItemsScanned = items.Count;

        foreach (var config in configs)
        {
            var matches = items.Where(i =>
                i.DaysRemaining <= config.ThresholdDays &&
                (config.EntityType is null || i.EntityType == config.EntityType) &&
                (string.IsNullOrWhiteSpace(config.DocumentType) || i.DocumentType == config.DocumentType));

            foreach (var item in matches)
            {
                var alreadySent = await _db.AlertLog.AnyAsync(l =>
                    l.EntityType == item.EntityType && l.EntityId == item.EntityId &&
                    l.DocumentType == item.DocumentType && l.ExpiryDate == item.ExpiryDate &&
                    l.Severity == item.Severity, ct);
                if (alreadySent) continue;

                var recipients = config.RecipientEmails
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (recipients.Length == 0) continue;

                var subject = $"[Compliance] {item.DocumentType} for {item.EntityName} — {item.Severity}";
                var body = $"{item.EntityType} {item.EntityCode} ({item.EntityName})'s {item.DocumentType} " +
                    (item.DaysRemaining < 0
                        ? $"expired {-item.DaysRemaining} day(s) ago"
                        : $"expires in {item.DaysRemaining} day(s)") +
                    $" on {item.ExpiryDate:dd-MMM-yyyy}.";

                var sent = await _email.TrySendAsync(recipients, subject, body, ct);
                if (!sent) continue;

                _db.AlertLog.Add(new AlertLog
                {
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    DocumentType = item.DocumentType,
                    ExpiryDate = item.ExpiryDate,
                    Severity = item.Severity,
                    RecipientEmails = config.RecipientEmails
                });
                result.AlertsSent++;
            }
        }

        if (result.AlertsSent > 0)
            await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Compliance alert scan: {Scanned} item(s) scanned, {Sent} alert(s) sent.",
            result.ItemsScanned, result.AlertsSent);

        return result;
    }
}
