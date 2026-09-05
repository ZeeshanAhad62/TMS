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

    public async Task<List<TripListItemDto>> GetTripsAsync(int? vehicleId = null, int? driverId = null, TripStatus? status = null, string? search = null, int? customerId = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (vehicleId.HasValue) query.Add($"vehicleId={vehicleId}");
        if (driverId.HasValue) query.Add($"driverId={driverId}");
        if (customerId.HasValue) query.Add($"customerId={customerId}");
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

    // ----- Trip Expenses -----

    public async Task<List<TripExpenseDto>> GetTripExpensesAsync(int tripId)
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<TripExpenseDto>>($"api/trips/{tripId}/expenses", JsonOptions) ?? new();
    }

    public async Task<TripExpenseDto> CreateTripExpenseAsync(int tripId, TripExpenseUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/trips/{tripId}/expenses", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<TripExpenseDto>(JsonOptions))!;
    }

    public async Task UpdateTripExpenseAsync(int tripId, int expenseId, TripExpenseUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/trips/{tripId}/expenses/{expenseId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteTripExpenseAsync(int tripId, int expenseId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/trips/{tripId}/expenses/{expenseId}");
        await EnsureSuccess(response);
    }

    // ----- Invoices (Billing & A/R) -----

    public async Task<List<InvoiceListItemDto>> GetInvoicesAsync(int? customerId = null, InvoiceStatus? status = null, bool? overdue = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (customerId.HasValue) query.Add($"customerId={customerId}");
        if (status.HasValue) query.Add($"status={status}");
        if (overdue == true) query.Add("overdue=true");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<InvoiceListItemDto>>($"api/invoices{qs}", JsonOptions) ?? new();
    }

    public async Task<InvoiceDetailDto?> GetInvoiceAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/invoices/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InvoiceDetailDto>(JsonOptions);
    }

    public async Task<InvoiceDetailDto> CreateInvoiceAsync(InvoiceUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/invoices", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<InvoiceDetailDto>(JsonOptions))!;
    }

    public async Task<InvoiceDetailDto> CreateInvoiceFromTripsAsync(CreateInvoiceFromTripsDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/invoices/from-trips", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<InvoiceDetailDto>(JsonOptions))!;
    }

    public async Task UpdateInvoiceAsync(int id, InvoiceUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/invoices/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteInvoiceAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/invoices/{id}");
        await EnsureSuccess(response);
    }

    public async Task<List<BillableTripDto>> GetBillableTripsAsync(int customerId)
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<BillableTripDto>>($"api/invoices/billable-trips?customerId={customerId}", JsonOptions) ?? new();
    }

    public async Task<InvoiceAgingDto?> GetInvoiceAgingAsync()
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync("api/invoices/aging");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InvoiceAgingDto>(JsonOptions);
    }

    public async Task<InvoiceLineDto> CreateInvoiceLineAsync(int invoiceId, InvoiceLineUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/invoices/{invoiceId}/lines", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<InvoiceLineDto>(JsonOptions))!;
    }

    public async Task UpdateInvoiceLineAsync(int invoiceId, int lineId, InvoiceLineUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/invoices/{invoiceId}/lines/{lineId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteInvoiceLineAsync(int invoiceId, int lineId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/invoices/{invoiceId}/lines/{lineId}");
        await EnsureSuccess(response);
    }

    public async Task<PaymentDto> CreatePaymentAsync(int invoiceId, PaymentUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/invoices/{invoiceId}/payments", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<PaymentDto>(JsonOptions))!;
    }

    public async Task UpdatePaymentAsync(int invoiceId, int paymentId, PaymentUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/invoices/{invoiceId}/payments/{paymentId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeletePaymentAsync(int invoiceId, int paymentId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/invoices/{invoiceId}/payments/{paymentId}");
        await EnsureSuccess(response);
    }

    // ----- Customers -----

    public async Task<List<CustomerListItemDto>> GetCustomersAsync(CustomerStatus? status = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (status.HasValue) query.Add($"status={status}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<CustomerListItemDto>>($"api/customers{qs}", JsonOptions) ?? new();
    }

    public async Task<CustomerDetailDto?> GetCustomerAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/customers/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);
    }

    public async Task<CustomerDetailDto> CreateCustomerAsync(CustomerUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/customers", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions))!;
    }

    public async Task UpdateCustomerAsync(int id, CustomerUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/customers/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteCustomerAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/customers/{id}");
        await EnsureSuccess(response);
    }

    // ----- Fuel Entries -----

    public async Task<List<FuelEntryListItemDto>> GetFuelEntriesAsync(
        int? vehicleId = null, int? driverId = null, int? tripId = null,
        FuelType? fuelType = null, DateOnly? from = null, DateOnly? to = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (vehicleId.HasValue) query.Add($"vehicleId={vehicleId}");
        if (driverId.HasValue) query.Add($"driverId={driverId}");
        if (tripId.HasValue) query.Add($"tripId={tripId}");
        if (fuelType.HasValue) query.Add($"fuelType={fuelType}");
        if (from.HasValue) query.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<FuelEntryListItemDto>>($"api/fuel-entries{qs}", JsonOptions) ?? new();
    }

    public async Task<FuelEntryDetailDto?> GetFuelEntryAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/fuel-entries/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<FuelEntryDetailDto>(JsonOptions);
    }

    public async Task<FuelEntryDetailDto> CreateFuelEntryAsync(FuelEntryUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/fuel-entries", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<FuelEntryDetailDto>(JsonOptions))!;
    }

    public async Task UpdateFuelEntryAsync(int id, FuelEntryUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/fuel-entries/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteFuelEntryAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/fuel-entries/{id}");
        await EnsureSuccess(response);
    }

    // ----- Compliance -----

    public async Task<List<ComplianceItemDto>> GetComplianceExpiriesAsync(
        ComplianceEntityType? entityType = null, ComplianceSeverity? severity = null, int withinDays = 60, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string> { $"withinDays={withinDays}" };
        if (entityType.HasValue) query.Add($"entityType={entityType}");
        if (severity.HasValue) query.Add($"severity={severity}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = "?" + string.Join("&", query);

        return await _http.GetFromJsonAsync<List<ComplianceItemDto>>($"api/compliance/expiries{qs}", JsonOptions) ?? new();
    }

    public async Task<ComplianceSummaryDto?> GetComplianceSummaryAsync(int withinDays = 60)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/compliance/summary?withinDays={withinDays}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ComplianceSummaryDto>(JsonOptions);
    }

    public async Task<string[]> GetComplianceDocumentTypesAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<string[]>("api/compliance/document-types", JsonOptions) ?? Array.Empty<string>();
    }

    // ----- Compliance Alert Config -----

    public async Task<List<AlertConfigDto>> GetAlertConfigsAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<AlertConfigDto>>("api/compliance/config", JsonOptions) ?? new();
    }

    public async Task<AlertConfigDto> CreateAlertConfigAsync(AlertConfigUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/compliance/config", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<AlertConfigDto>(JsonOptions))!;
    }

    public async Task UpdateAlertConfigAsync(int id, AlertConfigUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/compliance/config/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteAlertConfigAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/compliance/config/{id}");
        await EnsureSuccess(response);
    }

    public async Task<List<AlertLogDto>> GetAlertLogAsync(int take = 50)
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<AlertLogDto>>($"api/compliance/alert-log?take={take}", JsonOptions) ?? new();
    }

    public async Task<AlertRunResultDto> RunAlertsNowAsync()
    {
        await AuthorizeAsync();
        var response = await _http.PostAsync("api/compliance/run-alerts", null);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<AlertRunResultDto>(JsonOptions))!;
    }

    // ----- Tyre Management (module 6, top-level asset module) -----

    public async Task<List<TyreListItemDto>> GetTyresAsync(TyreStatus? status = null, int? vehicleId = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (status.HasValue) query.Add($"status={status}");
        if (vehicleId.HasValue) query.Add($"vehicleId={vehicleId}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";

        return await _http.GetFromJsonAsync<List<TyreListItemDto>>($"api/tyres{qs}", JsonOptions) ?? new();
    }

    public async Task<List<TyreListItemDto>> GetTyreStockAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<TyreListItemDto>>("api/tyres/stock", JsonOptions) ?? new();
    }

    public async Task<TyreDetailDto?> GetTyreAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/tyres/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TyreDetailDto>(JsonOptions);
    }

    public async Task<TyreDetailDto> CreateTyreRecordAsync(TyreCreateDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/tyres", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<TyreDetailDto>(JsonOptions))!;
    }

    public async Task UpdateTyreRecordAsync(int id, TyreCreateDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/tyres/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteTyreRecordAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/tyres/{id}");
        await EnsureSuccess(response);
    }

    public async Task<TyreEventDto> AddTyreEventAsync(int tyreId, TyreEventUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/tyres/{tyreId}/events", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<TyreEventDto>(JsonOptions))!;
    }

    public async Task DeleteTyreEventAsync(int tyreId, int eventId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/tyres/{tyreId}/events/{eventId}");
        await EnsureSuccess(response);
    }

    // ----- Spare Parts Inventory (module 7) -----

    public async Task<List<PartListItemDto>> GetPartsAsync(string? search = null)
    {
        await AuthorizeAsync();
        var qs = string.IsNullOrWhiteSpace(search) ? "" : $"?search={Uri.EscapeDataString(search)}";
        return await _http.GetFromJsonAsync<List<PartListItemDto>>($"api/parts{qs}", JsonOptions) ?? new();
    }

    public async Task<List<PartListItemDto>> GetLowStockPartsAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<PartListItemDto>>("api/parts/low-stock", JsonOptions) ?? new();
    }

    public async Task<PartDetailDto?> GetPartAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/parts/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PartDetailDto>(JsonOptions);
    }

    public async Task<PartDetailDto> CreatePartAsync(PartUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/parts", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<PartDetailDto>(JsonOptions))!;
    }

    public async Task UpdatePartAsync(int id, PartUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/parts/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeletePartAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/parts/{id}");
        await EnsureSuccess(response);
    }

    public async Task<StockMovementDto> AddStockMovementAsync(int partId, StockMovementUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/parts/{partId}/movements", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<StockMovementDto>(JsonOptions))!;
    }

    public async Task DeleteStockMovementAsync(int partId, int movementId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/parts/{partId}/movements/{movementId}");
        await EnsureSuccess(response);
    }

    // ----- Driver Payroll (module 8) -----

    public async Task<List<DriverAdvanceDto>> GetDriverAdvancesAsync(int driverId)
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<DriverAdvanceDto>>($"api/drivers/{driverId}/advances", JsonOptions) ?? new();
    }

    public async Task<DriverAdvanceDto> CreateDriverAdvanceAsync(int driverId, DriverAdvanceUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/drivers/{driverId}/advances", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<DriverAdvanceDto>(JsonOptions))!;
    }

    public async Task UpdateDriverAdvanceAsync(int driverId, int advanceId, DriverAdvanceUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/drivers/{driverId}/advances/{advanceId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteDriverAdvanceAsync(int driverId, int advanceId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/drivers/{driverId}/advances/{advanceId}");
        await EnsureSuccess(response);
    }

    public async Task<List<PayRunListItemDto>> GetPayRunsAsync(int? driverId = null, PayRunStatus? status = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (driverId.HasValue) query.Add($"driverId={driverId}");
        if (status.HasValue) query.Add($"status={status}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return await _http.GetFromJsonAsync<List<PayRunListItemDto>>($"api/payruns{qs}", JsonOptions) ?? new();
    }

    public async Task<PayRunDetailDto?> GetPayRunAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/payruns/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PayRunDetailDto>(JsonOptions);
    }

    public async Task<PayRunDetailDto> CreatePayRunAsync(PayRunUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/payruns", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<PayRunDetailDto>(JsonOptions))!;
    }

    public async Task<PayRunDetailDto> GeneratePayRunAsync(GeneratePayRunDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/payruns/generate", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<PayRunDetailDto>(JsonOptions))!;
    }

    public async Task UpdatePayRunAsync(int id, PayRunUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/payruns/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeletePayRunAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/payruns/{id}");
        await EnsureSuccess(response);
    }

    public async Task<PayRunLineDto> CreatePayRunLineAsync(int payRunId, PayRunLineUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync($"api/payruns/{payRunId}/lines", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<PayRunLineDto>(JsonOptions))!;
    }

    public async Task UpdatePayRunLineAsync(int payRunId, int lineId, PayRunLineUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/payruns/{payRunId}/lines/{lineId}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeletePayRunLineAsync(int payRunId, int lineId)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/payruns/{payRunId}/lines/{lineId}");
        await EnsureSuccess(response);
    }

    // ----- GPS / Live Tracking (module 9) -----

    public async Task<IngestResultDto> IngestPositionsAsync(IngestRequestDto request)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/tracking/ingest", request, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<IngestResultDto>(JsonOptions))!;
    }

    public async Task<List<LiveVehicleDto>> GetLiveVehiclesAsync()
    {
        await AuthorizeAsync();
        return await _http.GetFromJsonAsync<List<LiveVehicleDto>>("api/tracking/live", JsonOptions) ?? new();
    }

    public async Task<List<VehiclePositionDto>> GetVehicleHistoryAsync(int vehicleId, DateTime? from = null, DateTime? to = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return await _http.GetFromJsonAsync<List<VehiclePositionDto>>($"api/tracking/vehicle/{vehicleId}/history{qs}", JsonOptions) ?? new();
    }

    public async Task<VehiclePositionDto?> GetVehicleLatestPositionAsync(int vehicleId)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/tracking/vehicle/{vehicleId}/latest");
        if (!response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        return await response.Content.ReadFromJsonAsync<VehiclePositionDto>(JsonOptions);
    }

    public async Task<TripPathDto?> GetTripPathAsync(int tripId)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/tracking/trip/{tripId}/path");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TripPathDto>(JsonOptions);
    }

    public async Task<List<GeofenceEventDto>> GetGeofenceEventsAsync(int? vehicleId = null, int? geofenceId = null, int take = 100)
    {
        await AuthorizeAsync();
        var query = new List<string> { $"take={take}" };
        if (vehicleId.HasValue) query.Add($"vehicleId={vehicleId}");
        if (geofenceId.HasValue) query.Add($"geofenceId={geofenceId}");
        return await _http.GetFromJsonAsync<List<GeofenceEventDto>>($"api/tracking/geofence-events?{string.Join("&", query)}", JsonOptions) ?? new();
    }

    public async Task<List<GeofenceListItemDto>> GetGeofencesAsync(bool? activeOnly = null, string? search = null)
    {
        await AuthorizeAsync();
        var query = new List<string>();
        if (activeOnly.HasValue) query.Add($"activeOnly={activeOnly.Value.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        return await _http.GetFromJsonAsync<List<GeofenceListItemDto>>($"api/geofences{qs}", JsonOptions) ?? new();
    }

    public async Task<GeofenceDetailDto?> GetGeofenceAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync($"api/geofences/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<GeofenceDetailDto>(JsonOptions);
    }

    public async Task<GeofenceDetailDto> CreateGeofenceAsync(GeofenceUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PostAsJsonAsync("api/geofences", dto, JsonOptions);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<GeofenceDetailDto>(JsonOptions))!;
    }

    public async Task UpdateGeofenceAsync(int id, GeofenceUpsertDto dto)
    {
        await AuthorizeAsync();
        var response = await _http.PutAsJsonAsync($"api/geofences/{id}", dto, JsonOptions);
        await EnsureSuccess(response);
    }

    public async Task DeleteGeofenceAsync(int id)
    {
        await AuthorizeAsync();
        var response = await _http.DeleteAsync($"api/geofences/{id}");
        await EnsureSuccess(response);
    }

    // ----- Reports & Analytics -----

    public async Task<ReportsSummaryDto?> GetReportsSummaryAsync()
    {
        await AuthorizeAsync();
        var response = await _http.GetAsync("api/reports/summary");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReportsSummaryDto>(JsonOptions);
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
