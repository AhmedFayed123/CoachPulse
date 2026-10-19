import type { components } from './generated'

// All API DTOs and enums come from docs/swagger.json via openapi-typescript.
export type Schema<T extends keyof components['schemas']> = components['schemas'][T]
export type AuthResponse = Schema<'AuthSessionResponse'>
export type LoginRequest = Schema<'LoginRequest'>
export type RegisterTenantRequest = Schema<'RegisterTenantRequest'>
export type RegisterClientRequest = Schema<'RegisterClientRequest'>
export type TenantEmailRequest = Schema<'TenantEmailRequest'>
export type ResetPasswordRequest = Schema<'ResetPasswordRequest'>
export type AcceptStaffInviteRequest = Schema<'AcceptStaffInviteRequest'>
export type SendMessageRequest = Schema<'SendMessageRequest'>
export type UserRole = Schema<'UserRole'>
export type Client = Schema<'ClientListItem'>
export type ClientsPage = Schema<'ClientListItemPagedResult'>
export type Conversation = Schema<'ConversationListItem'>
export type ConversationsPage = Schema<'ConversationListItemPagedResult'>
export type Payment = Schema<'PaymentListItem'>
export type PaymentsPage = Schema<'PaymentListItemPagedResult'>
export type DashboardSummary = Schema<'TenantDashboardSummary'>
export type Profile = Schema<'AuthSessionUser'>

export type ProblemDetails = Schema<'ProblemDetails'>

export class ApiError extends Error {
  readonly status?: number
  readonly traceId?: string
  readonly fieldErrors?: Record<string, string[]>
  readonly retryAfter?: number
  readonly isConflict: boolean
  readonly isValidation: boolean
  readonly isRateLimited: boolean

  constructor(problem: ProblemDetails, retryAfter?: number) {
    const extensions = problem as Record<string, unknown>
    super(problem.detail || problem.title || (typeof extensions.message === 'string' ? extensions.message : 'Request failed'))
    this.name = 'ApiError'
    this.status = problem.status ?? undefined
    this.traceId = typeof extensions.traceId === 'string' ? extensions.traceId : undefined
    this.fieldErrors = extensions.errors && typeof extensions.errors === 'object' ? extensions.errors as Record<string, string[]> : undefined
    this.retryAfter = retryAfter
    this.isConflict = problem.status === 409
    this.isValidation = problem.status === 422
    this.isRateLimited = problem.status === 429
  }
}
