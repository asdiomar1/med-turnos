## Exploration Summary: Slot Fields Standardization

### Current State of Turnos/Bloques Endpoints

**Domain Layer (Entities):**
- `Appointment`: Already has **both** `MedicoId (int?)` and `MedicoUserId (Guid?)` — correctly tracking both catalog ID and user ID
- `BlockHistory`: Only has `MedicoId (int?)` — **missing `MedicoUserId`**
- `BlockHistoryCreateParams`: Only has `MedicoId` — **missing `MedicoUserId`**
- `Medico`: Has `Id (int)`, `Nombre`, `Activo`, `Orden`, `PerfilId (Guid?)` — represents the catalog entity

**Application Layer (DTOs):**
- `AppointmentSummary`: Has both `MedicoId` and `MedicoUserId` — correct
- `TurnoEnrichedSummary`: Has `MedicoId (int?)` and nested `MedicoEnrichedSummary` — but nested summary only has `int Id`, `Nombre`, `Activo` (catalog-only view)
- `MedicoEnrichedSummary`: Only has `int Id`, `string? Nombre`, `bool? Activo` — **missing `Guid? MedicoUserId`**
- `MedicoSummaryDto`: Only has `int Id`, `string? Nombre` — **missing `Guid? Id`**

**API Layer (Contracts/Responses):**
- `TurnoEnrichedResponse`: Has `[JsonPropertyName("medico_id")]` and nested `MedicoEnrichedResponse` — but nested only has `int Id`, `Nombre`, `Activo`
- `MedicoEnrichedResponse`: Only has `int Id`, `Nombre`, `Activo` — **missing `medico_user_id`**
- `MedicoResponse`: Only has `int Id`, `Nombre` — **missing `medico_user_id`**

**API Mappings:**
- `AppointmentResponseMappings.ToTurnoEnrichedResponse()`: Sends `MedicoId` at top level but nested `Medico` mapping uses catalog-only view (line 114-119)
- `SimpleResponseMappings.ToResponse(MedicoSummaryDto)`: Only sends `Id` and `Nombre` (line 39)
- `SimpleResponseMappings` ClinicalEvolution: Correctly maps `MedicoId`, `MedicoUserId`, `MedicoNombre`, `MedicoActivo` (lines 67, 68, 72) — **shows the correct pattern exists**

### How Medico Fields Are Currently Handled in DTOs/Services

**What works correctly:**
- The **domain** tracks both `MedicoId` (catalog int) and `MedicoUserId` (Guid user ID) in appointments
- The **services** (AppointmentsService.cs) correctly handle both: line 147 validates `command.MedicoUserId`, line 1015 queries `medicoRepository.GetByIdsAsync` for catalog IDs
- The **ClinicalEvolution** response mapping already uses the full pattern: `MedicoId`, `MedicoUserId`, `MedicoNombre`, `MedicoActivo` (lines 67-73)

**What is mismatched:**
1. **BlockHistory** endpoints only track/send `MedicoId` — never `MedicoUserId`
2. **TurnoEnrichedResponse** sends `MedicoId` at top level but nested `Medico` object uses catalog-only view (int ID, Nombre, Activo) — missing user context
3. **MedicoEnrichedSummary** DTO only has `int Id` — should have `int Id` **and** `Guid? MedicoUserId`
4. **MedicoEnrichedResponse** contract only has `int Id` — should have `int Id` **and** `Guid? MedicoUserId`
5. **MedicoResponse** (catalog endpoint) only has `int Id`, `Nombre` — should include `Guid? MedicoUserId`

### Gaps/Mismatches with Frontend Expectations

**Frontend expects (from user context):**
```json
{
  "medico_id": null,
  "medico_user_id": "de541a88-06f0-442b-9593-af390cd02ffb",
  "medico_nombre": "Dra. Ana Pérez",
  "medico": {
    "id": "de541a88-06f0-442b-9593-af390cd02ffb",
    "nombre": "Dra. Ana Pérez",
    "activo": true
  }
}
```

**Backend currently sends:**
- **Top level**: `MedicoId` (int?) — but NO `MedicoUserId`
- **Nested `Medico` object**: `id` (int), `nombre`, `activo` — NO `id` as Guid, NO `medico_user_id`

