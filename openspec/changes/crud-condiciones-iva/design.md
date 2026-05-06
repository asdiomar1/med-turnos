# Design: Full CRUD API for `public.condiciones_iva`

## Technical Approach

Extend the existing read-only catalog endpoint for `CondicionIva` into a full CRUD API following the exact pattern established by `ObraSocial` in `CatalogsService` / `CatalogsController`. The implementation requires:

- Domain entity mutation methods (`Update`, `SetActive`).
- Repository additions (`GetByIdAsync`, `GetByNormalizedNameAsync`, `AddAsync`).
- Service additions for create, update, and toggle-active operations.
- New controller actions with granular authorization policies.
- Cache invalidation on every mutating operation.
- Admin event feed logging for auditability.

No database migration is required — the table and EF configuration already exist.

## Architecture Decisions

| Decision | Options | Trade-offs | Choice |
|---|---|---|---|
| Entity type for event feed | Hardcode in helper vs pass as parameter | Hardcoding requires duplication; parameter keeps one helper | **Refactor `CatalogsService.RegisterCatalogEventAsync` to accept `entityType`** (matches `ProfessionalsService` pattern) |
| Class-level auth on `CatalogsController` | Keep `AdminAccess` vs switch to granular per-action | Keeping `AdminAccess` violates the requirement for `ConfigRead` on GETs | **Remove class-level `[Authorize(Policy = "AdminAccess")]` and apply per-action policies** |
| Validation strategy | Manual in service vs FluentValidation | No FluentValidation used elsewhere in catalogs; manual keeps consistency | **Manual validation in `CatalogsService`** (same as `ObraSocial`) |
| Cache invalidation scope | Invalidate active-only key vs both keys | Active-only miss would repopulate stale data if `all` still holds old state | **Invalidate BOTH `mc:catalog:condicioniva` and `mc:catalog:condicioniva:all` on every write** |
| Normalization for uniqueness | Case-insensitive comparison in SQL vs store normalized | Existing `ObraSocial` pattern uses `.ToLower()` in query; no DB-level unique index exists | **Follow existing pattern: `.ToLower()` comparison in `GetByNormalizedNameAsync`** |

## Data Flow

### Create Flow

```
POST /api/v1/catalogos/condiciones-iva
│
├─→ [Auth] ConfigCatalogsManage
├─→ CatalogsController.CreateCondicionIva
│   └─→ User.GetUserId() → actorUserId
├─→ CatalogsService.CreateCondicionIvaAsync
│   ├─→ Normalize(nombre)
│   ├─→ EnsureName (ValidationException if empty)
│   ├─→ EnsureUniqueNameAsync (ConflictException if duplicate)
│   ├─→ new CondicionIva(0, normalizedName, true, 0)
│   ├─→ condicionIvaRepository.AddAsync
│   │   └─→ dbContext.Add + cache.RemoveAsync(both keys)
│   ├─→ unitOfWork.SaveChangesAsync
│   ├─→ RegisterCatalogEventAsync (CondicionIvaCreated)
│   ├─→ unitOfWork.SaveChangesAsync (event)
│   └─→ return dto
└─→ 200 OK → CondicionIvaResponse
```

### Update Flow

```
PATCH /api/v1/catalogos/condiciones-iva/{id}
│
├─→ [Auth] ConfigCatalogsManage
├─→ CatalogsController.UpdateCondicionIva
├─→ CatalogsService.UpdateCondicionIvaAsync
│   ├─→ condicionIvaRepository.GetByIdAsync (NotFoundException if null)
│   ├─→ Normalize(nombre)
│   ├─→ EnsureName (ValidationException if empty)
│   ├─→ EnsureUniqueNameAsync (ConflictException if duplicate, except self)
│   ├─→ entity.Update(normalizedName, request.Orden)
│   ├─→ unitOfWork.SaveChangesAsync
│   ├─→ RegisterCatalogEventAsync (CondicionIvaUpdated)
│   ├─→ unitOfWork.SaveChangesAsync (event)
│   ├─→ cache.RemoveAsync(both keys)
│   └─→ return dto
└─→ 200 OK → CondicionIvaResponse
```

### Toggle Active Flow

