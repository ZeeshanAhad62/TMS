namespace TransportationSystemApi.Models;

public class AlertRule
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    // Which expiring document this alert relates to
    public DocumentCategory DocumentCategory { get; set; }

    // Days before expiry to notify (e.g. 30 / 15 / 7)
    public int ThresholdDays { get; set; }

    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;

    public string? RecipientRole { get; set; }

    public AlertStatus Status { get; set; } = AlertStatus.Pending;
}
