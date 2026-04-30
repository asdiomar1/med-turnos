import { supabase } from '../../../lib/supabase'
import { captureAppException } from '../../../lib/sentry'
import { createOperationHandle, releaseOperationHandle } from '../../../lib/operationLedger'
import { API_TEXT } from '../../../constants/uiText'

const portalFunctionsBaseUrl = String(
  import.meta.env.VITE_PORTAL_FUNCTIONS_BASE_URL || `${import.meta.env.VITE_SUPABASE_URL}/functions/v1`
).replace(/\/+$/, '')
const portalFunctionsAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY
const inFlightSensitiveOperations = new Map()

function logClientError(context, error) {
  if (import.meta.env.DEV) {
    console.error(context, error)
  }
  captureAppException(error, {
    feature: 'api',
    code_path: 'services/api/providers/supabaseApiProvider.js',
    context,
  })
}

function normalizePermissionDeniedMessage(error, fallbackMessage) {
  const message = error?.message?.toLowerCase?.() || ''

  if (message.includes('no autorizado')) {
    return API_TEXT.unauthorized
  }

  if (message.includes('prohibido')) {
    return fallbackMessage
  }

  return null
}

function throwNormalizedPermissionDenied(error, fallbackMessage) {
  const message = normalizePermissionDeniedMessage(error, fallbackMessage)
  throw message ? { ...error, message } : error
}

function normalizeSensitiveRpcError(error, fallbackMessage) {
  const message = error?.message?.toLowerCase?.() || ''

  if (message.includes('idempotency key') && message.includes('distint')) {
    return API_TEXT.operation_payload_mismatch
  }

  if (message.includes('sigue procesandose') || message.includes('sigue procesándose')) {
    return API_TEXT.operation_still_processing
  }

  if (
    message.includes('ya no esta disponible') ||
    message.includes('ya no está disponible') ||
    message.includes('slot destino invalido') ||
    message.includes('bloque destino invalido') ||
    message.includes('consulta destino invalida') ||
    message.includes('consulta no esta disponible') ||
    message.includes('consulta no está disponible')
  ) {
    return API_TEXT.slot_not_available
  }

  if (
    message.includes('ya existe un turno fuera de horario') ||
    (error?.code === '23505' && message.includes('turnos_fuera_horario'))
  ) {
    return API_TEXT.duplicate_turno_fuera_horario
  }

  if (message.includes('bloques seguidos')) {
    return API_TEXT.consecutive_block_conflict
  }

  return normalizePermissionDeniedMessage(error, fallbackMessage) || fallbackMessage || error?.message || API_TEXT.operation_failed_generic
}

function shouldKeepSensitiveOperationPending(error) {
  const message = error?.message?.toLowerCase?.() || ''
  return message.includes('sigue procesandose') || message.includes('sigue procesándose')
}

async function callIdempotentRpc(
  rpcName,
  params,
  {
    operationName,
    payload,
    fallbackMessage,
    onSuccess,
  }
) {
  const handle = createOperationHandle(operationName, payload)
  const inFlightKey = `${rpcName}:${handle.fingerprint}`

  if (inFlightSensitiveOperations.has(inFlightKey)) {
    return inFlightSensitiveOperations.get(inFlightKey)
  }

  const request = (async () => {
    try {
      const { data, error } = await supabase.rpc(rpcName, {
        ...params,
        p_idempotency_key: handle.idempotencyKey,
      })

      if (error) {
        throw {
          ...error,
          message: normalizeSensitiveRpcError(error, fallbackMessage),
        }
      }

      onSuccess?.(data)
      releaseOperationHandle(handle.fingerprint)
      return { data, error: null }
    } catch (error) {
      const normalizedError = {
        ...error,
        message: normalizeSensitiveRpcError(error, fallbackMessage),
      }
      logClientError(`${rpcName}:mutation`, normalizedError)
      if (!shouldKeepSensitiveOperationPending(normalizedError)) {
        releaseOperationHandle(handle.fingerprint)
      }
      return { data: null, error: normalizedError }
    } finally {
      inFlightSensitiveOperations.delete(inFlightKey)
    }
  })()

  inFlightSensitiveOperations.set(inFlightKey, request)
  return request
}

function isHorarioNoEncontradoError(error) {
  const message = error?.message?.toLowerCase?.() || ''
  return error?.code === 'P0001' && message.includes('horario no encontrado')
}

function normalizeTextValue(value) {
  return typeof value === 'string' ? value.trim() : ''
}

function normalizeDocumentoIdentidadValue(value) {
  return normalizeTextValue(value).toUpperCase().replace(/[^A-Z0-9]/g, '')
}

function normalizeModalidadCobroValue(value) {
  return value === 'obra_social' ? 'obra_social' : 'particular'
}

async function invokePortalPublicFunction(path, body) {
  const response = await fetch(`${portalFunctionsBaseUrl}/${path}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      apikey: portalFunctionsAnonKey,
      authorization: `Bearer ${portalFunctionsAnonKey}`,
    },
    body: JSON.stringify(body),
  })

  const contentType = response.headers.get('content-type') || ''
  const isJson = contentType.includes('application/json')
  const payload = isJson ? await response.json().catch(() => null) : null

  if (!response.ok) {
    const errorMessage = payload?.error || `La solicitud al portal devolvió ${response.status}.`
    throw new Error(errorMessage)
  }

  if (payload?.error) {
    throw new Error(payload.error)
  }

  return payload?.data ?? payload ?? null
}

async function invokePortalAuthenticatedFunction(path, body, accessToken) {
  const response = await fetch(`${portalFunctionsBaseUrl}/${path}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      apikey: portalFunctionsAnonKey,
      authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(body),
  })

  const contentType = response.headers.get('content-type') || ''
  const isJson = contentType.includes('application/json')
  const payload = isJson ? await response.json().catch(() => null) : null

  if (!response.ok) {
    const errorMessage = payload?.error || `La solicitud devolvió ${response.status}.`
    throw new Error(errorMessage)
  }

  if (payload?.error) {
    throw new Error(payload.error)
  }

  return payload
}

const DEFAULT_DIAS_LABORABLES = [1, 2, 3, 4, 5]

function normalizeDiasLaborables(value) {
  const dias = Array.isArray(value) ? value : []
  return [...new Set(
    dias
      .map((day) => Number.parseInt(day, 10))
      .filter((day) => Number.isInteger(day) && day >= 0 && day <= 6)
  )].sort((a, b) => a - b)
}

function isMissingRelationError(error) {
  return error?.code === '42P01'
}

function getIsoWeekday(isoDate) {
  if (!isoDate) return null
  const [year, month, day] = String(isoDate).split('-').map(Number)
  if (!year || !month || !day) return null
  return new Date(year, month - 1, day).getDay()
}

function parseIsoDateToLocalDate(isoDate) {
  if (!isoDate) return new Date()
  const [year, month, day] = String(isoDate).split('-').map(Number)
  if (!year || !month || !day) return new Date()
  return new Date(year, month - 1, day)
}

function getWeekRangeFromIsoDate(isoDate) {
  const baseDate = parseIsoDateToLocalDate(isoDate)
  const baseDay = baseDate.getDay()
  const offsetToMonday = baseDay === 0 ? -6 : 1 - baseDay
  const monday = new Date(baseDate)
  monday.setDate(baseDate.getDate() + offsetToMonday)
  const sunday = new Date(monday)
  sunday.setDate(monday.getDate() + 6)
  return { monday, sunday }
}

function getOperativoValue(operativo = {}, snakeKey, camelKey) {
  if (operativo && Object.prototype.hasOwnProperty.call(operativo, snakeKey)) {
    return operativo[snakeKey]
  }
  return operativo?.[camelKey]
}

function toLocalIsoDate(date = new Date()) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

async function fetchHorasActivasSet() {
  const { data, error } = await supabase
    .from('horarios_config')
    .select('hora')
    .eq('activo', true)

  if (error) throw error

  return new Set((data || []).map((row) => row.hora).filter(Boolean))
}

function filterSlotsByHorasActivas(slots, horasActivas) {
  if (!horasActivas?.size) return []
  return (slots || []).filter((slot) => horasActivas.has(slot.hora))
}

export function buildDashboardOcupacion({ camaras = [], slots = [] } = {}) {
  const slotsPorCamara = new Map()

  for (const slot of slots || []) {
    if (!slot?.camara_id) continue
    const current = slotsPorCamara.get(slot.camara_id) || []
    current.push(slot)
    slotsPorCamara.set(slot.camara_id, current)
  }

  return (camaras || [])
    .filter((camara) => camara?.activa)
    .map((camara) => {
      const slotsCamara = slotsPorCamara.get(camara.id) || []
      const capacidadTotal = slotsCamara.length
      const ocupados = slotsCamara.filter((slot) => ['ocupado', 'apartado'].includes(slot.estado)).length
      const libres = slotsCamara.filter((slot) => ['libre', 'cancelado'].includes(slot.estado)).length
      const porcentajeOcupacion = capacidadTotal === 0
        ? 0
        : Math.round((ocupados / capacidadTotal) * 10000) / 100

      return {
        camara_id: camara.id,
        camara_nombre: camara.nombre,
        capacidad_total: capacidadTotal,
        ocupados,
        libres,
        porcentaje_ocupacion: porcentajeOcupacion,
      }
    })
    .sort((a, b) => a.camara_id - b.camara_id)
}

export async function triggerWhatsappDispatch(slotIds = []) {
  const normalizedSlotIds = Array.isArray(slotIds)
    ? [...new Set(slotIds.filter((value) => typeof value === 'string' && value))]
    : []

  if (!normalizedSlotIds.length) return

  const { error } = await supabase.functions.invoke('whatsapp-dispatch', {
    body: { slot_ids: normalizedSlotIds, limit: normalizedSlotIds.length },
  })

  if (error) {
    logClientError('triggerWhatsappDispatch', error)
  }
}

