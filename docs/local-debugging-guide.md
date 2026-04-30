# Debug local del backend MedicalCenter

## Objetivo de esta guía

Este documento explica, de forma bien detallada, cómo:

1. levantar el backend localmente,
2. apuntarlo a una base local o a una base ya existente,
3. probar endpoints con Swagger, Postman o Bruno,
4. debuggear autenticación, agendas, pacientes y turnos,
5. entender qué partes hoy son scaffolding y qué partes ya están implementadas.

> Importante: el proyecto hoy está targeteado a **.NET 8** y la build quedó cerrada sobre esa base.  
> Las dependencias principales ya se bajaron a versiones compatibles con `net8.0`. Si ves errores raros de build en `Api` o `Infrastructure`, el primer chequeo sigue siendo el SDK activo (`dotnet --version`) y la restauración de paquetes (`dotnet restore`).

---

## Estado actual del backend

Hoy el backend expone una base funcional para:

- health checks,
- autenticación JWT + refresh tokens,
- portal/staff auth básico,
- lecturas y mutaciones de pacientes,
- lecturas y mutaciones de cámaras/horarios,
- lecturas de turnos,
- mutaciones de turnos sensibles:
  - asignar,
  - cancelar,
  - reprogramar,
  - apartar,
  - confirmar apartado,
  - liberar apartado,
  - asignar bloque completo,
  - cancelar bloque completo,
  - cancelar tanda,
- idempotencia persistida para mutaciones sensibles.

### Lo que sigue siendo parcial o incompleto

- permisos/policies finas por cada endpoint,
- tanda secuencial multi-fecha completa,
- reglas clínicas/operativas avanzadas,
- integración directa con la base productiva ya existente,
- tests end-to-end reales contra una instancia PostgreSQL accesible desde el runner.

### Estado de build y verificación local

1. **La solución está targeteada a .NET 8**
   - `dotnet --list-sdks` debería mostrar `8.0.420` o una versión 8.x compatible.
   - El `global.json` del repo fija ese SDK para evitar que se use otro por accidente.

2. **La validación actual está cerrada**
   - `dotnet build .\src\MedicalCenter.Api\MedicalCenter.Api.csproj -p:NoWarn=NU1900 -v minimal`
   - `dotnet test .\tests\MedicalCenter.UnitTests\MedicalCenter.UnitTests.csproj --no-restore -p:NoWarn=NU1900 -v minimal`
   - `dotnet test .\tests\MedicalCenter.IntegrationTests\MedicalCenter.IntegrationTests.csproj --no-restore -p:NoWarn=NU1900 -v minimal`

3. **Las dependencias principales ya están alineadas a net8**
   - `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11`
   - `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 8.0.11`
   - `Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11`
   - `Microsoft.EntityFrameworkCore.InMemory 8.0.11`
   - `Microsoft.AspNetCore.Mvc.Testing 8.0.25`

4. **Si `Api` o `Infrastructure` vuelven a fallar**
   - revisá primero `dotnet restore` sobre el `.csproj` puntual,
   - confirmá que no haya una instalación de SDK mezclada entre 8.x y 10.x,
   - y si el error vuelve a ser de grafo/resolución, limpia `bin/obj` y reintenta restore con `-m:1`.

### Qué hacer si te pasa lo mismo

- verificar `dotnet --version`,
- revisar `global.json`,
- correr `dotnet restore` sobre el `.csproj` que estés depurando,
- y si la build se rompe por SDK/workload resolver, preferir una instalación limpia del SDK 8 antes que mezclar versiones.

---

## Requisitos previos

### Opción A: correr con Docker Compose

Necesitás:

- Docker Desktop
- .NET SDK 8 instalado si también querés correr comandos `dotnet` fuera de Docker

### Opción B: correr desde Visual Studio / Rider / VS Code

Necesitás:

- .NET SDK 8
- PostgreSQL accesible

---

## Estructura importante del proyecto

- API: `src/MedicalCenter.Api`
- Application: `src/MedicalCenter.Application`
- Domain: `src/MedicalCenter.Domain`
- Infrastructure: `src/MedicalCenter.Infrastructure`
- Contracts: `src/MedicalCenter.Contracts`
- Tests unitarios: `tests/MedicalCenter.UnitTests`
- Tests integración: `tests/MedicalCenter.IntegrationTests`

