/*
 * Referencia histórica del envío WhatsApp vía Edge.
 *
 * Usar solo como apoyo para replicar comportamiento ya migrado al backend.
 */

import type {
  MetaSendMessageResponse,
  SendInteractiveButtonsMessageInput,
  SendTemplateMessageInput,
  SendTemplateMessageResult,
  SendTextMessageInput,
  WhatsAppProvider,
} from './whatsapp-types.ts'

function requireWhatsAppEnv(name: string, fallback?: string) {
  const value = Deno.env.get(name) || fallback
  if (!value) {
    throw new Error(`Missing env ${name}`)
  }
  return value
}

function normalizeWhatsAppProvider(value: string | null | undefined): WhatsAppProvider {
  return value === 'kapso' ? 'kapso' : 'meta'
}

function getPreferredWhatsAppProvider() {
  return normalizeWhatsAppProvider(Deno.env.get('WHATSAPP_PROVIDER'))
}

function getFallbackWhatsAppProvider(): WhatsAppProvider | null {
  const raw = (Deno.env.get('WHATSAPP_PROVIDER_FALLBACK') || '').trim().toLowerCase()
  if (!raw || raw === 'none') return null
  return normalizeWhatsAppProvider(raw)
}

function getKapsoBaseUrl() {
  return (Deno.env.get('KAPSO_BASE_URL') || 'https://api.kapso.ai/meta/whatsapp').replace(/\/+$/, '')
}

export function normalizePhoneToE164(value: unknown) {
  const raw = typeof value === 'string' ? value.trim() : ''
  if (!raw) return null

  const digits = raw.replace(/\D/g, '')
  if (!digits) return null

  if (raw.startsWith('+') && digits.length >= 10) {
    return `+${digits}`
  }

  if (digits.startsWith('549') && digits.length >= 12) {
    return `+${digits}`
  }

  if (digits.startsWith('54') && digits.length >= 11) {
    return `+549${digits.slice(2)}`
  }

  const localDigits = digits.replace(/^0+/, '')
  if (localDigits.length >= 10 && localDigits.length <= 11) {
    return `+549${localDigits}`
  }

  return null
}

function buildTemplateRequestPayload(input: SendTemplateMessageInput) {
  const languageCode = input.languageCode || Deno.env.get('WHATSAPP_DEFAULT_LANGUAGE_CODE') || 'es_AR'
  const bodyParameters = Array.isArray(input.bodyParameters) ? input.bodyParameters : []
  const quickReplyButtons = Array.isArray(input.quickReplyButtons) ? input.quickReplyButtons : []
  const components = []

  if (bodyParameters.length) {
    components.push({
      type: 'body',
      parameters: bodyParameters.map((text) => ({
        type: 'text',
        text,
      })),
    })
  }

  for (const button of quickReplyButtons) {
    components.push({
      type: 'button',
      sub_type: 'quick_reply',
      index: String(button.index),
      parameters: [
        {
          type: 'payload',
          payload: button.payload,
        },
      ],
    })
  }

  return {
    messaging_product: 'whatsapp',
    to: input.to,
    type: 'template',
    template: {
      name: input.templateName,
      language: { code: languageCode },
      components,
    },
  }
}

function buildTextRequestPayload(input: SendTextMessageInput) {
  return {
    messaging_product: 'whatsapp',
    to: input.to,
    type: 'text',
    text: {
      preview_url: false,
      body: input.body,
    },
  }
}

function buildInteractiveButtonsRequestPayload(input: SendInteractiveButtonsMessageInput) {
  return {
    messaging_product: 'whatsapp',
    to: input.to,
    type: 'interactive',
    interactive: {
      type: 'button',
      body: {
        text: input.body,
      },
      action: {
        buttons: input.buttons.map((button) => ({
          type: 'reply',
          reply: {
            id: button.id,
            title: button.title,
          },
        })),
      },
    },
  }
}

function normalizeProviderError(
  provider: WhatsAppProvider,
  payload: MetaSendMessageResponse | null,
  status: number
) {
  const errorCode = payload?.error?.code != null ? String(payload.error.code) : String(status)
  const fallbackLabel = provider === 'kapso' ? 'Kapso' : 'Meta'
  const errorMessage = payload?.error?.message || `No se pudo enviar el mensaje por ${fallbackLabel}`
  return { errorCode, errorMessage }
}

function buildSendResult(
  provider: WhatsAppProvider,
  ok: boolean,
  requestPayload: Record<string, unknown>,
  responsePayload: Record<string, unknown> | null,
  status: 'sent' | 'failed',
  errorCode: string | null,
  errorMessage: string | null
): SendTemplateMessageResult {
  const providerMessageId =
    ok && responsePayload && Array.isArray((responsePayload as MetaSendMessageResponse)?.messages)
      ? ((responsePayload as MetaSendMessageResponse).messages?.[0]?.id || null)
      : null

  return {
    ok,
    provider,
    providerMessageId,
    metaMessageId: providerMessageId,
    status,
    requestPayload,
    responsePayload,
    errorCode,
    errorMessage,
  }
}

