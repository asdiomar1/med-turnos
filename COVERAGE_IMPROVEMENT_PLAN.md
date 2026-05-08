# Plan de Mejora de Cobertura — MedicalCenter

> **Objetivo**: Alcanzar 80% de cobertura en código nuevo para pasar el quality gate de SonarCloud.
> **Restricción**: El quality gate "Sonar way" (80% en `new_coverage`) **no se puede modificar**.
> **Estrategia**: Trabajo en paralelo sin conflictos — cada tarea afecta archivos diferentes.
> **Estado**: En seguimiento auditable (T1-T16 + Final) | Basado en evidencia local + CI/Sonar cuando aplique

> 📋 **Última actualización**: 2026-05-08 — Se auditó el plan contra el estado real del repositorio y se ajustó con snapshot de `sonar-issues-main.json` (branch `main`).

---

## Version Ejecutiva (estado actual)

### Contexto de etapa

- El proyecto esta en fase de estabilizacion de calidad y cobertura.
- Seguimiento de Sonar en esta etapa: priorizar **tendencia de mejora** y reduccion de issues sobre cierre estricto inmediato del quality gate.
- Progreso observado: se redujo de **200+ issues** a **6 issues abiertos** en `main`.

### Estado ejecutivo por bloque

| Bloque | Estado ejecutivo | Nota |
|-------|------------------|------|
| Unit tests principales (T2-T8, T12) | En gran parte completado | `T7` queda como validacion parcial de profundidad de cobertura. |
| Integration tests (T9-T11, T13) | Implementados con desvio controlado | Cobertura alineada al contrato actual de API/repositorios. |
| Sonar fixes de codigo (T14, T14a, T16) | Completado | Correcciones aplicadas en repositorios y mappings. |
| T15 | Obsoleto | La ruta objetivo del plan original no existe en el repo actual. |
| T1 | Desvio temporal intencional | Se removio exclusion para medir cobertura real del proyecto. |
| Final (quality gate Sonar) | Seguimiento progresivo | Estado final depende de evidencia remota (CI/Sonar) al cierre de estabilizacion. |

### Snapshot Sonar actual (`sonar-issues-main.json`)

| Metrica | Valor |
|--------|-------|
| Exportado | 2026-05-08 16:35:48 (-03:00) |
| Branch | `main` |
| Total de issues abiertos | **6** |
| Severidad | 6 `INFO` |
| Tipo | 6 `CODE_SMELL` |
| Regla dominante | `CA1822` (5 issues) |
| Regla adicional | `CA1859` (1 issue) |

| Archivo | Issues abiertos | Reglas |
|--------|------------------|--------|
| `tests/MedicalCenter.IntegrationTests/Persistence/RbacAdminRepositoryTests.cs` | 5 | `CA1822` |
| `tests/MedicalCenter.UnitTests/Features/WhatsApp/WhatsappServiceTests.cs` | 1 | `CA1859` |

### Politica de actualizacion durante estabilizacion

- Mantener `Final` como seguimiento progresivo mientras se sostenga tendencia de mejora.
- Actualizar estado por evidencia verificable en repo y, cuando exista, run de CI/Sonar.
- Revisar semanalmente la matriz auditada para evitar desfasajes documentales.

---

## 📋 Organización del trabajo

### Convenciones generales (TODAS las tareas)

| Regla | Descripción |
|-------|-------------|
| **Ubicación de tests** | Mismo proyecto de tests existente según la capa (`UnitTests`, `IntegrationTests`) |
| **Framework** | xUnit (usado en todo el proyecto) |
| **Mocking** | Usar NSubstitute (ya referenciado) o los `Dummy`/`Fake` helpers existentes |
| **Nombres** | `{Método}_{Escenario}_{ResultadoEsperado}` |
| **Arrange/Act/Assert** | Separar con líneas en blanco |
| **No tocar** | No modificar el código fuente cubierto — solo agregar tests |
| **CI** | Cada PR debe pasar `dotnet test` completo antes de mergear |

