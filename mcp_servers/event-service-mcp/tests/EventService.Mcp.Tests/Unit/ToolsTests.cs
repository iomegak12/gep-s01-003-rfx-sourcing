using EventService.Mcp.Errors;
using EventService.Mcp.HttpClients;
using EventService.Mcp.Models.Requests;
using EventService.Mcp.Models.Responses;
using EventService.Mcp.Tools;
using ModelContextProtocol;
using Moq;

namespace EventService.Mcp.Tests.Unit;

public class ToolsTests
{
    private static EventResponse SampleEvent() => new(
        Id: Guid.NewGuid(),
        Title: "T",
        Description: null,
        Category: "IT",
        Currency: "INR",
        ResponseDeadlineUtc: DateTimeOffset.UtcNow.AddDays(7),
        Status: EventStatus.Draft,
        Version: 0,
        CreatedByUserId: "u",
        CreatedAtUtc: DateTimeOffset.UtcNow,
        UpdatedAtUtc: DateTimeOffset.UtcNow);

    [Fact]
    public async Task SupplierTools_pass_through_success()
    {
        var mock = new Mock<IEventServiceClient>();
        var expected = new SupplierListResponse(Array.Empty<SupplierResponse>(), 50, 0, 0);
        mock.Setup(c => c.ListSuppliersAsync(50, 0, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var tool = new SupplierTools(mock.Object);
        var result = await tool.ListSuppliers();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task SupplierTools_translate_UpstreamException_to_McpException()
    {
        var mock = new Mock<IEventServiceClient>();
        mock.Setup(c => c.GetSupplierAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UpstreamException(404, "Resource not found", "Supplier not found."));

        var tool = new SupplierTools(mock.Object);
        var ex = await Assert.ThrowsAsync<McpException>(() => tool.GetSupplier(Guid.NewGuid()));
        Assert.Contains("Not found", ex.Message);
    }

    [Fact]
    public async Task EventTools_publish_translates_409()
    {
        var mock = new Mock<IEventServiceClient>();
        mock.Setup(c => c.PublishEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UpstreamException(409, "Conflicting state", "Event has no line items.", correlationId: "x-1"));

        var tool = new EventTools(mock.Object);
        var ex = await Assert.ThrowsAsync<McpException>(() => tool.PublishEvent(Guid.NewGuid()));
        Assert.Contains("Operation rejected", ex.Message);
        Assert.Contains("no line items", ex.Message);
        Assert.Contains("[correlationId=x-1]", ex.Message);
    }

    [Fact]
    public async Task EventTools_create_returns_dto_unchanged()
    {
        var mock = new Mock<IEventServiceClient>();
        var ev = SampleEvent();
        mock.Setup(c => c.CreateEventAsync(It.IsAny<CreateEventRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        var tool = new EventTools(mock.Object);
        var result = await tool.CreateEvent(new CreateEventRequest("T", null, "IT", "INR", DateTimeOffset.UtcNow.AddDays(1)));

        Assert.Same(ev, result);
    }

    [Fact]
    public async Task LineItemTools_delete_returns_confirmation_string()
    {
        var mock = new Mock<IEventServiceClient>();
        mock.Setup(c => c.DeleteLineItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tool = new LineItemTools(mock.Object);
        var msg = await tool.DeleteLineItem(Guid.NewGuid(), Guid.NewGuid());

        Assert.Contains("removed from event", msg);
    }

    [Fact]
    public async Task InvitationTools_revoke_returns_confirmation()
    {
        var mock = new Mock<IEventServiceClient>();
        mock.Setup(c => c.RevokeInvitationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tool = new InvitationTools(mock.Object);
        var msg = await tool.RevokeInvitation(Guid.NewGuid(), Guid.NewGuid());

        Assert.Contains("revoked from event", msg);
    }

    [Fact]
    public async Task HealthTools_returns_health_response()
    {
        var mock = new Mock<IEventServiceHealthClient>();
        mock.Setup(c => c.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthResponse("ok", "event-service", DateTimeOffset.UtcNow));

        var tool = new HealthTools(mock.Object);
        var result = await tool.HealthCheck();

        Assert.Equal("ok", result.Status);
    }
}
