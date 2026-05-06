using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EventService.Tests.LiveServer;

/// <summary>
/// Shared fixture for live-server tests. Probes the running Kestrel instance
/// on construction; if unreachable, sets <see cref="IsAvailable"/> = false so
/// every test can skip gracefully instead of failing.
/// </summary>
public sealed class LiveServerFixture : IDisposable
{
    private static readonly string DefaultSigningKey =
        "bsprTMgGLN862rt3oy1Qeh289u0lWn6yOSBijQrDWDTxd44kiV2MsPgRrVd";

    public string BaseUrl { get; }
    public bool IsAvailable { get; }

    /// <summary>Pre-authenticated client (Bearer token with Buyer role).</summary>
    public HttpClient Client { get; }

    public LiveServerFixture()
    {
        BaseUrl = Environment.GetEnvironmentVariable("EVENT_SERVICE_BASE_URL")
                  ?? "http://localhost:5001";

        var signingKey = Environment.GetEnvironmentVariable("EVENT_SERVICE_JWT_KEY")
                         ?? DefaultSigningKey;

        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BuildBuyerToken(signingKey));

        IsAvailable = ProbeHealth();
    }

    /// <summary>Creates a bare (unauthenticated) HttpClient pointed at the live server.</summary>
    public HttpClient CreateUnauthenticatedClient()
        => new() { BaseAddress = new Uri(BaseUrl) };

    private bool ProbeHealth()
    {
        try
        {
            using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var resp = probe.GetAsync($"{BaseUrl}/api/v1/health").GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildBuyerToken(string signingKey)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "rfx-auth-service",
            audience: "rfx-sourcing-system",
            claims: new[]
            {
                new Claim("sub", "live-server-test-user"),
                new Claim("roles", "Buyer")
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose() => Client.Dispose();
}
