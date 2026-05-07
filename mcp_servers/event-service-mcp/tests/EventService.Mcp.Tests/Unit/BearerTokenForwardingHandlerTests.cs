using System.Net;
using EventService.Mcp.Errors;
using EventService.Mcp.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventService.Mcp.Tests.Unit;

public class BearerTokenForwardingHandlerTests
{
    private sealed class CapturingInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static (HttpMessageInvoker invoker, CapturingInnerHandler inner) BuildInvoker(
        IHttpContextAccessor accessor)
    {
        var inner = new CapturingInnerHandler();
        var handler = new BearerTokenForwardingHandler(accessor, NullLogger<BearerTokenForwardingHandler>.Instance)
        {
            InnerHandler = inner
        };
        return (new HttpMessageInvoker(handler), inner);
    }

    private static IHttpContextAccessor AccessorFor(string? authHeader, string? correlationId = null)
    {
        var ctx = new DefaultHttpContext();
        if (authHeader is not null) ctx.Request.Headers.Authorization = authHeader;
        if (correlationId is not null) ctx.Request.Headers["X-Correlation-Id"] = correlationId;
        return new HttpContextAccessor { HttpContext = ctx };
    }

    [Fact]
    public async Task Forwards_Bearer_token_verbatim()
    {
        var (invoker, inner) = BuildInvoker(AccessorFor("Bearer abc.def.ghi"));
        var req = new HttpRequestMessage(HttpMethod.Get, "https://upstream/api/v1/x");

        var resp = await invoker.SendAsync(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("Bearer", inner.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("abc.def.ghi", inner.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Forwards_correlation_id_when_present()
    {
        var (invoker, inner) = BuildInvoker(AccessorFor("Bearer t", correlationId: "corr-123"));
        var req = new HttpRequestMessage(HttpMethod.Get, "https://upstream/x");

        await invoker.SendAsync(req, CancellationToken.None);

        Assert.True(inner.LastRequest!.Headers.TryGetValues("X-Correlation-Id", out var v));
        Assert.Equal("corr-123", v!.First());
    }

    [Fact]
    public async Task Throws_UpstreamException_401_when_header_missing()
    {
        var (invoker, _) = BuildInvoker(AccessorFor(null));
        var req = new HttpRequestMessage(HttpMethod.Get, "https://upstream/x");

        var ex = await Assert.ThrowsAsync<UpstreamException>(
            () => invoker.SendAsync(req, CancellationToken.None));
        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("Missing Authorization", ex.Detail);
    }

    [Fact]
    public async Task Throws_UpstreamException_401_when_scheme_not_Bearer()
    {
        var (invoker, _) = BuildInvoker(AccessorFor("Basic dXNlcjpwYXNz"));
        var req = new HttpRequestMessage(HttpMethod.Get, "https://upstream/x");

        var ex = await Assert.ThrowsAsync<UpstreamException>(
            () => invoker.SendAsync(req, CancellationToken.None));
        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("Bearer scheme", ex.Detail);
    }
}
