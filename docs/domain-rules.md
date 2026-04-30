# Reglas de negocio del dominio (estado real del codigo)

## Alcance y criterio
Este documento resume reglas funcionales detectadas en el repositorio completo, con foco en:
- componentes React
- hooks
- helpers
- validaciones y formularios
- queries
- funciones SQL/RPC en Supabase

Cada regla esta marcada como:
- `Confirmada por codigo`: existe evidencia directa en frontend o backend SQL.
- `Inferida`: surge de composicion de flujos/UI, naming o comportamiento esperado, pero no aparece como restriccion dura en un unico punto.

---

## 1) Turnos hiperbaricos (slots)

### Reglas de disponibilidad y reserva
- `Confirmada por codigo` Un paciente solo puede reservar un slot si el estado es `libre` o `cancelado`.
  Fuentes: `supabase_schema.sql` (`paciente_reservar_slot` update con `estado IN ('libre','cancelado')`), `src/pages/PacientePortal.jsx`, `src/hooks/useSlots.js` (`calcFechasDisponibles`).

- `Confirmada por codigo` Un paciente no puede reservar ni cancelar turnos pasados.
  Fuentes: `supabase_schema.sql` (`paciente_reservar_slot`, `paciente_cancelar_slot`).

- `Confirmada por codigo` Un paciente no puede tener turnos consecutivos en el mismo dia (diferencia de hora <= 1).
  Fuentes: `supabase_schema.sql` (`paciente_reservar_slot`), `src/components/admin/slotConflictUtils.js`, `src/components/admin/ModalTanda.jsx`, `src/components/admin/AdminTurnosDialogs.jsx`.

- `Confirmada por codigo` En portal paciente, las acciones se limitan a turnos propios ocupados para cancelar.
  Fuentes: `supabase_schema.sql` (`paciente_cancelar_slot`), `src/pages/PacientePortal.jsx`.

- `Confirmada por codigo` La consulta de turnos del paciente devuelve solo ocupados y futuros.
  Fuentes: `src/lib/api.js` (`apiGetTurnosPaciente`: `.eq('estado','ocupado')` y `.gte('fecha', hoy)`).

### Reglas de lectura por rol paciente
- `Confirmada por codigo` El paciente puede leer: sus slots, o slots sin paciente en estado `libre/cancelado`.
  Fuentes: `supabase_schema.sql` policy `slots_paciente_read_limited`.

- `Inferida` El frontend asume que cualquier slot visible en portal que no sea propio y no este libre/cancelado no es reservable.
  Fuentes: `src/pages/PacientePortal.jsx` y filtros de disponibilidad.

---

## 2) Apartados, confirmacion y liberacion

- `Confirmada por codigo` Solo se pueden confirmar slots en estado `apartado`.
  Fuentes: `supabase_schema.sql` (`admin_confirmar_apartado`).

- `Confirmada por codigo` Confirmar un apartado requiere paciente final (propio del apartado o seleccionado en el momento).
  Fuentes: `supabase_schema.sql` (`Paciente requerido para confirmar el apartado`), `src/hooks/useAdminTurnosActions.js`.

- `Confirmada por codigo` Confirmar/liberar apartado limpia `apartado_por/apartado_ts` y normaliza estado.
  Fuentes: `supabase_schema.sql` (`admin_confirmar_apartado`, `admin_liberar_apartado`), `src/lib/api.js`.

- `Inferida` Apartado funciona como estado transitorio para bloquear disponibilidad sin confirmar tratamiento final.
  Fuentes: flujos en `AdminTurnosDialogs`, `ModalTanda`, `useAdminTurnosActions`.

---

## 3) Bloque completo y tanda

- `Confirmada por codigo` Un bloque completo requiere operar todos los lugares del mismo `fecha + hora + camara`.
  Fuentes: `supabase_schema.sql` (`admin_asignar_bloque_completo`, `admin_cancelar_bloque_completo`), `src/hooks/useAdminTurnosActions.js` (`esBloqueCompleto`).

- `Confirmada por codigo` Si un slot pertenece a bloque completo, su cancelacion en UI deriva a cancelacion de bloque.
  Fuentes: `src/hooks/useAdminTurnosActions.js` (`handleCancelarSlot`).

- `Confirmada por codigo` Tanda se asigna de forma secuencial y guarda `tanda_id`; al fallar parcial intenta reconciliar contra lo persistido.
  Fuentes: `src/hooks/useSlots.js` (`useAsignarTanda`), `src/lib/api.js` (`apiGetSlotsTanda`).

