using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EventService.Infrastructure.Auth;

/// <summary>
/// DI extensions that wire up JWT bearer authentication and the Buyer role
/// authorization policy. Validation runs locally inside this service; the
/// Gateway is not required for token validation in this build.
/// </summary>
public static class AuthExtensions
{
    public static IServiceCollection AddEventServiceAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                $"Missing '{JwtOptions.SectionName}' configuration section.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be set and at least 32 characters long.");
        }

        services.AddSingleton(jwt);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                    RoleClaimType = "roles",
                    NameClaimType = "sub"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(JwtOptions.BuyerPolicy, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(JwtOptions.BuyerRole));

        return services;
    }
}
