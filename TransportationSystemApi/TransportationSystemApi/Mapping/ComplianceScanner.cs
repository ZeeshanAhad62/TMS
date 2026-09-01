using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

// Walks the structured expiry fields already stored on vehicles and drivers
// and produces one flat, severity-graded list. Static so the (future) daily
// notification job can reuse it without DI.
public static class ComplianceScanner
{
    public const int DefaultWindowDays = 60;

    public static List<ComplianceItemDto> Scan(
        IEnumerable<Vehicle> vehicles,
        IEnumerable<Driver> drivers,
        int withinDays = DefaultWindowDays)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = new List<ComplianceItemDto>();

        foreach (var v in vehicles)
        {
            foreach (var (label, date) in VehicleDocuments(v))
            {
                if (date is null) continue;
                var days = date.Value.DayNumber - today.DayNumber;
                if (days > withinDays) continue;

                items.Add(new ComplianceItemDto
                {
                    EntityType = ComplianceEntityType.Vehicle,
                    EntityId = v.Id,
                    EntityCode = v.VehicleCode,
                    EntityName = v.RegistrationNumber,
                    DocumentType = label,
                    ExpiryDate = date.Value,
                    DaysRemaining = days,
                    Severity = Grade(days)
                });
            }
        }

        foreach (var d in drivers)
        {
            if (d.LicenseExpiryDate is null) continue;
            var days = d.LicenseExpiryDate.Value.DayNumber - today.DayNumber;
            if (days > withinDays) continue;

            items.Add(new ComplianceItemDto
            {
                EntityType = ComplianceEntityType.Driver,
                EntityId = d.Id,
                EntityCode = d.DriverCode,
                EntityName = d.FullName,
                DocumentType = "Driving License",
                ExpiryDate = d.LicenseExpiryDate.Value,
                DaysRemaining = days,
                Severity = Grade(days)
            });
        }

        return items.OrderBy(i => i.DaysRemaining).ToList();
    }

    public static ComplianceSummaryDto Summarise(List<ComplianceItemDto> items) => new()
    {
        Expired = items.Count(i => i.Severity == ComplianceSeverity.Expired),
        Critical = items.Count(i => i.Severity == ComplianceSeverity.Critical),
        Warning = items.Count(i => i.Severity == ComplianceSeverity.Warning),
        Upcoming = items.Count(i => i.Severity == ComplianceSeverity.Upcoming),
        Total = items.Count,
        VehicleItems = items.Count(i => i.EntityType == ComplianceEntityType.Vehicle),
        DriverItems = items.Count(i => i.EntityType == ComplianceEntityType.Driver)
    };

    private static ComplianceSeverity Grade(int days) => days switch
    {
        < 0 => ComplianceSeverity.Expired,
        <= 7 => ComplianceSeverity.Critical,
        <= 30 => ComplianceSeverity.Warning,
        _ => ComplianceSeverity.Upcoming
    };

    private static IEnumerable<(string Label, DateOnly? Date)> VehicleDocuments(Vehicle v) => new (string, DateOnly?)[]
    {
        ("Registration Certificate", v.RCExpiryDate),
        ("Fitness Certificate", v.FitnessExpiryDate),
        ("Route Permit", v.PermitExpiryDate),
        ("Insurance", v.InsuranceExpiryDate),
        ("Pollution Certificate", v.PollutionCertExpiryDate),
        ("Road Tax", v.TaxPaidTill)
    };
}
