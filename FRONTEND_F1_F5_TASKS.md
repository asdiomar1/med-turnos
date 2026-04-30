# Frontend — F1 & F5: Migración a Trilogía de Endpoints (sin Supabase Storage directo)

## Contexto

Actualmente, `AdminImportarPacientesTab.jsx` toca directamente `supabase.storage` y `supabase.auth.getSession()`. La migración introduce una **trilogía de endpoints** que ocultan el proveedor de storage (Supabase o R2). El frontend no debe conocer dónde se suben los archivos.

---

## F1 — Implementación de la Trilogía (cero R2, puro refactor de interfaz)

### Objetivo
Crear 3 métodos discretos en ambos providers (`aspnetApiProvider.js` y `supabaseApiProvider.js`) que implementen el mismo contrato. El componente habla solo con la trilogía.

### Cambios por archivo

#### 1. `src/services/api/index.js` (barrel/proxy)

**Agregar exports:**
```javascript
export {
  apiCrearImportacionPacientesUploadUrl,
  apiSubirArchivoImportacionPacientes,
  apiConfirmarImportacionPacientes,
  apiImportarPacientes, // deprecado, pero mantenido
} from './api';
```

O si está en un archivo separado:
```javascript
export {
  apiCrearImportacionPacientesUploadUrl,
  apiSubirArchivoImportacionPacientes,
  apiConfirmarImportacionPacientes,
  apiImportarPacientes,
} from './providerRegistry';
```

#### 2. `src/services/api/providerRegistry.js`

**Agregar a la exportación principal:**
```javascript
export const apiCrearImportacionPacientesUploadUrl = (args) =>
  activeProvider.apiCrearImportacionPacientesUploadUrl(args);

export const apiSubirArchivoImportacionPacientes = (args) =>
  activeProvider.apiSubirArchivoImportacionPacientes(args);

export const apiConfirmarImportacionPacientes = (args) =>
  activeProvider.apiConfirmarImportacionPacientes(args);

// Deprecado — mantener por compatibilidad, delegar a trilogía
export const apiImportarPacientes = async ({ file }) => {
  const upload = await apiCrearImportacionPacientesUploadUrl({
    file_name: file.name,
    size_bytes: file.size,
    content_type: file.type,
  });
  await apiSubirArchivoImportacionPacientes({
    upload_url: upload.upload_url,
    file,
    headers: upload.required_headers,
  });
  return apiConfirmarImportacionPacientes({
    importacion_id: upload.importacion_id,
  });
};
```

#### 3. `src/services/api/providers/aspnetApiProvider.js` (R2 real)

**Agregar:**
```javascript
export async function apiCrearImportacionPacientesUploadUrl({
  file_name,
  size_bytes,
  content_type,
}) {
  return withProviderFallback('crearImportacionPacientesUploadUrl', async () => {
    const res = await requestJson('/api/v1/importaciones/pacientes/upload-url', {
      method: 'POST',
      body: { file_name, size_bytes, content_type },
    });
    return extractApiData(res);
  });
}

export async function apiSubirArchivoImportacionPacientes({
  upload_url,
  file,
  headers,
}) {
  // PUT directo a R2 — no pasa por la API
  const res = await fetch(upload_url, {
    method: 'PUT',
    headers: {
      'Content-Type': headers['Content-Type'] || file.type,
      'Content-Length': headers['Content-Length'] || file.size.toString(),
    },
    body: file,
  });

  if (!res.ok) {
    const status = res.status;
    let message = 'Error al subir el archivo';
    if (status === 400) message = 'Solicitud inválida';
    if (status === 403) message = 'URL de subida expirada — solicitar nueva';
    if (status === 413) message = 'Archivo demasiado grande';
    if (status === 500) message = 'Error en el servidor de storage';
    throw new Error(message);
  }
}

export async function apiConfirmarImportacionPacientes({
  importacion_id,
}) {
  return withProviderFallback('confirmarImportacionPacientes', async () => {
    const res = await requestJson(
      `/api/v1/importaciones/pacientes/${importacion_id}/confirmar`,
      { method: 'POST' }
    );
    return extractApiData(res);
  });
}
```

