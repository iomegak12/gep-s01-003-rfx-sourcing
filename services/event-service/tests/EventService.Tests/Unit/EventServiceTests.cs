using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Infrastructure.Errors;
using EventService.Repositories;
using EventService.Services.Audit;
using EventService.Services.Events;
using Moq;
using Xunit;

namespace EventService.Tests.Unit;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<ILineItemRepository> _lineItemRepo = new();
    private readonly Mock<IInvitationRepository> _invRepo = new();
    private readonly Mock<IAuditService> _audit = new();

    private Services.Events.EventService BuildSut() =>
        new(_eventRepo.Object, _lineItemRepo.Object, _invRepo.Object, _audit.Object);

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_persists_event_and_records_audit()
    {
        Event? captured = null;
        _eventRepo.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
                  .Callback<Event, CancellationToken>((e, _) => captured = e);
        _audit.Setup(a => a.RecordAsync(It.IsAny<Guid>(), AuditAction.EventCreated,
                     It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var req = new CreateEventRequest(
            "RFP – Cloud Infra",
            "Annual cloud refresh",
            "IT",
            "INR",
            DateTime.UtcNow.AddDays(30));

        var sut = BuildSut();
        var result = await sut.CreateAsync(req, "user-1", "corr-1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("RFP – Cloud Infra", result.Title);
        Assert.Equal(EventStatus.Draft, result.Status);
        Assert.Equal(0, result.Version);
        Assert.Equal("user-1", result.CreatedByUserId);

        _eventRepo.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.RecordAsync(result.Id, AuditAction.EventCreated, "user-1", "corr-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_returns_response_when_found()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var sut = BuildSut();
        var result = await sut.GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal("Test Event", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        var id = Guid.NewGuid();
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var sut = BuildSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetByIdAsync(id, CancellationToken.None));
    }

    // ── ListAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_clamps_limit_to_max_100()
    {
        _eventRepo.Setup(r => r.ListAsync(100, 0, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Array.Empty<Event>(), 0));

        var sut = BuildSut();
        var result = await sut.ListAsync(limit: 999, offset: 0, CancellationToken.None);

        Assert.Equal(100, result.Limit);
        _eventRepo.Verify(r => r.ListAsync(100, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_clamps_negative_offset_to_zero()
    {
        _eventRepo.Setup(r => r.ListAsync(20, 0, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Array.Empty<Event>(), 0));

        var sut = BuildSut();
        var result = await sut.ListAsync(limit: 20, offset: -5, CancellationToken.None);

        Assert.Equal(0, result.Offset);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_updates_title_and_increments_version()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _eventRepo.Setup(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));
        _audit.Setup(a => a.RecordAsync(id, AuditAction.EventUpdated,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var req = new UpdateEventRequest("Updated Title", null, null, null);
        var sut = BuildSut();
        var result = await sut.UpdateAsync(id, req, "user-1", "corr-1", CancellationToken.None);

        Assert.Equal("Updated Title", result.Title);
        Assert.Equal(1, result.Version);
        _audit.Verify(a => a.RecordAsync(id, AuditAction.EventUpdated, "user-1", "corr-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_throws_ConflictException_when_not_Draft()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        e.Status = EventStatus.Published;
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var req = new UpdateEventRequest("Title", null, null, null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.UpdateAsync(id, req, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_throws_ConflictException_for_Awarded_status()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        e.Status = EventStatus.Awarded;
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var req = new UpdateEventRequest("Title", null, null, null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.UpdateAsync(id, req, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_event_missing()
    {
        var id = Guid.NewGuid();
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var req = new UpdateEventRequest("Title", null, null, null);
        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.UpdateAsync(id, req, "user-1", "corr-1", CancellationToken.None));
    }

    // ── PublishAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_transitions_to_Published_and_records_audit()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _lineItemRepo.Setup(r => r.CountByEventAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _invRepo.Setup(r => r.CountByEventAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _eventRepo.Setup(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()));
        _audit.Setup(a => a.RecordAsync(id, AuditAction.EventPublished,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var sut = BuildSut();
        var result = await sut.PublishAsync(id, "user-1", "corr-1", CancellationToken.None);

        Assert.Equal(EventStatus.Published, result.Status);
        Assert.Equal(1, result.Version);
        _audit.Verify(a => a.RecordAsync(id, AuditAction.EventPublished, "user-1", "corr-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_throws_ConflictException_when_not_Draft()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        e.Status = EventStatus.Published;
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.PublishAsync(id, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_throws_ConflictException_when_deadline_passed()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        e.ResponseDeadlineUtc = DateTime.UtcNow.AddSeconds(-1); // past deadline
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.PublishAsync(id, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_throws_ConflictException_when_no_line_items()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _lineItemRepo.Setup(r => r.CountByEventAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.PublishAsync(id, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_throws_ConflictException_when_no_invitations()
    {
        var id = Guid.NewGuid();
        var e = BuildDraftEvent(id);
        _eventRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _lineItemRepo.Setup(r => r.CountByEventAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _invRepo.Setup(r => r.CountByEventAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.PublishAsync(id, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact(Skip = "Awaiting BSS criteria endpoint — see TODO(BSS-INTEGRATION) in EventService.PublishAsync")]
    public async Task PublishAsync_throws_ConflictException_when_no_scoring_criteria()
    {
        // This gate requires GET /api/v1/events/{id}/criteria/count on the Bid Scoring Service.
        // Implement once IBidScoringClient is wired (Phase 6).
        await Task.CompletedTask;
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
}