### Dependencias entre tareas

```
T1 (Sonar exclusions) ─────── ✅ COMPLETED
                              │
T2 (Unit: Appointments) ───── ✅ COMPLETED
T3 (Unit: Consultations) ──── ✅ COMPLETED
T4 (Unit: Catalogs) ───────── ✅ COMPLETED
                              │
T14-T16 (Sonar fixes) ────────┤✅ COMPLETED
                              │
T5 (Unit: Patients) ──────────┤✅ COMPLETED
T6 (Unit: Professionals) ──────┤✅ COMPLETED
T7 (Unit: ClinicalHistory) ────┤✅ COMPLETED
T8 (Unit: OutOfHours) ────────┤✅ COMPLETED
                              │
T9 (Integration: AppointmentsRepo) ───┤✅ COMPLETED
T10 (Integration: Auth) ───────────────┤✅ COMPLETED
T11 (Integration: AppointmentsCtrl) ──┤✅ COMPLETED
T13 (Unit: RbacAdminRepo) ────────────┤✅ COMPLETED
                              │
T12 (Unit: WhatsApp) ───────── ✅ COMPLETED
```

Cada tarea es un **PR independiente**. Se pueden asignar a distintas personas sin riesgo de colisiones.

### Fases de implementación

| Fase | Tareas | Duración | Descripción |
|------|--------|----------|-------------|
| **Phase 1** | T14, T14a, T15, T16 | ~2 horas | Quick wins - fixes de Sonar rápidos |
| **Phase 2** | T5, T6, T7, T8 | ~3-4 días | Unit tests para servicios sin cobertura |
| **Phase 3** | T9, T10, T11, T13 | ~2-3 días | Integration tests para repos y controllers |
| **Phase 4** | T12 | ~3-4 días | Opcional - WhatsApp services complejos |

---

## T1 — Excluir `DatabaseInitializer.cs` de SonarCloud

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🔴 **Alta** |
| **Archivo** | `.github/workflows/ci-quality.yml` |
| **Líneas impactadas** | ~600 (seed data, no testeable) |
| **Ganancia de cobertura** | ~8% |
| **Duración estimada** | 5 min |
| **Asignable a** | Cualquiera |

### Especificación

Agregar `**/DatabaseInitializer.cs` a `sonar.exclusions` en el step `Sonar begin` del workflow.

**Cambio concreto** (línea 99 actual):
```yaml
/d:sonar.exclusions="**/Migrations/**" \
```
→
```yaml
/d:sonar.exclusions="**/Migrations/**,**/DatabaseInitializer.cs" \
```

### ✅ Criterios de aceptación
- [ ] `sonar.exclusions` incluye `**/DatabaseInitializer.cs`
- [x] Se documenta explicitamente el desvio temporal para medir cobertura real
- [ ] SonarCloud valida estado final una vez cerrada la etapa de estabilizacion

> Nota de estado actual: la exclusion de `DatabaseInitializer.cs` fue removida intencionalmente para observar cobertura real end-to-end.

### ❌ Fuera de alcance
- No modificar `DatabaseInitializer.cs`
- No agregar tests para seed data

---