#### 4. `src/services/api/providers/supabaseApiProvider.js` (compat)

**Agregar tres métodos con la misma firma:**
```javascript
export async function apiCrearImportacionPacientesUploadUrl({
  file_name,
  size_bytes,
  content_type,
}) {
  // Construye una storage_key determinística y un "upload_url" sintético
  // que apiSubirArchivoImportacionPacientes sabe interpretar
  const importacion_id = crypto.randomUUID();
  const storage_key = `imports/${importacion_id}/${file_name}`;
  
  return {
    importacion_id,
    upload_url: `supabase://imports/${storage_key}`, // marcador interno
    storage_key,
    required_headers: {},
  };
}

export async function apiSubirArchivoImportacionPacientes({
  upload_url,
  file,
  headers,
}) {
  if (!upload_url.startsWith('supabase://')) {
    throw new Error('upload_url inválida para provider Supabase');
  }

  const path = upload_url.replace('supabase://imports/', '');
  const { data: { session } } = await supabase.auth.getSession();
  
  if (!session?.access_token) {
    throw new Error('No hay una sesión activa válida');
  }

  const { error } = await supabase.storage
    .from('imports')
    .upload(path, file, { upsert: false });

  if (error) throw error;
}

export async function apiConfirmarImportacionPacientes({
  importacion_id,
  storage_key,
}) {
  // Sigue invocando la Edge Function existente
  // (si tienes el storage_key de la response anterior)
  const payload = await invokePortalAuthenticatedFunction(
    'importar-pacientes',
    {
      storage_path: storage_key.split('/').slice(0, -1).join('/'),
      file_name: storage_key.split('/').pop(),
    }
    // token si es necesario
  );
  return extractApiData(payload);
}
```

#### 5. `src/services/api/messages/imports.js` (NUEVA — centralizar errores)

**Crear:**
```javascript
/**
 * Mapea errores de importación a mensajes amigables para el usuario.
 * Centralizado para evitar duplicación entre providers.
 */
export function mapImportarPacientesErrorMessage(errorOrMessage) {
  const msg = (errorOrMessage?.message || String(errorOrMessage)).toLowerCase();

  // Validación de archivo
  if (msg.includes('nombre de archivo') || msg.includes('extensión'))
    return 'El archivo debe tener extensión .xlsx o .xls';
  if (msg.includes('tipo de contenido') || msg.includes('excel'))
    return 'Solo se aceptan archivos Excel (.xlsx, .xls)';
  if (msg.includes('tamaño máximo'))
    return 'El archivo es demasiado grande. Máximo 10 MB.';
  if (msg.includes('demasiado grande'))
    return 'El archivo es demasiado grande. Máximo 10 MB.';

  // Upload
  if (msg.includes('expirada'))
    return 'La URL de subida expiró. Solicita una nueva.';
  if (msg.includes('no subido') || msg.includes('archivo no fue encontrado'))
    return 'El archivo no se subió correctamente. Intenta de nuevo.';

  // Permisos
  if (msg.includes('permiso') || msg.includes('autorización'))
    return 'No tienes permiso para importar pacientes.';

  // Procesamiento
  if (msg.includes('formato'))
    return 'El archivo tiene un formato inválido.';
  if (msg.includes('encabezado') || msg.includes('header'))
    return 'El archivo no tiene los encabezados esperados.';
  if (msg.includes('obligatorio'))
    return 'Faltan campos obligatorios en el archivo.';

  // Genérico
  return 'Error al importar. Intenta de nuevo o contacta soporte.';
}
```

---

## F5 — Limpieza UI (eliminar Supabase del componente)

### Objetivo
`AdminImportarPacientesTab.jsx` debe dejar de tocar `supabase` directamente. Solo usa la trilogía.

### Cambios

#### Quitar imports de Supabase:
```javascript
// ❌ QUITAR
import { supabase } from '...';

