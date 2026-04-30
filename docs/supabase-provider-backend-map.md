# Mapeo `supabaseApiProvider.js` -> backend ASP.NET
> Documento de transici�n. Sirve para ubicar equivalencias del provider legacy del frontend
> frente al backend can�nico actual. No es fuente de verdad de negocio.


Este documento cruza las funciones del provider actual del front con los endpoints ya implementados en el backend ASP.NET Core.

> Objetivo: migrar de forma gradual, sin romper el front de una sola vez.

---

## Convención de uso

- **Listo ahora**: se puede apuntar al backend ASP.NET sin esperar más trabajo.
- **Parcial**: existe endpoint backend, pero el payload o la semántica todavía no calzan 1:1 con lo que usa hoy el front.
- **Pendiente**: todavía no existe equivalente backend; debe seguir en Supabase/legacy por ahora.

---

## 1) Auth

### 1.1 Login / sesión

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiPortalSignIn(identifier, password)` | `POST /api/v1/auth/portal/sign-in` | Listo ahora | Reemplaza el function call `portal-sign-in`. |
| `apiPortalActivateAccess(token, password)` | `POST /api/v1/auth/portal/activate` | Listo ahora | Reemplaza `portal-activate-access`. |
| `apiRequestPortalAccessRecovery(documentoIdentidad)` | `POST /api/v1/auth/portal/recovery` | Listo ahora | Reemplaza `portal-request-access-token`. |
| `apiIssuePortalAccessToken(pacienteId, purpose, deliveryChannel)` | `POST /api/v1/auth/portal/access-tokens` | Listo ahora | Reemplaza `portal-issue-access-token`. |
| `apiGetEffectiveAccess()` | `GET /api/v1/auth/me/effective-access` | Listo ahora | Reemplaza `me_get_effective_access`. |

### 1.2 Auth staff

En el front, la sesión staff debería migrar a:

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`

**Acción en front**

- [ ] reemplazar `supabase.auth` o cualquier login custom por `/api/v1/auth/login`
- [ ] guardar `access_token` y `refresh_token`
- [ ] implementar refresh automático
- [ ] invalidar sesión con `/api/v1/auth/logout`

---

## 2) Pacientes

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiGetPacientes()` | `GET /api/v1/pacientes` | Listo ahora | Reemplaza lectura de `perfiles` con `rol = paciente`. |
| `apiCrearPaciente(...)` | `POST /api/v1/pacientes` | Listo ahora | Acepta `opt_in_source` y el resto del payload legacy relevante. |
| `apiActualizarPaciente(id, ...)` | `PATCH /api/v1/pacientes/{id}` | Listo ahora | Acepta `opt_in_source`, `actualizar_notas` y el payload legado relevante. |
| `apiEliminarPaciente(pacienteId)` | `DELETE /api/v1/pacientes/{id}` | Listo ahora | Hoy es baja lógica. |
| `apiConfigurarPortalPaciente(pacienteId, portalHabilitado)` | `PATCH /api/v1/pacientes/{id}/portal` | Listo ahora | Reemplaza `admin_configurar_portal_paciente`. |
| `apiPermitirResetPortalPaciente(pacienteId)` | `POST /api/v1/pacientes/{id}/portal/reset-enable` | Listo ahora | Reemplaza `admin_permitir_reset_portal_paciente`. |
| `apiActualizarMisDatosPaciente(...)` | `PATCH /api/v1/pacientes/me` | Listo ahora | Reemplaza `paciente_actualizar_mis_datos`. |
| `apiActualizarMisDatosStaff(...)` | `PATCH /api/v1/staff/me` | Listo ahora | Requiere `StaffManage`; actualiza el nombre visible del usuario staff autenticado. |
| `apiImportarPacientes(...)` | `POST /api/v1/importaciones/pacientes` | Parcial | Requiere definir el flujo final con Cloudflare Storage. |
| `apiGetNotasPaciente(pacienteId)` | `GET /api/v1/pacientes/{pacienteId}/notas` | Listo ahora | Requiere `ClinicalHistoryRead`. |
| `apiCrearNotaPaciente(...)` | `POST /api/v1/pacientes/{pacienteId}/notas` | Listo ahora | Requiere `ClinicalHistoryManage`. |
| `apiEliminarNotaPaciente(id)` | `DELETE /api/v1/pacientes/{pacienteId}/notas/{notaId}` | Listo ahora | Requiere `pacienteId` y `notaId`. |
| `apiGetSesionesCompletadas(pacienteId)` | `GET /api/v1/consultas/pacientes/{pacienteId}/sesiones-completadas` | Listo ahora | Devuelve sesiones ordenadas por fecha/hora de forma descendente. |
| `apiGetHistoriaClinica(...)` | `GET /api/v1/pacientes/{pacienteId}/historia-clinica` | Listo ahora |  |
| `apiGetHistoriasClinicasResumen()` | `GET /api/v1/historias-clinicas/resumen` | Listo ahora | Devuelve `{ paciente_id, numero }[]`. |
| `apiGetHistoriaClinicaEvoluciones(pacienteId)` | `GET /api/v1/pacientes/{pacienteId}/historia-clinica/evoluciones` | Listo ahora |  |
| `apiActualizarHistoriaClinica(...)` | `PATCH /api/v1/pacientes/{pacienteId}/historia-clinica` | Listo ahora |  |
| `apiActualizarHistoriaClinicaNumero(pacienteId, numero)` | `PATCH /api/v1/pacientes/{pacienteId}/historia-clinica/numero` | Listo ahora |  |
| `apiCrearHistoriaClinicaEvolucion(...)` | `POST /api/v1/pacientes/{pacienteId}/historia-clinica/evoluciones` | Listo ahora |  |

### Recomendación de migración

Primero migrar:

1. `apiGetPacientes`
2. `apiCrearPaciente`
3. `apiActualizarPaciente`
4. `apiEliminarPaciente`
5. `apiConfigurarPortalPaciente`
6. `apiPermitirResetPortalPaciente`
7. `apiActualizarMisDatosPaciente`

---

## 3) Profesionales

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiGetMedicos()` | `GET /api/v1/profesionales/medicos` | Listo ahora | Reemplaza `medicos`. |
| `apiCrearMedico({ nombre })` | `POST /api/v1/profesionales/medicos` | Listo ahora |  |
| `apiActualizarMedico(id, { nombre })` | `PATCH /api/v1/profesionales/medicos/{id}` | Listo ahora |  |
| `apiToggleMedicoActivo(id, activo)` | `PATCH /api/v1/profesionales/medicos/{id}/estado` | Listo ahora |  |
| `apiGetReferentes()` | `GET /api/v1/profesionales/referentes` | Listo ahora | Reemplaza `referentes`. |
| `apiCrearReferente({ nombre, tipo })` | `POST /api/v1/profesionales/referentes` | Listo ahora |  |
| `apiActualizarReferente(id, { nombre, tipo })` | `PATCH /api/v1/profesionales/referentes/{id}` | Listo ahora |  |
| `apiToggleReferenteActivo(id, activo)` | `PATCH /api/v1/profesionales/referentes/{id}/estado` | Listo ahora |  |

