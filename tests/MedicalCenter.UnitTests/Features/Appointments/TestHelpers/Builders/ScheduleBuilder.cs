using MedicalCenter.Domain.Entities;

namespace MedicalCenter.UnitTests.Features.Appointments.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating Schedule and ScheduleHour entities in tests.
/// </summary>
public sealed class ScheduleBuilder
{
    private Guid _id = Guid.NewGuid();
    private DateOnly _fecha = new(2026, 5, 2);
    private TimeOnly _hora = new(9, 0);
    private int _lugar = 1;
    private string _agendaKey = "default";

    public ScheduleBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ScheduleBuilder WithFecha(DateOnly fecha)
    {
        _fecha = fecha;
        return this;
    }

    public ScheduleBuilder WithFecha(int year, int month, int day)
    {
        _fecha = new DateOnly(year, month, day);
        return this;
    }

    public ScheduleBuilder WithHora(TimeOnly hora)
    {
        _hora = hora;
        return this;
    }

    public ScheduleBuilder WithHora(int hour, int minute)
    {
        _hora = new TimeOnly(hour, minute);
        return this;
    }

    public ScheduleBuilder WithLugar(int lugar)
    {
        _lugar = lugar;
        return this;
    }

    public ScheduleBuilder WithAgendaKey(string agendaKey)
    {
        _agendaKey = agendaKey;
        return this;
    }

    /// <summary>
    /// Builds the Schedule entity with configured values.
    /// </summary>
    public Schedule Build()
    {
        return new Schedule(_id, _fecha, _hora, _lugar, _agendaKey);
    }

    /// <summary>
    /// Implicit conversion to Schedule for cleaner test syntax.
    /// </summary>
    public static implicit operator Schedule(ScheduleBuilder builder) => builder.Build();
}

/// <summary>
/// Fluent builder for creating ScheduleHour entities in tests.
/// </summary>
public sealed class ScheduleHourBuilder
{
    private int _id;
    private string _hora = "09:00";
    private int _orden = 1;
    private bool _activo = true;

    public ScheduleHourBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public ScheduleHourBuilder WithHora(string hora)
    {
        _hora = hora;
        return this;
    }

    public ScheduleHourBuilder WithHora(int hour, int minute)
    {
        _hora = $"{hour:D2}:{minute:D2}";
        return this;
    }

    public ScheduleHourBuilder WithOrden(int orden)
    {
        _orden = orden;
        return this;
    }

    public ScheduleHourBuilder Activo(bool activo = true)
    {
        _activo = activo;
        return this;
    }

    public ScheduleHourBuilder Inactivo()
    {
        _activo = false;
        return this;
    }

    /// <summary>
    /// Builds the ScheduleHour entity with configured values.
    /// </summary>
    public ScheduleHour Build()
    {
        return new ScheduleHour(_id, _hora, _orden, _activo);
    }

    /// <summary>
    /// Implicit conversion to ScheduleHour for cleaner test syntax.
    /// </summary>
    public static implicit operator ScheduleHour(ScheduleHourBuilder builder) => builder.Build();

    /// <summary>
    /// Creates a collection of standard schedule hours for testing.
    /// </summary>
    public static IReadOnlyCollection<ScheduleHour> CreateStandardHours()
    {
        return new[]
        {
            new ScheduleHour(1, "08:00", 1, true),
            new ScheduleHour(2, "09:00", 2, true),
            new ScheduleHour(3, "10:00", 3, true),
            new ScheduleHour(4, "11:00", 4, true),
            new ScheduleHour(5, "12:00", 5, true),
            new ScheduleHour(6, "13:00", 6, true),
            new ScheduleHour(7, "14:00", 7, true),
            new ScheduleHour(8, "15:00", 8, true),
            new ScheduleHour(9, "16:00", 9, true),
            new ScheduleHour(10, "17:00", 10, true)
        };
    }
}
