using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Responses;
using EventService.Mcp.Resources;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Moq;

namespace EventService.Mcp.Tests.Unit;

public class ResourcesTests
{
    [Fact]
    public async Task EventResources_invalid_guid_throws_McpException()
    {
        var mock = new Mock<IEventServiceClient>();
        var res = new EventResources(mock.Object);

        await Assert.ThrowsAsync<McpException>(() => res.GetEvent("not-a-guid"));
    }

    [Fact]
    public async Task EventResources_404_translates_to_not_found_message()
    {
        var mock = new Mock<IEventServiceClient>();
        mock.Setup(c => c.GetEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UpstreamException(404, "Resource not found", "Event not found."));

        var res = new EventResources(mock.Object);
        var id = Guid.NewGuid().ToString();
        var ex = await Assert.ThrowsAsync<McpException>(() => res.GetEvent(id));
        Assert.Contains("Event not found", ex.Message);
        Assert.Contains(id, ex.Message);
    }

    [Fact]
    public async Task EventResources_success_returns_text_resource_contents()
    {
        var mock = new Mock<IEventServiceClient>();
        var ev = new EventResponse(
            Guid.NewGuid(), "T", null, "IT", "INR",
            DateTimeOffset.UtcNow.AddDays(7),
            EventStatus.Draft, 0, "u",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        mock.Setup(c => c.GetEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var res = new EventResources(mock.Object);
        var contents = await res.GetEvent(ev.Id.ToString());

        var text = Assert.IsType<TextResourceContents>(contents);
        Assert.Equal("application/json", text.MimeType);
        Assert.Contains(ev.Id.ToString(), text.Text);
    }

    [Fact]
    public void ReferenceResources_returns_static_document()
    {
        var contents = ReferenceResources.GetEventStatusReference();
        var text = Assert.IsType<TextResourceContents>(contents);
        Assert.Contains("publishGates", text.Text);
        Assert.Contains("Draft", text.Text);
        Assert.Contains("Published", text.Text);
    }

    [Fact]
    public async Task SupplierResources_paginates_and_caps()
    {
        var mock = new Mock<IEventServiceClient>();
        // Return 100 items for two pages, then 0 to terminate.
        var page = new SupplierListResponse(
            Enumerable.Range(0, 100).Select(_ => new SupplierResponse(Guid.NewGuid(), "S", null, true)).ToArray(),
            100, 0, 250);
        var emptyPage = new SupplierListResponse(Array.Empty<SupplierResponse>(), 100, 0, 0);
        mock.SetupSequence(c => c.ListSuppliersAsync(100, It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page)
            .ReturnsAsync(page)
            .ReturnsAsync(emptyPage);

        var res = new SupplierResources(mock.Object);
        var contents = await res.GetMasterList();
        var text = Assert.IsType<TextResourceContents>(contents);
        Assert.Contains("\"count\":200", text.Text);
    }
}
