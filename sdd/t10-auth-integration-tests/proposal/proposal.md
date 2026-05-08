# Proposal: T10 - Integration Tests Auth/Login

## Intent

Agregar 6 integration tests E2E para endpoints de autenticación (login, refresh, logout) según COVERAGE_IMPROVEMENT_PLAN.md, alcanzando 80% de cobertura en AuthService (299 líneas). Los tests validan comportamiento real contra PostgreSQL sin mocks.

## Scope

### In Scope
- 6 escenarios de Auth endpoints en `SecurityE2ETests.cs`
- Seed de usuarios, roles y refresh tokens via EF Core
- Validación de tokens JWT emitidos correctamente

### Out of Scope
- Tests de cambio de password (ya existen en unit tests)
- Tests de portal activation/recovery
- Tests de rate limiting

## Capabilities

### New Capabilities
- `auth-integration-tests`: 6 scenarios E2E para AuthService con PostgreSQL real

### Modified Capabilities
- `security-e2e-tests`: Ampliar existentes con 6 nuevos tests de autenticación

## Approach

**Direct Seed via EF Core** — Crear entidades directamente en la DB de test:

1. Resolver `IPasswordHasher` del service provider del factory
2. Seed de usuarios en tabla `Users` con password hasheado
3. Seed de roles y permisos en tablas RBAC (`perfiles`, `rbac_roles`, `rbac_user_roles`)
4. Seed de refresh tokens en tabla `RefreshTokens`
5. Llamar endpoints reales con `HttpClient` del factory
6. Para token expirado: crear refresh token con `ExpiresAt` en pasado

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `tests/MedicalCenter.IntegrationTests/Api/E2E/SecurityE2ETests.cs` | Modified | Agregar 6 nuevos tests E2E |

## Scenarios to Implement

| # | Escenario | Endpoint | HTTP | Assert |
|---|-----------|----------|------|--------|
| 1 | Login con credentials válidas | `/api/v1/auth/login` | POST | 200 OK + access_token + refresh_token |
| 2 | Login con email en mayúsculas | `/api/v1/auth/login` | POST | 200 OK (case insensitive) |
| 3 | Login con usuario inactivo | `/api/v1/auth/login` | POST | 401 Unauthorized |
| 4 | Refresh con token válido | `/api/v1/auth/refresh` | POST | 200 OK |
| 5 | Refresh con token expirado | `/api/v1/auth/refresh` | POST | 401 Unauthorized |
| 6 | Logout con token válido | `/api/v1/auth/logout` | POST | 204 No Content |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Role loading requiere seed RBAC completo | Medium | Seed de User + Roles + UserRoles + RolePermissions |
| Password hashing con PBKDF2 necesita instanciar hasher | Low | Resolver IPasswordHasher del factory.Services |
| Token expirado necesita manipulación de tiempo | Medium | Crear refresh token con ExpiresAt en pasado |

## Rollback Plan

1. Revertir cambios en `SecurityE2ETests.cs`
2. Ejecutar `dotnet test --filter SecurityE2ETests` para verificar tests existentes
3. Verificar que cobertura de SonarCloud no bajó

## Dependencies

- `CustomWebApplicationFactory` con Testcontainers.PostgreSql configurado
- `IPasswordHasher` registrado en DI container
- Tablas RBAC existentes en schema

## Success Criteria

- [ ] Los 6 escenarios pasan en `dotnet test --filter SecurityE2ETests`
- [ ] Tests usan PostgreSQL real (Testcontainers) — no mocks del DbContext
- [ ] Cada test crea sus propios datos y los limpia
- [ ] JWT tokens emitidos son válidos y contain los claims esperados
- [ ] No hay llamadas a servicios externos (todo via Testcontainers)