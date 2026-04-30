# Cloudflare R2 — Guía Completa de Configuración

## 1. Creación de Buckets

En el dashboard de Cloudflare (https://dash.cloudflare.com), en **R2** (Object Storage):

### 1.1 Crear 3 buckets (uno por ambiente)

```
Nombre del bucket          Región     Uso
─────────────────────────────────────────────────
medicalcenter-imports-dev      WNAM     Desarrollo local
medicalcenter-imports-staging  WNAM     Staging
medicalcenter-imports-prod     WNAM     Producción
```

**Pasos:**
1. Click en **Create bucket**
2. Nombre: `medicalcenter-imports-dev` (etc)
3. Región: elige una (ej. `WNAM` = us-west)
4. Deja todo default (no public, no CORS yet)
5. Click **Create bucket**
6. Repetir x3

---

## 2. Configurar CORS por Bucket

Necesario para que el frontend haga `PUT` directo a R2 desde el navegador.

### 2.1 Para cada bucket (dev, staging, prod):

1. Click en el bucket
2. **Settings** → **CORS**
3. Click **Add CORS rule**

**Para desarrollo local + staging:**
```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173",
    "https://staging.tu-clinica.com"
  ],
  "AllowedMethods": ["PUT", "OPTIONS"],
  "AllowedHeaders": ["Content-Type", "Content-Length"],
  "MaxAgeSeconds": 3600,
  "ExposedHeaders": ["ETag"]
}
```

**Para producción:**
```json
{
  "AllowedOrigins": [
    "https://app.tu-clinica.com"
  ],
  "AllowedMethods": ["PUT", "OPTIONS"],
  "AllowedHeaders": ["Content-Type", "Content-Length"],
  "MaxAgeSeconds": 3600,
  "ExposedHeaders": ["ETag"]
}
```

**Notas:**
- `AllowedMethods`: solo PUT y OPTIONS (sin DELETE, GET global, LIST)
- `AllowedHeaders`: solo los que el SDK S3 va a firmar (Content-Type, Content-Length)
- No permitas `*` — especifica orígenes concretos

---

## 3. Política de Lifecycle (retención automática)

Borra los archivos XLSX después de N días (los datos ya están en PostgreSQL).

### 3.1 Para cada bucket:

1. Click en el bucket
2. **Settings** → **Lifecycle**
3. Click **Add lifecycle rule**

**Desarrollo (7 días):**
```
Rule name: delete-old-imports
Prefix:    imports/pacientes/
Action:    Delete object
Days:      7
```

**Staging (30 días):**
```
Rule name: delete-old-imports
Prefix:    imports/pacientes/
Action:    Delete object
Days:      30
```

**Producción (90 días):**
```
Rule name: delete-old-imports
Prefix:    imports/pacientes/
Action:    Delete object
Days:      90
```

---

## 4. Crear API Token (credenciales)

Las credenciales que el backend usa para generar presigned URLs y leer/escribir archivos.

> ⚠️ **PRERREQUISITO**: Debes haber **comprado R2** en tu plan de Cloudflare antes de poder generar API tokens. Si intentas crear un token sin R2 activo, recibirás un error.

### 4.0 Tipos de API Tokens (Account vs User)

Cloudflare ofrece dos tipos de tokens con comportamientos diferentes:

| Característica | Account API Token | User API Token |
|----------------|-------------------|----------------|
| **Persistencia** | Sobrevive cambios de usuario | Se invalida si el usuario que lo creó deja la organización |
| **Recomendado para** | ✅ Producción | Desarrollo únicamente |
| **Creación** | Dashboard → Account API Tokens | Dashboard → My API Tokens |
| **Scope** | Puede ограничить por bucket | tied a tu usuario |

**Recomendación para producción:**
- ✅ Usa **Account API Token** (más robusto, sobrevive cambios de personal)
- ⚠️ User API Tokens aceptables solo para desarrollo local

### 4.1 En Cloudflare Dashboard:

1. Ve a **R2 Object Storage** → **Overview**
2. Busca **Account Details** (en la página de overview del bucket o account)
3. Click en **Manage API Tokens**
4. Click **Create API Token**

**Ruta alternativa (según tu plan de Cloudflare):**
- **R2** → **API Tokens** (si está visible directamente)

**Configurar permisos mínimos:**
```
Token name:          medicalcenter-r2-token-dev
Permissions:         Object Read, Object Write
Bucket:              medicalcenter-imports-dev (SOLO este bucket)
TTL:                 Never expires (o 90 días según policy)
```

Repite para staging y prod (3 tokens, uno por bucket).

> 💡 **Tip**: Al crear el token, selecciona "Specific bucket" en lugar de "All buckets" para mayor seguridad.

### 4.2 Guardar las credenciales

Cloudflare te muestra (una sola vez):
```
Access Key ID:       xxxxxxxxxxxxxxxx
Secret Access Key:   xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

**Guardar en:**
- `.env.local` (desarrollo, NO commitear)
- Sistema de secrets del VPS (staging, prod)
- 1Password / Vault si tienes

---

## 5. Endpoint de R2 (para ASP.NET)

Cada cuenta de Cloudflare tiene un endpoint único.

### 5.1 Encontrar el endpoint:

1. En **R2** → **Overview**
2. Busca **S3 API endpoint**: `https://<account-id>.r2.cloudflarestorage.com`

Copiar y guardar como variable:
```
R2__Endpoint=https://abc123def456.r2.cloudflarestorage.com
```

---

## 6. Configuración en ASP.NET

### 6.1 `appsettings.json` (valores default/placeholder)

```json
{
  "R2": {
    "AccountId": "placeholder-account-id",
    "AccessKeyId": "placeholder-access-key",
    "SecretAccessKey": "placeholder-secret-key",
    "Bucket": "medicalcenter-imports-dev",
    "Endpoint": "https://placeholder.r2.cloudflarestorage.com",
    "Region": "auto",
    "PresignTtlSeconds": 300
  },
  "Imports": {
    "MaxFileSizeBytes": 10485760,
    "StorageBucket": "medicalcenter-imports-dev",
    "StorageProvider": "r2"
  }
}
```

### 6.2 `appsettings.Development.json` (override local)

```json
{
  "R2": {
    "AccountId": "tu-account-id",
    "AccessKeyId": "tu-access-key-id",
    "SecretAccessKey": "tu-secret-access-key",
    "Bucket": "medicalcenter-imports-dev",
    "Endpoint": "https://tu-account-id.r2.cloudflarestorage.com",
    "Region": "auto",
    "PresignTtlSeconds": 300
  },
  "Imports": {
    "MaxFileSizeBytes": 10485760,
    "StorageBucket": "medicalcenter-imports-dev",
    "StorageProvider": "r2"
  }
}
```

**NO COMMITEAR — agregar a `.gitignore`:**
```
appsettings.Development.json
appsettings.Production.json
.env.local
.env.*.local
```

### 6.3 Variables de entorno (Docker / VPS)

En `docker-compose.yml`:
```yaml
services:
  api:
    environment:
      - R2__AccountId=${R2_ACCOUNT_ID}
      - R2__AccessKeyId=${R2_ACCESS_KEY_ID}
      - R2__SecretAccessKey=${R2_SECRET_ACCESS_KEY}
      - R2__Bucket=${R2_BUCKET:-medicalcenter-imports-dev}
      - R2__Endpoint=${R2_ENDPOINT}
      - R2__Region=auto
      - R2__PresignTtlSeconds=300
      - Imports__MaxFileSizeBytes=10485760
      - Imports__StorageBucket=${R2_BUCKET:-medicalcenter-imports-dev}
      - Imports__StorageProvider=r2
```

En `.env` (para `docker compose up`):
```env
# Cloudflare R2
R2_ACCOUNT_ID=abc123def456
R2_ACCESS_KEY_ID=xxxxxxxxxxxxxxxx
R2_SECRET_ACCESS_KEY=yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy
R2_BUCKET=medicalcenter-imports-dev
R2_ENDPOINT=https://abc123def456.r2.cloudflarestorage.com
```

**NO COMMITEAR `.env`** — es un arquivo local.

### 6.4 Verificar en appsettings.json (DI/Options)

El código en `ServiceCollectionExtensions.cs` ya lee:
```csharp
services.Configure<R2Options>(configuration.GetSection(R2Options.SectionName)); // "R2"
services.Configure<ImportsOptions>(configuration.GetSection(ImportsOptions.SectionName)); // "Imports"
```

Estos se mapean desde environment variables (ej. `R2__AccountId` → `R2:AccountId`).

---

## 7. Flujo Local de Testing (desarrollo)

### 7.1 Clonar credenciales a `.env.local`

```bash
# En la raíz del repo
cat > .env.local <<EOF
R2_ACCOUNT_ID=abc123
R2_ACCESS_KEY_ID=your-access-key
R2_SECRET_ACCESS_KEY=your-secret-key
R2_BUCKET=medicalcenter-imports-dev
R2_ENDPOINT=https://abc123.r2.cloudflarestorage.com
EOF
```

### 7.2 Levantar con docker-compose

```bash
docker-compose up
# Lee las variables de .env.local automáticamente
```

### 7.3 Verificar configuración

```bash
# Dentro del contenedor
printenv | grep R2
# Debe mostrar: R2__Endpoint=..., R2__Bucket=..., etc.
```

### 7.4 Smoke test con Postman

**1) POST /api/v1/importaciones/pacientes/upload-url**
```
Body (JSON):
{
  "file_name": "pacientes.xlsx",
  "size_bytes": 5242880,
  "content_type": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
}

Response (esperado):
{
  "importacion_id": "uuid",
  "upload_url": "https://bucket.r2.cloudflarestorage.com/...",
  "storage_key": "imports/pacientes/2026/04/uuid/pacientes.xlsx",
  "expires_at": "2026-04-28T12:05:00Z",
  "required_headers": {
    "Content-Type": "...",
    "Content-Length": "..."
  }
}
```

