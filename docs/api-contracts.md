# Contratos API (v1) orientados a contratos canónicos del backend
## Objetivo
Definir contratos REST canónicos para el backend ASP.NET Core, alineados al comportamiento actual del frontend y pensados como fuente de verdad para la migración.
## Principios de diseño
- Mantener nombres de campos `snake_case` en v1.
- Evitar envelope obligatorio en v1: listas y objetos se devuelven en formato directo.
- Mantener semántica de errores estable (`No autorizado`, `Prohibido`, conflictos funcionales).
- Soportar idempotencia en mutaciones sensibles con header `Idempotency-Key`.
- Permitir alias de entrada (`camelCase` opcional), pero responder en `snake_case`.
- Versionar por prefijo: `/api/v1/...`.

---

## Convenciones transversales

## AuthN/AuthZ
- `Authorization: Bearer <access_token>` para endpoints privados.
- Claims de permisos equivalentes a `effectivePermissions` actuales.

## Errores (formato estandar)
```json
{
  "error": "string",
  "code": "forbidden|unauthorized|validation_error|conflict|not_found|idempotency_mismatch|operation_pending",
  "details": {}
}
```

HTTP recomendados:
- `400` validacion
- `401` no autenticado
- `403` sin permiso
- `404` no encontrado
- `409` conflicto funcional/concurrencia
- `422` regla de negocio no cumplida

## Idempotencia
- Header recomendado: `Idempotency-Key: <uuid|string>`.
- Si misma key + payload distinto: `409` + `code=idempotency_mismatch`.
- Si misma key aun en proceso: `409` + `code=operation_pending`.

---

## 1) Auth

## 1.1 Portal sign-in
`POST /api/v1/auth/portal/sign-in`

Request:
```json
{
  "identifier": "DNI|login_identifier|email",
  "password": "string"
}
```

Response `200` (compatible con `apiPortalSignIn`):
```json
{
  "session": {
    "access_token": "jwt",
    "refresh_token": "jwt",
    "expires_in": 3600,
    "token_type": "bearer"
  },
  "user": {
    "id": "uuid",
    "email": "string"
  }
}
```

## 1.2 Activar acceso portal
`POST /api/v1/auth/portal/activate`

Request:
```json
{
  "token": "string",
  "login_identifier": "string",
  "password": "string"
}
```

Response `200`:
```json
{
  "ok": true,
  "login_identifier": "string"
}
```

## 1.3 Solicitar recuperacion portal
`POST /api/v1/auth/portal/recovery`

Request:
```json
{
  "documento_identidad": "string"
}
```

Response `200`:
```json
{
  "ok": true,
  "needs_manual_support": false
}
```

## 1.4 Access token manual admin (activation/reset)
`POST /api/v1/auth/portal/access-tokens`

Request:
```json
{
  "paciente_id": "uuid",
  "purpose": "activation|reset",
  "delivery_channel": "manual|whatsapp|email"
}
```

Response `200`:
```json
{
  "data": {
    "token_id": "uuid",
    "purpose": "activation",
    "delivery_channel": "manual",
    "expires_at": "2026-04-20T12:00:00Z",
    "token_plain": "123456"
  },
  "error": null
}
```

## 1.5 Access efectivo (RBAC)
`GET /api/v1/auth/me/effective-access`

Response `200` (compatible con `apiGetEffectiveAccess`):
```json
{
  "profile_id": "uuid",
  "roles": ["admin"],
  "effective_permissions": ["turnos.read", "turnos.asignar"],
  "primary_role": "admin",
  "default_home": "/usuario",
  "is_staff": true
}
```

## 1.6 Alta de staff
`POST /api/v1/rbac/staff-users`

`login_identifier` acepta alias, documento o correo, y se normaliza al guardar. `email` es opcional; si no se envía, el backend genera un correo interno sintético.

Request:
```json
{
  "nombre": "string",
  "login_identifier": "string",
  "email": "string|null",
  "password": "string",
  "role_slug": "string",
  "primary": true
}
```

Response `201`:
```json
{
  "data": {
    "id": "uuid",
    "nombre": "string",
    "email": "string|null"
  },
  "error": null
}
```

