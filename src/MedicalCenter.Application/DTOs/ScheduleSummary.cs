namespace MedicalCenter.Application.DTOs;

public sealed record ScheduleSummary(Guid Id, DateOnly Fecha, TimeOnly Hora, int Lugar, string AgendaKey);