### Operadores de cámara

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiGetOperadoresCamara()` | `GET /api/v1/profesionales/operadores-camara` | Listo ahora |  |

---

## 4) Agendas / cámaras / horarios

### Cámaras

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiGetCamaras()` | `GET /api/v1/agendas/camaras` | Listo ahora | Reemplaza `camaras`. |
| `apiCrearCamara({ nombre, capacidad })` | `POST /api/v1/agendas/camaras` | Listo ahora |  |
| `apiActualizarCamara(id, ...)` | `PATCH /api/v1/agendas/camaras/{id}` | Listo ahora |  |
| `apiToggleActiva(id, activa)` | `PATCH /api/v1/agendas/camaras/{id}/estado` | Listo ahora |  |

### Horarios

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiGetHorariosConfig()` | `GET /api/v1/agendas/horarios` | Listo ahora | Reemplaza `horarios_config`. |
| `apiCrearHorarioConfig({ hora, orden })` | `POST /api/v1/agendas/horarios` | Listo ahora |  |
| `apiActualizarHorarioConfig({ id, hora, orden })` | `PATCH /api/v1/agendas/horarios/{id}` | Listo ahora |  |
| `apiToggleHorarioConfig(id, activo)` | `PATCH /api/v1/agendas/horarios/{id}/estado` | Listo ahora |  |
| `apiPreviewEliminarHorarioConfig(id)` | `GET /api/v1/agendas/horarios/{id}/eliminacion-preview` | Listo ahora |  |
| `apiEliminarHorarioConfig(id, resoluciones, motivo)` | `DELETE /api/v1/agendas/horarios/{id}` | Listo ahora | Hoy es baja lógica / desactivación. |

### Consultas y turnos: estado hist�rico

Este bloque se conserva como referencia de evoluci�n del provider legacy. La
superficie real migrada se documenta en las secciones 9 y 16.

| Función front | Estado | Nota |
|---|---:|---|
| `apiAperturarTurnosFecha(fecha)` | Listo ahora | `POST /api/v1/turnos/generar`. |
| `apiRepararSlotsRango(fechaInicio, fechaFin)` | Listo ahora | `POST /api/v1/turnos/reparar`. |
| `apiGetConsultasHorariosConfig()` | Listo ahora | `GET /api/v1/consultas/horarios`. |
| `apiToggleConsultaHorarioConfig(...)` | Listo ahora | `PATCH /api/v1/consultas/horarios/{id}/estado`. |
| `apiCrearConsultaHorarioConfig(...)` | Listo ahora | `POST /api/v1/consultas/horarios`. |
| `apiActualizarConsultaHorarioConfig(...)` | Listo ahora | `PATCH /api/v1/consultas/horarios/{id}`. |
| `apiPreviewEliminarConsultaHorarioConfig(id)` | Listo ahora | `GET /api/v1/consultas/horarios/{id}/eliminacion-preview`. |
| `apiEliminarConsultaHorarioConfig(...)` | Listo ahora | `DELETE /api/v1/consultas/horarios/{id}`. |

---

## 5) Turnos: lecturas

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiGetSlotsByFecha(fecha)` | `GET /api/v1/turnos?fecha=YYYY-MM-DD` | Listo ahora | Reemplaza lectura principal de slots. |
| `apiGetSlotsDisponiblesPacienteByFecha(fecha)` | `GET /api/v1/turnos/disponibles-portal?fecha=YYYY-MM-DD` | Listo ahora | Lectura portal paciente. |
| `apiGetSlotsByRango(fechaInicio, fechaFin)` | `GET /api/v1/turnos/rango?fecha_inicio=...&fecha_fin=...` | Listo ahora | Devuelve agrupado por fecha. |
| `apiGetSlotsByRangoLite(fechaInicio, fechaFin)` | `GET /api/v1/turnos/rango?...` | Listo ahora | En el front podés dejar un solo helper. |
| `apiGetTurnosPaciente(pacienteId)` | `GET /api/v1/turnos/pacientes/{pacienteId}/activos` | Listo ahora |  |
| `apiGetSlotsActivosTanda(tandaId)` | `GET /api/v1/turnos/tandas/{tandaId}/slots/activos` | Listo ahora |  |
| `apiGetSlotsTanda(tandaId)` | `GET /api/v1/turnos/tandas/{tandaId}/slots` | Listo ahora |  |
| `apiGetHistorialBloque(fecha, hora, camaraId)` | `GET /api/v1/turnos/bloques/historial` | Listo ahora |  |

