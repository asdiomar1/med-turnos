using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedicalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_event_feed",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action_family = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    agenda_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    paciente_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    medico_id = table.Column<int>(type: "integer", nullable: true),
                    medico_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_system = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_record_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_event_feed", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProfessionalId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: true),
                    TandaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CameraId = table.Column<int>(type: "integer", nullable: true),
                    EsBloqueCompleto = table.Column<bool>(type: "boolean", nullable: false),
                    EsTanda = table.Column<bool>(type: "boolean", nullable: false),
                    ReferidoTercero = table.Column<bool>(type: "boolean", nullable: false),
                    ReferenteId = table.Column<int>(type: "integer", nullable: true),
                    ModalidadCobro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "particular"),
                    ObraSocialId = table.Column<int>(type: "integer", nullable: true),
                    NumeroAutorizacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SesionesAutorizadas = table.Column<int>(type: "integer", nullable: true),
                    CicloObraSocialId = table.Column<Guid>(type: "uuid", nullable: true),
                    IniciarNuevoCicloObraSocial = table.Column<bool>(type: "boolean", nullable: false),
                    ConvenioCorroborado = table.Column<bool>(type: "boolean", nullable: false),
                    MedicoId = table.Column<int>(type: "integer", nullable: true),
                    EsNuevoIngreso = table.Column<bool>(type: "boolean", nullable: false),
                    EsMonoxido = table.Column<bool>(type: "boolean", nullable: false),
                    MonoxidoOrdenMedica = table.Column<bool>(type: "boolean", nullable: false),
                    MonoxidoResumenClinico = table.Column<bool>(type: "boolean", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Lugar = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApartadoPorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApartadoTs = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "camaras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_camaras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "campos_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campos_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "condiciones_iva",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condiciones_iva", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consultas_horarios_config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Hora = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultas_horarios_config", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "consultas_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    MedicoId = table.Column<int>(type: "integer", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ObservacionesAdmin = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ConfirmadoAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    CerradoAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CerradoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultas_slots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "daily_closings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DetallesJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReopenedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    MotivoReapertura = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReopenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_closings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dias_laborables_config",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dias_semana = table.Column<short[]>(type: "smallint[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dias_laborables_config", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "historia_clinica_evoluciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consulta_slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    medico_id = table.Column<int>(type: "integer", nullable: false),
                    autor_perfil_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_clinica = table.Column<DateOnly>(type: "date", nullable: false),
                    titulo = table.Column<string>(type: "text", nullable: true),
                    nota = table.Column<string>(type: "text", nullable: false),
                    diagnostico_impresion = table.Column<string>(type: "text", nullable: true),
                    indicaciones = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historia_clinica_evoluciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "historial_bloques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CamaraId = table.Column<int>(type: "integer", nullable: true),
                    SlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Lugar = table.Column<int>(type: "integer", nullable: true),
                    Accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    RealizadoPor = table.Column<Guid>(type: "uuid", nullable: true),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    ReferidoTercero = table.Column<bool>(type: "boolean", nullable: false),
                    ModalidadCobro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "particular"),
                    ObraSocialId = table.Column<int>(type: "integer", nullable: true),
                    NumeroAutorizacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ObraSocialValidadaPor = table.Column<Guid>(type: "uuid", nullable: true),
                    ObraSocialValidadaAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    MedicoId = table.Column<int>(type: "integer", nullable: true),
                    EsNuevoIngreso = table.Column<bool>(type: "boolean", nullable: false),
                    ReferenteId = table.Column<int>(type: "integer", nullable: true),
                    TandaId = table.Column<Guid>(type: "uuid", nullable: true),
                    SesionesAutorizadas = table.Column<int>(type: "integer", nullable: true),
                    CicloObraSocialId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_bloques", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "historias_clinicas",
                columns: table => new
                {
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<long>(type: "bigint", nullable: false),
                    antecedentes = table.Column<string>(type: "text", nullable: true),
                    alergias = table.Column<string>(type: "text", nullable: true),
                    medicacion_actual = table.Column<string>(type: "text", nullable: true),
                    observaciones_relevantes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historias_clinicas", x => x.paciente_id);
                });

            migrationBuilder.CreateTable(
                name: "horarios_config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Hora = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarios_config", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "importaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    storage_bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    total_filas = table.Column<int>(type: "integer", nullable: false),
                    filas_validas = table.Column<int>(type: "integer", nullable: false),
                    filas_con_error = table.Column<int>(type: "integer", nullable: false),
                    filas_insertadas = table.Column<int>(type: "integer", nullable: false),
                    filas_actualizadas = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medicos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    perfil_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notas_paciente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    paciente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    autor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mensaje = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_paciente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "obras_sociales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    tiene_convenio = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    abreviatura = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obras_sociales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "operation_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResponsePayload = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operation_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentoIdentidad = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentoIdentidadNormalizado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Nacionalidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CondicionIvaId = table.Column<int>(type: "integer", nullable: false),
                    ObraSocialId = table.Column<int>(type: "integer", nullable: true),
                    NumeroCredencialObraSocial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PortalHabilitado = table.Column<bool>(type: "boolean", nullable: false),
                    RequiereResetPortal = table.Column<bool>(type: "boolean", nullable: false),
                    LoginIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Claustrofobico = table.Column<bool>(type: "boolean", nullable: false),
                    Notas = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DatosExtra = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    OptInWhatsapp = table.Column<bool>(type: "boolean", nullable: false),
                    OptInSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "portal_access_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryChannel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedToMasked = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portal_access_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "professionals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_professionals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referentes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referentes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JwtId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_staff = table.Column<bool>(type: "boolean", nullable: false),
                    default_home = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    permissions = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Lugar = table.Column<int>(type: "integer", nullable: false),
                    AgendaKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sesiones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CamaraId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ModalidadCobro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "particular"),
                    ObraSocialId = table.Column<int>(type: "integer", nullable: true),
                    CierreId = table.Column<Guid>(type: "uuid", nullable: true),
                    NumeroAutorizacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SesionesAutorizadas = table.Column<int>(type: "integer", nullable: true),
                    CicloObraSocialId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "turnos_fuera_horario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notas = table.Column<string>(type: "text", nullable: true),
                    CreadoPor = table.Column<Guid>(type: "uuid", nullable: false),
                    OperadorCamaraId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    EsMonoxido = table.Column<bool>(type: "boolean", nullable: false),
                    MonoxidoOrdenMedica = table.Column<bool>(type: "boolean", nullable: false),
                    MonoxidoResumenClinico = table.Column<bool>(type: "boolean", nullable: false),
                    MonoxidoMedicoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turnos_fuera_horario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsStaff = table.Column<bool>(type: "boolean", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_dispatch_queue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tanda_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_dispatch_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_message_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    whatsapp_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone_e164 = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    incoming_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    confirmed_incoming_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    raw_context = table.Column<string>(type: "jsonb", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_message_actions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_message_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    message_text = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_message_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tanda_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_id = table.Column<long>(type: "bigint", nullable: true),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    meta_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_e164 = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    request_payload = table.Column<string>(type: "jsonb", nullable: false),
                    response_payload = table.Column<string>(type: "jsonb", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_templates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    meta_template_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    language_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    meta_object = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entry_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false),
                    processing_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_whatsapp_webhook_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "importacion_errores",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    importacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importacion_errores", x => x.id);
                    table.ForeignKey(
                        name: "FK_importacion_errores_importaciones_importacion_id",
                        column: x => x.importacion_id,
                        principalTable: "importaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomColorsJson = table.Column<string>(type: "jsonb", nullable: true),
                    TurnosLayout = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FontScale = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_event_feed_action_code",
                table: "admin_event_feed",
                column: "action_code");

            migrationBuilder.CreateIndex(
                name: "IX_admin_event_feed_actor_user_id",
                table: "admin_event_feed",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_event_feed_occurred_at_id",
                table: "admin_event_feed",
                columns: new[] { "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_event_feed_source_system_source_record_key",
                table: "admin_event_feed",
                columns: new[] { "source_system", "source_record_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_Fecha_Hora_CameraId",
                table: "appointments",
                columns: new[] { "Fecha", "Hora", "CameraId" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_PatientId_Fecha_Hora",
                table: "appointments",
                columns: new[] { "PatientId", "Fecha", "Hora" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_ScheduleId_Fecha_Hora_Lugar",
                table: "appointments",
                columns: new[] { "ScheduleId", "Fecha", "Hora", "Lugar" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_TandaId",
                table: "appointments",
                column: "TandaId");

            migrationBuilder.CreateIndex(
                name: "IX_campos_config_orden",
                table: "campos_config",
                column: "orden");

            migrationBuilder.CreateIndex(
                name: "IX_condiciones_iva_nombre",
                table: "condiciones_iva",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "IX_consultas_horarios_config_Hora",
                table: "consultas_horarios_config",
                column: "Hora",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consultas_horarios_config_Orden",
                table: "consultas_horarios_config",
                column: "Orden",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consultas_slots_Estado",
                table: "consultas_slots",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_consultas_slots_Fecha_Hora",
                table: "consultas_slots",
                columns: new[] { "Fecha", "Hora" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consultas_slots_MedicoId",
                table: "consultas_slots",
                column: "MedicoId");

            migrationBuilder.CreateIndex(
                name: "IX_consultas_slots_PacienteId",
                table: "consultas_slots",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_closings_Fecha",
                table: "daily_closings",
                column: "Fecha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_historia_clinica_evoluciones_paciente_fecha",
                table: "historia_clinica_evoluciones",
                columns: new[] { "paciente_id", "fecha_clinica", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_historial_bloques_Fecha_Hora_CamaraId",
                table: "historial_bloques",
                columns: new[] { "Fecha", "Hora", "CamaraId" });

            migrationBuilder.CreateIndex(
                name: "IX_historial_bloques_SlotId",
                table: "historial_bloques",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_historial_bloques_TandaId",
                table: "historial_bloques",
                column: "TandaId");

            migrationBuilder.CreateIndex(
                name: "IX_horarios_config_Hora",
                table: "horarios_config",
                column: "Hora",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_horarios_config_Orden",
                table: "horarios_config",
                column: "Orden",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_importacion_errores_importacion",
                table: "importacion_errores",
                columns: new[] { "importacion_id", "row_number" });

            migrationBuilder.CreateIndex(
                name: "ix_importaciones_usuario",
                table: "importaciones",
                columns: new[] { "usuario_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ux_importaciones_storage_key",
                table: "importaciones",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_medicos_nombre",
                table: "medicos",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "notas_paciente_created_at_idx",
                table: "notas_paciente",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "notas_paciente_paciente_id_idx",
                table: "notas_paciente",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_obras_sociales_nombre",
                table: "obras_sociales",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "IX_operation_requests_Operation_Key",
                table: "operation_requests",
                columns: new[] { "Operation", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patients_DocumentoIdentidadNormalizado",
                table: "patients",
                column: "DocumentoIdentidadNormalizado");

            migrationBuilder.CreateIndex(
                name: "IX_patients_LoginIdentifier",
                table: "patients",
                column: "LoginIdentifier",
                unique: true,
                filter: "\"LoginIdentifier\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_portal_access_tokens_PacienteId_Purpose",
                table: "portal_access_tokens",
                columns: new[] { "PacienteId", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_portal_access_tokens_TokenHash",
                table: "portal_access_tokens",
                column: "TokenHash",
                unique: true,
                filter: "\"UsedAt\" IS NULL AND \"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_referentes_nombre_tipo",
                table: "referentes",
                columns: new[] { "nombre", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_Status",
                table: "refresh_tokens",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_Fecha",
                table: "sesiones",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_PacienteId",
                table: "sesiones",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_SlotId",
                table: "sesiones",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_turnos_fuera_horario_Fecha_Hora_PacienteId",
                table: "turnos_fuera_horario",
                columns: new[] { "Fecha", "Hora", "PacienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_turnos_fuera_horario_PacienteId",
                table: "turnos_fuera_horario",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Identifier",
                table: "users",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_PatientId",
                table: "users",
                column: "PatientId",
                unique: true,
                filter: "\"PatientId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_dispatch_queue_idempotency_key",
                table: "whatsapp_dispatch_queue",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_dispatch_queue_slot_id_kind",
                table: "whatsapp_dispatch_queue",
                columns: new[] { "slot_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_dispatch_queue_status_created_at",
                table: "whatsapp_dispatch_queue",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_dispatch_queue_tanda_id_kind",
                table: "whatsapp_dispatch_queue",
                columns: new[] { "tanda_id", "kind" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_actions_phone_e164_status_created_at",
                table: "whatsapp_message_actions",
                columns: new[] { "phone_e164", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_actions_slot_id_created_at",
                table: "whatsapp_message_actions",
                columns: new[] { "slot_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_message_actions_whatsapp_message_id_created_at",
                table: "whatsapp_message_actions",
                columns: new[] { "whatsapp_message_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_messages_idempotency_key",
                table: "whatsapp_messages",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_messages_meta_message_id_created_at",
                table: "whatsapp_messages",
                columns: new[] { "meta_message_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_messages_patient_id_kind_created_at",
                table: "whatsapp_messages",
                columns: new[] { "patient_id", "kind", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_messages_slot_id_kind_created_at",
                table: "whatsapp_messages",
                columns: new[] { "slot_id", "kind", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_templates_key",
                table: "whatsapp_templates",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_templates_kind_active",
                table: "whatsapp_templates",
                columns: new[] { "kind", "active" });

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_templates_meta_template_name",
                table: "whatsapp_templates",
                column: "meta_template_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_webhook_events_message_id",
                table: "whatsapp_webhook_events",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_webhook_events_received_at_id",
                table: "whatsapp_webhook_events",
                columns: new[] { "received_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_event_feed");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "blocks");

            migrationBuilder.DropTable(
                name: "camaras");

            migrationBuilder.DropTable(
                name: "campos_config");

            migrationBuilder.DropTable(
                name: "condiciones_iva");

            migrationBuilder.DropTable(
                name: "consultas_horarios_config");

            migrationBuilder.DropTable(
                name: "consultas_slots");

            migrationBuilder.DropTable(
                name: "daily_closings");

            migrationBuilder.DropTable(
                name: "dias_laborables_config");

            migrationBuilder.DropTable(
                name: "historia_clinica_evoluciones");

            migrationBuilder.DropTable(
                name: "historial_bloques");

            migrationBuilder.DropTable(
                name: "historias_clinicas");

            migrationBuilder.DropTable(
                name: "horarios_config");

            migrationBuilder.DropTable(
                name: "importacion_errores");

            migrationBuilder.DropTable(
                name: "medicos");

            migrationBuilder.DropTable(
                name: "notas_paciente");

            migrationBuilder.DropTable(
                name: "obras_sociales");

            migrationBuilder.DropTable(
                name: "operation_requests");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropTable(
                name: "portal_access_tokens");

            migrationBuilder.DropTable(
                name: "professionals");

            migrationBuilder.DropTable(
                name: "referentes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "schedules");

            migrationBuilder.DropTable(
                name: "sesiones");

            migrationBuilder.DropTable(
                name: "turnos_fuera_horario");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "whatsapp_dispatch_queue");

            migrationBuilder.DropTable(
                name: "whatsapp_message_actions");

            migrationBuilder.DropTable(
                name: "whatsapp_message_settings");

            migrationBuilder.DropTable(
                name: "whatsapp_messages");

            migrationBuilder.DropTable(
                name: "whatsapp_templates");

            migrationBuilder.DropTable(
                name: "whatsapp_webhook_events");

            migrationBuilder.DropTable(
                name: "importaciones");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
