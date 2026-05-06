using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Application.DTOs;
using NSubstitute;

namespace MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Helpers;

/// <summary>
/// Custom assertion extensions for appointment-related tests.
/// </summary>
public static class AppointmentAssertionExtensions
{
    /// <summary>
    /// Asserts that the appointment summary represents a successful assignment.
    /// </summary>
    public static void ShouldBeSuccessAssignment(this AppointmentSummary result, Guid expectedPatientId)
    {
        Assert.NotNull(result);
        Assert.Equal(expectedPatientId, result.PacienteId);
        Assert.Equal("ocupado", result.Estado);
    }

    /// <summary>
    /// Asserts that the appointment summary represents a cancelled appointment.
    /// </summary>
    public static void ShouldBeCancelled(this AppointmentSummary result)
    {
        Assert.NotNull(result);
        Assert.Equal("cancelado", result.Estado);
    }

    /// <summary>
    /// Asserts that the appointment summary represents a held appointment.
    /// </summary>
    public static void ShouldBeHeld(this AppointmentSummary result)
    {
        Assert.NotNull(result);
        Assert.Equal("apartado", result.Estado);
    }

    /// <summary>
    /// Asserts that the appointment summary represents a free slot.
    /// </summary>
    public static void ShouldBeLibre(this AppointmentSummary result)
    {
        Assert.NotNull(result);
        Assert.Equal("libre", result.Estado);
    }

    /// <summary>
    /// Asserts that save changes was called exactly once.
    /// </summary>
    public static async Task ShouldSaveChangesAsync(this IUnitOfWork unitOfWork)
    {
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Asserts that save changes was never called.
    /// </summary>
    public static async Task ShouldNotSaveChangesAsync(this IUnitOfWork unitOfWork)
    {
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Asserts that the collection contains exactly the expected number of items.
    /// </summary>
    public static void ShouldHaveCount<T>(this IReadOnlyCollection<T> collection, int expectedCount)
    {
        Assert.Equal(expectedCount, collection.Count);
    }

    /// <summary>
    /// Asserts that the collection is empty.
    /// </summary>
    public static void ShouldBeEmpty<T>(this IReadOnlyCollection<T> collection)
    {
        Assert.Empty(collection);
    }

    /// <summary>
    /// Asserts that the collection is not empty.
    /// </summary>
    public static void ShouldNotBeEmpty<T>(this IReadOnlyCollection<T> collection)
    {
        Assert.NotEmpty(collection);
    }

    /// <summary>
    /// Asserts that the result is not null and returns it.
    /// </summary>
    public static T ShouldNotBeNull<T>(this T? result) where T : class
    {
        Assert.NotNull(result);
        return result;
    }
}
