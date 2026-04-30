namespace MedicalCenter.Application.DTOs;

public sealed record WhatsappDispatchCommand(IReadOnlyCollection<Guid> SlotIds, int? Limit);

public sealed record WhatsappDispatchResult(int Requested, int Found);

public sealed record WhatsappReminderCommand(DateOnly? FechaObjetivo);

public sealed record WhatsappReminderResult(DateOnly FechaObjetivo, int Total);
