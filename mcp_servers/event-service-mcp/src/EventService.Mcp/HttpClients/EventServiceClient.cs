using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventService.Mcp.Errors;
using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;

namespace EventService.Mcp.HttpClients;

public sealed class EventServiceClient : IEventServiceClient
{
    public static readonly JsonSerializerOptions JsonOpts = CreateJsonOpts();

    private readonly HttpClient _http;
    private readonly ILogger<EventServiceClient> _logger;

    public EventServiceClient(HttpClient http, ILogger<EventServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    private static JsonSerializerOptions CreateJsonOpts()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    // --- Suppliers ---------------------------------------------------------

    public async Task<SupplierListResponse> ListSuppliersAsync(int limit, int offset, bool activeOnly, CancellationToken ct)
    {
        var url = $"/api/v1/suppliers?limit={limit}&offset={offset}&activeOnly={(activeOnly ? "true" : "false")}";
        using var resp = await _http.GetAsync(url, ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<SupplierListResponse>(JsonOpts, ct))!;
    }

    public async Task<SupplierResponse> GetSupplierAsync(Guid id, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"/api/v1/suppliers/{id}", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<SupplierResponse>(JsonOpts, ct))!;
    }

    // --- Events ------------------------------------------------------------

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest req, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync("/api/v1/events", req, JsonOpts, ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<EventResponse>(JsonOpts, ct))!;
    }

    public async Task<EventListResponse> ListEventsAsync(int limit, int offset, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"/api/v1/events?limit={limit}&offset={offset}", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<EventListResponse>(JsonOpts, ct))!;
    }

    public async Task<EventResponse> GetEventAsync(Guid id, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"/api/v1/events/{id}", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<EventResponse>(JsonOpts, ct))!;
    }

    public async Task<EventResponse> UpdateEventAsync(Guid id, UpdateEventRequest req, CancellationToken ct)
    {
        using var resp = await _http.PutAsJsonAsync($"/api/v1/events/{id}", req, JsonOpts, ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<EventResponse>(JsonOpts, ct))!;
    }

    public async Task<EventResponse> PublishEventAsync(Guid id, CancellationToken ct)
    {
        using var resp = await _http.PostAsync($"/api/v1/events/{id}/publish", content: null, ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<EventResponse>(JsonOpts, ct))!;
    }

    // --- Line items --------------------------------------------------------

    public async Task<LineItemResponse> AddLineItemAsync(Guid eventId, AddLineItemRequest req, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync($"/api/v1/events/{eventId}/line-items", req, JsonOpts, ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<LineItemResponse>(JsonOpts, ct))!;
    }

    public async Task<LineItemListResponse> ListLineItemsAsync(Guid eventId, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"/api/v1/events/{eventId}/line-items", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<LineItemListResponse>(JsonOpts, ct))!;
    }

    public async Task DeleteLineItemAsync(Guid eventId, Guid itemId, CancellationToken ct)
    {
        using var resp = await _http.DeleteAsync($"/api/v1/events/{eventId}/line-items/{itemId}", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
    }

    // --- Invitations -------------------------------------------------------

    public async Task<InvitationResponse> InviteSupplierAsync(Guid eventId, InviteSupplierRequest req, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", req, JsonOpts, ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<InvitationResponse>(JsonOpts, ct))!;
    }

    public async Task<InvitationListResponse> ListInvitationsAsync(Guid eventId, CancellationToken ct)
    {
        using var resp = await _http.GetAsync($"/api/v1/events/{eventId}/invitations", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<InvitationListResponse>(JsonOpts, ct))!;
    }

    public async Task RevokeInvitationAsync(Guid eventId, Guid supplierId, CancellationToken ct)
    {
        using var resp = await _http.DeleteAsync($"/api/v1/events/{eventId}/invitations/{supplierId}", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
    }
}
