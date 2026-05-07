# Exploration: T10 - Integration Tests Auth/Login

## Current State

### Auth Architecture Overview
- **AuthService** (`src/MedicalCenter.Application/Features/Auth/AuthService.cs`): 570 líneas, maneja LoginAsync, RefreshAsync, LogoutAsync, ChangeOwnPasswordAsync, y operaciones de portal
- **AuthController** (`src/MedicalCenter.Api/Controllers/Auth/AuthController.cs`): Endpoints en `/api/v1/auth/*`
- **JWT Config** (appsettings.json):
  - Issuer: `MedicalCenter`
  - Audience: `MedicalCenter.Client`
  - SecretKey: 32+ chars
  - AccessTokenExpiration: 30 minutos
  - RefreshTokenExpiration: 7 días

### Affected Areas
- `src/MedicalCenter.Application/Features/Auth/AuthService.cs` — Core logic de autenticación
- `src/MedicalCenter.Api/Controllers/Auth/AuthController.cs` — API endpoints
- `src/MedicalCenter.Infrastructure/Persistence/Repositories/UserRepository.cs` — Repository con queries SQL complejas (ILike para case-insensitive)
- `src/MedicalCenter.Domain/Entities/User.cs` — Entidad con UserCreateParams record
- `tests/MedicalCenter.IntegrationTests/Api/E2E/SecurityE2ETests.cs` — Tests existentes (ampliar)

### Current Test Coverage (SecurityE2ETests.cs)
| Test | Estado |
|------|--------|
| HealthEndpoint_ContainsSecurityHeaders | ✅ Existe |
| ProtectedEndpoint_WithoutAuth_Returns401 | ✅ Existe |
| Login_WithInvalidCredentials_Returns401 | ✅ Existe |
| Login_WithEmptyBody_Returns400 | ✅ Existe |

### Tests NOT Covered (T10 Scenarios)
| Escenario | Assert | Status |
|-----------|--------|--------|
| Login con credentials válidas | `200 OK` + `access_token` + `refresh_token` | ❌ Faltante |
| Login con email en mayúsculas | `200 OK` (case insensitive) | ❌ Faltante |
| Login con usuario inactivo | `401 Unauthorized` | ❌ Faltante |
| Refresh con token válido | `200 OK` | ❌ Faltante |
| Refresh con token expirado | `401 Unauthorized` | ❌ Faltante |
| Logout con token válido | `204 No Content` | ❌ Faltante |

---

## Test Infrastructure Analysis

### CustomWebApplicationFactory
- ✅ Usa **Testcontainers.PostgreSql** para PostgreSQL real
- ✅ Configura JWT SecretKey: `this-is-a-32-char-long-secret-key!`
- ✅ Crea database schema con `EnsureCreatedAsync()`
- ✅ Expone `ConnectionString` para usar en tests de repositorio

### Password Hashing
- Interfaz: `IPasswordHasher` (Hash/Verify)
- Implementación: `Pbkdf2PasswordHasher` (PBKDF2)
- **Challenge**: En integration tests, se necesita acceso al IPasswordHasher para hashear passwords de test seed

### User Entity
```csharp
public sealed record UserCreateParams(
    Guid Id,
    string Identifier,  // case-insensitive via ILike
    string Email,        // case-insensitive via ILike
    string PasswordHash,
    bool IsActive,
    bool IsStaff,
    Guid? PatientId = null,
    string? Nombre = null);
```

### Role Entity
- Roles se cargan desde PostgreSQL vía SQL complejo en UserRepository
- Los tests necesitan roles en la DB para autenticación exitosa
- Mapeo: `perfiles` → `rbac_user_roles` → `rbac_roles` → `rbac_permissions`

---

## Approaches

### Approach A: Direct Seed via EF Core (Recomendado)
**Descripción**: Crear usuarios directamente en el DbContext de test usando IPasswordHasher del container DI.

- **Pros**:
  - Flexible, permite crear cualquier estado de test
  - Pattern usado en AppointmentRepositoryTests existente
  - No necesita mock复杂的 role loading
- **Cons**:
  - Requiere resolver IPasswordHasher del service provider
  - Roles necesitan seed en múltiples tablas (perfiles, rbac_user_roles, rbac_roles)
- **Effort**: Medium

### Approach B: Use Existing Seed Data
**Descripción**: Usar usuarios existentes del DatabaseInitializer.

- **Pros**:
  - No necesita crear usuarios nuevos
  - Admin ya existe en seed
- **Cons**:
  - Dificultad para testing de edge cases (inactive user, case sensitivity)
  - Tests no son independientes entre sí
  - Dependencia de estado externo
- **Effort**: Low (pero limitado)

### Approach C: Separate Test Helper con Service Scope
**Descripción**: Crear un TestAuthHelper que use IServiceScopeFactory para obtener servicios.

- **Pros**:
  - Acceso completo al DI container
  - Permite usar AuthService real para crear usuarios de test
  - Pattern más limpio
- **Cons**:
  - Más código boilerplate
  - Introduce dependencias adicionales
- **Effort**: Medium-High

---

## Recommendation

**Approach A** con refinamiento: Usar DbContext directo para seed, pero crear helper que:
1. Use el `ConnectionString` de CustomWebApplicationFactory
2. Cree usuarios con password hasheado usando IPasswordHasher resueltodesde el service provider del factory
3. Cree roles básicos en las tablas de RBAC necesarias

**Justificación**:
- El pattern de AppointmentRepositoryTests ya usa DbContext directo
- La complejidad del role loading en UserRepository requiere seed completo en DB
- Mayor control sobre estados de test (active/inactive, case variations)

### Password Hashing Strategy
```csharp
// En el test fixture:
var services = _factory.Services;
var passwordHasher = services.GetRequiredService<IPasswordHasher>();
var passwordHash = passwordHasher.Hash("test-password-123");
```

### Test Data Structure Needed
| Tabla | Datos Requeridos |
|-------|------------------|
| `users` | User con Identifier, Email, PasswordHash, IsActive/IsStaff |
| `perfiles` | Profile linked a auth_user_id |
| `rbac_roles` | Rol activo con slug (ej: "admin", "staff") |
| `rbac_user_roles` | Unión usuario-rol con expires_at null |

---

## Risks

1. **Role Loading Complexity**: El SQL en UserRepository.LoadRolesAsync es complejo y requiere múltiples tablas. Tests fallarán silenciosamente si no se seeds correctamente.
2. **Password Hashing State**: El password hasher usa random salt — cada hash es único. Los tests deben usar el mismo hasher instance para hash + verify.
3. **Case Insensitivity**: PostgreSQL con ILike es case-insensitive, pero el test debe verificar explícitamente el comportamiento con mayúsculas.
4. **Refresh Token Expiration**: Testing con token expirado requiere manipulación de tiempo (freeze clock o crear token con fecha old).

---

## Ready for Proposal

**Yes** — La investigación es suficiente para crear el proposal.

### Required Artifacts from Orchestrator
- Change name: `t10-auth-integration-tests`
- Scope: Ampliar SecurityE2ETests.cs con 6 scenarios nuevos
- Approach: Approach A (Direct seed via EF Core)
- Delivery: Single PR (estimado ~150 líneas de test)

### Next Phase
- **sdd-propose**: Crear proposal con scope, approach, y criterios de aceptación