**2) PUT <upload_url>**
```bash
curl -X PUT "https://bucket.r2.cloudflarestorage.com/..." \
  -H "Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" \
  -H "Content-Length: 5242880" \
  --data-binary @pacientes.xlsx

# Esperado: HTTP 200
```

**3) POST /api/v1/importaciones/pacientes/{id}/confirmar**
```
Response (esperado):
{
  "importacion_id": "uuid",
  "estado": "procesado",
  "total_rows": 100,
  "created_rows": 80,
  "updated_rows": 15,
  "skipped_rows": 5,
  "error_rows": 0,
  "errors": []
}
```

**4) GET /api/v1/importaciones/pacientes/{id}**
```
Response (esperado):
{
  "importacion_id": "uuid",
  "tipo": "pacientes",
  "estado": "procesado",
  "file_name": "pacientes.xlsx",
  "size_bytes": 5242880,
  "total_filas": 100,
  "filas_insertadas": 80,
  "filas_actualizadas": 15,
  "filas_con_error": 5,
  "error_message": null,
  "created_at": "...",
  "started_at": "...",
  "finished_at": "...",
  "errors": [ { "row_number": 5, "message": "..." } ]
}
```

---

## 8. Verificación de Seguridad

### 8.1 Permisos mínimos

✅ API Token con scope mínimo: Object Read + Object Write (sin delete).
✅ CORS: solo orígenes concretos (sin `*`).
✅ Presigned URLs: TTL 5 min (expiración rápida).
✅ Content-Type + Content-Length: firmados (no puede alterarse).

