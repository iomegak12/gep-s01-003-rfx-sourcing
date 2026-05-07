namespace EventService.Mcp.Configuration;

public sealed class McpServerOptions
{
    public const string SectionName = "Mcp";

    public string ServerName { get; init; } = "event-service-mcp";
    public string ServerVersion { get; init; } = "1.0.0";
    public string EndpointPath { get; init; } = "/mcp";
    public bool Stateless { get; init; } = true;
}