- `Confirmada por codigo` En modalidad obra social, la seleccion de dias de tanda se limita por `sesiones_autorizadas`.
  Fuentes: `src/components/admin/ModalTanda.jsx`.

- `Confirmada por codigo` En tanda/bloque se bloquea asignacion si hay conflictos por consecutividad u otros pacientes en bloque.
  Fuentes: `src/components/admin/ModalTanda.jsx`, `src/components/admin/slotConflictUtils.js`, mensajes normalizados en `AdminTurnosDialogs`.

- `Inferida` La regla operativa de negocio es "una unica unidad terapeutica por bloque horario por paciente", incluso en reprogramaciones.
  Fuentes: validadores de `AdminTurnosDialogs` y `ModalDetallePaciente`.

---

## 4) Cancelacion y reprogramacion

- `Confirmada por codigo` Cancelar un slot ocupado restablece datos operativos y deja estado `cancelado`.
  Fuentes: `supabase_schema.sql` (`admin_cancelar_slot`, `paciente_cancelar_slot`, `admin_cancelar_tanda`).

- `Confirmada por codigo` Reprogramar valida slot origen/destino, compatibilidad y disponibilidad del destino.
  Fuentes: `supabase_schema.sql` (`admin_reprogramar_slot*`), `src/lib/api.js` (normalizacion de errores `slot destino invalido`, `bloque destino invalido`), `src/components/admin/AdminTurnosDialogs.jsx`.

- `Confirmada por codigo` Bloques completos no se reprograman como slot individual.
  Fuentes: validaciones en `src/components/admin/ModalDetallePaciente.jsx`, errores SQL de reprogramacion de bloque/tanda.

- `Inferida` Reprogramacion desde detalle paciente mantiene dia y se usa como ajuste fino de hora/camara.
  Fuentes: texto UX en `ModalDetallePaciente.jsx`.

---

## 5) Agenda (horarios, camaras, generacion de slots)

- `Confirmada por codigo` Horarios (`horarios_config`) validan formato `HH:MM`, orden positivo y unicidad por hora.
  Fuentes: `supabase_schema.sql` (`admin_crear_horario_config`, `admin_actualizar_horario_config`).

- `Confirmada por codigo` No se puede cambiar la hora de un horario con slots futuros ya generados.
  Fuentes: `supabase_schema.sql` (`admin_actualizar_horario_config`), `src/hooks/useAdminConfiguracionActions.js` (mensaje UX).

- `Confirmada por codigo` Activar horario o camara dispara generacion de slots futuros.
  Fuentes: `supabase_schema.sql` (`admin_toggle_horario_config` -> `generar_slots_hora`, `admin_toggle_camara_activa` -> `generar_slots_camara_rango`).

- `Confirmada por codigo` Camara requiere nombre y capacidad >= 1.
  Fuentes: `supabase_schema.sql` (`admin_crear_camara`, `admin_actualizar_camara`).

- `Confirmada por codigo` Reducir capacidad de camara reconcilia slots futuros (movidos/cancelados/apartados liberados/eliminados) y devuelve resumen.
  Fuentes: `supabase_schema.sql` (`admin_actualizar_camara` + `admin_reconciliar_capacidad_camara`), `src/hooks/useAdminConfiguracionActions.js`.

- `Inferida` La agenda efectiva de UI usa solo horas activas para visualizacion y operacion.
  Fuentes: `src/lib/api.js` (`filterSlotsByHorasActivas` en lecturas de slots), hooks de horarios.

---

## 6) Turnos fuera de horario

- `Confirmada por codigo` Crear turno fuera de horario requiere fecha hoy/futura, hora y paciente valido.
  Fuentes: `supabase_schema.sql` (`admin_crear_turno_fuera_horario`), `src/components/admin/ModalTurnoFueraHorario.jsx`.

- `Confirmada por codigo` Crear/cancelar turno fuera de horario esta protegido por permiso dedicado (`can_turnos_fuera_horario` / `turnos.fuera_horario`).
  Fuentes: `supabase_schema.sql` (policy + funciones), `src/pages/AdminPanel.jsx`.

- `Confirmada por codigo` Para casos de monoxido: orden medica + resumen clinico son obligatorios, y medico tambien es obligatorio en UI.
  Fuentes: `supabase_schema.sql` (raise `Monoxido requiere orden medica y resumen clinico`), `src/components/admin/ModalTurnoFueraHorario.jsx`.

- `Inferida` Operador de camara en turno extra se trata como dato operativo obligatorio de trazabilidad.
  Fuentes: `ModalTurnoFueraHorario.jsx` (`canSubmit` exige `operadorCamaraId`), payload `apiCrearTurnoFueraHorario`.

