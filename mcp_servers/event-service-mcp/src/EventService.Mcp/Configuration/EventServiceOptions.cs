using System.ComponentModel.DataAnnotations;

namespace EventService.Mcp.Configuration;

public sealed class EventServiceOptions
{
    public const string SectionName = "EventService";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 30;
}