// ❌ QUITAR
import { useSupabaseAuth } from '...'; // si existe
```

#### Reescribir `handleImport`:
```javascript
async function handleImport() {
  const localErrors = getValidationErrors(file, headers, rows);
  if (localErrors.length) {
    setError('El archivo tiene errores:\n' + localErrors.join('\n'));
    return;
  }

  try {
    setBusy('subiendo');

    // 1) Pedir URL al provider activo
    const uploadData = await apiCrearImportacionPacientesUploadUrl({
      file_name: file.name,
      size_bytes: file.size,
      content_type: file.type,
    });

    // 2) Subir vía provider activo (PUT a R2 o Supabase)
    await apiSubirArchivoImportacionPacientes({
      upload_url: uploadData.upload_url,
      file,
      headers: uploadData.required_headers,
    });

    // 3) Confirmar y procesar
    setBusy('procesando');
    const result = await apiConfirmarImportacionPacientes({
      importacion_id: uploadData.importacion_id,
    });

    // 4) Mostrar resultado
    setResult(result);
    setError(null);
    setFile(null);
  } catch (e) {
    const friendlyMsg = mapImportarPacientesErrorMessage(e?.message);
    setError(friendlyMsg);
  } finally {
    setBusy(null);
  }
}
```

#### Mantener (no tocar):
- `cellValue` — lógica pura de celdas XLSX
- `parseXlsxFile` — parseo local del XLSX en memoria
- `getValidationErrors` — validaciones locales
- `buildImportTemplateWorkbook` — generador de template
- Handlers UI: `handleDownloadTemplate`, `handleFileSelected`, `handleDrop`, `handleReset`

#### Marcar `apiImportarPacientes` como deprecated en JSDoc:
Si la usas en algún lugar:
```javascript
/**
 * @deprecated Usar en cambio apiCrearImportacionPacientesUploadUrl + apiSubirArchivoImportacionPacientes + apiConfirmarImportacionPacientes
 */
export const apiImportarPacientes = async ({ file }) => {
  // ... wrapper a la trilogía
};
```

### Verificación de F5
```bash
cd src/components/admin
grep -n "supabase" AdminImportarPacientesTab.jsx
# Debe devolver 0 resultados
```

---

## Resumen de archivos a cambiar

| Archivo | Cambios |
|---------|---------|
| `src/services/api/index.js` | Exportar trilogía |
| `src/services/api/providerRegistry.js` | Agregar trilogía + wrapper deprecado |
| `src/services/api/messages/imports.js` | **CREAR** — centralizar `mapImportarPacientesErrorMessage` |
| `src/services/api/providers/aspnetApiProvider.js` | Agregar 3 métodos (R2) |
| `src/services/api/providers/supabaseApiProvider.js` | Agregar 3 métodos (compat) |
| `src/components/admin/AdminImportarPacientesTab.jsx` | Reescribir `handleImport`, eliminar imports de Supabase |

---

## Testing

1. **Provider Supabase (legacy):** Importar XLSX, verificar que sigue funcionando idéntico a hoy.
2. **Provider ASP.NET (R2):** Importar XLSX, obtener upload_url, hacer PUT, confirmar, ver resultado.
3. **Errores:** Probar con archivo inválido (.txt), tamaño > 10MB, sin headers obligatorios.
4. **Permisos:** Probar con usuario sin permiso `pacientes.manage` — debe rechazar en confirm.

---

## Notas
- La trilogía es el contrato. Ambos providers lo implementan.
- El componente es agnóstico al storage.
- `mapImportarPacientesErrorMessage` debe mapear **todos** los códigos de error HTTP y de validación a mensajes UX consistentes.
- El método `apiImportarPacientes` (deprecado) queda como wrapper. No eliminarlo aún (otros consumidores pueden usarlo).