function mapCrearPacienteErrorMessage(message) {
  const normalized = typeof message === 'string' ? message.toLowerCase() : ''

  if (
    normalized.includes('no hay una sesión activa válida') ||
    normalized.includes('no hay una sesion activa valida')
  ) {
    return API_TEXT.create_paciente_no_active_session
  }

  if (
    normalized.includes('failed to fetch') ||
    normalized.includes('failed to send a request') ||
    normalized.includes('networkerror') ||
    normalized.includes('fetch')
  ) {
    return API_TEXT.create_paciente_connection_failed
  }

  if (normalized.includes('ya existe un paciente con ese email')) {
    return 'Ya existe un paciente con ese email.'
  }

  if (normalized.includes('ya existe un usuario con ese email')) {
    return 'Ya existe un usuario con ese email.'
  }

  if (normalized.includes('email invalido')) {
    return API_TEXT.create_paciente_invalid_email
  }

  if (normalized.includes('no autorizado')) {
    return API_TEXT.create_paciente_unauthorized_session
  }

  if (normalized.includes('prohibido')) {
    return API_TEXT.create_paciente_forbidden
  }

  if (normalized.includes('telefono requerido')) {
    return API_TEXT.create_paciente_phone_required
  }

  if (normalized.includes('documento de identidad requerido')) {
    return API_TEXT.create_paciente_document_required
  }

  if (normalized.includes('documento de identidad ya en uso')) {
    return API_TEXT.create_paciente_document_in_use
  }

  if (normalized.includes('condicion de iva requerida')) {
    return API_TEXT.create_paciente_iva_required
  }

  if (normalized.includes('condicion de iva invalida')) {
    return API_TEXT.create_paciente_iva_invalid
  }

  if (normalized.includes('obra social invalida')) {
    return API_TEXT.create_paciente_obra_social_invalid
  }

  if (normalized.includes('nacionalidad requerida')) {
    return API_TEXT.create_paciente_nationality_required
  }

  if (normalized.includes('no autorizado')) {
    return API_TEXT.create_paciente_permissions_invalid
  }

  if (normalized.includes('prohibido')) {
    return API_TEXT.create_paciente_forbidden
  }

  return API_TEXT.create_paciente_failed
}

function mapImportarPacientesErrorMessage(message) {
  const normalized = typeof message === 'string' ? message.toLowerCase() : ''

  if (
    normalized.includes('no hay una sesión activa válida') ||
    normalized.includes('no hay una sesion activa valida')
  ) {
    return API_TEXT.import_pacientes_no_active_session
  }

  if (
    normalized.includes('failed to fetch') ||
    normalized.includes('failed to send a request') ||
    normalized.includes('networkerror') ||
    normalized.includes('fetch')
  ) {
    return API_TEXT.import_pacientes_connection_failed
  }

  if (normalized.includes('no autorizado')) {
    return API_TEXT.import_pacientes_unauthorized_session
  }

  if (normalized.includes('prohibido')) {
    return API_TEXT.import_pacientes_forbidden
  }

  return API_TEXT.import_pacientes_failed
}

// -- User preferences ---------------------------------------------------------

export async function apiGetUserPreferences(userId) {
  const { data, error } = await supabase
    .from('user_preferences')
    .select('*')
    .eq('id', userId)
    .maybeSingle()
  if (error) throw error
  return data
}

export async function apiUpsertUserPreferences(userId, { theme, custom_colors, turnos_layout, font_scale }) {
  const payload = {
    id: userId,
    updated_at: new Date().toISOString(),
  }

  if (typeof theme === 'string' && theme) {
    payload.theme = theme
  }

  if (custom_colors !== undefined) {
    payload.custom_colors = custom_colors
  }

  if (turnos_layout === 'camera' || turnos_layout === 'hora') {
    payload.turnos_layout = turnos_layout
  }

  if (typeof font_scale === 'number' && font_scale > 0) {
    payload.font_scale = font_scale
  }

  const { data, error } = await supabase
    .from('user_preferences')
    .upsert(payload)
    .select()
    .single()
  if (error) throw error
  return data
}

// -- Slots: read --------------------------------------------------------------

export async function apiGetSlotsByFecha(fecha) {
  let horasActivas = new Set()

  try {
    horasActivas = await fetchHorasActivasSet()
  } catch (error) {
    logClientError('apiGetSlotsByFecha:horarios_activos', error)
  }

  const { data, error } = await supabase
    .from('slots')
    .select(`
      *,
      paciente:paciente_id(id, nombre, email, obra_social_id),
      medico:medico_id(id, nombre, activo),
      referente:referente_id(id, nombre, tipo, activo),
      camara:camara_id(id, nombre, capacidad, activa),
      obra_social:obra_social_id(id, nombre, activa, tiene_convenio),
      apartado_por_perfil:apartado_por(id, nombre),
      obra_social_validada_por_perfil:obra_social_validada_por(id, nombre)
    `)
    .eq('fecha', fecha)
    .order('hora')
    .order('camara_id')
    .order('lugar')
  if (error) logClientError('apiGetSlotsByFecha', error)
  return filterSlotsByHorasActivas(data || [], horasActivas)
}

export async function apiGetSlotsDisponiblesPacienteByFecha(fecha) {
  let horasActivas = new Set()

  try {
    horasActivas = await fetchHorasActivasSet()
  } catch (error) {
    logClientError('apiGetSlotsDisponiblesPacienteByFecha:horarios_activos', error)
  }

  const { data, error } = await supabase
    .from('slots')
    .select(`
      id, fecha, hora, camara_id, lugar, estado, paciente_id, es_tanda, es_bloque_completo, referido_tercero, modalidad_cobro, obra_social_id, numero_autorizacion,
      camara:camara_id(id, nombre, capacidad)
    `)
    .eq('fecha', fecha)
    .order('hora')
    .order('camara_id')
    .order('lugar')
  if (error) logClientError('apiGetSlotsDisponiblesPacienteByFecha', error)
  return filterSlotsByHorasActivas(data || [], horasActivas)
}

export async function apiGetSlotsByRango(fechaInicio, fechaFin) {
  let horasActivas = new Set()

  try {
    horasActivas = await fetchHorasActivasSet()
  } catch (error) {
    logClientError('apiGetSlotsByRango:horarios_activos', error)
  }

  const { data, error } = await supabase
    .from('slots')
    .select(`
      id, fecha, hora, camara_id, lugar, estado, paciente_id, es_tanda, tanda_id, es_bloque_completo, referido_tercero, referente_id, modalidad_cobro, obra_social_id, numero_autorizacion, sesiones_autorizadas, ciclo_obra_social_id, medico_id, es_nuevo_ingreso,
      paciente:paciente_id(id, nombre, obra_social_id),
      medico:medico_id(id, nombre, activo),
      referente:referente_id(id, nombre, tipo, activo),
      camara:camara_id(id, nombre, capacidad),
      obra_social:obra_social_id(id, nombre, activa, tiene_convenio),
      obra_social_validada_por,
      obra_social_validada_at,
      obra_social_validada_por_perfil:obra_social_validada_por(id, nombre)
    `)
    .gte('fecha', fechaInicio)
    .lte('fecha', fechaFin)
    .order('fecha')
    .order('hora')
    .order('camara_id')
    .order('lugar')
    .range(0, 4999)

  if (error) {
    logClientError('apiGetSlotsByRango', error)
    return {}
  }

  return filterSlotsByHorasActivas(data || [], horasActivas).reduce((acc, s) => {
    if (!acc[s.fecha]) acc[s.fecha] = []
    acc[s.fecha].push(s)
    return acc
  }, {})
}

export async function apiGetSlotsByRangoLite(fechaInicio, fechaFin) {
  let horasActivas = new Set()

  try {
    horasActivas = await fetchHorasActivasSet()
  } catch (error) {
    logClientError('apiGetSlotsByRangoLite:horarios_activos', error)
  }

  const { data, error } = await supabase
    .from('slots')
    .select('id, fecha, hora, camara_id, lugar, estado, paciente_id, tanda_id, es_bloque_completo')
    .gte('fecha', fechaInicio)
    .lte('fecha', fechaFin)
    .order('fecha')
    .order('hora')
    .order('camara_id')
    .order('lugar')
    .range(0, 4999)

  if (error) {
    logClientError('apiGetSlotsByRangoLite', error)
    return {}
  }

  return filterSlotsByHorasActivas(data || [], horasActivas).reduce((acc, s) => {
    if (!acc[s.fecha]) acc[s.fecha] = []
    acc[s.fecha].push(s)
    return acc
  }, {})
}

export async function apiGetDisponibilidadTandaMes(fechaInicio, fechaFin, pacienteId = null) {
  const { data, error } = await supabase.rpc('admin_get_disponibilidad_tanda_mes', {
    p_fecha_inicio: fechaInicio,
    p_fecha_fin: fechaFin,
    p_paciente_id: pacienteId,
  })
  if (error) {
    logClientError('apiGetDisponibilidadTandaMes', error)
    return []
  }
  return data || []
}

export async function apiGetDisponibilidadTandaDetalleMes(fechaInicio, fechaFin, pacienteId = null) {
  const { data, error } = await supabase.rpc('admin_get_disponibilidad_tanda_detalle_mes', {
    p_fecha_inicio: fechaInicio,
    p_fecha_fin: fechaFin,
    p_paciente_id: pacienteId,
  })
  if (error) {
    logClientError('apiGetDisponibilidadTandaDetalleMes', error)
    return []
  }
  return data || []
}

export async function apiGetSlotsActivosTanda(tandaId) {
  const { data, error } = await supabase
    .from('slots')
    .select(`
      id, fecha, hora, camara_id, lugar, estado, paciente_id, es_tanda, tanda_id,
      camara:camara_id(id, nombre, capacidad)
    `)
    .eq('tanda_id', tandaId)
    .eq('estado', 'ocupado')
    .order('fecha')
    .order('hora')
    .order('camara_id')
    .order('lugar')
  if (error) throw error
  return data || []
}

export async function apiGetSlotsTanda(tandaId) {
  const { data, error } = await supabase
    .from('slots')
    .select(`
      id, fecha, hora, camara_id, lugar, estado, paciente_id, es_tanda, tanda_id, es_bloque_completo,
      referido_tercero, referente_id, modalidad_cobro, obra_social_id, numero_autorizacion, sesiones_autorizadas, ciclo_obra_social_id, medico_id, es_nuevo_ingreso,
      camara:camara_id(id, nombre, capacidad),
      paciente:paciente_id(id, nombre),
      obra_social_validada_por,
      obra_social_validada_at,
      obra_social_validada_por_perfil:obra_social_validada_por(id, nombre)
    `)
    .eq('tanda_id', tandaId)
    .order('fecha')
    .order('hora')
    .order('camara_id')
    .order('lugar')

  if (error) throw error
  return data || []
}

