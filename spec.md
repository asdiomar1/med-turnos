# Spec: Fix SonarQube Maintainability Issues

## Constraints
- No functional changes.
- Existing tests MUST pass.
- Migration file MUST use `#pragma warning disable CA1861` only.
- Conventional commits; no AI attribution.

## Batch 1 — INFO Mechanical Fixes (~74 issues)

| Rule | Fix Pattern |
|------|-------------|
| **CA1861** | Extract inline arrays to `static readonly` fields. Skip migration file (Batch 4). |
| **CA1862** | Replace `.ToLower().Equals()` / `.ToLower().Contains()` with `string.Equals(..., StringComparison.OrdinalIgnoreCase)` / `.Contains(..., StringComparison.OrdinalIgnoreCase)`. |
| **CA1859** | Widen parameter/return types from interface to concrete type (e.g., `IReadOnlyCollection<T>` -> `List<T>`, `Exception` -> `InvalidOperationException`). |
| **CA1822** | Mark instance methods that don't use instance data as `static`. |
| **SYSLIB1045** | Replace `new Regex(...)` with `[GeneratedRegex(...)]` on a partial method in a partial class. |
| **CA1866** | Replace `EndsWith("x")` / `StartsWith("x")` with `EndsWith('x')` / `StartsWith('x')`. |
| **CA2254** | Use structured logging: `logger.LogWarning("Template {Value}", value)` instead of string interpolation. |
| **CA1869** | Cache `JsonSerializerOptions` in a `static readonly` field. |
| **CA1816** | Call `GC.SuppressFinalize(this)` in `Dispose()` implementations. |
| **CA1806** | Use `TryParse` return value or check the `out` parameter. |
| **CA1068** | Reorder `CancellationToken` parameter to be last. |
| **CA1512** | Use `ArgumentOutOfRangeException.ThrowIfLessThan(...)` instead of manual throw. |
| **ASP0025** | Add `ProducesResponseType` attributes for documented status codes. |

### File Inventory
- `src/Infrastructure/Persistence/Repositories/*Repository.cs` (CA1862)
- `src/Application/Features/**/*Service.cs` (CA1859, CA1822)
- `src/Api/Extensions/ApiServiceExtensions.cs` (CA2254)
- `tools/Launcher/**/*` (CA1861, CA1869, CA1806, CA1068, SYSLIB1045, CA1866)
- `tests/**/*Tests.cs` (CA1816, CA1859, CA1861)
- `src/Application/Features/Imports/ImportPatientsOrchestrator.cs` (SYSLIB1045)
- `src/Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` (ASP0025)
- `src/Infrastructure/Auth/JwtTokenService.cs` (CA1512)

### Scenarios
- **GIVEN** a CA1862 violation in a repository, **WHEN** the fix is applied, **THEN** the code uses `StringComparison.OrdinalIgnoreCase` and SonarQube marks it resolved.
- **GIVEN** a CA1859 method returning an interface, **WHEN** the return type is narrowed to the concrete type, **THEN** callers compile without changes and tests pass.

### Verification
- `dotnet build` passes.
- xUnit tests pass.
- SonarQube count for Batch 1 rules = 0 (excluding migration CA1861).

## Batch 2 — MINOR Mechanical Fixes (~24 issues)

| Rule | Fix Pattern |
|------|-------------|
| **S6667** | Pass the caught exception as a parameter to the logger call: `_logger.LogError(ex, "msg")`. |
| **S6610** | Use `char` overload for `EndsWith`/`StartsWith` when the argument is a single character. |
| **S1172** | Remove unused method parameter or suppress with `#pragma` if part of an interface contract. |
| **S1192** (selected) | Extract repeated string literals in application/service files to `private const string` fields. |
| **S2325** | Mark methods as `static` when they do not access instance state. |
| **S6608** | Replace `.First()` on arrays/lists with `[0]` indexing. |
| **S3267** | Simplify LINQ `.Where(...).Any()` to `.Any(...)`. |
| **S3376** | Move nested exception classes to namespace scope or rename to avoid confusion. |
| **S1075** | Extract hardcoded URIs to configurable constants. |
| **S1144** | Remove unused private methods/ctors or mark with `[Obsolete]` if reflection-used. |
| **S1481** | Remove unused local variables. |
| **S1643** | Use `StringBuilder` or string interpolation instead of chained `+=` in loops. |
| **S2094** | Remove empty marker classes or add a comment justifying their existence. |
| **S4136** | Group method overloads together in the same class. |