---

## 7) Pacientes (alta, edicion, identidad y portal)

- `Confirmada por codigo` Alta/edicion de paciente exige: telefono, documento, condicion IVA; y nacionalidad cuando el documento contiene letras.
  Fuentes: `src/components/admin/FormNuevoPaciente.jsx`, `src/components/admin/ModalDetallePaciente.jsx`, `supabase_schema.sql` (`admin_actualizar_paciente`).

- `Confirmada por codigo` Si hay obra social cargada, numero de credencial es obligatorio en UI.
  Fuentes: `FormNuevoPaciente.jsx`, `ModalDetallePaciente.jsx`.

- `Confirmada por codigo` `datos_extra` debe ser JSON objeto valido.
  Fuentes: `supabase_schema.sql` (`admin_actualizar_paciente` -> `datos_extra invalido`).

- `Confirmada por codigo` Con portal habilitado, el documento normalizado debe ser unico entre pacientes con portal.
  Fuentes: `supabase_schema.sql` (`admin_actualizar_paciente`, `admin_configurar_portal_paciente`).

- `Confirmada por codigo` Habilitar portal exige documento valido y puede marcar `requiere_reset_portal`.
  Fuentes: `supabase_schema.sql` (`admin_configurar_portal_paciente`), `src/components/admin/ModalDetallePaciente.jsx`.

- `Confirmada por codigo` Login/activacion de portal exige clave minima de 8 caracteres y confirmacion coincidente.
  Fuentes: `src/pages/Login.jsx`, `src/pages/PacientePortal.jsx`.

- `Inferida` `login_identifier` de alta se considera inmutable luego de crear paciente.
  Fuentes: copy UI en `FormNuevoPaciente.jsx`; no hay enforcement duro visible en frontend.

---

## 8) Reglas operativas (obra social, referido, medico, nuevo ingreso, monoxido)

- `Confirmada por codigo` Modalidad `obra_social` requiere obra social del paciente valida y numero de autorizacion.
  Fuentes: `supabase_schema.sql` (`admin_resolver_operativo_obra_social`), `src/components/admin/AdminTurnosDialogs.jsx`, `ModalTanda.jsx`.

- `Confirmada por codigo` Si la obra social no tiene convenio, se exige corroboracion explicita de pago.
  Fuentes: `supabase_schema.sql` (`Debes corroborar el pago de la obra social`), `ModalTanda.jsx`, `AdminTurnosDialogs.jsx`.

- `Confirmada por codigo` Para autorizacion `S/N` hay reglas de ciclo de obra social (iniciar o informar ciclo existente).
  Fuentes: `supabase_schema.sql` (`admin_resolver_operativo_obra_social` version con ciclo), `ModalTanda.jsx`.

- `Confirmada por codigo` Se valida tope de sesiones autorizadas antes de confirmar/asignar series de obra social.
  Fuentes: `supabase_schema.sql` (`admin_validar_tope_obra_social_serie` invocado en asignar/confirmar/editar operativo).

- `Confirmada por codigo` Si `referido_tercero` es true, se exige referente.
  Fuentes: `ModalTanda.jsx`, `AdminTurnosDialogs.jsx`, resolucion de referente en SQL.

- `Confirmada por codigo` Si `es_nuevo_ingreso` es true, se exige medico.
  Fuentes: `ModalTanda.jsx`, `AdminTurnosDialogs.jsx`, `admin_resolver_medico_operativo`.

---

## 9) Consultas medicas e historia clinica

- `Confirmada por codigo` Consultas usan agenda propia (`consultas_slots` + horarios de consulta) con CRUD via RPC idempotentes.
  Fuentes: `src/hooks/useConsultas.js`, `src/lib/api.js`, `supabase_schema.sql` (`admin_*_consulta*_idempotent`).

- `Confirmada por codigo` Operaciones de consulta (asignar, cancelar, reprogramar, cerrar) invalidan historia clinica asociada.
  Fuentes: `src/hooks/useConsultas.js`.

- `Confirmada por codigo` Historia clinica tiene permisos separados para leer resumen/detalle, editar ficha, editar numero y crear evolucion.
  Fuentes: `src/pages/AdminPanel.jsx`, `src/lib/api.js` (mensajes de permiso).

- `Inferida` Consulta cerrada es parte del circuito clinico formal y se usa como insumo de evoluciones.
  Fuentes: RPC `admin_cerrar_consulta*`, `apiCrearHistoriaClinicaEvolucion`.

---

## 10) Roles y permisos (RBAC)

