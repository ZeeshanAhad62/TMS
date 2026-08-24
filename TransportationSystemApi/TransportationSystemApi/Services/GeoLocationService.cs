using System.Net;
using System.Text.Json.Serialization;

namespace TransportationSystemApi.Services;

public class GeoLocationService
{
    private readonly HttpClient _http;

    public GeoLocationService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(3);
    }

    public async Task<(string? City, string? Region, string? Country)> LookupAsync(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || IsPrivateOrLoopback(ipAddress))
            return (null, null, null);

        try
        {
            var result = await _http.GetFromJsonAsync<IpApiResponse>(
                $"http://ip-api.com/json/{ipAddress}?fields=status,city,regionName,country");

            if (result?.Status == "success")
                return (result.City, result.RegionName, result.Country);
        }
        catch
        {
            // Best-effort only -- login must never fail because geolocation is down.
        }

        return (null, null, null);
    }

    private static bool IsPrivateOrLoopback(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip)) return true;
        if (IPAddress.IsLoopback(ip)) return true;

        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }

        return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal;
    }

    private class IpApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("regionName")]
        public string? RegionName { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}
