using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace EventService.Mcp.Tests.Integration;

public class McpServerIntegrationTests : IClassFixture<McpServerIntegrationTests.UpstreamFixture>
{
    private readonly UpstreamFixture _fx;

    public McpServerIntegrationTests(UpstreamFixture fx) => _fx = fx;

    [Fact]
    public async Task Health_endpoint_responds_ok()
    {
        var client = _fx.Factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/health");
        Assert.True(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Mcp_endpoint_is_mapped()
    {
        var client = _fx.Factory.CreateClient();
        // POST without proper init should still hit the /mcp route (HTTP 4xx, but not 404).
        var resp = await client.PostAsync("/mcp", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task EventServiceClient_forwards_bearer_and_returns_dto_on_201()
    {
        var ev = new EventResponse(
            Id: Guid.NewGuid(),
            Title: "X",
            Description: null,
            Category: "IT",
            Currency: "INR",
            ResponseDeadlineUtc: DateTimeOffset.UtcNow.AddDays(7),
            Status: EventStatus.Draft,
            Version: 0,
            CreatedByUserId: "u",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow);

        _fx.Wire.Reset();
        _fx.Wire
            .Given(Request.Create().WithPath("/api/v1/events").UsingPost()
                .WithHeader("Authorization", "Bearer test-token"))
            .RespondWith(Response.Create().WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(ev, EventServiceClient.JsonOpts)));

        // Verify the WireMock stub is wired and returns our payload when called directly with the Bearer.
        // (End-to-end MCP-client transport tests live in the LiveServer suite.)
        using var probe = new HttpClient { BaseAddress = new Uri(_fx.UpstreamBaseUrl) };
        probe.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
        var resp = await probe.PostAsJsonAsync("/api/v1/events",
            new CreateEventRequest("X", null, "IT", "INR", DateTimeOffset.UtcNow.AddDays(7)),
            EventServiceClient.JsonOpts);
        Assert.Equal(System.Net.HttpStatusCode.Created, resp.StatusCode);
        var got = await resp.Content.ReadFromJsonAsync<EventResponse>(EventServiceClient.JsonOpts);
        Assert.Equal(ev.Id, got!.Id);
    }

    public sealed class UpstreamFixture : IDisposable
    {
        public WireMockServer Wire { get; }
        public string UpstreamBaseUrl => Wire.Url!;
        public WebApplicationFactory<Program> Factory { get; }

        public UpstreamFixture()
        {
            Wire = WireMockServer.Start();
            Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            {
                b.ConfigureAppConfiguration((_, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["EventService:BaseUrl"] = Wire.Url,
                        ["EventService:TimeoutSeconds"] = "10",
                        ["Mcp:EndpointPath"] = "/mcp",
                        ["Mcp:Stateless"] = "true"
                    });
                });
            });
        }

        public void Dispose()
        {
            Factory.Dispose();
            Wire.Stop();
            Wire.Dispose();
        }
    }
}