---

## 2) Pacientes

## 2.1 Listado
`GET /api/v1/pacientes`

Query opcional:
- `search`
- `include_inactive` (default `false`)

Response `200`: `Paciente[]` (array directo)

Modelo `Paciente` v1 (campos usados hoy):
```json
{
  "id": "uuid",
  "nombre": "string",
  "email": "string|null",
  "telefono": "string",
  "documento_identidad": "string",
  "documento_identidad_normalizado": "string|null",
  "nacionalidad": "string|null",
  "condicion_iva_id": 1,
  "obra_social_id": 10,
  "numero_credencial_obra_social": "string|null",
  "portal_habilitado": true,
  "requiere_reset_portal": false,
  "login_identifier": "string|null",
  "claustrofobico": false,
  "notas": "string|null",
  "datos_extra": {},
  "opt_in_whatsapp": false
}
```

## 2.2 Crear
`POST /api/v1/pacientes`

Request (alineado a `apiCrearPaciente`):
`login_identifier` es opcional cuando el alta solo prepara la invitación al portal; la activación final se completa con el código de 6 dígitos.
```json
{
  "nombre": "string",
  "email": "string|null",
  "telefono": "string",
  "documento_identidad": "string",
  "login_identifier": "string|null",
  "nacionalidad": "string|null",
  "condicion_iva_id": 1,
  "obra_social_id": 10,
  "numero_credencial_obra_social": "string|null",
  "portal_habilitado": true,
  "opt_in_whatsapp": false,
  "opt_in_source": "admin_alta",
  "claustrofobico": false,
  "notas": "string|null",
  "datos_extra": {}
}
```

Response `201`:
```json
{
  "data": {
    "id": "uuid",
    "nombre": "string"
  },
  "error": null
}
```

## 2.3 Actualizar paciente
`PATCH /api/v1/pacientes/{pacienteId}`

Request (alineado a `apiActualizarPaciente`):
```json
{
  "email": "string|null",
  "telefono": "string",
  "documento_identidad": "string",
  "nacionalidad": "string|null",
  "condicion_iva_id": 1,
  "obra_social_id": 10,
  "numero_credencial_obra_social": "string|null",
  "claustrofobico": false,
  "notas": "string|null",
  "datos_extra": {},
  "actualizar_notas": true,
  "opt_in_whatsapp": false,
  "opt_in_source": "admin_edicion"
}
```

Response `200`: `Paciente`

## 2.4 Eliminar paciente
`DELETE /api/v1/pacientes/{pacienteId}`

Response `200`:
```json
{
  "ok": true
}
```

## 2.5 Configuracion portal paciente
`PATCH /api/v1/pacientes/{pacienteId}/portal`

Request:
```json
{
  "portal_habilitado": true
}
```

Response `200`: `Paciente`

## 2.6 Permitir reset portal
`POST /api/v1/pacientes/{pacienteId}/portal/reset-enable`

Response `200`: `Paciente`

## 2.7 Datos propios paciente
`PATCH /api/v1/pacientes/me`

Request:
```json
{
  "nombre": "string",
  "email": "string|null",
  "telefono": "string"
}
```

Response `200`: `Paciente`

---

## 3) Profesionales

En el frontend actual se usan al menos `medicos`, `referentes` y `operadores` (staff para turno fuera de horario).

## 3.1 Medicos
- `GET /api/v1/profesionales/medicos`
- `POST /api/v1/profesionales/medicos`
- `PATCH /api/v1/profesionales/medicos/{id}`
- `PATCH /api/v1/profesionales/medicos/{id}/estado`

Modelo:
```json
{
  "id": 1,
  "nombre": "string",
  "activo": true,
  "orden": 10
}
```

`POST .../medicos` request:
```json
{
  "nombre": "string"
}
```

`PATCH .../medicos/{id}` request:
```json
{
  "nombre": "string"
}
```

`PATCH .../estado` request:
```json
{
  "activo": false
}
```