### 8.2 Validaciones en backend

✅ Magic bytes (PK\x03\x04 para XLSX).
✅ Sanitización de filenames (regex [^a-z0-9._-]).
✅ Validación de extensión (.xlsx, .xls).
✅ Validación de content-type (MIME).
✅ Validación de tamaño (≤ MaxFileSizeBytes).
✅ Ownership check (usuario_id en importaciones).
✅ Auditoría (admin_event_feed con importacion.creada/confirmada/fallida).

### 8.3 Credenciales nunca en el frontend

✅ Frontend recibe presigned URL opaca (sin credenciales).
✅ AccessKeyId/SecretAccessKey nunca viajan por la red pública.
✅ Todas las firmas las genera el backend.

---

## 9. Troubleshooting

| Problema | Causa | Solución |
|----------|-------|----------|
| `RequestError: connect ENOTFOUND` | Endpoint URL mal | Verifica `R2__Endpoint` en env vars |
| `AccessDenied` en CreatePresignedUrl | Token incorrecto o sin permisos | Revisa AccessKeyId/SecretAccessKey y permisos en el token |
| `InvalidBucket` | Bucket no existe o nombre mal | Verifica `R2__Bucket` coincide exactamente con el nombre en Cloudflare |
| CORS preflight falla (OPTIONS 403) | CORS no configurado en bucket | Agregar regla CORS en Cloudflare dashboard |
| `SignatureDoesNotMatch` en PUT | Headers Content-Type/Content-Length no firmados | Asegurar que `required_headers` del presign se pasan en el PUT |
| `RequestTimeout` | Timeout muy corto para archivo grande | Aumentar `PresignTtlSeconds` (default 300 = 5 min) |
| Archivos aparecen en Cloudflare pero no en code | Verificar que PresignTtlSeconds no expiró | Default es 300 segundos (5 min), suficiente para 10 MB @10Mbps |

