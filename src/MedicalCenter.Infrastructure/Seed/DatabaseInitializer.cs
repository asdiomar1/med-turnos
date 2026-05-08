using System.IO;
using MedicalCenter.Application.Abstractions.Common;
using MedicalCenter.Domain.Constants;
using MedicalCenter.Domain.Entities;
using MedicalCenter.Infrastructure.Options;
using MedicalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MedicalCenter.Infrastructure.Seed;

public static class DatabaseInitializer
{
    // Project root - used to locate seed files
    private static readonly string ProjectRoot = FindProjectRoot();

    private static string FindProjectRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "MedicalCenter.sln")))
        {
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return current ?? Directory.GetCurrentDirectory();
    }

    private static readonly (long Id, string Key, string Nombre, string? Descripcion, string Modulo)[] Permissions =
    [
        (1, PermissionConstants.AppAdminPanelAccess, "Acceso panel interno", "Permite ingresar al panel de administración", PermissionConstants.ModuleApp),
        (2, PermissionConstants.PortalAccess, "Acceso portal paciente", "Permite ingresar al portal paciente", PermissionConstants.ModulePortal),
        (3, PermissionConstants.PortalTurnosReserve, "Reservar turnos portal", "Permite reservar turnos desde el portal", PermissionConstants.ModulePortal),
        (4, PermissionConstants.PortalTurnosCancel, "Cancelar turnos portal", "Permite cancelar turnos propios desde el portal", PermissionConstants.ModulePortal),
        (5, PermissionConstants.PortalSelfUpdate, "Editar datos propios portal", "Permite editar datos personales del paciente", PermissionConstants.ModulePortal),
        (6, PermissionConstants.DashboardRead, "Ver dashboard", "Permite leer métricas y agenda del dashboard", PermissionConstants.ModuleDashboard),
        (7, PermissionConstants.TurnosRead, "Ver turnos cámara", "Permite consultar agenda de turnos de cámara", PermissionConstants.ModuleTurnos),
        (8, PermissionConstants.TurnosAsignar, "Asignar turnos cámara", "Permite asignar turnos de cámara", PermissionConstants.ModuleTurnos),
        (9, PermissionConstants.TurnosCancelar, "Cancelar turnos cámara", "Permite cancelar turnos de cámara", PermissionConstants.ModuleTurnos),
        (10, PermissionConstants.TurnosReprogramar, "Reprogramar turnos cámara", "Permite reprogramar turnos de cámara", PermissionConstants.ModuleTurnos),
        (11, PermissionConstants.TurnosApartar, "Apartar turnos cámara", "Permite apartar turnos de cámara", PermissionConstants.ModuleTurnos),
        (12, PermissionConstants.TurnosConfirmarApartado, "Confirmar apartados", "Permite confirmar turnos apartados", PermissionConstants.ModuleTurnos),
        (13, PermissionConstants.TurnosLiberarApartado, "Liberar apartados", "Permite liberar turnos apartados", PermissionConstants.ModuleTurnos),
        (14, PermissionConstants.TurnosBloqueCompleto, "Operar bloque completo", "Permite asignar/cancelar bloque completo", PermissionConstants.ModuleTurnos),
        (15, PermissionConstants.TurnosTanda, "Operar tandas", "Permite crear y gestionar tandas", PermissionConstants.ModuleTurnos),
        (16, PermissionConstants.TurnosCierreDiario, "Gestionar cierre diario", "Permite confirmar/reabrir cierre diario", PermissionConstants.ModuleTurnos),
        (17, PermissionConstants.TurnosFueraHorario, "Gestionar turnos fuera de horario", "Permite crear/cancelar turnos fuera de horario", PermissionConstants.ModuleTurnos),
        (18, PermissionConstants.ConsultasRead, "Ver consultas", "Permite ver agenda de consultas médicas", PermissionConstants.ModuleConsultas),
        (19, PermissionConstants.ConsultasAsignar, "Asignar consultas", "Permite asignar consultas médicas", PermissionConstants.ModuleConsultas),
        (20, PermissionConstants.ConsultasCancelar, "Cancelar consultas", "Permite cancelar consultas médicas", PermissionConstants.ModuleConsultas),
        (21, PermissionConstants.ConsultasReprogramar, "Reprogramar consultas", "Permite reprogramar consultas médicas", PermissionConstants.ModuleConsultas),
        (22, PermissionConstants.ConsultasCerrar, "Cerrar consultas", "Permite cerrar consultas médicas", PermissionConstants.ModuleConsultas),
        (24, PermissionConstants.HistoriaClinicaEditarFicha, "Editar ficha clínica", "Permite editar ficha clínica resumida", PermissionConstants.ModuleHistoriaClinica),
        (25, PermissionConstants.HistoriaClinicaCrearEvolucion, "Crear evoluciones", "Permite cargar evoluciones clínicas", PermissionConstants.ModuleHistoriaClinica),
        (26, PermissionConstants.HistoriaClinicaEditarNumero, "Editar número HC", "Permite cambiar número de historia clínica", PermissionConstants.ModuleHistoriaClinica),
        (27, PermissionConstants.PacientesRead, "Ver pacientes", "Permite consultar listado y detalle de pacientes", PermissionConstants.ModulePacientes),
        (28, PermissionConstants.PacientesCrear, "Crear pacientes", "Permite alta de pacientes", PermissionConstants.ModulePacientes),
        (29, PermissionConstants.PacientesEditar, "Editar pacientes", "Permite edición de pacientes", PermissionConstants.ModulePacientes),
        (30, PermissionConstants.PacientesPortalManage, "Gestionar portal paciente", "Permite configurar acceso al portal paciente", PermissionConstants.ModulePacientes),
        (31, PermissionConstants.ConfigRead, "Ver configuración", "Permite consultar catálogos y configuración", PermissionConstants.ModuleConfiguracion),
        (32, PermissionConstants.ConfigHorariosManage, "Gestionar horarios", "Permite editar horarios y apertura/reparación de slots", PermissionConstants.ModuleConfiguracion),
        (33, PermissionConstants.ConfigCamarasManage, "Gestionar cámaras", "Permite crear/editar/activar cámaras", PermissionConstants.ModuleConfiguracion),
        (34, PermissionConstants.ConfigCatalogosManage, "Gestionar catálogos", "Permite editar médicos, referentes, obras sociales y campos", PermissionConstants.ModuleConfiguracion),
        (35, PermissionConstants.ConfigWhatsappManage, "Gestionar configuración WhatsApp", "Permite editar plantillas y settings WhatsApp", PermissionConstants.ModuleConfiguracion),
        (36, PermissionConstants.ActividadRead, "Ver actividad", "Permite consultar feed de actividad y auditoría", PermissionConstants.ModuleActividad),
        (37, PermissionConstants.ReportesRead, "Ver reportes", "Permite acceder a cierres y reportes", PermissionConstants.ModuleReportes),
        (38, PermissionConstants.ReportesExport, "Exportar reportes", "Permite exportar cierres y reportes", PermissionConstants.ModuleReportes),
        (39, PermissionConstants.StaffRead, "Ver usuarios internos", "Permite listar usuarios internos", PermissionConstants.ModuleUsuarios),
        (40, PermissionConstants.StaffManage, "Gestionar usuarios internos", "Permite crear usuarios staff y asignar roles", PermissionConstants.ModuleUsuarios),
        (41, PermissionConstants.RbacRead, "Ver roles y permisos", "Permite ver configuración RBAC", PermissionConstants.ModuleRbac),
        (42, PermissionConstants.RbacManage, "Gestionar roles y permisos", "Permite editar roles, permisos y asignaciones", PermissionConstants.ModuleRbac),
        (43, PermissionConstants.WhatsappDispatch, "Despachar WhatsApp", "Permite ejecutar envíos y recordatorios manuales", PermissionConstants.ModuleWhatsapp),
        (44, PermissionConstants.HistoriaClinicaResumenRead, "Ver resumen HC", "Permite ver número de HC en el listado de pacientes", PermissionConstants.ModuleHistoriaClinica),
        (47, PermissionConstants.HistoriaClinicaDetalleRead, "Ver detalle HC", "Permite ver ficha clínica completa y evoluciones", PermissionConstants.ModuleHistoriaClinica)
    ];

    private static readonly (long Id, string Slug, string Nombre, string? Descripcion, bool IsStaff, string DefaultHome)[] Roles =
    [
        (1, PermissionConstants.RolePaciente, "Paciente", "Acceso exclusivo al portal de paciente", false, PermissionConstants.RoutePaciente),
        (2, PermissionConstants.RoleStaffInactivo, "Staff inactivo", "Cuenta interna desactivada sin permisos operativos", true, PermissionConstants.RouteLogin),
        (3, PermissionConstants.RoleOperadorCamara, "Operador de cámara", "Operación diaria de agenda y cierres operativos", true, PermissionConstants.RouteUsuarioTurnos),
        (4, PermissionConstants.RoleSecretaria, "Secretaría", "Gestión operativa de pacientes, turnos y consultas", true, PermissionConstants.RouteUsuarioPacientes),
        (5, PermissionConstants.RoleMedico, "Médico", "Gestión clínica de consultas e historias clínicas", true, PermissionConstants.RouteUsuarioHistoriasClinicas),
        (6, PermissionConstants.RoleAdmin, "Administrador", "Acceso total y gestión del sistema", true, PermissionConstants.RouteUsuario)
    ];

    private static readonly (string RoleSlug, string[] PermissionKeys)[] RolePermissions =
    [
        (PermissionConstants.RolePaciente, [PermissionConstants.PortalAccess, PermissionConstants.PortalTurnosReserve, PermissionConstants.PortalTurnosCancel, PermissionConstants.PortalSelfUpdate]),
        (PermissionConstants.RoleStaffInactivo, []),
        (PermissionConstants.RoleOperadorCamara, [
            PermissionConstants.AppAdminPanelAccess,
            PermissionConstants.DashboardRead,
            PermissionConstants.TurnosRead,
            PermissionConstants.TurnosAsignar,
            PermissionConstants.TurnosCancelar,
            PermissionConstants.TurnosReprogramar,
            PermissionConstants.TurnosApartar,
            PermissionConstants.TurnosConfirmarApartado,
            PermissionConstants.TurnosLiberarApartado,
            PermissionConstants.TurnosBloqueCompleto,
            PermissionConstants.TurnosTanda,
            PermissionConstants.TurnosCierreDiario,
            PermissionConstants.TurnosFueraHorario,
            PermissionConstants.ConfigHorariosManage,
            PermissionConstants.ConfigCamarasManage,
            PermissionConstants.ConfigCatalogosManage,
            PermissionConstants.ActividadRead,
            PermissionConstants.HistoriaClinicaResumenRead
        ]),
        (PermissionConstants.RoleSecretaria, [
            PermissionConstants.AppAdminPanelAccess,
            PermissionConstants.DashboardRead,
            PermissionConstants.StaffRead,
            PermissionConstants.PacientesRead,
            PermissionConstants.PacientesCrear,
            PermissionConstants.PacientesEditar,
            PermissionConstants.PacientesPortalManage,
            PermissionConstants.TurnosRead,
            PermissionConstants.TurnosAsignar,
            PermissionConstants.TurnosCancelar,
            PermissionConstants.TurnosReprogramar,
            PermissionConstants.TurnosApartar,
            PermissionConstants.TurnosConfirmarApartado,
            PermissionConstants.TurnosLiberarApartado,
            PermissionConstants.TurnosBloqueCompleto,
            PermissionConstants.TurnosTanda,
            PermissionConstants.ConsultasRead,
            PermissionConstants.ConsultasAsignar,
            PermissionConstants.ConsultasCancelar,
            PermissionConstants.ConsultasReprogramar,
            PermissionConstants.ConsultasCerrar,
            PermissionConstants.ConfigHorariosManage,
            PermissionConstants.ConfigCamarasManage,
            PermissionConstants.ConfigCatalogosManage,
            PermissionConstants.ActividadRead,
            PermissionConstants.ReportesRead,
            PermissionConstants.ReportesExport,
            PermissionConstants.HistoriaClinicaResumenRead
        ]),
        (PermissionConstants.RoleMedico, [
            PermissionConstants.ConsultasRead,
            PermissionConstants.ConsultasAsignar,
            PermissionConstants.ConsultasCancelar,
            PermissionConstants.ConsultasReprogramar,
            PermissionConstants.ConsultasCerrar,
            PermissionConstants.HistoriaClinicaEditarFicha,
            PermissionConstants.HistoriaClinicaCrearEvolucion,
            PermissionConstants.HistoriaClinicaEditarNumero,
            PermissionConstants.PacientesRead,
            PermissionConstants.HistoriaClinicaResumenRead
        ]),
        (PermissionConstants.RoleAdmin, [
            PermissionConstants.AppAdminPanelAccess,
            PermissionConstants.PortalAccess,
            PermissionConstants.PortalTurnosReserve,
            PermissionConstants.PortalTurnosCancel,
            PermissionConstants.PortalSelfUpdate,
            PermissionConstants.DashboardRead,
            PermissionConstants.TurnosRead,
            PermissionConstants.TurnosAsignar,
            PermissionConstants.TurnosCancelar,
            PermissionConstants.TurnosReprogramar,
            PermissionConstants.TurnosApartar,
            PermissionConstants.TurnosConfirmarApartado,
            PermissionConstants.TurnosLiberarApartado,
            PermissionConstants.TurnosBloqueCompleto,
            PermissionConstants.TurnosTanda,
            PermissionConstants.TurnosCierreDiario,
            PermissionConstants.TurnosFueraHorario,
            PermissionConstants.ConsultasRead,
            PermissionConstants.ConsultasAsignar,
            PermissionConstants.ConsultasCancelar,
            PermissionConstants.ConsultasReprogramar,
            PermissionConstants.ConsultasCerrar,
            PermissionConstants.HistoriaClinicaEditarFicha,
            PermissionConstants.HistoriaClinicaCrearEvolucion,
            PermissionConstants.HistoriaClinicaEditarNumero,
            PermissionConstants.PacientesRead,
            PermissionConstants.PacientesCrear,
            PermissionConstants.PacientesEditar,
            PermissionConstants.PacientesPortalManage,
            PermissionConstants.ConfigRead,
            PermissionConstants.ConfigHorariosManage,
            PermissionConstants.ConfigCamarasManage,
            PermissionConstants.ConfigCatalogosManage,
            PermissionConstants.ConfigWhatsappManage,
            PermissionConstants.ActividadRead,
            PermissionConstants.ReportesRead,
            PermissionConstants.ReportesExport,
            PermissionConstants.StaffRead,
            PermissionConstants.StaffManage,
            PermissionConstants.RbacRead,
            PermissionConstants.RbacManage,
            PermissionConstants.WhatsappDispatch,
            PermissionConstants.HistoriaClinicaResumenRead,
            PermissionConstants.HistoriaClinicaDetalleRead
        ])
    ];

    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MedicalCenterDbContext>();
        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        await SquashMigrationsAsync(dbContext, cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await EnsureRbacSchemaAsync(dbContext, cancellationToken);
        await ResetAndSeedRbacAsync(dbContext, cancellationToken);
        await EnsureCatalogDataAsync(dbContext, cancellationToken);
        await SeedAdminUserAsync(dbContext, seedOptions, passwordHasher, cancellationToken);

        // Load development data if in Development mode
        Console.WriteLine($"[DEBUG] Environment: {environment.EnvironmentName}, IsDevelopment: {environment.IsDevelopment()}");
        
        if (environment.IsDevelopment())
        {
            await LoadDevDataAsync(dbContext, cancellationToken);
        }
    }

    /// <summary>
    /// Loads development data from schema/dev-data-only-YYYYMMDD-HHMMSS.sql
    /// Only runs in Development mode.
    /// </summary>
    private static async Task LoadDevDataAsync(MedicalCenterDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            // Search multiple candidate paths to support both local and Docker environments
            var candidateDirs = new List<string>
            {
                Path.Combine(ProjectRoot, "schema"),
                "/app/schema", // Docker volume mount location
            };

            // Fallback for when ProjectRoot resolves to root in Docker
            if (ProjectRoot != "/app")
            {
                candidateDirs.Add("/schema");
            }

            string? schemaDir = null;
            foreach (var candidate in candidateDirs)
            {
                Console.WriteLine($"[DEBUG] Checking schema candidate: {candidate} - Exists: {Directory.Exists(candidate)}");
                if (Directory.Exists(candidate))
                {
                    schemaDir = candidate;
                    break;
                }
            }

            if (schemaDir == null)
            {
                Console.WriteLine($"[DEBUG] No schema directory found in any candidate location");
                return;
            }

            var devDataFile = Directory.GetFiles(schemaDir, "dev-data-only-*.sql")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (devDataFile == null)
            {
                Console.WriteLine($"[DEBUG] No dev data file found in {schemaDir}/");
                return;
            }

            Console.WriteLine($"Loading development data from {Path.GetFileName(devDataFile)}...");

            var executed = await ExecuteSqlScriptAsync(dbContext, devDataFile, cancellationToken);

            Console.WriteLine($"Development data loaded successfully ({executed} statements executed)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load dev data: {ex.Message}");
            // Don't fail - dev data is optional
        }
    }

    /// <summary>
    /// Executes a pg_dump plain-text SQL file line-by-line, skipping metadata/comments
    /// and grouping multi-line statements terminated by a semicolon at end-of-line.
    /// Uses the underlying Npgsql connection to bypass EF Core parameter formatting,
    /// which breaks on JSON strings containing '{' and '}'.
    /// 
    /// Uses SAVEPOINTS to isolate each statement - if one fails, PostgreSQL rolls back
    /// only that statement and continues with the rest (instead of aborting the entire transaction).
    /// </summary>
    private static async Task<int> ExecuteSqlScriptAsync(
        MedicalCenterDbContext dbContext,
        string filePath,
        CancellationToken cancellationToken)
    {
        var executed = 0;
        var skipped = 0;
        var buffer = new System.Text.StringBuilder();
        var savepointIndex = 0;

        await using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);

        // Ensure connection is open
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var trimmed = line.Trim();

            if (ShouldSkipSqlLine(trimmed))
                continue;

            buffer.AppendLine(line);

            if (!trimmed.EndsWith(';'))
                continue;

            var statement = buffer.ToString().Trim();
            buffer.Clear();

            if (string.IsNullOrEmpty(statement))
                continue;

            var savepointName = $"sp_{savepointIndex++}";
            var executedSuccessfully = await TryExecuteStatementWithSavepointAsync(
                connection,
                transaction,
                statement,
                savepointName,
                cancellationToken);

            if (executedSuccessfully)
                executed++;
            else
                skipped++;
        }

        await transaction.CommitAsync(cancellationToken);
        Console.WriteLine($"[DEBUG] Dev data summary: {executed} executed, {skipped} skipped");
        return executed;
    }

    private static bool ShouldSkipSqlLine(string trimmedLine) =>
        string.IsNullOrEmpty(trimmedLine)
        || trimmedLine.StartsWith("--", StringComparison.Ordinal)
        || IsPgDumpMetadataLine(trimmedLine);

    private static async Task<bool> TryExecuteStatementWithSavepointAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string statement,
        string savepointName,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteNonQueryAsync(connection, transaction, $"SAVEPOINT {savepointName}", cancellationToken);
            await ExecuteNonQueryAsync(connection, transaction, statement, cancellationToken);
            await ExecuteNonQueryAsync(connection, transaction, $"RELEASE SAVEPOINT {savepointName}", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            await RollbackSavepointQuietlyAsync(connection, transaction, savepointName, cancellationToken);

            var message = ex.Message.Trim();
            if (!message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine($"[DEBUG] SQL statement skipped: {message}");

            return false;
        }
    }

    private static async Task ExecuteNonQueryAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RollbackSavepointQuietlyAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string savepointName,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteNonQueryAsync(connection, transaction, $"ROLLBACK TO SAVEPOINT {savepointName}", cancellationToken);
        }
        catch (Exception rollbackEx)
        {
            Console.WriteLine($"[DEBUG] Failed to rollback savepoint: {rollbackEx.Message}");
        }
    }

    /// <summary>
    /// Detects lines produced by pg_dump that look like metadata but are not commented out.
    /// Examples: "Type: TABLE DATA;", "Schema: public;", "Owner: postgres;"
    /// </summary>
    private static bool IsPgDumpMetadataLine(string line)
    {
        // Fast path: if it doesn't contain a colon it's not metadata
        if (!line.Contains(':'))
            return false;

        // These are common pg_dump metadata tokens that appear as standalone pseudo-statements
        var metadataTokens = new[] { "Type:", "Schema:", "Owner:" };
        return metadataTokens.Any(token => line.StartsWith(token, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Handles the migration squash on existing databases.
    /// Clears __EFMigrationsHistory and marks the squashed migration as applied.
    /// This is idempotent — safe to run multiple times.
    /// </summary>
    private static async Task SquashMigrationsAsync(MedicalCenterDbContext dbContext, CancellationToken cancellationToken)
    {
        // Get EF Core version dynamically from the executing assembly
        var efCoreVersion = typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly.GetName().Version?.ToString() ?? "8.0.11";
        
        try
        {
            // Unconditionally clear old migrations and insert the squashed migration.
            // This works because if the table doesn't exist, it throws and we catch it.
            // If the squashed migration is already there, the DELETE removes it and we re-insert.
            // Using parameterized query to avoid SQL injection (EF will use the value as a literal since it's a string)
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                DELETE FROM "__EFMigrationsHistory";
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ('20260430180403_InitialCreate', @p0);
                """,
                cancellationToken,
                efCoreVersion);
        }
        catch (Exception ex) when (ex is Npgsql.PostgresException { SqlState: "42P01" } or InvalidOperationException)
        {
            // Table doesn't exist yet (42P01 = undefined_table) or other expected error.
            // MigrateAsync will create the table and apply InitialCreate normally.
        }
    }

    public static async Task EnsureRbacSchemaAsync(MedicalCenterDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE EXTENSION IF NOT EXISTS pgcrypto;

            DO $$
            BEGIN
                CREATE TYPE public.rol_usuario AS ENUM ('admin', 'paciente');
            EXCEPTION
                WHEN duplicate_object THEN NULL;
            END $$;

            CREATE TABLE IF NOT EXISTS public.perfiles (
                id uuid DEFAULT gen_random_uuid() NOT NULL,
                nombre text NOT NULL,
                email text NULL,
                telefono text NULL,
                documento_identidad text NULL,
                nacionalidad text NULL,
                condicion_iva_id integer NULL,
                obra_social_id integer NULL,
                rol public.rol_usuario NOT NULL DEFAULT 'paciente',
                created_at timestamp with time zone DEFAULT now(),
                claustrofobico boolean DEFAULT false,
                notas text NULL,
                datos_extra jsonb DEFAULT '{{}}'::jsonb,
                opt_in_whatsapp boolean NOT NULL DEFAULT false,
                opt_in_at timestamp with time zone NULL,
                opt_in_source text NULL,
                whatsapp_last_error text NULL,
                auth_user_id uuid NULL,
                portal_habilitado boolean NOT NULL DEFAULT false,
                requiere_reset_portal boolean NOT NULL DEFAULT false,
                portal_login_email text NULL,
                portal_habilitado_at timestamp with time zone NULL,
                portal_ultima_activacion_at timestamp with time zone NULL,
                documento_identidad_normalizado text NULL,
                primera_sesion_efectivizada boolean NOT NULL DEFAULT false,
                updated_at timestamp with time zone DEFAULT now(),
                numero_credencial_obra_social text NULL,
                portal_access_token_issued_at timestamp with time zone NULL,
                portal_access_token_expires_at timestamp with time zone NULL,
                portal_access_token_status text NULL,
                portal_access_token_channel text NULL,
                portal_access_token_purpose text NULL,
                CONSTRAINT perfiles_pkey PRIMARY KEY (id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS perfiles_auth_user_id_uidx
                ON public.perfiles USING btree (auth_user_id)
                WHERE (auth_user_id IS NOT NULL);

            CREATE UNIQUE INDEX IF NOT EXISTS perfiles_portal_login_email_uidx
                ON public.perfiles USING btree (portal_login_email)
                WHERE (portal_login_email IS NOT NULL);

            CREATE UNIQUE INDEX IF NOT EXISTS perfiles_documento_portal_uidx
                ON public.perfiles USING btree (documento_identidad_normalizado)
                WHERE ((rol = 'paciente'::public.rol_usuario) AND (portal_habilitado = true) AND (documento_identidad_normalizado IS NOT NULL));

            CREATE TABLE IF NOT EXISTS public.rbac_permissions (
                id bigint GENERATED BY DEFAULT AS IDENTITY,
                key text NOT NULL,
                nombre text NOT NULL,
                descripcion text NULL,
                modulo text NOT NULL DEFAULT 'general',
                is_system boolean NOT NULL DEFAULT false,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                updated_at timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT rbac_permissions_pkey PRIMARY KEY (id),
                CONSTRAINT rbac_permissions_key_key UNIQUE (key)
            );

            CREATE TABLE IF NOT EXISTS public.rbac_roles (
                id bigint GENERATED BY DEFAULT AS IDENTITY,
                slug text NOT NULL,
                nombre text NOT NULL,
                descripcion text NULL,
                activo boolean NOT NULL DEFAULT true,
                is_system boolean NOT NULL DEFAULT false,
                is_staff boolean NOT NULL DEFAULT true,
                default_home text NOT NULL DEFAULT '/usuario',
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                updated_at timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT rbac_roles_pkey PRIMARY KEY (id),
                CONSTRAINT rbac_roles_slug_key UNIQUE (slug)
            );

            CREATE TABLE IF NOT EXISTS public.rbac_role_permissions (
                role_id bigint NOT NULL REFERENCES public.rbac_roles(id) ON DELETE CASCADE,
                permission_id bigint NOT NULL REFERENCES public.rbac_permissions(id) ON DELETE CASCADE,
                granted boolean NOT NULL DEFAULT true,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT rbac_role_permissions_pkey PRIMARY KEY (role_id, permission_id)
            );

            CREATE TABLE IF NOT EXISTS public.rbac_user_roles (
                user_id uuid NOT NULL,
                role_id bigint NOT NULL REFERENCES public.rbac_roles(id) ON DELETE CASCADE,
                is_primary boolean NOT NULL DEFAULT false,
                assigned_by uuid NULL,
                assigned_at timestamp with time zone NOT NULL DEFAULT now(),
                expires_at timestamp with time zone NULL,
                CONSTRAINT rbac_user_roles_pkey PRIMARY KEY (user_id, role_id)
            );

            CREATE TABLE IF NOT EXISTS public.rbac_effective_permissions (
                user_id uuid NOT NULL,
                permission_key text NOT NULL,
                source_role_id bigint NULL,
                created_at timestamp with time zone NOT NULL DEFAULT now(),
                updated_at timestamp with time zone NOT NULL DEFAULT now(),
                CONSTRAINT rbac_effective_permissions_pkey PRIMARY KEY (user_id, permission_key)
            );
            """, cancellationToken);
    }

    private static async Task ResetAndSeedRbacAsync(MedicalCenterDbContext dbContext, CancellationToken cancellationToken)
    {
        await ResetAndSeedRbacAsync(dbContext, minimal: false, cancellationToken);
    }

    /// <summary>
    /// Seeds RBAC data. When minimal=true, seeds only essential data for auth tests:
    /// - 1 role: staff
    /// - 3 permissions: app.admin_panel.access, portal.access, dashboard.read
    /// - Assigns permissions to the staff role
    /// </summary>
    public static async Task ResetAndSeedRbacAsync(MedicalCenterDbContext dbContext, bool minimal, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                delete from public.rbac_effective_permissions;
                delete from public.rbac_user_roles;
                delete from public.rbac_role_permissions;
                delete from public.rbac_roles;
                delete from public.rbac_permissions;
                """, cancellationToken);

            if (minimal)
            {
                // Minimal seed for tests: 1 role + essential permissions
                await SeedMinimalRbacAsync(dbContext, cancellationToken);
            }
            else
            {
                // Full seed with all roles and permissions (dev data)
                await SeedFullRbacAsync(dbContext, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task SeedMinimalRbacAsync(MedicalCenterDbContext dbContext, CancellationToken cancellationToken)
    {
        // Minimal permissions needed for auth tests
        var minimalPermissions = new (long Id, string Key, string Nombre, string? Descripcion, string Modulo)[]
        {
            (1, PermissionConstants.AppAdminPanelAccess, "Acceso panel interno", "Permite ingresar al panel de administración", PermissionConstants.ModuleApp),
            (2, PermissionConstants.PortalAccess, "Acceso portal paciente", "Permite ingresar al portal paciente", PermissionConstants.ModulePortal),
            (3, PermissionConstants.DashboardRead, "Ver dashboard", "Permite leer métricas y agenda del dashboard", PermissionConstants.ModuleDashboard)
        };

        foreach (var permission in minimalPermissions)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                insert into public.rbac_permissions (id, key, nombre, descripcion, modulo, is_system, created_at, updated_at)
                values ({0}, {1}, {2}, {3}, {4}, true, now(), now());
                """,
                new object[] { permission.Id, permission.Key, permission.Nombre, permission.Descripcion!, permission.Modulo },
                cancellationToken);
        }

        // Single staff role for tests
        await dbContext.Database.ExecuteSqlRawAsync("""
            insert into public.rbac_roles (id, slug, nombre, descripcion, activo, is_system, is_staff, default_home, created_at, updated_at)
            values (1, 'staff', 'Staff', 'Rol de prueba para tests de autenticación', true, true, true, '/usuario', now(), now());
            """, cancellationToken);

        // Assign all minimal permissions to staff role
        foreach (var permission in minimalPermissions)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                insert into public.rbac_role_permissions (role_id, permission_id, granted, created_at)
                select r.id, p.id, true, now()
                from public.rbac_roles r
                join public.rbac_permissions p on p.key = {1}
                where r.slug = {0};
                """,
                ["staff", permission.Key],
                cancellationToken);
        }

        // Reset sequences
        await dbContext.Database.ExecuteSqlRawAsync("""
            select setval('public.rbac_permissions_id_seq', (select coalesce(max(id), 1) from public.rbac_permissions), true);
            select setval('public.rbac_roles_id_seq', (select coalesce(max(id), 1) from public.rbac_roles), true);
            """, cancellationToken);
    }

    private static async Task SeedFullRbacAsync(MedicalCenterDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (var permission in Permissions)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                insert into public.rbac_permissions (id, key, nombre, descripcion, modulo, is_system, created_at, updated_at)
                values ({0}, {1}, {2}, {3}, {4}, true, now(), now());
                """,
                new object[] { permission.Id, permission.Key, permission.Nombre, permission.Descripcion!, permission.Modulo },
                cancellationToken);
        }

        foreach (var role in Roles)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                insert into public.rbac_roles (id, slug, nombre, descripcion, activo, is_system, is_staff, default_home, created_at, updated_at)
                values ({0}, {1}, {2}, {3}, true, true, {4}, {5}, now(), now());
                """,
                new object[] { role.Id, role.Slug, role.Nombre, role.Descripcion!, role.IsStaff, role.DefaultHome },
                cancellationToken);
        }

        foreach (var (roleSlug, permissionKeys) in RolePermissions)
        {
            var normalizedKeys = permissionKeys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var permissionKey in normalizedKeys)
            {
                await dbContext.Database.ExecuteSqlRawAsync("""
                    insert into public.rbac_role_permissions (role_id, permission_id, granted, created_at)
                    select r.id, p.id, true, now()
                    from public.rbac_roles r
                    join public.rbac_permissions p on p.key = {1}
                    where r.slug = {0};
                    """,
                    [roleSlug, permissionKey],
                    cancellationToken);
            }
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
            select setval('public.rbac_permissions_id_seq', (select coalesce(max(id), 1) from public.rbac_permissions), true);
            select setval('public.rbac_roles_id_seq', (select coalesce(max(id), 1) from public.rbac_roles), true);
            """, cancellationToken);
    }

    private static async Task EnsureCatalogDataAsync(MedicalCenterDbContext dbContext, CancellationToken cancellationToken)
    {
        var condicionesIva = new (int Id, string Nombre)[]
        {
            (1, "Consumidor final"),
            (2, "Responsable inscripto"),
            (3, "Monotributista"),
            (4, "Exento"),
            (5, "No responsable"),
            (6, "Sujeto no categorizado"),
            (7, "Consumidor final extranjero")
        };

        foreach (var (id, nombre) in condicionesIva)
        {
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("""
                    insert into public.condiciones_iva (id, nombre, activo, orden, created_at)
                    values ({0}, {1}, true, {0}, now())
                    on conflict (id) do update set
                        nombre = excluded.nombre,
                        activo = excluded.activo,
                        orden = excluded.orden;
                    """,
                    new object[] { id, nombre },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Failed to seed condicion_iva {id}: {ex.Message}");
            }
        }

        // Reset sequence if table exists and has a serial/auto-increment column
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                select setval('public.condiciones_iva_id_seq', (select coalesce(max(id), 1) from public.condiciones_iva), true);
                """, cancellationToken);
        }
        catch
        {
            // Sequence may not exist — ignore
        }
    }

    private static async Task SeedAdminUserAsync(
        MedicalCenterDbContext dbContext,
        SeedOptions seedOptions,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        var adminIdentifier = seedOptions.AdminIdentifier.Trim();
        var adminEmail = seedOptions.AdminEmail.Trim().ToLowerInvariant();
        var adminPasswordHash = passwordHasher.Hash(seedOptions.AdminPassword);

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Identifier == adminIdentifier || x.Email == adminEmail,
            cancellationToken);

        Guid authUserId;
        if (existingUser is null)
        {
            authUserId = Guid.NewGuid();
            existingUser = new User(new UserCreateParams(authUserId, adminIdentifier, adminEmail, adminPasswordHash, true, true, null, PermissionConstants.AdminProfileName));
            await dbContext.Users.AddAsync(existingUser, cancellationToken);
        }
        else
        {
            authUserId = existingUser.Id;
            existingUser.ActivatePortalUser(adminIdentifier, adminPasswordHash, adminEmail);
            existingUser.UpdateProfileName(PermissionConstants.AdminProfileName);
            existingUser.SetPasswordHash(adminPasswordHash);
            dbContext.Entry(existingUser).Property(x => x.IsStaff).CurrentValue = true;
            dbContext.Entry(existingUser).Property(x => x.PatientId).CurrentValue = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

            var existingProfileId = await dbContext.Database.SqlQueryRaw<Guid>(
            """
            select id as "Value"
            from public.perfiles
            where auth_user_id = {0}
            limit 1
            """,
            authUserId)
            .FirstOrDefaultAsync(cancellationToken);

        var profileId = existingProfileId == Guid.Empty ? Guid.NewGuid() : existingProfileId;
        if (existingProfileId == Guid.Empty)
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                insert into public.perfiles (
                    id,
                    nombre,
                    email,
                    telefono,
                    documento_identidad,
                    nacionalidad,
                    rol,
                    auth_user_id,
                    portal_habilitado,
                    requiere_reset_portal,
                    portal_login_email,
                    documento_identidad_normalizado
                )
                values (
                    {0},
                    {1},
                    {2},
                    null,
                    null,
                    null,
                    'admin',
                    {3},
                    false,
                    false,
                    {2},
                    null
                );
                """, [profileId, PermissionConstants.AdminProfileName, adminEmail, authUserId], cancellationToken);

            profileId = await dbContext.Database.SqlQueryRaw<Guid>(
                """
                select id as "Value"
                from public.perfiles
                where auth_user_id = {0}
                limit 1
                """,
                authUserId)
                .FirstAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.ExecuteSqlRawAsync("""
                update public.perfiles
                set nombre = {1},
                    email = {2},
                    rol = 'admin',
                    auth_user_id = {3},
                    portal_login_email = {2},
                    updated_at = now()
                where id = {0};
                """, [profileId, PermissionConstants.AdminProfileName, adminEmail, authUserId], cancellationToken);
        }

        var adminRoleId = await dbContext.Database.SqlQueryRaw<long>(
            """
            select id as "Value"
            from public.rbac_roles
            where slug = 'admin'
            limit 1
            """)
            .SingleAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            delete from public.rbac_user_roles where user_id = {0};
            """, [profileId], cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            insert into public.rbac_user_roles (user_id, role_id, is_primary, assigned_by, assigned_at)
            values ({0}, {1}, true, null, now());
            """, [profileId, adminRoleId], cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("""
            delete from public.rbac_effective_permissions where user_id = {0};

            insert into public.rbac_effective_permissions (user_id, permission_key, source_role_id, created_at, updated_at)
            select
                ur.user_id,
                perm.key,
                ur.role_id,
                now(),
                now()
            from public.rbac_user_roles ur
            join public.rbac_roles r on r.id = ur.role_id and r.activo = true
            join public.rbac_role_permissions rp on rp.role_id = ur.role_id and rp.granted = true
            join public.rbac_permissions perm on perm.id = rp.permission_id
            where ur.user_id = {0}
              and ur.expires_at is null
            group by ur.user_id, perm.key, ur.role_id;
            """, [profileId], cancellationToken);
    }
}
