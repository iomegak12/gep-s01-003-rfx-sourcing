using System.Net.Http.Json;
using EventService.Mcp.Errors;
using EventService.Mcp.Models.Responses;

namespace EventService.Mcp.HttpClients;

public sealed class EventServiceHealthClient : IEventServiceHealthClient
{
    private readonly HttpClient _http;

    public EventServiceHealthClient(HttpClient http) => _http = http;

    public async Task<HealthResponse> GetHealthAsync(CancellationToken ct)
    {
        using var resp = await _http.GetAsync("/api/v1/health", ct);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<HealthResponse>(EventServiceClient.JsonOpts, ct))!;
    }
}
