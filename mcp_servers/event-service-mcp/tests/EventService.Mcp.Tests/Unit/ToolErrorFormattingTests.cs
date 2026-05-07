using EventService.Mcp.Errors;
using EventService.Mcp.Tools;

namespace EventService.Mcp.Tests.Unit;

public class ToolErrorFormattingTests
{
    [Theory]
    [InlineData(400, "Validation failed")]
    [InlineData(401, "Authentication failed")]
    [InlineData(403, "Access denied")]
    [InlineData(404, "Not found")]
    [InlineData(409, "Operation rejected")]
    [InlineData(500, "Upstream error")]
    public void Maps_status_code_to_message_template(int status, string expectedPrefix)
    {
        var ex = new UpstreamException(status, "Some title", "Some detail");
        var msg = ToolErrorFormatting.FormatUpstreamMessage(ex);
        Assert.StartsWith(expectedPrefix, msg);
    }

    [Fact]
    public void Appends_correlation_id_when_present()
    {
        var ex = new UpstreamException(409, "Conflicting state", "Event is not Draft.", correlationId: "abc-1");
        var msg = ToolErrorFormatting.FormatUpstreamMessage(ex);
        Assert.Contains("[correlationId=abc-1]", msg);
    }

    [Fact]
    public void Omits_correlation_block_when_absent()
    {
        var ex = new UpstreamException(400, "Validation failed", "bad input");
        var msg = ToolErrorFormatting.FormatUpstreamMessage(ex);
        Assert.DoesNotContain("correlationId", msg);
    }
}