## T2 — Unit tests: `AppointmentsService`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🔴 **Alta** |
| **Archivo fuente** | `src/MedicalCenter.Application/Features/Appointments/AppointmentsService.cs` |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/Appointments/AppointmentsServiceTests.cs` |
| **Cobertura actual** | ~21% (1.227 líneas) |
| **Cobertura objetivo** | 80% |
| **Líneas a cubrir** | ~980 |
| **Duración estimada** | 4-6 horas |
| **Asignable a** | Persona A |

### Especificación

Agregar tests unitarios para `AppointmentsService` usando NSubstitute para mockear dependencias.

**Dependencias a mockear** (interfaz → mock):
- `IAppointmentRepository` → `_appointmentRepo`
- `IPatientRepository` → `_patientRepo`
- `IUnitOfWork` → `_unitOfWork`
- `IClock` → `_clock`

### Métodos a cubrir (por orden de prioridad)

| Método | Escenarios a testear | Assert clave |
|--------|---------------------|-------------|
| `CreateAsync` | - Paciente existe → crea turno<br>- Paciente no existe → exception<br>- Fecha inválida (pasada) → exception<br>- Superposición de horario → exception | `_appointmentRepo.Received(1).AddAsync(...)` |
| `ReserveAsync` | - Turno disponible → reserva<br>- Turno ya reservado → exception<br>- Turno cancelado → exception | `result.Status == Reserved` |
| `ConfirmAsync` | - Turno reservado por el mismo paciente → confirma<br>- Turno no reservado → exception | `result.Status == Confirmed` |
| `CancelAsync` | - Turno existe → cancela<br>- Turno ya cancelado → exception | `result.Status == Cancelled` |
| `GetByIdAsync` | - Turno existe → devuelve datos<br>- Turno no existe → null | `result != null` / `result == null` |
| `ListByPatientAsync` | - Paciente con turnos → lista<br>- Paciente sin turnos → lista vacía | `result.Count >= 0` |
| `ListByDateRangeAsync` | - Rango con turnos → lista<br>- Rango sin turnos → lista vacía | `result.Count >= 0` |

### Estructura del archivo test

```csharp
namespace MedicalCenter.UnitTests.Features.Appointments;

public sealed class AppointmentsServiceTests
{
    private readonly IAppointmentRepository _appointmentRepo = Substitute.For<IAppointmentRepository>();
    private readonly IPatientRepository _patientRepo = Substitute.For<IPatientRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly AppointmentsService _sut;

    public AppointmentsServiceTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));
        _sut = new AppointmentsService(
            _appointmentRepo, _patientRepo, _unitOfWork, _clock);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsAppointment()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

### ✅ Criterios de aceptación
- [x] Todos los métodos públicos de `AppointmentsService` tienen tests
- [x] Cada método cubre: caso exitoso + al menos 2 casos edge/error
- [x] Los tests NO tocan la base de datos (todo mockeado)
- [x] `dotnet test tests/MedicalCenter.UnitTests` pasa completo
- [x] Nuevos tests corren en < 100ms cada uno (sin IO real)

### ❌ Fuera de alcance
- No modificar `AppointmentsService.cs`
- No crear integration tests para esto (son unit tests)
- No mockear `AppointmentRepository` si usa métodos de extensión no virtuales

---