### Recomendación de front

Si hoy tenés dos helpers (`byFecha` y `byRangoLite`), dejá uno solo y apuntalo a `/api/v1/turnos/rango`.

---

## 6) Turnos: mutaciones

### 6.1 Admin

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiAsignarSlot(slotId, pacienteId, esTanda, accion, operativo)` | `POST /api/v1/turnos/{slotId}/asignaciones` | Listo ahora | Soporta todo el payload legado operativo que usa el front. |
| `apiCancelarSlot(slotId, motivo)` | `POST /api/v1/turnos/{slotId}/cancelaciones` | Listo ahora | Mandar `Idempotency-Key`. |
| `apiReprogramarSlot(slotId, targetSlotId)` | `POST /api/v1/turnos/{slotId}/reprogramaciones` | Listo ahora | `scope="normal"` soportado. |
| `apiReprogramarSlotTanda(slotId, targetSlotId)` | `POST /api/v1/turnos/{slotId}/reprogramaciones` | Listo ahora | `scope="tanda"` soportado. |
| `apiReprogramarBloqueTanda(slotId, targetSlotId)` | `POST /api/v1/turnos/{slotId}/reprogramaciones` | Listo ahora | `scope="bloque_tanda"` soportado. |
| `apiApartarSlot(slotId, pacienteId, esMonoxido)` | `POST /api/v1/turnos/{slotId}/apartados` | Listo ahora | Mandar `Idempotency-Key`. |
| `apiConfirmarApartado(slotId, pacienteId, operativo)` | `POST /api/v1/turnos/{slotId}/apartados/confirmaciones` | Listo ahora | Consume el payload operativo legado. |
| `apiLiberarApartado(slotId)` | `POST /api/v1/turnos/{slotId}/apartados/liberaciones` | Listo ahora |  |
| `apiAsignarBloqueCompleto(fecha, hora, camaraId, pacienteId, operativo)` | `POST /api/v1/turnos/bloques/asignaciones` | Listo ahora | Acepta el payload operativo legado. |
| `apiCancelarBloqueCompleto(fecha, hora, camaraId, pacienteId, motivo)` | `POST /api/v1/turnos/bloques/cancelaciones` | Listo ahora |  |
| `apiCancelarTanda(tandaId, motivo)` | `POST /api/v1/turnos/tandas/{tandaId}/cancelaciones` | Listo ahora | Cancela todos los slots asociados a la tanda. |
| `apiActualizarDatosOperativosSlot(...)` | `PATCH /api/v1/turnos/{slotId}/datos-operativos` | Listo ahora |  |
| `apiActualizarDatosOperativosTanda(...)` | `PATCH /api/v1/turnos/tandas/{tandaId}/datos-operativos` | Listo ahora |  |
| `apiRegistrarHistorial(entradas)` | `POST /api/v1/turnos/bloques/historial` | Listo ahora | Registra entries de bit�cora de bloque. |
| `apiGetHistorialBloque(fecha, hora, camaraId)` | `GET /api/v1/turnos/bloques/historial` | Listo ahora | Consulta bit�cora del bloque exacto. |
| `apiGetHistorialBloquePorSlot(slotId)` | `GET /api/v1/turnos/bloques/historial/slot/{slotId}` | Listo ahora | Consulta bit�cora por slot. |
| `apiGetHistorialBloquePorRango(fechaInicio, fechaFin, camaraId)` | `GET /api/v1/turnos/bloques/historial/rango` | Listo ahora | Consulta bit�cora por rango de fechas. |

### 6.2 Portal paciente

| Función front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiPacienteReservarSlot(slotId)` | `POST /api/v1/portal/turnos/{slotId}/reservas` | Listo ahora | Puede mandar `Idempotency-Key`. |
| `apiPacienteCancelarSlot(slotId)` | `POST /api/v1/portal/turnos/{slotId}/cancelaciones` | Listo ahora | Puede mandar `Idempotency-Key`. |

