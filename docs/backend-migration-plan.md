# Plan incremental de migracion: Supabase -> ASP.NET Core + PostgreSQL

## 1. Objetivo
Migrar el frontend hacia una API propia en ASP.NET Core, retirando la dependencia runtime de Supabase.
- React frontend (sin acceso directo a Supabase)
- ASP.NET Core Web API (backend principal)
- PostgreSQL gestionado por el equipo
- despliegue en VPS con Docker Compose

Condicion clave: sin "big bang", sin frenar operacion diaria y sin perder reglas criticas de turnos.

---

## 2. Alcance real del sistema a migrar

Basado en el estado actual del repo, la migracion debe cubrir:
- Auth portal + staff (hoy combinado entre Supabase Auth y funciones `portal-*`)
- RBAC por permisos efectivos (hoy `me_get_effective_access` + checks `hasPermission`)
- Dominio de turnos hiperbaricos:
  - asignar/cancelar/reprogramar/apartar/confirmar/liberar
  - bloque completo
  - tanda
  - turnos fuera de horario
- Reglas operativas (obra social, autorizaciones, ciclos, referido, medico, monoxido)
- Consultas medicas + historia clinica
- Auditoria (`historial_bloques`, eventos)
- Idempotencia operacional (`operation_requests` + `*_idempotent`)
- Notificaciones/dispatch (WhatsApp)

Fuera de alcance inicial (puede ir luego):
- cambios mayores de UX
- rediseño de modelo funcional
- descomposicion a microservicios

---

## 3. Principios de migracion
- Migrar por "strangler pattern": backend nuevo rodea y reemplaza endpoints por dominio.
- Mantener una sola fuente de verdad por operacion en cada fase (no doble escritura sin control).
- Preservar semantica actual de negocio antes de optimizar.
- Reproducir primero comportamiento; refactorizar despues.
- Cada fase debe incluir:
  - feature flag
  - pruebas de regresion
  - rollback claro

---

## 4. Arquitectura destino (v1)

### 4.1 Backend
- ASP.NET Core 8 Web API
- Monolito modular:
  - `Api`
  - `Application`
  - `Domain`
  - `Infrastructure`
- Persistencia:
  - EF Core + Npgsql para CRUD y consultas comunes
  - SQL explicito / funciones SQL para operaciones de concurrencia fuerte

### 4.2 Base de datos
- PostgreSQL dedicado
- Migraciones versionadas (EF Migrations + scripts SQL para reglas complejas)
- Tablas de soporte:
  - `operation_requests` (idempotencia)
  - `outbox_events` (integracion async / notificaciones)
  - `audit_log` (si se decide separar de `historial_bloques`)

### 4.3 Seguridad
- JWT emitido por backend propio
- Authorization basada en permisos (claims) + validacion server-side
- Rate limiting para endpoints publicos (portal)
- Auditoria de acciones sensibles

### 4.4 Frontend
- Capa `BackendApiClient` desacoplada de proveedor
- Feature flags por modulo (auth, turnos, pacientes, consultas, etc.)
- Eliminacion gradual de llamadas directas a `supabase` desde `src/lib/api.js`

---

## 5. Dependencias criticas (en orden de bloqueo)

1. **Inventario funcional cerrado**
- `docs/current-architecture.md`
- `docs/domain-rules.md`
- inventario de uso Supabase y RPCs idempotentes

2. **Contratos API**
- OpenAPI por modulo
- payloads y errores compatibles con frontend actual

3. **Modelo de auth y permisos**
- matriz permisos actual -> claims JWT
- estrategia de migracion de usuarios (staff/pacientes)

4. **Estrategia de datos**
- esquema inicial destino
- plan de backfill + validaciones de consistencia

5. **Observabilidad minima**
- logs estructurados
- metricas de exito/falla por endpoint
- trazabilidad de idempotencia y conflictos de concurrencia

---

## 6. Riesgos principales y mitigaciones