## 3.2 Referentes
- `GET /api/v1/profesionales/referentes`
- `POST /api/v1/profesionales/referentes`
- `PATCH /api/v1/profesionales/referentes/{id}`
- `PATCH /api/v1/profesionales/referentes/{id}/estado`

Modelo:
```json
{
  "id": 1,
  "nombre": "string",
  "tipo": "doctor|institucion|otro",
  "activo": true,
  "orden": 10
}
```

`POST .../referentes` request:
```json
{
  "nombre": "string",
  "tipo": "doctor|institucion|otro"
}
```

`PATCH .../referentes/{id}` request:
```json
{
  "nombre": "string",
  "tipo": "doctor|institucion|otro"
}
```

## 3.3 Operadores de camara (staff activos)
`GET /api/v1/profesionales/operadores-camara`

Response `200`:
```json
[
  {
    "id": "uuid",
    "nombre": "string",
    "is_active": true
  }
]
```

---

## 3.4 Campos config

- `GET /api/v1/configuracion/campos-config`
- `POST /api/v1/configuracion/campos-config`
- `PATCH /api/v1/configuracion/campos-config/{id}`
- `DELETE /api/v1/configuracion/campos-config/{id}`

Modelo:
```json
{
  "id": "uuid",
  "nombre": "string",
  "tipo": "texto|checkbox|numero",
  "orden": 10
}
```

`POST .../campos-config` request:
```json
{
  "nombre": "string",
  "tipo": "texto|checkbox|numero"
}
```

`PATCH .../campos-config/{id}` request:
```json
{
  "nombre": "string",
  "tipo": "texto|checkbox|numero"
}
```

## 4) Agendas

## 4.1 Camaras
- `GET /api/v1/agendas/camaras`
- `POST /api/v1/agendas/camaras`
- `PATCH /api/v1/agendas/camaras/{id}`
- `PATCH /api/v1/agendas/camaras/{id}/estado`

Modelo camara:
```json
{
  "id": 1,
  "nombre": "Camara 1",
  "capacidad": 3,
  "activa": true
}
```

`PATCH /agendas/camaras/{id}` puede devolver resumen de reconciliacion (compat con `apiActualizarCamara`):
```json
{
  "camara": { "id": 1, "nombre": "Camara 1", "capacidad": 2, "activa": true },
  "movidos": 1,
  "cancelados": 0,
  "apartados_liberados": 0,
  "eliminados": 1
}
```

## 4.2 Horarios hiperbaricos
- `GET /api/v1/agendas/horarios`
- `POST /api/v1/agendas/horarios`
- `PATCH /api/v1/agendas/horarios/{id}`
- `PATCH /api/v1/agendas/horarios/{id}/estado`
- `GET /api/v1/agendas/horarios/{id}/eliminacion-preview`
- `DELETE /api/v1/agendas/horarios/{id}`

Modelo horario:
```json
{
  "id": 1,
  "hora": "09:00",
  "orden": 1,
  "activo": true
}
```

`DELETE` request:
```json
{
  "resoluciones": [],
  "motivo": "string|null"
}
```

## 4.3 Operaciones de apertura/reparacion
- `POST /api/v1/agendas/slots/generar-dia`
- `POST /api/v1/agendas/slots/reparar-rango`

Request generar dia:
```json
{
  "fecha": "2026-04-20"
}
```

Request reparar rango:
```json
{
  "fecha_inicio": "2026-04-20",
  "fecha_fin": "2026-04-27"
}
```

Response `200` en ambos: `number` (cantidad de slots generados/reparados), para mantener compatibilidad.

---

## 5) Turnos

## 5.1 Lecturas

- `GET /api/v1/turnos?fecha=YYYY-MM-DD`
  - equivalente a `apiGetSlotsByFecha`

- `GET /api/v1/turnos/disponibles-portal?fecha=YYYY-MM-DD`
  - equivalente a `apiGetSlotsDisponiblesPacienteByFecha`
  - incluye `camara` anidada compatible con UI:
    ```json
    {
      "camara_id": 1,
      "camara": {
        "id": 1,
        "nombre": "Camara 1",
        "capacidad": 3
      }
    }
    ```

