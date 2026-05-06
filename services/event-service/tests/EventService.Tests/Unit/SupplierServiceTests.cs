using EventService.Domain.Entities;
using EventService.Infrastructure.Errors;
using EventService.Repositories;
using EventService.Services.Suppliers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EventService.Tests.Unit;

public class SupplierServiceTests
{
    private readonly Mock<ISupplierRepository> _repo = new();

    private SupplierService BuildSut() =>
        new(_repo.Object, NullLogger<SupplierService>.Instance);

    [Fact]
    public async Task ListAsync_clamps_limit_above_max_to_100()
    {
        _repo.Setup(r => r.CountAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repo.Setup(r => r.ListAsync(true, 100, 0, It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<Supplier>());

        var sut = BuildSut();
        var result = await sut.ListAsync(activeOnly: true, limit: 5000, offset: 0, CancellationToken.None);

        Assert.Equal(100, result.Limit);
        _repo.Verify(r => r.ListAsync(true, 100, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_uses_default_limit_when_zero()
    {
        _repo.Setup(r => r.CountAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repo.Setup(r => r.ListAsync(true, 50, 0, It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<Supplier>());

        var sut = BuildSut();
        var result = await sut.ListAsync(activeOnly: true, limit: 0, offset: 0, CancellationToken.None);

        Assert.Equal(50, result.Limit);
    }

    [Fact]
    public async Task ListAsync_clamps_negative_offset_to_zero()
    {
        _repo.Setup(r => r.CountAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _repo.Setup(r => r.ListAsync(true, 50, 0, It.IsAny<CancellationToken>()))
             .ReturnsAsync(Array.Empty<Supplier>());

        var sut = BuildSut();
        var result = await sut.ListAsync(activeOnly: true, limit: 50, offset: -42, CancellationToken.None);

        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public async Task ListAsync_maps_entities_to_response_dtos()
    {
        var entities = new[]
        {
            new Supplier { Id = Guid.NewGuid(), Name = "A", ContactEmail = "a@x", IsActive = true },
            new Supplier { Id = Guid.NewGuid(), Name = "B", ContactEmail = "b@x", IsActive = false }
        };
        _repo.Setup(r => r.CountAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _repo.Setup(r => r.ListAsync(false, 50, 0, It.IsAny<CancellationToken>())).ReturnsAsync(entities);

        var sut = BuildSut();
        var result = await sut.ListAsync(activeOnly: false, limit: 50, offset: 0, CancellationToken.None);

        Assert.Equal(2, result.Total);
        Assert.Collection(result.Items,
            r => { Assert.Equal("A", r.Name); Assert.True(r.IsActive); },
            r => { Assert.Equal("B", r.Name); Assert.False(r.IsActive); });
    }

    [Fact]
    public async Task GetByIdAsync_returns_dto_when_found()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new Supplier { Id = id, Name = "S", ContactEmail = "s@x", IsActive = true });

        var sut = BuildSut();
        var result = await sut.GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(id, result.Id);
        Assert.Equal("S", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFound_when_absent()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.GetByIdAsync(id, CancellationToken.None));
    }
}
