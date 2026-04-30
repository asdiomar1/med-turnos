namespace MedicalCenter.Application.DTOs;

public sealed record CancelBlockAppointmentsCommand(DateOnly Fecha, TimeOnly Hora, int CamaraId, Guid PacienteId, string? Motivo);