### Recomendación de migración

Empezar por:

1. `apiCancelarSlot`
2. `apiPacienteReservarSlot`
3. `apiPacienteCancelarSlot`
4. `apiApartarSlot`
5. `apiLiberarApartado`
6. `apiConfirmarApartado`

Luego ir por:

- bloque completo,
- cancelación de tanda,
- reprogramaciones avanzadas.

---

## 7) Dashboard / cierre diario / eventos

La base de dashboards y cierres diarios ya est� en backend.
Queda pendiente �nicamente el feed de eventos de administraci�n.

| Función front | Estado | Nota |
|---|---:|---|
| `apiGetCierreDiarioEstado(fecha)` | Listo ahora |  |
| `apiPreviewCierreDiario(fecha)` | Listo ahora |  |
| `apiConfirmarCierreDiario(fecha, detalles)` | Listo ahora |  |
| `apiGetCierreDiarioDetalle(...)` | Listo ahora |  |
| `apiGetCierreDiarioExport(...)` | Listo ahora |  |
| `apiReabrirCierreDiario(...)` | Listo ahora |  |
| `apiGetCierreMensualExport(...)` | Listo ahora |  |
| `apiGetDashboardResumen(fecha)` | Listo ahora |  |
| `apiGetDashboardOcupacion(fecha)` | Listo ahora |  |
| `apiGetDashboardAgenda(fecha)` | Listo ahora |  |
| `apiGetDashboardAlertas(fecha)` | Listo ahora |  |
| `apiGetDashboardVolumenSemanal(fecha)` | Listo ahora |  |
| `apiListAdminEventFeed(...)` | Listo ahora |  |
| `apiGetAdminEventFeedFilterOptions()` | Listo ahora |  |

### Nota

El backend nuevo ya expone los endpoints de dashboards, cierres diarios y admin event feed. Si la base de datos todav�a no tiene la tabla `admin_event_feed`, los endpoints del feed devuelven colecciones vac�as en lugar de fallar.

---

## 8) Configuración auxiliar

| Función front | Estado | Nota |
|---|---:|---|
| `apiGetUserPreferences(userId)` | Listo ahora | El backend expone `/api/v1/users/me/preferences` para el usuario autenticado. |
| `apiUpsertUserPreferences(...)` | Listo ahora |  |
| `apiGetDiasLaborablesConfig()` | `GET /api/v1/configuracion/dias-laborables` | Listo ahora |  |
| `apiUpsertDiasLaborablesConfig(...)` | `PUT /api/v1/configuracion/dias-laborables` | Listo ahora |  |
| `apiGetCondicionesIva()` | `GET /api/v1/catalogos/condiciones-iva` | Listo ahora | Cat�logo read-only. |
| `apiGetObrasSociales()` | `GET /api/v1/catalogos/obras-sociales` | Listo ahora |  |
| `apiCrearObraSocial(...)` | `POST /api/v1/catalogos/obras-sociales` | Listo ahora |  |
| `apiActualizarObraSocial(...)` | `PATCH /api/v1/catalogos/obras-sociales/{id}` | Listo ahora |  |
| `apiToggleObraSocialActiva(...)` | `PATCH /api/v1/catalogos/obras-sociales/{id}/estado` | Listo ahora |  |
| `apiToggleObraSocialConvenio(...)` | `PATCH /api/v1/catalogos/obras-sociales/{id}/convenio` | Listo ahora |  |
| `apiGetWhatsappMessageSettings()` | `GET /api/v1/configuracion/whatsapp-message-settings` | Listo ahora |  |
| `apiActualizarWhatsappMessageSetting(...)` | `PUT /api/v1/configuracion/whatsapp-message-settings/{key}` | Listo ahora |  |
| `apiGetCamposConfig()` | `GET /api/v1/configuracion/campos-config` | Listo ahora |  |
| `apiCrearCampoConfig(...)` | `POST /api/v1/configuracion/campos-config` | Listo ahora |  |
| `apiActualizarCampoConfig(...)` | `PATCH /api/v1/configuracion/campos-config/{id}` | Listo ahora |  |
| `apiEliminarCampoConfig(...)` | `DELETE /api/v1/configuracion/campos-config/{id}` | Listo ahora |  |