- `GET /api/v1/turnos/rango?fecha_inicio=...&fecha_fin=...`
  - equivalente a `apiGetSlotsByRango` (respuesta agrupada por fecha para compatibilidad)

- `GET /api/v1/turnos/pacientes/{pacienteId}/activos`
  - equivalente a `apiGetTurnosPaciente`

## 5.2 Mutaciones admin (con idempotencia)

Todos aceptan `Idempotency-Key`.

### Asignar slot
`POST /api/v1/turnos/{slotId}/asignaciones`

Request:
```json
{
  "paciente_id": "uuid",
  "es_tanda": false,
  "tanda_id": null,
  "accion": "asignado",
  "referido_tercero": false,
  "referente_id": null,
  "modalidad_cobro": "particular|obra_social",
  "obra_social_id": null,
  "numero_autorizacion": null,
  "sesiones_autorizadas": null,
  "ciclo_obra_social_id": null,
  "iniciar_nuevo_ciclo_obra_social": false,
  "convenio_corroborado": false,
  "medico_id": null,
  "es_nuevo_ingreso": false,
  "es_monoxido": false,
  "monoxido_orden_medica": false,
  "monoxido_resumen_clinico": false
}
```

Response `200`: `Slot`

### Cancelar slot
`POST /api/v1/turnos/{slotId}/cancelaciones`

Request:
```json
{ "motivo": "string|null" }
```

Response `200`: `Slot`

### Reprogramar slot
`POST /api/v1/turnos/{slotId}/reprogramaciones`

Request:
```json
{
  "target_slot_id": "uuid",
  "scope": "normal|tanda|bloque_tanda"
}
```

Response `200`: `Slot`

### Bloque completo
- `POST /api/v1/turnos/bloques/asignaciones`
- `POST /api/v1/turnos/bloques/cancelaciones`

Request asignar:
```json
{
  "fecha": "2026-04-20",
  "hora": "10:00",
  "camara_id": 1,
  "paciente_id": "uuid",
  "es_tanda": false,
  "tanda_id": null,
  "referido_tercero": false,
  "referente_id": null,
  "modalidad_cobro": "particular|obra_social",
  "obra_social_id": null,
  "numero_autorizacion": null,
  "sesiones_autorizadas": null,
  "ciclo_obra_social_id": null,
  "iniciar_nuevo_ciclo_obra_social": false,
  "convenio_corroborado": false,
  "medico_id": null,
  "es_nuevo_ingreso": false,
  "es_monoxido": false,
  "monoxido_orden_medica": false,
  "monoxido_resumen_clinico": false
}
```

Request cancelar:
```json
{
  "fecha": "2026-04-20",
  "hora": "10:00",
  "camara_id": 1,
  "paciente_id": "uuid",
  "motivo": "string|null"
}
```

Response `200`: `Slot[]`

### Tanda
`POST /api/v1/turnos/tandas/{tandaId}/cancelaciones`

Request:
```json
{ "motivo": "string|null" }
```

Response `200`: `Slot[]`

### Apartar/confirmar/liberar
- `POST /api/v1/turnos/{slotId}/apartados`
- `POST /api/v1/turnos/{slotId}/apartados/confirmaciones`
- `POST /api/v1/turnos/{slotId}/apartados/liberaciones`

Requests:
```json
{ "paciente_id": "uuid|null", "es_monoxido": false }
```
```json
{
  "paciente_id": "uuid|null",
  "referido_tercero": false,
  "referente_id": null,
  "modalidad_cobro": "particular|obra_social",
  "obra_social_id": null,
  "numero_autorizacion": null,
  "sesiones_autorizadas": null,
  "ciclo_obra_social_id": null,
  "iniciar_nuevo_ciclo_obra_social": false,
  "convenio_corroborado": false,
  "medico_id": null,
  "es_nuevo_ingreso": false,
  "es_monoxido": false,
  "monoxido_orden_medica": false,
  "monoxido_resumen_clinico": false
}
```

Response `200`: `Slot`

### Actualizar datos operativos
- `PATCH /api/v1/turnos/{slotId}/datos-operativos`
- `PATCH /api/v1/turnos/tandas/{tandaId}/datos-operativos`

