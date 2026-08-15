namespace TransportationSystemApi.Models;

public class CompanyProfile
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
