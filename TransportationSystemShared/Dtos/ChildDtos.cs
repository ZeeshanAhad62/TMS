using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class VehicleDocumentDto
{
    public int Id { get; set; }
    public DocumentCategory Category { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class AlertRuleDto
{
    public int Id { get; set; }
    public DocumentCategory DocumentCategory { get; set; }
    public int ThresholdDays { get; set; }
    public NotificationChannel Channel { get; set; }
    public string? RecipientRole { get; set; }
    public AlertStatus Status { get; set; }
}

public class AlertRuleUpsertDto
{
    [Required]
    public DocumentCategory DocumentCategory { get; set; }

    [Range(1, 365)]
    public int ThresholdDays { get; set; } = 30;

    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string? RecipientRole { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Pending;
}

public class TyreDto
{
    public int Id { get; set; }
    public TyrePosition Position { get; set; }
    public string? BrandAndSize { get; set; }
    public DateOnly? InstallationDate { get; set; }
    public decimal? InstallationOdometer { get; set; }
    public string? CurrentCondition { get; set; }
    public DateOnly? LastRotationDate { get; set; }
    public List<TyreReplacementHistoryDto> ReplacementHistory { get; set; } = new();
}

public class TyreUpsertDto
{
    [Required]
    public TyrePosition Position { get; set; }
    public string? BrandAndSize { get; set; }
    public DateOnly? InstallationDate { get; set; }
    public decimal? InstallationOdometer { get; set; }
    public string? CurrentCondition { get; set; }
    public DateOnly? LastRotationDate { get; set; }
}

public class TyreReplacementHistoryDto
{
    public int Id { get; set; }
    public DateOnly ReplacedDate { get; set; }
    public decimal? OdometerAtReplacement { get; set; }
    public string? OldBrandAndSize { get; set; }
    public string? NewBrandAndSize { get; set; }
    public string? Reason { get; set; }
}

public class TyreReplacementHistoryUpsertDto
{
    [Required]
    public DateOnly ReplacedDate { get; set; }
    public decimal? OdometerAtReplacement { get; set; }
    public string? OldBrandAndSize { get; set; }
    public string? NewBrandAndSize { get; set; }
    public string? Reason { get; set; }
}

public class MaintenanceRecordDto
{
    public int Id { get; set; }
    public MaintenanceType Type { get; set; }
    public DateOnly Date { get; set; }
    public decimal? Odometer { get; set; }
    public string? Description { get; set; }
    public string? ServiceVendor { get; set; }
    public decimal? Cost { get; set; }
}

public class MaintenanceRecordUpsertDto
{
    [Required]
    public MaintenanceType Type { get; set; }

    [Required]
    public DateOnly Date { get; set; }
    public decimal? Odometer { get; set; }
    public string? Description { get; set; }
    public string? ServiceVendor { get; set; }
    public decimal? Cost { get; set; }
}