Response `200`: `Slot` o `Slot[]` (igual que hoy).

## 5.3 Mutaciones portal paciente

- `POST /api/v1/portal/turnos/{slotId}/reservas`
- `POST /api/v1/portal/turnos/{slotId}/cancelaciones`

Ambos con `Idempotency-Key` opcional/recomendado.
Response `200`: `Slot`

## 5.4 Turnos fuera de horario

- `GET /api/v1/turnos/fuera-horario?fecha=YYYY-MM-DD`
- `POST /api/v1/turnos/fuera-horario`
- `DELETE /api/v1/turnos/fuera-horario/{id}`

Request crear:
```json
{
  "fecha": "2026-04-20",
  "hora": "18:30",
  "paciente_id": "uuid",
  "operador_camara_id": "uuid",
  "notas": "string|null",
  "es_monoxido": false,
  "monoxido_orden_medica": false,
  "monoxido_resumen_clinico": false,
  "monoxido_medico_id": null
}
```

Response `200` crear:
```json
{
  "data": { "id": "uuid", "fecha": "2026-04-20", "hora": "18:30" },
  "error": null
}
```

Response cancelar:
```json
{
  "error": null
}
```

## 5.5 Mantenimiento de slots e historial operativo

### Generar slots de un día
`POST /api/v1/turnos/generar`

Request:
```json
{
  "fecha": "2026-04-20"
}
```

Response `200`:
```json
{
  "total": 12
}
```

### Reparar slots de un rango
`POST /api/v1/turnos/reparar`

Request:
```json
{
  "fecha_inicio": "2026-04-20",
  "fecha_fin": "2026-04-27"
}
```

Response `200`:
```json
{
  "total": 42
}
```

### Registrar historial de bloque
`POST /api/v1/turnos/bloques/historial`

Request:
```json
[
  {
    "fecha": "2026-04-20",
    "hora": "10:00",
    "camara_id": 1,
    "slot_id": "uuid",
    "lugar": 1,
    "accion": "asignado",
    "paciente_id": "uuid",
    "motivo": "string|null"
  }
]
```

Response `200`:
```json
{
  "total": 1
}
```

### Consultar bit�cora operativa por slot
`GET /api/v1/turnos/bloques/historial/slot/{slotId}`

Response `200`: `BlockHistoryResponse[]`

Shape de referencia can�nico:
```json
{
  "id": "uuid",
  "fecha": "2026-04-20",
  "hora": "10:00:00",
  "camara_id": 1,
  "slot_id": "uuid|null",
  "lugar": 1,
  "accion": "asignado",
  "paciente_id": "uuid|null",
  "realizado_por": "uuid|null",
  "motivo": "string|null",
  "referido_tercero": false,
  "modalidad_cobro": "particular",
  "obra_social_id": 10,
  "numero_autorizacion": "string|null",
  "obra_social_validada_por": "uuid|null",
  "obra_social_validada_at": "2026-04-20T10:00:00Z|null",
  "medico_id": 3,
  "es_nuevo_ingreso": false,
  "referente_id": 2,
  "tanda_id": "uuid|null",
  "sesiones_autorizadas": 1,
  "ciclo_obra_social_id": "uuid|null",
  "created_at": "2026-04-20T10:00:00Z",
  "paciente": { "nombre": "Juan Perez" },
  "medico": { "nombre": "Dra. Gomez" },
  "referente": { "nombre": "Dr. Lopez", "tipo": "interno" },
  "obra_social": { "nombre": "OSDE" },
  "realizado_por_perfil": { "nombre": "Admin" },
  "obra_social_validada_por_perfil": { "nombre": "Admin" }
}
```

### Consultar bit�cora operativa por rango
`GET /api/v1/turnos/bloques/historial/rango?fecha_inicio=YYYY-MM-DD&fecha_fin=YYYY-MM-DD&camara_id=1`

Response `200`: `BlockHistoryResponse[]`

## 5.6 WhatsApp / recordatorios