---

## 9) Consultas médicas

| Función front | Estado | Nota |
|---|---:|---|
| `apiGetConsultasByFecha(fecha)` | Listo ahora | `GET /api/v1/consultas?fecha=YYYY-MM-DD`. |
| `apiGetConsultasByRango(fechaInicio, fechaFin)` | Listo ahora | `GET /api/v1/consultas/rango?fecha_inicio=...&fecha_fin=...`. |
| `apiAsignarConsulta(...)` | Listo ahora | `POST /api/v1/consultas/{slotId}/asignaciones` (con `Idempotency-Key`). |
| `apiCancelarConsulta(...)` | Listo ahora | `POST /api/v1/consultas/{slotId}/cancelaciones` (con `Idempotency-Key`). |
| `apiReprogramarConsulta(...)` | Listo ahora | `POST /api/v1/consultas/{slotId}/reprogramaciones` (con `Idempotency-Key`). |
| `apiCerrarConsulta(...)` | Listo ahora | `POST /api/v1/consultas/{slotId}/cierres` (con `Idempotency-Key`). |
| `apiGetConsultasHorariosConfig()` | Listo ahora | `GET /api/v1/consultas/horarios`. |
| `apiToggleConsultaHorarioConfig(...)` | Listo ahora | `PATCH /api/v1/consultas/horarios/{id}/estado`. |
| `apiCrearConsultaHorarioConfig(...)` | Listo ahora | `POST /api/v1/consultas/horarios`. |
| `apiActualizarConsultaHorarioConfig(...)` | Listo ahora | `PATCH /api/v1/consultas/horarios/{id}`. |
| `apiPreviewEliminarConsultaHorarioConfig(...)` | Listo ahora | `GET /api/v1/consultas/horarios/{id}/eliminacion-preview`. |
| `apiEliminarConsultaHorarioConfig(...)` | Listo ahora | `DELETE /api/v1/consultas/horarios/{id}`. |
| `apiGenerarConsultasFecha(...)` | Listo ahora | `POST /api/v1/consultas/generar`. |
| `apiRepararConsultasRango(...)` | Listo ahora | `POST /api/v1/consultas/reparar`. |

---

## 10) Historia clínica / notas / sesiones

| Función front | Estado | Nota |
|---|---:|---|
| `apiGetNotasPaciente(...)` | Listo ahora | `GET /api/v1/pacientes/{pacienteId}/notas`. |
| `apiCrearNotaPaciente(...)` | Listo ahora | `POST /api/v1/pacientes/{pacienteId}/notas`. |
| `apiEliminarNotaPaciente(...)` | Listo ahora | `DELETE /api/v1/pacientes/{pacienteId}/notas/{notaId}`. |
| `apiGetHistoriaClinica(...)` | Listo ahora | `GET /api/v1/pacientes/{pacienteId}/historia-clinica`. |
| `apiGetHistoriasClinicasResumen()` | Listo ahora | `GET /api/v1/historias-clinicas/resumen`. |
| `apiGetHistoriaClinicaEvoluciones(...)` | Listo ahora | `GET /api/v1/pacientes/{pacienteId}/historia-clinica/evoluciones`. |
| `apiActualizarHistoriaClinica(...)` | Listo ahora | `PATCH /api/v1/pacientes/{pacienteId}/historia-clinica`. |
| `apiActualizarHistoriaClinicaNumero(...)` | Listo ahora | `PATCH /api/v1/pacientes/{pacienteId}/historia-clinica/numero`. |
| `apiCrearHistoriaClinicaEvolucion(...)` | Listo ahora | `POST /api/v1/pacientes/{pacienteId}/historia-clinica/evoluciones`. |
| `apiGetSesionesCompletadas(...)` | Listo ahora | `GET /api/v1/consultas/pacientes/{pacienteId}/sesiones-completadas`. |

---

## 11) RBAC / staff

