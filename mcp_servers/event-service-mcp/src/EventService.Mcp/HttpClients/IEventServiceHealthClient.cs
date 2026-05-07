using EventService.Mcp.Models.Responses;

namespace EventService.Mcp.HttpClients;

public interface IEventServiceHealthClient
{
    Task<HealthResponse> GetHealthAsync(CancellationToken ct);
}