export async function apiGetTurnosPaciente(pacienteId) {
  const hoy = toLocalIsoDate()
  const { data, error } = await supabase
    .from('slots')
    .select(`
      id, fecha, hora, camara_id, lugar, estado, paciente_id, es_tanda, tanda_id, es_bloque_completo,
      referido_tercero, referente_id, modalidad_cobro, obra_social_id, numero_autorizacion, sesiones_autorizadas, ciclo_obra_social_id, medico_id, es_nuevo_ingreso,
      obra_social_validada_por,
      obra_social_validada_at,
      camara:camara_id(id, nombre, activa),
      paciente:paciente_id(id, nombre),
      obra_social_validada_por_perfil:obra_social_validada_por(id, nombre)
    `)
    .eq('paciente_id', pacienteId)
    .eq('estado', 'ocupado')
    .gte('fecha', hoy)
    .order('fecha')
    .order('hora')
  if (error) logClientError('apiGetTurnosPaciente', error)
  return data || []
}

export async function apiGetHistorialBloque(fecha, hora, camaraId) {
  const { data, error } = await supabase
    .from('historial_bloques')
    .select(`
      id, accion, lugar, motivo, paciente_id, tanda_id, created_at, referido_tercero, referente_id, modalidad_cobro, obra_social_id, numero_autorizacion, sesiones_autorizadas, ciclo_obra_social_id, medico_id, es_nuevo_ingreso, obra_social_validada_por, obra_social_validada_at,
      paciente:paciente_id(nombre),
      medico:medico_id(nombre),
      referente:referente_id(nombre, tipo),
      obra_social:obra_social_id(nombre),
      realizado_por_perfil:realizado_por(nombre),
      obra_social_validada_por_perfil:obra_social_validada_por(nombre)
    `)
    .eq('fecha', fecha)
    .eq('hora', hora)
    .eq('camara_id', camaraId)
    .order('created_at', { ascending: false })
  if (error) logClientError('apiGetHistorialBloque', error)
  return data || []
}

// -- Pacientes: read ----------------------------------------------------------

export async function apiGetPacientes() {
  const { data, error } = await supabase
    .from('perfiles')
    .select('*')
    .eq('rol', 'paciente')
    .order('nombre')
  if (error) logClientError('apiGetPacientes', error)
  return data || []
}

export async function apiGetSesionesCompletadas(pacienteId) {
  const { data } = await supabase
    .from('sesiones')
    .select('*')
    .eq('paciente_id', pacienteId)
    .order('fecha', { ascending: false })
  return data || []
}

export async function apiGetCierreDiarioEstado(fecha) {
  const { data, error } = await supabase.rpc('admin_get_cierre_diario_estado', {
    p_fecha: fecha,
  })
  if (error) {
    if (error.code === 'P0001' && /prohibido/i.test(error.message || '')) {
      return null
    }
    throw error
  }
  return Array.isArray(data) ? data[0] || null : data || null
}

export async function apiPreviewCierreDiario(fecha) {
  const { data, error } = await supabase.rpc('admin_preview_cierre_diario', {
    p_fecha: fecha,
  })
  if (error) throw error
  return data || []
}

export async function apiConfirmarCierreDiario(fecha, detalles) {
  const { data, error } = await supabase.rpc('admin_confirmar_cierre_diario', {
    p_fecha: fecha,
    p_detalles: detalles || [],
  })
  if (error) throw error
  return data
}

export async function apiGetCierreDiarioDetalle({ fecha = null, cierreId = null } = {}) {
  const { data, error } = await supabase.rpc('admin_get_cierre_diario_detalle', {
    p_fecha: fecha,
    p_cierre_id: cierreId,
  })
  if (error) throw error
  return data || []
}

export async function apiGetCierreDiarioExport({ fecha = null, cierreId = null } = {}) {
  const { data, error } = await supabase.rpc('admin_export_cierre_diario', {
    p_fecha: fecha,
    p_cierre_id: cierreId,
  })
  if (error) throw error
  return data || []
}

export async function apiReabrirCierreDiario({ fecha = null, cierreId = null } = {}) {
  const { data, error } = await supabase.rpc('admin_reabrir_cierre_diario', {
    p_fecha: fecha,
    p_cierre_id: cierreId,
  })
  if (error) throw error
  return data
}

export async function apiGetCierreMensualExport({ anio, mes }) {
  const { data, error } = await supabase.rpc('admin_export_cierre_mensual', {
    p_anio: anio,
    p_mes: mes,
  })
  if (error) throw error
  return data || []
}

export async function apiGetDashboardResumen(fecha) {
  const { data, error } = await supabase.rpc('admin_dashboard_resumen', {
    p_fecha: fecha,
  })
  if (error) throw error
  return Array.isArray(data) ? data[0] || null : data || null
}

export async function apiGetDashboardOcupacion(fecha) {
  const [horasActivas, camaras, slots] = await Promise.all([
    fetchHorasActivasSet(),
    apiGetCamaras(),
    supabase
      .from('slots')
      .select('id, camara_id, hora, estado')
      .eq('fecha', fecha),
  ])

  if (slots.error) throw slots.error

  return buildDashboardOcupacion({
    camaras,
    slots: filterSlotsByHorasActivas(slots.data || [], horasActivas),
  })
}

export async function apiGetDashboardAgenda(fecha) {
  const { data, error } = await supabase.rpc('admin_dashboard_agenda_hoy', {
    p_fecha: fecha,
  })
  if (error) throw error
  return data || []
}

export async function apiGetDashboardAlertas(fecha) {
  const { data, error } = await supabase.rpc('admin_dashboard_alertas', {
    p_fecha: fecha,
  })
  if (error) throw error
  return data || []
}

export async function apiGetDashboardVolumenSemanal(fecha) {
  const { monday, sunday } = getWeekRangeFromIsoDate(fecha)
  const fechaInicio = toLocalIsoDate(monday)
  const fechaFin = toLocalIsoDate(sunday)

  const [diasLaborablesConfig, horasActivas, camaras, slots] = await Promise.all([
    apiGetDiasLaborablesConfig(),
    fetchHorasActivasSet(),
    apiGetCamaras(),
    supabase
      .from('slots')
      .select('id, fecha, hora, camara_id, estado, paciente_id')
      .gte('fecha', fechaInicio)
      .lte('fecha', fechaFin),
  ])

  if (slots.error) throw slots.error

  const camarasActivas = new Set(
    (camaras || [])
      .filter((camara) => camara?.activa)
      .map((camara) => camara.id)
  )

  const agregadosPorDia = new Map()
  for (let dayOffset = 0; dayOffset < 7; dayOffset += 1) {
    const date = new Date(monday)
    date.setDate(monday.getDate() + dayOffset)
    const isoDate = toLocalIsoDate(date)
    agregadosPorDia.set(isoDate, {
      dia: isoDate,
      dia_nombre: date.toLocaleDateString('en-US', { weekday: 'short' }),
      pacientesSet: new Set(),
      slots_ocupados: 0,
      capacidad_total: 0,
    })
  }

  const slotsFiltrados = filterSlotsByHorasActivas(slots.data || [], horasActivas)
  for (const slot of slotsFiltrados) {
    if (!camarasActivas.has(slot.camara_id)) continue
    const bucket = agregadosPorDia.get(slot.fecha)
    if (!bucket) continue

    bucket.capacidad_total += 1

    if (slot.estado === 'ocupado' || slot.estado === 'apartado') {
      bucket.slots_ocupados += 1
      if (slot.paciente_id) bucket.pacientesSet.add(slot.paciente_id)
    }
  }

  const diasLaborables = new Set(diasLaborablesConfig?.dias_semana || DEFAULT_DIAS_LABORABLES)
  return [...agregadosPorDia.values()]
    .map((row) => ({
      dia: row.dia,
      dia_nombre: row.dia_nombre,
      pacientes: row.pacientesSet.size,
      slots_ocupados: row.slots_ocupados,
      capacidad_total: row.capacidad_total,
    }))
    .filter((row) => diasLaborables.has(getIsoWeekday(row.dia)))
}

export async function apiGetDiasLaborablesConfig() {
  const { data, error } = await supabase
    .from('dias_laborables_config')
    .select('*')
    .eq('key', 'centro')
    .maybeSingle()

  if (error) {
    if (isMissingRelationError(error)) {
      return { key: 'centro', dias_semana: DEFAULT_DIAS_LABORABLES }
    }
    throw error
  }

  return {
    key: data?.key || 'centro',
    dias_semana: normalizeDiasLaborables(data?.dias_semana).length
      ? normalizeDiasLaborables(data?.dias_semana)
      : DEFAULT_DIAS_LABORABLES,
  }
}

export async function apiUpsertDiasLaborablesConfig(diasSemana) {
  const normalizedDias = normalizeDiasLaborables(diasSemana)

  const { data, error } = await supabase
    .from('dias_laborables_config')
    .upsert({
      key: 'centro',
      dias_semana: normalizedDias.length ? normalizedDias : DEFAULT_DIAS_LABORABLES,
      updated_at: new Date().toISOString(),
    })
    .select('*')
    .single()

  if (error) throw error

  return {
    key: data?.key || 'centro',
    dias_semana: normalizeDiasLaborables(data?.dias_semana).length
      ? normalizeDiasLaborables(data?.dias_semana)
      : DEFAULT_DIAS_LABORABLES,
  }
}

export async function apiListAdminEventFeed({
  limit = 50,
  beforeOccurredAt = null,
  beforeId = null,
  actorUserId = null,
  actionCodes = [],
  dateFrom = null,
  dateTo = null,
} = {}) {
  const normalizedActionCodes = Array.isArray(actionCodes)
    ? actionCodes.map((value) => normalizeTextValue(value)).filter(Boolean)
    : []

  const { data, error } = await supabase.rpc('admin_list_event_feed', {
    p_limit: limit,
    p_before_occurred_at: beforeOccurredAt || null,
    p_before_id: beforeId || null,
    p_actor_user_id: actorUserId || null,
    p_action_codes: normalizedActionCodes,
    p_date_from: dateFrom || null,
    p_date_to: dateTo || null,
  })
  if (error) throw error
  return data || []
}

export async function apiGetAdminEventFeedFilterOptions() {
  const { data, error } = await supabase.rpc('admin_event_feed_filter_options')
  if (error) throw error
  const payload = data || { actors: [], actions: [] }
  const actorsRaw = Array.isArray(payload.actors) ? payload.actors : []
  const byActorId = new Map()
  for (const actor of actorsRaw) {
    const id = actor?.id
    if (id == null || id === '') continue
    if (!byActorId.has(id)) byActorId.set(id, actor)
  }
  return { ...payload, actors: Array.from(byActorId.values()) }
}

