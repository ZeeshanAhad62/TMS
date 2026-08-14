using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace FleetMaster.Web.Services;

public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

public class FleetApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FleetApiClient(HttpClient http)
    {
        _http = http;
    }

    // ----- Vehicles -----

    public async Task<List<VehicleListItemDto>> GetVehiclesAsync(string? search = null, VehicleType? type = null, OperationalStatus? status = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (type.HasValue) query.Add($"vehicleType={type}");
        if (status.HasValue) query.Add($"status={status}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<VehicleListItemDto>>($"api/vehicles{qs}", JsonOptions) ?? new();
    }

    public async Task<VehicleDetailDto?> GetVehicleAsync(int id)
    {
        var response = await _http.GetAsync($"api/vehicles/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<VehicleDetailDto>(JsonOptions);
    }

    public async Task<VehicleDetailDto> CreateVehicleAsync(VehicleUpsertDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/vehicles", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<VehicleDetailDto>(JsonOptions))!;
    }

    public async Task UpdateVehicleAsync(int id, VehicleUpsertDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/vehicles/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteVehicleAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/vehicles/{id}");
        await EnsureSuccess(response);
    }

    // ----- Documents -----

    public async Task<VehicleDocumentDto> UploadDocumentAsync(int vehicleId, DocumentCategory category, string fileName, string contentType, Stream content)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(new StringContent(category.ToString()), "category");
        form.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync($"api/vehicles/{vehicleId}/documents", form);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<VehicleDocumentDto>(JsonOptions))!;
    }

    public async Task DeleteDocumentAsync(int vehicleId, int documentId)
    {
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/documents/{documentId}");
        await EnsureSuccess(response);
    }

    // ----- Alerts -----

    public async Task<AlertRuleDto> CreateAlertAsync(int vehicleId, AlertRuleUpsertDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/alerts", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<AlertRuleDto>(JsonOptions))!;
    }

    public async Task UpdateAlertAsync(int vehicleId, int alertId, AlertRuleUpsertDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/vehicles/{vehicleId}/alerts/{alertId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteAlertAsync(int vehicleId, int alertId)
    {
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/alerts/{alertId}");
        await EnsureSuccess(response);
    }

    // ----- Tyres -----

    public async Task<TyreDto> CreateTyreAsync(int vehicleId, TyreUpsertDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/tyres", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<TyreDto>(JsonOptions))!;
    }

    public async Task UpdateTyreAsync(int vehicleId, int tyreId, TyreUpsertDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/vehicles/{vehicleId}/tyres/{tyreId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteTyreAsync(int vehicleId, int tyreId)
    {
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/tyres/{tyreId}");
        await EnsureSuccess(response);
    }

    public async Task AddTyreReplacementAsync(int vehicleId, int tyreId, TyreReplacementHistoryUpsertDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/tyres/{tyreId}/replacements", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    // ----- Maintenance -----

    public async Task CreateMaintenanceRecordAsync(int vehicleId, MaintenanceRecordUpsertDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/maintenance", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteMaintenanceRecordAsync(int vehicleId, int recordId)
    {
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/maintenance/{recordId}");
        await EnsureSuccess(response);
    }

    // ----- Bookings -----

    public async Task CreateBookingAsync(int vehicleId, BookingRecordUpsertDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/bookings", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteBookingAsync(int vehicleId, int bookingId)
    {
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/bookings/{bookingId}");
        await EnsureSuccess(response);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new ApiException(string.IsNullOrWhiteSpace(body)
            ? $"Request failed with status {(int)response.StatusCode}."
            : body);
    }
}
