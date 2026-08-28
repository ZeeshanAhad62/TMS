using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemWeb.Services;

public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

// Note: the Authorization header is set here in the typed client itself, not in a
// DelegatingHandler on the HttpClientFactory pipeline. Handlers registered via
// AddHttpMessageHandler run in a pooled scope outside the current circuit, so
// AuthenticationStateProvider.GetAuthenticationStateAsync() throws there. Typed
// clients, in contrast, are constructed in the caller's (circuit) DI scope.
public class FleetApiClient
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FleetApiClient(HttpClient http, AuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
    }

    private async Task AuthorizeAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var token = state.User.FindFirst("access_token")?.Value;
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    // ----- Company Profile -----

    public async Task<CompanyProfileDto?> GetCompanyProfileAsync()
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync("api/company-profile");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);
    }

    public async Task<CompanyProfileDto> UpdateCompanyProfileAsync(CompanyProfileUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync("api/company-profile", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions))!;
    }

    public async Task<CompanyProfileDto> UploadCompanyLogoAsync(string fileName, string contentType, Stream content)
    {
        await AuthorizeAsync();
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync("api/company-profile/logo", form);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions))!;
    }

    // ----- Users -----

    public async Task<List<UserDto>> GetUsersAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<UserDto>>("api/users", JsonOptions) ?? new();
    }

    public async Task<UserDto> CreateUserAsync(UserCreateDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/users", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;
    }

    public async Task DeactivateUserAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsync($"api/users/{id}/deactivate", null);
        await EnsureSuccess(response);
    }

    public async Task ActivateUserAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsync($"api/users/{id}/activate", null);
        await EnsureSuccess(response);
    }

    // ----- Login History -----

    public async Task<List<LoginHistoryDto>> GetLoginHistoryAsync(int? userId = null, int limit = 100)
    {
        await AuthorizeAsync();
        var qs = userId.HasValue ? $"?userId={userId}&limit={limit}" : $"?limit={limit}";
        return await _http.GetFromJsonAsync<List<LoginHistoryDto>>($"api/login-history{qs}", JsonOptions) ?? new();
    }

    // ----- Vehicles -----

    public async Task<List<VehicleListItemDto>> GetVehiclesAsync(string? search = null, VehicleType? type = null, OperationalStatus? status = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (type.HasValue) query.Add($"vehicleType={type}");
        if (status.HasValue) query.Add($"status={status}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<VehicleListItemDto>>($"api/vehicles{qs}", JsonOptions) ?? new();
    }

    public async Task<VehicleDetailDto?> GetVehicleAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/vehicles/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<VehicleDetailDto>(JsonOptions);
    }

    public async Task<VehicleDetailDto> CreateVehicleAsync(VehicleUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/vehicles", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<VehicleDetailDto>(JsonOptions))!;
    }

    public async Task UpdateVehicleAsync(int id, VehicleUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/vehicles/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteVehicleAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/vehicles/{id}");
        await EnsureSuccess(response);
    }

    // ----- Documents -----

    public async Task<VehicleDocumentDto> UploadDocumentAsync(int vehicleId, DocumentCategory category, string fileName, string contentType, Stream content)
    {
        await AuthorizeAsync();
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
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/documents/{documentId}");
        await EnsureSuccess(response);
    }

    // ----- Alerts -----

    public async Task<AlertRuleDto> CreateAlertAsync(int vehicleId, AlertRuleUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/alerts", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<AlertRuleDto>(JsonOptions))!;
    }

    public async Task UpdateAlertAsync(int vehicleId, int alertId, AlertRuleUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/vehicles/{vehicleId}/alerts/{alertId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteAlertAsync(int vehicleId, int alertId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/alerts/{alertId}");
        await EnsureSuccess(response);
    }

    // ----- Tyres -----

    public async Task<TyreDto> CreateTyreAsync(int vehicleId, TyreUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/tyres", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<TyreDto>(JsonOptions))!;
    }

    public async Task UpdateTyreAsync(int vehicleId, int tyreId, TyreUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/vehicles/{vehicleId}/tyres/{tyreId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteTyreAsync(int vehicleId, int tyreId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/tyres/{tyreId}");
        await EnsureSuccess(response);
    }

    public async Task AddTyreReplacementAsync(int vehicleId, int tyreId, TyreReplacementHistoryUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/tyres/{tyreId}/replacements", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    // ----- Maintenance -----

    public async Task CreateMaintenanceRecordAsync(int vehicleId, MaintenanceRecordUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/vehicles/{vehicleId}/maintenance", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteMaintenanceRecordAsync(int vehicleId, int recordId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/vehicles/{vehicleId}/maintenance/{recordId}");
        await EnsureSuccess(response);
    }

    // ----- Trips -----

    public async Task<List<TripListItemDto>> GetTripsAsync(int? vehicleId = null, int? driverId = null, TripStatus? status = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (vehicleId.HasValue) query.Add($"vehicleId={vehicleId}");
        if (driverId.HasValue) query.Add($"driverId={driverId}");
        if (status.HasValue) query.Add($"status={status}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<TripListItemDto>>($"api/trips{qs}", JsonOptions) ?? new();
    }

    public async Task<TripDetailDto?> GetTripAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/trips/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TripDetailDto>(JsonOptions);
    }

    public async Task<TripDetailDto> CreateTripAsync(TripUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/trips", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<TripDetailDto>(JsonOptions))!;
    }

    public async Task UpdateTripAsync(int id, TripUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/trips/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteTripAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/trips/{id}");
        await EnsureSuccess(response);
    }

    // ----- Work Orders (Maintenance & Workshop) -----

    public async Task<List<WorkOrderListItemDto>> GetWorkOrdersAsync(int? vehicleId = null, WorkOrderStatus? status = null, MaintenanceType? type = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (vehicleId.HasValue) query.Add($"vehicleId={vehicleId}");
        if (status.HasValue) query.Add($"status={status}");
        if (type.HasValue) query.Add($"type={type}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<WorkOrderListItemDto>>($"api/workorders{qs}", JsonOptions) ?? new();
    }

    public async Task<WorkOrderDetailDto?> GetWorkOrderAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/workorders/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<WorkOrderDetailDto>(JsonOptions);
    }

    public async Task<WorkOrderDetailDto> CreateWorkOrderAsync(WorkOrderUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/workorders", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<WorkOrderDetailDto>(JsonOptions))!;
    }

    public async Task UpdateWorkOrderAsync(int id, WorkOrderUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/workorders/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteWorkOrderAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/workorders/{id}");
        await EnsureSuccess(response);
    }

    public async Task<WorkOrderItemDto> CreateWorkOrderItemAsync(int workOrderId, WorkOrderItemUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/workorders/{workOrderId}/items", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<WorkOrderItemDto>(JsonOptions))!;
    }

    public async Task UpdateWorkOrderItemAsync(int workOrderId, int itemId, WorkOrderItemUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/workorders/{workOrderId}/items/{itemId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteWorkOrderItemAsync(int workOrderId, int itemId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/workorders/{workOrderId}/items/{itemId}");
        await EnsureSuccess(response);
    }

    // ----- Drivers -----

    public async Task<List<DriverListItemDto>> GetDriversAsync(string? search = null, DriverStatus? status = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (status.HasValue) query.Add($"status={status}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<DriverListItemDto>>($"api/drivers{qs}", JsonOptions) ?? new();
    }

    public async Task<DriverDetailDto?> GetDriverAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/drivers/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DriverDetailDto>(JsonOptions);
    }

    public async Task<DriverDetailDto> CreateDriverAsync(DriverUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/drivers", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<DriverDetailDto>(JsonOptions))!;
    }

    public async Task UpdateDriverAsync(int id, DriverUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/drivers/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteDriverAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/drivers/{id}");
        await EnsureSuccess(response);
    }

    // ----- Driver Documents -----

    public async Task<DriverDocumentDto> UploadDriverDocumentAsync(int driverId, DriverDocumentCategory category, string fileName, string contentType, Stream content)
    {
        await AuthorizeAsync();
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(new StringContent(category.ToString()), "category");
        form.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync($"api/drivers/{driverId}/documents", form);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<DriverDocumentDto>(JsonOptions))!;
    }

    public async Task DeleteDriverDocumentAsync(int driverId, int documentId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/drivers/{driverId}/documents/{documentId}");
        await EnsureSuccess(response);
    }

    // ----- Driver Assignments -----

    public async Task<DriverVehicleAssignmentDto> CreateAssignmentAsync(int driverId, DriverVehicleAssignmentUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/drivers/{driverId}/assignments", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<DriverVehicleAssignmentDto>(JsonOptions))!;
    }

    public async Task UpdateAssignmentAsync(int driverId, int assignmentId, DriverVehicleAssignmentUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/drivers/{driverId}/assignments/{assignmentId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteAssignmentAsync(int driverId, int assignmentId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/drivers/{driverId}/assignments/{assignmentId}");
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
