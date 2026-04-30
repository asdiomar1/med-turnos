using MedicalCenter.Application.DTOs;
using MedicalCenter.Domain.Entities;

namespace MedicalCenter.Application.Features.WhatsApp;

public interface IWhatsappService
{
    Task<WhatsappDispatchResult> DispatchAsync(WhatsappDispatchCommand command, CancellationToken cancellationToken);
    Task<WhatsappReminderResult> SendRemindersAsync(WhatsappReminderCommand command, CancellationToken cancellationToken);
    Task EnqueueTurnoConfirmadoAsync(Appointment appointment, string triggerSource, CancellationToken cancellationToken);
    Task EnqueueTurnoCancelacionAsync(Appointment appointment, string triggerSource, string? operationKey, CancellationToken cancellationToken);
    Task EnqueueTurnosCancelacionAsync(Guid patientId, IReadOnlyCollection<Appointment> appointments, string operationKey, string triggerSource, CancellationToken cancellationToken);
}