function toResponseRecord(payload: MetaSendMessageResponse | null) {
  return payload ? (payload as unknown as Record<string, unknown>) : null
}

async function sendPayloadViaMeta(
  requestPayload: Record<string, unknown>
): Promise<SendTemplateMessageResult> {
  const phoneNumberId = requireWhatsAppEnv('WHATSAPP_PHONE_NUMBER_ID')
  const accessToken = requireWhatsAppEnv('WHATSAPP_ACCESS_TOKEN')

  try {
    const response = await fetch(
      `https://graph.facebook.com/v23.0/${phoneNumberId}/messages`,
      {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestPayload),
      }
    )

    const responsePayload = (await response.json().catch(() => null)) as MetaSendMessageResponse | null

    if (!response.ok) {
      const providerError = normalizeProviderError('meta', responsePayload, response.status)
      return buildSendResult(
        'meta',
        false,
        requestPayload,
        toResponseRecord(responsePayload),
        'failed',
        providerError.errorCode,
        providerError.errorMessage
      )
    }

    return buildSendResult('meta', true, requestPayload, toResponseRecord(responsePayload), 'sent', null, null)
  } catch (error) {
    return buildSendResult(
      'meta',
      false,
      requestPayload,
      null,
      'failed',
      'fetch_error',
      error instanceof Error ? error.message : 'No se pudo enviar el mensaje a Meta'
    )
  }
}

async function sendPayloadViaKapso(
  requestPayload: Record<string, unknown>
): Promise<SendTemplateMessageResult> {
  try {
    const phoneNumberId = requireWhatsAppEnv('WHATSAPP_PHONE_NUMBER_ID')
    const apiKey = requireWhatsAppEnv('KAPSO_API_KEY')
    const response = await fetch(`${getKapsoBaseUrl()}/v24.0/${phoneNumberId}/messages`, {
      method: 'POST',
      headers: {
        'X-API-Key': apiKey,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(requestPayload),
    })

    const responsePayload = (await response.json().catch(() => null)) as MetaSendMessageResponse | null

    if (!response.ok) {
      const providerError = normalizeProviderError('kapso', responsePayload, response.status)
      return buildSendResult(
        'kapso',
        false,
        requestPayload,
        toResponseRecord(responsePayload),
        'failed',
        providerError.errorCode,
        providerError.errorMessage
      )
    }

    return buildSendResult('kapso', true, requestPayload, toResponseRecord(responsePayload), 'sent', null, null)
  } catch (error) {
    return buildSendResult(
      'kapso',
      false,
      requestPayload,
      null,
      'failed',
      'fetch_error',
      error instanceof Error ? error.message : 'No se pudo enviar el mensaje por Kapso'
    )
  }
}

async function sendRawMessage(
  requestPayload: Record<string, unknown>,
  provider = getPreferredWhatsAppProvider()
): Promise<SendTemplateMessageResult> {
  const primaryResult =
    provider === 'kapso'
      ? await sendPayloadViaKapso(requestPayload)
      : await sendPayloadViaMeta(requestPayload)

  if (primaryResult.ok) {
    return primaryResult
  }

  if (provider !== 'kapso') {
    return primaryResult
  }

  const fallbackProvider = getFallbackWhatsAppProvider()
  if (fallbackProvider !== 'meta') {
    return primaryResult
  }

  const fallbackResult = await sendPayloadViaMeta(requestPayload)
  if (fallbackResult.ok) {
    return {
      ...fallbackResult,
      responsePayload: {
        primary_provider: 'kapso',
        primary_error: {
          code: primaryResult.errorCode,
          message: primaryResult.errorMessage,
        },
        fallback_provider: 'meta',
        fallback_response: fallbackResult.responsePayload,
      },
    }
  }

  return {
    ...fallbackResult,
    responsePayload: {
      primary_provider: 'kapso',
      primary_response: primaryResult.responsePayload,
      fallback_provider: 'meta',
      fallback_response: fallbackResult.responsePayload,
    },
    errorCode: fallbackResult.errorCode || primaryResult.errorCode,
    errorMessage: `Kapso: ${primaryResult.errorMessage || 'error desconocido'} | Meta: ${fallbackResult.errorMessage || 'error desconocido'}`,
  }
}

export async function sendTemplateMessage(
  input: SendTemplateMessageInput
): Promise<SendTemplateMessageResult> {
  return await sendRawMessage(buildTemplateRequestPayload(input))
}

export async function sendTextMessage(
  input: SendTextMessageInput
): Promise<SendTemplateMessageResult> {
  return await sendRawMessage(buildTextRequestPayload(input))
}

export async function sendInteractiveButtonsMessage(
  input: SendInteractiveButtonsMessageInput
): Promise<SendTemplateMessageResult> {
  return await sendRawMessage(buildInteractiveButtonsRequestPayload(input))
}
