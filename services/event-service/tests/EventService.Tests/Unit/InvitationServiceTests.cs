using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Infrastructure.Errors;
using EventService.Repositories;
using EventService.Services.Audit;
using EventService.Services.Invitations;
using Moq;
using Xunit;

namespace EventService.Tests.Unit;

public class InvitationServiceTests
{
    private readonly Mock<IInvitationRepository> _invRepo = new();
    private readonly Mock<IEventRepository> _eventRepo = new();
    private readonly Mock<ISupplierRepository> _supplierRepo = new();
    private readonly Mock<IAuditService> _audit = new();

    private InvitationService BuildSut() =>
        new(_invRepo.Object, _eventRepo.Object, _supplierRepo.Object, _audit.Object);

    // ── InviteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task InviteAsync_creates_invitation_and_records_audit()
    {
        var eventId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        var supplier = BuildActiveSupplier(supplierId);

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        _invRepo.Setup(r => r.GetAsync(eventId, supplierId, It.IsAny<CancellationToken>())).ReturnsAsync((EventSupplier?)null);
        _invRepo.Setup(r => r.AddAsync(It.IsAny<EventSupplier>(), It.IsAny<CancellationToken>()));
        _audit.Setup(a => a.RecordAsync(eventId, AuditAction.InvitationSent,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var sut = BuildSut();
        var result = await sut.InviteAsync(eventId, supplierId, "user-1", "corr-1", CancellationToken.None);

        Assert.Equal(eventId, result.EventId);
        Assert.Equal(supplierId, result.SupplierId);
        Assert.Equal(supplier.Name, result.SupplierName);

        _invRepo.Verify(r => r.AddAsync(It.IsAny<EventSupplier>(), It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.RecordAsync(eventId, AuditAction.InvitationSent, "user-1", "corr-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InviteAsync_throws_NotFoundException_when_event_missing()
    {
        var eventId = Guid.NewGuid();
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        var sut = BuildSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.InviteAsync(eventId, Guid.NewGuid(), "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task InviteAsync_throws_ConflictException_when_event_not_Draft()
    {
        var eventId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        e.Status = EventStatus.Published;
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.InviteAsync(eventId, Guid.NewGuid(), "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task InviteAsync_throws_NotFoundException_when_supplier_missing()
    {
        var eventId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildDraftEvent(eventId));
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var sut = BuildSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.InviteAsync(eventId, supplierId, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task InviteAsync_throws_ConflictException_when_supplier_inactive()
    {
        var eventId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var supplier = BuildActiveSupplier(supplierId);
        supplier.IsActive = false;

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildDraftEvent(eventId));
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.InviteAsync(eventId, supplierId, "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task InviteAsync_throws_ConflictException_when_already_invited()
    {
        var eventId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var existing = new EventSupplier { EventId = eventId, SupplierId = supplierId, InvitedByUserId = "u", InvitedAtUtc = DateTime.UtcNow };

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildDraftEvent(eventId));
        _supplierRepo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildActiveSupplier(supplierId));
        _invRepo.Setup(r => r.GetAsync(eventId, supplierId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.InviteAsync(eventId, supplierId, "user-1", "corr-1", CancellationToken.None));
    }

    // ── RevokeAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_deletes_invitation_and_records_audit()
    {
        var eventId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        var invitation = new EventSupplier
        {
            EventId = eventId, SupplierId = supplierId,
            InvitedByUserId = "user-1", InvitedAtUtc = DateTime.UtcNow,
            Supplier = BuildActiveSupplier(supplierId)
        };

        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        _invRepo.Setup(r => r.GetAsync(eventId, supplierId, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        _invRepo.Setup(r => r.DeleteAsync(invitation, It.IsAny<CancellationToken>()));
        _audit.Setup(a => a.RecordAsync(eventId, AuditAction.InvitationRevoked,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        var sut = BuildSut();
        await sut.RevokeAsync(eventId, supplierId, "user-1", "corr-1", CancellationToken.None);

        _invRepo.Verify(r => r.DeleteAsync(invitation, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.RecordAsync(eventId, AuditAction.InvitationRevoked, "user-1", "corr-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_throws_ConflictException_when_event_not_Draft()
    {
        var eventId = Guid.NewGuid();
        var e = BuildDraftEvent(eventId);
        e.Status = EventStatus.Published;
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        var sut = BuildSut();
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RevokeAsync(eventId, Guid.NewGuid(), "user-1", "corr-1", CancellationToken.None));
    }

    [Fact]
    public async Task RevokeAsync_throws_NotFoundException_when_invitation_missing()
    {
        var eventId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        _eventRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(BuildDraftEvent(eventId));
        _invRepo.Setup(r => r.GetAsync(eventId, supplierId, It.IsAny<CancellationToken>())).ReturnsAsync((EventSupplier?)null);

        var sut = BuildSut();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.RevokeAsync(eventId, supplierId, "user-1", "corr-1", CancellationToken.None));
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static Event BuildDraftEvent(Guid id) => new()
    {
        Id = id, Title = "Test", Category = "IT", Currency = "INR",
        ResponseDeadlineUtc = DateTime.UtcNow.AddDays(30),
        Status = EventStatus.Draft, Version = 0,
        CreatedByUserId = "user-1", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
    };

    private static Supplier BuildActiveSupplier(Guid id) => new()
    {
        Id = id, Name = "Acme Corp", ContactEmail = "a@acme.com",
        IsActive = true, CreatedAtUtc = DateTime.UtcNow
    };
}
