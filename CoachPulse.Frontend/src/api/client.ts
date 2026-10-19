import { ApiError, type AuthResponse, type ProblemDetails } from './types'

const env = import.meta.env as ImportMetaEnv & {
  VITE_API_BASE_URL?: string
  VITE_HUB_BASE_URL?: string
}
const configuredApiBase = (env.VITE_API_BASE_URL || '').replace(/\/$/, '')
const API_BASE = configuredApiBase ? (configuredApiBase.endsWith('/api') ? configuredApiBase : `${configuredApiBase}/api`) : '/api'
export const HUB_BASE = (env.VITE_HUB_BASE_URL || '').replace(/\/$/, '')
const ACCESS_KEY = 'coachpulse.accessToken'
const REFRESH_KEY = 'coachpulse.refreshToken'
let refreshInFlight: Promise<string | null> | null = null

type RequestOptions = RequestInit & { idempotencyKey?: string }

export function saveSession(session: AuthResponse) {
  sessionStorage.setItem(ACCESS_KEY, session.accessToken)
  sessionStorage.setItem(REFRESH_KEY, session.refreshToken)
}

export function clearSession() {
  sessionStorage.removeItem(ACCESS_KEY)
  sessionStorage.removeItem(REFRESH_KEY)
}

export function accessToken() {
  return sessionStorage.getItem(ACCESS_KEY)
}

function parseRetryAfter(value: string | null) {
  if (!value) return undefined
  const seconds = Number(value)
  if (Number.isFinite(seconds)) return seconds
  const date = Date.parse(value)
  return Number.isFinite(date) ? Math.max(0, Math.ceil((date - Date.now()) / 1000)) : undefined
}

function makeIdempotencyKey() {
  return crypto.randomUUID()
}

async function parseBody(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined
  const contentType = response.headers.get('content-type') || ''
  if (contentType.includes('json')) return response.json()
  const text = (await response.text()).slice(0, 2000)
  if (!text) return undefined
  if (contentType.includes('text/html') || /^\s*<!doctype\s+html|^\s*<html\b/i.test(text)) {
    return { detail: 'The server returned an HTML page instead of an API response.' }
  }
  return { detail: text }
}

async function refreshAccessToken(): Promise<string | null> {
  if (refreshInFlight) return refreshInFlight
  const refreshToken = sessionStorage.getItem(REFRESH_KEY)
  if (!refreshToken) return null

  refreshInFlight = (async () => {
    try {
      const response = await fetch(`${API_BASE}/auth/refresh-token`, {
        method: 'POST',
        headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!response.ok) {
        clearSession()
        return null
      }
      const session = (await response.json()) as AuthResponse
      saveSession(session) // Rotation: store both the replacement access and refresh tokens.
      return session.accessToken
    } catch {
      return null
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

async function execute<T>(path: string, options: RequestOptions, allowRefresh: boolean): Promise<T> {
  const { idempotencyKey, ...init } = options
  const headers = new Headers(init.headers)
  headers.set('Accept', headers.get('Accept') || 'application/json')
  if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  const token = accessToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (idempotencyKey) headers.set('Idempotency-Key', idempotencyKey)

  const response = await fetch(`${API_BASE}${path.startsWith('/') ? path : `/${path}`}`, { ...init, headers })
  if (response.status === 401 && allowRefresh && !path.endsWith('/auth/refresh-token')) {
    // Another request may already have completed rotation while this response was in flight.
    // Reuse that access token instead of rotating its replacement a second time.
    const latestToken = accessToken()
    if (token && latestToken && latestToken !== token) return execute<T>(path, options, false)
    const replacement = await refreshAccessToken()
    if (replacement) return execute<T>(path, options, false)
  }

  const body = await parseBody(response)
  if (!response.ok) {
    const problem = (body && typeof body === 'object' ? body : { detail: 'Request failed' }) as ProblemDetails
    throw new ApiError({ ...problem, status: problem.status ?? response.status }, parseRetryAfter(response.headers.get('Retry-After')))
  }
  return body as T
}

export function apiFetch<T>(path: string, init: RequestOptions = {}): Promise<T> {
  return execute<T>(path, init, true)
}

export function apiPost<T>(path: string, body?: unknown, idempotent = false): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    body: body === undefined ? undefined : JSON.stringify(body),
    ...(idempotent ? { idempotencyKey: makeIdempotencyKey() } : {}),
  })
}

export function uploadFile<T>(path: string, form: FormData): Promise<T> {
  return apiFetch<T>(path, { method: 'POST', body: form })
}

export async function downloadFile(path: string): Promise<{ blob: Blob; filename: string }> {
  const headers = new Headers()
  headers.set('Accept', '*/*')
  const token = accessToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  let response = await fetch(`${API_BASE}${path.startsWith('/') ? path : `/${path}`}`, { headers })
  if (response.status === 401) {
    const replacement = await refreshAccessToken()
    if (replacement) {
      headers.set('Authorization', `Bearer ${replacement}`)
      response = await fetch(`${API_BASE}${path.startsWith('/') ? path : `/${path}`}`, { headers })
    }
  }
  if (!response.ok) {
    const body = await parseBody(response)
    const problem = (body && typeof body === 'object' ? body : { detail: 'Download failed' }) as ProblemDetails
    throw new ApiError({ ...problem, status: problem.status ?? response.status }, parseRetryAfter(response.headers.get('Retry-After')))
  }
  const disposition = response.headers.get('Content-Disposition') || ''
  const filename = disposition.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i)?.[1] ?? 'download'
  return { blob: await response.blob(), filename: decodeURIComponent(filename) }
}

export const endpoints = {
  login: '/auth/login', registerTenant: '/auth/register-tenant', registerClient: '/auth/register-client',
  acceptStaffInvite: '/auth/accept-staff-invite', refresh: '/auth/refresh-token', logout: '/auth/logout',
  logoutAll: '/auth/logout-all', me: '/account/me', dashboard: '/tenant/dashboard/summary',
  clients: '/clients', conversations: '/conversations', payments: '/tenant/payments',
  pendingPayments: '/tenant/payments/pending', myDashboard: '/my/dashboard', myProgram: '/my/program/today',
  myMealPlan: '/my/meal-plan', myMessages: '/my/messages', notifications: '/notifications',
} as const
