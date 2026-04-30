namespace MedicalCenter.Application.DTOs;

public sealed record AppointmentGroupSummary(DateOnly Fecha, IReadOnlyCollection<AppointmentSummary> Slots);