---

## Variables de entorno importantes

El backend usa principalmente:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SecretKey`
- `Seed__AdminIdentifier`
- `Seed__AdminEmail`
- `Seed__AdminPassword`
- `ASPNETCORE_ENVIRONMENT`

### Ejemplo para PowerShell

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=medical_center;Username=postgres;Password=postgres"
$env:Jwt__Issuer="MedicalCenter"
$env:Jwt__Audience="MedicalCenter.Client"
$env:Jwt__SecretKey="change-this-secret-in-production-with-at-least-32-chars"
```

---

## Cómo levantar el proyecto localmente

## Opción 1: Docker Compose

Desde la raíz:

```powershell
docker compose up --build
```

Servicios:

- API: `http://localhost:8080`
- Postgres: `localhost:5432`

### Endpoints útiles

- Swagger UI: `http://localhost:8080/swagger`
- Health readiness: `http://localhost:8080/health/ready`

---

## Opción 2: correr solo la API local

1. levantá PostgreSQL por tu cuenta,
2. seteá las variables de entorno,
3. ejecutá:

```powershell
dotnet run --project src/MedicalCenter.Api/MedicalCenter.Api.csproj
```

Si usás Visual Studio o Rider:

- proyecto de inicio: `MedicalCenter.Api`
- profile: cualquiera que herede variables de entorno correctas

---

## Qué hace el startup al arrancar

Al iniciar, la aplicación:

1. registra servicios de Application + Infrastructure,
2. configura controllers,
3. habilita Swagger en Development,
4. registra health checks,
5. aplica migraciones EF,
6. si la base está vacía, inserta seed mínima de arranque.

### Ojo con esto

El seed actual **solo corre si no existen usuarios**.

Eso significa:

- si apuntás a una base vacía, vas a tener datos demo;
- si apuntás a una base preexistente con usuarios, no se insertará seed.

---

## Cómo probar con Swagger

Si corrés en Development:

1. abrí `http://localhost:8080/swagger`
2. probá primero:
   - `GET /health/ready`
   - `POST /api/v1/auth/login`
3. copiá el `access_token`
4. en Swagger usá **Authorize**
5. probá endpoints protegidos

---

## Cómo probar con Postman

## 1. Crear environment

Variables sugeridas:

- `base_url = http://localhost:8080`
- `access_token =`
- `refresh_token =`
- `patient_id =`
- `slot_id =`
- `slot_target_id =`
- `tanda_id =`

## 2. Login staff

### Request

`POST {{base_url}}/api/v1/auth/login`

Body:

```json
{
  "identifier": "admin",
  "password": "Admin123!"
}
```

> Esto sirve si estás usando la base demo/seed local.

Guardá:

- `access_token`
- `refresh_token`

## 3. Authorization header

En requests protegidos:

```http
Authorization: Bearer {{access_token}}
```

## 4. Probar health

`GET {{base_url}}/health/ready`

## 5. Probar pacientes

### Listar

`GET {{base_url}}/api/v1/pacientes`

### Crear

`POST {{base_url}}/api/v1/pacientes`

Body ejemplo:

```json
{
  "nombre": "Paciente Test",
  "email": "paciente.test@local.dev",
  "telefono": "1122334455",
  "documento_identidad": "30111222",
  "documento_identidad_normalizado": "30111222",
  "nacionalidad": null,
  "condicion_iva_id": 1,
  "obra_social_id": null,
  "numero_credencial_obra_social": null,
  "portal_habilitado": true,
  "requiere_reset_portal": true,
  "login_identifier": "paciente.test",
  "claustrofobico": false,
  "notas": "Creado desde Postman",
  "datos_extra": {},
  "opt_in_whatsapp": true
}
```

## 6. Probar agendas

### Cámaras

- `GET {{base_url}}/api/v1/agendas/camaras`
- `POST {{base_url}}/api/v1/agendas/camaras`
- `PATCH {{base_url}}/api/v1/agendas/camaras/{id}`
- `PATCH {{base_url}}/api/v1/agendas/camaras/{id}/estado`

### Horarios

- `GET {{base_url}}/api/v1/agendas/horarios`
- `POST {{base_url}}/api/v1/agendas/horarios`
- `PATCH {{base_url}}/api/v1/agendas/horarios/{id}`
- `PATCH {{base_url}}/api/v1/agendas/horarios/{id}/estado`

