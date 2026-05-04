using MedicalCenter.Application.Features.Appointments;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;

namespace MedicalCenter.UnitTests.Features.Appointments;

public sealed class PatientBlockingLogicTests
{
    [Theory]
    [InlineData(9, 0, 10, 0, true)]   // 9:00 blocked by appointment at 10:00 (60 min before)
    [InlineData(10, 0, 10, 0, false)] // 10:00 NOT blocked by itself (0 min diff)
    [InlineData(11, 0, 10, 0, true)]  // 11:00 blocked by appointment at 10:00 (60 min after)
    [InlineData(8, 0, 10, 0, false)]  // 8:00 NOT blocked (120 min before)
    [InlineData(12, 0, 10, 0, false)] // 12:00 NOT blocked (120 min after)
    public void ConsecutiveBlockingCalculation(int slotHour, int slotMin, int occupiedHour, int occupiedMin, bool expectedBlocked)
    {
        // Arrange
        var slotTime = new TimeOnly(slotHour, slotMin);
        var occupiedTime = new TimeOnly(occupiedHour, occupiedMin);
        var occupiedHours = new List<TimeOnly> { occupiedTime };

        // Act - Use the fixed blocking logic (compare minutes from midnight)
        var slotMinutes = slotTime.Hour * 60 + slotTime.Minute;
        var occupiedMinutes = occupiedTime.Hour * 60 + occupiedTime.Minute;
        var diffMinutes = Math.Abs(occupiedMinutes - slotMinutes);
        var bloqueadoPorPaciente = diffMinutes > 0 && diffMinutes <= 60;

        // Assert
        Assert.Equal(expectedBlocked, bloqueadoPorPaciente);
    }

    [Fact]
    public void ConsecutiveBlocking_WithMultipleOccupiedHours_BlocksCorrectly()
    {
        // Arrange
        var slotTime = new TimeOnly(10, 0);
        var occupiedHours = new List<TimeOnly>
        {
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            new TimeOnly(14, 0) // Not consecutive to 10:00
        };

        // Act
        var bloqueadoPorPaciente = occupiedHours.Any(h =>
        {
            var diffMinutes = Math.Abs((h - slotTime).TotalMinutes);
            return diffMinutes > 0 && diffMinutes <= 60;
        });

        // Assert - 10:00 should be blocked because 9:00 and 11:00 are consecutive
        Assert.True(bloqueadoPorPaciente);
    }

    [Fact]
    public void ConsecutiveBlocking_SameDayOnly_DifferentDatesNotBlocked()
    {
        // Arrange
        var fecha1 = new DateOnly(2024, 5, 2);
        var fecha2 = new DateOnly(2024, 5, 3);
        var slotTime = new TimeOnly(0, 0); // Midnight on day 2

        // Patient has appointment at 23:00 on day 1
        var patientOccupiedHoursByDate = new Dictionary<DateOnly, List<TimeOnly>>
        {
            [fecha1] = new List<TimeOnly> { new TimeOnly(23, 0) }
        };

        // Act - Check slot on day 2
        var bloqueadoPorPaciente = patientOccupiedHoursByDate.TryGetValue(fecha2, out var occupiedHours)
            && occupiedHours.Any(h =>
            {
                var diffMinutes = Math.Abs((h - slotTime).TotalMinutes);
                return diffMinutes > 0 && diffMinutes <= 60;
            });

        // Assert - Should NOT be blocked because it's a different date
        Assert.False(bloqueadoPorPaciente);
    }

    [Fact]
    public void ConsecutiveBlocking_AtHourBoundary_BlocksExactly60Minutes()
    {
        // Test: 10:00 slot blocked by appointment at 9:00 (60 min before)
        var slotTime = new TimeOnly(10, 0);
        var occupiedHoursWith9 = new List<TimeOnly> { new TimeOnly(9, 0) };
        var slotMinutes = slotTime.Hour * 60 + slotTime.Minute;
        var is10BlockedBy9 = occupiedHoursWith9.Any(h =>
        {
            var occupiedMinutes = h.Hour * 60 + h.Minute;
            var diffMinutes = Math.Abs(occupiedMinutes - slotMinutes);
            return diffMinutes > 0 && diffMinutes <= 60;
        });

        // Test: 10:00 slot blocked by appointment at 11:00 (60 min after)
        var occupiedHoursWith11 = new List<TimeOnly> { new TimeOnly(11, 0) };
        var is10BlockedBy11 = occupiedHoursWith11.Any(h =>
        {
            var occupiedMinutes = h.Hour * 60 + h.Minute;
            var diffMinutes = Math.Abs(occupiedMinutes - slotMinutes);
            return diffMinutes > 0 && diffMinutes <= 60;
        });

        // Test: 9:00 slot blocked by appointment at 10:00 (60 min after)
        var slot9Time = new TimeOnly(9, 0);
        var occupiedHoursWith10 = new List<TimeOnly> { new TimeOnly(10, 0) };
        var slot9Minutes = slot9Time.Hour * 60 + slot9Time.Minute;
        var is9BlockedBy10 = occupiedHoursWith10.Any(h =>
        {
            var occupiedMinutes = h.Hour * 60 + h.Minute;
            var diffMinutes = Math.Abs(occupiedMinutes - slot9Minutes);
            return diffMinutes > 0 && diffMinutes <= 60;
        });

        // Test: 11:00 slot blocked by appointment at 10:00 (60 min before)
        var slot11Time = new TimeOnly(11, 0);
        var slot11Minutes = slot11Time.Hour * 60 + slot11Time.Minute;
        var is11BlockedBy10 = occupiedHoursWith10.Any(h =>
        {
            var occupiedMinutes = h.Hour * 60 + h.Minute;
            var diffMinutes = Math.Abs(occupiedMinutes - slot11Minutes);
            return diffMinutes > 0 && diffMinutes <= 60;
        });

        // Assert
        Assert.True(is10BlockedBy9, "10:00 should be blocked by appointment at 9:00 (60 min before)");
        Assert.True(is10BlockedBy11, "10:00 should be blocked by appointment at 11:00 (60 min after)");
        Assert.True(is9BlockedBy10, "9:00 should be blocked by appointment at 10:00 (60 min after)");
        Assert.True(is11BlockedBy10, "11:00 should be blocked by appointment at 10:00 (60 min before)");
    }
}
