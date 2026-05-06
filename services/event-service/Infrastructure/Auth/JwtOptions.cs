namespace EventService.Infrastructure.Auth;

/// <summary>
/// Strongly-typed binding for the <c>Jwt</c> section of <c>appsettings.json</c>.
/// Values must match those configured on the Authentication Service.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int ClockSkewSeconds { get; init; } = 30;

    /// <summary>Authorization policy name applied to endpoints that require the Buyer role.</summary>
    public const string BuyerPolicy = "BuyerOnly";

    /// <summary>Role string expected in the JWT <c>roles</c> claim for buyer access.</summary>
    public const string BuyerRole = "Buyer";
}
