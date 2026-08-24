using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransportationSystemApi.Dtos;

namespace TransportationSystemWeb.Services;

// Separate from FleetApiClient: used only for the anonymous /auth/login endpoint,
// so it must not carry BearerTokenHandler (which needs a Razor component DI scope).
public class AuthApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<LoginResponseDto?> LoginAsync(string username, string password, string? ipAddress, string? deviceId, string? userAgent)
    {
        var dto = new LoginRequestDto
        {
            Username = username,
            Password = password,
            IpAddress = ipAddress,
            DeviceId = deviceId,
            UserAgent = userAgent
        };
        var response = await _http.PostAsJsonAsync("api/auth/login", dto, JsonOptions);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
    }
}