| Función front | Estado | Nota |
|---|---:|---|
| `apiListRbacRoles()` | `GET /api/v1/rbac/roles` | Listo ahora | Requiere `RbacRead`. |
| `apiListRbacPermissions()` | `GET /api/v1/rbac/permissions` | Listo ahora | Requiere `RbacRead`. |
| `apiUpsertRbacRole(...)` | `POST /api/v1/rbac/roles` | Listo ahora | Requiere `RbacManage`. |
| `apiSetRbacRolePermissions(...)` | `PUT /api/v1/rbac/roles/{roleSlug}/permissions` | Listo ahora | Requiere `RbacManage`. |
| `apiAssignRbacUserRoles(...)` | `PUT /api/v1/rbac/staff-users/{userId}/roles` | Listo ahora | Requiere `StaffManage`. |
| `apiListStaffUsers()` | `GET /api/v1/rbac/staff-users` | Listo ahora | Requiere `StaffRead`. |
| `apiCreateStaffUser(...)` | `POST /api/v1/rbac/staff-users` | Listo ahora | Requiere `StaffManage`. |
| `apiSetStaffUserActive(...)` | Listo ahora | `PATCH /api/v1/rbac/staff-users/{userId}/active` body: `{ active, role_slug }` |
| `apiActualizarMisDatosStaff(...)` | Listo ahora | `PATCH /api/v1/staff/me`. |

### Lo único equivalente hoy

- `GET /api/v1/auth/me/effective-access`

Ese sí lo podés usar para reemplazar la lectura del acceso efectivo del usuario logueado.

---

## 12) Adapter notes frontend (pendientes chicos)

Estos puntos no requieren cambios grandes de backend; son ajustes de adapter/helper para cerrar la migración sin romper la UI.

1. **`apiImportarPacientes(...)`**
   - Endpoint backend existe, pero la definici�n final de almacenamiento se deja pendiente.
   - Falta acordar el flujo definitivo con Cloudflare Storage antes de cerrar el adapter.
   - Mapear respuesta a UX actual (`total_rows`, `created_rows`, `updated_rows`, `skipped_rows`, `error_rows`, `errors[]`) cuando se habilite.

2. **`apiEliminarNotaPaciente(id)`**
   - Mantener la firma can�nica:
     - `DELETE /api/v1/pacientes/{pacienteId}/notas/{notaId}`
   - En el front conviene seguir resolviendo `pacienteId` cuando est� disponible.

3. **Mutaciones con idempotencia (`turnos` / `consultas`)**
   - Asegurar `Idempotency-Key` en:
     - asignar/cancelar/reprogramar/apartar/liberar/confirmar turnos,
     - asignar/cancelar/reprogramar/cerrar consultas.
   - Si no se manda, el backend puede devolver `400` en operaciones que lo requieren.

4. **`consultas` / historia cl�nica**
   - Ya est� alineado con el backend nuevo y puede migrarse sin m�s cambios de contrato.
   - Mantener el helper con estas rutas:
     - `GET /api/v1/consultas/horarios`
     - `POST /api/v1/consultas/horarios`
     - `PATCH /api/v1/consultas/horarios/{id}`
     - `PATCH /api/v1/consultas/horarios/{id}/estado`
     - `GET /api/v1/consultas/horarios/{id}/eliminacion-preview`
     - `DELETE /api/v1/consultas/horarios/{id}`
     - `POST /api/v1/consultas/generar`
     - `POST /api/v1/consultas/reparar`
     - `GET /api/v1/consultas`
     - `GET /api/v1/consultas/rango`
     - `POST /api/v1/consultas/{slotId}/asignaciones`
     - `POST /api/v1/consultas/{slotId}/cancelaciones`
     - `POST /api/v1/consultas/{slotId}/reprogramaciones`
     - `POST /api/v1/consultas/{slotId}/cierres`
     - `GET /api/v1/consultas/pacientes/{pacienteId}/sesiones-completadas`
     - `GET /api/v1/pacientes/{pacienteId}/historia-clinica`
     - `GET /api/v1/historias-clinicas/resumen`
     - `GET /api/v1/pacientes/{pacienteId}/historia-clinica/evoluciones`
     - `PATCH /api/v1/pacientes/{pacienteId}/historia-clinica`
     - `PATCH /api/v1/pacientes/{pacienteId}/historia-clinica/numero`
     - `POST /api/v1/pacientes/{pacienteId}/historia-clinica/evoluciones`
     - `GET /api/v1/pacientes/{pacienteId}/notas`
     - `POST /api/v1/pacientes/{pacienteId}/notas`
     - `DELETE /api/v1/pacientes/notas/{notaId}` o `DELETE /api/v1/pacientes/{pacienteId}/notas/{notaId}`
   - Las mutaciones sensibles deben conservar `Idempotency-Key`.

4. **Manejo de `204 No Content`**
   - `logout`, `set staff active`, y otros endpoints pueden devolver `204`.
   - El helper debe tolerar `body` vacío sin intentar parsear JSON obligatorio.

5. **Errores normalizados**
   - Unificar mapper de error del helper para priorizar:
     - `error`, `code`, `details`.
   - Mantener fallback para `ValidationProblemDetails` (`errors`) en `400`.

6. **Session handling (staff/portal)**
   - Consolidar uso de:
     - `/api/v1/auth/login`
     - `/api/v1/auth/refresh`
     - `/api/v1/auth/logout`
     - `/api/v1/auth/me/effective-access`
   - Mantener refresh automático ante `401` y retry de request original una sola vez.

