using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Infrastructure.Errors;
using EventService.Repositories;
using EventService.Services.Audit;
using EventService.Services.LineItems;
using Moq;
using Xunit;

namespace EventService.Tests.Unit;

public class LineItemServiceTests
{
    private readonly Mock<ILineItemRepository> _lineItemRepo = new();
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<IAuditService> _audit = new();

    private LineItemService BuildSut() =>
        new(_lineItemRepo.Object, _eventRepo.Object, _audit.Object);

    // ── AddAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_persists_line_item_and_records_audit()
    {
        var eventId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _lineItemRepo.Setup(r => r.AddAsync(It.IsAny<LineItem>(), It.IsAny<CancellationToken>()));
        _audit.Setup(a => a.RecordAsync(eventId, AuditAction.LineItemAdded,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var req = new AddLineItemRequest("Laptop", 10, 75000);
        var sut = BuildSut();
        var result = await sut.AddAsync(eventId, req, "user-1", "corr-1", CancellationToken.None);

        Assert.Equal("Laptop", result.Description);
        Assert.Equal(10m, result.Quantity);
        Assert.Equal(75000m, result.UnitPrice);
        Assert.Equal("INR", result.Currency);

        _lineItemRepo.Verify(r => r.AddAsync(It.IsAny<LineItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.RecordAsync(eventId, AuditAction.LineItemAdded, "user-1", "corr-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_throws_NotFoundException_when_event_missing()
    {
        var eventId = Guid.NewGuid();
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var req = new AddLineItemRequest("Laptop", 1, 1000);
        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.AddAsync(eventId, req, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_throws_ConflictException_when_event_not_Draft()
    {
        var eventId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        e.Status = EventStatus.Published;
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var req = new AddLineItemRequest("Laptop", 1, 1000);
        var sut = BuildSut();

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AddAsync(eventId, req, "user-1", "corr-1", CancellationToken.None));
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_removes_item_and_records_audit()
    {
        var eventId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        var item = BuildLineItem(itemId, eventId);

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _lineItemRepo.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _lineItemRepo.Setup(r => r.DeleteAsync(item, It.IsAny<CancellationToken>()));
        _audit.Setup(a => a.RecordAsync(eventId, AuditAction.LineItemRemoved,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var sut = BuildSut();
        await sut.DeleteAsync(eventId, itemId, "user-1", "corr-1", CancellationToken.None);

        _lineItemRepo.Verify(r => r.DeleteAsync(item, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.RecordAsync(eventId, AuditAction.LineItemRemoved, "user-1", "corr-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_ConflictException_when_event_not_Draft()
    {
        var eventId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        e.Status = EventStatus.Published;
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.DeleteAsync(eventId, itemId, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_item_not_on_event()
    {
        var eventId = Guid.NewGuid();
        var otherEventId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        var item = BuildLineItem(itemId, otherEventId); // belongs to a different event

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _lineItemRepo.Setup(r => r.GetByIdAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.DeleteAsync(eventId, itemId, "user-1", "corr-1", CancellationToken.None));
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static Event BuildDraftEvent(Guid id) => new()
    {
        Id = id,
        Title = "Test Event",
        Category = "IT",
        Currency = "INR",
        ResponseDeadlineUtc = DateTime.UtcNow.AddDays(30),
        Status = EventStatus.Draft,
        Version = 0,
        CreatedByUserId = "user-1",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static LineItem BuildLineItem(Guid itemId, Guid eventId) => new()
    {
        Id = itemId,
        EventId = eventId,
        Description = "Laptop",
        Quantity = 5,
        UnitPrice = 75000,
        Currency = "INR",
        CreatedAtUtc = DateTime.UtcNow
    };
}
