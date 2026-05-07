using System.Text.Json.Serialization;

namespace EventService.Mcp.Errors;

public sealed class ProblemDetails
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}