### File Inventory
- `src/Infrastructure/Persistence/Repositories/AppointmentRepository.cs` (S6667)
- `src/Infrastructure/Seed/DatabaseInitializer.cs` (S6610)
- `src/Application/Features/Appointments/AppointmentsService.cs` (S1172, S2325, S6608, S3267)
- `src/Application/Features/Consultations/ConsultationsService.cs` (S2325)
- `src/Application/Features/WhatsApp/WhatsappService.cs` (S2325)
- `src/Application/Exceptions/ApplicationExceptionBase.cs` (S3376)
- `tools/MedicalCenter.Launcher/Checks/MigrationStatusChecker.cs` (S6610)
- `tools/MedicalCenter.Launcher/Services/HealthWaiter.cs` (S1075, S1806)
- `tools/MedicalCenter.Launcher/Services/DockerComposeRunner.cs` (S1643)
- `tools/MedicalCenter.Launcher/Config/ConfigGenerator.cs` (S1481)
- `src/MedicalCenter.Api/Extensions/ApiServiceExtensions.cs` (S2094)
- `src/MedicalCenter.Api/Mappings/AppointmentResponseMappings.cs`, `SimpleResponseMappings.cs` (S4136)
- `src/MedicalCenter.Domain/Entities/Appointment.cs` (S1144)
- `src/MedicalCenter.Infrastructure/Persistence/Repositories/RbacAdminRepository.cs` (S1144, S1481)
- `src/MedicalCenter.Infrastructure/WhatsApp/WhatsAppSender.cs` (S1144)

### Verification
- Build and tests pass.
- SonarQube count for Batch 2 rules = 0.

## Batch 3 — DatabaseInitializer Constants (31 S1192)

### Requirement
All repeated permission key strings and module name strings inside `DatabaseInitializer.cs` MUST be replaced with references to a new `PermissionConstants` static class.

### File Inventory
- **New**: `src/MedicalCenter.Domain/Constants/PermissionConstants.cs`
- **Modified**: `src/MedicalCenter.Infrastructure/Seed/DatabaseInitializer.cs`

### Scenarios
- **GIVEN** the string `"portal.access"` appears 3 times in `DatabaseInitializer.cs`, **WHEN** the constant `PermissionConstants.PortalAccess` is introduced, **THEN** all occurrences reference the constant.
- **GIVEN** the module string `"turnos"` appears 10+ times, **WHEN** `PermissionConstants.Module.Turnos` is introduced, **THEN** all occurrences reference the constant.

### Verification
- Build and tests pass.
- SonarQube S1192 count in `DatabaseInitializer.cs` = 0.

## Batch 4 — Migration File Pragma (26 CA1861)

### Requirement
The `InitialCreate.cs` migration file MUST suppress CA1861 at file level via `#pragma warning disable CA1861` placed immediately after the namespace declaration and re-enabled at EOF with `#pragma warning restore CA1861`. No migration logic SHALL be changed.

### File Inventory
- `src/MedicalCenter.Infrastructure/Persistence/Migrations/20260430180403_InitialCreate.cs`

### Verification
- Build passes.
- Migration file diff shows only pragma additions.

## Batch 5 — Structural MAJOR S107 (~20 issues)

### Requirement
Domain entity constructors and application service method signatures with >7 parameters MUST introduce parameter DTOs/records. All existing call sites MUST be updated atomically in the same commit. No property assignment logic SHALL change.

### Affected Entities & Patterns

