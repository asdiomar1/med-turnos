namespace MedicalCenter.Application.Features.AdminEventFeed;

public static class AdminEventFeedConstants
{
    public const string DefaultActorLabel = "Usuario";
    public const string SourceSystemApi = "api";
    public const string ActionFamilyCatalog = "catalog";
    public const string ActionFamilyImport = "import";

    public const string ActionFamilyPatient = "patient";

    public static class EntityTypes
    {
        public const string Camera = "camara";
        public const string ObraSocial = "obra_social";
        public const string CondicionIva = "condicion_iva";
        public const string Medico = "medico";
        public const string Referente = "referente";
        public const string CampoConfig = "campo_config";
        public const string Importacion = "importacion";
        public const string Paciente = "paciente";
    }

    public static class ActionCodes
    {
        public const string ImportacionCreada = "importacion.creada";
        public const string ImportacionConfirmada = "importacion.confirmada";
        public const string ImportacionFallida = "importacion.fallida";

        public const string CameraCreated = "camera.created";
        public const string CameraUpdated = "camera.updated";
        public const string CameraStatusUpdated = "camera.status_updated";

        public const string ObraSocialCreated = "obra_social.created";
        public const string ObraSocialUpdated = "obra_social.updated";
        public const string ObraSocialStatusUpdated = "obra_social.status_updated";
        public const string ObraSocialConvenioUpdated = "obra_social.convenio_updated";

        public const string CondicionIvaCreated = "condicion_iva.created";
        public const string CondicionIvaUpdated = "condicion_iva.updated";
        public const string CondicionIvaStatusUpdated = "condicion_iva.status_updated";

        public const string MedicoCreated = "medico.created";
        public const string MedicoUpdated = "medico.updated";
        public const string MedicoStatusUpdated = "medico.status_updated";

        public const string ReferenteCreated = "referente.created";
        public const string ReferenteUpdated = "referente.updated";
        public const string ReferenteStatusUpdated = "referente.status_updated";

        public const string CampoConfigCreated = "campo_config.created";
        public const string CampoConfigUpdated = "campo_config.updated";
        public const string CampoConfigDeleted = "campo_config.deleted";

        public const string PacienteCreated = "paciente.created";
        public const string PacienteUpdated = "paciente.updated";
    }

    public static IReadOnlyCollection<AdminEventActionDefinition> CatalogActionDefinitions { get; } =
    [
        new(ActionCodes.CameraCreated, ActionFamilyCatalog, EntityTypes.Camera, "Cámara creada"),
        new(ActionCodes.CameraUpdated, ActionFamilyCatalog, EntityTypes.Camera, "Cámara actualizada"),
        new(ActionCodes.CameraStatusUpdated, ActionFamilyCatalog, EntityTypes.Camera, "Estado de cámara actualizado"),

        new(ActionCodes.ObraSocialCreated, ActionFamilyCatalog, EntityTypes.ObraSocial, "Obra social creada"),
        new(ActionCodes.ObraSocialUpdated, ActionFamilyCatalog, EntityTypes.ObraSocial, "Obra social actualizada"),
        new(ActionCodes.ObraSocialStatusUpdated, ActionFamilyCatalog, EntityTypes.ObraSocial, "Estado de obra social actualizado"),
        new(ActionCodes.ObraSocialConvenioUpdated, ActionFamilyCatalog, EntityTypes.ObraSocial, "Convenio de obra social actualizado"),

        new(ActionCodes.CondicionIvaCreated, ActionFamilyCatalog, EntityTypes.CondicionIva, "Condición IVA creada"),
        new(ActionCodes.CondicionIvaUpdated, ActionFamilyCatalog, EntityTypes.CondicionIva, "Condición IVA actualizada"),
        new(ActionCodes.CondicionIvaStatusUpdated, ActionFamilyCatalog, EntityTypes.CondicionIva, "Estado de condición IVA actualizado"),

        new(ActionCodes.MedicoCreated, ActionFamilyCatalog, EntityTypes.Medico, "Médico creado"),
        new(ActionCodes.MedicoUpdated, ActionFamilyCatalog, EntityTypes.Medico, "Médico actualizado"),
        new(ActionCodes.MedicoStatusUpdated, ActionFamilyCatalog, EntityTypes.Medico, "Estado de médico actualizado"),

        new(ActionCodes.ReferenteCreated, ActionFamilyCatalog, EntityTypes.Referente, "Referente creado"),
        new(ActionCodes.ReferenteUpdated, ActionFamilyCatalog, EntityTypes.Referente, "Referente actualizado"),
        new(ActionCodes.ReferenteStatusUpdated, ActionFamilyCatalog, EntityTypes.Referente, "Estado de referente actualizado"),

        new(ActionCodes.CampoConfigCreated, ActionFamilyCatalog, EntityTypes.CampoConfig, "Campo de configuración creado"),
        new(ActionCodes.CampoConfigUpdated, ActionFamilyCatalog, EntityTypes.CampoConfig, "Campo de configuración actualizado"),
        new(ActionCodes.CampoConfigDeleted, ActionFamilyCatalog, EntityTypes.CampoConfig, "Campo de configuración eliminado")
    ];
}

public sealed record AdminEventActionDefinition(string Code, string Family, string EntityType, string Label);