---

## 10. Rollout por Ambientes

### Desarrollo

1. Crear bucket `medicalcenter-imports-dev`
2. Crear token R2
3. Copiar credenciales a `.env.local`
4. Run `docker-compose up`
5. Smoke test con Postman
6. Test con AdminImportarPacientesTab.jsx (provider=aspnet)

### Staging

1. Crear bucket `medicalcenter-imports-staging`
2. Crear token R2 con mismo scope
3. En CI/CD o secrets del VPS, agregar env vars `R2__*`
4. Deploy backend
5. Deploy frontend con `provider=aspnet`
6. QA: importar XLSX real (10-100 filas)
7. Verificar en Cloudflare que blob aparece
8. Verificar en PostgreSQL que importaciones + errores se persisten

### Producción

1. Crear bucket `medicalcenter-imports-prod`
2. Crear token R2 con mismo scope
3. En secrets del VPS, agregar env vars `R2__*` (con credenciales de prod)
4. Lifecycle policy: 90 días (no 7)
5. CORS: solo `https://app.tu-clinica.com`
6. Deploy con feature flag: `provider=aspnet` (o toggle dinámico)
7. Monitoreo: alertas en `admin_event_feed` si % de fallidos > 5%

---

## 11. Referencias

- **Cloudflare R2 Docs:** https://developers.cloudflare.com/r2/
- **S3 Presigned URLs (compatible):** https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html
- **CORS en R2:** https://developers.cloudflare.com/r2/platform/s3-compatibility/
- **AWSSDK.S3 (C#):** https://github.com/aws/aws-sdk-net
- **ClosedXML (XLSX parsing):** https://closedxml.io/

---

## Checklist Final

- [ ] 3 buckets creados (dev, staging, prod)
- [ ] CORS configurado en cada bucket
- [ ] Lifecycle policies aplicadas (7/30/90 días)
- [ ] R2 purchased/activado (prerequisito para crear tokens)
- [ ] 3 Account API Tokens generados (uno por bucket) — usar Account, no User tokens
- [ ] Credenciales guardadas en `.env.local` (NO commitear)
- [ ] `appsettings.json` tiene placeholders
- [ ] `docker-compose.yml` lee variables
- [ ] Smoke test local pasó (4-step flow)
- [ ] Backend compila sin errores (0 warnings)
- [ ] Migration aplicada: `dotnet ef database update`
- [ ] Frontend tiene provider=aspnet activo
- [ ] `git grep supabase` en AdminImportarPacientesTab.jsx = 0