---

## 12) Funciones utilitarias que no dependen del backend

Estas se pueden dejar tal cual en el front:

- `buildDashboardOcupacion(...)`
- `normalizeTextValue(...)`
- `normalizeDocumentoIdentidadValue(...)`
- `normalizeModalidadCobroValue(...)`
- `getWeekRangeFromIsoDate(...)`
- `toLocalIsoDate(...)`
- otros helpers de UI y normalización

---

## 13) Mapeo de `functions.invoke` de Supabase

| Function actual | Backend ASP.NET | Estado |
|---|---|---:|
| `portal-issue-access-token` | `POST /api/v1/auth/portal/access-tokens` | Listo ahora |
| `portal-request-access-token` | `POST /api/v1/auth/portal/recovery` | Listo ahora |
| `portal-sign-in` | `POST /api/v1/auth/portal/sign-in` | Listo ahora |
| `portal-activate-access` | `POST /api/v1/auth/portal/activate` | Listo ahora |
| `crear-paciente` | `POST /api/v1/pacientes` | Listo ahora |
| `eliminar-paciente` | `DELETE /api/v1/pacientes/{id}` | Listo ahora |
| `importar-pacientes` | — | Pendiente de definici�n externa | Requiere acordar Cloudflare Storage para el flujo de archivos. |
| `crear-staff` | — | Pendiente |
| `whatsapp-dispatch` | `POST /api/v1/whatsapp/dispatch` | Parcial | Falta integrar el env�o real contra Meta WhatsApp Cloud API. |
| `whatsapp-send-reminders-24h` | `POST /api/v1/whatsapp/send-reminders-24h` | Parcial | Falta integrar el env�o real contra Meta WhatsApp Cloud API. |
| Webhook WhatsApp | `GET /api/v1/whatsapp/webhook` / `POST /api/v1/whatsapp/webhook` | Listo ahora | Verificaci�n y registro de eventos entrantes. |

---

## 14) Checklist de migración recomendado

### Fase 1

- [ ] auth staff
- [ ] auth portal
- [ ] pacientes básicos
- [ ] cámaras / horarios
- [ ] lecturas de turnos

### Fase 2

- [ ] mutaciones simples de turnos
- [ ] apartados
- [ ] cancelaciones

### Fase 3

- [ ] portal reservas/cancelaciones
- [ ] reprogramación normal
- [ ] bloque completo

### Fase 4

- [ ] RBAC
- [ ] dashboards
- [ ] consultas
- [ ] historia clínica
- [ ] cierres

---

## 15) Qué te recomiendo hacer ahora en el front

1. Crear un cliente HTTP único.
2. Migrar auth.
3. Migrar pacientes.
4. Migrar cámaras/horarios.
5. Migrar lecturas de turnos.
6. Empezar a mover mutaciones de turnos usando `Idempotency-Key`.

Si querés, el siguiente paso puedo hacerlo todavía más práctico:

> te preparo un **adapter de transición** para el front con nombre de función vieja -> llamada nueva -> payload nuevo.

---

## Estado actualizado 2026-04-22

### Listo ahora

- `apiGetUserPreferences(userId)` -> `GET /api/v1/users/me/preferences`
- `apiUpsertUserPreferences(userId, ...)` -> `PUT /api/v1/users/me/preferences`
- `apiGetDashboardResumen(fecha)` -> `GET /api/v1/dashboards/resumen`
- `apiGetDashboardOcupacion(fecha)` -> `GET /api/v1/dashboards/ocupacion`
- `apiGetDashboardAgenda(fecha)` -> `GET /api/v1/dashboards/agenda`
- `apiGetDashboardAlertas(fecha)` -> `GET /api/v1/dashboards/alertas`
- `apiGetDashboardVolumenSemanal(fecha)` -> `GET /api/v1/dashboards/volumen-semanal`
- `apiGetCierreDiarioEstado(fecha)` -> `GET /api/v1/cierres-diarios/estado`
- `apiPreviewCierreDiario(fecha)` -> `POST /api/v1/cierres-diarios/preview`
- `apiConfirmarCierreDiario(fecha, detalles)` -> `POST /api/v1/cierres-diarios/confirmar`
- `apiGetCierreDiarioDetalle({ fecha, cierreId })` -> `GET /api/v1/cierres-diarios/detalle`
- `apiGetCierreDiarioExport({ fecha, cierreId })` -> `GET /api/v1/cierres-diarios/export`
- `apiGetCierreMensualExport({ anio, mes })` -> `GET /api/v1/cierres-diarios/export/mensual`
- `apiReabrirCierreDiario({ fecha, cierreId })` -> `POST /api/v1/cierres-diarios/reabrir`
- `apiListAdminEventFeed(...)` -> `GET /api/v1/admin/event-feed`
- `apiGetAdminEventFeedFilterOptions()` -> `GET /api/v1/admin/event-feed/filter-options`