**Specific gaps:**
1. **Slot endpoints** (`/api/v1/turnos/fecha`, `/turnos/rango`, etc.): Send `MedicoId` at top level but **omit `MedicoUserId`** entirely
2. **Block history endpoints** (`/bloques/historial`, `/bloques/historial/rango`): Only track `MedicoId` in `BlockHistory` entity — **never store `MedicoUserId`**
3. **Nested `Medico` object**: Frontend expects `id` as **Guid** (user ID) but backend sends `id` as **int** (catalog ID)
4. **No normalized `MedicoNombre`**: Frontend accepts `medico_nombre`, `medicoNombre`, `medico_user_nombre`, `medicoUserNombre` — but only `Medico.Nombre` is sometimes sent (line 72 in SimpleResponseMappings)
5. **BlockHistory entity**: Missing migration to add `MedicoUserId` column — only has `MedicoId`

### Recommended Approaches (Standardization Strategy)

**Per user requirements, the backend should send the THREE fields in every endpoint:**

1. **`MedicoUserId` (Guid)** — Always present when a medico is involved
   - Use when: `MedicoId.HasValue`, fetch from `MedicoRepository` via `PerfilId`
   - Use when: Direct user assignment (hold, assign, update operative)
   
2. **Nested `Medico` object with full context** — `{ id: Guid, nombre: string, activo: bool }`
   - `id` should be the **user ID** (Guid), not catalog int
   - Or include both: `{ id: int, medicoUserId: Guid, nombre, activo }`

3. **`MedicoNombre` (string)** — Denormalized quick-access name
   - Always sent alongside other fields for efficient frontend rendering
   - Same as `Medico.Nombre` or `MedicoUser.Nombre`

**Implementation approach (3-tier):**

**Tier 1 — Domain Entities (minimal changes):**
- Add `MedicoUserId (Guid?)` to `BlockHistory` and `BlockHistoryCreateParams` (match Appointment pattern)
- Add `MedicoNombre (string?)` to `BlockHistorySummary` DTOs

**Tier 2 — Application Layer (core changes):**
- Update `TurnoEnrichedSummary`: Add `Guid? MedicoUserId` and `string? MedicoNombre`
- Update `MedicoEnrichedSummary`: Change to `{ int Id, Guid? MedicoUserId, string? Nombre, bool? Activo }`
- Update `MedicoSummaryDto`: Change to `{ int Id, Guid? MedicoUserId, string? Nombre }`
- Update `AppointmentSummary`: Already correct — no changes needed
- Update `BlockHistorySummary` (if exists): Add `MedicoUserId`, `MedicoNombre`

**Tier 3 — API/Contracts Layer (response mapping):**
- Update `MedicoEnrichedResponse`: Add `Guid? MedicoUserId` 
- Update `MedicoResponse`: Add `Guid? MedicoUserId`
- Update `AppointmentResponseMappings.ToTurnoEnrichedResponse()`: Map nested `Medico` to include user context
- Update `SimpleResponseMappings`: Ensure `MedicoResponse` includes `MedicoUserId`
- Add `MedicoNombre` to block history responses

**Files that need changes:**
- `src/MedicalCenter.Domain/Entities/BlockHistory.cs` — Add `MedicoUserId`
- `src/MedicalCenter.Domain/Entities/BlockHistory.cs` — Add `MedicoUserId` to `BlockHistoryCreateParams`
- `src/MedicalCenter.Application/DTOs/TurnoEnrichedDtos.cs` — Update `MedicoEnrichedSummary`
- `src/MedicalCenter.Application/DTOs/TurnoEnrichedDtos.cs` — Add `MedicoUserId` to `TurnoEnrichedSummary`
- `src/MedicalCenter.Contracts/Appointments/TurnoEnrichedResponses.cs` — Update `MedicoEnrichedResponse`
- `src/MedicalCenter.Contracts/Professionals/ProfessionalContracts.cs` — Add `MedicoUserId` to `MedicoResponse`
- `src/MedicalCenter.Api/Mappings/AppointmentResponseMappings.cs` — Fix nested `Medico` mapping
- `src/MedicalCenter.Api/Mappings/SimpleResponseMappings.cs` — Update `MedicoResponse` mapping
- Migrations: Add `medico_user_id` column to `block_history` table
- `src/MedicalCenter.Infrastructure/Persistence/Configurations/BlockHistoryConfiguration.cs` — Add `MedicoUserId` config
- `src/MedicalCenter.Infrastructure/Persistence/Repositories/BlockHistoryRepository.cs` — Handle `MedicoUserId`
- `src/MedicalCenter.Application/Features/Appointments/AppointmentsService.cs` — Handle `MedicoUserId` in block history methods