// -- Camaras: read ------------------------------------------------------------

export async function apiGetCamaras() {
  const { data, error } = await supabase.from('camaras').select('*').order('id')
  if (error) logClientError('apiGetCamaras', error)
  return data || []
}

export async function apiGetHorariosConfig() {
  const { data, error } = await supabase
    .from('horarios_config')
    .select('*')
    .order('orden')
  if (error) throw error
  return data || []
}

export async function apiGetCondicionesIva() {
  const { data, error } = await supabase
    .from('condiciones_iva')
    .select('*')
    .eq('activo', true)
    .order('orden')
  if (error) throw error
  return data || []
}

export async function apiGetObrasSociales() {
  const { data, error } = await supabase
    .from('obras_sociales')
    .select('*')
    .order('orden')
    .order('nombre')
  if (error) throw error
  return data || []
}

export async function apiGetMedicos() {
  const { data, error } = await supabase
    .from('medicos')
    .select('*')
    .order('orden')
    .order('nombre')
  if (error) throw error
  return data || []
}

export async function apiGetReferentes() {
  const { data, error } = await supabase
    .from('referentes')
    .select('*')
    .order('orden')
    .order('nombre')
  if (error) throw error
  return data || []
}

export async function apiGetWhatsappMessageSettings() {
  const { data, error } = await supabase.rpc('admin_listar_whatsapp_message_settings')
  if (error) throw error
  return data || []
}

export async function apiActualizarWhatsappMessageSetting({
  key,
  message_text,
  active,
}) {
  const { data, error } = await supabase.rpc('admin_actualizar_whatsapp_message_setting', {
    p_key: normalizeTextValue(key),
    p_message_text: normalizeTextValue(message_text) || null,
    p_active: active === true,
  })
  if (error) throw error
  return data
}

export async function apiIssuePortalAccessToken(pacienteId, purpose = 'activation', deliveryChannel = 'manual') {
  const { data, error } = await supabase.functions.invoke('portal-issue-access-token', {
    body: {
      paciente_id: pacienteId,
      purpose,
      delivery_channel: deliveryChannel,
    },
  })
  if (error) throw error
  if (data?.error) throw new Error(data.error)
  return data?.data || null
}

export async function apiRequestPortalAccessRecovery(documentoIdentidad) {
  return invokePortalPublicFunction('portal-request-access-token', {
    documento_identidad: normalizeTextValue(documentoIdentidad),
  })
}

export async function apiPortalSignIn(identifier, password) {
  return invokePortalPublicFunction('portal-sign-in', {
    identifier: normalizeTextValue(identifier),
    password,
  })
}

export async function apiPortalActivateAccess(token, password) {
  return invokePortalPublicFunction('portal-activate-access', {
    token: normalizeTextValue(token),
    password,
  })
}

// -- Slots: mutations ---------------------------------------------------------