## T3 — Unit tests: `ConsultationsService`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🔴 **Alta** |
| **Archivo fuente** | `src/MedicalCenter.Application/Features/Consultations/ConsultationsService.cs` |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/Consultations/ConsultationsServiceTests.cs` |
| **Cobertura actual** | ~18% (300 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 2-3 horas |
| **Asignable a** | Persona B |

### Especificación

Misma estructura que T2. Mockear dependencias via NSubstitute. Tests unitarios puros, sin base de datos.

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `CreateAsync` | Consulta válida, paciente no existe, médico no disponible |
| `GetByIdAsync` | Consulta existe, no existe |
| `ListByPatientAsync` | Paciente con consultas, sin consultas |
| `ListByProfessionalAsync` | Profesional con consultas, sin consultas |
| `CancelAsync` | Consulta activa → cancela, ya cancelada → exception |
| `UpdateAsync` | Actualización válida, consulta no encontrada |

### ✅ Criterios de aceptación
- [x] Mismos criterios que T2

---

## T4 — Unit tests: `CatalogsService`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivo fuente** | `src/MedicalCenter.Application/Features/Catalogs/CatalogsService.cs` |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/Catalogs/CatalogsServiceTests.cs` |
| **Cobertura actual** | 0% (165 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 1-2 horas |
| **Asignable a** | Persona C |

### Especificación

CatalogsService suele ser CRUD simple con lógica de búsqueda/filtrado. Tests unitarios.

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `GetAllAsync` | Lista completa, lista vacía, filtro por activos |
| `GetByIdAsync` | Existe, no existe |
| `CreateAsync` | Creación válida, duplicado → exception |
| `UpdateAsync` | Update válido, no encontrado |
| `DeleteAsync` | Eliminación lógica, no encontrado |

### ✅ Criterios de aceptación
- [x] Mismos criterios que T2

---

## T14 — Fix CA1862: `PatientRepository.cs` string comparison

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🔴 **Alta** |
| **Archivo** | `src/MedicalCenter.Infrastructure/Persistence/Repositories/PatientRepository.cs` |
| **Líneas afectadas** | 22, 23, 24, 37, 38, 46, 59, 60, 61, 62 (10 issues) |
| **Severidad** | INFO |
| **Duración estimada** | 1-2 horas |
| **Asignable a** | Cualquiera |

### Especificación

Reemplazar uso de `.ToLower().Contains(...)` o `.ToLower() == ...` por sobrecargas con `StringComparison.OrdinalIgnoreCase` para mejorar performance y legibilidad.

**Patrón actual** (incorrecto):
```csharp
.Where(p => p.FirstName.ToLower().Contains(searchTerm.ToLower()))
```

**Patrón corregido**:
```csharp
.Where(p => p.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
```

### Métodos a modificar

| Método | Líneas | Cambio |
|--------|--------|--------|
| `SearchAsync` | 22-24 | Usar `StringComparison.OrdinalIgnoreCase` |
| `SearchAsync` | 37-38 | Usar `StringComparison.OrdinalIgnoreCase` |
| `SearchAsync` | 46 | Usar `StringComparison.OrdinalIgnoreCase` |
| `SearchAsync` | 59-62 | Usar `StringComparison.OrdinalIgnoreCase` |

### ✅ Criterios de aceptación
- [x] Ningún uso de `.ToLower()` para comparaciones de strings
- [x] Todos los métodos de búsqueda usan `StringComparison.OrdinalIgnoreCase`
- [x] El comportamiento es idéntico (case-insensitive matching)
- [x] CI pasa sin errores

### ❌ Fuera de alcance
- No modificar lógica de negocio
- No agregar nuevos métodos

---

## T14a — Fix CA1862: `UserRepository.cs` string comparison

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🔴 **Alta** |
| **Archivo** | `src/MedicalCenter.Infrastructure/Persistence/Repositories/UserRepository.cs` |
| **Líneas afectadas** | 16, 23 (2 issues) |
| **Severidad** | INFO |
| **Duración estimada** | 30 minutos |
| **Asignable a** | Cualquiera |

### Especificación

Mismo patrón que T14, aplicado a UserRepository.

**Patrón actual** (incorrecto):
```csharp
.Where(u => u.Email.ToLower() == email.ToLower())
```

**Patrón corregido**:
```csharp
.Where(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
```

### ✅ Criterios de aceptación
- [x] Ningún uso de `.ToLower()` para comparaciones de strings
- [x] CI pasa sin errores

---

## T15 — Fix S1172: Unused parameter in `ConfigGenerator.cs`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🔴 **Alta** |
| **Archivo** | `src/MedicalCenter.Cli/Generators/ConfigGenerator.cs` |
| **Línea afectada** | 185 |
| **Severidad** | MAJOR |
| **Duración estimada** | 5 minutos |
| **Asignable a** | Cualquiera |

### Especificación

El método `MergeAppSettings` tiene un parámetro `path` que no se utiliza.

**Patrón actual** (incorrecto):
```csharp
private static void MergeAppSettings(string path, string environment)
{
    // 'path' no se usa en el método
}
```

**Opción A - Remover parámetro** (si no se usa en ningún llamador):
```csharp
private static void MergeAppSettings(string environment)
```

**Opción B - Usar el parámetro** (si debería usarse):
Revisar si el path debería usarse para cargar configuración.

### ✅ Criterios de aceptación
- [x] Parámetro no utilizado removido o utilizado correctamente
- [x] Todos los llamadores actualizados
- [x] CI pasa sin errores

---

## T16 — Fix S4136: Non-adjacent overloads in `AppointmentResponseMappings.cs`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivo** | `src/MedicalCenter.Api/Mappings/AppointmentResponseMappings.cs` |
| **Líneas afectadas** | 10, 119 |
| **Severidad** | MINOR |
| **Duración estimada** | 1 minuto |
| **Asignable a** | Cualquiera |

### Especificación

Las sobrecargas del método `ToResponse` deben estar agrupadas juntas según la convención S4136.

**Patrón actual** (incorrecto):
```csharp
// Línea 10
public static AppointmentResponse ToResponse(this Appointment appointment) { ... }

// ... otros métodos ...

// Línea 119
public static AppointmentResponse ToResponse(this Appointment appointment, Patient patient) { ... }
```

**Patrón corregido**:
Mover ambas sobrecargas de `ToResponse` para que estén adyacentes en el archivo.

### ✅ Criterios de aceptación
- [x] Todas las sobrecargas de `ToResponse` están agrupadas
- [x] CI pasa sin errores

---

## T5 — Unit tests: `PatientsService`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivo fuente** | `src/MedicalCenter.Application/Features/Patients/PatientsService.cs` |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/Patients/PatientsServiceTests.cs` |
| **Cobertura actual** | 0% (137 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 1-2 horas |
| **Asignable a** | Persona C |

### Especificación

CRUD de pacientes con búsqueda. Tests unitarios.

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `RegisterAsync` | Registro válido, duplicado (documento), email inválido |
| `GetByIdAsync` | Existe, no existe |
| `SearchAsync` | Por nombre, por documento, por email, sin resultados |
| `UpdateAsync` | Update válido, no encontrado |
| `DeactivateAsync` | Desactivación, ya inactivo |

### ✅ Criterios de aceptación
- [x] Mismos criterios que T2

---

## T6 — Unit tests: `ProfessionalsService`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivo fuente** | `src/MedicalCenter.Application/Features/Professionals/ProfessionalsService.cs` |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/Professionals/ProfessionalsServiceTests.cs` |
| **Cobertura actual** | 0% (102 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 1 hora |
| **Asignable a** | Persona D |

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `GetAllAsync` | Con filtros, sin filtros, vacío |
| `GetByIdAsync` | Existe, no existe |
| `GetAvailableBySpecialtyAsync` | Disponibles, ninguno disponible |

### ✅ Criterios de aceptación
- [x] Mismos criterios que T2

---

## T7 — Unit tests: `ClinicalHistoryService`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivo fuente** | `src/MedicalCenter.Application/Features/ClinicalHistory/ClinicalHistoryService.cs` |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/ClinicalHistory/ClinicalHistoryServiceTests.cs` |
| **Cobertura actual** | 0% (98 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 1 hora |
| **Asignable a** | Persona D |

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `GetByPatientAsync` | Paciente con historial, sin historial |
| `AddEntryAsync` | Entrada válida, paciente no existe |
| `GetEntryByIdAsync` | Existe, no existe |

### ✅ Criterios de aceptación
- [x] Mismos criterios que T2

---

## T8 — Unit tests: `OutOfHoursTurnsService`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivo fuente** | `src/MedicalCenter.Application/Features/OutOfHoursTurns/OutOfHoursTurnsService.cs` |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/OutOfHoursTurns/OutOfHoursTurnsServiceTests.cs` |
| **Cobertura actual** | 0% (125 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 1 hora |
| **Asignable a** | Persona E |

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `CreateOutOfHoursTurnAsync` | Turno válido, fuera del horario permitido, duplicado |
| `GetByDateAsync` | Fecha con turnos, sin turnos |
| `ApproveAsync` | Aprobación válida, turno no encontrado |

### ✅ Criterios de aceptación
- [x] Mismos criterios que T2

---

## T9 — Integration tests: `AppointmentRepository`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🔴 **Alta** |
| **Archivo fuente** | `src/MedicalCenter.Infrastructure/Persistence/Repositories/AppointmentRepository.cs` |
| **Archivo test** | `tests/MedicalCenter.IntegrationTests/Persistence/AppointmentRepositoryTests.cs` |
| **Cobertura actual** | ~18% (130 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 2-3 horas |
| **Asignable a** | Persona F |
| **Nota** | Usa PostgreSQL vía Testcontainers (ya configurado) |

### Especificación

Agregar tests de integración contra PostgreSQL real (Testcontainers). Los tests deben crear datos de prueba via EF Core directamente y luego ejecutar los métodos del repositorio.

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `GetByIdAsync` | Turno existe, no existe, con relaciones (paciente, médico) |
| `GetByPatientIdAsync` | Paciente con turnos, sin turnos, múltiples turnos |
| `GetByProfessionalIdAndDateAsync` | Profesional con turnos en fecha, sin turnos |
| `GetOverlappingAsync` | Superposición exacta, parcial, sin superposición |
| `AddAsync` | Creación exitosa, verificar que persiste |
| `UpdateAsync` | Update y verify cambios en DB |
| `DeleteAsync` | Soft delete, verify en DB |

### Estructura del archivo test

```csharp
namespace MedicalCenter.IntegrationTests.Persistence;

public sealed class AppointmentRepositoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly MedicalCenterDbContext _dbContext;

    public AppointmentRepositoryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        var options = new DbContextOptionsBuilder<MedicalCenterDbContext>()
            .UseNpgsql(factory.ConnectionString)
            .Options;
        _dbContext = new MedicalCenterDbContext(options);
    }

    private async Task SeedAppointmentAsync(Appointment appointment)
    {
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingAppointment_ReturnsAppointment()
    {
        // Arrange - seed data
        var appointment = new Appointment { /* ... */ };
        await SeedAppointmentAsync(appointment);
        var repo = new AppointmentRepository(_dbContext);

        // Act
        var result = await repo.GetByIdAsync(appointment.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(appointment.Id, result.Id);
    }
}
```

### ✅ Criterios de aceptación
- [x] Tests crean datos reales en PostgreSQL y verifican persistencia
- [x] Cada método del repositorio tiene al menos 2 escenarios (éxito + no encontrado)
- [x] Tests usan `CustomWebApplicationFactory.ConnectionString`
- [x] No se mockea el DbContext (es integration test real)
- [x] `dotnet test tests/MedicalCenter.IntegrationTests` pasa completo
- [x] Limpieza de datos entre tests (cada test crea sus propios datos)

### ❌ Fuera de alcance
- No probar configuraciones de EF Core (fluent API)
- No probar migrations (ya excluidas de Sonar)

---

## T10 — Integration tests: Auth/Login

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivos fuente** | `AuthService.cs`, `UserRepository.cs`, `PatientRepository.cs` |
| **Archivo test** | `tests/MedicalCenter.IntegrationTests/Api/E2E/SecurityE2ETests.cs` (ya existe, ampliar) |
| **Cobertura actual** | ~16% en AuthService (299 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 1-2 horas |
| **Asignable a** | Persona F |

### Escenarios a agregar

| Escenario | Endpoint | Assert |
|-----------|----------|--------|
| Login con credentials válidas | `POST /api/v1/auth/login` | `200 OK` + `access_token` + `refresh_token` |
| Login con email en mayúsculas | `POST /api/v1/auth/login` | `200 OK` (case insensitive) |
| Login con usuario inactivo | `POST /api/v1/auth/login` | `401 Unauthorized` |
| Refresh con token válido | `POST /api/v1/auth/refresh` | `200 OK` |
| Refresh con token expirado | `POST /api/v1/auth/refresh` | `401 Unauthorized` |
| Logout con token válido | `POST /api/v1/auth/logout` | `204 No Content` |

### ✅ Criterios de aceptación
- [x] Tests crean usuario via repositorio y luego llaman endpoints reales
- [x] Verifican tokens JWT emitidos correctamente
- [x] `dotnet test --filter SecurityE2ETests` pasa completo

---

## T11 — Integration tests: AppointmentsController

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟡 **Media** |
| **Archivo fuente** | `src/MedicalCenter.Api/Controllers/V1/AppointmentsController.cs` |
| **Archivo test** | `tests/MedicalCenter.IntegrationTests/Api/V1/AppointmentsControllerTests.cs` |
| **Cobertura actual** | 0% (260 líneas) |
| **Cobertura objetivo** | 80% |
| **Duración estimada** | 2-3 horas |
| **Asignable a** | Persona G |

### Especificación

Tests E2E de API contra la aplicación real (WebApplicationFactory) + PostgreSQL (Testcontainers). Los tests deben autenticarse con un token JWT real.

### Setup

```csharp
public sealed class AppointmentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private async Task<string> GetAuthTokenAsync()
    {
        // Crear usuario + login, devolver token JWT
    }
}
```

### Endpoints a cubrir

| Método | Endpoint | Escenarios |
|--------|----------|-----------|
| `GET` | `/api/v1/appointments/{id}` | - Turno existe (autenticado)<br>- No autenticado → 401<br>- No existe → 404 |
| `GET` | `/api/v1/appointments?patientId=` | - Paciente con turnos<br>- Paciente sin turnos |
| `POST` | `/api/v1/appointments` | - Creación válida<br>- Datos inválidos → 400<br>- Superposición → 409 |
| `PUT` | `/api/v1/appointments/{id}` | - Update válido<br>- No encontrado → 404 |
| `DELETE` | `/api/v1/appointments/{id}` | - Cancelación válida<br>- Ya cancelado → 409 |

### ✅ Criterios de aceptación
- [x] Tests autenticados (token JWT en header `Authorization: Bearer`)
- [x] Cada endpoint: éxito + al menos 1 caso error
- [x] `dotnet test --filter AppointmentsController` pasa completo

---

## T12 — Unit tests: `WhatsappService` + `WhatsappWebhookProcessor`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟢 **Baja** (depende de API externa) |
| **Archivo fuente** | `WhatsappService.cs` (378 líneas) + `WhatsappWebhookProcessor.cs` (351 líneas) |
| **Archivo test** | `tests/MedicalCenter.UnitTests/Features/WhatsApp/WhatsappServiceTests.cs` |
| **Cobertura actual** | 0% (~729 líneas total) |
| **Cobertura objetivo** | 60% (realista por integración externa) |
| **Duración estimada** | 3-4 horas |
| **Asignable a** | Persona H |

### Especificación

Servicios de WhatsApp dependen de API externa (Meta/WhatsApp Cloud API). Tests unitarios con HTTP message handlers mockeados.

### Claves técnicas

- Usar `HttpMessageHandler` mockeado para evitar llamadas reales a Meta API
- Separar lógica de negocio de la comunicación HTTP

### Métodos a cubrir

| Servicio | Método | Escenarios |
|----------|--------|-----------|
| `WhatsappService` | `SendMessageAsync` | Envío exitoso, error 4xx, timeout |
| `WhatsappService` | `SendTemplateAsync` | Template válido, template no existe |
| `WhatsappWebhookProcessor` | `ProcessIncomingAsync` | Mensaje de texto, imagen, ubicación |
| `WhatsappWebhookProcessor` | `VerifyWebhookAsync` | Challenge válido, token inválido |

### ✅ Criterios de aceptación
- [x] Tests usan `DelegatingHandler` falso para simular HTTP
- [x] No hay llamadas reales a WhatsApp API
- [x] `dotnet test --filter Whatsapp` pasa completo

---

## T13 — Unit tests: `RbacAdminRepository`

| Campo | Valor |
|-------|-------|
| **Prioridad** | 🟢 **Baja** (lógica de RBAC compleja pero estable) |
| **Archivo fuente** | `src/MedicalCenter.Infrastructure/Persistence/Repositories/RbacAdminRepository.cs` |
| **Archivo test** | `tests/MedicalCenter.IntegrationTests/Persistence/RbacAdminRepositoryTests.cs` |
| **Cobertura actual** | ~0.6% (498 líneas) |
| **Cobertura objetivo** | 70% |
| **Duración estimada** | 3-4 horas |
| **Asignable a** | Persona I |
| **Nota** | Requiere integration test (usa SQL directo + relaciones complejas) |

### Especificación

Usa SQL directo (no EF Core queries simples). Requiere integration tests con PostgreSQL real.

### Métodos a cubrir

| Método | Escenarios |
|--------|-----------|
| `GetUserRolesAsync` | Usuario con roles, sin roles, inactivo |
| `AssignRoleAsync` | Asignación válida, rol duplicado |
| `RemoveRoleAsync` | Remoción válida, rol no asignado |
| `GetPermissionsForRoleAsync` | Rol con permisos, sin permisos |
| `GrantPermissionAsync` | Grant válido, permiso ya otorgado |
| `RevokePermissionAsync` | Revoke válido, permiso no otorgado |

### ✅ Criterios de aceptación
- [x] Tests con PostgreSQL real (Testcontainers)
- [x] Seed de datos de prueba con roles y permisos
- [x] `dotnet test --filter RbacAdminRepository` pasa completo

---

## 📊 Estado auditado del plan (repositorio actual)

### Estado por sectores

#### ✅ Tareas completadas (15)

`T1`, `T2`, `T3`, `T4`, `T5`, `T6`, `T8`, `T9`, `T10`, `T11`, `T12`, `T13`, `T14`, `T14a`, `T16`

#### ⚠️ Tareas parciales (1)

| Tarea | Situacion actual | Falta para cerrar |
|------|------------------|-------------------|
| `T7` | Tests presentes, cobertura parcial validada | Confirmar evidencia de cobertura objetivo de `ClinicalHistoryService` |

#### ❗ Pendientes de cierre global (1)

| Item | Situacion actual | Falta para cerrar |
|------|------------------|-------------------|
| `Final` | Hay mejora sostenida, pero no cierre definitivo | Ejecutar CI/Sonar y confirmar quality gate al final de estabilizacion |

#### 🟡 Tareas obsoletas/desfasadas (1)

| Tarea | Motivo |
|------|--------|
| `T15` | El archivo objetivo `src/MedicalCenter.Cli/Generators/ConfigGenerator.cs` no existe en el repo actual |

### Que falta para cerrar

| Item | Falta concreta | Prioridad |
|------|----------------|-----------|
| **T7** | Confirmar cobertura objetivo del servicio (`ClinicalHistoryService`) con evidencia de cobertura | Alta |
| **Issues Sonar abiertos** | Resolver 6 code smells INFO en tests (`CA1822` x5, `CA1859` x1) | Alta |
| **Final** | Ejecutar CI/Sonar y confirmar quality gate al cierre de estabilizacion | Alta |

### Tareas completadas con desvio (referencia)

| Tarea | Estado | Motivo del desvio |
|------|--------|-------------------|
| **T1** | Verified with Deviation | Exclusion removida intencionalmente para medir cobertura real |
| **T2** | Verified with Deviation | Cobertura implementada en varios archivos de tests |
| **T6** | Verified with Deviation | Servicio actual evoluciono respecto al plan original |
| **T9** | Verified with Deviation | Escenarios de integracion adaptados al contrato actual |
| **T11** | Verified with Deviation | Endpoints cubiertos segun API actual |
| **T12** | Verified with Deviation | Queda 1 issue INFO (`CA1859`) en tests |
| **T13** | Verified with Deviation | Quedan 5 issues INFO (`CA1822`) en tests |

### Próxima actualización recomendada

- En cada cierre de ciclo, adjuntar referencia de run de CI y resultado de SonarCloud para actualizar el estado de **Final (Quality Gate)**.
- Mantener `T1` como desvío temporal mientras siga activa la medición de cobertura real sin exclusión de `DatabaseInitializer.cs`.
- Priorizar cleanup de los 6 issues INFO actuales en tests (`CA1822` y `CA1859`) como quick wins antes del cierre de estabilización.
