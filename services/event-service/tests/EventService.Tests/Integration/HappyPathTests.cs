using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EventService.Domain.Enums;
using EventService.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EventService.Tests.Integration;

/// <summary>
/// End-to-end integration tests using <see cref="EventServiceWebFactory"/>.
/// Each test class instance gets an isolated SQLite file — no shared state between tests.
/// </summary>
public sealed class HappyPathTests : IDisposable
{
    private const string SigningKey = "bsprTMgGLN862rt3oy1Qeh289u0lWn6yOSBijQrDWDTxd44kiV2MsPgRrVd";

    private readonly EventServiceWebFactory _factory = new();
    private readonly HttpClient _client;

    public HappyPathTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BuyerToken());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Auth ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Auth_NoToken_Returns401()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/v1/suppliers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Auth_ValidBuyerToken_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/suppliers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Health ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Returns200_WithoutToken()
    {
        using var unauthClient = _factory.CreateClient();
        var response = await unauthClient.GetAsync("/api/v1/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Full happy-path scenario ───────────────────────────────────────────────

    [Fact]
    public async Task HappyPath_SeedCreatePublish_EventStatusIsPublished()
    {
        // 1. Fetch two seeded suppliers to use as invitees.
        var suppliersJson = await _client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var supplierItems = suppliersJson.GetProperty("items").EnumerateArray().ToList();
        Assert.True(supplierItems.Count >= 2, "Seed must provide at least 2 suppliers.");

        var supplier1Id = supplierItems[0].GetProperty("id").GetGuid();
        var supplier2Id = supplierItems[1].GetProperty("id").GetGuid();

        // 2. Create a Draft event.
        var createPayload = new
        {
            title = "Annual Cloud Infrastructure RFP",
            description = "Refresh of cloud compute and storage estate.",
            category = "IT",
            currency = "INR",
            responseDeadlineUtc = DateTime.UtcNow.AddDays(30)
        };

        var createResp = await _client.PostAsJsonAsync("/api/v1/events", createPayload);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var eventJson = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = eventJson.GetProperty("id").GetGuid();
        Assert.Equal("Draft", eventJson.GetProperty("status").GetString());

        // 3. Add two line items.
        var li1Resp = await _client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/line-items",
            new { description = "Compute Nodes (Rack)", quantity = 10m, unitPrice = 250000m });
        Assert.Equal(HttpStatusCode.Created, li1Resp.StatusCode);

        var li2Resp = await _client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/line-items",
            new { description = "SSD Storage (TB)", quantity = 50m, unitPrice = 18000m });
        Assert.Equal(HttpStatusCode.Created, li2Resp.StatusCode);

        // 4. Invite two suppliers.
        var inv1Resp = await _client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/invitations",
            new { supplierId = supplier1Id });
        Assert.Equal(HttpStatusCode.Created, inv1Resp.StatusCode);

        var inv2Resp = await _client.PostAsJsonAsync(
            $"/api/v1/events/{eventId}/invitations",
            new { supplierId = supplier2Id });
        Assert.Equal(HttpStatusCode.Created, inv2Resp.StatusCode);

        // 5. Publish the event.
        var publishResp = await _client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publishResp.StatusCode);

        var publishedJson = await publishResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Published", publishedJson.GetProperty("status").GetString());

        // 6. GET the event and verify status.
        var getResp = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/events/{eventId}");
        Assert.Equal("Published", getResp.GetProperty("status").GetString());

        // 7. Verify audit trail in the database.
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

        var auditActions = await db.AuditEvents
            .Where(a => a.EventId == eventId)
            .Select(a => a.Action)
            .ToListAsync();

        Assert.Contains(AuditAction.EventCreated, auditActions);
        Assert.Contains(AuditAction.LineItemAdded, auditActions);
        Assert.Contains(AuditAction.InvitationSent, auditActions);
        Assert.Contains(AuditAction.EventPublished, auditActions);
        Assert.Equal(2, auditActions.Count(a => a == AuditAction.LineItemAdded));
        Assert.Equal(2, auditActions.Count(a => a == AuditAction.InvitationSent));
    }

    // ── Publish gate tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Publish_NoLineItems_Returns409()
    {
        var eventId = await CreateDraftEventAsync();

        // Invite one supplier but skip line items.
        var suppliersJson = await _client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var supplierId = suppliersJson.GetProperty("items").EnumerateArray().First().GetProperty("id").GetGuid();
        await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId });

        var resp = await _client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Publish_NoInvitations_Returns409()
    {
        var eventId = await CreateDraftEventAsync();

        // Add a line item but no invitations.
        await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/line-items",
            new { description = "Widget", quantity = 1m, unitPrice = 100m });

        var resp = await _client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Publish_AlreadyPublished_Returns409()
    {
        var eventId = await CreateAndPublishEventAsync();
        var resp = await _client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ── State-transition guard tests ───────────────────────────────────────────

    [Fact]
    public async Task UpdateEvent_OnPublishedEvent_Returns409()
    {
        var eventId = await CreateAndPublishEventAsync();

        var resp = await _client.PutAsJsonAsync($"/api/v1/events/{eventId}",
            new { title = "Attempted update" });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task AddLineItem_OnPublishedEvent_Returns409()
    {
        var eventId = await CreateAndPublishEventAsync();

        var resp = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/line-items",
            new { description = "Extra item", quantity = 1m, unitPrice = 100m });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task DuplicateInvitation_Returns409()
    {
        var eventId = await CreateDraftEventAsync();
        var suppliersJson = await _client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var supplierId = suppliersJson.GetProperty("items").EnumerateArray().First().GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId });

        var resp = await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task GetEvent_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/api/v1/events/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<Guid> CreateDraftEventAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/events", new
        {
            title = "Test Event",
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

        var suppliersJson = await _client.GetFromJsonAsync<JsonElement>("/api/v1/suppliers");
        var items = suppliersJson.GetProperty("items").EnumerateArray().ToList();
        var s1 = items[0].GetProperty("id").GetGuid();
        var s2 = items[1].GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/line-items",
            new { description = "Item A", quantity = 1m, unitPrice = 500m });
        await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId = s1 });
        await _client.PostAsJsonAsync($"/api/v1/events/{eventId}/invitations", new { supplierId = s2 });

        var pub = await _client.PostAsync($"/api/v1/events/{eventId}/publish", null);
        pub.EnsureSuccessStatusCode();

        return eventId;
    }

    private static string BuyerToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "rfx-auth-service",
            audience: "rfx-sourcing-system",
            claims: new[]
            {
                new Claim("sub", "integration-test-user"),
                new Claim("roles", "Buyer")
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