## 7. Probar turnos

### Lecturas

- `GET {{base_url}}/api/v1/turnos?fecha=2026-04-25`
- `GET {{base_url}}/api/v1/turnos/rango?fecha_inicio=2026-04-25&fecha_fin=2026-04-30`
- `GET {{base_url}}/api/v1/turnos/pacientes/{pacienteId}/activos`

### Mutaciones con idempotencia

Para estas requests agregá:

```http
Idempotency-Key: 11111111-1111-1111-1111-111111111111
```

#### Asignar

`POST {{base_url}}/api/v1/turnos/{{slot_id}}/asignaciones`

```json
{
  "paciente_id": "{{patient_id}}",
  "es_tanda": false,
  "tanda_id": null,
  "accion": "asignado"
}
```

#### Cancelar

`POST {{base_url}}/api/v1/turnos/{{slot_id}}/cancelaciones`

```json
{
  "motivo": "Cancelado desde Postman"
}
```

#### Reprogramar

`POST {{base_url}}/api/v1/turnos/{{slot_id}}/reprogramaciones`

```json
{
  "target_slot_id": "{{slot_target_id}}",
  "scope": "normal"
}
```

#### Apartar

`POST {{base_url}}/api/v1/turnos/{{slot_id}}/apartados`

```json
{
  "paciente_id": "{{patient_id}}",
  "es_monoxido": false
}
```

#### Confirmar apartado

`POST {{base_url}}/api/v1/turnos/{{slot_id}}/apartados/confirmaciones`

```json
{
  "paciente_id": "{{patient_id}}",
  "es_monoxido": false
}
```

#### Liberar apartado

`POST {{base_url}}/api/v1/turnos/{{slot_id}}/apartados/liberaciones`

```json
{
  "motivo": "Liberado manualmente"
}
```

#### Asignar bloque completo

`POST {{base_url}}/api/v1/turnos/bloques/asignaciones`

```json
{
  "fecha": "2026-04-25",
  "hora": "10:00:00",
  "camara_id": 1,
  "paciente_id": "{{patient_id}}",
  "es_tanda": false,
  "tanda_id": null
}
```

#### Cancelar bloque completo

`POST {{base_url}}/api/v1/turnos/bloques/cancelaciones`

```json
{
  "fecha": "2026-04-25",
  "hora": "10:00:00",
  "camara_id": 1,
  "motivo": "Cancelación de bloque"
}
```

#### Cancelar tanda

`POST {{base_url}}/api/v1/turnos/tandas/{{tanda_id}}/cancelaciones`

```json
{
  "motivo": "Cancelación de tanda"
}
```

---

## Cómo obtener IDs para probar

### Paciente

Usá:

```http
GET /api/v1/pacientes
```

### Slot

Usá:

```http
GET /api/v1/turnos?fecha=YYYY-MM-DD
```

### Tanda

Hoy `tanda_id` aparece cuando:

- se asignó bloque con `es_tanda = true`, o
- ya existía persistido.

---

## Cómo debuggear desde el front existente

## Qué se puede probar desde el front hoy

### Muy probable que funcione o se pueda adaptar fácil

- login básico del staff si el frontend usa payload compatible,
- listados de pacientes,
- listados/configuración de cámaras y horarios,
- lecturas de turnos por fecha/rango,
- algunas mutaciones de turnos si el frontend manda:
  - la ruta exacta,
  - el payload actual,
  - y `Idempotency-Key`.

### Lo que todavía puede requerir adapter o feature flag

- flujos completos de tanda multi-fecha,
- reglas clínicas/obra social avanzadas,
- algunos flujos portal existentes si esperan shape legacy exacto,
- cualquier cosa que en el frontend dependa de RPC específicos de Supabase aún no replicados 1:1.

### Recomendación para front

Para probarlo bien sin romper UX:

1. crear un feature flag tipo `USE_DOTNET_BACKEND=true`,
2. redirigir solo algunos hooks/servicios:
   - auth,
   - pacientes,
   - agendas,
   - lecturas de turnos,
3. dejar Supabase como fallback mientras se valida.

---

## Debug paso a paso de errores comunes

## 1. `401 Unauthorized`

Revisar:

