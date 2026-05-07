using System.Net.Http.Headers;
using EventService.Mcp.Errors;

namespace EventService.Mcp.Handlers;

public sealed class BearerTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BearerTokenForwardingHandler> _logger;

    public BearerTokenForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<BearerTokenForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;
        var authHeader = ctx?.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader))
        {
            throw new UpstreamException(
                statusCode: 401,
                title: "Unauthorized",
                detail: "Missing Authorization header on the MCP request.");
        }

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new UpstreamException(
                statusCode: 401,
                title: "Unauthorized",
                detail: "Authorization header must use the Bearer scheme.");
        }

        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);

        if (ctx is not null && ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var cid))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", cid.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