### R1. Reglas de negocio distribuidas entre UI + SQL
- Riesgo: divergencia funcional al mover logica a C#.
- Mitigacion:
  - porting por caso de uso (no por tabla)
  - tests de contrato con fixtures reales
  - checklist de equivalencia por RPC actual

### R2. Concurrencia de turnos (doble reserva / race conditions)
- Riesgo: inconsistencias criticas de agenda.
- Mitigacion:
  - operaciones transaccionales con lock por slot/bloque
  - restricciones unicas e invariantes SQL
  - idempotencia obligatoria en mutaciones sensibles

### R3. Perdida de seguridad al reemplazar RLS
- Riesgo: exponer datos por error en backend.
- Mitigacion:
  - policy parity checklist (RLS -> authorization handlers)
  - tests negativos por permiso (como los SEC-* actuales)
  - revision de threat model en cada release

### R4. Auth portal (activacion/reset/login) sensible a abuso
- Riesgo: account takeover, enumeracion, brute force.
- Mitigacion:
  - replicar tokens one-time, TTL, consumo atomico
  - rate limiting por IP + identificador
  - respuestas homogeneas

### R5. Corte operativo en migracion
- Riesgo: downtime o degradacion en flujos admin.
- Mitigacion:
  - convivencia temporal con feature flags
  - rollout canary por modulo/rol
  - rollback inmediato por flag

### R6. Integraciones WhatsApp/notificaciones
- Riesgo: duplicados o perdida de mensajes.
- Mitigacion:
  - outbox pattern
  - deduplicacion por idempotency key
  - reconciliacion batch post-release

---

## 7. Estrategia de convivencia temporal (Supabase + backend nuevo)

## 7.1 Patrone de convivencia
- Frontend llama siempre a una fachada local (`DomainApi`).
- La fachada enruta por feature flag:
  - `provider = supabase`
  - `provider = aspnet`

## 7.2 Reglas de convivencia
- Nunca mezclar proveedores para una misma mutacion dentro de una pantalla.
- Una operacion critica (ej: asignar slot) se ejecuta en un solo backend por request.
- Lecturas pueden convivir temporalmente con "shadow read" para comparacion, sin impactar UX.

## 7.3 Shadow mode sugerido
- Fase inicial:
  - write en Supabase (oficial)
  - write espejo en ASP.NET deshabilitada
  - read espejo desde ASP.NET solo para comparar resultados en logs
- Fase intermedia:
  - write oficial en ASP.NET para modulo migrado
  - fallback a Supabase via flag de emergencia

## 7.4 Flags minimos
- `ff_backend_auth`
- `ff_backend_pacientes`
- `ff_backend_turnos_read`
- `ff_backend_turnos_write`
- `ff_backend_consultas`
- `ff_backend_portal`
- `ff_backend_notificaciones`

---

## 8. Orden sugerido de migracion (incremental)

## Fase 0 - Preparacion (semana 1-2)
Entregables:
- contratos OpenAPI baseline
- solucion ASP.NET inicial + healthcheck + auth stub
- pipeline CI/CD + entorno staging
- esquema PostgreSQL inicial

Salida:
- backend deployable vacio pero operativo

## Fase 1 - Capa de abstraccion frontend (semana 2-3)
Entregables:
- `src/lib/api.js` desacoplado a interfaz proveedor
- adaptador Supabase actual como implementacion por defecto
- feature flags operativas

Salida:
- frontend listo para cambiar provider sin reescribir UI

## Fase 2 - Auth y acceso (semana 3-5)
Alcance:
- login staff/paciente
- activacion/reset portal
- endpoint equivalente a `me_get_effective_access`

Dependencias:
- modelo JWT + claims permisos
- tablas portal_access_tokens / login_failures / rate_limit

Salida:
- `ff_backend_auth` y `ff_backend_portal` habilitables por entorno

