namespace MedicalCenter.Domain.Constants;

public static class PermissionConstants
{
    // ─── Modules ───
    public const string ModuleApp = "app";
    public const string ModulePortal = "portal";
    public const string ModuleDashboard = "dashboard";
    public const string ModuleTurnos = "turnos";
    public const string ModuleConsultas = "consultas";
    public const string ModuleHistoriaClinica = "historia_clinica";
    public const string ModulePacientes = "pacientes";
    public const string ModuleConfiguracion = "configuracion";
    public const string ModuleActividad = "actividad";
    public const string ModuleReportes = "reportes";
    public const string ModuleUsuarios = "usuarios";
    public const string ModuleRbac = "rbac";
    public const string ModuleWhatsapp = "whatsapp";

    // ─── App Permissions ───
    public const string AppAdminPanelAccess = "app.admin_panel.access";

    // ─── Portal Permissions ───
    public const string PortalAccess = "portal.access";
    public const string PortalTurnosReserve = "portal.turnos.reserve";
    public const string PortalTurnosCancel = "portal.turnos.cancel";
    public const string PortalSelfUpdate = "portal.self.update";

    // ─── Dashboard Permissions ───
    public const string DashboardRead = "dashboard.read";

    // ─── Turnos Permissions ───
    public const string TurnosRead = "turnos.read";
    public const string TurnosAsignar = "turnos.asignar";
    public const string TurnosCancelar = "turnos.cancelar";
    public const string TurnosReprogramar = "turnos.reprogramar";
    public const string TurnosApartar = "turnos.apartar";
    public const string TurnosConfirmarApartado = "turnos.confirmar_apartado";
    public const string TurnosLiberarApartado = "turnos.liberar_apartado";
    public const string TurnosBloqueCompleto = "turnos.bloque_completo";
    public const string TurnosTanda = "turnos.tanda";
    public const string TurnosCierreDiario = "turnos.cierre_diario";
    public const string TurnosFueraHorario = "turnos.fuera_horario";

    // ─── Consultas Permissions ───
    public const string ConsultasRead = "consultas.read";
    public const string ConsultasAsignar = "consultas.asignar";
    public const string ConsultasCancelar = "consultas.cancelar";
    public const string ConsultasReprogramar = "consultas.reprogramar";
    public const string ConsultasCerrar = "consultas.cerrar";

    // ─── Historia Clínica Permissions ───
    public const string HistoriaClinicaEditarFicha = "historia_clinica.editar_ficha";
    public const string HistoriaClinicaCrearEvolucion = "historia_clinica.crear_evolucion";
    public const string HistoriaClinicaEditarNumero = "historia_clinica.editar_numero";
    public const string HistoriaClinicaResumenRead = "historia_clinica.resumen.read";
    public const string HistoriaClinicaDetalleRead = "historia_clinica.detalle.read";

    // ─── Pacientes Permissions ───
    public const string PacientesRead = "pacientes.read";
    public const string PacientesCrear = "pacientes.crear";
    public const string PacientesEditar = "pacientes.editar";
    public const string PacientesPortalManage = "pacientes.portal.manage";

    // ─── Config Permissions ───
    public const string ConfigRead = "config.read";
    public const string ConfigHorariosManage = "config.horarios.manage";
    public const string ConfigCamarasManage = "config.camaras.manage";
    public const string ConfigCatalogosManage = "config.catalogos.manage";
    public const string ConfigWhatsappManage = "config.whatsapp.manage";

    // ─── Actividad Permissions ───
    public const string ActividadRead = "actividad.read";

    // ─── Reportes Permissions ───
    public const string ReportesRead = "reportes.read";
    public const string ReportesExport = "reportes.export";

    // ─── Staff Permissions ───
    public const string StaffRead = "staff.read";
    public const string StaffManage = "staff.manage";

    // ─── RBAC Permissions ───
    public const string RbacRead = "rbac.read";
    public const string RbacManage = "rbac.manage";

    // ─── WhatsApp Permissions ───
    public const string WhatsappDispatch = "whatsapp.dispatch";

    // ─── Role Slugs ───
    public const string RolePaciente = "paciente";
    public const string RoleStaffInactivo = "staff_inactivo";
    public const string RoleOperadorCamara = "operador_camara";
    public const string RoleSecretaria = "secretaria";
    public const string RoleMedico = "medico";
    public const string RoleAdmin = "admin";

    // ─── Default Home Routes ───
    public const string RoutePaciente = "/paciente";
    public const string RouteLogin = "/login";
    public const string RouteUsuarioTurnos = "/usuario/turnos";
    public const string RouteUsuarioPacientes = "/usuario/pacientes";
    public const string RouteUsuarioHistoriasClinicas = "/usuario/historias-clinicas";
    public const string RouteUsuario = "/usuario";

    // ─── Seed Constants ───
    public const string AdminProfileName = "Administrador";
    public const string SqlRoleAdmin = "admin";
}
