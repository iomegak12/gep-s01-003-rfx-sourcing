using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EventService.Mcp.Tests.LiveServer;

public sealed class LiveServerFixture : IDisposable
{
    private const string DefaultSigningKey = "bsprTMgGLN862rt3oy1Qeh289u0lWn6yOSBijQrDWDTxd44kiV2MsPgRrVd";
    private const string Issuer = "rfx-auth-service";
    private const string Audience = "rfx-sourcing-system";

    public string McpBaseUrl { get; }
    public string EventServiceBaseUrl { get; }
    public string BearerToken { get; }
    public bool IsAvailable { get; }
    public HttpClient McpHttp { get; }
    public HttpClient EventServiceHttp { get; }

    public LiveServerFixture()
    {
        McpBaseUrl = Environment.GetEnvironmentVariable("MCP_SERVER_BASE_URL")
                     ?? "http://localhost:5005";
        EventServiceBaseUrl = Environment.GetEnvironmentVariable("EVENT_SERVICE_BASE_URL")
                              ?? "http://localhost:5001";

        var signingKey = Environment.GetEnvironmentVariable("EVENT_SERVICE_JWT_KEY")
                         ?? DefaultSigningKey;
        BearerToken = BuildBuyerToken(signingKey);

        McpHttp = new HttpClient { BaseAddress = new Uri(McpBaseUrl), Timeout = TimeSpan.FromSeconds(10) };
        EventServiceHttp = new HttpClient { BaseAddress = new Uri(EventServiceBaseUrl), Timeout = TimeSpan.FromSeconds(10) };

        IsAvailable = ProbeHealth(McpHttp, "/api/v1/health") && ProbeHealth(EventServiceHttp, "/api/v1/health");
    }

    private static bool ProbeHealth(HttpClient http, string path)
    {
        try
        {
            using var probe = new HttpClient { BaseAddress = http.BaseAddress, Timeout = TimeSpan.FromSeconds(3) };
            var resp = probe.GetAsync(path).GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static string BuildBuyerToken(string signingKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "live-server-test-user"),
            new Claim("roles", "Buyer")
        };
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        McpHttp.Dispose();
        EventServiceHttp.Dispose();
    }
}
