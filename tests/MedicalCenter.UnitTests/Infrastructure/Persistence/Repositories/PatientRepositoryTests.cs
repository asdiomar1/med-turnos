using MedicalCenter.Infrastructure.Persistence.Repositories;

namespace MedicalCenter.UnitTests.Infrastructure.Persistence.Repositories;

/// <summary>
/// Approval tests for PatientRepository guard clauses (no DB required).
/// These tests capture current behavior before CA1862 refactoring.
/// </summary>
public sealed class PatientRepositoryTests
{
    private readonly PatientRepository _repo = new(null!); // DbContext not used for guard clauses

    [Fact]
    public async Task GetByLoginIdentifierAsync_WhenLoginIdentifierIsNull_ReturnsNull()
    {
        var result = await _repo.GetByLoginIdentifierAsync(null!, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByLoginIdentifierAsync_WhenLoginIdentifierIsWhitespace_ReturnsNull()
    {
        var result = await _repo.GetByLoginIdentifierAsync("   ", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPortalIdentifierAsync_WhenIdentifierIsNull_ReturnsNull()
    {
        var result = await _repo.GetByPortalIdentifierAsync(null!, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPortalIdentifierAsync_WhenIdentifierIsWhitespace_ReturnsNull()
    {
        var result = await _repo.GetByPortalIdentifierAsync("   ", CancellationToken.None);

        Assert.Null(result);
    }
}