```
PATCH /api/v1/catalogos/condiciones-iva/{id}/estado
│
├─→ [Auth] ConfigCatalogsManage
├─→ CatalogsController.ToggleCondicionIvaActive
├─→ CatalogsService.SetCondicionIvaActiveAsync
│   ├─→ condicionIvaRepository.GetByIdAsync (NotFoundException if null)
│   ├─→ entity.SetActive(request.Activo)
│   ├─→ unitOfWork.SaveChangesAsync
│   ├─→ RegisterCatalogEventAsync (CondicionIvaStatusUpdated)
│   ├─→ unitOfWork.SaveChangesAsync (event)
│   ├─→ cache.RemoveAsync(both keys)
│   └─→ return dto
└─→ 200 OK → CondicionIvaResponse
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/MedicalCenter.Domain/Entities/CondicionIva.cs` | **Modify** | Add `Update(string nombre, int orden)` and `SetActive(bool activo)` methods |
| `src/MedicalCenter.Application/Abstractions/Persistence/ICondicionIvaRepository.cs` | **Modify** | Add `GetByIdAsync`, `GetByNormalizedNameAsync`, `AddAsync` |
| `src/MedicalCenter.Application/Features/Catalogs/ICatalogsService.cs` | **Modify** | Add `CreateCondicionIvaAsync`, `UpdateCondicionIvaAsync`, `SetCondicionIvaActiveAsync` |
| `src/MedicalCenter.Application/Features/Catalogs/CatalogsService.cs` | **Modify** | Implement three new methods; refactor `RegisterCatalogEventAsync` to accept `entityType` parameter; add `EnsureCondicionIvaUniqueNameAsync` helper |
| `src/MedicalCenter.Infrastructure/Persistence/Repositories/CondicionIvaRepository.cs` | **Modify** | Implement `GetByIdAsync`, `GetByNormalizedNameAsync`, `AddAsync` with cache invalidation |
| `src/MedicalCenter.Contracts/Catalogs/CatalogResponses.cs` | **Modify** | Add `CreateCondicionIvaRequest`, `UpdateCondicionIvaRequest`, `ToggleCondicionIvaActiveRequest` |
| `src/MedicalCenter.Api/Controllers/V1/CatalogsController.cs` | **Modify** | Add POST, PATCH, PATCH/estado actions; replace class-level `AdminAccess` with per-action policies |
| `src/MedicalCenter.Application/Features/AdminEventFeed/AdminEventFeedConstants.cs` | **Modify** | Add `EntityTypes.CondicionIva` and action codes `CondicionIvaCreated`, `CondicionIvaUpdated`, `CondicionIvaStatusUpdated`; add to `CatalogActionDefinitions` |

## Interfaces / Contracts

### Domain Entity

```csharp
public sealed class CondicionIva : Entity<int>
{
    // existing properties and ctor...

    public void Update(string nombre, int orden)
    {
        Nombre = nombre;
        Orden = orden;
    }

    public void SetActive(bool activo) => Activo = activo;
}
```

### Repository Interface

```csharp
public interface ICondicionIvaRepository
{
    Task<IReadOnlyCollection<CondicionIva>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<CondicionIva?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CondicionIva?> GetByNormalizedNameAsync(string normalizedName, int? exceptId, CancellationToken cancellationToken);
    Task AddAsync(CondicionIva condicionIva, CancellationToken cancellationToken);
}
```

### Service Interface

```csharp
public interface ICatalogsService
{
    // existing methods...

    Task<CondicionIvaSummaryDto> CreateCondicionIvaAsync(
        Guid actorUserId, string nombre, CancellationToken cancellationToken);

    Task<CondicionIvaSummaryDto> UpdateCondicionIvaAsync(
        Guid actorUserId, int id, string nombre, int orden, CancellationToken cancellationToken);

    Task<CondicionIvaSummaryDto> SetCondicionIvaActiveAsync(
        Guid actorUserId, int id, bool activo, CancellationToken cancellationToken);
}
```

### Controller Actions

```csharp
[ApiController]
[Route("api/v1/catalogos")]
// NO class-level [Authorize(Policy = "AdminAccess")]
public sealed class CatalogsController(ICatalogsService catalogsService) : ControllerBase
{
    [HttpGet("condiciones-iva")]
    [Authorize(Policy = "ConfigRead")]
    public async Task<IActionResult> GetCondicionesIva(
        [FromQuery(Name = "include_inactive")] bool includeInactive = false,
        CancellationToken cancellationToken = default)
        => Ok((await catalogsService.GetCondicionesIvaAsync(includeInactive, cancellationToken))
            .Select(x => x.ToResponse()));

    [HttpPost("condiciones-iva")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> CreateCondicionIva(
        [FromBody] CreateCondicionIvaRequest request,
        CancellationToken cancellationToken)
        => Ok((await catalogsService.CreateCondicionIvaAsync(
            User.GetUserId(), request.Nombre, cancellationToken)).ToResponse());

    [HttpPatch("condiciones-iva/{id:int}")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> UpdateCondicionIva(
        int id,
        [FromBody] UpdateCondicionIvaRequest request,
        CancellationToken cancellationToken)
        => Ok((await catalogsService.UpdateCondicionIvaAsync(
            User.GetUserId(), id, request.Nombre, request.Orden, cancellationToken)).ToResponse());

    [HttpPatch("condiciones-iva/{id:int}/estado")]
    [Authorize(Policy = "ConfigCatalogsManage")]
    public async Task<IActionResult> ToggleCondicionIvaActive(
        int id,
        [FromBody] ToggleCondicionIvaActiveRequest request,
        CancellationToken cancellationToken)
        => Ok((await catalogsService.SetCondicionIvaActiveAsync(
            User.GetUserId(), id, request.Activo, cancellationToken)).ToResponse());
}
```

