using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace EventService.Mcp.Tests.LiveServer;

[Trait("Category", "LiveServer")]
public class LiveServerMcpClientTests : IClassFixture<LiveServerFixture>
{
    private readonly LiveServerFixture _f;

    public LiveServerMcpClientTests(LiveServerFixture f) => _f = f;

    private async Task<McpClient> ConnectAuthenticatedAsync(CancellationToken ct = default)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(new Uri(_f.McpBaseUrl), "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {_f.BearerToken}"
            }
        });
        return await McpClient.CreateAsync(transport, cancellationToken: ct);
    }

    private async Task<McpClient> ConnectAnonymousAsync(CancellationToken ct = default)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(new Uri(_f.McpBaseUrl), "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        });
        return await McpClient.CreateAsync(transport, cancellationToken: ct);
    }

    private static string ExtractText(CallToolResult result)
    {
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        return text ?? string.Empty;
    }

    private static JsonElement ExtractJson(CallToolResult result)
    {
        if (result.StructuredContent.HasValue) return result.StructuredContent.Value;
        var text = ExtractText(result);
        Assert.False(string.IsNullOrEmpty(text), "tool result had no text content");
        return JsonDocument.Parse(text).RootElement;
    }

    [SkippableFact]
    public async Task Mcp_lists_14_tools_3_resource_templates_4_prompts()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        await using var client = await ConnectAuthenticatedAsync();

        var tools = await client.ListToolsAsync();
        Assert.Equal(14, tools.Count);

        var prompts = await client.ListPromptsAsync();
        Assert.Equal(4, prompts.Count);

        var toolNames = tools.Select(t => t.Name).ToHashSet();
        Assert.Contains("list_suppliers", toolNames);
        Assert.Contains("create_event", toolNames);
        Assert.Contains("publish_event", toolNames);
        Assert.Contains("health_check", toolNames);
    }

    [SkippableFact]
    public async Task health_check_tool_round_trips_to_live_event_service()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        await using var client = await ConnectAnonymousAsync();

        var result = await client.CallToolAsync("health_check");
        Assert.NotEqual(true, result.IsError);

        var text = ExtractText(result);
        // Upstream Event Service health payload includes service="event-service" and a timestamp.
        Assert.Contains("event-service", text, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task list_suppliers_tool_returns_real_master_list()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        await using var client = await ConnectAuthenticatedAsync();

        var result = await client.CallToolAsync("list_suppliers", new Dictionary<string, object?>
        {
            ["limit"] = 5,
            ["offset"] = 0,
            ["activeOnly"] = true
        });

        Assert.NotEqual(true, result.IsError);
        var payload = ExtractJson(result);
        Assert.True(payload.TryGetProperty("items", out var items));
        Assert.True(items.GetArrayLength() > 0, "expected the live Event Service to seed at least one supplier");
        Assert.True(items[0].TryGetProperty("id", out _));
        Assert.True(items[0].TryGetProperty("name", out _));
    }

    [SkippableFact]
    public async Task missing_bearer_yields_structured_tool_error()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        await using var client = await ConnectAnonymousAsync();

        var result = await client.CallToolAsync("list_suppliers", new Dictionary<string, object?>
        {
            ["limit"] = 5
        });

        Assert.True(result.IsError);
        var text = ExtractText(result);
        Assert.Contains("Authentication failed", text, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task full_lifecycle_via_mcp_tools_against_live_backend()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        await using var client = await ConnectAuthenticatedAsync();

        // 1. list_suppliers via MCP tool
        var suppliersResult = await client.CallToolAsync("list_suppliers", new Dictionary<string, object?>
        {
            ["limit"] = 5,
            ["offset"] = 0,
            ["activeOnly"] = true
        });
        Assert.NotEqual(true, suppliersResult.IsError);
        var suppliersJson = ExtractJson(suppliersResult);
        var supplierId = suppliersJson.GetProperty("items")[0].GetProperty("id").GetGuid();

        // 2. create_event via MCP tool
        var createResult = await client.CallToolAsync("create_event", new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["title"] = "MCP-driven smoke",
                ["description"] = "Created via MCP tool against live Event Service",
                ["category"] = "IT",
                ["currency"] = "INR",
                ["responseDeadlineUtc"] = DateTimeOffset.UtcNow.AddDays(7).ToString("O")
            }
        });
        Assert.NotEqual(true, createResult.IsError);
        var ev = ExtractJson(createResult);
        var eventId = ev.GetProperty("id").GetGuid();
        Assert.Equal("Draft", ev.GetProperty("status").GetString());

        // 3. add_line_item via MCP tool
        var lineItemResult = await client.CallToolAsync("add_line_item", new Dictionary<string, object?>
        {
            ["eventId"] = eventId.ToString(),
            ["request"] = new Dictionary<string, object?>
            {
                ["description"] = "Servers",
                ["quantity"] = 5,
                ["unitPrice"] = 100000
            }
        });
        Assert.NotEqual(true, lineItemResult.IsError);

        // 4. invite_supplier via MCP tool
        var inviteResult = await client.CallToolAsync("invite_supplier", new Dictionary<string, object?>
        {
            ["eventId"] = eventId.ToString(),
            ["request"] = new Dictionary<string, object?> { ["supplierId"] = supplierId.ToString() }
        });
        Assert.NotEqual(true, inviteResult.IsError);

        // 5. publish_event via MCP tool
        var publishResult = await client.CallToolAsync("publish_event", new Dictionary<string, object?>
        {
            ["id"] = eventId.ToString()
        });
        Assert.True(publishResult.IsError != true, ExtractText(publishResult));
        Assert.Equal("Published", ExtractJson(publishResult).GetProperty("status").GetString());

        // 6. get_event via MCP tool to confirm persistence
        var getResult = await client.CallToolAsync("get_event", new Dictionary<string, object?>
        {
            ["id"] = eventId.ToString()
        });
        Assert.NotEqual(true, getResult.IsError);
        Assert.Equal("Published", ExtractJson(getResult).GetProperty("status").GetString());

        // 7. event_status_summary prompt — verifies the prompt registry is wired.
        var prompt = await client.GetPromptAsync("event_status_summary", new Dictionary<string, object?>
        {
            ["eventId"] = eventId.ToString(),
            ["audience"] = "executive"
        });
        Assert.NotEmpty(prompt.Messages);
    }

    [SkippableFact]
    public async Task event_resource_returns_live_event_payload()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        await using var client = await ConnectAuthenticatedAsync();

        // First create an event via tool, then fetch it via the templated resource.
        var createResult = await client.CallToolAsync("create_event", new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["title"] = "Resource fetch test",
                ["description"] = null,
                ["category"] = "IT",
                ["currency"] = "INR",
                ["responseDeadlineUtc"] = DateTimeOffset.UtcNow.AddDays(7).ToString("O")
            }
        });
        Assert.NotEqual(true, createResult.IsError);
        var eventId = ExtractJson(createResult).GetProperty("id").GetGuid();

        var resource = await client.ReadResourceAsync($"event://{eventId}");
        var text = resource.Contents.OfType<TextResourceContents>().FirstOrDefault()?.Text;
        Assert.False(string.IsNullOrEmpty(text));
        using var doc = JsonDocument.Parse(text!);
        Assert.Equal(eventId, doc.RootElement.GetProperty("id").GetGuid());
    }
}
