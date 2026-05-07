using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EventService.Mcp.Errors;

namespace EventService.Mcp.Tests.Unit;

public class ProblemDetailsExceptionMapperTests
{
    [Fact]
    public async Task Success_response_does_not_throw()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.OK);
        await ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, CancellationToken.None);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400)]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.Conflict, 409)]
    [InlineData(HttpStatusCode.InternalServerError, 500)]
    public async Task Maps_problem_json_with_correlationId(HttpStatusCode status, int expected)
    {
        const string body =
            """
            {"type":"about:blank","title":"Conflicting state","status":409,"detail":"Event is not in Draft status.","correlationId":"abc-123"}
            """;
        var resp = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8)
        };
        resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/problem+json");

        var ex = await Assert.ThrowsAsync<UpstreamException>(
            () => ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, CancellationToken.None));
        Assert.Equal(expected, ex.StatusCode);
        Assert.Equal("Conflicting state", ex.Title);
        Assert.Equal("Event is not in Draft status.", ex.Detail);
        Assert.Equal("abc-123", ex.CorrelationId);
    }

    [Fact]
    public async Task Falls_back_when_body_not_problem_details()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream is down", Encoding.UTF8)
        };
        resp.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var ex = await Assert.ThrowsAsync<UpstreamException>(
            () => ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, CancellationToken.None));
        Assert.Equal(502, ex.StatusCode);
        Assert.False(string.IsNullOrEmpty(ex.Title));
    }

    [Fact]
    public async Task Reads_correlationId_from_header_when_body_missing_it()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"title\":\"Resource not found\"}", Encoding.UTF8)
        };
        resp.Content.Headers.ContentType = new MediaTypeHeaderValue("application/problem+json");
        resp.Headers.Add("X-Correlation-Id", "hdr-corr");

        var ex = await Assert.ThrowsAsync<UpstreamException>(
            () => ProblemDetailsExceptionMapper.EnsureSuccessAsync(resp, CancellationToken.None));
        Assert.Equal("hdr-corr", ex.CorrelationId);
    }
}
