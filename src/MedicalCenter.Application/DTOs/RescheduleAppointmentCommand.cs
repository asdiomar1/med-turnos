namespace MedicalCenter.Application.DTOs;

public sealed record RescheduleAppointmentCommand(Guid TargetSlotId, string Scope);