- `Confirmada por codigo` Acceso a `/usuario/*` requiere `app.admin_panel.access`; acceso a `/paciente/*` requiere `portal.access`.
  Fuentes: `src/App.jsx`, `src/hooks/useAccessControl.js`.

- `Confirmada por codigo` Cada tab del panel admin tiene permiso explicito (`turnos.read`, `consultas.read`, `rbac.read`, etc).
  Fuentes: `src/pages/AdminPanel.jsx` (`TAB_PERMISSIONS`).

- `Confirmada por codigo` Las acciones sensibles estan doblemente protegidas: gating frontend + validacion SQL/RLS.
  Fuentes: `AdminPanel.jsx`, `api.js` (mensajes `Prohibido/No autorizado`), `supabase_schema.sql` (funciones `can_*` y raises).

- `Inferida` El rol "admin" convive con permisos granulares; el sistema opera por permisos efectivos antes que por rol fijo.
  Fuentes: `useAuth` (`me_get_effective_access`), `useAccessControl`, `AdminPanel`.

---

## 11) Auditoria, trazabilidad e idempotencia

- `Confirmada por codigo` Cambios de turnos se auditan en `historial_bloques` con datos operativos (obra social, referente, medico, tanda, validacion).
  Fuentes: `schema/medical_center_schema.sql` (`admin_insert_historial_bloque` y llamadas desde funciones de turnos), `docs/aspnetApiProvider.js` (`apiGetHistorialBloque`).

- `Confirmada por codigo` Operaciones criticas usan idempotency key y ledger de requests (`operation_requests`) para evitar duplicados y resolver reintentos.
  Fuentes: `supabase_schema.sql` (`operation_request_*`, wrappers `*_idempotent`), `src/lib/api.js` (`callIdempotentRpc`).

- `Confirmada por codigo` Si se reenvia misma operacion con payload distinto, se devuelve error de conflicto de idempotencia.
  Fuentes: `src/lib/api.js` (`normalizeSensitiveRpcError`), SQL de `operation_request_begin`.

- `Inferida` El sistema esta disenado para tolerar doble click/reintentos de red sin duplicar efectos de negocio.
  Fuentes: patron idempotente transversal en turnos, consultas y turnos fuera de horario.

---

## 12) Reglas detectadas en queries y filtros de lectura

- `Confirmada por codigo` Queries de slots/consultas usan rango acotado y orden estable por fecha/hora/camara/lugar.
  Fuentes: `src/lib/api.js` (`apiGetSlotsByRango`, `apiGetConsultasByRango`).

- `Confirmada por codigo` En frontend se filtran slots por horas activas configuradas.
  Fuentes: `src/lib/api.js` (`filterSlotsByHorasActivas` aplicado en `apiGetSlotsByFecha`, `apiGetSlotsDisponiblesPacienteByFecha`, `apiGetSlotsByRango`).

- `Confirmada por codigo` Hooks de lectura se activan solo con parametros completos (`enabled`), evitando consultas invalidas.
  Fuentes: `src/hooks/useSlots.js`, `src/hooks/useConsultas.js`.

- `Inferida` Parte de la "regla de negocio de disponibilidad" esta en SQL y otra parte en filtros de presentacion (horarios activos).
  Fuentes: combinacion de SQL + `filterSlotsByHorasActivas`.

---

## 13) Posibles inconsistencias o puntos a vigilar

- `Confirmada por codigo` `supabase_schema.sql` contiene multiples redefiniciones historicas de funciones (`CREATE OR REPLACE`), por lo que la regla efectiva depende de la ultima version aplicada en la base remota.
  Impacto: riesgo de drift entre repo y entorno remoto si faltan migraciones.

- `Inferida` Algunas validaciones aparecen en UI (por ejemplo inmutabilidad del identificador de acceso) sin evidencia de enforcement duro equivalente en todas las rutas backend.
  Impacto: posible bypass si se llama RPC/Edge Function fuera del flujo UI esperado.

- `Inferida` Hay reglas duplicadas entre frontend (prevalidacion) y backend (validacion dura). Si divergen en el tiempo, puede haber mensajes confusos o rechazos tardios.
  Impacto: deuda de mantenimiento funcional.

---

## Resumen operativo
- La mayor parte de reglas criticas de negocio de turnos esta `Confirmada por codigo` en SQL (validacion dura + permisos + auditoria + idempotencia).
- El frontend agrega reglas de experiencia y prevalidacion (conflictos de consecutividad, wizard de tanda, formularios), tambien `Confirmada por codigo`.
- Las reglas `Inferidas` se concentran en intencion de producto o en restricciones expresadas en UX pero no siempre verificadas de forma explicita en todas las capas.