## Fase 3 - Lecturas de catalogos y pacientes (semana 5-6)
Alcance:
- pacientes, obras sociales, medicos, referentes, horarios/camaras (read)
- compatibilidad de filtros actuales

Salida:
- `ff_backend_pacientes` (read-first)

## Fase 4 - Turnos write criticos (semana 6-9)
Alcance:
- asignar/cancelar/reprogramar
- apartar/confirmar/liberar apartado
- bloque completo
- tanda
- fuera de horario

Requisito:
- idempotencia obligatoria en backend nuevo
- pruebas de concurrencia y no-regresion de reglas consecutivas/obra social

Salida:
- `ff_backend_turnos_write` en canary

## Fase 5 - Consultas e historia clinica (semana 9-10)
Alcance:
- asignar/cancelar/reprogramar/cerrar consultas
- editar ficha, numero HC, evoluciones

Salida:
- `ff_backend_consultas` activo por roles piloto

## Fase 6 - Notificaciones y eventos (semana 10-11)
Alcance:
- confirmaciones/cancelaciones/recordatorios
- outbox + workers

Salida:
- `ff_backend_notificaciones`

## Fase 7 - Decomision de Supabase runtime (semana 11-12)
Acciones:
- frontend sin llamadas runtime a Supabase
- congelar RPCs legacy
- plan de retiro de Edge Functions
- mantener acceso read-only temporal para auditoria

---

## 9. Estrategia de datos y migracion de estado

## 9.1 Enfoque
- Crear esquema destino compatible con dominio actual.
- Backfill inicial por lotes.
- Delta sync temporal (si conviven writes en ambos, solo durante ventana controlada).

## 9.2 Datos sensibles a migrar con cuidado
- perfiles + permisos/roles efectivos
- slots y su estado operativo
- tandas y bloques completos
- historial_bloques
- portal_access_tokens y flags de reset
- operation_requests (si se mantiene continuidad de idempotencia)

## 9.3 Validaciones de consistencia post-backfill
- conteo por tabla
- checks por dia/camara/hora/lugar
- turnos ocupados por paciente
- integridad de tandas (slots activos por tanda)
- muestreo de historial vs acciones recientes

---

## 10. Pruebas y criterios de salida por fase

Cada fase se considera completa si cumple:
- tests unitarios de casos de uso
- tests de integracion con PostgreSQL real
- tests negativos de permisos
- pruebas de concurrencia (turnos)
- comparacion de respuestas contra provider Supabase (cuando aplique)
- rollback probado via feature flag

KPIs recomendados:
- tasa de error por endpoint
- latencia p95
- conflictos de idempotencia
- inconsistencias de agenda detectadas
- incidentes de seguridad/permisos

---

## 11. Plan de rollback

Rollback por fase:
- toggle de feature flag al provider Supabase
- mantener ambos stacks desplegados durante ventana de estabilizacion
- scripts de reparacion de datos para operaciones parcialmente aplicadas

Regla:
- ningun corte irreversible hasta completar Fase 7 + 2 ciclos de cierre operativo sin incidentes.

---

## 12. Secuencia recomendada de implementacion tecnica

1. Crear solucion ASP.NET + proyecto de dominios + auth base.
2. Implementar `DomainApi` en frontend con adapter Supabase actual.
3. Definir contratos de `Auth/Access`.
4. Migrar auth portal y `me_get_effective_access`.
5. Migrar lecturas maestras.
6. Migrar mutaciones criticas de turnos con idempotencia.
7. Migrar consultas e historia clinica.
8. Migrar notificaciones con outbox.
9. Retirar supabase-js del runtime frontend.

---

## 13. Resultado esperado final
- Frontend sin dependencia runtime de Supabase.
- Reglas criticas concentradas en ASP.NET Core + PostgreSQL.
- Seguridad y permisos equivalentes o superiores al estado actual.
- Operacion estable en VPS con observabilidad y rollback.
- Supabase retirado del camino transaccional principal.

