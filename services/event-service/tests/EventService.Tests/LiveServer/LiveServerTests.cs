using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventService.Tests.LiveServer;

/// <summary>
/// Black-box tests against the externally-running Event Service (Kestrel on
/// <c>http://localhost:5001</c> by default). All tests skip gracefully when the
/// server is not reachable — they never fail just because the process is down.
///
/// Run with the server already started (<c>./scripts/run.ps1</c>):
/// <code>dotnet test --filter "Category=LiveServer"</code>
/// </summary>
[Trait("Category", "LiveServer")]
public sealed class LiveServerTests : IClassFixture<LiveServerFixture>
{
    private readonly LiveServerFixture _f;

    public LiveServerTests(LiveServerFixture fixture) => _f = fixture;

    // ── Auth ───────────────────────────────────────────────────────────────────

    [SkippableFact]
    public async Task Auth_NoToken_Returns401()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        using var client = _f.CreateUnauthenticatedClient();
        var resp = await client.GetAsync("/api/v1/suppliers");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [SkippableFact]
    public async Task Auth_ValidBuyerToken_Returns200()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var resp = await _f.Client.GetAsync("/api/v1/suppliers");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Health ─────────────────────────────────────────────────────────────────

    [SkippableFact]
    public async Task Health_Returns200_WithoutToken()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        using var client = _f.CreateUnauthenticatedClient();
        var resp = await client.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ── Full happy-path scenario ───────────────────────────────────────────────

    [SkippableFact]
    public async Task HappyPath_SeedCreatePublish_EventStatusIsPublished()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        // 1. Fetch two seeded suppliers.
        var suppliersJson = await _f.Client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var supplierItems = suppliersJson.GetProperty("items").EnumerateArray().ToList();
        Assert.True(supplierItems.Count >= 2, "Live server seed must provide at least 2 suppliers.");

        var supplier1Id = supplierItems[0].GetProperty("id").GetGuid();
        var supplier2Id = supplierItems[1].GetProperty("id").GetGuid();

        // 2. Create a Draft event.
        var createResp = await _f.Client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Live-Server RFP Test",
            description = "Automated live-server test run.",
            category = "IT",
            currency = "INR",
            responseDeadlineUtc = DateTime.UtcNow.AddDays(30)
        });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var eventJson = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = eventJson.GetProperty("id").GetGuid();
        Assert.Equal("Draft", eventJson.GetProperty("status").GetString());

        // 3. Add two line items.
        var li1 = await _f.Client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/line-items",
            new { description = "Compute Nodes", quantity = 5m, unitPrice = 300000m });
        Assert.Equal(HttpStatusCode.Created, li1.StatusCode);

        var li2 = await _f.Client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/line-items",
            new { description = "Network Switches", quantity = 10m, unitPrice = 45000m });
        Assert.Equal(HttpStatusCode.Created, li2.StatusCode);

        // 4. Invite two suppliers.
        var inv1 = await _f.Client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/invitations", new { supplierId = supplier1Id });
        Assert.Equal(HttpStatusCode.Created, inv1.StatusCode);

        var inv2 = await _f.Client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/invitations", new { supplierId = supplier2Id });
        Assert.Equal(HttpStatusCode.Created, inv2.StatusCode);

        // 5. Publish.
        var pubResp = await _f.Client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, pubResp.StatusCode);

        var pubJson = await pubResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Published", pubJson.GetProperty("status").GetString());

        // 6. GET and confirm status.
        var getJson = await _f.Client.GetFromJsonAsync<JsonElement>($"/api/v1/events/{eventId}");
        Assert.Equal("Published", getJson.GetProperty("status").GetString());
    }

    // ── Publish gate tests ─────────────────────────────────────────────────────

    [SkippableFact]
    public async Task Publish_NoLineItems_Returns409()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var eventId = await CreateDraftEventAsync();

        var suppliersJson = await _f.Client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var supplierId = suppliersJson.GetProperty("items").EnumerateArray().First().GetProperty("id").GetGuid();
        await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId });

        var resp = await _f.Client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [SkippableFact]
    public async Task Publish_NoInvitations_Returns409()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var eventId = await CreateDraftEventAsync();
        await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/line-items",
            new { description = "Widget", quantity = 1m, unitPrice = 100m });

        var resp = await _f.Client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [SkippableFact]
    public async Task Publish_AlreadyPublished_Returns409()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var eventId = await CreateAndPublishEventAsync();
        var resp = await _f.Client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ── State-transition guard tests ───────────────────────────────────────────

    [SkippableFact]
    public async Task UpdateEvent_OnPublishedEvent_Returns409()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var eventId = await CreateAndPublishEventAsync();
        var resp = await _f.Client.PutAsJsonAsync($"/api/v1/events/{eventId}",
            new { title = "Attempted update" });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [SkippableFact]
    public async Task AddLineItem_OnPublishedEvent_Returns409()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var eventId = await CreateAndPublishEventAsync();
        var resp = await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/line-items",
            new { description = "Extra", quantity = 1m, unitPrice = 50m });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [SkippableFact]
    public async Task DuplicateInvitation_Returns409()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var eventId = await CreateDraftEventAsync();
        var suppliersJson = await _f.Client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var supplierId = suppliersJson.GetProperty("items").EnumerateArray().First().GetProperty("id").GetGuid();

        await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId });
        var resp = await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [SkippableFact]
    public async Task GetEvent_NotFound_Returns404()
    {
        Skip.If(!_f.IsAvailable, $"Live server not reachable at {_f.BaseUrl}");

        var resp = await _f.Client.GetAsync($"/api/v1/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<Guid> CreateDraftEventAsync()
    {
        var resp = await _f.Client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Live Test Event",
            category = "IT",
            currency = "INR",
            responseDeadlineUtc = DateTime.UtcNow.AddDays(14)
        });

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"POST /api/v1/events → {(int)resp.StatusCode}: {body}");
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateAndPublishEventAsync()
    {
        var eventId = await CreateDraftEventAsync();

        var suppliersJson = await _f.Client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var items = suppliersJson.GetProperty("items").EnumerateArray().ToList();
        var s1 = items[0].GetProperty("id").GetGuid();
        var s2 = items[1].GetProperty("id").GetGuid();

        await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/line-items",
            new { description = "Item A", quantity = 1m, unitPrice = 500m });
        await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId = s1 });
        await _f.Client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId = s2 });

        var pub = await _f.Client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        pub.EnsureSuccessStatusCode();

        return eventId;
    }
}