| Entity / Service | Record Name | Parameter Count |
|------------------|-------------|-----------------|
| `AdminEventFeedEntry` | `AdminEventFeedEntryParams` | 18 |
| `BlockHistory` | `BlockHistoryParams` | 22 |
| `ClinicalHistory` | `ClinicalHistoryParams` | 8 |
| `ConsultationSession` | `ConsultationSessionParams` | 12 |
| `OutOfHoursTurn` | `OutOfHoursTurnParams` | 12 |
| `Role` | `RoleParams` | 9 |
| `User` | `UserParams` | 8 |
| `WhatsappDispatchQueueItem` | `WhatsappDispatchQueueItemParams` | 9 |
| `WhatsappMessage` | `WhatsappMessageParams` | 10 |
| `WhatsappMessageAction` | `WhatsappMessageActionParams` | 8 |
| `WhatsappTemplate` | `WhatsappTemplateParams` | 8 |
| `AppointmentsService` ctor | `AppointmentsServiceDependencies` | 10 |
| `AuthService` ctor | `AuthServiceDependencies` | 9 |
| `CatalogsService` method | `CatalogFilterParams` | 8 |
| `IAdminEventFeedRepository` method | `AdminEventFeedQueryParams` | 8 |
| `IAdminEventFeedService` method | `AdminEventFeedQueryParams` | 8 |
| `IPatientsService.CreateAsync` | `CreatePatientParams` | 17 |
| `IPatientsService.UpdateAsync` | `UpdatePatientParams` | 16 |
| `PatientsService` method | `PatientSearchParams` | 8 |
| `IClinicalHistoryService` method | `ClinicalHistoryQueryParams` | 11 |

### Example (Before -> After)
```csharp
// Before
public User(Guid id, string identifier, string email, string passwordHash,
            bool isActive, bool isStaff, Guid? patientId = null, string? nombre = null)

// After
public record UserParams(Guid Id, string Identifier, string Email, string PasswordHash,
                         bool IsActive, bool IsStaff, Guid? PatientId = null, string? Nombre = null);
public User(UserParams p) { Id = p.Id; Identifier = p.Identifier; ... }
```

### File Inventory
- `src/MedicalCenter.Domain/Entities/*.cs` (9 entities)
- `src/MedicalCenter.Application/Features/**/*.cs` (service interfaces + implementations)
- `src/MedicalCenter.Application/Abstractions/Persistence/*.cs` (repository interfaces)
- All test files that instantiate affected entities or call affected methods.

### Verification
- Build passes.
- All xUnit tests pass.
- SonarQube S107 count = 0.

## Batch 6 — API Structural (~7 issues)

### Requirement
- **S6932**: Controllers reading `Request.Headers["Idempotency-Key"]` directly MUST use a custom model binder or `[FromHeader(Name = "Idempotency-Key")]` binding.
- **S6960**: `ConfigurationController` MUST be split into `WorkingDaysConfigController`, `WhatsappMessageSettingsController`, and `CamposConfigController`. `WhatsAppWebhookController` MUST be split into `WhatsAppWebhookReceiveController` and `WhatsAppWebhookStatusController`. Routes MUST remain identical.
- **S4144**: `DailyClosingsController` MUST deduplicate the method implementation identical to `Detail` by extracting a shared private method or calling the existing one.

### File Inventory
- **Modified**: `AppointmentsController.cs`, `ConsultationsController.cs`, `OutOfHoursTurnsController.cs`, `PortalAppointmentsController.cs`, `DailyClosingsController.cs`
- **New**: `WorkingDaysConfigController.cs`, `WhatsappMessageSettingsController.cs`, `CamposConfigController.cs`, `WhatsAppWebhookReceiveController.cs`, `WhatsAppWebhookStatusController.cs`
- **Deleted**: `ConfigurationController.cs`, `WhatsAppWebhookController.cs` (after content migrated)
- **New**: `IdempotencyKeyModelBinder.cs` (or inline `[FromHeader]` attribute changes)

### Verification
- Build passes; Swagger JSON diff shows zero route changes.
- All xUnit tests pass.
- SonarQube S6932, S6960, S4144 counts = 0.
