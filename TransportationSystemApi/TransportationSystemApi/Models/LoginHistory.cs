namespace TransportationSystemApi.Models;

public class LoginHistory
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string AttemptedUsername { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public DateTime LoginAt { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }
    public string? DeviceId { get; set; }
    public string? UserAgent { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }

    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
}
