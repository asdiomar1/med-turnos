namespace MedicalCenter.Domain.Constants;

public static class WhatsAppConstants
{
    /// <summary>
    /// Default hour for sending WhatsApp messages in Argentina timezone (ART: UTC-3)
    /// </summary>
    public const string DefaultArgentinaSendHour = "14:00";

    /// <summary>
    /// Argentina timezone identifier
    /// </summary>
    public const string ArgentinaTimeZone = "America/Argentina/Buenos_Aires";

    /// <summary>
    /// Maximum message length for WhatsApp
    /// </summary>
    public const int MaxMessageLength = 4096;

    /// <summary>
    /// Template names
    /// </summary>
    public static class Templates
    {
        public const string AppointmentConfirmed = "turno_confirmado";
        public const string AppointmentReminder = "recordatorio_turno";
        public const string AppointmentCancelled = "turno_cancelado";
    }
}