- `Authorization: Bearer ...`
- expiración del access token
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SecretKey`

## 2. `409 conflict`

Puede deberse a:

- turno no disponible,
- operación concurrente,
- idempotency key reutilizada con payload distinto,
- turno pasado,
- conflicto de consecutividad.

Revisar:

- body enviado,
- `Idempotency-Key`,
- estado actual del turno en DB,
- logs del backend.

## 3. `code = idempotency_mismatch`

La misma `Idempotency-Key` se reutilizó con payload distinto.

Solución:

- generar una key nueva,
- o reenviar exactamente el mismo payload.

## 4. `code = operation_pending`

Otra request con la misma key todavía está en curso o quedó marcada como pendiente.

Revisar tabla:

- `operation_requests`

## 5. El seed no aparece

Si ya hay usuarios en la base:

- no corre el seed demo.

---

## Cómo inspeccionar la base

Podés usar:

- pgAdmin
- DBeaver
- Azure Data Studio con extensión PostgreSQL
- psql

### Query útiles

```sql
select * from users order by id;
select * from patients order by nombre;
select * from cameras order by id;
select * from schedule_hours order by "Order";
select * from appointments order by "Fecha", "Hora", "CameraId", "Lugar";
select * from operation_requests order by "CreatedAt" desc;
```

> Ojo: los nombres exactos de columnas dependen del mapping actual de EF, no necesariamente del SQL legacy.

---

## Cómo debuggear migraciones

### Ver migraciones existentes

```powershell
dotnet ef migrations list --project src/MedicalCenter.Infrastructure --startup-project src/MedicalCenter.Api
```

### Aplicar migraciones

```powershell
dotnet ef database update --project src/MedicalCenter.Infrastructure --startup-project src/MedicalCenter.Api
```

### Agregar nueva migración

```powershell
dotnet ef migrations add NombreMigracion --project src/MedicalCenter.Infrastructure --startup-project src/MedicalCenter.Api
```

---

## Muy importante: base productiva existente

Hoy el backend **no está listo para conectarse directamente a producción sin un análisis previo de compatibilidad**.

### Por qué

Aunque se tomó como referencia la documentación del dominio y se revisó `schema/medical_center_schema.sql`, el scaffolding **no fue modelado como réplica exacta 1:1 del esquema productivo legacy**.

En particular:

- sí se tomaron referencias de nombres y conceptos de dominio,
- sí se respetaron muchos nombres funcionales del negocio,
- pero **EF hoy está generando su propio modelo físico** para el backend nuevo,
- y no todos los nombres/tablas/columnas coinciden necesariamente con la base existente en producción.

### Respuesta corta a tu pregunta

**No, no me basé exclusivamente en `schema/medical_center_schema.sql` para clonar exactamente todas las tablas tal cual existen hoy en producción.**

Lo usé como **fuente de referencia del dominio y de varias reglas/entidades**, junto con:

- `docs/api-contracts.md`
- `docs/domain-rules.md`
- `docs/backend-migration-plan.md`
- `docs/codex-instructions.md`

Pero el resultado actual es un **backend nuevo con modelo propio**, no un mirror exacto del esquema legacy.

### Qué habría que hacer antes de usar la base productiva real

1. comparar tabla por tabla:
   - nombres,
   - columnas,
   - tipos,
   - nullability,
   - índices,
   - FKs,
   - enums/estados;
2. definir si la estrategia será:
   - **compatibilidad con esquema existente**, o
   - **esquema nuevo con migración de datos**;
3. revisar seeds / initializers para que no interfieran;
4. probar todo primero contra una copia anonimizada o staging de producción.

---

## Recomendación práctica antes de seguir

Antes de avanzar más, conviene decidir una de estas dos rutas:

### Ruta A: backend nuevo con esquema propio

- seguir con EF/migraciones actuales,
- luego crear scripts de migración/importación,
- adaptar el frontend gradualmente.

### Ruta B: backend compatible con la base existente

- refactorizar el mapping EF para alinearlo al schema real,
- evitar drift entre migraciones y producción,
- validar cuidadosamente constraints y nombres legacy.

Si vas por la **Ruta B**, el próximo paso ideal es una tarea de:

> “hacer un gap analysis entre el modelo EF actual y `schema/medical_center_schema.sql` / base real existente, y proponer plan de compatibilidad”.