### Nota

Estos endpoints ya est�n disponibles para migraci�n desde el provider legacy.

---

## 16) Consultas, turnos avanzados y fuera de horario

### Listo ahora

- `apiGetConsultasHorariosConfig()` -> `GET /api/v1/consultas/horarios`
- `apiCrearConsultaHorarioConfig({ hora, orden })` -> `POST /api/v1/consultas/horarios`
- `apiActualizarConsultaHorarioConfig({ id, hora, orden })` -> `PATCH /api/v1/consultas/horarios/{id}`
- `apiToggleConsultaHorarioConfig(id, activo)` -> `PATCH /api/v1/consultas/horarios/{id}/estado`
- `apiPreviewEliminarConsultaHorarioConfig(id)` -> `GET /api/v1/consultas/horarios/{id}/eliminacion-preview`
- `apiEliminarConsultaHorarioConfig(id, resoluciones, motivo)` -> `DELETE /api/v1/consultas/horarios/{id}`
- `apiGenerarConsultasFecha(fecha)` -> `POST /api/v1/consultas/generar`
- `apiRepararConsultasRango(fechaInicio, fechaFin)` -> `POST /api/v1/consultas/reparar`
- `apiGetConsultasByFecha(fecha)` -> `GET /api/v1/consultas?fecha=...`
- `apiGetConsultasByRango(fechaInicio, fechaFin)` -> `GET /api/v1/consultas/rango`
- `apiAsignarConsulta(slotId, pacienteId, medicoId, observaciones)` -> `POST /api/v1/consultas/{slotId}/asignaciones`
- `apiCancelarConsulta(slotId, motivo)` -> `POST /api/v1/consultas/{slotId}/cancelaciones`
- `apiReprogramarConsulta(slotId, targetSlotId, medicoId = null)` -> `POST /api/v1/consultas/{slotId}/reprogramaciones`
- `apiCerrarConsulta(slotId, { estado, titulo, nota, diagnostico_impresion, indicaciones })` -> `POST /api/v1/consultas/{slotId}/cierres`
- `apiGetSesionesCompletadas(pacienteId)` -> `GET /api/v1/consultas/pacientes/{pacienteId}/sesiones-completadas`

- `apiGetDisponibilidadTandaMes(fechaInicio, fechaFin, pacienteId = null)` -> `GET /api/v1/turnos/tandas/disponibilidad`
- `apiGetDisponibilidadTandaDetalleMes(fechaInicio, fechaFin, pacienteId = null)` -> `GET /api/v1/turnos/tandas/disponibilidad/detalle`
- `apiGetSlotsActivosTanda(tandaId)` -> `GET /api/v1/turnos/tandas/{tandaId}/slots/activos`
- `apiGetSlotsTanda(tandaId)` -> `GET /api/v1/turnos/tandas/{tandaId}/slots`
- `apiGetHistorialBloque(fecha, hora, camaraId)` -> `GET /api/v1/turnos/bloques/historial`
- `apiActualizarDatosOperativosSlot(slotId, operativo)` -> `PATCH /api/v1/turnos/{slotId}/datos-operativos`
- `apiActualizarDatosOperativosTanda(tandaId, operativo)` -> `PATCH /api/v1/turnos/tandas/{tandaId}/datos-operativos`
- `apiReprogramarSlotTanda(slotId, targetSlotId)` -> `POST /api/v1/turnos/{slotId}/reprogramaciones/tanda`
- `apiReprogramarBloqueTanda(slotId, targetSlotId)` -> `POST /api/v1/turnos/{slotId}/reprogramaciones/bloque`
- `apiGetTurnosFueraHorario(fecha)` -> `GET /api/v1/turnos/fuera-horario?fecha=...`
- `apiCrearTurnoFueraHorario({ ... })` -> `POST /api/v1/turnos/fuera-horario`
- `apiCancelarTurnoFueraHorario(id)` -> `DELETE /api/v1/turnos/fuera-horario/{turnoId}`

### Nota

Estos endpoints ya est�n disponibles para migraci�n desde el provider legacy.

---

## 17) Importaci�n de pacientes y utilidades operativas

| Funci�n front | Backend ASP.NET | Estado | Nota |
|---|---|---:|---|
| `apiImportarPacientes({ storage_path, file_name })` | `POST /api/v1/importaciones/pacientes` | Parcial | Requiere definir el flujo final con Cloudflare Storage. |
| `apiSetStaffUserActive(...)` | `PATCH /api/v1/rbac/staff-users/{userId}/active` | Listo ahora | Requiere `StaffManage`. |

### Nota

`apiCreateStaffUser(...)` ya est� cubierto por `POST /api/v1/rbac/staff-users`, as� que `crear-staff` no requiere un endpoint separado.
