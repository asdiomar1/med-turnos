/*
 * Referencia histórica del flujo Edge de WhatsApp.
 *
 * La lógica vigente del sistema debe residir en ASP.NET Core; este archivo
 * se conserva para consulta durante la migración y comparación de comportamiento.
 */

import { serve } from 'https://deno.land/std@0.168.0/http/server.ts'
import { buildCorsHeaders, jsonResponse } from '../_shared/http.ts'
import { createServiceRoleClient, requirePermissionSession, sessionHasPermission } from '../_shared/supabase.ts'
import { processWhatsappDispatchQueue } from '../_shared/whatsapp-dispatch.ts'

const ARGENTINA_TIMEZONE = 'America/Argentina/Buenos_Aires'
const REMINDER_DISPATCH_HOUR_AR = 14

function formatDateInTimezone(date: Date, timeZone: string) {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(date)

  const year = parts.find((part) => part.type === 'year')?.value || '0000'
  const month = parts.find((part) => part.type === 'month')?.value || '01'
  const day = parts.find((part) => part.type === 'day')?.value || '01'
  return `${year}-${month}-${day}`
}

function getHourInTimezone(date: Date, timeZone: string) {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    hour: '2-digit',
    hour12: false,
  }).formatToParts(date)

  return Number(parts.find((part) => part.type === 'hour')?.value || '0')
}

function getTimePartsInTimezone(date: Date, timeZone: string) {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).formatToParts(date)

  const hour = parts.find((part) => part.type === 'hour')?.value || '00'
  const minute = parts.find((part) => part.type === 'minute')?.value || '00'
  return {
    hour: Number(hour),
    minute: Number(minute),
    hhmm: `${hour}:${minute}`,
  }
}

function getTomorrowDateInArgentina(date: Date) {
  const tomorrow = new Date(date)
  tomorrow.setUTCDate(tomorrow.getUTCDate() + 1)
  return formatDateInTimezone(tomorrow, ARGENTINA_TIMEZONE)
}

export const reminderDateUtils = {
  formatDateInTimezone,
  getHourInTimezone,
  getTimePartsInTimezone,
  getTomorrowDateInArgentina,
}

export async function handler(req: Request) {
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: buildCorsHeaders() })
  }

  if (req.method !== 'POST') {
    return jsonResponse({ error: 'Método no permitido' }, 405)
  }

  try {
    let supabaseAdmin = createServiceRoleClient()
    let isManualAdminRun = false
    const authHeader =
      req.headers.get('Authorization') ||
      req.headers.get('authorization') ||
      req.headers.get('x-forwarded-authorization')

    if (authHeader) {
      const adminSession = await requirePermissionSession(req, 'whatsapp.dispatch')
      supabaseAdmin = adminSession.supabaseAdmin
      isManualAdminRun = true

      const canManageWhatsappConfig = await sessionHasPermission(adminSession, 'config.whatsapp.manage')
      if (!canManageWhatsappConfig) {
        return jsonResponse({ error: 'Prohibido' }, 403)
      }
    } else {
      const internalSecret = Deno.env.get('WHATSAPP_WEBHOOK_SECRET') || ''
      const providedSecret = req.headers.get('x-internal-secret') || ''
      if (!internalSecret || providedSecret !== internalSecret) {
        return jsonResponse({ error: 'No autorizado' }, 401)
      }
    }

    const now = new Date()
    const rawBody = await req.text()
    const body = rawBody ? JSON.parse(rawBody) as Record<string, unknown> : {}
    const fechaObjetivo =
      typeof body?.fecha_objetivo === 'string' && body.fecha_objetivo.trim()
        ? body.fecha_objetivo.trim()
        : getTomorrowDateInArgentina(now)
    const horaArgentina = getHourInTimezone(now, ARGENTINA_TIMEZONE)
    const fechaArgentinaActual = formatDateInTimezone(now, ARGENTINA_TIMEZONE)
    const horaArgentinaActual = getTimePartsInTimezone(now, ARGENTINA_TIMEZONE)
    const horaMinimaManual =
      isManualAdminRun && fechaObjetivo === fechaArgentinaActual
        ? horaArgentinaActual.hhmm
        : null

    if (!isManualAdminRun && horaArgentina !== REMINDER_DISPATCH_HOUR_AR) {
      return jsonResponse(
        {
          data: {
            skipped: true,
            reason: 'Fuera de la ventana de envío diario',
            hora_argentina: horaArgentina,
            hora_objetivo: REMINDER_DISPATCH_HOUR_AR,
            fecha_objetivo: fechaObjetivo,
          },
        },
        200
      )
    }

    const { data: candidatos, error: candidatosError } = await supabaseAdmin.rpc(
      'whatsapp_enlistar_recordatorios_dia',
      {
        p_fecha_objetivo: fechaObjetivo,
      }
    )

    if (candidatosError) throw candidatosError

    const candidatosFiltrados = horaMinimaManual
      ? (candidatos || []).filter((candidato: Record<string, unknown>) => {
          const horaCandidato =
            typeof candidato.hora === 'string' ? candidato.hora.slice(0, 5) : ''
          return horaCandidato >= horaMinimaManual
        })
      : candidatos || []

    const slotIds: string[] = []

    for (const candidato of candidatosFiltrados) {
      const idempotencyKey = `recordatorio_24h:${candidato.slot_id}:${candidato.fecha}`
      const { error } = await supabaseAdmin
        .from('whatsapp_dispatch_queue')
        .insert({
          patient_id: candidato.paciente_id,
          slot_id: candidato.slot_id,
          kind: 'recordatorio_24h',
          template_key: 'turno_recordatorio_24h_v3',
          idempotency_key: idempotencyKey,
          trigger_source: isManualAdminRun ? 'manual_recordatorio_dia_siguiente' : 'cron_14h_ar',
          payload: {
            fecha: candidato.fecha,
            hora: candidato.hora,
            fecha_objetivo: fechaObjetivo,
            hora_envio_argentina: REMINDER_DISPATCH_HOUR_AR,
          },
        })

      if (!error) {
        slotIds.push(candidato.slot_id)
        continue
      }

      if (error.code !== '23505') {
        throw error
      }
    }

    const dispatchResult = await processWhatsappDispatchQueue(supabaseAdmin, {
      limit: Math.max(candidatosFiltrados.length + 10, 25),
    })

    return jsonResponse(
      {
        data: {
          fecha_objetivo: fechaObjetivo,
          hora_argentina: horaArgentina,
          hora_minima_aplicada: horaMinimaManual,
          candidatos: candidatosFiltrados.length,
          encolados: slotIds.length,
          ...dispatchResult,
        },
      },
      200
    )
  } catch (error) {
    console.error('whatsapp-send-reminders-24h', error)
    return jsonResponse(
      { error: error instanceof Error ? error.message : 'No se pudieron enviar los recordatorios' },
      500
    )
  }
}

if (import.meta.main) {
  serve(handler)
}

