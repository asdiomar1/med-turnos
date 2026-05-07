using MedicalCenter.Infrastructure.Persistence.Repositories;

namespace MedicalCenter.UnitTests.Infrastructure.Persistence.Repositories;

/// <summary>
/// Approval tests for UserRepository guard clauses (no DB required).
/// These tests capture current behavior before CA1862 refactoring.
/// </summary>
public sealed class UserRepositoryTests
{
    private readonly UserRepository _repo = new(null!); // DbContext not used for guard clauses

    [Fact]
    public async Task GetByIdentifierAsync_WhenIdentifierIsNull_ReturnsNull()
    {
        var result = await _repo.GetByIdentifierAsync(null!, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdentifierAsync_WhenIdentifierIsWhitespace_ReturnsNull()
    {
        var result = await _repo.GetByIdentifierAsync("   ", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenEmailIsNull_ReturnsNull()
    {
        var result = await _repo.GetByEmailAsync(null!, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenEmailIsWhitespace_ReturnsNull()
    {
        var result = await _repo.GetByEmailAsync("   ", CancellationToken.None);

        Assert.Null(result);
    }
}