export async function apiAsignarSlot(slotId, pacienteId, esTanda = false, accion = 'asignado', operativo = {}) {
  const payload = {
    p_slot_id: slotId,
    p_paciente_id: pacienteId,
    p_es_tanda: esTanda === true,
    p_tanda_id: getOperativoValue(operativo, 'tanda_id', 'tandaId') || null,
    p_accion: accion,
    p_referido_tercero: getOperativoValue(operativo, 'referido_tercero', 'referidoTercero') === true,
    p_referente_id: getOperativoValue(operativo, 'referente_id', 'referenteId') || null,
    p_modalidad_cobro: normalizeModalidadCobroValue(getOperativoValue(operativo, 'modalidad_cobro', 'modalidadCobro')),
    p_obra_social_id: getOperativoValue(operativo, 'obra_social_id', 'obraSocialId') || null,
    p_numero_autorizacion: normalizeTextValue(getOperativoValue(operativo, 'numero_autorizacion', 'numeroAutorizacion')) || null,
    p_sesiones_autorizadas: Number.isFinite(Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas')))
      ? Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas'))
      : null,
    p_ciclo_obra_social_id: getOperativoValue(operativo, 'ciclo_obra_social_id', 'cicloObraSocialId') || null,
    p_iniciar_nuevo_ciclo_obra_social: getOperativoValue(operativo, 'iniciar_nuevo_ciclo_obra_social', 'iniciarNuevoCicloObraSocial') === true,
    p_convenio_corroborado: getOperativoValue(operativo, 'convenio_corroborado', 'convenioCorroborado') === true,
    p_medico_id: getOperativoValue(operativo, 'medico_id', 'medicoId') || null,
    p_es_nuevo_ingreso: getOperativoValue(operativo, 'es_nuevo_ingreso', 'esNuevoIngreso') === true,
    p_es_monoxido: getOperativoValue(operativo, 'es_monoxido', 'esMonoxido') === true,
    p_monoxido_orden_medica: getOperativoValue(operativo, 'monoxido_orden_medica', 'monoxidoOrdenMedica') === true,
    p_monoxido_resumen_clinico: getOperativoValue(operativo, 'monoxido_resumen_clinico', 'monoxidoResumenClinico') === true,
  }

  return callIdempotentRpc('admin_asignar_slot', payload, {
    operationName: 'admin_asignar_slot',
    payload,
    fallbackMessage: API_TEXT.unauthorized_assign_turnos,
    onSuccess: (data) => {
      if (data?.id && esTanda !== true) {
        triggerWhatsappDispatch([data.id]).catch(() => {})
      }
    },
  })
}

export async function apiPacienteReservarSlot(slotId) {
  const payload = { p_slot_id: slotId }
  return callIdempotentRpc('paciente_reservar_slot', payload, {
    operationName: 'paciente_reservar_slot',
    payload,
    fallbackMessage: API_TEXT.reserve_turno_failed,
    onSuccess: (data) => {
      if (data?.id) {
        triggerWhatsappDispatch([data.id]).catch(() => {})
      }
    },
  })
}

export async function apiCancelarSlot(slotId, motivo = null) {
  const payload = {
    p_slot_id: slotId,
    p_motivo: normalizeTextValue(motivo) || null,
  }
  return callIdempotentRpc('admin_cancelar_slot_idempotent', payload, {
    operationName: 'admin_cancelar_slot',
    payload,
    fallbackMessage: API_TEXT.unauthorized_cancel_turnos,
    onSuccess: (data) => {
      if (data?.id) {
        triggerWhatsappDispatch([data.id]).catch(() => {})
      }
    },
  })
}

export async function apiPacienteCancelarSlot(slotId) {
  const payload = { p_slot_id: slotId }
  return callIdempotentRpc('paciente_cancelar_slot', payload, {
    operationName: 'paciente_cancelar_slot',
    payload,
    fallbackMessage: API_TEXT.cancel_turno_failed,
    onSuccess: (data) => {
      if (data?.id) {
        triggerWhatsappDispatch([data.id]).catch(() => {})
      }
    },
  })
}

export async function apiCancelarBloqueCompleto(fecha, hora, camaraId, pacienteId, motivo = null) {
  const payload = {
    p_fecha: fecha,
    p_hora: hora,
    p_camara_id: camaraId,
    p_paciente_id: pacienteId,
    p_motivo: normalizeTextValue(motivo) || null,
  }
  return callIdempotentRpc('admin_cancelar_bloque_completo_idempotent', payload, {
    operationName: 'admin_cancelar_bloque_completo',
    payload,
    fallbackMessage: API_TEXT.unauthorized_cancel_bloques,
    onSuccess: (data) => {
      const slotIds = Array.isArray(data) ? data.map((slot) => slot?.id).filter(Boolean) : []
      triggerWhatsappDispatch(slotIds).catch(() => {})
    },
  })
}

export async function apiCancelarTanda(tandaId, motivo = null) {
  const payload = {
    p_tanda_id: tandaId,
    p_motivo: normalizeTextValue(motivo) || null,
  }
  return callIdempotentRpc('admin_cancelar_tanda_idempotent', payload, {
    operationName: 'admin_cancelar_tanda',
    payload,
    fallbackMessage: API_TEXT.unauthorized_cancel_tandas,
    onSuccess: (data) => {
      const slotIds = Array.isArray(data) ? data.map((slot) => slot?.id).filter(Boolean) : []
      triggerWhatsappDispatch(slotIds).catch(() => {})
    },
  })
}

export async function apiAsignarBloqueCompleto(fecha, hora, camaraId, pacienteId, operativo = {}) {
  const payload = {
    p_fecha: fecha,
    p_hora: hora,
    p_camara_id: camaraId,
    p_paciente_id: pacienteId,
    p_es_tanda: getOperativoValue(operativo, 'es_tanda', 'esTanda') === true,
    p_tanda_id: getOperativoValue(operativo, 'tanda_id', 'tandaId') || null,
    p_referido_tercero: getOperativoValue(operativo, 'referido_tercero', 'referidoTercero') === true,
    p_referente_id: getOperativoValue(operativo, 'referente_id', 'referenteId') || null,
    p_modalidad_cobro: normalizeModalidadCobroValue(getOperativoValue(operativo, 'modalidad_cobro', 'modalidadCobro')),
    p_obra_social_id: getOperativoValue(operativo, 'obra_social_id', 'obraSocialId') || null,
    p_numero_autorizacion: normalizeTextValue(getOperativoValue(operativo, 'numero_autorizacion', 'numeroAutorizacion')) || null,
    p_sesiones_autorizadas: Number.isFinite(Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas')))
      ? Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas'))
      : null,
    p_ciclo_obra_social_id: getOperativoValue(operativo, 'ciclo_obra_social_id', 'cicloObraSocialId') || null,
    p_iniciar_nuevo_ciclo_obra_social: getOperativoValue(operativo, 'iniciar_nuevo_ciclo_obra_social', 'iniciarNuevoCicloObraSocial') === true,
    p_convenio_corroborado: getOperativoValue(operativo, 'convenio_corroborado', 'convenioCorroborado') === true,
    p_medico_id: getOperativoValue(operativo, 'medico_id', 'medicoId') || null,
    p_es_nuevo_ingreso: getOperativoValue(operativo, 'es_nuevo_ingreso', 'esNuevoIngreso') === true,
    p_es_monoxido: getOperativoValue(operativo, 'es_monoxido', 'esMonoxido') === true,
    p_monoxido_orden_medica: getOperativoValue(operativo, 'monoxido_orden_medica', 'monoxidoOrdenMedica') === true,
    p_monoxido_resumen_clinico: getOperativoValue(operativo, 'monoxido_resumen_clinico', 'monoxidoResumenClinico') === true,
  }
  return callIdempotentRpc('admin_asignar_bloque_completo', payload, {
    operationName: 'admin_asignar_bloque_completo',
    payload,
    fallbackMessage: API_TEXT.unauthorized_reserve_bloques,
    onSuccess: (data) => {
      if (getOperativoValue(operativo, 'es_tanda', 'esTanda') !== true) {
        const slotIds = Array.isArray(data) ? data.map((slot) => slot?.id).filter(Boolean) : []
        triggerWhatsappDispatch(slotIds).catch(() => {})
      }
    },
  })
}

export async function apiApartarSlot(slotId, pacienteId, esMonoxido = false) {
  const payload = {
    p_slot_id: slotId,
    p_paciente_id: pacienteId || null,
    p_es_monoxido: esMonoxido === true,
  }
  return callIdempotentRpc('admin_apartar_slot', payload, {
    operationName: 'admin_apartar_slot',
    payload,
    fallbackMessage: API_TEXT.unauthorized_apartar_slots,
  })
}

export async function apiConfirmarApartado(slotId, pacienteId, operativo = {}) {
  const payload = {
    p_slot_id: slotId,
    p_paciente_id: pacienteId || null,
    p_referido_tercero: getOperativoValue(operativo, 'referido_tercero', 'referidoTercero') === true,
    p_referente_id: getOperativoValue(operativo, 'referente_id', 'referenteId') || null,
    p_modalidad_cobro: normalizeModalidadCobroValue(getOperativoValue(operativo, 'modalidad_cobro', 'modalidadCobro')),
    p_obra_social_id: getOperativoValue(operativo, 'obra_social_id', 'obraSocialId') || null,
    p_numero_autorizacion: normalizeTextValue(getOperativoValue(operativo, 'numero_autorizacion', 'numeroAutorizacion')) || null,
    p_sesiones_autorizadas: Number.isFinite(Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas')))
      ? Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas'))
      : null,
    p_ciclo_obra_social_id: getOperativoValue(operativo, 'ciclo_obra_social_id', 'cicloObraSocialId') || null,
    p_iniciar_nuevo_ciclo_obra_social: getOperativoValue(operativo, 'iniciar_nuevo_ciclo_obra_social', 'iniciarNuevoCicloObraSocial') === true,
    p_convenio_corroborado: getOperativoValue(operativo, 'convenio_corroborado', 'convenioCorroborado') === true,
    p_medico_id: getOperativoValue(operativo, 'medico_id', 'medicoId') || null,
    p_es_nuevo_ingreso: getOperativoValue(operativo, 'es_nuevo_ingreso', 'esNuevoIngreso') === true,
    p_es_monoxido: getOperativoValue(operativo, 'es_monoxido', 'esMonoxido') === true,
    p_monoxido_orden_medica: getOperativoValue(operativo, 'monoxido_orden_medica', 'monoxidoOrdenMedica') === true,
    p_monoxido_resumen_clinico: getOperativoValue(operativo, 'monoxido_resumen_clinico', 'monoxidoResumenClinico') === true,
  }
  return callIdempotentRpc('admin_confirmar_apartado', payload, {
    operationName: 'admin_confirmar_apartado',
    payload,
    fallbackMessage: API_TEXT.unauthorized_confirmar_apartados,
    onSuccess: (data) => {
      if (data?.id) {
        triggerWhatsappDispatch([data.id]).catch(() => {})
      }
    },
  })
}

export async function apiLiberarApartado(slotId) {
  const { data, error } = await supabase.rpc('admin_liberar_apartado', {
    p_slot_id: slotId,
  })
  return { data, error }
}

export async function apiActualizarDatosOperativosSlot(slotId, operativo = {}) {
  const { data, error } = await supabase.rpc('admin_actualizar_datos_operativos_slot', {
    p_slot_id: slotId,
    p_referido_tercero: getOperativoValue(operativo, 'referido_tercero', 'referidoTercero') === true,
    p_referente_id: getOperativoValue(operativo, 'referente_id', 'referenteId') || null,
    p_modalidad_cobro: normalizeModalidadCobroValue(getOperativoValue(operativo, 'modalidad_cobro', 'modalidadCobro')),
    p_obra_social_id: getOperativoValue(operativo, 'obra_social_id', 'obraSocialId') || null,
    p_numero_autorizacion: normalizeTextValue(getOperativoValue(operativo, 'numero_autorizacion', 'numeroAutorizacion')) || null,
    p_sesiones_autorizadas: Number.isFinite(Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas')))
      ? Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas'))
      : null,
    p_ciclo_obra_social_id: getOperativoValue(operativo, 'ciclo_obra_social_id', 'cicloObraSocialId') || null,
    p_iniciar_nuevo_ciclo_obra_social: getOperativoValue(operativo, 'iniciar_nuevo_ciclo_obra_social', 'iniciarNuevoCicloObraSocial') === true,
    p_convenio_corroborado: getOperativoValue(operativo, 'convenio_corroborado', 'convenioCorroborado') === true,
    p_medico_id: getOperativoValue(operativo, 'medico_id', 'medicoId') || null,
    p_es_nuevo_ingreso: getOperativoValue(operativo, 'es_nuevo_ingreso', 'esNuevoIngreso') === true,
    p_es_monoxido: getOperativoValue(operativo, 'es_monoxido', 'esMonoxido') === true,
    p_monoxido_orden_medica: getOperativoValue(operativo, 'monoxido_orden_medica', 'monoxidoOrdenMedica') === true,
    p_monoxido_resumen_clinico: getOperativoValue(operativo, 'monoxido_resumen_clinico', 'monoxidoResumenClinico') === true,
  })
  return { data, error }
}

export async function apiActualizarDatosOperativosTanda(tandaId, operativo = {}) {
  const { data, error } = await supabase.rpc('admin_actualizar_datos_operativos_tanda', {
    p_tanda_id: tandaId,
    p_referido_tercero: getOperativoValue(operativo, 'referido_tercero', 'referidoTercero') === true,
    p_referente_id: getOperativoValue(operativo, 'referente_id', 'referenteId') || null,
    p_modalidad_cobro: normalizeModalidadCobroValue(getOperativoValue(operativo, 'modalidad_cobro', 'modalidadCobro')),
    p_obra_social_id: getOperativoValue(operativo, 'obra_social_id', 'obraSocialId') || null,
    p_numero_autorizacion: normalizeTextValue(getOperativoValue(operativo, 'numero_autorizacion', 'numeroAutorizacion')) || null,
    p_sesiones_autorizadas: Number.isFinite(Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas')))
      ? Number(getOperativoValue(operativo, 'sesiones_autorizadas', 'sesionesAutorizadas'))
      : null,
    p_ciclo_obra_social_id: getOperativoValue(operativo, 'ciclo_obra_social_id', 'cicloObraSocialId') || null,
    p_iniciar_nuevo_ciclo_obra_social: getOperativoValue(operativo, 'iniciar_nuevo_ciclo_obra_social', 'iniciarNuevoCicloObraSocial') === true,
    p_convenio_corroborado: getOperativoValue(operativo, 'convenio_corroborado', 'convenioCorroborado') === true,
    p_medico_id: getOperativoValue(operativo, 'medico_id', 'medicoId') || null,
    p_es_nuevo_ingreso: getOperativoValue(operativo, 'es_nuevo_ingreso', 'esNuevoIngreso') === true,
  })
  return { data, error }
}

export async function apiReprogramarSlotTanda(slotId, targetSlotId) {
  const payload = {
    p_slot_id: slotId,
    p_target_slot_id: targetSlotId,
  }
  return callIdempotentRpc('admin_reprogramar_slot_tanda', payload, {
    operationName: 'admin_reprogramar_slot_tanda',
    payload,
    fallbackMessage: API_TEXT.unauthorized_reprogramar_tanda,
  })
}

export async function apiReprogramarSlot(slotId, targetSlotId) {
  const payload = {
    p_slot_id: slotId,
    p_target_slot_id: targetSlotId,
  }
  return callIdempotentRpc('admin_reprogramar_slot', payload, {
    operationName: 'admin_reprogramar_slot',
    payload,
    fallbackMessage: API_TEXT.unauthorized_reprogramar_turnos,
  })
}

export async function apiReprogramarBloqueTanda(slotId, targetSlotId) {
  const payload = {
    p_slot_id: slotId,
    p_target_slot_id: targetSlotId,
  }
  return callIdempotentRpc('admin_reprogramar_bloque_tanda', payload, {
    operationName: 'admin_reprogramar_bloque_tanda',
    payload,
    fallbackMessage: API_TEXT.unauthorized_reprogramar_bloques_tanda,
  })
}

export async function apiRegistrarHistorial(entradas) {
  if (!entradas || entradas.length === 0) return
  const sanitizedEntries = entradas.map((entrada) => ({
    fecha: entrada.fecha,
    hora: entrada.hora,
    camara_id: entrada.camara_id ?? null,
    slot_id: entrada.slot_id ?? null,
    lugar: entrada.lugar ?? null,
    accion: entrada.accion,
    paciente_id: entrada.paciente_id ?? null,
    motivo: normalizeTextValue(entrada.motivo) || null,
  }))
  const { error } = await supabase.rpc('admin_registrar_historial_bloques', {
    p_entradas: sanitizedEntries,
  })
  if (error) {
    logClientError('apiRegistrarHistorial', error)
    throw error
  }
}

// -- Notas de paciente --------------------------------------------------------

export async function apiGetNotasPaciente(pacienteId) {
  const { data, error } = await supabase
    .from('notas_paciente')
    .select('id, mensaje, created_at, autor:autor_id(nombre)')
    .eq('paciente_id', pacienteId)
    .order('created_at', { ascending: false })
  if (error) logClientError('apiGetNotasPaciente', error)
  return data || []
}

export async function apiCrearNotaPaciente({ pacienteId, mensaje }) {
  const { data, error } = await supabase.rpc('admin_crear_nota_paciente', {
    p_paciente_id: pacienteId,
    p_mensaje: normalizeTextValue(mensaje),
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_crear_notas_paciente)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiEliminarNotaPaciente(id) {
  const { error } = await supabase.rpc('admin_eliminar_nota_paciente', {
    p_nota_id: id,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_eliminar_notas_paciente)
    throw message ? { ...error, message } : error
  }
}

// -- Pacientes: mutations -----------------------------------------------------

export async function apiActualizarPaciente(
  id,
  {
    email,
    telefono,
    documento_identidad,
    nacionalidad,
    condicion_iva_id,
    obra_social_id,
    numero_credencial_obra_social,
    claustrofobico,
    notas,
    datos_extra,
    opt_in_whatsapp,
    opt_in_source,
  }
) {
  const payload = {
    p_paciente_id: id,
    p_email: normalizeTextValue(email) || null,
    p_telefono: normalizeTextValue(telefono) || null,
    p_documento_identidad: normalizeTextValue(documento_identidad) || null,
    p_nacionalidad: normalizeTextValue(nacionalidad) || null,
    p_condicion_iva_id: condicion_iva_id || null,
    p_obra_social_id: obra_social_id || null,
    p_numero_credencial_obra_social: normalizeTextValue(numero_credencial_obra_social) || null,
    p_claustrofobico: claustrofobico === true,
    p_datos_extra: datos_extra || {},
    p_opt_in_whatsapp: opt_in_whatsapp === true,
    p_opt_in_source: normalizeTextValue(opt_in_source) || null,
    p_actualizar_notas: notas !== undefined,
  }
  if (notas !== undefined) {
    payload.p_notas = normalizeTextValue(notas) || null
  }
  const { data, error } = await supabase.rpc('admin_actualizar_paciente', payload)
  if (error) {
    const isMissingCredencialParam =
      error?.code === 'PGRST202' &&
      String(error?.message || '').toLowerCase().includes('p_numero_credencial_obra_social')
    if (isMissingCredencialParam) {
      throw new Error(API_TEXT.missing_supabase_migration_credencial_obra_social)
    }
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_editar_pacientes)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiCrearPaciente({
  nombre,
  email,
  telefono,
  documento_identidad,
  login_identifier,
  nacionalidad,
  condicion_iva_id,
  obra_social_id,
  claustrofobico,
  notas,
  datos_extra,
  portal_habilitado,
  opt_in_whatsapp,
  opt_in_source,
}) {
  const { data: { session } } = await supabase.auth.getSession()
  if (!session?.access_token) {
    return {
      error: {
        message: API_TEXT.create_paciente_no_active_session_retry,
      },
    }
  }

  const { data, error } = await supabase.functions.invoke('crear-paciente', {
    body: {
      nombre,
      email,
      telefono,
      documento_identidad,
      login_identifier,
      nacionalidad,
      condicion_iva_id,
      obra_social_id,
      claustrofobico,
      notas,
      datos_extra,
      portal_habilitado: portal_habilitado === true,
      opt_in_whatsapp: opt_in_whatsapp === true,
      opt_in_source: normalizeTextValue(opt_in_source) || null,
    },
  })
  if (error) {
    logClientError('apiCrearPaciente', error)
    const details = typeof error.context?.json === 'function'
      ? await error.context.json().catch(() => null)
      : null
    const rawMessage = error.message?.includes('Failed to send a request')
      ? API_TEXT.create_paciente_contact_function_failed
      : details?.error || error.message
    const message = mapCrearPacienteErrorMessage(rawMessage)
    return { error: { ...error, message } }
  }
  if (data?.error) {
    logClientError('apiCrearPaciente', data.error)
    return { error: { message: mapCrearPacienteErrorMessage(data.error) } }
  }
  return { data: data.data, error: null }
}

export async function apiEliminarPaciente(pacienteId) {
  const { data: { session } } = await supabase.auth.getSession()
  if (!session?.access_token) {
    throw new Error(API_TEXT.eliminar_paciente_no_active_session)
  }

  const { data, error } = await supabase.functions.invoke('eliminar-paciente', {
    body: { paciente_id: pacienteId },
  })

  if (error) {
    logClientError('apiEliminarPaciente', error)
    const details = typeof error.context?.json === 'function'
      ? await error.context.json().catch(() => null)
      : null
    const rawMessage = details?.error || error.message
    const messageText = typeof rawMessage === 'string' ? rawMessage : API_TEXT.operation_failed_generic
    if (messageText === 'Prohibido' || /403|forbidden/i.test(String(error.message || ''))) {
      throw new Error(API_TEXT.unauthorized_eliminar_pacientes)
    }
    if (String(error.message || '').toLowerCase().includes('failed to send a request')) {
      throw new Error(API_TEXT.create_paciente_contact_function_failed)
    }
    throw new Error(messageText)
  }

  if (data?.error) {
    const msg = typeof data.error === 'string' ? data.error : API_TEXT.operation_failed_generic
    if (msg === 'Prohibido') {
      throw new Error(API_TEXT.unauthorized_eliminar_pacientes)
    }
    throw new Error(msg)
  }

  return data
}

export async function apiImportarPacientes({ storage_path, file_name }) {
  const { data: { session } } = await supabase.auth.getSession()
  if (!session?.access_token) {
    return {
      error: {
        message: API_TEXT.import_pacientes_no_active_session_retry,
      },
    }
  }

  try {
    const payload = await invokePortalAuthenticatedFunction(
      'importar-pacientes',
      {
        storage_path,
        file_name,
      },
      session.access_token
    )

    const data = payload?.data ?? payload

    if (data?.error) {
      logClientError('apiImportarPacientes', data.error)
      return { error: { message: mapImportarPacientesErrorMessage(data.error) } }
    }

    return { data, error: null }
  } catch (error) {
    logClientError('apiImportarPacientes', error)
    const message = mapImportarPacientesErrorMessage(error?.message || 'Error inesperado')
    return { error: { ...error, message } }
  }
}

export async function apiConfigurarPortalPaciente(pacienteId, portalHabilitado) {
  const { data, error } = await supabase.rpc('admin_configurar_portal_paciente', {
    p_paciente_id: pacienteId,
    p_portal_habilitado: portalHabilitado === true,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_modificar_portal_paciente)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiPermitirResetPortalPaciente(pacienteId) {
  const { data, error } = await supabase.rpc('admin_permitir_reset_portal_paciente', {
    p_paciente_id: pacienteId,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_habilitar_reset_portal)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiActualizarMisDatosPaciente({ nombre, email, telefono }) {
  const { data, error } = await supabase.rpc('paciente_actualizar_mis_datos', {
    p_nombre: normalizeTextValue(nombre) || null,
    p_email: normalizeTextValue(email) || null,
    p_telefono: normalizeTextValue(telefono) || null,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_actualizar_mis_datos)
  return data
}

export async function apiActualizarMisDatosStaff({ nombre }) {
  const { data, error } = await supabase.rpc('me_actualizar_mis_datos', {
    p_nombre: normalizeTextValue(nombre) || null,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_actualizar_mis_datos)
  return data
}

// -- Camaras: mutations -------------------------------------------------------

export async function apiCrearCamara({ nombre, capacidad }) {
  const { data, error } = await supabase.rpc('admin_crear_camara', {
    p_nombre: normalizeTextValue(nombre),
    p_capacidad: Number.isFinite(capacidad) ? capacidad : Number.parseInt(capacidad, 10),
  })
  if (error) {
    logClientError('apiCrearCamara', error)
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_create_cameras)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiActualizarCamara(id, { nombre, capacidad }) {
  const { data, error } = await supabase.rpc('admin_actualizar_camara', {
    p_camara_id: id,
    p_nombre: normalizeTextValue(nombre),
    p_capacidad: Number.isFinite(capacidad) ? capacidad : Number.parseInt(capacidad, 10),
  })
  if (error) {
    logClientError('apiActualizarCamara', error)
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_edit_cameras)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiToggleActiva(id, activa) {
  const { data, error } = await supabase.rpc('admin_toggle_camara_activa', {
    p_camara_id: id,
    p_activa: activa === true,
  })
  if (error) {
    logClientError('apiToggleActiva', error)
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_state_cameras)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiToggleHorarioConfig(id, activo) {
  const { data, error } = await supabase.rpc('admin_toggle_horario_config', {
    p_horario_id: id,
    p_activo: activo === true,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_cambiar_estado_horarios)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiCrearHorarioConfig({ hora, orden }) {
  const parsedOrden = Number.isFinite(orden) ? orden : Number.parseInt(orden, 10)
  const { data, error } = await supabase.rpc('admin_crear_horario_config', {
    p_hora: normalizeTextValue(hora),
    p_orden: parsedOrden,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_crear_horarios)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiActualizarHorarioConfig({ id, hora, orden }) {
  const parsedOrden = Number.isFinite(orden) ? orden : Number.parseInt(orden, 10)
  const { data, error } = await supabase.rpc('admin_actualizar_horario_config', {
    p_horario_id: id,
    p_hora: normalizeTextValue(hora),
    p_orden: parsedOrden,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_editar_horarios)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiPreviewEliminarHorarioConfig(id) {
  const { data, error } = await supabase.rpc('admin_preview_eliminar_horario_config', {
    p_horario_id: id,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_eliminar_horarios)
  return data || null
}

export async function apiEliminarHorarioConfig(id, resoluciones = [], motivo = null) {
  const { data, error } = await supabase.rpc('admin_eliminar_horario_config', {
    p_horario_id: id,
    p_resoluciones: resoluciones,
    p_motivo: normalizeTextValue(motivo) || null,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_eliminar_horarios)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiAperturarTurnosFecha(fecha) {
  const { data, error } = await supabase.rpc('admin_generar_slots_dia', {
    p_fecha: fecha,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_abrir_turnos)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiRepararSlotsRango(fechaInicio, fechaFin) {
  const { data, error } = await supabase.rpc('admin_reparar_slots_rango', {
    p_fecha_inicio: fechaInicio,
    p_fecha_fin: fechaFin,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_reparar_slots)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiGetConsultasHorariosConfig() {
  const { data, error } = await supabase
    .from('consultas_horarios_config')
    .select('*')
    .order('orden')
  if (error) throw error
  return data || []
}

export async function apiToggleConsultaHorarioConfig(id, activo) {
  const { data, error } = await supabase.rpc('admin_toggle_consulta_horario_config', {
    p_horario_id: id,
    p_activo: activo === true,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_gestionar_horarios_consulta)
  return data
}

export async function apiCrearConsultaHorarioConfig({ hora, orden }) {
  const parsedOrden = Number.isFinite(orden) ? orden : Number.parseInt(orden, 10)
  const { data, error } = await supabase.rpc('admin_crear_consulta_horario_config', {
    p_hora: normalizeTextValue(hora),
    p_orden: parsedOrden,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_crear_horarios_consulta)
  return data
}

export async function apiActualizarConsultaHorarioConfig({ id, hora, orden }) {
  const parsedOrden = Number.isFinite(orden) ? orden : Number.parseInt(orden, 10)
  const { data, error } = await supabase.rpc('admin_actualizar_consulta_horario_config', {
    p_horario_id: id,
    p_hora: normalizeTextValue(hora),
    p_orden: parsedOrden,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_editar_horarios_consulta)
  return data
}

export async function apiPreviewEliminarConsultaHorarioConfig(id) {
  const { data, error } = await supabase.rpc('admin_preview_eliminar_consulta_horario_config', {
    p_horario_id: id,
  })
  if (error && isHorarioNoEncontradoError(error)) {
    return null
  }
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_eliminar_horarios_consulta)
  return data || null
}

export async function apiEliminarConsultaHorarioConfig(id, resoluciones = [], motivo = null) {
  const { data, error } = await supabase.rpc('admin_eliminar_consulta_horario_config', {
    p_horario_id: id,
    p_resoluciones: resoluciones,
    p_motivo: normalizeTextValue(motivo) || null,
  })
  if (error && isHorarioNoEncontradoError(error)) {
    return null
  }
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_eliminar_horarios_consulta)
  return data
}

export async function apiGenerarConsultasFecha(fecha) {
  const { data, error } = await supabase.rpc('admin_generar_consultas_slots_dia', {
    p_fecha: fecha,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_generar_turnos_consulta)
  return data
}

export async function apiRepararConsultasRango(fechaInicio, fechaFin) {
  const { data, error } = await supabase.rpc('admin_reparar_consultas_slots_rango', {
    p_fecha_inicio: fechaInicio,
    p_fecha_fin: fechaFin,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_reparar_turnos_consulta)
  return data
}

export async function apiGetConsultasByFecha(fecha) {
  const { data, error } = await supabase
    .from('consultas_slots')
    .select(`
      *,
      paciente:paciente_id(id, nombre, documento_identidad),
      medico:medico_id(id, nombre, activo),
      confirmado_por_perfil:confirmado_por(id, nombre),
      cerrado_por_perfil:cerrado_por(id, nombre)
    `)
    .eq('fecha', fecha)
    .order('hora')
  if (error) {
    logClientError('apiGetConsultasByFecha', error)
    return []
  }
  return data || []
}

export async function apiGetConsultasByRango(fechaInicio, fechaFin) {
  const { data, error } = await supabase
    .from('consultas_slots')
    .select(`
      *,
      paciente:paciente_id(id, nombre, documento_identidad),
      medico:medico_id(id, nombre, activo),
      confirmado_por_perfil:confirmado_por(id, nombre),
      cerrado_por_perfil:cerrado_por(id, nombre)
    `)
    .gte('fecha', fechaInicio)
    .lte('fecha', fechaFin)
    .order('fecha')
    .order('hora')
    .range(0, 4999)
  if (error) {
    logClientError('apiGetConsultasByRango', error)
    return []
  }
  return data || []
}

export async function apiAsignarConsulta(slotId, pacienteId, medicoId, observaciones = null) {
  const normalizedObservaciones = normalizeTextValue(observaciones) || null
  const { data, error } = await callIdempotentRpc('admin_asignar_consulta_idempotent', {
    p_slot_id: slotId,
    p_paciente_id: pacienteId,
    p_medico_id: medicoId,
    p_observaciones_admin: normalizedObservaciones,
  }, {
    operationName: 'admin_asignar_consulta',
    payload: {
      slotId,
      pacienteId,
      medicoId,
      observaciones: normalizedObservaciones,
    },
    fallbackMessage: API_TEXT.unauthorized_asignar_consultas,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_asignar_consultas)
  return data
}

export async function apiCancelarConsulta(slotId, motivo = null) {
  const normalizedMotivo = normalizeTextValue(motivo) || null
  const { data, error } = await callIdempotentRpc('admin_cancelar_consulta_idempotent', {
    p_slot_id: slotId,
    p_motivo: normalizedMotivo,
  }, {
    operationName: 'admin_cancelar_consulta',
    payload: {
      slotId,
      motivo: normalizedMotivo,
    },
    fallbackMessage: API_TEXT.unauthorized_cancelar_consultas,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_cancelar_consultas)
  return data
}

export async function apiReprogramarConsulta(slotId, targetSlotId, medicoId = null) {
  const normalizedMedicoId = medicoId || null
  const { data, error } = await callIdempotentRpc('admin_reprogramar_consulta', {
    p_slot_id: slotId,
    p_target_slot_id: targetSlotId,
    p_medico_id: normalizedMedicoId,
  }, {
    operationName: 'admin_reprogramar_consulta',
    payload: {
      slotId,
      targetSlotId,
      medicoId: normalizedMedicoId,
    },
    fallbackMessage: API_TEXT.unauthorized_reprogramar_consultas,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_reprogramar_consultas)
  return data
}

export async function apiCerrarConsulta(slotId, { estado, titulo, nota, diagnostico_impresion, indicaciones }) {
  const normalizedEstado = normalizeTextValue(estado)
  const normalizedTitulo = normalizeTextValue(titulo) || null
  const normalizedNota = normalizeTextValue(nota) || null
  const normalizedDiagnostico = normalizeTextValue(diagnostico_impresion) || null
  const normalizedIndicaciones = normalizeTextValue(indicaciones) || null
  const { data, error } = await callIdempotentRpc('admin_cerrar_consulta_idempotent', {
    p_slot_id: slotId,
    p_estado: normalizedEstado,
    p_titulo: normalizedTitulo,
    p_nota: normalizedNota,
    p_diagnostico_impresion: normalizedDiagnostico,
    p_indicaciones: normalizedIndicaciones,
  }, {
    operationName: 'admin_cerrar_consulta',
    payload: {
      slotId,
      estado: normalizedEstado,
      titulo: normalizedTitulo,
      nota: normalizedNota,
      diagnostico_impresion: normalizedDiagnostico,
      indicaciones: normalizedIndicaciones,
    },
    fallbackMessage: API_TEXT.unauthorized_cerrar_consultas,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_cerrar_consultas)
  return data
}
export async function apiGetHistoriaClinica(pacienteId) {
  const { data, error } = await supabase
    .from('historias_clinicas')
    .select('paciente_id, numero, antecedentes, alergias, medicacion_actual, observaciones_relevantes, created_at, updated_at')
    .eq('paciente_id', pacienteId)
    .maybeSingle()
  if (error) throw error
  return data || null
}

export async function apiGetHistoriasClinicasResumen() {
  const { data, error } = await supabase
    .from('historias_clinicas')
    .select('paciente_id, numero')
  if (error) throw error
  return data || []
}

export async function apiGetHistoriaClinicaEvoluciones(pacienteId) {
  const { data, error } = await supabase
    .from('historia_clinica_evoluciones')
    .select(`
      id, paciente_id, consulta_slot_id, medico_id, autor_perfil_id,
      fecha_clinica, titulo, nota, diagnostico_impresion, indicaciones,
      created_at, updated_at,
      medico:medico_id(id, nombre, activo),
      autor:autor_perfil_id(id, nombre)
    `)
    .eq('paciente_id', pacienteId)
    .order('fecha_clinica', { ascending: false })
    .order('created_at', { ascending: false })
  if (error) throw error
  return data || []
}

export async function apiActualizarHistoriaClinica(
  pacienteId,
  { antecedentes, alergias, medicacion_actual, observaciones_relevantes }
) {
  const { data, error } = await supabase.rpc('admin_actualizar_historia_clinica', {
    p_paciente_id: pacienteId,
    p_antecedentes: normalizeTextValue(antecedentes) || null,
    p_alergias: normalizeTextValue(alergias) || null,
    p_medicacion_actual: normalizeTextValue(medicacion_actual) || null,
    p_observaciones_relevantes: normalizeTextValue(observaciones_relevantes) || null,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_editar_ficha_clinica)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiActualizarHistoriaClinicaNumero(pacienteId, numero) {
  const parsedNumero = Number.isFinite(numero) ? numero : Number.parseInt(numero, 10)
  const { data, error } = await supabase.rpc('admin_actualizar_historia_clinica_numero', {
    p_paciente_id: pacienteId,
    p_numero: parsedNumero,
  })
  if (error) {
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_editar_numero_hc)
    throw message ? { ...error, message } : error
  }
  return data
}

export async function apiCrearHistoriaClinicaEvolucion(
  pacienteId,
  { medico_id, fecha_clinica, titulo, nota, diagnostico_impresion, indicaciones, consulta_slot_id }
) {
  const { data, error } = await supabase.rpc('admin_crear_historia_clinica_evolucion', {
    p_paciente_id: pacienteId,
    p_medico_id: medico_id,
    p_fecha_clinica: fecha_clinica,
    p_titulo: normalizeTextValue(titulo) || null,
    p_nota: normalizeTextValue(nota) || null,
    p_diagnostico_impresion: normalizeTextValue(diagnostico_impresion) || null,
    p_indicaciones: normalizeTextValue(indicaciones) || null,
    p_consulta_slot_id: consulta_slot_id || null,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_crear_evoluciones)
  return data
}

export async function apiEnviarRecordatoriosTurnos(fechaObjetivo) {
  const body =
    typeof fechaObjetivo === 'string' && fechaObjetivo.trim()
      ? { fecha_objetivo: fechaObjetivo.trim() }
      : {}

  const { data, error } = await supabase.functions.invoke('whatsapp-send-reminders-24h', {
    body,
  })

  if (error) {
    logClientError('apiEnviarRecordatoriosTurnos', error)
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_enviar_recordatorios)
    throw message ? { ...error, message } : error
  }

  if (data?.error) {
    throw new Error(data.error)
  }

  return data?.data || data || null
}

// -- Campos config: CRUD ------------------------------------------------------

export async function apiGetCamposConfig() {
  const { data, error } = await supabase.from('campos_config').select('*').order('orden')
  if (error) logClientError('apiGetCamposConfig', error)
  return data || []
}

export async function apiCrearObraSocial({ nombre, tiene_convenio, abreviatura }) {
  const { data, error } = await supabase.rpc('admin_crear_obra_social', {
    p_nombre: normalizeTextValue(nombre),
    p_tiene_convenio: tiene_convenio === true,
    p_abreviatura: normalizeTextValue(abreviatura).toUpperCase() || null,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_crear_obras_sociales)
  return data
}

export async function apiActualizarObraSocial(id, { nombre, tiene_convenio, abreviatura }) {
  const { data, error } = await supabase.rpc('admin_actualizar_obra_social', {
    p_obra_social_id: id,
    p_nombre: normalizeTextValue(nombre),
    p_tiene_convenio: tiene_convenio === true,
    p_abreviatura: normalizeTextValue(abreviatura).toUpperCase() || null,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_editar_obras_sociales)
  return data
}

export async function apiToggleObraSocialActiva(id, activa) {
  const { data, error } = await supabase.rpc('admin_toggle_obra_social_activa', {
    p_obra_social_id: id,
    p_activa: activa === true,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_estado_obras_sociales)
  return data
}

export async function apiToggleObraSocialConvenio(id, tieneConvenio) {
  const { data, error } = await supabase.rpc('admin_toggle_obra_social_convenio', {
    p_obra_social_id: id,
    p_tiene_convenio: tieneConvenio === true,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_convenio_obras_sociales)
  return data
}

export async function apiCrearMedico({ nombre }) {
  const { data, error } = await supabase.rpc('admin_crear_medico', {
    p_nombre: normalizeTextValue(nombre),
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_crear_medicos)
  return data
}

export async function apiActualizarMedico(id, { nombre }) {
  const { data, error } = await supabase.rpc('admin_actualizar_medico', {
    p_medico_id: id,
    p_nombre: normalizeTextValue(nombre),
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_editar_medicos)
  return data
}

export async function apiToggleMedicoActivo(id, activo) {
  const { data, error } = await supabase.rpc('admin_toggle_medico_activo', {
    p_medico_id: id,
    p_activo: activo === true,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_estado_medicos)
  return data
}

export async function apiCrearReferente({ nombre, tipo }) {
  const { data, error } = await supabase.rpc('admin_crear_referente', {
    p_nombre: normalizeTextValue(nombre),
    p_tipo: normalizeTextValue(tipo),
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_crear_referentes)
  return data
}

export async function apiActualizarReferente(id, { nombre, tipo }) {
  const { data, error } = await supabase.rpc('admin_actualizar_referente', {
    p_referente_id: id,
    p_nombre: normalizeTextValue(nombre),
    p_tipo: normalizeTextValue(tipo),
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_editar_referentes)
  return data
}

export async function apiToggleReferenteActivo(id, activo) {
  const { data, error } = await supabase.rpc('admin_toggle_referente_activo', {
    p_referente_id: id,
    p_activo: activo === true,
  })
  if (error) throwNormalizedPermissionDenied(error, API_TEXT.unauthorized_estado_referentes)
  return data
}

export async function apiCrearCampoConfig({ nombre, tipo }) {
  const { data, error } = await supabase.rpc('admin_crear_campo_config', {
    p_nombre: normalizeTextValue(nombre),
    p_tipo: normalizeTextValue(tipo),
  })
  if (error) {
    logClientError('apiCrearCampoConfig', error)
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_crear_campos_clinicos)
    return { data, error: { ...error, message: message || error.message } }
  }
  return { data, error: null }
}

export async function apiActualizarCampoConfig(id, { nombre, tipo }) {
  const { data, error } = await supabase.rpc('admin_actualizar_campo_config', {
    p_campo_id: id,
    p_nombre: normalizeTextValue(nombre),
    p_tipo: normalizeTextValue(tipo),
  })
  if (error) {
    logClientError('apiActualizarCampoConfig', error)
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_editar_campos_clinicos)
    return { data, error: { ...error, message: message || error.message } }
  }
  return { data, error: null }
}

export async function apiEliminarCampoConfig(id) {
  const { error } = await supabase.rpc('admin_eliminar_campo_config', {
    p_campo_id: id,
  })
  if (error) {
    logClientError('apiEliminarCampoConfig', error)
    const message = normalizePermissionDeniedMessage(error, API_TEXT.unauthorized_eliminar_campos_clinicos)
    return { error: { ...error, message: message || error.message } }
  }
  return { error: null }
}

// -- Turnos fuera de horario --------------------------------------------------

export async function apiGetTurnosFueraHorario(fecha) {
  const { data, error } = await supabase
    .from('turnos_fuera_horario')
    .select('*, paciente:paciente_id(id, nombre, email), monoxido_medico:monoxido_medico_id(id, nombre, activo), operador_camara:operador_camara_id(id, nombre)')
    .eq('fecha', fecha)
    .order('hora')
  if (error) logClientError('apiGetTurnosFueraHorario', error)
  return data || []
}

export async function apiCrearTurnoFueraHorario({
  fecha,
  hora,
  pacienteId,
  operadorCamaraId,
  notas,
  esMonoxido = false,
  monoxidoOrdenMedica = false,
  monoxidoResumenClinico = false,
  monoxidoMedicoId = null,
}) {
  const payload = {
    p_fecha: fecha,
    p_hora: normalizeTextValue(hora),
    p_paciente_id: pacienteId,
    p_operador_camara_id: operadorCamaraId || null,
    p_notas: normalizeTextValue(notas) || null,
    p_es_monoxido: !!esMonoxido,
    p_monoxido_orden_medica: !!monoxidoOrdenMedica,
    p_monoxido_resumen_clinico: !!monoxidoResumenClinico,
    p_monoxido_medico_id: esMonoxido && Number(monoxidoMedicoId) > 0 ? Number(monoxidoMedicoId) : null,
  }

  const { data, error } = await callIdempotentRpc('admin_crear_turno_fuera_horario_idempotent', payload, {
    operationName: 'admin_crear_turno_fuera_horario',
    payload: {
      fecha,
      hora: payload.p_hora,
      pacienteId,
      operadorCamaraId: payload.p_operador_camara_id,
      notas: payload.p_notas,
      esMonoxido: payload.p_es_monoxido,
      monoxidoOrdenMedica: payload.p_monoxido_orden_medica,
      monoxidoResumenClinico: payload.p_monoxido_resumen_clinico,
      monoxidoMedicoId: payload.p_monoxido_medico_id,
    },
  })
  if (error) logClientError('apiCrearTurnoFueraHorario', error)
  return { data, error }
}

export async function apiCancelarTurnoFueraHorario(id) {
  const { error } = await callIdempotentRpc('admin_cancelar_turno_fuera_horario_idempotent', {
    p_turno_id: id,
  }, {
    operationName: 'admin_cancelar_turno_fuera_horario',
    payload: {
      turnoId: id,
    },
  })
  if (error) logClientError('apiCancelarTurnoFueraHorario', error)
  return { error }
}
// -- RBAC / Access control ---------------------------------------------------

export async function apiGetEffectiveAccess() {
  const { data, error } = await supabase.rpc('me_get_effective_access')
  if (error) throw error
  return data || null
}

export async function apiListRbacRoles() {
  const { data, error } = await supabase.rpc('admin_list_roles')
  if (error) throw error
  return data || []
}

export async function apiListRbacPermissions() {
  const { data, error } = await supabase
    .from('rbac_permissions')
    .select('id, key, nombre, descripcion, modulo, is_system')
    .order('modulo')
    .order('key')

  if (error) throw error
  return data || []
}

export async function apiUpsertRbacRole({
  slug,
  nombre,
  descripcion = null,
  activo = true,
  is_staff = true,
  default_home = '/usuario',
}) {
  const { data, error } = await supabase.rpc('admin_upsert_role', {
    p_slug: normalizeTextValue(slug).toLowerCase(),
    p_nombre: normalizeTextValue(nombre),
    p_descripcion: normalizeTextValue(descripcion) || null,
    p_activo: activo === true,
    p_is_staff: is_staff === true,
    p_default_home: normalizeTextValue(default_home) || '/usuario',
  })
  if (error) throw error
  return data
}

export async function apiSetRbacRolePermissions(roleSlug, permissionKeys = []) {
  const keys = Array.isArray(permissionKeys)
    ? [...new Set(permissionKeys.filter((key) => typeof key === 'string' && key.trim()))]
    : []
  const { error } = await supabase.rpc('admin_set_role_permissions', {
    p_role_slug: normalizeTextValue(roleSlug).toLowerCase(),
    p_permission_keys: keys,
  })
  if (error) throw error
}

export async function apiAssignRbacUserRoles(userId, roleSlugs = [], primaryRoleSlug = null) {
  const roles = Array.isArray(roleSlugs)
    ? [...new Set(roleSlugs.filter((value) => typeof value === 'string' && value.trim()).map((value) => normalizeTextValue(value).toLowerCase()))]
    : []
  const { error } = await supabase.rpc('admin_assign_user_roles', {
    p_user_id: userId,
    p_role_slugs: roles,
    p_primary_role_slug: primaryRoleSlug ? normalizeTextValue(primaryRoleSlug).toLowerCase() : null,
  })
  if (error) throw error
}

export async function apiListStaffUsers() {
  const { data, error } = await supabase.rpc('admin_list_staff_users')
  if (error) throw error
  return data || []
}

export async function apiCreateStaffUser({
  nombre,
  login_identifier = null,
  email = null,
  documento_identidad = null,
  password,
  role_slug,
  primary = true,
}) {
  const normalizedPassword = normalizeTextValue(password)
  if (!normalizedPassword) {
    throw new Error(API_TEXT.create_staff_password_required)
  }

  const { data, error } = await supabase.functions.invoke('crear-staff', {
    body: {
      nombre: normalizeTextValue(nombre),
      login_identifier: login_identifier ? normalizeTextValue(login_identifier) : null,
      email: email ? normalizeTextValue(email).toLowerCase() : null,
      documento_identidad: documento_identidad ? normalizeDocumentoIdentidadValue(documento_identidad) : null,
      password: normalizedPassword,
      role_slug: normalizeTextValue(role_slug).toLowerCase(),
      primary: primary === true,
    },
  })
  if (error) throw error
  if (data?.error) throw new Error(data.error)
  return data?.data || data
}

export async function apiSetStaffUserActive(userId, active, roleSlug = null) {
  const { error } = await supabase.rpc('admin_set_staff_user_active', {
    p_user_id: userId,
    p_active: active === true,
    p_role_slug: roleSlug ? normalizeTextValue(roleSlug).toLowerCase() : null,
  })
  if (error) throw error
}

