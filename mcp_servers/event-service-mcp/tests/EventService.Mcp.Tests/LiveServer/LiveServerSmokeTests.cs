using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;

namespace EventService.Mcp.Tests.LiveServer;

[Trait("Category", "LiveServer")]
public class LiveServerSmokeTests : IClassFixture<LiveServerFixture>
{
    private readonly LiveServerFixture _f;

    public LiveServerSmokeTests(LiveServerFixture f)
    {
        _f = f;
        if (_f.IsAvailable)
        {
            _f.EventServiceHttp.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _f.BearerToken);
            _f.McpHttp.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _f.BearerToken);
        }
    }

    [SkippableFact]
    public async Task Mcp_health_endpoint_responds()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        var resp = await _f.McpHttp.GetAsync("/api/v1/health");
        Assert.True(resp.IsSuccessStatusCode);
    }

    [SkippableFact]
    public async Task Full_lifecycle_via_event_service_succeeds()
    {
        Skip.If(!_f.IsAvailable, "MCP server or Event Service unavailable.");

        // 1. List suppliers, pick the first active one.
        var suppliers = await _f.EventServiceHttp.GetFromJsonAsync<SupplierListResponse>(
            "/api/v1/suppliers?limit=5&offset=0&activeOnly=true",
            EventServiceClient.JsonOpts);
        Assert.NotNull(suppliers);
        Assert.NotEmpty(suppliers!.Items);
        var supplierId = suppliers.Items[0].Id;

        // 2. Create event.
        var createResp = await _f.EventServiceHttp.PostAsJsonAsync("/api/v1/events",
            new CreateEventRequest(
                Title: "Live-server smoke",
                Description: "MCP smoke",
                Category: "IT",
                Currency: "INR",
                ResponseDeadlineUtc: DateTimeOffset.UtcNow.AddDays(7)),
            EventServiceClient.JsonOpts);
        Assert.Equal(System.Net.HttpStatusCode.Created, createResp.StatusCode);
        var ev = (await createResp.Content.ReadFromJsonAsync<EventResponse>(EventServiceClient.JsonOpts))!;

        // 3. Add a line item.
        var liResp = await _f.EventServiceHttp.PostAsJsonAsync($"/api/v1/events/{ev.Id}/line-items",
            new AddLineItemRequest("Servers", 5, 100000),
            EventServiceClient.JsonOpts);
        Assert.Equal(System.Net.HttpStatusCode.Created, liResp.StatusCode);

        // 4. Invite supplier.
        var invResp = await _f.EventServiceHttp.PostAsJsonAsync($"/api/v1/events/{ev.Id}/invitations",
            new InviteSupplierRequest(supplierId),
            EventServiceClient.JsonOpts);
        Assert.Equal(System.Net.HttpStatusCode.Created, invResp.StatusCode);

        // 5. Publish.
        var pubResp = await _f.EventServiceHttp.PostAsync($"/api/v1/events/{ev.Id}/publish", content: null);
        Assert.True(pubResp.IsSuccessStatusCode, await pubResp.Content.ReadAsStringAsync());

        // 6. Confirm Published.
        var got = await _f.EventServiceHttp.GetFromJsonAsync<EventResponse>(
            $"/api/v1/events/{ev.Id}",
            EventServiceClient.JsonOpts);
        Assert.Equal(EventStatus.Published, got!.Status);
    }
}