### Contracts (Requests)

```csharp
public sealed class CreateCondicionIvaRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class UpdateCondicionIvaRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("orden")]
    public int Orden { get; init; }
}

public sealed class ToggleCondicionIvaActiveRequest
{
    [JsonPropertyName("activo")]
    public bool Activo { get; init; }
}
```

## Cache Strategy

| Key | Purpose | TTL |
|---|---|---|
| `mc:catalog:condicioniva` | Active-only list (ordered by `orden`, then `nombre`) | 1440 min (24 h) |
| `mc:catalog:condicioniva:all` | All records including inactive | 1440 min (24 h) |

**Invalidation**: On `AddAsync`, `UpdateCondicionIvaAsync`, and `SetCondicionIvaActiveAsync`, call `_cache.RemoveAsync` for **both** keys immediately after the mutating DB operation succeeds. The `AddAsync` method already invalidates inside the repository; for update/toggle, the service layer must call `_cache.RemoveAsync` after `SaveChangesAsync`.

## Auth & Security

| Endpoint | HTTP | Policy | Rationale |
|---|---|---|---|
| `GET /condiciones-iva` | GET | `ConfigRead` | Read-only catalog access |
| `POST /condiciones-iva` | POST | `ConfigCatalogsManage` | Catalog mutation |
| `PATCH /condiciones-iva/{id}` | PATCH | `ConfigCatalogsManage` | Catalog mutation |
| `PATCH /condiciones-iva/{id}/estado` | PATCH | `ConfigCatalogsManage` | Catalog mutation |

**Actor User ID**: Obtained via `User.GetUserId()` extension method (`ClaimsPrincipalExtensions.cs`), which reads `ClaimTypes.NameIdentifier` and falls back to `"sub"`, parsing as `Guid`. Throws `UnauthorizedAccessException` if unparsable — caught by middleware as `500`, but in practice the auth pipeline guarantees a valid claim before reaching the controller.

## Error Handling

| Layer | Condition | Exception | HTTP Status |
|---|---|---|---|
| Service | `nombre` is null/whitespace after normalization | `ValidationException("Nombre requerido.")` | 400 Bad Request |
| Service | Another record already has the same name (case-insensitive) | `ConflictException("Ya existe una condición de IVA con ese nombre.")` | 409 Conflict |
| Service | `id` not found in repository | `NotFoundException("Condición de IVA no encontrada.")` | 404 Not Found |
| Middleware | Any unhandled exception | `ApiErrorResponse { Code = "internal_error" }` | 500 Internal Server Error |

## Admin Event Feed Integration

### Constants to Add

```csharp
public static class EntityTypes
{
    public const string CondicionIva = "condicion_iva";
}

public static class ActionCodes
{
    public const string CondicionIvaCreated = "condicion_iva.created";
    public const string CondicionIvaUpdated = "condicion_iva.updated";
    public const string CondicionIvaStatusUpdated = "condicion_iva.status_updated";
}
```

### Event Payloads

| Action | Title | Summary | Entity Type | Entity ID |
|---|---|---|---|---|
| Create | "Condición de IVA creada" | `Se creó la condición de IVA "{nombre}".` | `condicion_iva` | `{id}` |
| Update | "Condición de IVA actualizada" | `Se actualizó la condición de IVA "{prev}" → "{nombre}".` | `condicion_iva` | `{id}` |
| Toggle | "Estado de condición de IVA actualizado" | `La condición de IVA "{nombre}" quedó {activa/inactiva}.` | `condicion_iva` | `{id}` |

**Event family**: `catalog`
**Source**: `api`
**Trace id format**: `"condicion_iva:{actionCode}:{entityId}:{Guid.NewGuid():N}"`

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `CondicionIva.Update` / `SetActive` state changes | Direct instantiation, assert property values |
| Integration | `CatalogsService` create/update/toggle with duplicate name, not-found, and success paths | In-memory EF + stubbed `IAdminEventFeedRepository` and `ICacheService` |
| Integration | `CatalogsController` authorization attributes | Reflection scan to verify `[Authorize(Policy = ...)]` is present on every action |
| E2E | Full happy path for all four endpoints | TestServer with seeded DB; assert response shape and HTTP status |

## Migration / Rollout

No migration required. The table, EF configuration, and existing GET endpoint are already in place.

## Open Questions

1. **Ordering on create**: Should new `CondicionIva` entries default to `orden = 0` (as currently hardcoded in the entity constructor) or compute `MAX(orden) + 1` dynamically like `Medico`/`Referente` do via `GetNextOrderAsync`?
2. **Class-level auth removal**: Removing `[Authorize(Policy = "AdminAccess")]` from `CatalogsController` affects the existing `ObraSocial` endpoints. Are we also switching `ObraSocial` endpoints to granular policies in this change, or should that be a separate follow-up?
3. **Cache TTL**: 1440 minutes (24 hours) is very long for catalog data. Is this intentional, or should the TTL be reduced to something like 60 minutes?