Estos endpoints reflejan el flujo migrado desde el backend anterior del front y ya
se ejecutan en ASP.NET Core. La l�gica de negocio vive en backend; la base de datos
queda para persistencia, integridad y trazabilidad.

- `POST /api/v1/whatsapp/dispatch`
- `POST /api/v1/whatsapp/send-reminders-24h`
- `GET /api/v1/whatsapp/webhook`
- `POST /api/v1/whatsapp/webhook`

### Dispatch manual
Request:
```json
{
  "slot_ids": ["uuid"],
  "limit": 10
}
```

Response `200`:
```json
{
  "requested": 1,
  "found": 1
}
```

### Recordatorios 24h
Request:
```json
{
  "fecha_objetivo": "2026-04-20"
}
```

Response `200`:
```json
{
  "fecha_objetivo": "2026-04-20",
  "total": 8
}
```

### Webhook de WhatsApp

El endpoint `GET /api/v1/whatsapp/webhook` responde al challenge de verificación de
Meta/Kapso. Requiere que `hub.verify_token` coincida con la configuración del backend.

El endpoint `POST /api/v1/whatsapp/webhook` recibe eventos crudos, los normaliza y
los registra en la bitácora operativa del backend.

Request `GET`:
```text
/api/v1/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=...&hub.challenge=12345
```

Response `200`:
```text
12345
```

Response `POST 200`:
```json
{
  "stored": true,
  "processed": true,
  "event_type": "messages",
  "entry_id": "entry-id",
  "message_id": "wamid.id"
}
```

---

## 6) Importaciones

### 6.1 Importar pacientes
- `POST /api/v1/importaciones/pacientes`

Request `multipart/form-data`:
- `file` opcional: CSV subido al backend
- `storage_path` opcional: carpeta local compartida
- `file_name` opcional: nombre del archivo dentro de `storage_path`

Response `200`:
```json
{
  "total_rows": 10,
  "created_rows": 8,
  "updated_rows": 1,
  "skipped_rows": 1,
  "error_rows": 1,
  "errors": [
    {
      "row_number": 7,
      "message": "condicion_iva_id o condicion_iva son obligatorios."
    }
  ]
}
```

---

## 7) Estrategia de migracion y compatibilidad (frontend actual)

## 7.1 Adapter compat en `src/lib/api.js`
Mantener firmas actuales (`apiAsignarSlot`, `apiPortalSignIn`, etc.) y cambiar solo implementación interna:
- primero `provider=supabase` (actual),
- luego `provider=aspnet` por feature flag.

## 7.2 Mapeo de compatibilidad recomendado
- Auth portal:
  - `portal-sign-in` -> `POST /api/v1/auth/portal/sign-in`
  - `portal-activate-access` -> `POST /api/v1/auth/portal/activate`
  - `portal-request-access-token` -> `POST /api/v1/auth/portal/recovery`
- Pacientes:
  - `crear-paciente`/`eliminar-paciente` function -> `POST/DELETE /api/v1/pacientes`
  - `admin_actualizar_paciente` RPC -> `PATCH /api/v1/pacientes/{id}`
- Turnos:
  - RPC idempotentes -> endpoints REST con `Idempotency-Key`
  - devolver mismo shape de `Slot` que hoy consume UI

## 7.3 Compatibilidad de errores
- Mantener mensajes de negocio clave para no romper mapeos UX actuales:
  - `No autorizado`
  - `Prohibido`
  - `El turno ya no esta disponible`
  - `No puedes reservar turnos consecutivos en el mismo dia`
  - `Monoxido requiere orden medica y resumen clinico`

## 7.4 Cambios diferidos a v2
- Envelope uniforme para todas las respuestas (`{data,error,meta}`).
- CamelCase en todo el contrato.
- Normalizacion completa de recursos (hipermedia/links/paginacion unificada).

---

## 8) Criterio de aceptacion de contratos v1
- El frontend puede alternar Supabase/ASP.NET por flag sin cambios de UI.
- Operaciones sensibles soportan idempotencia.
- Reglas criticas de turnos y portal quedan del lado backend.
- Los payloads de v1 cubren todos los campos usados hoy en `src/lib/api.js`.


