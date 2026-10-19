import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, Navigate, NavLink, Route, Routes, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { apiFetch, apiPost, clearSession, endpoints, saveSession, accessToken, downloadFile, uploadFile } from './api/client'
import { ApiError, type AcceptStaffInviteRequest, type AuthResponse, type LoginRequest, type PaymentsPage, type RegisterClientRequest, type RegisterTenantRequest, type TenantEmailRequest, type UserRole, type Schema } from './api/types'
import { queryKeys, useChatConversation, useClients, useConversations, useDashboard, useMe, useMyDashboard, useMyMealPlan, useMyProgram, useNotifications, useNotificationsHub, usePayments, useSendClientMessage, useUpdateProfile } from './api/hooks'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { Activity, ArrowUpLeft, Building2, Check, ChevronDown, ClipboardList, LayoutDashboard, Menu, RefreshCw, Search, ShieldCheck, Sparkles, Users, X } from 'lucide-react'
import { LanguageToggle, toArabic, toEnglish, useLanguage } from './i18n'

function Loading() { const { t } = useLanguage(); return <div className="state loading-state" role="status" aria-live="polite"><span className="loading-spinner" aria-hidden="true" />{t('Loading data...')}</div> }
function ErrorState({ error }: { error: unknown }) {
  const { t } = useLanguage()
  const apiError = error instanceof ApiError ? error : null
  const rawMessage = error instanceof Error ? error.message : ''
  const labels: Record<number, [string, string]> = {
    401: ["\u0627\u0646\u062A\u0647\u062A \u062C\u0644\u0633\u0629 \u0627\u0644\u062F\u062E\u0648\u0644", "\u0633\u062C\u0651\u0644 \u0627\u0644\u062F\u062E\u0648\u0644 \u0645\u0631\u0629 \u0623\u062E\u0631\u0649 \u0644\u0644\u0645\u062A\u0627\u0628\u0639\u0629."],
    403: ["\u0627\u0644\u0635\u0644\u0627\u062D\u064A\u0629 \u063A\u064A\u0631 \u0645\u062A\u0627\u062D\u0629", "\u062D\u0633\u0627\u0628\u0643 \u0644\u0627 \u064A\u0645\u0644\u0643 \u0635\u0644\u0627\u062D\u064A\u0629 \u062A\u0646\u0641\u064A\u0630 \u0647\u0630\u0627 \u0627\u0644\u0625\u062C\u0631\u0627\u0621."],
    404: ["\u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A \u063A\u064A\u0631 \u0645\u062A\u0627\u062D\u0629", "\u0642\u062F \u064A\u0643\u0648\u0646 \u0627\u0644\u0639\u0646\u0635\u0631 \u063A\u064A\u0631 \u0645\u0648\u062C\u0648\u062F \u0623\u0648 \u0644\u0645 \u064A\u0639\u062F \u0645\u062A\u0627\u062D\u0627\u064B \u0644\u062D\u0633\u0627\u0628\u0643."],
    409: ["\u062A\u0639\u0630\u0651\u0631 \u0625\u062A\u0645\u0627\u0645 \u0627\u0644\u062A\u063A\u064A\u064A\u0631", "\u062A\u063A\u064A\u0651\u0631\u062A \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A \u0645\u0646\u0630 \u0622\u062E\u0631 \u0645\u0631\u0629 \u0641\u062A\u062D\u062A\u0647\u0627. \u062D\u062F\u0651\u062B \u0627\u0644\u0635\u0641\u062D\u0629 \u062B\u0645 \u062D\u0627\u0648\u0644 \u0645\u062C\u062F\u062F\u0627\u064B."],
    422: ["\u0631\u0627\u062C\u0639 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u0645\u062F\u062E\u0644\u0629", "\u062A\u062D\u0642\u0642 \u0645\u0646 \u0627\u0644\u062D\u0642\u0648\u0644 \u0627\u0644\u0645\u0637\u0644\u0648\u0628\u0629 \u0648\u0627\u0644\u0642\u064A\u0645 \u062B\u0645 \u0623\u0639\u062F \u0627\u0644\u0645\u062D\u0627\u0648\u0644\u0629."],
    429: ["\u0637\u0644\u0628\u0627\u062A \u0643\u062B\u064A\u0631\u0629 \u062E\u0644\u0627\u0644 \u0648\u0642\u062A \u0642\u0635\u064A\u0631", "\u0627\u0646\u062A\u0638\u0631 \u0642\u0644\u064A\u0644\u0627\u064B \u0642\u0628\u0644 \u0625\u0639\u0627\u062F\u0629 \u0627\u0644\u0645\u062D\u0627\u0648\u0644\u0629."],
    502: ["\u0627\u0644\u062E\u062F\u0645\u0629 \u063A\u064A\u0631 \u0645\u062A\u0627\u062D\u0629 \u0645\u0624\u0642\u062A\u0627\u064B", "\u062A\u0639\u0630\u0631 \u0627\u0644\u0627\u062A\u0635\u0627\u0644 \u0628\u0627\u0644\u062E\u062F\u0645\u0629 \u0627\u0644\u0645\u0637\u0644\u0648\u0628\u0629. \u062D\u0627\u0648\u0644 \u0645\u0631\u0629 \u0623\u062E\u0631\u0649 \u0628\u0639\u062F \u0642\u0644\u064A\u0644."],
    503: ["\u0627\u0644\u062E\u062F\u0645\u0629 \u063A\u064A\u0631 \u0645\u062A\u0627\u062D\u0629 \u0645\u0624\u0642\u062A\u0627\u064B", "\u0627\u0644\u062E\u062F\u0645\u0629 \u0627\u0644\u0645\u0637\u0644\u0648\u0628\u0629 \u0645\u062A\u0648\u0642\u0641\u0629 \u0645\u0624\u0642\u062A\u0627\u064B. \u062D\u0627\u0648\u0644 \u0645\u0631\u0629 \u0623\u062E\u0631\u0649 \u0628\u0639\u062F \u0642\u0644\u064A\u0644."],
  }
  const [heading, fallback] = apiError?.status && labels[apiError.status] ? labels[apiError.status] : ['Could not load data', 'Please try again in a moment.']
  const genericMessages = /^(forbidden|not found|conflict|unprocessable entity|too many requests|service unavailable|request failed)$/i
  const networkError = /failed to fetch|networkerror|load failed/i.test(rawMessage)
  const message = apiError?.status && labels[apiError.status]
    ? ([401, 403, 404, 429, 502, 503].includes(apiError.status) || genericMessages.test(rawMessage) ? fallback : rawMessage || fallback)
    : networkError ? 'The server could not be reached. Check that it is running and try again.' : rawMessage || fallback
  const trace = apiError?.traceId
  return <div className="state error" role="alert"><span className="state-icon" aria-hidden="true">!</span><div><strong>{t(heading)}</strong><p>{t(message)}</p>{trace && <small className="error-trace">{t('Reference')}: {trace}</small>}</div></div>
}
const fieldLabels: Record<string, string> = {
  id: "\u0627\u0644\u0645\u0639\u0631\u0651\u0641", name: "\u0627\u0644\u0627\u0633\u0645", title: "\u0627\u0644\u0639\u0646\u0648\u0627\u0646", email: "\u0627\u0644\u0628\u0631\u064A\u062F \u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A", role: "\u0627\u0644\u062F\u0648\u0631", status: "\u0627\u0644\u062D\u0627\u0644\u0629",
  slug: "\u0627\u0644\u0631\u0627\u0628\u0637 \u0627\u0644\u0645\u062E\u062A\u0635\u0631", createdAt: "\u062A\u0627\u0631\u064A\u062E \u0627\u0644\u0625\u0646\u0634\u0627\u0621", updatedAt: "\u0622\u062E\u0631 \u062A\u062D\u062F\u064A\u062B", createdAtUtc: "\u062A\u0627\u0631\u064A\u062E \u0627\u0644\u0625\u0646\u0634\u0627\u0621",
  amount: "\u0627\u0644\u0645\u0628\u0644\u063A", currency: "\u0627\u0644\u0639\u0645\u0644\u0629", price: "\u0627\u0644\u0633\u0639\u0631", durationDays: "\u0627\u0644\u0645\u062F\u0629 \u0628\u0627\u0644\u0623\u064A\u0627\u0645", durationWeeks: "\u0627\u0644\u0645\u062F\u0629 \u0628\u0627\u0644\u0623\u0633\u0627\u0628\u064A\u0639",
  startDate: "\u062A\u0627\u0631\u064A\u062E \u0627\u0644\u0628\u062F\u0621", endDate: "\u062A\u0627\u0631\u064A\u062E \u0627\u0644\u0627\u0646\u062A\u0647\u0627\u0621", expiresAt: "\u062A\u0627\u0631\u064A\u062E \u0627\u0644\u0627\u0646\u062A\u0647\u0627\u0621", isActive: "\u0646\u0634\u0637", isRead: "\u0645\u0642\u0631\u0648\u0621",
  clientId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0639\u0645\u064A\u0644", coachId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0645\u062F\u0631\u0628", tenantId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0645\u0633\u0627\u062D\u0629", planId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0628\u0627\u0642\u0629",
  subscriptionId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643", paymentId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u062F\u0641\u0639\u0629", programId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C", exerciseId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646",
  purpose: "\u0627\u0644\u063A\u0631\u0636", originalName: "\u0627\u0633\u0645 \u0627\u0644\u0645\u0644\u0641", contentType: "\u0646\u0648\u0639 \u0627\u0644\u0645\u0644\u0641", sizeBytes: "\u0627\u0644\u062D\u062C\u0645 \u0628\u0627\u0644\u0628\u0627\u064A\u062A",
  riskLevel: "\u0645\u0633\u062A\u0648\u0649 \u0627\u0644\u0645\u062E\u0627\u0637\u0631", riskScore: "\u062F\u0631\u062C\u0629 \u0627\u0644\u0645\u062E\u0627\u0637\u0631", unreadCount: "\u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u063A\u064A\u0631 \u0627\u0644\u0645\u0642\u0631\u0648\u0621\u0629", totalCount: "\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0646\u062A\u0627\u0626\u062C",
  page: "\u0627\u0644\u0635\u0641\u062D\u0629", pageSize: "\u0639\u062F\u062F \u0627\u0644\u0639\u0646\u0627\u0635\u0631 \u0641\u064A \u0627\u0644\u0635\u0641\u062D\u0629", items: "\u0627\u0644\u0639\u0646\u0627\u0635\u0631", features: "\u0627\u0644\u0645\u0645\u064A\u0632\u0627\u062A", meals: "\u0627\u0644\u0648\u062C\u0628\u0627\u062A",
  exercises: "\u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646", workoutPlan: "\u062E\u0637\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646", alerts: "\u0627\u0644\u062A\u0646\u0628\u064A\u0647\u0627\u062A", message: "\u0627\u0644\u0631\u0633\u0627\u0644\u0629", body: "\u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644",
  type: "\u0627\u0644\u0646\u0648\u0639", action: "\u0627\u0644\u0625\u062C\u0631\u0627\u0621", entityType: "\u0646\u0648\u0639 \u0627\u0644\u0633\u062C\u0644", entityId: "\u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0633\u062C\u0644", actorRole: "\u062F\u0648\u0631 \u0627\u0644\u0645\u0646\u0641\u0651\u0630",
  isTemplate: "\u0642\u0627\u0644\u0628", duration: "\u0627\u0644\u0645\u062F\u0629", calories: "\u0627\u0644\u0633\u0639\u0631\u0627\u062A \u0627\u0644\u062D\u0631\u0627\u0631\u064A\u0629", protein: "\u0628\u0631\u0648\u062A\u064A\u0646", carbs: "\u0643\u0631\u0628\u0648\u0647\u064A\u062F\u0631\u0627\u062A", fat: "\u062F\u0647\u0648\u0646",
  muscleGroup: "\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629", equipment: "\u0627\u0644\u0645\u0639\u062F\u0627\u062A", videoUrl: "\u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648", rowVersion: "\u0625\u0635\u062F\u0627\u0631 \u0627\u0644\u0633\u062C\u0644",
  frequency: "\u0627\u0644\u062A\u0643\u0631\u0627\u0631", dayOfWeek: "\u064A\u0648\u0645 \u0627\u0644\u0623\u0633\u0628\u0648\u0639", lastCheckInAt: "\u0622\u062E\u0631 Check-in", nextCheckInAt: "\u0645\u0648\u0639\u062F Check-in \u0627\u0644\u0642\u0627\u062F\u0645",
  monthlyRevenue: "\u0625\u064A\u0631\u0627\u062F \u0627\u0644\u0634\u0647\u0631", activeClients: "\u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0627\u0644\u0646\u0634\u0637\u0648\u0646", expiringSubscriptionsCount: "\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0642\u0627\u0631\u0628\u062A \u0639\u0644\u0649 \u0627\u0644\u0627\u0646\u062A\u0647\u0627\u0621",
  pendingCheckIns: "Check-ins \u0628\u0627\u0646\u062A\u0638\u0627\u0631 \u0627\u0644\u0645\u0631\u0627\u062C\u0639\u0629", pendingPayments: "\u062F\u0641\u0639\u0627\u062A \u0628\u0627\u0646\u062A\u0638\u0627\u0631 \u0627\u0644\u062A\u0623\u0643\u064A\u062F", unreadMessages: "\u0631\u0633\u0627\u0626\u0644 \u063A\u064A\u0631 \u0645\u0642\u0631\u0648\u0621\u0629",
}
const englishFieldLabels: Record<string, string> = {
  id: 'ID', name: 'Name', title: 'Title', email: 'Email', role: 'Role', status: 'Status', slug: 'Club address', createdAt: 'Created', updatedAt: 'Updated',
  amount: 'Amount', currency: 'Currency', price: 'Price', durationDays: 'Duration (days)', durationWeeks: 'Duration (weeks)', startDate: 'Start date', endDate: 'End date',
  expiresAt: 'Expires', isActive: 'Active', isRead: 'Read', clientId: 'Client', coachId: 'Coach', tenantId: 'Club', planId: 'Plan', subscriptionId: 'Subscription',
  paymentId: 'Payment', programId: 'Program', exerciseId: 'Exercise', purpose: 'Purpose', originalName: 'File name', contentType: 'File type', sizeBytes: 'File size',
  riskLevel: 'Risk level', riskScore: 'Risk score', unreadCount: 'Unread messages', totalCount: 'Total results', page: 'Page', pageSize: 'Results per page',
  items: 'Items', features: 'Features', meals: 'Meals', exercises: 'Exercises', workoutPlan: 'Workout plan', alerts: 'Alerts', message: 'Message', body: 'Details',
  type: 'Type', action: 'Action', entityType: 'Record type', entityId: 'Record', actorRole: 'Actor role', isTemplate: 'Template', duration: 'Duration', calories: 'Calories',
  protein: 'Protein', carbs: 'Carbohydrates', fat: 'Fat', muscleGroup: 'Muscle group', equipment: 'Equipment', videoUrl: 'Video URL', rowVersion: 'Version',
  frequency: 'Frequency', dayOfWeek: 'Day of week', lastCheckInAt: 'Last check-in', nextCheckInAt: 'Next check-in', monthlyRevenue: 'Monthly revenue',
  activeClients: 'Active clients', expiringSubscriptionsCount: 'Subscriptions expiring soon', pendingCheckIns: 'Check-ins to review', pendingPayments: 'Payments to confirm'
}
function displayLabel(key: string, language: 'ar' | 'en') { return language === 'ar' ? fieldLabels[key] ?? key.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/[_-]/g, ' ') : englishFieldLabels[key] ?? key.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/[_-]/g, ' ') }
function DataValue({ value }: { value: unknown }): React.ReactNode {
  const { t, language } = useLanguage()
  if (value === null || value === undefined || value === '') return <span className="data-muted">{t('Not available')}</span>
  if (typeof value === 'boolean') return <span>{value ? (language === 'ar' ? '\u0646\u0639\u0645' : 'Yes') : (language === 'ar' ? '\u0644\u0627' : 'No')}</span>
  if (typeof value === 'number') return <span>{new Intl.NumberFormat(language === 'ar' ? 'ar-EG' : 'en-US').format(value)}</span>
  if (typeof value === 'string') {
    if (/^(\d{4}-\d\d-\d\dT\d\d:\d\d)/.test(value)) {
      const date = new Date(value)
      if (!Number.isNaN(date.getTime())) return <time dateTime={value}>{new Intl.DateTimeFormat(language === 'ar' ? 'ar-EG' : 'en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(date)}</time>
    }
    if (value.trim().startsWith('{') || value.trim().startsWith('[')) { try { return <DataValue value={JSON.parse(value)} /> } catch { /* Render malformed JSON as ordinary text. */ } }
    const enumValues = new Set(['SuperAdmin', 'TenantOwner', 'CoachStaff', 'Client', 'Active', 'Paused', 'Archived', 'Pending', 'Accepted', 'Expired', 'Confirmed', 'Rejected', 'PendingVerification', 'Cancelled', 'Draft', 'Template', 'Completed', 'Low', 'Medium', 'High', 'Weekly', 'BiWeekly', 'Monthly', 'Paymob', 'InstaPay', 'Cash'])
    return <span className="data-text">{enumValues.has(value) ? t(value) : value}</span>
  }
  if (Array.isArray(value)) {
    if (value.length === 0) return <span className="data-muted">{t('No items')}</span>
    if (value.every(item => ['string', 'number', 'boolean'].includes(typeof item))) return <div className="data-chips">{value.map((item, index) => <span className="data-chip" key={index}><DataValue value={item} /></span>)}</div>
    return <div className="data-list">{value.map((item, index) => <article className="data-item" key={stringField(item, 'id') ?? index}><DataValue value={item} /></article>)}</div>
  }
  if (typeof value === 'object') {
    const record = value as Record<string, unknown>
    if (Array.isArray(record.items) && typeof record.page === 'number') return <DataValue value={record.items} />
    const fields = Object.entries(record).filter(([key]) => !/password|token|secret|rawPayload|^(id|.*Id|rowVersion|page|pageSize|totalCount)$/i.test(key))
    if (fields.length === 0) return <span className="data-muted">{t('No details available')}</span>
    return <dl className="data-grid">{fields.map(([key, field]) => <div className="data-field" key={key}><dt>{displayLabel(key, language)}</dt><dd><DataValue value={field} /></dd></div>)}</dl>
  }
  return <span>{String(value)}</span>
}
function JsonPanel({ value }: { value: unknown }) { return <section className="panel data-panel"><DataValue value={value} /></section> }
function stringField(value: unknown, key: string): string | undefined {
  if (!value || typeof value !== 'object') return undefined
  const field = (value as Record<string, unknown>)[key]
  return typeof field === 'string' ? field : undefined
}
function objectItems(value: unknown): Record<string, unknown>[] {
  const items = Array.isArray(value) ? value : value && typeof value === 'object' && 'items' in value && Array.isArray(value.items) ? value.items : []
  return items.filter((item): item is Record<string, unknown> => Boolean(item && typeof item === 'object'))
}
function nestedRecords(value: unknown): Record<string, unknown>[] {
  const found: Record<string, unknown>[] = []
  const visit = (node: unknown, depth: number) => {
    if (depth > 5 || !node || typeof node !== 'object') return
    if (Array.isArray(node)) { node.forEach(item => visit(item, depth + 1)); return }
    const record = node as Record<string, unknown>
    found.push(record)
    Object.values(record).forEach(item => visit(item, depth + 1))
  }
  visit(value, 0)
  return found
}
function EntitySelect({ label, value, onChange, data, placeholder = 'Choose an item', disabled = false, optional = false }: { label: string; value: string; onChange: (value: string) => void; data: unknown; placeholder?: string; disabled?: boolean; optional?: boolean }) {
  const { t } = useLanguage()
  const options = nestedRecords(data).map((row, index) => {
    const id = ['id', 'clientId', 'planId', 'subscriptionId', 'paymentId', 'exerciseId', 'programExerciseId', 'mealId', 'checkInId', 'noteId', 'messageId', 'notificationId', 'generatedProgramId'].map(key => row[key]).find(item => typeof item === 'string' && item.length > 0)
    const name = ['name', 'title', 'clientName', 'planName', 'exerciseName', 'mealName', 'email', 'content'].map(key => row[key]).find(item => typeof item === 'string' && item.length > 0)
    const context = ['performedAt', 'createdAt', 'date', 'amount', 'reps', 'method'].map(key => row[key]).find(item => typeof item === 'string' || typeof item === 'number')
    const displayName = typeof name === 'string' ? name.length > 56 ? `${name.slice(0, 56)}â€¦` : name : context !== undefined ? `${t('Record')} ${String(context)}` : `${t('Item')} ${index + 1}`
    return { id: typeof id === 'string' ? id : '', label: displayName, status: typeof row.status === 'string' ? row.status : '' }
  }).filter(option => option.id)
  return <label>{label}<select value={value} onChange={event => onChange(event.target.value)} required={!optional} disabled={disabled || options.length === 0}><option value="">{options.length ? t(placeholder) : t('No items available')}</option>{options.map(option => <option key={option.id} value={option.id}>{option.label}{option.status ? ` آ· ${t(option.status)}` : ''}</option>)}</select></label>
}
function requiresInitialPasswordChange(token: string) {
  try {
    const payload = token.split('.')[1]
    if (!payload) return false
    const bytes = Uint8Array.from(atob(payload.replace(/-/g, '+').replace(/_/g, '/')), character => character.charCodeAt(0))
    const claims = JSON.parse(new TextDecoder().decode(bytes)) as Record<string, unknown>
    return claims.mustChangePassword === 'true' || claims.mustChangePassword === true
  } catch {
    return false
  }
}

function Login() {
  const navigate = useNavigate()
  const tenants = useQuery({ queryKey: ['public', 'tenants'], queryFn: () => apiFetch<unknown>('/public/tenants') })
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [tenantSlug, setTenantSlug] = useState<string | null>(null)
  const [clubSearch, setClubSearch] = useState('')
  const [clubPickerOpen, setClubPickerOpen] = useState(false)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const clubs = objectItems(tenants.data).filter(row => String(row.name ?? '').toLocaleLowerCase().includes(clubSearch.trim().toLocaleLowerCase()))
  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (tenantSlug === null) { setError('Choose your club, or choose SuperAdmin access.'); return }
    setBusy(true); setError('')
    try {
      const session = await apiFetch<AuthResponse>(endpoints.login, { method: 'POST', body: JSON.stringify({ email, password, tenantSlug: tenantSlug || null } satisfies LoginRequest) })
      saveSession(session)
      if (requiresInitialPasswordChange(session.accessToken)) { navigate('/first-login-password', { replace: true }); return }
      navigate(session.user.role === 'SuperAdmin' ? '/admin/dashboard' : session.user.role === 'Client' ? '/my/dashboard' : '/app/dashboard', { replace: true })
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.isRateLimited ? `Too many attempts. Try again in ${cause.retryAfter ?? 60} seconds.` : cause.isValidation ? 'Check your email, password and club selection.' : cause.message : 'We could not sign you in. Check your details and try again.')
    } finally { setBusy(false) }
  }
  return <main className="login"><div className="auth-layout"><aside className="auth-intro"><div className="brand">Coach<span>Pulse</span></div><span className="eyebrow">TRAINING AND CLIENT MANAGEMENT</span><h1>Better coaching.<br />Clearer progress.</h1><p>Manage training plans, client progress and communication in one place.</p><ul><li><Check size={17} /> Coaching plans and exercise tracking</li><li><Check size={17} /> Progress reviews and client messaging</li><li><Check size={17} /> Clear tools for coaches and club owners</li></ul><div className="auth-intro-note">Your coaching club starts here.</div></aside><div className="auth-main"><div className="login-card"><div className="auth-card-heading"><span className="eyebrow">Welcome back</span><h2>Sign in</h2><p>Choose the club linked to your account, then enter your sign-in details.</p></div><form onSubmit={submit}><div className="login-tenant-field"><span className="login-field-label">Select a club</span>{tenants.isLoading ? <div className="club-picker-loading"><span className="loading-spinner" />Loading clubsâ€¦</div> : tenants.isError ? <div className="club-picker-error"><p>We could not load the club list.</p><button type="button" className="text-button" onClick={() => void tenants.refetch()}>Try again</button></div> : <div className="club-picker"><button type="button" className={'club-picker-trigger' + (clubPickerOpen ? ' is-open' : '')} aria-haspopup="listbox" aria-expanded={clubPickerOpen} onClick={() => setClubPickerOpen(value => !value)}><span className="club-picker-icon">{tenantSlug === '' ? <ShieldCheck size={19} /> : <Building2 size={19} />}</span><span className="club-picker-value"><strong>{tenantSlug === '' ? 'SuperAdmin' : tenantSlug ? String(objectItems(tenants.data).find(row => row.slug === tenantSlug)?.name ?? 'Selected club') : 'Select your club'}</strong><small>{tenantSlug === '' ? 'Platform administrator' : tenantSlug ? 'Club selected' : 'Choose the club you use to sign in'}</small></span><ChevronDown size={18} className="club-picker-chevron" /></button>{clubPickerOpen && <div className="club-picker-popover"><label className="club-picker-search"><Search size={17} /><input autoFocus value={clubSearch} onChange={event => setClubSearch(event.target.value)} placeholder="Search by club name" aria-label="Search by club name" /></label><div className="club-picker-options" role="listbox" aria-label="Available clubs">{clubSearch.trim().length < 2 && <p className="club-picker-empty">Type at least two letters to search.</p>}<button type="button" role="option" aria-selected={tenantSlug === ''} className="club-picker-option platform-option" onClick={() => { setTenantSlug(''); setClubPickerOpen(false); setClubSearch('') }}><span className="club-picker-avatar platform-avatar"><ShieldCheck size={18} /></span><span><strong>SuperAdmin</strong><small>Sign in to manage the platform</small></span>{tenantSlug === '' && <Check size={17} />}</button>{clubSearch.trim().length >= 2 && clubs.map((row,index) => { const slug=String(row.slug??''); const name=String(row.name??slug); const selected=slug===tenantSlug; return <button type="button" role="option" aria-selected={selected} className="club-picker-option" key={slug||index} onClick={() => { setTenantSlug(slug); setClubSearch(''); setClubPickerOpen(false) }}><span className="club-picker-avatar">{row.logoUrl ? <img src={String(row.logoUrl)} alt="" /> : <Building2 size={18} />}</span><span><strong>{name}</strong><small>CoachPulse club</small></span>{selected && <Check size={17} />}</button>})}{clubSearch.trim().length>=2&&clubs.length===0&&<p className="club-picker-empty">No clubs found. Try another name.</p>}</div></div>}</div>}</div><label>Email address<input value={email} onChange={event => setEmail(event.target.value)} type="email" required autoComplete="username" placeholder="name@example.com" /></label><label className="password-field"><span>Password</span><Link to="/forgot-password">Forgot password?</Link><input value={password} onChange={event => setPassword(event.target.value)} type="password" required autoComplete="current-password" /></label>{error&&<div className="form-error" role="alert">{error}</div>}<button disabled={busy}>{busy?'Signing inâ€¦':'Sign in'}</button></form><section className="auth-paths" aria-labelledby="auth-paths-title"><h3 id="auth-paths-title">New to CoachPulse?</h3><Link className="auth-path-card" to="/register/tenant"><span><strong>Request a new club</strong><small>For coaches who want to set up a club</small></span><ChevronDown size={17} /></Link><Link className="auth-path-card" to="/register/client"><span><strong>Join as a client</strong><small>Complete registration with your coachâ€™s invitation</small></span><ChevronDown size={17} /></Link><Link className="auth-path-secondary" to="/accept-staff-invite">Have a staff invitation? Accept it here.</Link><details className="auth-more"><summary>Other account options</summary><nav><Link to="/confirm-email">Confirm email</Link><Link to="/resend-confirmation">Resend confirmation email</Link><Link to="/reset-password">Reset password</Link></nav></details></section></div><p className="auth-privacy-note">Your account and club information are protected by role-based access controls.</p></div></div></main>
}
function FirstLoginPassword() {
  const navigate = useNavigate()
  const token = accessToken()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [done, setDone] = useState(false)
  const [busy, setBusy] = useState(false)

  if (!done && (!token || !requiresInitialPasswordChange(token))) return <Navigate to="/login" replace />

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError('')
    if (newPassword !== confirmPassword) { setError("\u0643\u0644\u0645\u062A\u0627 \u0627\u0644\u0645\u0631\u0648\u0631 \u063A\u064A\u0631 \u0645\u062A\u0637\u0627\u0628\u0642\u062A\u064A\u0646."); return }
    setBusy(true)
    try {
      const body: Schema<'ChangePasswordRequest'> = { currentPassword, newPassword }
      await apiFetch<void>('/account/change-password', { method: 'PUT', body: JSON.stringify(body) })
      clearSession()
      setDone(true)
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : "\u062A\u0639\u0630\u0631 \u062A\u063A\u064A\u064A\u0631 \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631.")
    } finally { setBusy(false) }
  }

  return <div className="login"><div className="login-card"><div className="brand">Coach<span>Pulse</span></div><h1>{done ? "\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631" : "\u063A\u064A\u0651\u0631 \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631 \u0627\u0644\u0645\u0624\u0642\u062A\u0629"}</h1>{done ? <><p>ط³ط¬ظ‘ظ„ ط§ظ„ط¯ط®ظˆظ„ ط§ظ„ط¢ï؟½?  ط¨ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط¬ط¯ï؟½? ط¯ط©.</p><button onClick={() => navigate('/login', { replace: true })}>ط§ظ„ط¹ظˆط¯ط© ظ„طھط³ط¬ï؟½? ظ„ ط§ظ„ط¯ط®ظˆظ„</button></> : <><p>ï؟½? ظ„ط²ظ… طھط¹ï؟½? ï؟½? ï؟½?  ظƒظ„ظ…ط© ظ…ط±ظˆط± ط¬ط¯ï؟½? ط¯ط© ظ‚ط¨ظ„ ط§ط³طھط®ط¯ط§ظ… ظ„ظˆط­ط© ط§ظ„طھط­ظƒظ….</p><form onSubmit={event => void submit(event)}><label>ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط­ط§ظ„ï؟½? ط©<input type="password" value={currentPassword} onChange={event => setCurrentPassword(event.target.value)} autoComplete="current-password" required /></label><label>ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط¬ط¯ï؟½? ط¯ط©<input type="password" value={newPassword} onChange={event => setNewPassword(event.target.value)} autoComplete="new-password" minLength={8} required /></label><label>طھط£ظƒï؟½? ط¯ ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط¬ط¯ï؟½? ط¯ط©<input type="password" value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} autoComplete="new-password" minLength={8} required /></label>{error && <div className="form-error" role="alert">{error}</div>}<button disabled={busy}>{busy ? "\u062C\u0627\u0631\u064D \u0627\u0644\u062A\u062D\u062F\u064A\u062B..." : "\u062A\u062D\u062F\u064A\u062B \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631"}</button><button type="button" className="secondary-button" onClick={() => { clearSession(); navigate('/login', { replace: true }) }}>طھط³ط¬ï؟½? ظ„ ط§ظ„ط®ط±ظˆط¬</button></form></>}</div></div>
}

type PublicAuthMode = 'tenant' | 'client' | 'staff' | 'forgot' | 'reset' | 'confirm' | 'resend'
function PublicAuth({ mode }: { mode: PublicAuthMode }) {
  const navigate = useNavigate()
  const [search] = useSearchParams()
  const [email, setEmail] = useState('')
  const [name, setName] = useState('')
  const [password, setPassword] = useState('')
  const [tenantSlug, setTenantSlug] = useState('')
  const [tenantName, setTenantName] = useState('')
  const [token, setToken] = useState(search.get('token') ?? '')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  const title = mode === 'tenant' ? "\u0625\u0646\u0634\u0627\u0621 \u0645\u0633\u0627\u062D\u0629 \u0645\u062F\u0631\u0628" : mode === 'client' ? "\u062A\u0633\u062C\u064A\u0644 \u0639\u0645\u064A\u0644" : mode === 'staff' ? "\u0642\u0628\u0648\u0644 \u062F\u0639\u0648\u0629 \u0645\u062F\u0631\u0628" : mode === 'reset' ? "\u0625\u0639\u0627\u062F\u0629 \u062A\u0639\u064A\u064A\u0646 \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631" : mode === 'confirm' ? "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u0628\u0631\u064A\u062F \u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A" : mode === 'resend' ? "\u0625\u0639\u0627\u062F\u0629 \u0625\u0631\u0633\u0627\u0644 \u0631\u0633\u0627\u0644\u0629 \u0627\u0644\u062A\u0623\u0643\u064A\u062F" : "\u0627\u0633\u062A\u0639\u0627\u062F\u0629 \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631"
  const submit = async (event: React.FormEvent) => {
    event.preventDefault(); setBusy(true); setMessage('')
    try {
      if (mode === 'tenant') {
        const body: RegisterTenantRequest = { email, password, name, tenantName, tenantSlug }
        const session = await apiFetch<AuthResponse>('/auth/register-tenant', { method: 'POST', body: JSON.stringify(body) })
        saveSession(session); navigate('/app/dashboard', { replace: true }); return
      }
      if (mode === 'client') {
        const body: RegisterClientRequest = { email, password, name, inviteToken: token }
        const session = await apiFetch<AuthResponse>('/auth/register-client', { method: 'POST', body: JSON.stringify(body) })
        saveSession(session); navigate('/my/dashboard', { replace: true }); return
      }
      if (mode === 'staff') {
        const body: AcceptStaffInviteRequest = { token, password }
        const session = await apiFetch<AuthResponse>('/auth/accept-staff-invite', { method: 'POST', body: JSON.stringify(body) })
        saveSession(session); navigate('/app/dashboard', { replace: true }); return
      }
      if (mode === 'reset') {
        const body: Schema<'ResetPasswordRequest'> = { tenantSlug, email, token, newPassword: password }
        await apiFetch<unknown>('/auth/reset-password', { method: 'POST', body: JSON.stringify(body) })
        setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631. \u064A\u0645\u0643\u0646\u0643 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u062F\u062E\u0648\u0644 \u0627\u0644\u0622\u0646.")
        return
      }
      if (mode === 'confirm') {
        const body: Schema<'EmailTokenRequest'> = { tenantSlug, email, token }
        await apiFetch<unknown>('/auth/confirm-email', { method: 'POST', body: JSON.stringify(body) })
        setMessage("\u062A\u0645 \u062A\u0623\u0643\u064A\u062F \u0627\u0644\u0628\u0631\u064A\u062F \u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A.")
        return
      }
      if (mode === 'resend') {
        const body: Schema<'TenantEmailRequest'> = { tenantSlug, email }
        await apiFetch<unknown>('/auth/resend-confirmation', { method: 'POST', body: JSON.stringify(body) })
        setMessage("\u0625\u0630\u0627 \u0643\u0627\u0646\u062A \u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u062D\u0633\u0627\u0628 \u0635\u062D\u064A\u062D\u0629\u060C \u0633\u062A\u0635\u0644\u0643 \u0631\u0633\u0627\u0644\u0629 \u0627\u0644\u062A\u0623\u0643\u064A\u062F.")
        return
      }
      const body: TenantEmailRequest = { tenantSlug, email }
      await apiFetch<unknown>('/auth/forgot-password', { method: 'POST', body: JSON.stringify(body) })
      setMessage("\u0625\u0630\u0627 \u0643\u0627\u0646\u062A \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A \u0635\u062D\u064A\u062D\u0629\u060C \u0633\u062A\u0635\u0644\u0643 \u062A\u0639\u0644\u064A\u0645\u0627\u062A \u0627\u0644\u0627\u0633\u062A\u0639\u0627\u062F\u0629 \u0639\u0644\u0649 \u0627\u0644\u0628\u0631\u064A\u062F.")
    } catch (cause) { setMessage(cause instanceof ApiError ? cause.message : "\u062A\u0639\u0630\u0631 \u0625\u0643\u0645\u0627\u0644 \u0627\u0644\u0637\u0644\u0628.") }
    finally { setBusy(false) }
  }
  return <div className="login"><div className="login-card"><h1>{title}</h1><form onSubmit={event => void submit(event)}>
    {(mode === 'tenant' || mode === 'client') && <label>ط§ظ„ط§ط³ظ…<input value={name} onChange={event => setName(event.target.value)} required /></label>}
    {(mode === 'tenant' || mode === 'client') && <label>ط§ظ„ط¨ط±ï؟½? ط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆï؟½? ï؟½? <input type="email" value={email} onChange={event => setEmail(event.target.value)} required /></label>}
    {mode === 'tenant' && <><label>ط§ط³ظ… ط§ظ„ظ…ط³ط§ط­ط©<input value={tenantName} onChange={event => setTenantName(event.target.value)} required /></label><label>ط±ط§ط¨ط· ط§ظ„ظ…ط³ط§ط­ط©<input value={tenantSlug} onChange={event => setTenantSlug(event.target.value)} required pattern="[a-z0-9-]{3,30}" /></label></>}
    {['forgot', 'reset', 'confirm', 'resend'].includes(mode) && <><label>ط±ط§ط¨ط· ط§ظ„ظ…ط³ط§ط­ط©<input value={tenantSlug} onChange={event => setTenantSlug(event.target.value)} required /></label><label>ط§ظ„ط¨ط±ï؟½? ط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆï؟½? ï؟½? <input type="email" value={email} onChange={event => setEmail(event.target.value)} required /></label></>}
    {(mode === 'client' || mode === 'staff' || mode === 'reset' || mode === 'confirm') && <label>ط§ظ„ط±ظ…ط²<input value={token} onChange={event => setToken(event.target.value)} required /></label>}
    {mode !== 'forgot' && mode !== 'confirm' && mode !== 'resend' && <label>ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±{mode === 'reset' ? " \u0627\u0644\u062C\u062F\u064A\u062F\u0629" : ''}<input type="password" value={password} onChange={event => setPassword(event.target.value)} minLength={8} required /></label>}
    {message && <div className="form-error">{message}</div>}<button disabled={busy}>{busy ? "\u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0631\u0633\u0627\u0644..." : "\u0645\u062A\u0627\u0628\u0639\u0629"}</button>
  </form><Link to="/login">ط§ظ„ط¹ظˆط¯ط© ظ„طھط³ط¬ï؟½? ظ„ ط§ظ„ط¯ط®ظˆظ„</Link></div></div>
}

function AppGuard({ roles, children }: { roles?: UserRole[]; children: React.ReactNode }) {
  const me = useMe()
  if (!accessToken()) return <Navigate to="/login" replace />
  if (me.isLoading) return <Loading />
  if (me.isError || !me.data) return <Navigate to="/login" replace />
  if (roles && !roles.includes(me.data.role)) return <Navigate to={me.data.role === 'Client' ? '/my/dashboard' : me.data.role === 'SuperAdmin' ? '/admin/dashboard' : '/app/dashboard'} replace />
  return <>{children}</>
}

function Shell() {
  const me = useMe()
  const { t } = useLanguage()
  const navigate = useNavigate()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const closeDrawer = () => setDrawerOpen(false)
  const logout = async () => {
    try { await apiFetch<void>(endpoints.logout, { method: 'POST', body: JSON.stringify({ refreshToken: sessionStorage.getItem('coachpulse.refreshToken') }) }) }
    finally { clearSession(); navigate('/login', { replace: true }) }
  }
  return <div className={`shell workspace-shell${drawerOpen ? ' drawer-open' : ''}`}>
    <button className="workspace-scrim" aria-label="Close navigation" onClick={closeDrawer} tabIndex={drawerOpen ? 0 : -1} />
    <aside id="workspace-navigation" className="workspace-drawer"><div className="brand">Coach<span>Pulse</span></div><div className="identity">{me.data?.name || 'Workspace'}<small>{me.data?.role}</small></div><nav onClick={closeDrawer}>
      <NavLink to="/app/dashboard">{t('Overview')}</NavLink><NavLink to="/app/clients">{t('Clients')}</NavLink>{me.data?.role === 'TenantOwner' && <NavLink to="/app/coaches">{t('Coaching team')}</NavLink>}<NavLink to="/app/checkins-review">{t('Check-in reviews')}</NavLink><NavLink to="/app/programs">{t('Programs')}</NavLink><NavLink to="/app/exercises">{t('Exercises')}</NavLink><NavLink to="/app/meal-plans">{t('Meal plans')}</NavLink>{me.data?.role === 'TenantOwner' && <NavLink to="/app/plans">{t('Plans')}</NavLink>}<NavLink to="/app/conversations">{t('Conversations')}</NavLink><NavLink to="/app/payments">{t('Payments')}</NavLink><NavLink to="/app/subscriptions">{t('Subscriptions')}</NavLink><NavLink to="/app/notifications">{t('Notifications')}</NavLink><NavLink to="/app/ai">{t('AI tools')}</NavLink><NavLink to="/app/ai-review">{t('AI review')}</NavLink>{me.data?.role === 'TenantOwner' && <><NavLink to="/app/settings">{t('Workspace settings')}</NavLink><NavLink to="/app/analytics">{t('Analytics')}</NavLink><NavLink to="/app/audit">{t('Audit log')}</NavLink></>}<NavLink to="/app/account">{t('Account')}</NavLink>
    </nav><button className="logout" onClick={() => void logout()}>Sign out</button></aside>
    <main><div className="workspace-mobilebar"><button className="workspace-menu-toggle" aria-label={drawerOpen ? 'Close navigation' : 'Open navigation'} aria-expanded={drawerOpen} aria-controls="workspace-navigation" onClick={() => setDrawerOpen(value => !value)}>{drawerOpen ? <X size={19} /> : <Menu size={19} />}<span>Menu</span></button><span className="workspace-mobile-brand">Coach<span>Pulse</span></span><span className="workspace-mobile-role">{me.data?.role}</span></div><Routes>
      <Route path="dashboard" element={<Dashboard />} /><Route path="clients" element={<Clients />} /><Route path="coaches" element={<CoachTeamWorkspace />} /><Route path="checkins-review" element={<PendingCheckInReviews />} /><Route path="programs" element={<CoachPrograms />} /><Route path="exercises" element={<ExerciseLibrary />} /><Route path="meal-plans" element={<MealPlanWorkspace />} /><Route path="plans" element={me.data?.role === 'TenantOwner' ? <TenantPlans /> : <Navigate to="dashboard" replace />} /><Route path="conversations" element={<Conversations />} /><Route path="payments" element={<><Payments /><TenantPaymentInsights /></>} /><Route path="subscriptions" element={<SubscriptionManagement />} /><Route path="notifications" element={<NotificationCenter />} /><Route path="ai" element={<AiWorkspace />} /><Route path="ai-review" element={<AiAdvancedWorkspace />} /><Route path="settings" element={<TenantSettings />} /><Route path="analytics" element={<TenantAnalytics />} /><Route path="audit" element={<TenantAudit />} /><Route path="account" element={<AccountPage />} /><Route path="*" element={<Navigate to="dashboard" replace />} />
    </Routes></main>
  </div>
}

function Page({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  const { t } = useLanguage()
  return <div className="page-content"><header className="page-header"><div><span className="eyebrow">COACHPULSE / WORKSPACE</span><h1>{t(title)}</h1>{subtitle && <p>{t(subtitle)}</p>}</div></header><div className="page-sections">{children}</div></div>
}

function DashboardHero({ title, subtitle, image }: { title: string; subtitle: string; image: string }) { const { t } = useLanguage(); return <section className="visual-hero"><img src={image} alt="" loading="lazy" /><div className="visual-hero-shade" /><div className="visual-hero-copy"><span className="eyebrow">COACHPULSE / TRAINING SPACE</span><h2>{t(title)}</h2><p>{t(subtitle)}</p></div><div className="visual-hero-mark" aria-hidden="true"><span>CP</span></div></section> }
function ExerciseMedia({ value }: { value: unknown }) { const videoUrl = stringField(value, 'videoUrl'); const mediaUrl = (() => { try { const parsed = videoUrl ? new URL(videoUrl) : null; return parsed?.protocol === 'https:' ? parsed : null } catch { return null } })(); const directVideo = Boolean(mediaUrl && /\.(mp4|webm)$/i.test(mediaUrl.pathname)); return <section className="exercise-media"><div className="exercise-media-image"><img src="/media/exercise-training.jpg" alt="Strength training equipment" loading="lazy" /></div><div className="exercise-media-copy"><span className="eyebrow">MOVEMENT LIBRARY</span><h3>{stringField(value, 'name') ?? 'Exercise preview'}</h3><p>{stringField(value, 'muscleGroup') ?? 'Explore the selected movement and its training details.'}</p>{directVideo && mediaUrl ? <video controls preload="metadata" playsInline src={mediaUrl.href} aria-label="Exercise demonstration video" /> : mediaUrl ? <a className="media-link" href={mediaUrl.href} target="_blank" rel="noreferrer">Open exercise demonstration</a> : <small>Add a secure demo video URL to this exercise to show it here.</small>}</div></section> }

function Dashboard() {
  const query = useDashboard()
  if (query.isLoading) return <Loading />
  if (query.isError) return <ErrorState error={query.error} />
  const summary = query.data!
  return <Page title={'\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629'} subtitle={'\u0645\u0644\u062E\u0635 \u0645\u0633\u0627\u062D\u0629 \u0627\u0644\u0646\u0627\u062F\u064A'}><DashboardHero title="Build stronger coaching habits" subtitle="Your workspace at a glance: clients, revenue and the next actions that need your attention." image="/media/gym-workspace.jpg" /><div className="cards">
    <Metric label={'\u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0627\u0644\u0646\u0634\u0637\u0648\u0646'} value={summary.activeClients} /><Metric label={'\u0625\u064A\u0631\u0627\u062F \u0627\u0644\u0634\u0647\u0631'} value={summary.monthlyRevenue ?? '\u2014'} /><Metric label={'\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0642\u0627\u0631\u0628\u062A \u0639\u0644\u0649 \u0627\u0644\u0627\u0646\u062A\u0647\u0627\u0621'} value={summary.expiringSubscriptionsCount} />
  </div><section className="panel"><h2>ط§ظ„طھï؟½? ط¨ï؟½? ظ‡ط§طھ</h2><p>Check-ins ظ„ظ„ظ…ط±ط§ط¬ط¹ط©: {summary.alerts.pendingCheckIns ?? 'â€”'}  آ· ط¯ظپط¹ط§طھ ظ„ظ„طھط£ظƒï؟½? ط¯: {summary.alerts.pendingPayments ?? 'â€”'}  آ· ط±ط³ط§ط¦ظ„ ط؛ï؟½? ط± ظ…ظ‚ط±ظˆط،ط©: {summary.alerts.unreadMessages ?? 'â€”'}</p></section></Page>
}
function Metric({ label, value }: { label: string; value: number | string }) { return <article className="metric"><span>{label}</span><strong>{value}</strong></article> }
function responseId(value: unknown): string | null {
  if (!value || typeof value !== 'object') return null
  const record = value as Record<string, unknown>
  if (typeof record.generatedProgramId === 'string') return record.generatedProgramId
  if (typeof record.id === 'string') return record.id
  if (typeof record.Id === 'string') return record.Id
  for (const key of ['plan', 'Plan', 'program', 'Program', 'mealPlan', 'MealPlan', 'generatedProgram', 'GeneratedProgram', 'invitation', 'Invitation', 'file', 'File']) {
    const nested = record[key]
    if (nested && typeof nested === 'object') { const nestedRecord = nested as Record<string, unknown>; if (typeof nestedRecord.id === 'string') return nestedRecord.id; if (typeof nestedRecord.Id === 'string') return nestedRecord.Id }
  }
  return null
}
function CoachPrograms() {
  const cache = useQueryClient()
  const { t } = useLanguage()
  const [searchParams] = useSearchParams()
  const query = useQuery({ queryKey: ['programs'], queryFn: () => apiFetch<unknown>('/programs?page=1&pageSize=100') })
  const clients = useClients(1, 100)
  const [title, setTitle] = useState('')
  const [durationDays, setDurationDays] = useState(28)
  const [programId, setProgramId] = useState(() => searchParams.get('programId') ?? '')
  const [clientId, setClientId] = useState('')
  const [startDate, setStartDate] = useState(() => { const now = new Date(); return new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 10) })
  const [message, setMessage] = useState('')
  const [assignmentMessage, setAssignmentMessage] = useState('')
  const [assigning, setAssigning] = useState(false)
  const [creating, setCreating] = useState(false)
  const programs = objectItems(query.data)
  const selectedExercises = useQuery({ queryKey: ['program', programId, 'exercises'], queryFn: () => apiFetch<unknown>(`/programs/${programId}/exercises?page=1&pageSize=100`), enabled: Boolean(programId) })
  const hasExercises = objectItems(selectedExercises.data).length > 0
  const create = async (event: React.FormEvent) => { event.preventDefault(); if (creating) return; setCreating(true); try { const result = await apiFetch<unknown>('/programs', { method: 'POST', body: JSON.stringify({ title, durationDays, isTemplate: false } satisfies Schema<'CreateProgramRequest'>) }); setProgramId(responseId(result) ?? ''); setTitle(''); setMessage('Draft created. Add exercises, then assign it to a client.'); await cache.invalidateQueries({ queryKey: ['programs'] }) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not create program.') } finally { setCreating(false) } }
  const assign = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!programId || !clientId || assigning) return
    setAssigning(true)
    setAssignmentMessage('')
    // The API interprets this date in the tenant timezone; keep the selected calendar date intact.
    const body: Schema<'AssignProgramRequest'> = { clientId, startDate: `${startDate}T00:00:00` }
    try {
      await apiFetch<unknown>(`/programs/${programId}/assign`, { method: 'PUT', body: JSON.stringify(body) })
      setAssignmentMessage('Program assigned.')
      await Promise.all([
        cache.invalidateQueries({ queryKey: ['programs'] }),
        cache.invalidateQueries({ queryKey: ['program', programId] }),
        cache.invalidateQueries({ queryKey: ['client-programs', clientId] })
      ])
    } catch (error) {
      setAssignmentMessage(error instanceof Error ? error.message : 'Could not assign program.')
    } finally { setAssigning(false) }
  }
  return <Page title={t('Training programs')} subtitle={t('Create a program, add its exercises, then assign it to a client.')}>
    <section className="panel"><h2>{t('Create a program')}</h2><form className="form-grid" onSubmit={event => void create(event)}><label>{t('Program name')}<input value={title} onChange={event => setTitle(event.target.value)} required maxLength={160} placeholder={t('Example: Strength foundation')} /></label><label>{t('Duration in days')}<input type="number" min={1} max={365} value={durationDays} onChange={event => setDurationDays(Number(event.target.value))} required /></label><button type="submit" disabled={creating}>{t(creating ? 'Creatingâ€¦' : 'Create draft')}</button></form>{message && <p role="status" className="form-feedback">{t(message)}</p>}{programId && <div className="program-next-step"><div className="section-heading"><div><strong>{t('Program setup')}</strong><p>{hasExercises ? t('Program exercises are ready. Assign this program when you are ready.') : t('Step 1 of 2: add at least one exercise. Then assign the program to a client.')}</p></div><button className="secondary-button" type="button" onClick={() => setProgramId('')}>{t('Close details')}</button></div><ProgramActions id={programId} onProgramCreated={setProgramId} />{hasExercises ? <form className="form-grid program-assign-form" onSubmit={event => void assign(event)}><h3>{t('Step 2: assign to a client')}</h3><EntitySelect label={t('Client')} value={clientId} onChange={setClientId} data={clients.data} /><label>{t('Start date')}<input type="date" value={startDate} onChange={event => setStartDate(event.target.value)} required /></label><button disabled={!clientId || assigning}>{t(assigning ? 'Assigning...' : 'Assign program')}</button>{assignmentMessage && <p className="form-feedback assignment-feedback" role="status">{t(assignmentMessage)}</p>}</form> : <div className="empty-state">{selectedExercises.isLoading ? t('Loading program exercisesâ€¦') : t('Add an exercise above to unlock assignment.')}</div>}</div>}</section>
    <section className="panel"><div className="section-heading"><div><h2>{t('Workspace programs')}</h2><p>{t('Open a program to review or edit it. Status and assignment are shown below.')}</p></div><span className="count-badge">{programs.length} {t('programs')}</span></div>{query.isLoading ? <Loading /> : query.isError ? <ErrorState error={query.error} /> : programs.length === 0 ? <div className="empty-state">{t('No programs yet. Create your first draft above.')}</div> : <div className="program-card-grid">{programs.map((item, index) => { const id = String(item.id ?? ''); return <article className="program-list-card" key={id || index}><div><span className={'subscription-status status-' + String(item.status ?? 'draft').toLowerCase()}>{t(String(item.status ?? 'Draft'))}</span><h3>{String(item.title ?? item.name ?? t('Training program'))}</h3><small>{item.durationDays == null ? t('Duration not set') : `${String(item.durationDays)} ${t('days')}`}</small><p>{item.clientName ? `${t('Assigned to')}: ${String(item.clientName)}` : t('Not assigned')}</p></div><button type="button" onClick={() => setProgramId(id)} disabled={!id}>{t('Manage')}</button></article> })}</div>}</section>
  </Page>
}function ProgramActions({ id, onProgramCreated }: { id: string; onProgramCreated: (id: string) => void }) {
  const cache = useQueryClient()
  const { t } = useLanguage()
  const clients = useClients()
  const detail = useQuery({ queryKey: ['program', id], queryFn: () => apiFetch<unknown>(`/programs/${id}`), enabled: Boolean(id) })
  const programExercises = useQuery({ queryKey: ['program', id, 'exercises'], queryFn: () => apiFetch<unknown>(`/programs/${id}/exercises?page=1&pageSize=100`), enabled: Boolean(id) })
  const [rowVersion, setRowVersion] = useState('')
  const [title, setTitle] = useState('')
  const [durationDays, setDurationDays] = useState('28')
  const [exerciseId, setExerciseId] = useState('')
  const [sets, setSets] = useState('3')
  const [reps, setReps] = useState('10-12')
  const [day, setDay] = useState('1')
  const [message, setMessage] = useState('')
  const [templateId, setTemplateId] = useState('')
  const [templateClientId, setTemplateClientId] = useState('')
  const [programExerciseId, setProgramExerciseId] = useState('')
  const [exerciseRowVersion, setExerciseRowVersion] = useState('')
  const [exerciseIdsOrder, setExerciseIdsOrder] = useState('')
  const templates = useQuery({ queryKey: ['program-templates'], queryFn: () => apiFetch<unknown>('/programs/templates') })
  const exercises = useQuery({ queryKey: ['exercises', 'selector'], queryFn: () => apiFetch<unknown>('/exercises?page=1&pageSize=100') })
  useEffect(() => {
    if (detail.data && typeof detail.data === 'object') {
      const value = detail.data as Record<string, unknown>
      if (typeof value.rowVersion === 'string') setRowVersion(value.rowVersion)
      if (typeof value.title === 'string') setTitle(value.title)
      if (typeof value.durationDays === 'number') setDurationDays(String(value.durationDays))
    }
    const firstExercise = nestedRecords(programExercises.data).find(item => typeof item.rowVersion === 'string')
    if (firstExercise?.rowVersion && typeof firstExercise.rowVersion === 'string') setExerciseRowVersion(firstExercise.rowVersion)
  }, [detail.data, programExercises.data])
  const update = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'ProgramUpdateRequest'> = { title, durationDays: Number(durationDays), rowVersion }; try { await apiFetch<void>(`/programs/${id}`, { method: 'PUT', body: JSON.stringify(body) }); setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C."); await cache.invalidateQueries({ queryKey: ['program', id] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.") } }
  const addExercise = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'AddProgramExerciseRequest'> = { exerciseId, sets: Number(sets), reps, day: Number(day), rowVersion }; try { await apiFetch<unknown>(`/programs/${id}/exercises`, { method: 'POST', body: JSON.stringify(body) }); setMessage("\u062A\u0645\u062A \u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0644\u0644\u0628\u0631\u0646\u0627\u0645\u062C."); await cache.invalidateQueries({ queryKey: ['program', id] }); await cache.invalidateQueries({ queryKey: ['program', id, 'exercises'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631\u062A \u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.") } }
  const act = async (action: 'archive' | 'duplicate' | 'delete') => { if (action === 'delete' && !window.confirm(t('Delete this draft?'))) return; try { if (action === 'archive') await apiFetch<void>('/programs/' + id + '/archive', { method: 'PUT', body: JSON.stringify({ rowVersion } satisfies Schema<'ProgramVersionRequest'>) }); else if (action === 'duplicate') await apiFetch<unknown>('/programs/' + id + '/duplicate', { method: 'POST' }); else await apiFetch<void>('/programs/' + id, { method: 'DELETE', body: JSON.stringify({ rowVersion } satisfies Schema<'ProgramVersionRequest'>) }); setMessage(action === 'delete' ? 'Draft deleted.' : action === 'archive' ? 'Program archived.' : 'Program duplicated.'); await cache.invalidateQueries({ queryKey: ['programs'] }); await cache.invalidateQueries({ queryKey: ['program', id] }) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not complete the program action.') } }
  const saveTemplate = async () => { try { const body: Schema<'CreateProgramRequest'> = { title: title.trim() || stringField(detail.data, 'title') || '' }; await apiFetch<unknown>(`/programs/${id}/save-as-template`, { method: 'POST', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['program-templates'] }); setMessage("\u062A\u0645 \u062D\u0641\u0638 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C \u0643\u0642\u0627\u0644\u0628.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u0642\u0627\u0644\u0628.") } }
  const createFromTemplate = async () => { if (!templateId || !templateClientId) return; const body: Schema<'CreateProgramFromTemplateRequest'> = { clientId: templateClientId }; try { const created = await apiFetch<unknown>(`/programs/${templateId}/from-template`, { method: 'POST', body: JSON.stringify(body) }); const newId = responseId(created); await cache.invalidateQueries({ queryKey: ['programs'] }); setTemplateId(''); setTemplateClientId(''); if (newId) { onProgramCreated(newId); setMessage("\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C. \u062A\u0645 \u0627\u0633\u0646\u0627\u062F\u0647 \u0644\u0644\u0639\u0645\u064A\u0644 \u0627\u0644\u0645\u062E\u062A\u0627\u0631.") } else setMessage("\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C \u0645\u0646 \u0627\u0644\u0642\u0627\u0644\u0628. \u0627\u0641\u062A\u062D\u0647 \u0645\u0646 \u0642\u0627\u0626\u0645\u0629 \u0627\u0644\u0628\u0631\u0627\u0645\u062C.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C \u0645\u0646 \u0627\u0644\u0642\u0627\u0644\u0628.") } }
  const editExercise = async (event: React.FormEvent<HTMLFormElement>) => { event.preventDefault(); const form = new FormData(event.currentTarget); const body: Schema<'ProgramExerciseUpdateRequest'> = { sets: Number(form.get('sets')), reps: String(form.get('reps') ?? ''), restSeconds: Number(form.get('restSeconds')), dayNumber: Number(form.get('dayNumber')), rowVersion: exerciseRowVersion || null }; try { await apiFetch<void>(`/programs/${id}/exercises/${programExerciseId}`, { method: 'PUT', body: JSON.stringify(body) }); setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0648\u062A\u062D\u062F\u064A\u062B \u0646\u0633\u062E\u0629 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C."); await cache.invalidateQueries({ queryKey: ['program', id] }); await cache.invalidateQueries({ queryKey: ['program', id, 'exercises'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u062A\u0645\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.") } }
  const removeExercise = async () => { if (!programExerciseId || !window.confirm("\u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0645\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C\u061F")) return; try { await apiFetch<void>(`/programs/${id}/exercises/${programExerciseId}`, { method: 'DELETE', body: JSON.stringify({ rowVersion: exerciseRowVersion || null } satisfies Schema<'ProgramVersionRequest'>) }); setMessage("\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0645\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C."); await cache.invalidateQueries({ queryKey: ['program', id] }); await cache.invalidateQueries({ queryKey: ['program', id, 'exercises'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u062A\u0645\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.") } }
  const reorderExercises = async (event: React.FormEvent<HTMLFormElement>) => { event.preventDefault(); const body: Schema<'ReorderProgramExercisesRequest'> = { dayNumber: Number(day), programExerciseIds: exerciseIdsOrder.split(',').map(value => value.trim()).filter(Boolean), rowVersion: exerciseRowVersion || null }; try { await apiFetch<void>(`/programs/${id}/exercises/reorder`, { method: 'PUT', body: JSON.stringify(body) }); setMessage("\u062A\u0645 \u062D\u0641\u0638 \u062A\u0631\u062A\u064A\u0628 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646."); await cache.invalidateQueries({ queryKey: ['program', id] }); await cache.invalidateQueries({ queryKey: ['program', id, 'exercises'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0631\u062A\u064A\u0628 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646.") } }
  const program = detail.data && typeof detail.data === 'object' ? detail.data as Record<string, unknown> : {}
  const exerciseRows = objectItems(programExercises.data)
  if (detail.isLoading) return <Loading />
  return <section className="nested-panel">
    <header className="section-heading"><div><span className="eyebrow">{t('Program details')}</span><h3>{String(program.title ?? t('Training program'))}</h3></div><span className="subscription-status">{t(String(program.status ?? 'Draft'))}</span></header>
    {detail.isError ? <ErrorState error={detail.error} /> : <dl className="data-grid program-summary"><div className="data-field"><dt>{t('Duration in days')}</dt><dd>{program.durationDays == null ? '\u2014' : <DataValue value={program.durationDays} />}</dd></div><div className="data-field"><dt>{t('Start date')}</dt><dd><DataValue value={program.startDate} /></dd></div><div className="data-field"><dt>{t('Client')}</dt><dd>{String(program.clientName ?? t('Not assigned'))}</dd></div></dl>}
    <section className="program-exercises-section"><h4>{t('Program exercises')}</h4>{programExercises.isLoading ? <Loading /> : programExercises.isError ? <ErrorState error={programExercises.error} /> : exerciseRows.length === 0 ? <p className="empty-state">{t('No exercises added yet.')}</p> : <div className="admin-table-scroll"><table className="admin-table"><thead><tr><th>{t('Exercise')}</th><th>{t('Day')}</th><th>{t('Sets')}</th><th>{t('Reps')}</th><th>{t('Rest seconds')}</th></tr></thead><tbody>{exerciseRows.map((item, index) => <tr key={String(item.programExerciseId ?? item.id ?? index)}><td>{String(item.exerciseName ?? item.name ?? t('Exercise'))}</td><td>{String(item.dayNumber ?? item.day ?? '\u2014')}</td><td>{String(item.sets ?? '\u2014')}</td><td>{String(item.reps ?? '\u2014')}</td><td>{String(item.restSeconds ?? '\u2014')}</td></tr>)}</tbody></table></div>}</section>
    <div className="program-editor-grid"><form className="form-grid" onSubmit={event => void update(event)}><h4>{t('Edit program')}</h4><label>{t('Program name')}<input value={title} onChange={event => setTitle(event.target.value)} required maxLength={160} /></label><label>{t('Duration in days')}<input type="number" min="1" max="365" value={durationDays} onChange={event => setDurationDays(event.target.value)} required /></label><button>{t('Save changes')}</button></form>
    <form className="form-grid" onSubmit={event => void addExercise(event)}><h4>{t('Add an exercise')}</h4><EntitySelect label={t('Exercise')} value={exerciseId} onChange={setExerciseId} data={exercises.data} /><label>{t('Sets')}<input type="number" min="1" max="20" value={sets} onChange={event => setSets(event.target.value)} required /></label><label>{t('Reps')}<input value={reps} onChange={event => setReps(event.target.value)} required /></label><label>{t('Day number')}<input type="number" min="1" max={Number(durationDays)} value={day} onChange={event => setDay(event.target.value)} required /></label><button disabled={!exerciseId}>{t('Add exercise')}</button></form></div>
    <form className="form-grid" onSubmit={event => void editExercise(event)}><h4>{t('Edit a program exercise')}</h4><EntitySelect label={t('Choose an exercise to edit')} value={programExerciseId} onChange={setProgramExerciseId} data={programExercises.data} /><label>{t('Sets')}<input name="sets" type="number" min="1" max="20" defaultValue="3" /></label><label>{t('Reps')}<input name="reps" defaultValue="10-12" /></label><label>{t('Rest seconds')}<input name="restSeconds" type="number" min="0" max="600" defaultValue="60" /></label><label>{t('Day number')}<input name="dayNumber" type="number" min="1" max={Number(durationDays)} defaultValue={day} /></label><div className="actions"><button disabled={!programExerciseId}>{t('Save exercise changes')}</button><button type="button" className="danger" disabled={!programExerciseId} onClick={() => void removeExercise()}>{t('Delete exercise')}</button></div></form>
    <form className="form-grid" onSubmit={event => void reorderExercises(event)}><h4>{t('Reorder exercises')}</h4><label>{t('Day number')}<input type="number" min="1" max={Number(durationDays)} value={day} onChange={event => setDay(event.target.value)} /></label><label>{t('Exercise order')}<select multiple value={exerciseIdsOrder.split(',').filter(Boolean)} onChange={event => setExerciseIdsOrder(Array.from(event.target.selectedOptions, option => option.value).join(','))}>{exerciseRows.map((item, index) => { const optionId = String(item.programExerciseId ?? item.id ?? ''); return <option key={optionId || index} value={optionId}>{String(item.exerciseName ?? item.name ?? `${t('Exercise')} ${index + 1}`)}</option> })}</select></label><button>{t('Save order')}</button></form>
    <div className="actions program-actions"><button onClick={() => void act('archive')}>{t('Archive program')}</button><button onClick={() => void act('duplicate')}>{t('Duplicate program')}</button><button className="danger" onClick={() => void act('delete')}>{t('Delete draft')}</button><button onClick={() => void saveTemplate()}>{t('Save as template')}</button></div>
    <section className="program-templates"><h4>{t('Templates')}</h4>{templates.isLoading ? <Loading /> : templates.isError ? <ErrorState error={templates.error} /> : objectItems(templates.data).length === 0 ? <p className="empty-state">{t('No templates available.')}</p> : null}<form className="form-grid" onSubmit={event => { event.preventDefault(); void createFromTemplate() }}><EntitySelect label={t('Choose a template')} value={templateId} onChange={setTemplateId} data={templates.data} /><EntitySelect label={t('Client')} value={templateClientId} onChange={setTemplateClientId} data={clients.data} /><button disabled={!templateId || !templateClientId}>{t('Create program from template')}</button></form></section>
    {message && <p role="status" className="form-feedback">{t(message)}</p>}
  </section>
}
function ExerciseLibrary() {
  const cache = useQueryClient()
  const { t } = useLanguage()
  const [name, setName] = useState('')
  const [muscleGroup, setMuscleGroup] = useState('')
  const [equipment, setEquipment] = useState('')
  const [videoUrl, setVideoUrl] = useState('')
  const [search, setSearch] = useState('')
  const [exerciseId, setExerciseId] = useState('')
  const [editName, setEditName] = useState('')
  const [editMuscleGroup, setEditMuscleGroup] = useState('')
  const [editEquipment, setEditEquipment] = useState('')
  const [editVideoUrl, setEditVideoUrl] = useState('')
  const [page, setPage] = useState(1)
  const [message, setMessage] = useState('')
  const query = useQuery({ queryKey: ['exercises', muscleGroup, equipment, search, page], queryFn: () => apiFetch<unknown>(`/exercises?page=${page}&pageSize=20&muscleGroup=${encodeURIComponent(muscleGroup)}&equipment=${encodeURIComponent(equipment)}&search=${encodeURIComponent(search)}`) })
  const meta = useQuery({ queryKey: ['exercises', 'meta'], queryFn: () => apiFetch<unknown>('/exercises/meta') })
  const detail = useQuery({ queryKey: ['exercise', exerciseId], queryFn: () => apiFetch<unknown>(`/exercises/${exerciseId}`), enabled: Boolean(exerciseId) })
  const rows = objectItems(query.data)
  const metaValue = meta.data && typeof meta.data === 'object' ? meta.data as Record<string, unknown> : {}
  const muscleGroups = Array.isArray(metaValue.muscleGroups) ? metaValue.muscleGroups.filter((item): item is string => typeof item === 'string') : []
  const equipmentOptions = Array.isArray(metaValue.equipment) ? metaValue.equipment.filter((item): item is string => typeof item === 'string') : []
  const total = Number((query.data as Record<string, unknown> | undefined)?.totalCount ?? (query.data as Record<string, unknown> | undefined)?.total ?? rows.length)
  const edit = detail.data && typeof detail.data === 'object' ? detail.data as Record<string, unknown> : null
  useEffect(() => {
    if (!detail.data || typeof detail.data !== 'object') return
    const selected = detail.data as Record<string, unknown>
    setEditName(String(selected.name ?? ''))
    setEditMuscleGroup(String(selected.muscleGroup ?? ''))
    setEditEquipment(String(selected.equipment ?? ''))
    setEditVideoUrl(String(selected.videoUrl ?? ''))
  }, [detail.data])
  const add = async (event: React.FormEvent) => {
    event.preventDefault()
    const body: Schema<'CreateExerciseRequest'> = { name: name.trim(), muscleGroup: muscleGroup.trim(), equipment: equipment.trim() || null, videoUrl: videoUrl.trim() || null }
    try {
      await apiFetch<unknown>('/exercises', { method: 'POST', body: JSON.stringify(body) })
      setName(''); setMuscleGroup(''); setEquipment(''); setVideoUrl('')
      setMessage('Exercise added to your library.')
      await cache.invalidateQueries({ queryKey: ['exercises'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not add the exercise.') }
  }
  const update = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!exerciseId) return
    const body: Schema<'ExerciseRequest'> = { name: editName.trim(), muscleGroup: editMuscleGroup.trim(), equipment: editEquipment.trim() || null, videoUrl: editVideoUrl.trim() || null }
    try {
      await apiFetch<void>(`/exercises/${exerciseId}`, { method: 'PUT', body: JSON.stringify(body) })
      setMessage('Exercise details saved.')
      await cache.invalidateQueries({ queryKey: ['exercise', exerciseId] })
      await cache.invalidateQueries({ queryKey: ['exercises'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save the exercise.') }
  }
  const remove = async () => {
    if (!exerciseId || !window.confirm(t('Delete this exercise?'))) return
    try {
      await apiFetch<void>(`/exercises/${exerciseId}`, { method: 'DELETE' })
      setMessage('Exercise deleted.')
      setExerciseId('')
      await cache.invalidateQueries({ queryKey: ['exercises'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not delete the exercise.') }
  }
  const choose = (id: string) => { setExerciseId(id); setMessage('') }
  return <Page title={t('Exercise library')} subtitle={t('Find, add, and maintain the exercises used in your training programs.')}>
    <section className="panel exercise-library-tools">
      <div className="section-heading"><div><h2>{t('Filter exercises')}</h2><p>{t('Narrow the library by muscle group, equipment, or name.')}</p></div><span className="count-badge">{t('Muscle groups')}: {muscleGroups.length} آ· {t('Equipment types')}: {equipmentOptions.length}</span></div>
      {meta.isError && <ErrorState error={meta.error} />}
      <div className="form-grid"><label>{t('Search exercises')}<input value={search} onChange={event => { setSearch(event.target.value); setPage(1) }} placeholder={t('Search by exercise name')} /></label><label>{t('Muscle group')}<select value={muscleGroup} onChange={event => { setMuscleGroup(event.target.value); setPage(1) }}><option value="">{t('All muscle groups')}</option>{muscleGroups.map(option => <option value={option} key={option}>{option}</option>)}</select></label><label>{t('Equipment')}<select value={equipment} onChange={event => { setEquipment(event.target.value); setPage(1) }}><option value="">{t('All equipment')}</option>{equipmentOptions.map(option => <option value={option} key={option}>{option}</option>)}</select></label></div>
    </section>
    <section className="panel"><div className="section-heading"><div><h2>{t('Add an exercise')}</h2><p>{t('Add a name and muscle group. An HTTPS video link is optional.')}</p></div></div><form className="form-grid" onSubmit={event => void add(event)}><label>{t('Exercise name')}<input value={name} onChange={event => setName(event.target.value)} required maxLength={160} /></label><label>{t('Muscle group')}<input list="exercise-muscle-groups" value={muscleGroup} onChange={event => setMuscleGroup(event.target.value)} required maxLength={80} /><datalist id="exercise-muscle-groups">{muscleGroups.map(option => <option value={option} key={option} />)}</datalist></label><label>{t('Equipment')}<input list="exercise-equipment-options" value={equipment} onChange={event => setEquipment(event.target.value)} maxLength={80} /><datalist id="exercise-equipment-options">{equipmentOptions.map(option => <option value={option} key={option} />)}</datalist></label><label>{t('Video link (optional)')}<input type="url" inputMode="url" value={videoUrl} onChange={event => setVideoUrl(event.target.value)} placeholder="https://..." /></label><button type="submit" disabled={!name.trim() || !muscleGroup.trim()}>{t('Add exercise')}</button></form>
    </section>
    {message && <p role="status" className="inline-notice">{t(message)}</p>}
    <section className="panel"><div className="section-heading"><div><h2>{t('Exercises')}</h2><p>{t('Select an exercise to view its video and edit its details.')}</p></div><span className="count-badge">{total} {t('exercises')}</span></div>{query.isLoading ? <Loading /> : query.isError ? <ErrorState error={query.error} /> : rows.length === 0 ? <div className="empty-state">{t('No exercises match these filters.')}</div> : <div className="program-card-grid exercise-card-grid exercise-library-grid">{rows.map((item, index) => { const id = String(item.id ?? ''); const selected = exerciseId === id; return <button type="button" className={selected ? 'exercise-library-card selected' : 'exercise-library-card'} key={id || index} aria-pressed={selected} onClick={() => choose(id)}><span className="exercise-card-topline"><span className="exercise-card-icon" aria-hidden="true"><Activity size={20} /></span><span className="exercise-card-index">{String((page - 1) * 20 + index + 1).padStart(2, '0')}</span></span><span className="exercise-card-title">{String(item.name ?? t('Exercise'))}</span><span className="exercise-card-tags"><span><small>{t('Muscle')}</small><b>{String(item.muscleGroup ?? t('Not specified'))}</b></span><span><small>{t('Equipment')}</small><b>{String(item.equipment ?? t('Not specified'))}</b></span></span><span className="exercise-card-bottom">{(typeof item.videoUrl === 'string' && item.videoUrl.length > 0) ? <span className="exercise-video-indicator"><span aria-hidden="true">â–¶</span> {t('Video available')}</span> : <span className="exercise-video-indicator muted">{t('No video link')}</span>}<span className="exercise-card-open">{selected ? t('Selected') : t('View details')}</span></span></button> })}</div>}
      <div className="pagination-actions"><button type="button" className="secondary-button" onClick={() => setPage(current => Math.max(1, current - 1))} disabled={page <= 1}>{t('Previous')}</button><span>{t('Page')} {page}</span><button type="button" className="secondary-button" onClick={() => setPage(current => current + 1)} disabled={rows.length < 20 || page * 20 >= total}>{t('Next')}</button></div>
    </section>
    {exerciseId && <section className="panel"><div className="section-heading"><div><h2>{t('Exercise details')}</h2><p>{t('Update its information, video link, or remove it from the library.')}</p></div><button className="secondary-button" type="button" onClick={() => setExerciseId('')}>{t('Close details')}</button></div>{detail.isLoading ? <Loading /> : detail.isError ? <ErrorState error={detail.error} /> : <>{edit?.videoUrl && <ExerciseMedia value={detail.data} />}<form className="form-grid" onSubmit={event => void update(event)}><label>{t('Exercise name')}<input value={editName} onChange={event => setEditName(event.target.value)} required maxLength={160} /></label><label>{t('Muscle group')}<input value={editMuscleGroup} onChange={event => setEditMuscleGroup(event.target.value)} required maxLength={80} /></label><label>{t('Equipment')}<input value={editEquipment} onChange={event => setEditEquipment(event.target.value)} maxLength={80} /></label><label>{t('Video link (optional)')}<input type="url" inputMode="url" value={editVideoUrl} onChange={event => setEditVideoUrl(event.target.value)} placeholder="https://..." /></label><div className="actions"><button type="submit" disabled={!editName.trim() || !editMuscleGroup.trim()}>{t('Save changes')}</button><button type="button" className="danger" onClick={() => void remove()}>{t('Delete exercise')}</button></div></form></>}</section>}
  </Page>
}function MealPlanWorkspace() {
  const cache = useQueryClient()
  const [searchParams] = useSearchParams()
  const query = useQuery({ queryKey: ['meal-plans'], queryFn: () => apiFetch<unknown>('/meal-plans?page=1&pageSize=100') })
  const [title, setTitle] = useState('')
  const [planId, setPlanId] = useState(() => searchParams.get('planId') ?? '')
  const [message, setMessage] = useState('')
  const plans = objectItems(query.data)
  const create = async (event: React.FormEvent) => { event.preventDefault(); try { const result = await apiFetch<unknown>('/meal-plans', { method: 'POST', body: JSON.stringify({ title, isTemplate: false } satisfies Schema<'MealPlanCreate'>) }); setPlanId(responseId(result) ?? ''); setTitle(''); setMessage('Draft created. Add meals and nutrition details, then assign it to a client.'); await cache.invalidateQueries({ queryKey: ['meal-plans'] }) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not create meal plan.') } }
  return <Page title="Meal plans" subtitle="Create a meal plan, add nutrition details, and assign it to a client."><section className="panel"><h2>Create a meal plan</h2><form className="form-grid" onSubmit={event => void create(event)}><label>Plan name<input value={title} onChange={event => setTitle(event.target.value)} required maxLength={160} placeholder="Example: Balanced nutrition" /></label><button>Create draft</button></form>{message && <p role="status" className="form-feedback">{message}</p>}</section><section className="panel"><div className="section-heading"><div><h2>Workspace meal plans</h2><p>Open a plan to manage meals or assign it.</p></div><span className="count-badge">{plans.length} plans</span></div>{query.isLoading ? <Loading /> : query.isError ? <ErrorState error={query.error} /> : plans.length===0 ? <div className="empty-state">No meal plans yet.</div> : <div className="program-card-grid">{plans.map((item,index)=>{const id=String(item.id??'');return <article className="program-list-card" key={id||index}><div><span className={'subscription-status status-'+String(item.status??'draft').toLowerCase()}>{String(item.status??'Draft')}</span><h3>{String(item.title??'Meal plan')}</h3><p>{item.clientName?`Client: ${String(item.clientName)}`:'Not assigned'}</p></div><button type="button" onClick={()=>setPlanId(id)} disabled={!id}>Manage plan</button></article>})}</div>}{planId && <MealPlanActions key={planId} id={planId} />}</section></Page>
}function MealPlanActions({ id }: { id: string }) {
  const cache = useQueryClient()
  const clients = useClients()
  const detail = useQuery({ queryKey: ['meal-plan', id], queryFn: () => apiFetch<unknown>(`/meal-plans/${id}`) })
  const mealOptions = { items: nestedRecords(detail.data).filter(meal => typeof meal.mealId === 'string' || typeof meal.mealName === 'string' || typeof meal.calories === 'number') }
  const [title, setTitle] = useState('')
  const [rowVersion, setRowVersion] = useState('')
  const [mealId, setMealId] = useState('')
  const [clientId, setClientId] = useState('')
  const [mealName, setMealName] = useState('')
  const [calories, setCalories] = useState('')
  const [protein, setProtein] = useState('')
  const [carbs, setCarbs] = useState('')
  const [fat, setFat] = useState('')
  const [message, setMessage] = useState('')
  useEffect(() => {
    if (detail.data && typeof detail.data === 'object') {
      const value = detail.data as Record<string, unknown>
      if (typeof value.rowVersion === 'string') setRowVersion(value.rowVersion)
      if (typeof value.title === 'string') setTitle(value.title)
    }
  }, [detail.data])
  const update = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'MealPlanUpdate'> = { title, rowVersion }; try { await apiFetch<void>(`/meal-plans/${id}`, { method: 'PUT', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['meal-plan', id] }); setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u062E\u0637\u0629.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u062E\u0637\u0629.") } }
  const addMeal = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'MealRequest'> = { name: mealName, calories: Number(calories), protein: Number(protein), carbs: Number(carbs), fat: Number(fat) }; try { await apiFetch<unknown>(`/meal-plans/${id}/meals`, { method: 'POST', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['meal-plan', id] }); setMessage("\u062A\u0645\u062A \u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u0648\u062C\u0628\u0629.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631\u062A \u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u0648\u062C\u0628\u0629.") } }
  const updateMeal = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'MealRequest'> = { name: mealName, calories: Number(calories), protein: Number(protein), carbs: Number(carbs), fat: Number(fat) }; try { await apiFetch<void>(`/meal-plans/${id}/meals/${mealId}`, { method: 'PUT', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['meal-plan', id] }); setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0648\u062C\u0628\u0629.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0648\u062C\u0628\u0629.") } }
  const assign = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'MealPlanAssign'> = { clientId }; try { await apiFetch<void>(`/meal-plans/${id}/assign`, { method: 'PUT', body: JSON.stringify(body) }); setMessage("\u062A\u0645 \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u062E\u0637\u0629 \u0644\u0644\u0639\u0645\u064A\u0644.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u062E\u0637\u0629.") } }
  if (detail.isLoading) return <Loading />
  return <div className="nested-panel"><h3>طھظپط§طµï؟½? ظ„ ط§ظ„ط®ط·ط©</h3>{detail.isError ? <ErrorState error={detail.error} /> : <JsonPanel value={detail.data} />}<form onSubmit={event => void update(event)}><label>ط¹ï؟½? ظˆط§ï؟½?  ط§ظ„ط®ط·ط©<input value={title} onChange={event => setTitle(event.target.value)} required /></label><button>طھط­ط¯ï؟½? ط« ط§ظ„ط®ط·ط©</button></form><form onSubmit={event => void addMeal(event)}><label>ط§ط³ظ… ط§ظ„ظˆط¬ط¨ط©<input value={mealName} onChange={event => setMealName(event.target.value)} required /></label><label>ط§ظ„ط³ط¹ط±ط§طھ<input type="number" min="0" value={calories} onChange={event => setCalories(event.target.value)} /></label><label>Protein<input type="number" min="0" value={protein} onChange={event => setProtein(event.target.value)} /></label><label>Carbs<input type="number" min="0" value={carbs} onChange={event => setCarbs(event.target.value)} /></label><label>Fat<input type="number" min="0" value={fat} onChange={event => setFat(event.target.value)} /></label><button>ط£ع©ط¢آ¥ط£ع©ط¢آ¶ط£ع©ط¢آ§ط£â„¢ط¢ظ¾ط£ع©ط¢آ© ط£â„¢ط«â€ ط£ع©ط¢آ¬ط£ع©ط¢آ¨ط£ع©ط¢آ©</button></form><form onSubmit={event => void updateMeal(event)}><EntitySelect label={'\u0627\u062E\u062A\u0631 \u0627\u0644\u0648\u062C\u0628\u0629'} value={mealId} onChange={setMealId} data={mealOptions} /><button>طھط­ط¯ï؟½? ط« ط§ظ„ظˆط¬ط¨ط©</button><button type="button" className="danger" onClick={async () => { try { await apiFetch<void>(`/meal-plans/${id}/meals/${mealId}`, { method: 'DELETE' }); await cache.invalidateQueries({ queryKey: ['meal-plan', id] }); setMealId(''); setMessage("\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u0648\u062C\u0628\u0629.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u0648\u062C\u0628\u0629.") } }} disabled={!mealId}>ط­ط°ظپ ط§ظ„ظˆط¬ط¨ط©</button></form><form onSubmit={event => void assign(event)}><EntitySelect label={'\u0627\u0644\u0639\u0645\u064A\u0644'} value={clientId} onChange={setClientId} data={clients.data} /><button>ط¥ط³ï؟½? ط§ط¯ ط§ظ„ط®ط·ط©</button></form><div className="actions"><button onClick={async () => { try { await apiFetch<void>(`/meal-plans/${id}`, { method: 'DELETE' }); setMessage("\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u062E\u0637\u0629.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u062E\u0637\u0629.") } }}>ط­ط°ظپ ظ…ط³ظˆط¯ط©</button></div>{message && <p>{message}</p>}</div>
}
function TenantPlans() {
  const { t } = useLanguage()
  const cache = useQueryClient()
  const plans = useQuery({ queryKey: ['tenant', 'plans'], queryFn: () => apiFetch<unknown>('/plans') })
  const [name, setName] = useState('')
  const [price, setPrice] = useState('')
  const [durationDays, setDurationDays] = useState('30')
  const [message, setMessage] = useState('')
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const selectedPlan = useQuery({ queryKey: ['tenant', 'plan', selectedPlanId], queryFn: () => apiFetch<unknown>(`/plans/${selectedPlanId}`), enabled: Boolean(selectedPlanId) })
  const create = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'CreatePlanRequest'> = { name, price: Number(price), durationDays: Number(durationDays) }; try { await apiFetch<unknown>('/tenant/plans', { method: 'POST', body: JSON.stringify(body) }); setName(''); setPrice(''); setMessage(t('Plan created. Clients can now request it.')); await cache.invalidateQueries({ queryKey: ['tenant', 'plans'] }); await cache.invalidateQueries({ queryKey: ['public', 'plans'] }) } catch (error) { setMessage(error instanceof Error ? error.message : t('Could not create the plan.')) } }
  const update = async (event: React.FormEvent<HTMLFormElement>) => { event.preventDefault(); const form = new FormData(event.currentTarget); const body: Schema<'PlanUpdate'> = { name: String(form.get('name') ?? ''), price: Number(form.get('price')), durationDays: Number(form.get('durationDays')) }; try { await apiFetch<void>(`/tenant/plans/${selectedPlanId}`, { method: 'PUT', body: JSON.stringify(body) }); setMessage(t('Plan updated. Existing subscriptions keep their original price and duration.')); await cache.invalidateQueries({ queryKey: ['tenant', 'plans'] }); await cache.invalidateQueries({ queryKey: ['tenant', 'plan', selectedPlanId] }) } catch (error) { setMessage(error instanceof Error ? error.message : t('Could not update the plan.')) } }
  const archive = async () => { if (!selectedPlanId || !window.confirm(t('Archive this plan? Clients can no longer request it.'))) return; try { await apiFetch<void>(`/tenant/plans/${selectedPlanId}/archive`, { method: 'PUT' }); setMessage(t('Plan archived. Existing subscriptions are unchanged.')); await cache.invalidateQueries({ queryKey: ['tenant', 'plans'] }); await cache.invalidateQueries({ queryKey: ['tenant', 'plan', selectedPlanId] }) } catch (error) { setMessage(error instanceof Error ? error.message : t('Could not archive the plan.')) } }
  const planRows = objectItems(plans.data)
  const selected = selectedPlan.data && typeof selectedPlan.data === 'object' ? selectedPlan.data as Record<string, unknown> : {}
  return <Page title={t('Plans')} subtitle={t('Create clear membership offers and manage what clients can request.')}>
    <section className="panel plan-setup-panel"><div className="section-heading"><div><span className="eyebrow">{t('STEP 1')}</span><h2>{t('Create a membership plan')}</h2><p>{t('Set the price and access period. Clients can request an active plan from their subscription page.')}</p></div></div>
      <form className="form-grid plan-create-form" onSubmit={event => void create(event)}><label>{t('Plan name')}<input value={name} onChange={event => setName(event.target.value)} placeholder={t('Example: 12-week coaching')} maxLength={120} required /></label><label>{t('Price')}<input type="number" min="0" step="0.01" value={price} onChange={event => setPrice(event.target.value)} required /></label><label>{t('Duration in days')}<input type="number" min="1" max="3650" value={durationDays} onChange={event => setDurationDays(event.target.value)} required /></label><button disabled={plans.isPending}>{t('Create plan')}</button></form>{message && <p role="status" className="form-feedback">{message}</p>}
    </section>
    <section className="panel"><div className="section-heading"><div><span className="eyebrow">{t('STEP 2')}</span><h2>{t('Your membership plans')}</h2><p>{t('Active plans appear to clients. Existing subscriptions keep their original price and duration.')}</p></div><span className="count-badge">{planRows.length} {t('plans')}</span></div>
      {plans.isLoading ? <Loading /> : plans.isError ? <ErrorState error={plans.error} /> : planRows.length === 0 ? <div className="empty-state"><strong>{t('No plans yet')}</strong><p>{t('Create your first plan above. Then clients can request it and pay online, by InstaPay or through a recorded cash payment.')}</p></div> : <div className="owner-plan-grid">{planRows.map((plan,index) => { const id=String(plan.id??''); const currency=/^[A-Z]{3}$/.test(String(plan.currency??''))?String(plan.currency):'EGP'; return <article className="owner-plan-card" key={id||index}><div className="owner-plan-card-heading"><span className={'status-pill '+(plan.isActive===false?'':'positive')}>{t(plan.isActive===false?'Archived':'Active')}</span><strong>{String(plan.name??t('Membership plan'))}</strong></div><div className="owner-plan-price">{new Intl.NumberFormat(undefined,{style:'currency',currency}).format(Number(plan.price??0))}</div><p>{String(plan.durationDays??'أ¢â‚¬â€‌')} {t('days of access')}</p><button type="button" className="secondary-button" onClick={()=>setSelectedPlanId(id)}>{t('Edit plan')}</button></article> })}</div>}
    </section>
    <section className="panel"><div className="section-heading"><div><span className="eyebrow">{t('STEP 3')}</span><h2>{t('Edit or archive a plan')}</h2><p>{t('Archiving prevents new requests but preserves existing subscriptions.')}</p></div></div><EntitySelect label={t('Choose a plan')} value={selectedPlanId} onChange={setSelectedPlanId} data={plans.data} />{selectedPlan.isLoading&&selectedPlanId&&<Loading/>}{selectedPlan.isError&&<ErrorState error={selectedPlan.error}/>}<>{selectedPlanId&&selectedPlan.data&&<><form className="form-grid" key={selectedPlanId} onSubmit={event=>void update(event)}><label>{t('Plan name')}<input name="name" defaultValue={String(selected.name??'')} required /></label><label>{t('Price')}<input name="price" type="number" min="0" step="0.01" defaultValue={String(selected.price??'')} required /></label><label>{t('Duration in days')}<input name="durationDays" type="number" min="1" max="3650" defaultValue={String(selected.durationDays??'')} required /></label><button>{t('Save changes')}</button></form><button className="danger" onClick={()=>void archive()}>{t('Archive plan')}</button></>}</></section>
    <section className="owner-flow-next"><div><strong>{t('Next: build your coaching offer')}</strong><p>{t('Add exercises and create training programs before assigning a program to a client.')}</p></div><Link className="secondary-button" to="/app/exercises">{t('Open exercise library')}</Link><Link className="primary-button" to="/app/subscriptions">{t('Manage subscriptions')}</Link></section>
  </Page>
}function Clients() {
  const { t } = useLanguage()
  const query = useClients()
  const me = useMe()
  const cache = useQueryClient()
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const invitations = useQuery({ queryKey: ['client-invitations'], queryFn: () => apiFetch<unknown>('/clients/invitations?page=1&pageSize=20') })
  const coaches = useQuery({ queryKey: ['tenant', 'coaches'], queryFn: () => apiFetch<unknown>('/tenant/coaches?page=1&pageSize=100'), enabled: me.data?.role === 'TenantOwner' })
  const [email, setEmail] = useState('')
  const [inviteName, setInviteName] = useState('')
  const [message, setMessage] = useState('')
  const [invitationToken, setInvitationToken] = useState('')
  const invite = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'CreateClientInvitationRequest'> = { email, name: inviteName || null }; try { const result = await apiFetch<unknown>('/clients/invite', { method: 'POST', body: JSON.stringify(body) }); setInvitationToken(result && typeof result === 'object' && 'invitationToken' in result && typeof result.invitationToken === 'string' ? result.invitationToken : ''); setEmail(''); setInviteName(''); setMessage("\u062A\u0645 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062F\u0639\u0648\u0629."); await cache.invalidateQueries({ queryKey: queryKeys.clients }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062F\u0639\u0648\u0629.") } }
  if (query.isLoading) return <Loading />
  if (query.isError) return <ErrorState error={query.error} />
  return <Page title={'\u0627\u0644\u0639\u0645\u0644\u0627\u0621'} subtitle={'\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0648\u0627\u0644\u062F\u0639\u0648\u0627\u062A \u0648\u0645\u062A\u0627\u0628\u0639\u0629 \u0645\u0633\u062A\u0648\u0649 \u0627\u0644\u0645\u062E\u0627\u0637\u0631'}><section className="panel"><h2>ط¯ط¹ظˆط© ط¹ظ…ï؟½? ظ„</h2><form onSubmit={event => void invite(event)}><label>ط§ظ„ط§ط³ظ…<input value={inviteName} onChange={event => setInviteName(event.target.value)} /></label><label>ط§ظ„ط¨ط±ï؟½? ط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆï؟½? ï؟½? <input type="email" value={email} onChange={event => setEmail(event.target.value)} required /></label><button>ط¥ط±ط³ط§ظ„ ط¯ط¹ظˆط©</button></form>{message && <p>{message}</p>}{invitationToken && <p>ط±ظ…ط² ط§ظ„ط¯ط¹ظˆط© ظ„ظ„طھط·ظˆï؟½? ط±: <code>{invitationToken}</code> ط£â€ڑ |  <Link to={`/register/client?token=${encodeURIComponent(invitationToken)}`}>ظپطھط­ طھط³ط¬ï؟½? ظ„ ط§ظ„ط¹ظ…ï؟½? ظ„</Link></p>}</section><section className="panel"><h2>ط§ظ„ط¯ط¹ظˆط§طھ</h2>{invitations.isLoading ? <Loading /> : invitations.isError ? <ErrorState error={invitations.error} /> : objectItems(invitations.data).map(invitation => <InvitationRow key={stringField(invitation, 'id')} invitation={invitation} />)}</section>{me.data?.role === 'TenantOwner' && <section className="panel coach-team-entry"><div><h2>{t('Coach team')}</h2><p>{t('Manage staff access and assign clients to their responsible coach.')}</p></div><span className="count-badge">{objectItems(coaches.data).filter(row => row.role === 'CoachStaff').length} {t('coaches')}</span><Link className="primary-button" to="/app/team">{t('Manage coaches and assignments')}</Link></section>}<section className="panel table-wrap"><table><thead><tr>{["\u0627\u0644\u0627\u0633\u0645", "\u0627\u0644\u0628\u0631\u064A\u062F", "\u0627\u0644\u062D\u0627\u0644\u0629", "\u0645\u0633\u062A\u0648\u0649 \u0627\u0644\u0645\u062E\u0627\u0637\u0631"].map(header => <th key={header}>{header}</th>)}</tr></thead><tbody>{(query.data?.items ?? []).map(client => <tr key={client.id} onClick={() => setSelectedId(client.id ?? null)} className="clickable"><td>{client.name ?? 'â€”'}</td><td>{client.email ?? 'â€”'}</td><td>{client.status ?? 'â€”'}</td><td>{client.riskLevel ?? 'â€”'}</td></tr>)}</tbody></table></section>{selectedId && <><ClientDetails id={selectedId} canAssignCoach={me.data?.role === 'TenantOwner'} /><ClientEditActions id={selectedId} /><ClientAdditionalReads id={selectedId} /></>}</Page>
}
function InvitationRow({ invitation }: { invitation: Record<string, unknown> }) {
  const cache = useQueryClient()
  const id = stringField(invitation, 'id')
  const update = async (action: 'resend' | 'delete') => { if (!id) return; try { if (action === 'resend') await apiFetch<void>(`/clients/invitations/${id}/resend`, { method: 'POST' }); else await apiFetch<void>(`/clients/invitations/${id}`, { method: 'DELETE' }); await cache.invalidateQueries({ queryKey: ['client-invitations'] }) } catch (error) { window.alert(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u062F\u0639\u0648\u0629.") } }
  return <div className="invitation-row"><span>{stringField(invitation, 'email') ?? 'â€”'} آ· {stringField(invitation, 'status') ?? 'â€”'}</span>{stringField(invitation, 'status') === 'Pending' && <><button onClick={() => void update('resend')}>ط¥ط¹ط§ط¯ط© ط§ظ„ط¥ط±ط³ط§ظ„</button><button onClick={() => void update('delete')}>ط¥ظ„ط؛ط§ط، ط§ظ„ط¯ط¹ظˆط©</button></>}</div>
}

function CoachTeamWorkspace() {
  const me = useMe(); const cache = useQueryClient(); const { t } = useLanguage()
  const coaches = useQuery({ queryKey: ['tenant', 'coaches'], queryFn: () => apiFetch<unknown>('/tenant/coaches?page=1&pageSize=100'), enabled: me.data?.role === 'TenantOwner' })
  const clientsQuery = useClients(1, 100)
  const [coachId, setCoachId] = useState(''); const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [displayName, setDisplayName] = useState(''); const [editEmail, setEditEmail] = useState(''); const [clientId, setClientId] = useState(''); const [message, setMessage] = useState(''); const [busy, setBusy] = useState(false)
  const detail = useQuery({ queryKey: ['tenant', 'coach', coachId], queryFn: () => apiFetch<unknown>(`/tenant/coaches/${coachId}`), enabled: Boolean(coachId) && me.data?.role === 'TenantOwner' })
  const assignedResponse = useQuery({ queryKey: ['tenant', 'coach', coachId, 'clients'], queryFn: () => apiFetch<unknown>(`/tenant/coaches/${coachId}/clients?page=1&pageSize=100`), enabled: Boolean(coachId) && me.data?.role === 'TenantOwner' })
  const staff = objectItems(coaches.data).filter(row => row.role === 'CoachStaff'); const allClients = objectItems(clientsQuery.data); const selected = staff.find(row => String(row.id ?? '') === coachId)
  const assigned = allClients.filter(row => String(row.coachId ?? '') === coachId); const available = allClients.filter(row => String(row.coachId ?? '') !== coachId && String(row.status ?? 'Active') !== 'Archived')
  useEffect(() => { const row = detail.data && typeof detail.data === 'object' ? detail.data as Record<string, unknown> : null; setDisplayName(String(row?.name ?? '')); setEditEmail(String(row?.email ?? '')) }, [coachId, detail.data])
  if (me.data?.role !== 'TenantOwner') return <Navigate to="/app/dashboard" replace />
  const create = async (event: React.FormEvent) => { event.preventDefault(); setBusy(true); setMessage(''); try { await apiFetch<void>('/tenant/coaches', { method: 'POST', body: JSON.stringify({ email, password } satisfies Schema<'CreateCoachRequest'>) }); setEmail(''); setPassword(''); setMessage('Coach added successfully.'); await cache.invalidateQueries({ queryKey: ['tenant', 'coaches'] }) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not add the coach.') } finally { setBusy(false) } }
  const save = async (event: React.FormEvent) => { event.preventDefault(); setBusy(true); try { await apiFetch<void>(`/tenant/coaches/${coachId}`, { method: 'PUT', body: JSON.stringify({ name: displayName || null, email: editEmail || null } satisfies Schema<'UpdateCoachRequest'>) }); setMessage('Coach details saved.'); await Promise.all([cache.invalidateQueries({ queryKey: ['tenant', 'coaches'] }), cache.invalidateQueries({ queryKey: ['tenant', 'coach', coachId] })]) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save coach details.') } finally { setBusy(false) } }
  const setAccess = async (action: 'activate' | 'deactivate') => { setBusy(true); try { await apiFetch<void>(`/tenant/coaches/${coachId}/${action}`, { method: 'PUT' }); setMessage(action === 'activate' ? 'Coach access enabled.' : 'Coach access paused.'); await Promise.all([cache.invalidateQueries({ queryKey: ['tenant', 'coaches'] }), cache.invalidateQueries({ queryKey: ['tenant', 'coach', coachId] })]) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not change coach access.') } finally { setBusy(false) } }
  const assign = async (event: React.FormEvent) => { event.preventDefault(); if (!clientId || busy) return; setBusy(true); try { await apiFetch<void>(`/clients/${clientId}/assign-coach`, { method: 'PUT', body: JSON.stringify({ coachId } satisfies Schema<'AssignCoachRequest'>) }); setMessage('Client assigned to this coach.'); setClientId(''); await Promise.all([cache.invalidateQueries({ queryKey: queryKeys.clients }), cache.invalidateQueries({ queryKey: ['tenant', 'coach', coachId, 'clients'] }), cache.invalidateQueries({ queryKey: ['tenant', 'coaches'] }), cache.invalidateQueries({ queryKey: ['client', clientId] })]) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not assign this client.') } finally { setBusy(false) } }
  return <Page title="Coach team" subtitle="Manage staff access and assign each client to their responsible coach.">
    <section className="panel coach-team-create"><div className="section-heading"><div><h2>Add a coach</h2><p>Create a staff account using their email and temporary password.</p></div></div><form className="form-grid" onSubmit={event => void create(event)}><label>Email address<input type="email" value={email} onChange={event => setEmail(event.target.value)} required autoComplete="off" placeholder="coach@example.com" /></label><label>Temporary password<input type="password" minLength={8} value={password} onChange={event => setPassword(event.target.value)} required autoComplete="new-password" /></label><button disabled={busy}>{busy ? 'Saving...' : 'Add coach'}</button></form>{message && <p className="form-feedback" role="status">{message}</p>}</section>
    <section className="panel coach-team-workspace"><div className="section-heading"><div><h2>Staff coaches</h2><p>Select a coach to manage their account and clients.</p></div><span className="count-badge">{staff.length} coaches</span></div>{coaches.isLoading ? <Loading /> : coaches.isError ? <ErrorState error={coaches.error} /> : staff.length === 0 ? <div className="empty-state">No staff coaches yet. Add the first coach above.</div> : <div className="coach-team-grid">{staff.map((row, index) => { const id = String(row.id ?? ''); return <button type="button" key={id || index} className={coachId === id ? 'coach-team-card selected' : 'coach-team-card'} onClick={() => { setCoachId(id); setMessage('') }}><span className="coach-team-avatar">{String(row.name ?? row.email ?? 'C').slice(0, 1).toUpperCase()}</span><span className="coach-team-card-copy"><strong>{String(row.name ?? 'Coach')}</strong><small>{String(row.email ?? '')}</small></span><span className="coach-team-count">{allClients.filter(client => String(client.coachId ?? '') === id).length} clients</span></button> })}</div>}</section>
    {coachId && <section className="panel coach-team-detail">{detail.isLoading ? <Loading /> : detail.isError ? <ErrorState error={detail.error} /> : <><div className="section-heading"><div><span className="eyebrow">Coach profile</span><h2>{String((detail.data as Record<string, unknown> | undefined)?.name ?? selected?.name ?? 'Coach')}</h2><p>{String((detail.data as Record<string, unknown> | undefined)?.email ?? selected?.email ?? '')}</p></div><span className={((detail.data as Record<string, unknown> | undefined)?.isActive === true) ? 'status-pill positive' : 'status-pill'}>{((detail.data as Record<string, unknown> | undefined)?.isActive === true) ? 'Active' : 'Inactive'}</span></div><form className="form-grid coach-profile-form" onSubmit={event => void save(event)}><label>Display name<input value={displayName} onChange={event => setDisplayName(event.target.value)} maxLength={150} placeholder="Coach name" /></label><label>Email address<input type="email" value={editEmail} onChange={event => setEditEmail(event.target.value)} maxLength={255} required /></label><button disabled={busy}>{busy ? 'Saving...' : 'Save changes'}</button></form><div className="actions coach-access-actions"><button type="button" disabled={busy || ((detail.data as Record<string, unknown> | undefined)?.isActive === true)} onClick={() => void setAccess('activate')}>Enable access</button><button type="button" className="secondary-button" disabled={busy || ((detail.data as Record<string, unknown> | undefined)?.isActive !== true)} onClick={() => void setAccess('deactivate')}>Pause access</button></div></>}
      <section className="coach-assignment-panel"><div className="section-heading"><div><h3>Clients assigned to this coach</h3><p>Assign an unassigned client or move one from another coach.</p></div><span className="count-badge">{assigned.length} clients</span></div>{clientsQuery.isLoading || assignedResponse.isLoading ? <Loading /> : clientsQuery.isError ? <ErrorState error={clientsQuery.error} /> : assignedResponse.isError ? <ErrorState error={assignedResponse.error} /> : <><form className="form-grid coach-assign-form" onSubmit={event => void assign(event)}><label>Client to assign<select value={clientId} onChange={event => setClientId(event.target.value)} required disabled={!available.length}><option value="">{available.length ? 'Choose a client' : 'All clients are already assigned to this coach'}</option>{available.map((client, index) => <option key={String(client.id ?? index)} value={String(client.id ?? '')}>{String(client.name ?? 'Client')} آ· {String(client.email ?? '')}{client.coachId ? ' آ· currently assigned' : ' آ· unassigned'}</option>)}</select></label><button disabled={!clientId || busy}>{busy ? 'Assigning...' : 'Assign client'}</button></form>{!assigned.length ? <div className="empty-state">No clients assigned yet. Choose a client above to assign one.</div> : <div className="coach-client-grid">{assigned.map((client, index) => <Link className="coach-client-card" key={String(client.id ?? index)} to="/app/clients"><span className="coach-team-avatar small">{String(client.name ?? 'C').slice(0, 1).toUpperCase()}</span><span><strong>{String(client.name ?? 'Client')}</strong><small>{String(client.email ?? '')}</small></span><span className="subscription-status status-active">{t(String(client.status ?? 'Active'))}</span></Link>)}</div>}</>}</section>{message && <p className="form-feedback" role="status">{message}</p>}</section>}
  </Page>
}

function AdminExerciseWorkspace() {
  const cache = useQueryClient()
  const exercises = useQuery({ queryKey: ['admin', 'public-exercises'], queryFn: () => apiFetch<unknown>('/superadmin/exercises?page=1&pageSize=100') })

  const [id, setId] = useState('')
  const [name, setName] = useState('')
  const [muscleGroup, setMuscleGroup] = useState('')
  const [equipment, setEquipment] = useState('')
  const [videoUrl, setVideoUrl] = useState('')
  const [message, setMessage] = useState('')
  useEffect(() => {
    const selected = objectItems(exercises.data).find(row => String(row.id ?? row.Id ?? '') === id)
    if (!selected) { if (!id) { setName(''); setMuscleGroup(''); setEquipment(''); setVideoUrl('') }; return }
    setName(String(selected.name ?? selected.Name ?? ''))
    setMuscleGroup(String(selected.muscleGroup ?? selected.MuscleGroup ?? ''))
    setEquipment(String(selected.equipment ?? selected.Equipment ?? ''))
    setVideoUrl(String(selected.videoUrl ?? selected.VideoUrl ?? ''))
  }, [id, exercises.data])
  const body = (): Schema<'AdminExerciseRequest'> => ({ name, muscleGroup, equipment: equipment || null, videoUrl: videoUrl || null, videoFileId: null })
  const save = async (method: 'POST' | 'PUT') => { try { if (method === 'POST') await apiFetch<void>('/superadmin/exercises', { method: 'POST', body: JSON.stringify(body()) }); else await apiFetch<void>(`/superadmin/exercises/${id}`, { method: 'PUT', body: JSON.stringify(body()) }); setMessage(method === 'POST' ? "\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u062A\u0645\u0631\u064A\u0646 \u0639\u0627\u0645." : "\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0627\u0644\u0639\u0627\u0645."); await cache.invalidateQueries({ queryKey: ['admin', 'public-exercises'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.") } }
  const remove = async () => { if (!id || !window.confirm("\u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0627\u0644\u0639\u0627\u0645\u061F")) return; try { await apiFetch<void>(`/superadmin/exercises/${id}`, { method: 'DELETE' }); setMessage("\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646."); setId(''); await cache.invalidateQueries({ queryKey: ['admin', 'public-exercises'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.") } }
  const importCsv = async (file?: File) => { if (!file) return; const form = new FormData(); form.append('File', file); try { const result = await uploadFile<unknown>('/superadmin/exercises/import', form); setMessage(`ط£ع©ط¢آ§ط£â„¢أ¢â‚¬ ط£ع©ط¢ع¾ط£â„¢أ¢â‚¬طŒط£â„¢أ¢â‚¬آ° ط£ع©ط¢آ§ط£ع©ط¢آ³ط£ع©ط¢ع¾ط£â„¢ط¥ ط£ع©ط¢آ±ط£ع©ط¢آ§ط£ع©ط¢آ¯ CSV: ${JSON.stringify(result)}`); await cache.invalidateQueries({ queryKey: ['admin', 'public-exercises'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u0627\u0633\u062A\u064A\u0631\u0627\u062F CSV.") } }
  return <section className="panel admin-exercise-workspace"><h2>طھظ…ط§ط±ï؟½? ï؟½?  ط§ظ„ظ…ï؟½? طµط© ط§ظ„ط¹ط§ظ…ط©</h2><p>ط§ط®طھط± طھظ…ط±ï؟½? ï؟½? ط§ظ‹ ظ„طھط¹ط¯ï؟½? ظ„ظ‡ ط£ظˆ ط­ط°ظپظ‡. ط§ظ„ط­ط°ظپ ظ‚ط¯ ï؟½? ظڈط±ظپط¶ ط¥ط°ط§ ظƒط§ï؟½?  ط§ظ„طھظ…ط±ï؟½? ï؟½?  ظ…ط³طھط®ط¯ظ…ط§ظ‹.</p>{exercises.isLoading ? <Loading /> : exercises.isError ? <div><ErrorState error={exercises.error} /><button type="button" className="secondary-button" onClick={() => void exercises.refetch()}>Retry loading exercises</button></div> : objectItems(exercises.data).length === 0 ? <p className="empty-state">{'\u0644\u0627 \u062a\u0648\u062c\u062f \u062a\u0645\u0627\u0631\u064a\u0646 \u0639\u0627\u0645\u0629 \u0628\u0639\u062f. \u064a\u0645\u0643\u0646\u0643 \u0625\u0646\u0634\u0627\u0621 \u0623\u0648\u0644 \u062a\u0645\u0631\u064a\u0646 \u0645\u0646 \u0627\u0644\u0646\u0645\u0648\u0630\u062c.'}</p> : <EntitySelect label={'\u0627\u0644\u062A\u0645\u0631\u064A\u0646'} value={id} onChange={setId} data={exercises.data} />}<div className="admin-exercise-fields"><label>ط§ظ„ط§ط³ظ…<input value={name} onChange={event => setName(event.target.value)} required /></label><label>ط§ظ„ظ…ط¬ظ…ظˆط¹ط© ط§ظ„ط¹ط¶ظ„ï؟½? ط©<input value={muscleGroup} onChange={event => setMuscleGroup(event.target.value)} required /></label><label>ط§ظ„ظ…ط¹ط¯ط§طھ<input value={equipment} onChange={event => setEquipment(event.target.value)} /></label><label>ط±ط§ط¨ط· ط§ظ„ظپï؟½? ط¯ï؟½? ظˆ<input type="url" value={videoUrl} onChange={event => setVideoUrl(event.target.value)} /></label></div><div className="actions"><button onClick={() => void save('POST')}>ط¥ï؟½? ط´ط§ط،</button><button onClick={() => void save('PUT')} disabled={!id}>ط£ع©ط¢ع¾ط£ع©ط¢آ­ط£ع©ط¢آ¯ط£â„¢ط¥ ط£ع©ط¢آ«</button><button className="danger" onClick={() => void remove()} disabled={!id}>ط£ع©ط¢آ­ط£ع©ط¢آ°ط£â„¢ط¢ظ¾</button></div><label className="admin-csv-upload">ط£ع©ط¢آ§ط£ع©ط¢آ³ط£ع©ط¢ع¾ط£â„¢ط¥ ط£ع©ط¢آ±ط£ع©ط¢آ§ط£ع©ط¢آ¯ CSV<input type="file" accept=".csv,text/csv" onChange={event => void importCsv(event.target.files?.[0])} /></label>{message && <p role="status">{message}</p>}</section>
}
function ClientDetails({ id, canAssignCoach }: { id: string; canAssignCoach: boolean }) {
  const cache = useQueryClient()
  const coaches = useQuery({ queryKey: ['tenant', 'coaches'], queryFn: () => apiFetch<unknown>('/tenant/coaches?page=1&pageSize=100'), enabled: canAssignCoach })
  const client = useQuery({ queryKey: ['client', id], queryFn: () => apiFetch<unknown>(`/clients/${id}`) })
  const overview = useQuery({ queryKey: ['client', id, 'overview'], queryFn: () => apiFetch<unknown>(`/clients/${id}/overview`) })
  const checkIns = useQuery({ queryKey: ['client', id, 'checkins'], queryFn: () => apiFetch<unknown>(`/clients/${id}/checkins?page=1&pageSize=20`) })
  const workouts = useQuery({ queryKey: ['client', id, 'workouts'], queryFn: () => apiFetch<unknown>(`/clients/${id}/workout-logs?page=1&pageSize=20`) })
  const adherence = useQuery({ queryKey: ['client', id, 'adherence'], queryFn: () => apiFetch<unknown>(`/clients/${id}/adherence?period=week&date=${new Date().toISOString().slice(0, 10)}`) })
  const [health, setHealth] = useState<unknown>()
  const [notes, setNotes] = useState<unknown>()
  const [note, setNote] = useState('')
  const [checkInId, setCheckInId] = useState('')
  const selectedCheckIn = useQuery({ queryKey: ['client', id, 'checkin', checkInId], queryFn: () => apiFetch<unknown>(`/clients/${id}/checkins/${checkInId}`), enabled: Boolean(checkInId) })
  const [feedback, setFeedback] = useState('')
  const [coachId, setCoachId] = useState('')
  const [frequency, setFrequency] = useState('Weekly')
  const [dayOfWeek, setDayOfWeek] = useState<Schema<'DayOfWeek'>>('Sunday')
  const [message, setMessage] = useState('')
  const loadHealth = async () => { try { setHealth(await apiFetch<unknown>(`/clients/${id}/health-info`)) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not load health information.') } }
  const loadNotes = async () => { try { setNotes(await apiFetch<unknown>(`/clients/${id}/notes`)) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not load notes.') } }
  const addNote = async (event: React.FormEvent) => { event.preventDefault(); try { await apiFetch<unknown>(`/clients/${id}/notes`, { method: 'POST', body: JSON.stringify({ content: note } satisfies Schema<'CreateClientNoteRequest'>) }); setNote(''); await loadNotes() } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save the note.') } }
  const setStatus = async (status: string) => { try { await apiFetch<void>(`/clients/${id}/status`, { method: 'PUT', body: JSON.stringify({ status } satisfies Schema<'UpdateClientStatusRequest'>) }); await cache.invalidateQueries({ queryKey: queryKeys.clients }); setMessage('Client status updated.') } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not update status.') } }
  const sendFeedback = async (event: React.FormEvent) => { event.preventDefault(); try { await apiFetch<void>(`/clients/${id}/checkins/${checkInId}/feedback`, { method: 'POST', body: JSON.stringify({ feedback } satisfies Schema<'CheckInFeedbackRequest'>) }); setFeedback(''); setMessage('Feedback saved.'); await cache.invalidateQueries({ queryKey: ['client', id, 'checkins'] }) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save feedback.') } }
  const assignCoach = async (event: React.FormEvent) => { event.preventDefault(); const selectedCoachId = coachId || assignedCoachId; if (!selectedCoachId) return; try { await apiFetch<void>(`/clients/${id}/assign-coach`, { method: 'PUT', body: JSON.stringify({ coachId: selectedCoachId } satisfies Schema<'AssignCoachRequest'>) }); setMessage('Client assigned to the selected coach.'); await cache.invalidateQueries({ queryKey: ['client', id] }); await cache.invalidateQueries({ queryKey: queryKeys.clients }); await cache.invalidateQueries({ queryKey: ['tenant', 'coaches'] }) } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not assign the coach.') } }
  const saveSchedule = async (event: React.FormEvent) => { event.preventDefault(); try { await apiFetch<void>(`/clients/${id}/checkin-schedule`, { method: 'PUT', body: JSON.stringify({ frequency, dayOfWeek } satisfies Schema<'UpdateCheckInScheduleRequest'>) }); setMessage('Check-in schedule saved.') } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save the schedule.') } }
  if (client.isLoading) return <Loading />
  if (client.isError) return <ErrorState error={client.error} />
  const coachRows = objectItems(coaches.data).filter(row => row.role === 'CoachStaff' && row.isActive !== false)
  const clientRecord = client.data && typeof client.data === 'object' ? client.data as Record<string, unknown> : {}
  const assignedCoachId = typeof clientRecord.coachId === 'string' ? clientRecord.coachId : ''
  return <section className="panel"><h2>Client details</h2><div className="client-detail-summary"><p><strong>{stringField(client.data, 'name') ?? 'Client'}</strong></p><p>{stringField(client.data, 'email') ?? ''}</p><p>Status: {stringField(client.data, 'status') ?? 'Active'}</p></div><h3>Overview</h3>{overview.isLoading ? <Loading /> : overview.isError ? <ErrorState error={overview.error} /> : <JsonPanel value={overview.data} />}<div className="actions"><button onClick={() => void setStatus('Active')}>Set active</button><button onClick={() => void setStatus('Paused')}>Pause</button><button onClick={() => void setStatus('Archived')}>Archive</button><button onClick={() => void loadHealth()}>View health information</button></div>{canAssignCoach && <section className="owner-assign-coach"><h3>Assign this client to a coach</h3><p>Only active staff coaches in this club are listed.</p>{coaches.isLoading ? <Loading /> : coaches.isError ? <ErrorState error={coaches.error} /> : coachRows.length === 0 ? <div className="empty-state">No active staff coaches are available. Add or activate a coach first.</div> : <form className="form-grid" onSubmit={event => void assignCoach(event)}><label>Staff coach<select value={coachId || assignedCoachId} onChange={event => setCoachId(event.target.value)} required><option value="">Choose a staff coach</option>{coachRows.map(coach => <option key={String(coach.id)} value={String(coach.id)}>{String(coach.name ?? coach.email ?? 'Staff coach')}</option>)}</select></label><button disabled={!(coachId || assignedCoachId)}>Assign coach</button></form>}</section>}<form onSubmit={event => void saveSchedule(event)}><label>Check-in frequency<select value={frequency} onChange={event => setFrequency(event.target.value)}><option>Weekly</option><option>BiWeekly</option><option>Monthly</option></select></label><label>Day of week<select value={dayOfWeek} onChange={event => setDayOfWeek(event.target.value as Schema<'DayOfWeek'>)}>{['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'].map(day => <option key={day}>{day}</option>)}</select></label><button>Save check-in schedule</button></form>{health !== undefined && <JsonPanel value={health} />}<h3>Check-ins and feedback</h3>{checkIns.isLoading ? <Loading /> : checkIns.isError ? <ErrorState error={checkIns.error} /> : <JsonPanel value={checkIns.data} />}<EntitySelect label={'Choose a check-in'} value={checkInId} onChange={setCheckInId} data={checkIns.data} />{checkInId && (selectedCheckIn.isLoading ? <Loading /> : selectedCheckIn.isError ? <ErrorState error={selectedCheckIn.error} /> : <JsonPanel value={selectedCheckIn.data} />)}<form onSubmit={event => void sendFeedback(event)}><label>Coach feedback<textarea value={feedback} onChange={event => setFeedback(event.target.value)} maxLength={4000} required /></label><button disabled={!checkInId}>Save feedback</button></form><h3>Adherence</h3>{adherence.isLoading ? <Loading /> : adherence.isError ? <ErrorState error={adherence.error} /> : <JsonPanel value={adherence.data} />}{workouts.isLoading ? <Loading /> : workouts.isError ? <ErrorState error={workouts.error} /> : <JsonPanel value={workouts.data} />}<h3>Notes</h3><button onClick={() => void loadNotes()}>Load notes</button>{notes !== undefined && <JsonPanel value={notes} />}<form onSubmit={event => void addNote(event)}><label>New note<input value={note} onChange={event => setNote(event.target.value)} maxLength={4000} required /></label><button>Add note</button></form>{message && <p role="status">{message}</p>}</section>
}
function Conversations() {
  const { t, language } = useLanguage()
  const query = useConversations()
  const [selectedClient, setSelectedClient] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [mobileChatOpen, setMobileChatOpen] = useState(false)
  if (query.isLoading) return <Page title={t('Messages')}><Loading /></Page>
  if (query.isError) return <Page title={t('Messages')}><ErrorState error={query.error} /></Page>
  const conversations = query.data?.items ?? []
  const active = conversations.find(item => item.clientId === selectedClient) ?? conversations[0]
  const visible = conversations.filter(item => (item.name ?? '').toLocaleLowerCase().includes(search.toLocaleLowerCase()) && (!unreadOnly || (item.unreadCount ?? 0) > 0))
  return <Page title={t('Messages')} subtitle={t('Follow your client conversations and reply from one place.')}>
    <section dir={language === 'ar' ? 'rtl' : 'ltr'} className={'conversation-workspace' + (mobileChatOpen ? ' mobile-chat-open' : '')}>
      <aside className={'conversation-sidebar' + (mobileChatOpen ? ' mobile-hidden' : '')}>
        <header className="conversation-sidebar-header"><div><strong>{t('Inbox')}</strong><small>{conversations.length} {t(conversations.length === 1 ? 'conversation' : 'conversations')}</small></div><span className="conversation-total-unread" title={t('Unread messages')}><b>{conversations.reduce((sum, item) => sum + (item.unreadCount ?? 0), 0)}</b><small>{t('Unread')}</small></span></header>
        <label className="conversation-search"><span>{t('Search clients')}</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder={t('Type a client name')} /></label>
        <div className="conversation-filter-row"><span>{t('Your conversations')}</span><button className={'conversation-filter ' + (unreadOnly ? 'active' : '')} onClick={() => setUnreadOnly(value => !value)} aria-pressed={unreadOnly}>{unreadOnly ? t('Show all conversations') : t('Unread only')}</button></div>
        <div className="conversation-list" role="list">
          {visible.map(item => <button key={item.clientId} className={'conversation-list-item ' + (active?.clientId === item.clientId ? 'active' : '')} onClick={() => { setSelectedClient(item.clientId ?? null); setMobileChatOpen(true) }} role="listitem">
            <span className="conversation-avatar">{(item.name ?? '?').trim().slice(0, 1)}</span><span className="conversation-item-copy"><strong>{item.name || t('Client')}</strong><small>{item.lastMessage?.content || t('No messages yet. Start the conversation.')}</small></span>
            <span className="conversation-item-meta">{item.lastMessage?.timestamp ? new Intl.DateTimeFormat(language === 'ar' ? 'ar-EG' : 'en', { hour: '2-digit', minute: '2-digit' }).format(new Date(item.lastMessage.timestamp)) : ''}{(item.unreadCount ?? 0) > 0 && <b>{item.unreadCount}</b>}</span>
          </button>)}
          {visible.length === 0 && <div className="conversation-list-empty">{conversations.length ? t('No conversations match your search.') : t('No conversations yet. Client messages will appear here.')}</div>}
        </div>
      </aside>
      <div className={'conversation-main' + (mobileChatOpen ? ' mobile-active' : '')}>{active?.clientId ? <ChatPanel key={active.clientId} clientId={active.clientId} clientName={active.name ?? t('Client')} unreadCount={active.unreadCount ?? 0} onBack={() => setMobileChatOpen(false)} /> : <div className="conversation-welcome"><span aria-hidden="true">?</span><strong>{t('Select a conversation')}</strong><small>{t('Messages and client updates will appear here.')}</small></div>}</div>
    </section>
  </Page>
}
function ChatPanel({ clientId, clientName, unreadCount, onBack }: { clientId: string; clientName: string; unreadCount: number; onBack: () => void }) {
  const { language, t } = useLanguage()
  const messages = useChatConversation(clientId)
  const me = useMe()
  const send = useSendClientMessage(clientId)
  const [content, setContent] = useState('')
  const [attachments, setAttachments] = useState<File[]>([])
  const [uploading, setUploading] = useState(false)
  const [deleteMessageId, setDeleteMessageId] = useState('')
  const cache = useQueryClient()
  const markedMessageRef = useRef('')
  const incoming = objectItems(messages.data).filter(item => item.receiverId === me.data?.id && item.isRead !== true)
  const lastMessageId = incoming.at(-1)?.id
  const markRead = useCallback(async () => {
    if (typeof lastMessageId !== 'string') return
    try {
      await apiFetch<void>('/messages/' + clientId + '/read', { method: 'PUT', body: JSON.stringify({ lastMessageId } satisfies Schema<'ReadMessageRequest'>) })
      markedMessageRef.current = lastMessageId
      await cache.invalidateQueries({ queryKey: ['messages', clientId] })
      await cache.invalidateQueries({ queryKey: queryKeys.conversations })
      await cache.invalidateQueries({ queryKey: ['messages', 'unread-count'] })
    } catch (error) { window.alert(error instanceof Error ? error.message : t('Could not update read status.')) }
  }, [cache, clientId, lastMessageId, t])
  useEffect(() => { if (typeof lastMessageId === 'string' && markedMessageRef.current !== lastMessageId) void markRead() }, [lastMessageId, markRead])
  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const form = event.currentTarget
    const files = attachments
    if (!content.trim() && files.length === 0) return
    if (files.length > 10) { window.alert(t('You can attach up to 10 files per message.')); return }
    const oversized = files.find(file => file.size > (file.type.startsWith('video/') ? 100 : 10) * 1024 * 1024)
    if (oversized) { window.alert(t('File is too large. Videos can be up to 100 MB; other files up to 10 MB.')); return }
    setUploading(true)
    try {
      const fileIds: string[] = []
      for (const file of files) {
        const data = new FormData(); data.append('File', file); data.append('Purpose', 'attachment')
        const uploaded = await uploadFile<unknown>('/files/upload', data)
        const id = responseId(uploaded)
        if (!id) throw new Error(t('The uploaded file could not be identified.'))
        fileIds.push(id)
      }
      await send.mutateAsync({ content: content.trim() || null, fileIds })
      setContent(''); setAttachments([]); form.reset()
      await cache.invalidateQueries({ queryKey: ['messages', clientId] })
    } catch (error) { window.alert(error instanceof Error ? error.message : t('Could not send the message.')) }
    finally { setUploading(false) }
  }
  const deleteMessage = async () => {
    if (!deleteMessageId || !window.confirm(t('Delete your message?'))) return
    try {
      await apiFetch<void>('/messages/' + deleteMessageId, { method: 'DELETE' })
      setDeleteMessageId('')
      await cache.invalidateQueries({ queryKey: ['messages', clientId] })
      await cache.invalidateQueries({ queryKey: queryKeys.conversations })
    } catch (error) { window.alert(error instanceof Error ? error.message : t('Could not delete the message.')) }
  }
  if (messages.isLoading) return <div className="conversation-inline-state"><Loading /></div>
  if (messages.isError) return <div className="conversation-inline-state"><ErrorState error={messages.error} /></div>
  return <section dir={language === 'ar' ? 'rtl' : 'ltr'} className="panel chat-panel conversation-chat">
    <header className="chat-partner">
      <button type="button" className="conversation-back" onClick={onBack} aria-label={t('Back to conversations')}><span aria-hidden="true">&#8592;</span><span>{t('Inbox')}</span></button>
      <div className="chat-avatar">{clientName.trim().slice(0, 1)}</div>
      <div className="conversation-contact-copy"><strong>{clientName}</strong><small>{t('Client conversation')}</small></div>
      <span className={'chat-presence ' + (messages.partnerOnline ? 'online' : '')}><i />{messages.partnerOnline === null ? t('Presence unknown') : messages.partnerOnline ? t('Online now') : t('Offline')}{messages.isTyping ? ' ? ' + t('Typing&') : ''}</span>
    </header>
    <div className="conversation-chat-toolbar"><span className="conversation-unread-label"><b>{unreadCount}</b> {t('Unread')}</span><button type="button" className="chat-read-button" onClick={() => void markRead()} disabled={typeof lastMessageId !== 'string'}>{t('Mark as read')}</button></div>
    <ChatTranscript value={messages.data} ownUserId={me.data?.id} />
    {attachments.length > 0 && <div className="chat-selected-files" aria-live="polite">{attachments.map((file,index) => <span className="chat-selected-file" key={file.name + index}><span aria-hidden="true">{String.fromCharCode(file.type.startsWith('image/') ? 9639 : file.type.startsWith('video/') ? 9654 : 8613)}</span><span title={file.name}>{file.name}</span><button type="button" onClick={() => setAttachments(current => current.filter((_,itemIndex) => itemIndex !== index))} aria-label={t('Remove attachment')}>&times;</button></span>)}</div>}
    <form className="chat-compose conversation-compose" onSubmit={event => void submit(event)}>
      <label>{t('Message')}<textarea value={content} onChange={event => { setContent(event.target.value); messages.sendTyping() }} maxLength={2000} placeholder={t('Write to') + ' ' + clientName} rows={2} /></label>
      <label className="chat-file-label"><span aria-hidden="true">&#43;</span> {t('Attach photos, videos or files')}<input name="attachments" type="file" multiple accept="image/jpeg,image/png,image/webp,video/mp4,video/webm,.pdf,.doc,.docx" onChange={event => setAttachments(Array.from(event.target.files ?? []).slice(0, 10))} /></label>
      <button disabled={send.isPending || uploading || (!content.trim() && attachments.length === 0)}>{uploading ? t('Uploading files&') : send.isPending ? t('Sending&') : t('Send message')}</button>
    </form>
    {send.isError && <ErrorState error={send.error} />}
    <details className="conversation-message-actions"><summary>{t('Manage my messages')}</summary><div><EntitySelect label={t('Select a message to delete')} value={deleteMessageId} onChange={setDeleteMessageId} data={{ items: objectItems(messages.data).filter(item => item.senderId === me.data?.id) }} /><button type="button" className="danger" onClick={() => void deleteMessage()} disabled={!deleteMessageId}>{t('Delete my message')}</button></div></details>
  </section>
}
function Payments() {
  const me = useMe()
  const { t } = useLanguage()
  const query = usePayments()
  const cache = useQueryClient()
  const clients = useClients(1,100)
  const subscriptions = useQuery({ queryKey:['tenant','subscriptions'], queryFn:()=>apiFetch<unknown>('/subscriptions?page=1&pageSize=100'), enabled:me.data?.role==='TenantOwner' })
 const pending = useQuery({ queryKey:['tenant','payments','pending'], queryFn:()=>apiFetch<PaymentsPage>('/tenant/payments/pending?page=1&pageSize=20') })
  const [refundId,setRefundId]=useState(''); const [refundAmount,setRefundAmount]=useState(''); const [refundReason,setRefundReason]=useState(''); const [manualSubscription,setManualSubscription]=useState(''); const [manualAmount,setManualAmount]=useState(''); const [message,setMessage]=useState('')
  const pendingSubscriptions=objectItems(subscriptions.data).filter(row=>String(row.status??'').toLowerCase()==='pending')
  useEffect(()=>{const row=objectItems(subscriptions.data).find(item=>String(item.status??'').toLowerCase()==='pending'&&String(item.id??'')===manualSubscription);if(row)setManualAmount(String(row.price??row.snapshotPrice??''))},[manualSubscription,subscriptions.data])
  const review=async(id:string,confirm:boolean)=>{try{if(confirm)await apiFetch<void>(`/payments/instapay/${id}/confirm`,{method:'PUT'});else{const reason:Schema<'RejectPaymentRequest'>={reason:window.prompt(t('Rejection reason'))??''};if(!(reason.reason??'').trim())return;await apiFetch<void>(`/payments/instapay/${id}/reject`,{method:'PUT',body:JSON.stringify(reason)})}await cache.invalidateQueries({queryKey:queryKeys.payments});await cache.invalidateQueries({queryKey:['tenant','payments','pending']});await cache.invalidateQueries({queryKey:['tenant','subscriptions']});setMessage(t(confirm?'Payment confirmed and subscription activated.':'Payment rejected.'))}catch(error){setMessage(error instanceof Error?error.message:t('Could not review the payment.'))}}
  const refund=async(event:React.FormEvent)=>{event.preventDefault();const body:Schema<'RefundRequest'>={amount:Number(refundAmount),reason:refundReason};try{await apiPost(`/payments/${refundId}/refund`,body,true);setMessage(t('Refund recorded.'));await cache.invalidateQueries({queryKey:queryKeys.payments})}catch(error){setMessage(error instanceof Error?error.message:t('Could not process the refund.'))}}
  const manual=async(event:React.FormEvent)=>{event.preventDefault();if(!manualSubscription)return;const body:Schema<'ManualPaymentRequest'>={subscriptionId:manualSubscription,amount:Number(manualAmount)};try{await apiPost('/tenant/payments/manual',body,true);setMessage(t('Cash payment recorded and subscription activated.'));setManualSubscription('');setManualAmount('');await cache.invalidateQueries({queryKey:queryKeys.payments});await cache.invalidateQueries({queryKey:['tenant','payments','pending']});await cache.invalidateQueries({queryKey:['tenant','subscriptions']})}catch(error){setMessage(error instanceof Error?error.message:t('Could not record the cash payment.'))}}
  const exportCsv=async()=>{try{const file=await downloadFile('/tenant/payments/export');const url=URL.createObjectURL(file.blob);const anchor=document.createElement('a');anchor.href=url;anchor.download=file.filename;anchor.click();URL.revokeObjectURL(url)}catch(error){setMessage(error instanceof Error?error.message:t('Could not download the report.'))}}
  if(query.isLoading)return <Loading />
  if(query.isError)return <ErrorState error={query.error}/>
  return <Page title={t('Payments')} subtitle={t('Review client transfers, record cash collections and manage refunds.')}>
    <section className="panel"><div className="section-heading"><div><h2>{t('Transfers awaiting review')}</h2><p>{t('Confirm only after matching the transfer reference and receipt.')}</p></div></div>{pending.isLoading?<Loading/>:pending.isError?<ErrorState error={pending.error}/>:!pending.data?.items?.length?<div className="empty-state">{t('No transfers are waiting for review.')}</div>:<div className="table-wrap"><table><thead><tr><th>{t('Client')}</th><th>{t('Amount')}</th><th>{t('Method')}</th><th>{t('Status')}</th><th>{t('Review')}</th></tr></thead><tbody>{pending.data.items.map((payment,index)=><tr key={payment.id??index}><td>{String(payment.subscriptionId??t('Subscription payment'))}</td><td>{payment.amount??0} {payment.currency??''}</td><td>{t(String(payment.method??''))}</td><td>{t(String(payment.status??''))}</td><td>{payment.id&&<><button onClick={()=>void review(payment.id!,true)}>{t('Confirm and activate')}</button> <button className="danger" onClick={()=>void review(payment.id!,false)}>{t('Reject')}</button></>}</td></tr>)}</tbody></table></div>}</section>
    {me.data?.role==='TenantOwner'&&<section className="panel"><h2>{t('Record a cash payment')}</h2><p>{t('Only a pending client request can be recorded. The exact plan price is filled in automatically. Recording the payment confirms it and activates the subscription.')}</p>{subscriptions.isLoading?<Loading/>:subscriptions.isError?<ErrorState error={subscriptions.error}/>:pendingSubscriptions.length===0?<div className="empty-state">{t('No pending requests. Ask the client to choose a plan and submit a subscription request first.')}</div>:<form onSubmit={event=>void manual(event)}><EntitySelect label={t('Pending client request')} value={manualSubscription} onChange={setManualSubscription} data={{items:pendingSubscriptions.map(row=>({...row,id:row.id,name:`${String(objectItems(clients.data).find(client=>String(client.id??'')===String(row.clientId??''))?.name??t('Client'))} - ${String(row.planName??'Plan')} - ${String(row.price??row.snapshotPrice??'')} ${String(row.currency??'EGP')}`}))}}/><label>{t('Amount')}<input type="number" step="0.01" min="0.01" value={manualAmount} readOnly required/></label><button disabled={!manualSubscription}>{t('Record cash and activate')}</button></form>}</section>}
    {me.data?.role==='TenantOwner'&&<section className="panel"><h2>{t('Refund a payment')}</h2><form onSubmit={event=>void refund(event)}><EntitySelect label={t('Choose a payment')} value={refundId} onChange={setRefundId} data={query.data}/><label>{t('Amount')}<input type="number" step="0.01" min="0.01" value={refundAmount} onChange={event=>setRefundAmount(event.target.value)} required/></label><label>{t('Reason')}<input value={refundReason} onChange={event=>setRefundReason(event.target.value)} required/></label><button disabled={!refundId}>{t('Process refund')}</button></form><button className="secondary-button" onClick={()=>void exportCsv()}>{t('Export CSV')}</button></section>}
    <Table headers={[t('Client'),t('Amount'),t('Method'),t('Status')]} rows={(query.data?.items??[]).map(payment=>{const subscription=objectItems(subscriptions.data).find(row=>String(row.id??'')===String(payment.subscriptionId??''));const client=objectItems(clients.data).find(row=>String(row.id??'')===String(subscription?.clientId??''));return [String(client?.name??t('Client')),`${payment.amount??0} ${payment.currency??''}`,t(String(payment.method??'')),t(String(payment.status??''))]})}/>{message&&<p role="status" className="form-feedback">{message}</p>}
  </Page>
}function notificationText(item: Record<string, unknown>, field: 'title' | 'body', t: (text: string) => string) {
  const raw = typeof item[field] === 'string' ? item[field] as string : ''
  if (/[ط£ع© | ط£â€ڑ | ][ط£â€ڑ | ط£ع© | ط£â€ڑط¢آ¢ط£ع©ط¢آ¢ط£â€ڑط¢آ¸]|ط£ع©ط¢آ£ط£â€ أ¢â‚¬â„¢|ط£ع©ط¢آ£ط£آ¢أ¢â€ڑآ¬ط¥طŒ/.test(raw)) {
    if (item.type === 'MessageReceived' || item.type === 'NewMessage') return t(field === 'title' ? 'New message' : 'You have a new message.')
    return t('New update')
  }
  return raw || (field === 'title' ? t('Update') : '')
}
function NotificationCenter() {
  const { t } = useLanguage()
  const [page,setPage]=useState(1);const [unreadOnly,setUnreadOnly]=useState(false);const query=useNotifications(page,20,unreadOnly);const cache=useQueryClient();useNotificationsHub()
  const unread=useQuery({queryKey:['notifications','unread-count'],queryFn:()=>apiFetch<unknown>('/notifications/unread-count'),refetchInterval:30_000})
  const preferenceQuery=useQuery({queryKey:['notifications','preferences'],queryFn:()=>apiFetch<unknown>('/notifications/preferences')})
  const [preferences,setPreferences]=useState<{type:string;inAppEnabled:boolean;emailEnabled:boolean}[]>([]);const [message,setMessage]=useState('')
  const data=query.data&&typeof query.data==='object'?query.data as Record<string,unknown>:{}
  const total=typeof data.total==='number'?data.total:typeof data.totalCount==='number'?data.totalCount:0
  const items=objectItems(data.items)
  const unreadCount=unread.data&&typeof unread.data==='object'?Number((unread.data as Record<string,unknown>).count??(unread.data as Record<string,unknown>).unreadCount??0):0
  const criticalTypes=new Set(['PaymentStatusChanged','SecurityAlert','PaymentConfirmed','PaymentRejected'])
  useEffect(()=>{if(!preferenceQuery.data)return;const current=objectItems(preferenceQuery.data);const known=['CheckInSubmitted','CheckInFeedback','PaymentStatusChanged','PendingPaymentCreated','PaymentConfirmed','PaymentRejected','SubscriptionStatusChanged','SubscriptionExpiryReminder','ChurnRiskAlert','NewMessage','SecurityAlert'];setPreferences(known.map(type=>{const row=current.find(item=>item.type===type);return{type,inAppEnabled:typeof row?.inAppEnabled==='boolean'?row.inAppEnabled:true,emailEnabled:typeof row?.emailEnabled==='boolean'?row.emailEnabled:false}}))},[preferenceQuery.data])
  const invalidate=async()=>{await cache.invalidateQueries({queryKey:queryKeys.notifications});await cache.invalidateQueries({queryKey:['notifications','unread-count']})}
  const readAll=async()=>{try{await apiFetch<void>('/notifications/read-all',{method:'PUT'});await invalidate();setMessage('All notifications marked as read.')}catch(error){setMessage(error instanceof Error?error.message:'Could not update notifications.')}}
  const changeNotification=async(id:string,action:'read'|'delete')=>{try{if(action==='read')await apiFetch<void>('/notifications/'+id+'/read',{method:'PUT'});else await apiFetch<void>('/notifications/'+id,{method:'DELETE'});await invalidate();setMessage(action==='read'?'Notification marked as read.':'Notification deleted.')}catch(error){setMessage(error instanceof Error?error.message:'Could not update notification.')}}
  const savePreferences=async()=>{try{await apiFetch<void>('/notifications/preferences',{method:'PUT',body:JSON.stringify({preferences})});await preferenceQuery.refetch();setMessage('Notification preferences saved.')}catch(error){setMessage(error instanceof Error?error.message:'Could not save preferences.')}}
  if(query.isLoading)return <Loading />;if(query.isError)return <ErrorState error={query.error} />
  return <Page title="Notifications" subtitle="Your updates, reminders and account activity.">
    <section className="notification-summary"><div><span className="notification-kicker">INBOX</span><h2>Your notifications</h2><p>Important updates from your coaching workspace.</p></div><div className="notification-summary-count"><strong>{Number.isFinite(unreadCount)?unreadCount:0}</strong><span>unread</span></div></section>
    <section className="panel notification-panel"><div className="notification-toolbar"><div className="notification-filter"><button className={!unreadOnly?'active':''} onClick={()=>{setPage(1);setUnreadOnly(false)}}>All notifications</button><button className={unreadOnly?'active':''} onClick={()=>{setPage(1);setUnreadOnly(true)}}>Unread</button></div><button className="secondary-button" onClick={()=>void readAll()} disabled={unreadCount===0}>Mark all as read</button></div>
      {message&&<p className="notification-feedback" role="status">{message}</p>}
      {items.length===0?<div className="notification-empty"><span aria-hidden="true">?</span><h3>{unreadOnly?t('You are all caught up'):t('No notifications yet')}</h3><p>{unreadOnly?t('There are no unread updates.'):t('Updates and reminders will appear here when they arrive.')}</p></div>:<div className="notification-list">{items.map((item,index)=>{const id=typeof item.id==='string'?item.id:'';const isRead=item.isRead===true;const date=typeof item.createdAt==='string'?new Date(item.createdAt):null;return <article className={`notification-card${isRead?' is-read':''}`} key={id||index}><span className="notification-card-icon"><Activity size={18}/></span><div className="notification-card-content"><div className="notification-card-heading"><h3>{notificationText(item,'title',t)}</h3><span className={isRead?'notification-state':'notification-state unread'}>{t(isRead?'Read':'New')}</span></div><p>{notificationText(item,'body',t)}</p><small>{date&&!Number.isNaN(date.getTime())?new Intl.DateTimeFormat(undefined,{dateStyle:'medium',timeStyle:'short'}).format(date):''}</small></div><div className="notification-card-actions">{id&&!isRead&&<button className="notification-icon-button" aria-label={t('Mark as read')} title={t('Mark as read')} onClick={()=>void changeNotification(id,'read')}><Check size={17}/></button>}{id&&<button className="notification-icon-button danger" aria-label={t('Delete notification')} title={t('Delete notification')} onClick={()=>void changeNotification(id,'delete')}><X size={17}/></button>}</div></article>})}</div>}
      <div className="notification-pagination"><button className="secondary-button" onClick={()=>setPage(value=>Math.max(1,value-1))} disabled={page<=1}>Previous</button><span>Page {page} of {Math.max(1,Math.ceil(total/20))} <small>({total} total)</small></span><button className="secondary-button" onClick={()=>setPage(value=>value+1)} disabled={page*20>=total}>Next</button></div>
    </section>
    <section className="panel notification-preferences"><div className="section-heading"><div><span className="eyebrow">Delivery settings</span><h2>Notification preferences</h2><p>Choose where each type of update is delivered.</p></div></div>{preferenceQuery.isLoading?<Loading/>:preferenceQuery.isError?<ErrorState error={preferenceQuery.error}/>:<><div className="table-wrap"><table><thead><tr><th>Notification type</th><th>In app</th><th>Email</th></tr></thead><tbody>{preferences.map((pref,index)=><tr key={pref.type}><td>{displayLabel(pref.type, 'en')}</td><td><input aria-label={pref.type+' in app'} type="checkbox" checked={criticalTypes.has(pref.type)||pref.inAppEnabled} disabled={criticalTypes.has(pref.type)} onChange={event=>setPreferences(rows=>rows.map((row,i)=>i===index?{...row,inAppEnabled:event.target.checked}:row))}/></td><td><input aria-label={pref.type+' email'} type="checkbox" checked={criticalTypes.has(pref.type)||pref.emailEnabled} disabled={criticalTypes.has(pref.type)} onChange={event=>setPreferences(rows=>rows.map((row,i)=>i===index?{...row,emailEnabled:event.target.checked}:row))}/></td></tr>)}</tbody></table></div><p className="notification-preference-note">Security and payment critical alerts are always enabled.</p><button onClick={()=>void savePreferences()}>Save preferences</button></>}</section>
  </Page>
}
function AccountPage() {
  const me = useMe()
  const updateProfile = useUpdateProfile()
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [language, setLanguage] = useState('ar')
  const [avatarFileId, setAvatarFileId] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [message, setMessage] = useState('')
  const update = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'UpdateAccountRequest'> = { name: name || me.data?.name || undefined, phone: phone || stringField(me.data, 'phone'), language: language || stringField(me.data, 'language'), avatarFileId: avatarFileId || stringField(me.data, 'avatarFileId') }; try { await updateProfile.mutateAsync(body); setMessage("\u062A\u0645 \u062D\u0641\u0638 \u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u062D\u0633\u0627\u0628.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u062D\u0633\u0627\u0628.") } }
  const uploadAvatar = async (file?: File) => { if (!file) return; const form = new FormData(); form.append('File', file); form.append('Purpose', 'avatar'); try { const result = await uploadFile<unknown>('/files/upload', form); if (result && typeof result === 'object' && 'id' in result && typeof result.id === 'string') setAvatarFileId(result.id); else setMessage("\u062A\u0639\u0630\u0631 \u0642\u0631\u0627\u0621\u0629 \u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0635\u0648\u0631\u0629 \u0645\u0646 \u0627\u0633\u062A\u062C\u0627\u0628\u0629 \u0627\u0644\u062E\u0627\u062F\u0645.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u0631\u0641\u0639 \u0627\u0644\u0635\u0648\u0631\u0629.") } }
  const changePassword = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'ChangePasswordRequest'> = { currentPassword, newPassword }; try { await apiFetch<void>('/account/change-password', { method: 'PUT', body: JSON.stringify(body) }); setCurrentPassword(''); setNewPassword(''); setMessage("\u062A\u0645 \u062A\u063A\u064A\u064A\u0631 \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631. \u0633\u062C\u0651\u0644 \u0627\u0644\u062F\u062E\u0648\u0644 \u0645\u062C\u062F\u062F\u064B\u0627 \u0639\u0644\u0649 \u0627\u0644\u0623\u062C\u0647\u0632\u0629 \u0627\u0644\u0623\u062E\u0631\u0649.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u063A\u064A\u064A\u0631 \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631.") } }
  if (me.isLoading) return <Loading />
  if (me.isError) return <ErrorState error={me.error} />
  return <Page title={'\u0627\u0644\u062D\u0633\u0627\u0628'} subtitle={'\u0625\u062F\u0627\u0631\u0629 \u0628\u064A\u0627\u0646\u0627\u062A\u0643 \u0627\u0644\u0634\u062E\u0635\u064A\u0629 \u0648\u0648\u0633\u0627\u0626\u0644 \u0627\u0644\u0623\u0645\u0627\u0646'}><section className="panel"><p>{me.data?.name} ط£â€ڑ |  {me.data?.role}</p><form onSubmit={event => void update(event)}><label>ط§ظ„ط§ط³ظ…<input value={name} onChange={event => setName(event.target.value)} placeholder={me.data?.name ?? ''} /></label><label>ط§ظ„ظ‡ط§طھظپ<input value={phone} onChange={event => setPhone(event.target.value)} /></label><label>ط§ظ„ظ„ط؛ط©<input value={language} onChange={event => setLanguage(event.target.value)} /></label><label>طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨<input type="file" accept="image/jpeg,image/png,image/webp" onChange={event => void uploadAvatar(event.target.files?.[0])} /></label>{(avatarFileId || stringField(me.data, 'avatarFileId')) && <small>طھظ… ط§ط®طھï؟½? ط§ط± طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨.</small>}<button disabled={updateProfile.isPending}>ط­ظپط¸ ط§ظ„ط­ط³ط§ط¨</button></form></section><AccountFileActions fileId={avatarFileId || stringField(me.data, 'avatarFileId') || ''} onDeleted={() => setAvatarFileId('')} /><section className="panel"><h2>طھط؛ï؟½? ï؟½? ط± ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±</h2><form onSubmit={event => void changePassword(event)}><label>ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط­ط§ظ„ï؟½? ط©<input type="password" value={currentPassword} onChange={event => setCurrentPassword(event.target.value)} required /></label><label>ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط¬ط¯ï؟½? ط¯ط©<input type="password" value={newPassword} onChange={event => setNewPassword(event.target.value)} minLength={12} required /></label><button>طھط؛ï؟½? ï؟½? ط± ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±</button></form><button onClick={async () => { await apiFetch<void>(endpoints.logoutAll, { method: 'POST' }); clearSession(); location.assign('/login') }}>طھط³ط¬ï؟½? ظ„ ط§ظ„ط®ط±ظˆط¬ ظ…ï؟½?  ظƒظ„ ط§ظ„ط£ط¬ظ‡ط²ط©</button><button className="danger" onClick={async () => { if (!window.confirm("\u0633\u064A\u062A\u0645 \u0625\u062E\u0641\u0627\u0621 \u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u062D\u0633\u0627\u0628 \u0627\u0644\u0634\u062E\u0635\u064A\u0629. \u0647\u0644 \u062A\u0631\u064A\u062F \u0627\u0644\u0645\u062A\u0627\u0628\u0639\u0629\u061F")) return; try { await apiFetch<void>('/account/me', { method: 'DELETE' }); clearSession(); location.assign('/login') } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u062D\u0633\u0627\u0628.") } }}>ط­ط°ظپ ط§ظ„ط­ط³ط§ط¨ ظˆط¥ط®ظپط§ط، ط¨ï؟½? ط§ï؟½? ط§طھظ‡</button>{message && <p>{message}</p>}</section><DataExportPanel /></Page>
}

function AccountFileActions({ fileId, onDeleted }: { fileId: string; onDeleted: () => void }) {
  const [message, setMessage] = useState('')
  const download = async () => {
    try {
      const file = await downloadFile(`/files/${fileId}`)
      const url = URL.createObjectURL(file.blob); const anchor = document.createElement('a'); anchor.href = url; anchor.download = file.filename; anchor.click()
      window.setTimeout(() => URL.revokeObjectURL(url), 1000)
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0646\u0632\u064A\u0644 \u0627\u0644\u0635\u0648\u0631\u0629.") }
  }
  const remove = async () => {
    if (!window.confirm("\u062D\u0630\u0641 \u0635\u0648\u0631\u0629 \u0627\u0644\u062D\u0633\u0627\u0628 \u0645\u0646 Files\u061F")) return
    try { await apiFetch<void>(`/files/${fileId}`, { method: 'DELETE' }); onDeleted(); setMessage("\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u0645\u0644\u0641.") }
    catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u0645\u0644\u0641.") }
  }
  if (!fileId) return null
  return <section className="panel"><h2>ظ…ظ„ظپط§طھ ط§ظ„ط­ط³ط§ط¨</h2><button onClick={() => void download()}>طھï؟½? ط²ï؟½? ظ„ طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨</button><button className="danger" onClick={() => void remove()}>ط­ط°ظپ طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨</button>{message && <p role="status">{message}</p>}</section>
}

function DataExportPanel() {
  const [exportId, setExportId] = useState('')
  const [downloadToken, setDownloadToken] = useState('')
  const [message, setMessage] = useState('')
  const query = useQuery({
    queryKey: ['account', 'data-export', exportId],
    queryFn: () => apiFetch<unknown>(`/account/data-export/${exportId}`),
    enabled: Boolean(exportId),
    refetchInterval: current => stringField(current.state.data, 'status') === 'Ready' || stringField(current.state.data, 'status') === 'Failed' ? false : 2500,
  })
  const request = async () => {
    setMessage('')
    try {
      const result = await apiFetch<unknown>('/account/data-export', { method: 'POST' })
      setExportId(stringField(result, 'id') ?? '')
      setDownloadToken(stringField(result, 'downloadToken') ?? '')
      setMessage("\u062A\u0645 \u0627\u0633\u062A\u0644\u0627\u0645 \u0637\u0644\u0628 \u0627\u0644\u062A\u0635\u062F\u064A\u0631. \u0633\u062A\u062A\u062D\u062F\u062B \u0627\u0644\u062D\u0627\u0644\u0629 \u062A\u0644\u0642\u0627\u0626\u064A\u0627\u064B.")
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u0637\u0644\u0628 \u0646\u0633\u062E\u0629 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A.") }
  }
  const download = async () => {
    if (!exportId || !downloadToken) return
    try {
      const file = await downloadFile(`/account/data-export/${exportId}/download?token=${encodeURIComponent(downloadToken)}`)
      const url = URL.createObjectURL(file.blob)
      const anchor = document.createElement('a'); anchor.href = url; anchor.download = file.filename; anchor.click()
      window.setTimeout(() => URL.revokeObjectURL(url), 1000)
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0646\u0632\u064A\u0644 \u0646\u0633\u062E\u0629 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A.") }
  }
  const status = stringField(query.data, 'status')
  return <section className="panel"><h2>طھطµط¯ï؟½? ط± ط¨ï؟½? ط§ï؟½? ط§طھï؟½? </h2><p>ط§ظ„ظ…ظ„ظپ ï؟½? ط¶ظ… ط¨ï؟½? ط§ï؟½? ط§طھ ط­ط³ط§ط¨ظƒ ظپظ‚ط·طŒ ظˆط±ط§ط¨ط· ط§ظ„طھï؟½? ط²ï؟½? ظ„ طµط§ظ„ط­ ظ„ظ„ط§ط³طھط®ط¯ط§ظ… ظ…ط±ط© ظˆط§ط­ط¯ط© ظˆï؟½? ï؟½? طھظ‡ï؟½?  ط¨ط¹ط¯ ط³ط§ط¹ط©. ï؟½? ظ…ظƒï؟½?  ط·ظ„ط¨ طھطµط¯ï؟½? ط± ط¬ط¯ï؟½? ط¯ ظƒظ„ 24 ط³ط§ط¹ط©.</p><button onClick={() => void request()} disabled={query.isFetching}>ط·ظ„ط¨ طھطµط¯ï؟½? ط± ط§ظ„ط¨ï؟½? ط§ï؟½? ط§طھ</button>{exportId && <><p>ط§ظ„ط­ط§ظ„ط©: {query.isLoading ? "\u062C\u0627\u0631\u064D \u062A\u062C\u0647\u064A\u0632 \u0627\u0644\u0637\u0644\u0628" : status ?? "\u063A\u064A\u0631 \u0645\u0639\u0631\u0648\u0641\u0629"}</p>{query.isError && <ErrorState error={query.error} />}<button onClick={() => void download()} disabled={status !== 'Ready' || !downloadToken}>طھï؟½? ط²ï؟½? ظ„ ط§ظ„ظ…ظ„ظپ ط§ظ„ط¬ط§ظ‡ط²</button></>}{message && <p role="status">{message}</p>}</section>
}

function CheckInManagement() {
  const cache = useQueryClient()
  const list = useQuery({ queryKey: ['my', 'checkins'], queryFn: () => apiFetch<unknown>('/my/checkins') })
  const [id, setId] = useState('')
  const [weight, setWeight] = useState('')
  const [measurements, setMeasurements] = useState('')
  const [message, setMessage] = useState('')
  const detail = useQuery({ queryKey: ['my', 'checkin', id], queryFn: () => apiFetch<unknown>(`/my/checkins/${id}`), enabled: Boolean(id) })
  const save = async (event: React.FormEvent) => {
    event.preventDefault()
    try {
      const body: Schema<'CreateCheckInUpdateRequest'> = { weight: Number(weight), measurements: measurements || null }
      await apiFetch<void>(`/my/checkins/${id}`, { method: 'PUT', body: JSON.stringify(body) })
      setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062B Check-in."); await cache.invalidateQueries({ queryKey: ['my', 'checkins'] }); await cache.invalidateQueries({ queryKey: ['my', 'checkin', id] })
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0639\u062F\u064A\u0644 Check-in.") }
  }
  const remove = async () => {
    if (!id || !window.confirm("\u062D\u0630\u0641 Check-in\u061F \u0625\u0630\u0627 \u0639\u0644\u0651\u0642 \u0627\u0644\u0645\u062F\u0631\u0628 \u0639\u0644\u064A\u0647 \u0633\u064A\u0631\u0641\u0636 \u0627\u0644\u062E\u0627\u062F\u0645 \u0627\u0644\u062D\u0630\u0641.")) return
    try { await apiFetch<void>(`/my/checkins/${id}`, { method: 'DELETE' }); setMessage("\u062A\u0645 \u062D\u0630\u0641 Check-in."); setId(''); await cache.invalidateQueries({ queryKey: ['my', 'checkins'] }) }
    catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 Check-in.") }
  }
  return <section className="panel"><h2>ط³ط¬ظ„ط§طھ Check-in</h2>{list.isLoading ? <Loading /> : list.isError ? <ErrorState error={list.error} /> : objectItems(list.data).length === 0 ? <p className="empty-state">No check-ins recorded yet. Your progress updates will appear here.</p> : <div className="actions">{objectItems(list.data).map((item, index) => { const itemId = typeof item.id === 'string' ? item.id : ''; return <button key={itemId || index} onClick={() => { setId(itemId); setWeight(typeof item.weight === 'number' ? String(item.weight) : ''); setMeasurements(typeof item.measurements === 'string' ? item.measurements : '') }}>{String(item.date ?? item.createdAt ?? itemId)}</button> })}</div>}{id && <><h3>ط§ظ„طھظپط§طµï؟½? ظ„</h3>{detail.isLoading ? <Loading /> : detail.isError ? <ErrorState error={detail.error} /> : <JsonPanel value={detail.data} />}<form onSubmit={event => void save(event)}><label>ط§ظ„ظˆط²ï؟½? <input type="number" min="1" max="500" step="0.1" value={weight} onChange={event => setWeight(event.target.value)} required /></label><label>ط§ظ„ظ‚ï؟½? ط§ط³ط§طھ<textarea value={measurements} onChange={event => setMeasurements(event.target.value)} /></label><button disabled={detail.isFetching}>ط­ظپط¸ ط§ظ„طھط¹ط¯ï؟½? ظ„</button><button type="button" className="danger" onClick={() => void remove()}>ط­ط°ظپ ط§ظ„ط³ط¬ظ„</button></form></>}{message && <p role="status">{message}</p>}</section>
}

function MealLogAction() {
  const cache = useQueryClient()
  const plan = useMyMealPlan()
  const meals = { items: nestedRecords(plan.data).filter(item => typeof item.mealId === 'string' || typeof item.mealName === 'string' || typeof item.calories === 'number') }
  const [mealId, setMealId] = useState('')
  const [loggedOn, setLoggedOn] = useState(new Date().toISOString().slice(0, 10))
  const [message, setMessage] = useState('')
  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    try {
      await apiPost('/my/meal-logs', { mealId, loggedOn } satisfies Schema<'MyMealLogRequest'>)
      setMessage("\u062A\u0645 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0648\u062C\u0628\u0629."); setMealId(''); await cache.invalidateQueries({ queryKey: ['my', 'meal-plan'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0648\u062C\u0628\u0629.") }
  }
  return <section className="panel"><h2>طھط³ط¬ï؟½? ظ„ ظˆط¬ط¨ط© ظ…ï؟½?  ط®ط·طھï؟½? </h2><form onSubmit={event => void submit(event)}><EntitySelect label={'\u0627\u062E\u062A\u0631 \u0627\u0644\u0648\u062C\u0628\u0629'} value={mealId} onChange={setMealId} data={meals} /><label>ط§ظ„طھط§ط±ï؟½? ط®<input type="date" value={loggedOn} onChange={event => setLoggedOn(event.target.value)} required /></label><button disabled={!mealId}>طھط³ط¬ï؟½? ظ„ ط§ظ„ط§ظ„طھط²ط§ظ…</button></form>{message && <p role="status">{message}</p>}</section>
}

function ClientSubscriptionLifecycle() {
  const {t,language}=useLanguage();const cache=useQueryClient()
  const history=useQuery({queryKey:['my','subscriptions'],queryFn:()=>apiFetch<unknown>('/my/subscriptions')})
  const plans=useQuery({queryKey:['public','plans'],queryFn:()=>apiFetch<unknown>('/plans')})
  const [planId,setPlanId]=useState('');const [cancelId,setCancelId]=useState('');const [reason,setReason]=useState('');const [message,setMessage]=useState('')
  const rows=objectItems(history.data);const planRows=objectItems(plans.data);const active=rows.filter(row=>String(row.status??'').toLowerCase()==='active')
  const renew=async(event:React.FormEvent)=>{event.preventDefault();try{await apiPost('/my/subscription/renew',{planId} satisfies Schema<'RenewSubscriptionRequest'>,true);setMessage(t('Renewal request created. Complete payment to extend your access.'));await cache.invalidateQueries({queryKey:['my','subscriptions']})}catch(error){setMessage(error instanceof Error?error.message:t('Could not request renewal.'))}}
  const cancel=async()=>{if(!cancelId||!reason.trim())return;try{await apiFetch<void>('/my/subscription/cancel',{method:'PUT',body:JSON.stringify({subscriptionId:cancelId,reason:reason.trim()})});setMessage(t('Subscription cancelled.'));setCancelId('');setReason('');await cache.invalidateQueries({queryKey:['my','subscriptions']});await cache.invalidateQueries({queryKey:['my','subscription']})}catch(error){setMessage(error instanceof Error?error.message:t('Could not cancel subscription.'))}}
  return <section className="panel subscription-flow-panel"><div className="subscription-step-heading"><span>5</span><div><h2>{t('Subscription history & changes')}</h2><p>{t('Review past requests, request a renewal, or cancel an active subscription.')}</p></div></div>
    {history.isLoading?<Loading/>:history.isError?<ErrorState error={history.error}/>:rows.length===0?<div className="empty-state subscription-empty">{t('No subscription requests yet. Choose a plan above to begin.')}</div>:<div className="subscription-history-list">{rows.map((row,index)=>{const id=String(row.id??'');const status=String(row.status??'Pending');return <article className="subscription-history-item" key={id||index}><div><strong>{String(row.planName??t('Training plan'))}</strong><small>{row.startDate?new Intl.DateTimeFormat(language==='ar'?'ar-EG':'en-US',{dateStyle:'medium'}).format(new Date(String(row.startDate))):t('Request submitted')}</small></div><strong>{new Intl.NumberFormat(language==='ar'?'ar-EG':'en-US',{style:'currency',currency:/^[A-Z]{3}$/.test(String(row.currency??''))?String(row.currency):'EGP'}).format(Number(row.price??row.snapshotPrice??0))}</strong><span className={'subscription-status status-'+status.toLowerCase()}>{t(status)}</span>{status.toLowerCase()==='active'&&<button type="button" className="secondary-button" onClick={()=>{setCancelId(id);setReason('')}}>{t('Cancel')}</button>}</article>})}</div>}
    {planRows.length>0&&<form className="subscription-renew-form" onSubmit={event=>void renew(event)}><EntitySelect label={t('Choose a plan to renew or change your subscription')} value={planId} onChange={setPlanId} data={{items:planRows.map(plan=>({...plan,name:`${String(plan.name??t('Training plan'))} - ${String(plan.durationDays??'?')} ${t('days')} - ${String(plan.price??'')} ${String(plan.currency??'EGP')}`}))}}/><button disabled={!planId||plans.isLoading}>{t('Request renewal')}</button></form>}
    {cancelId&&<div className="subscription-cancel-box"><label>{t('Why are you cancelling?')}<input value={reason} onChange={event=>setReason(event.target.value)} maxLength={500} required/></label><div><button type="button" className="danger" disabled={!reason.trim()} onClick={()=>void cancel()}>{t('Confirm cancellation')}</button><button type="button" className="secondary-button" onClick={()=>{setCancelId('');setReason('')}}>{t('Keep subscription')}</button></div></div>}
    {active.length>0&&<p className="subscription-inline-note">{t('Cancelling stops renewal; your current access remains subject to the club subscription policy.')}</p>}{message&&<p role="status" className="form-feedback">{message}</p>}
  </section>
}
function ClientPaymentDetails() {
  const {t,language}=useLanguage();const [id,setId]=useState('');const [refresh,setRefresh]=useState(0);const [message,setMessage]=useState('')
  const payments=useQuery({queryKey:['my','payments'],queryFn:()=>apiFetch<unknown>('/my/payments?page=1&pageSize=100')});const rows=objectItems(payments.data)
  const payment=useQuery({queryKey:['my','payment',id],queryFn:()=>apiFetch<unknown>(`/my/payments/${id}`),enabled:Boolean(id),refetchInterval:q=>['Pending','PendingVerification'].includes(stringField(q.state.data,'status')??'')?5000:false})
  const status=useQuery({queryKey:['payment',id,'status',refresh],queryFn:()=>apiFetch<unknown>(`/payments/${id}/status`),enabled:Boolean(id),refetchInterval:q=>['Pending','PendingVerification'].includes(stringField(q.state.data,'status')??'')?5000:false})
  const invoice=async()=>{try{const file=await downloadFile(`/my/payments/${id}/invoice`);const url=URL.createObjectURL(file.blob);const a=document.createElement('a');a.href=url;a.download=file.filename;a.click();window.setTimeout(()=>URL.revokeObjectURL(url),1000)}catch(error){setMessage(error instanceof Error?error.message:t('Invoice is not available yet.'))}}
  const money=(row:Record<string,unknown>)=>{const currency=String(row.currency??'EGP');return new Intl.NumberFormat(language==='ar'?'ar-EG':'en-US',{style:'currency',currency:/^[A-Z]{3}$/.test(currency)?currency:'EGP'}).format(Number(row.amount??0))}
  return <section className="panel subscription-flow-panel"><div className="subscription-step-heading"><span>6</span><div><h2>{t('Payment details')}</h2><p>{t('Select a payment to see its verified status and invoice options.')}</p></div></div>
    {payments.isLoading?<Loading/>:payments.isError?<ErrorState error={payments.error}/>:rows.length===0?<div className="empty-state subscription-empty">{t('There are no payments to review yet.')}</div>:<div className="payment-history-list">{rows.map((row,index)=>{const paymentId=String(row.id??'');return <button type="button" className={'payment-history-item selectable'+(id===paymentId?' selected':'')} key={paymentId||index} onClick={()=>{setId(paymentId);setMessage('')}}><div><strong>{money(row)}</strong><small>{String(row.method??'')} ? {row.createdAt?new Intl.DateTimeFormat(language==='ar'?'ar-EG':'en-US',{dateStyle:'medium',timeStyle:'short'}).format(new Date(String(row.createdAt))):t('Date unavailable')}</small></div><span className={'subscription-status status-'+String(row.status??'pending').toLowerCase()}>{t(String(row.status??'Pending'))}</span></button>})}</div>}
    {id&&(payment.isLoading||status.isLoading)?<Loading/>:id&&(payment.isError||status.isError)?<ErrorState error={payment.error??status.error}/>:id&&<div className="payment-detail-card"><div className="section-heading"><div><h3>{t('Verified payment status')}</h3><p>{t('This status comes from the server; a return-page message alone does not confirm payment.')}</p></div><button type="button" className="secondary-button" onClick={()=>setRefresh(value=>value+1)}>{t('Refresh status')}</button></div><div className="payment-status-summary"><span className={'subscription-status status-'+String(stringField(status.data,'status')??'pending').toLowerCase()}>{t(stringField(status.data,'status')??'Pending')}</span><DataValue value={payment.data}/></div><button type="button" disabled={stringField(status.data,'status')!=='Confirmed'} onClick={()=>void invoice()}>{t('Download invoice (PDF)')}</button></div>}
    {message&&<p role="status" className="form-feedback">{message}</p>}
  </section>
}
function PaymobCheckoutAction({planId,disabled=false}:{planId:string;disabled?:boolean}) {
  const {t}=useLanguage(); const [error,setError]=useState(''); const [busy,setBusy]=useState(false)
  const start=async()=>{if(!planId||disabled)return;setBusy(true);setError('');try{const request:Schema<'CreatePaymentRequest'>={planId};const intention=await apiPost<{paymentId:string;checkoutUrl:string}>('/payments/paymob/create-intention',request,true);const checkout=new URL(intention.checkoutUrl);if(checkout.protocol!=='https:'||!checkout.pathname.endsWith('/unifiedcheckout/'))throw new Error(t('The payment provider returned an invalid checkout link.'));sessionStorage.setItem('coachpulse.paymob.paymentId',intention.paymentId);sessionStorage.setItem('coachpulse.paymob.startedAt',String(Date.now()));window.location.assign(checkout.href)}catch(cause){setError(cause instanceof Error?cause.message:t('Could not start Paymob checkout.'));setBusy(false)}}
  return <div className="paymob-action"><button type="button" disabled={busy||disabled||!planId} onClick={()=>void start()}>{busy?t('Opening secure checkout?'):t('Continue to Paymob')}</button>{error&&<p role="alert" className="form-error">{error}</p>}</div>
}
function PaymobPaymentReturn() {
  const paymentId = sessionStorage.getItem('coachpulse.paymob.paymentId') ?? ''
  const startedAt = Number(sessionStorage.getItem('coachpulse.paymob.startedAt') ?? Date.now())
  const [now, setNow] = useState(Date.now())
  useEffect(() => { const timer = window.setInterval(() => setNow(Date.now()), 1000); return () => window.clearInterval(timer) }, [])
  const query = useQuery({
    queryKey: ['paymob', 'return', paymentId],
    queryFn: () => apiFetch<{ status: string; subscriptionId: string }>(`/payments/${paymentId}/status`),
    enabled: Boolean(paymentId),
    refetchInterval: current => {
      const status = stringField(current.state.data, 'status')
      if (status === 'Confirmed' || status === 'Failed' || status === 'Rejected' || status === 'Refunded') return false
      return Date.now() - startedAt >= 120_000 ? false : 3000
    },
  })
  const status = stringField(query.data, 'status')
  const timedOut = now - startedAt >= 120_000
  const state = status === 'Confirmed' ? 'success' : status === 'Failed' || status === 'Rejected' || status === 'Refunded' ? 'failure' : timedOut ? 'pending' : 'checking'
  const labels = { success: "\u062A\u0645 \u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062F\u0641\u0639 \u0648\u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.", failure: "\u0644\u0645 \u064A\u0643\u062A\u0645\u0644 \u0627\u0644\u062F\u0641\u0639. \u0631\u0627\u062C\u0639 \u0637\u0631\u064A\u0642\u0629 \u0627\u0644\u062F\u0641\u0639 \u0623\u0648 \u0627\u0628\u062F\u0623 \u0645\u062D\u0627\u0648\u0644\u0629 \u062C\u062F\u064A\u062F\u0629.", pending: "\u0627\u0644\u062F\u0641\u0639 \u0645\u0627 \u0632\u0627\u0644 \u0645\u0639\u0644\u0642\u0627\u064B. \u064A\u0645\u0643\u0646\u0643 \u0627\u0644\u0639\u0648\u062F\u0629 \u0644\u0627\u062D\u0642\u0627\u064B \u0644\u0644\u062A\u062D\u0642\u0642 \u0645\u0646 \u0627\u0644\u062D\u0627\u0644\u0629.", checking: "\u062C\u0627\u0631\u064D \u0627\u0644\u062A\u062D\u0642\u0642 \u0645\u0646 \u0627\u0644\u062F\u0641\u0639 \u0645\u0646 \u0627\u0644\u062E\u0627\u062F\u0645\u2026" }
  return <Page title={'\u0639\u0648\u062F\u0629 Paymob'} subtitle={'\u062D\u0627\u0644\u0629 \u0627\u0644\u062F\u0641\u0639 \u0645\u0635\u062F\u0631\u0647\u0627 \u0627\u0644\u062E\u0627\u062F\u0645 \u0628\u0639\u062F \u0627\u0644\u062A\u062D\u0642\u0642 \u0645\u0646 Webhook'}><section className="panel" aria-live="polite"><h2>{labels[state]}</h2>{!paymentId ? <p>ظ„ط§ طھظˆط¬ط¯ ظ…ط­ط§ظˆظ„ط© ط¯ظپط¹ ظ…ط­ظپظˆط¸ط© ظ„ظ‡ط°ظ‡ ط§ظ„ط¬ظ„ط³ط©. ط§ظپطھط­ طµظپط­ط© ط§ظ„ط§ط´طھط±ط§ظƒط§طھ ظ„ط¨ط¯ط، ط§ظ„ط¯ظپط¹.</p> : query.isLoading ? <Loading /> : query.isError ? <ErrorState error={query.error} /> : <><p>ط§ظ„ط­ط§ظ„ط©: {status ?? 'Pending'}</p>{query.data?.subscriptionId && <p>ط§ظ„ط§ط´طھط±ط§ظƒ: {query.data.subscriptionId}</p>}</>}</section></Page>
}

function readGeneratedDraft(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object') return {}
  const row = value as Record<string, unknown>
  const raw = row.generatedContent
  if (raw && typeof raw === 'object') return raw as Record<string, unknown>
  if (typeof raw === 'string') {
    try { const parsed: unknown = JSON.parse(raw); return parsed && typeof parsed === 'object' ? parsed as Record<string, unknown> : {} }
    catch { return {} }
  }
  return row
}

function AiAdvancedWorkspace() {
  const cache = useQueryClient()
  const [searchParams] = useSearchParams()
  const { language, t } = useLanguage()
  const clients = useClients()
  const drafts = useQuery({ queryKey: ['ai', 'generated-programs'], queryFn: () => apiFetch<unknown>('/ai/generated-programs?page=1&pageSize=100') })
  const [programId, setProgramId] = useState(() => searchParams.get('draft') ?? '')
  const [clientId, setClientId] = useState('')
  const [notes, setNotes] = useState('')
  const [workoutPlan, setWorkoutPlan] = useState('')
  const [mealPlan, setMealPlan] = useState('')
  const [unmatched, setUnmatched] = useState('')
  const [result, setResult] = useState<Record<string, unknown> | null>(null)
  const [message, setMessage] = useState('')
  const detail = useQuery({ queryKey: ['ai', 'generated-program', programId], queryFn: () => apiFetch<unknown>(`/ai/generated-programs/${programId}`), enabled: Boolean(programId) })
  const history = useQuery({ queryKey: ['ai', 'churn-history', clientId], queryFn: () => apiFetch<unknown>(`/ai/churn/clients/${clientId}/history`), enabled: Boolean(clientId) })
  const draftContent = readGeneratedDraft(detail.data)
  const unmatchedItems = Array.isArray(draftContent.unmatchedExercises) ? draftContent.unmatchedExercises.map(String) : []
  useEffect(() => {
    if (!detail.data) return
    const content = readGeneratedDraft(detail.data)
    setWorkoutPlan(JSON.stringify(content.workoutPlan ?? {}, null, 2))
    setMealPlan(JSON.stringify(content.mealPlan ?? {}, null, 2))
    setUnmatched(Array.isArray(content.unmatchedExercises) ? content.unmatchedExercises.map(String).join('\n') : '')
  }, [detail.data])
  const save = async (event: React.FormEvent) => {
    event.preventDefault()
    try {
      const body: Schema<'AiGeneratedProgramEdit'> = { rowVersion: stringField(detail.data, 'rowVersion'), workoutPlan: JSON.parse(workoutPlan), mealPlan: JSON.parse(mealPlan), unmatchedExercises: unmatched.split('\n').map(item => item.trim()).filter(Boolean) }
      await apiFetch<void>(`/ai/generated-programs/${programId}`, { method: 'PUT', body: JSON.stringify(body) })
      setMessage('Draft changes saved.')
      await cache.invalidateQueries({ queryKey: ['ai', 'generated-program', programId] })
      await cache.invalidateQueries({ queryKey: ['ai', 'generated-programs'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save. Check the JSON format and draft version.') }
  }
  const regenerate = async () => {
    try {
      const created = await apiPost<unknown>(`/ai/generated-programs/${programId}/regenerate`, { coachNotes: notes } satisfies Schema<'AiRegenerateApiRequest'>)
      await cache.invalidateQueries({ queryKey: ['ai', 'generated-programs'] })
      const newId = responseId(created)
      if (newId) setProgramId(newId)
      setMessage('A new draft was created. The original draft is preserved.')
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not regenerate the draft.') }
  }
  const review = async (approve: boolean) => {
    if (!programId) return
    try {
      if (approve) {
        const approved = await apiFetch<unknown>(`/ai/generated-programs/${programId}/approve`, { method: 'PUT' })
        setResult(approved && typeof approved === 'object' ? approved as Record<string, unknown> : {})
        setMessage('Approved. A draft training program and meal plan were created. They still need to be assigned to a client.')
      } else {
        await apiFetch<void>(`/ai/generated-programs/${programId}/reject`, { method: 'PUT' })
        setResult(null)
        setMessage('Draft rejected.')
      }
      await cache.invalidateQueries({ queryKey: ['ai', 'generated-program', programId] })
      await cache.invalidateQueries({ queryKey: ['ai', 'generated-programs'] })
      await cache.invalidateQueries({ queryKey: ['programs'] })
      await cache.invalidateQueries({ queryKey: ['meal-plans'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : `Could not ${approve ? 'approve' : 'reject'} this draft.`) }
  }
  const progress = async () => {
    try { setResult(await apiPost(`/ai/progress-summary/${clientId}`, { language, coachNotes: notes } satisfies Schema<'AiProgressSummaryRequest'>) as Record<string, unknown>); setMessage('Progress summary created.') }
    catch (error) { setMessage(error instanceof Error ? error.message : 'Could not create the progress summary.') }
  }
  const draftRows = objectItems(drafts.data)
  const clientName = objectItems(clients.data).find(item => String(item.id) === String((detail.data as Record<string, unknown> | undefined)?.clientId))?.name
  const draftStatus = stringField(detail.data, 'status') ?? 'Draft'
  return <Page title={t('AI draft review')} subtitle={t('Review generated training and meal plans before adding them to your workspace.')}>
    <section className="panel">
      <div className="section-heading"><div><h2>{t('Saved AI drafts')}</h2><p>{t('Choose a draft to inspect its plan, resolve unmatched exercises, and approve it.')}</p></div><Link className="secondary-button" to="/app/ai">{t('Back to AI tools')}</Link></div>
      {drafts.isLoading ? <Loading /> : drafts.isError ? <ErrorState error={drafts.error} /> : draftRows.length === 0 ? <div className="empty-state">{t('No saved drafts yet. Generate a program first.')}</div> : <div className="program-card-grid">{draftRows.map((item, index) => { const id = String(item.id ?? ''); const name = objectItems(clients.data).find(client => String(client.id) === String(item.clientId))?.name; return <button type="button" className={programId === id ? 'exercise-list-card active' : 'exercise-list-card'} key={id || index} onClick={() => { setProgramId(id); setResult(null); setMessage('') }}><strong>{String(name ?? t('Client'))}</strong><span>{t(String(item.status ?? 'Draft'))}</span><small>{t('Generated with')} {String(item.modelVersion ?? t('AI model'))}</small></button> })}</div>}
    </section>
    {programId && <section className="panel">
      {detail.isLoading ? <Loading /> : detail.isError ? <ErrorState error={detail.error} /> : detail.data ? <>
        <div className="section-heading"><div><span className="eyebrow">{t(draftStatus)}</span><h2>{t('Review draft for')} {String(clientName ?? t('Client'))}</h2><p>{t('Prompt version')}: {stringField(detail.data, 'promptVersion') ?? '\u2014'} آ· {t('Model')}: {stringField(detail.data, 'modelVersion') ?? '\u2014'}</p></div></div>
        {unmatchedItems.length > 0 && <div className="form-error"><strong>{t('Exercises need attention before approval')}</strong><p>{t('Replace unmatched exercises with items from the exercise library, then save the draft.')}</p><div className="data-chips">{unmatchedItems.map((item, index) => <span className="data-chip" key={index}>{item}</span>)}</div></div>}
        <div className="admin-dashboard-grid"><section className="nested-panel"><h3>{t('Training plan preview')}</h3><DataValue value={draftContent.workoutPlan} /></section><section className="nested-panel"><h3>{t('Meal plan preview')}</h3><DataValue value={draftContent.mealPlan} /></section></div>
        {draftStatus === 'Draft' && <details className="nested-panel"><summary>{t('Advanced draft editing')}</summary><p>{t('Edit structured plan JSON only when needed. Invalid JSON will not be saved.')}</p><form onSubmit={event => void save(event)}><label>{t('Training plan JSON')}<textarea rows={12} value={workoutPlan} onChange={event => setWorkoutPlan(event.target.value)} /></label><label>{t('Meal plan JSON')}<textarea rows={10} value={mealPlan} onChange={event => setMealPlan(event.target.value)} /></label><label>{t('Unmatched exercise names, one per line')}<textarea rows={3} value={unmatched} onChange={event => setUnmatched(event.target.value)} /></label><button type="submit" disabled={detail.isFetching}>{t('Save draft changes')}</button></form></details>}
        {draftStatus === 'Draft' && <section className="nested-panel"><h3>{t('Request a revision')}</h3><p>{t('A revised draft is created separately. This draft remains available.')}</p><label>{t('Notes for the coach assistant')}<textarea maxLength={2000} value={notes} onChange={event => setNotes(event.target.value)} /></label><button onClick={() => void regenerate()} disabled={!notes.trim()}>{t('Create revised draft')}</button></section>}
        {draftStatus === 'Draft' && <div className="actions"><button onClick={() => void review(true)} disabled={unmatchedItems.length > 0}>{t('Approve and create program')}</button><button className="danger" onClick={() => void review(false)}>{t('Reject draft')}</button></div>}
        {result && <div className="owner-flow-next"><div><strong>{t('Created successfully')}</strong><p>{t('The training program and meal plan are saved as drafts. Open them to review details, add exercises if needed, and assign them to a client.')}</p></div>{typeof result.programId === 'string' && <Link className="primary-button" to={`/app/programs?programId=${encodeURIComponent(result.programId)}`}>{t('Manage training program')}</Link>}{typeof result.mealPlanId === 'string' && <Link className="secondary-button" to={`/app/meal-plans?planId=${encodeURIComponent(result.mealPlanId)}`}>{t('Manage meal plan')}</Link>}</div>}
      </> : <div className="empty-state">{t('Select a draft above.')}</div>}
    </section>}
    <section className="panel"><h2>{t('Client progress summary')}</h2><EntitySelect label={t('Client')} value={clientId} onChange={setClientId} data={clients.data} placeholder={t('Choose a client')} /><label>{t('Coach notes')}<textarea maxLength={2000} value={notes} onChange={event => setNotes(event.target.value)} /></label><button onClick={() => void progress()} disabled={!clientId}>{t('Generate summary')}</button>{result && !('programId' in result) && <JsonPanel value={result} />}</section>
    <section className="panel"><h2>{t('Churn risk history')}</h2><EntitySelect label={t('Client')} value={clientId} onChange={setClientId} data={clients.data} placeholder={t('Choose a client')} />{history.isLoading ? <Loading /> : history.isError ? <ErrorState error={history.error} /> : clientId ? <DataValue value={history.data} /> : <p>{t('Select a client to see historical scores.')}</p>}</section>
    {message && <p role="status" className="inline-notice">{t(message)}</p>}
  </Page>
}
function ClientEditActions({ id }: { id: string }) {
  const cache = useQueryClient()
  const notesData = useQuery({ queryKey: ['client', id, 'notes'], queryFn: () => apiFetch<unknown>(`/clients/${id}/notes`) })
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [goals, setGoals] = useState('')
  const [noteId, setNoteId] = useState('')
  const [message, setMessage] = useState('')
  const save = async (event: React.FormEvent) => {
    event.preventDefault()
    const body: Schema<'UpdateClientRequest'> = { name: name || null, phone: phone || null, goals: goals || null }
    try { await apiFetch<void>(`/clients/${id}`, { method: 'PUT', body: JSON.stringify(body) }); setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u0639\u0645\u064A\u0644."); await cache.invalidateQueries({ queryKey: ['client', id] }); await cache.invalidateQueries({ queryKey: queryKeys.clients }) }
    catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0639\u0645\u064A\u0644.") }
  }
  const deleteNote = async () => {
    if (!noteId || !window.confirm("\u062D\u0630\u0641 \u0627\u0644\u0645\u0644\u0627\u062D\u0638\u0629 \u0627\u0644\u0645\u062D\u062F\u062F\u0629\u061F")) return
    try { await apiFetch<void>(`/clients/${id}/notes/${noteId}`, { method: 'DELETE' }); setMessage("\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u0645\u0644\u0627\u062D\u0638\u0629."); setNoteId('') }
    catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u0645\u0644\u0627\u062D\u0638\u0629.") }
  }
  return <section className="panel"><h3>طھط¹ط¯ï؟½? ظ„ ط¨ï؟½? ط§ï؟½? ط§طھ ط§ظ„ط¹ظ…ï؟½? ظ„</h3><form onSubmit={event => void save(event)}><label>ط§ظ„ط§ط³ظ…<input value={name} onChange={event => setName(event.target.value)} /></label><label>ط§ظ„ظ‡ط§طھظپ<input value={phone} onChange={event => setPhone(event.target.value)} /></label><label>ط§ظ„ط£ظ‡ط¯ط§ظپ<textarea value={goals} onChange={event => setGoals(event.target.value)} /></label><button>ط­ظپط¸ ط§ظ„ط¨ï؟½? ط§ï؟½? ط§طھ</button></form><h3>ط­ط°ظپ ظ…ظ„ط§ط­ط¸ط©</h3><EntitySelect label={'\u0627\u062E\u062A\u0631 \u0645\u0644\u0627\u062D\u0638\u0629'} value={noteId} onChange={setNoteId} data={notesData.data} /><button className="danger" onClick={() => void deleteNote()} disabled={!noteId}>ط­ط°ظپ ط§ظ„ظ…ظ„ط§ط­ط¸ط©</button>{message && <p role="status">{message}</p>}</section>
}

function ClientAdditionalReads({ id }: { id: string }) {
  const mealPlan = useQuery({ queryKey: ['client', id, 'meal-plan'], queryFn: () => apiFetch<unknown>(`/clients/${id}/meal-plan`) })
  const programs = useQuery({ queryKey: ['client', id, 'programs'], queryFn: () => apiFetch<unknown>(`/clients/${id}/programs`) })
  const progress = useQuery({ queryKey: ['client', id, 'progress'], queryFn: () => apiFetch<unknown>(`/clients/${id}/progress`) })
  return <section className="cards">{[{ title: "\u0627\u0644\u062E\u0637\u0629 \u0627\u0644\u063A\u0630\u0627\u0626\u064A\u0629 \u0627\u0644\u062D\u0627\u0644\u064A\u0629", query: mealPlan }, { title: "\u0633\u062C\u0644 \u0627\u0644\u0628\u0631\u0627\u0645\u062C", query: programs }, { title: "\u0627\u0644\u062A\u0642\u062F\u0645", query: progress }].map(({ title, query }) => <section className="panel" key={title}><h3>{title}</h3>{query.isLoading ? <Loading /> : query.isError ? <ErrorState error={query.error} /> : <JsonPanel value={query.data} />}</section>)}</section>
}

function AdminTenantManager({ tenantRows, tenantLoading, tenantError, selectedTenantId, onSelectTenant }: { tenantRows: Record<string, unknown>[]; tenantLoading: boolean; tenantError: unknown; selectedTenantId: string; onSelectTenant: (id: string) => void }) {
  const cache = useQueryClient()
  const id = selectedTenantId
  const setId = onSelectTenant
  const [reason, setReason] = useState('')
  const [confirmSlug, setConfirmSlug] = useState('')
  const [message, setMessage] = useState('')
  const selectedTenant = tenantRows.find(tenant => String(tenant.id ?? '') === id)
  const selectedSlug = String(selectedTenant?.slug ?? '')
  const tenantIsActive = String(selectedTenant?.status ?? '').toLowerCase() === 'active'
  const detail = useQuery({ queryKey: ['admin', 'tenant', id], queryFn: () => apiFetch<Record<string, unknown>>(`/superadmin/tenants/${id}`), enabled: Boolean(id) })
  const usage = useQuery({ queryKey: ['admin', 'tenant-usage', id], queryFn: () => apiFetch<Record<string, unknown>>(`/superadmin/tenants/${id}/usage`), enabled: Boolean(id) })
  const changeStatus = async (action: 'activate' | 'suspend') => {
    if (!id || !reason.trim()) return
    try {
      const statusBody = JSON.stringify({ reason } satisfies Schema<'TenantStatusRequest'>)
      if (action === 'activate') await apiFetch<void>(`/superadmin/tenants/${id}/activate`, { method: 'PUT', body: statusBody })
      else await apiFetch<void>(`/superadmin/tenants/${id}/suspend`, { method: 'PUT', body: statusBody })
      setMessage(action === 'activate' ? "\u062A\u0645 \u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0645\u0633\u0627\u062D\u0629." : "\u062A\u0645 \u062A\u0639\u0644\u064A\u0642 \u0627\u0644\u0645\u0633\u0627\u062D\u0629.")
      await cache.invalidateQueries({ queryKey: ['admin', 'tenants'] })
      await cache.invalidateQueries({ queryKey: ['admin', 'tenant', id] })
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u063A\u064A\u064A\u0631 \u062D\u0627\u0644\u0629 \u0627\u0644\u0645\u0633\u0627\u062D\u0629.") }
  }
  const scheduleDelete = async () => {
    if (!id || !confirmSlug.trim() || !window.confirm("\u062C\u062F\u0648\u0644\u0629 \u062D\u0630\u0641 \u0627\u0644\u0645\u0633\u0627\u062D\u0629 \u0628\u0639\u062F \u0641\u062A\u0631\u0629 \u0627\u0644\u0633\u0645\u0627\u062D\u061F")) return
    try {
      await apiFetch<void>(`/superadmin/tenants/${id}`, { method: 'DELETE', body: JSON.stringify({ slug: confirmSlug } satisfies Schema<'DeleteTenantRequest'>) })
      setMessage("\u062A\u0645\u062A \u062C\u062F\u0648\u0644\u0629 \u0627\u0644\u062D\u0630\u0641 \u0628\u0639\u062F \u0641\u062A\u0631\u0629 \u0627\u0644\u0633\u0645\u0627\u062D.")
      await cache.invalidateQueries({ queryKey: ['admin', 'tenants'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062C\u062F\u0648\u0644\u0629 \u0627\u0644\u062D\u0630\u0641.") }
  }
  return <section className="panel admin-tenant-manager"><div className="section-heading"><div><span className="admin-section-kicker">ط¥ط¬ط±ط§ط،ط§طھ ط§ظ„ط¥ط¯ط§ط±ط©</span><h2>ط¥ط¯ط§ط±ط© ظ…ط³ط§ط­ط© طھط¯ط±ï؟½? ط¨</h2><p>ط§ط®طھط± ط§ظ„ظ…ط³ط§ط­ط© ط«ظ… ï؟½? ظپظ‘ط° ط§ظ„ط¥ط¬ط±ط§ط، ط§ظ„ظ…ï؟½? ط§ط³ط¨. طھط¸ظ‡ط± طھظپط§طµï؟½? ظ„ظ‡ط§ ظˆط§ط³طھط®ط¯ط§ظ…ظ‡ط§ ظ‡ï؟½? ط§.</p></div><label>ط§ظ„ظ…ط³ط§ط­ط© ط§ظ„ظ…ط­ط¯ط¯ط©<select value={id} onChange={event => { setId(event.target.value); onSelectTenant(event.target.value); setConfirmSlug(''); setReason(''); setMessage('') }}><option value="">ط§ط®طھط± ظ…ط³ط§ط­ط© ط§ظ„طھط¯ط±ï؟½? ط¨</option>{tenantRows.map((tenant, index) => <option key={String(tenant.id ?? index)} value={String(tenant.id ?? '')}>{String(tenant.name ?? tenant.slug ?? tenant.id)}</option>)}</select></label></div>
    {tenantLoading ? <Loading /> : tenantError ? <ErrorState error={tenantError} /> : tenantRows.length === 0 ? <p className="empty-state">ظ„ط§ طھظˆط¬ط¯ ظ…ط³ط§ط­ط§طھ ظ„ط¹ط±ط¶ظ‡ط§.</p> : !id && <p className="admin-selection-hint">ط§ط®طھط± ظ…ط³ط§ط­ط© ظ…ï؟½?  ط§ظ„ظ‚ط§ط¦ظ…ط© ط£ظˆ ط§ط³طھط®ط¯ظ… ط²ط± آ«ط¥ط¯ط§ط±ط© ط§ظ„ظ…ط³ط§ط­ط©آ» ظپï؟½?  ط¬ط¯ظˆظ„ ط§ظ„ظ…ط³ط§ط­ط§طھ.</p>}
    {id && <div className="admin-tenant-detail"><div className="admin-selected-heading"><div><span className="admin-section-kicker">ط§ظ„ظ…ط³ط§ط­ط© ط§ظ„ظ…ط­ط¯ط¯ط©</span><h3>{String(selectedTenant?.name ?? "\u062A\u0641\u0627\u0635\u064A\u0644 \u0627\u0644\u0645\u0633\u0627\u062D\u0629")}</h3></div><span className={`tenant-status ${tenantIsActive ? 'active' : 'suspended'}`}>{String(selectedTenant?.status ?? "\u063A\u064A\u0631 \u0645\u0639\u0631\u0648\u0641")}</span></div>{detail.isLoading ? <Loading /> : detail.isError ? <ErrorState error={detail.error} /> : detail.data !== undefined && <div className="admin-facts">{[["\u0627\u0644\u0627\u0633\u0645", detail.data.name], ["\u0627\u0644\u0631\u0627\u0628\u0637", detail.data.slug], ["\u0627\u0644\u062D\u0627\u0644\u0629", detail.data.status], ["\u0645\u0648\u0639\u062F \u0627\u0644\u062D\u0630\u0641", detail.data.deletionScheduledAt]].map(([label, value]) => <div key={String(label)}><span>{String(label)}</span><strong>{value == null ? 'â€”' : String(value)}</strong></div>)}</div>}
      <h3>ط§ظ„ط§ط³طھط®ط¯ط§ظ…</h3>{usage.isLoading ? <Loading /> : usage.isError ? <ErrorState error={usage.error} /> : usage.data !== undefined && <div className="admin-metric-grid compact">{[["\u0627\u0644\u0639\u0645\u0644\u0627\u0621", usage.data.clients], ["\u0637\u0644\u0628\u0627\u062A AI \u0627\u0644\u064A\u0648\u0645", usage.data.aiRequestsToday], ["\u0627\u0644\u062A\u062E\u0632\u064A\u0646", formatAdminValue(usage.data.storageBytes, 'storageBytes')]].map(([label, value]) => <div className="admin-metric" key={String(label)}><span>{String(label)}</span><strong>{value == null ? 'â€”' : String(value)}</strong></div>)}</div>}
      <div className="admin-status-actions"><label>ط³ط¨ط¨ ط§ظ„ط¥ط¬ط±ط§ط،<input maxLength={500} value={reason} onChange={event => setReason(event.target.value)} placeholder={'\u0627\u0643\u062A\u0628 \u0633\u0628\u0628\u0627\u064B \u0648\u0627\u0636\u062D\u0627\u064B \u0644\u0644\u062A\u0641\u0639\u064A\u0644 \u0623\u0648 \u0627\u0644\u062A\u0639\u0644\u064A\u0642'} /></label><div className="actions"><button onClick={() => void changeStatus('activate')} disabled={!reason.trim() || tenantIsActive}>طھظپط¹ï؟½? ظ„ ط§ظ„ظ…ط³ط§ط­ط©</button><button className="secondary-button" onClick={() => void changeStatus('suspend')} disabled={!reason.trim() || !tenantIsActive}>طھط¹ظ„ï؟½? ظ‚ ط§ظ„ظ…ط³ط§ط­ط©</button></div></div><details className="admin-danger-zone"><summary>ط¥ط¬ط±ط§ط،ط§طھ ط­ط°ظپ ط§ظ„ظ…ط³ط§ط­ط©</summary><p>ط³ï؟½? طھظ… ط¬ط¯ظˆظ„ط© ط§ظ„ط­ط°ظپ ط¨ط¹ط¯ ظپطھط±ط© ط§ظ„ط³ظ…ط§ط­. ط§ظƒطھط¨ ط±ط§ط¨ط· ط§ظ„ظ…ط³ط§ط­ط© ظƒظ…ط§ ظ‡ظˆ ظ„طھط£ظƒï؟½? ط¯ ط§ظ„ط¹ظ…ظ„ï؟½? ط©.</p><label>ط±ط§ط¨ط· ط§ظ„ظ…ط³ط§ط­ط©<input value={confirmSlug} onChange={event => setConfirmSlug(event.target.value)} placeholder={selectedSlug || 'workspace-link'} /></label><button className="danger" onClick={() => void scheduleDelete()} disabled={!selectedSlug || confirmSlug.trim() !== selectedSlug}>ط¬ط¯ظˆظ„ط© ط§ظ„ط­ط°ظپ ط¨ط¹ط¯ ظپطھط±ط© ط§ظ„ط³ظ…ط§ط­</button></details>{message && <p role="status">{message}</p>}</div>}
  </section>
}
function PendingCheckInReviews() {
  const cache = useQueryClient()
  const [page, setPage] = useState(1)
  const [feedback, setFeedback] = useState<Record<string, string>>({})
  const [message, setMessage] = useState('')
  const query = useQuery({ queryKey: ['checkins', 'pending-review', page], queryFn: () => apiFetch<unknown>(`/checkins/pending-review?page=${page}&pageSize=20`) })
  const rows = objectItems(query.data)
  const send = async (clientId: string, checkInId: string) => {
    const text = feedback[checkInId]?.trim()
    if (!text) return
    try {
      await apiFetch<void>(`/clients/${clientId}/checkins/${checkInId}/feedback`, { method: 'POST', body: JSON.stringify({ feedback: text } satisfies Schema<'CheckInFeedbackRequest'>) })
      setMessage("\u062A\u0645 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0645\u0631\u0627\u062C\u0639\u0629."); setFeedback(current => ({ ...current, [checkInId]: '' })); await cache.invalidateQueries({ queryKey: ['checkins', 'pending-review'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0645\u0631\u0627\u062C\u0639\u0629.") }
  }
  const total = query.data && typeof query.data === 'object' && 'totalCount' in query.data && typeof query.data.totalCount === 'number' ? query.data.totalCount : query.data && typeof query.data === 'object' && 'total' in query.data && typeof query.data.total === 'number' ? query.data.total : rows.length
  return <Page title={'\u0645\u0631\u0627\u062C\u0639\u0629 Check-ins'} subtitle={'\u0642\u0627\u0626\u0645\u0629 \u0627\u0644\u0623\u0642\u062F\u0645 \u0623\u0648\u0644\u0627\u064B\u060C \u0645\u0639 \u0635\u0644\u0627\u062D\u064A\u0627\u062A \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0627\u0644\u0645\u062E\u0635\u0635\u0629 \u0644\u0644\u0645\u062F\u0631\u0628'}><section className="panel">{query.isLoading ? <Loading /> : query.isError ? <ErrorState error={query.error} /> : rows.length === 0 ? <p>ظ„ط§ طھظˆط¬ط¯ ط³ط¬ظ„ط§طھ ط¨ط§ï؟½? طھط¸ط§ط± ط§ظ„ظ…ط±ط§ط¬ط¹ط©.</p> : rows.map((item, index) => { const checkInId = typeof item.id === 'string' ? item.id : ''; const clientId = typeof item.clientId === 'string' ? item.clientId : ''; return <article className="panel" key={checkInId || index}><h3>{String(item.clientName ?? clientId)}</h3><p>{String(item.date ?? '')} ط£â€ڑ |  {String(item.weight ?? '')}</p><label>طھط¹ظ„ï؟½? ظ‚ ط§ظ„ظ…ط¯ط±ط¨<textarea maxLength={4000} value={feedback[checkInId] ?? ''} onChange={event => setFeedback(current => ({ ...current, [checkInId]: event.target.value }))} required /></label><button onClick={() => void send(clientId, checkInId)} disabled={!feedback[checkInId]?.trim()}>ط¥ط±ط³ط§ظ„ ط§ظ„طھط¹ظ„ï؟½? ظ‚</button></article> })}<div className="actions"><button onClick={() => setPage(value => Math.max(1, value - 1))} disabled={page <= 1}>ط§ظ„ط³ط§ط¨ظ‚</button><span>طµظپط­ط© {page} آ· {total}  ط³ط¬ظ„</span><button onClick={() => setPage(value => value + 1)} disabled={page * 20 >= total}>ط§ظ„طھط§ظ„ï؟½? </button></div>{message && <p role="status">{message}</p>}</section></Page>
}

function SubscriptionManagement() {
  const { t } = useLanguage()
  const cache = useQueryClient(); const me = useMe(); const clients = useClients(1,100)
  const plans = useQuery({ queryKey:['tenant','plans'], queryFn:()=>apiFetch<unknown>('/plans') })
  const subscriptions = useQuery({ queryKey:['tenant','subscriptions'], queryFn:()=>apiFetch<unknown>('/subscriptions') })
  const [subscriptionId,setSubscriptionId]=useState(''); const [clientId,setClientId]=useState(''); const [planId,setPlanId]=useState(''); const [actionReason,setActionReason]=useState(''); const [days,setDays]=useState('30'); const [message,setMessage]=useState('')
  const rows=objectItems(subscriptions.data); const clientRows=objectItems(clients.data); const planRows=objectItems(plans.data); const selected=rows.find(row=>String(row.id??'')===subscriptionId); const owner=me.data?.role==='TenantOwner'
  const detail=useQuery({queryKey:['tenant','subscription',subscriptionId],queryFn:()=>apiFetch<unknown>(`/subscriptions/${subscriptionId}`),enabled:Boolean(subscriptionId)})
  const create=async(event:React.FormEvent)=>{event.preventDefault();if(!clientId||!planId)return;try{await apiPost('/subscriptions',{clientId,planId,reason:null} satisfies Schema<'ManualSubscriptionRequest'>);setMessage(t('Subscription activated.'));await cache.invalidateQueries({queryKey:['tenant','subscriptions']})}catch(error){setMessage(error instanceof Error?error.message:t('Could not activate subscription.'))}}
  const act=async(action:'extend'|'pause'|'resume'|'cancel')=>{if(!subscriptionId||!actionReason.trim())return;if(action==='cancel'&&!window.confirm(t('Cancel this subscription?')))return;try{await apiFetch<void>(`/subscriptions/${subscriptionId}/${action}`,{method:'PUT',body:JSON.stringify({reason:actionReason.trim(),days:action==='extend'?Number(days):null} satisfies Schema<'SubscriptionActionRequest'>)});setMessage(t('Subscription updated.'));setActionReason('');await cache.invalidateQueries({queryKey:['tenant','subscriptions']});await cache.invalidateQueries({queryKey:['tenant','subscription',subscriptionId]})}catch(error){setMessage(error instanceof Error?error.message:t('Could not update the subscription.'))}}
  return <Page title={t('Subscriptions')} subtitle={t('A clear path from published plan to active client access.')}>
    {owner&&<section className="owner-subscription-flow"><article><span>1</span><div><strong>{t('Publish a plan')}</strong><p>{t('Set a price and duration before clients can request membership.')}</p><Link to="/app/plans">{t('Manage plans')} &gt;</Link></div></article><article><span>2</span><div><strong>{t('Invite the client')}</strong><p>{t('The client chooses a plan and submits a payment request from their account.')}</p><Link to="/app/clients">{t('Manage clients')} &gt;</Link></div></article><article><span>3</span><div><strong>{t('Review payment and activate')}</strong><p>{t('Confirm InstaPay, record a pending cash payment, or activate access manually.')}</p><Link to="/app/payments">{t('Review payments')} &gt;</Link></div></article></section>}
    {owner&&<section className="panel"><h2>{t('Activate without online payment')}</h2><p>{t('Use this when payment was collected outside the platform. This grants access immediately and does not create a payment record. To record cash in the ledger, first have the client submit a subscription request, then record that pending request under Payments.')}</p>{clients.isError?<ErrorState error={clients.error}/>:plans.isError?<ErrorState error={plans.error}/>:<form className="form-grid" onSubmit={event=>void create(event)}><EntitySelect label={t('Client')} value={clientId} onChange={setClientId} data={{items:clientRows.map(row=>({...row,id:row.id,name:row.name??row.email??t('Client')}))}}/><EntitySelect label={t('Plan')} value={planId} onChange={setPlanId} data={{items:planRows.filter(row=>row.isActive!==false).map(row=>({...row,id:row.id,name:`${String(row.name??t('Plan'))} - ${String(row.price??'')} ${String(row.currency??'EGP')} - ${String(row.durationDays??'')} ${t('days')}`}))}}/><button disabled={!clientId||!planId}>{t('Activate subscription')}</button></form>}</section>}
    <section className="panel"><div className="section-heading"><div><h2>{t('Client subscriptions')}</h2><p>{t('Review status and dates. Select a subscription to manage it.')}</p></div><span className="count-badge">{rows.length} {t('subscriptions')}</span></div>{subscriptions.isLoading?<Loading/>:subscriptions.isError?<ErrorState error={subscriptions.error}/>:rows.length===0?<div className="empty-state"><strong>{t('No subscriptions yet.')}</strong><p>{t('Publish a plan, then invite clients to request it.')}</p><Link to="/app/plans">{t('Manage plans')} &gt;</Link></div>:<div className="table-wrap"><table><thead><tr><th>{t('Client')}</th><th>{t('Plan')}</th><th>{t('Status')}</th><th>{t('Price')}</th><th>{t('End date')}</th><th/></tr></thead><tbody>{rows.map((row,index)=>{const id=String(row.id??'');const client=clientRows.find(item=>String(item.id??'')===String(row.clientId??''));return <tr key={id||index} className={id===subscriptionId?'selected-row':''}><td>{String(client?.name??client?.email??t('Client'))}</td><td>{String(row.planName??t('Plan'))}</td><td><span className={'subscription-status status-'+String(row.status??'').toLowerCase()}>{t(String(row.status??'Unknown'))}</span></td><td>{String(row.price??'-')} {String(row.currency??'')}</td><td>{formatAdminValue(row.endDate,'endDate')}</td><td><button type="button" className="table-action" onClick={()=>{setSubscriptionId(id);setActionReason('')}}>{t('Manage')}</button></td></tr>})}</tbody></table></div>}</section>
    {subscriptionId&&<section className="panel"><div className="section-heading"><div><h2>{t('Subscription details')}</h2><p>{selected?`${String(selected.planName??t('Plan'))} - ${t(String(selected.status??''))}`:t('Loading subscription')}</p></div><button className="secondary-button" onClick={()=>setSubscriptionId('')}>{t('Close')}</button></div>{detail.isLoading?<Loading/>:detail.isError?<ErrorState error={detail.error}/>:<div className="subscription-summary">{[[t('Client'),clientRows.find(item=>String(item.id??'')===String(selected?.clientId??''))?.name??'-'],[t('Plan'),selected?.planName??'-'],[t('Price'),selected?.price??'-'],[t('Start date'),formatAdminValue(selected?.startDate,'startDate')],[t('End date'),formatAdminValue(selected?.endDate,'endDate')],[t('Status'),t(String(selected?.status??''))]].map(([label,value])=><div key={String(label)}><small>{String(label)}</small><strong>{String(value)}</strong></div>)}</div>}{owner&&<div className="subscription-actions"><label>{t('Reason')}<input value={actionReason} onChange={event=>setActionReason(event.target.value)} required placeholder={t('Reason for this change')}/></label><label>{t('Extension days')}<input type="number" min="1" max="3650" value={days} onChange={event=>setDays(event.target.value)}/></label><div className="actions"><button onClick={()=>void act('extend')} disabled={!actionReason.trim()}>{t('Extend')}</button><button onClick={()=>void act('pause')} disabled={!actionReason.trim()||String(selected?.status).toLowerCase()!=='active'}>{t('Pause')}</button><button onClick={()=>void act('resume')} disabled={!actionReason.trim()||String(selected?.status).toLowerCase()!=='paused'}>{t('Resume')}</button><button className="danger" onClick={()=>void act('cancel')} disabled={!actionReason.trim()||['cancelled','expired'].includes(String(selected?.status).toLowerCase())}>{t('Cancel')}</button></div></div>}</section>}{message&&<p role="status" className="form-feedback">{message}</p>}</Page>
}function TenantPaymentInsights() {
  const [id, setId] = useState('')
  const payments = usePayments()
  const [from, setFrom] = useState(new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10))
  const [to, setTo] = useState(new Date().toISOString().slice(0, 10))
  const detail = useQuery({ queryKey: ['tenant', 'payment', id], queryFn: () => apiFetch<unknown>(`/tenant/payments/${id}`), enabled: Boolean(id) })
  const summary = useQuery({ queryKey: ['tenant', 'payment-summary', from, to], queryFn: () => apiFetch<unknown>(`/tenant/payments/summary?from=${encodeURIComponent(new Date(`${from}T00:00:00Z`).toISOString())}&to=${encodeURIComponent(new Date(`${to}T23:59:59Z`).toISOString())}`) })
  return <section className="panel"><h2>ظ…ظ„ط®طµ ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ</h2><div className="actions"><label>ظ…ï؟½? <input type="date" value={from} onChange={event => setFrom(event.target.value)} /></label><label>ط¥ظ„ظ‰<input type="date" value={to} onChange={event => setTo(event.target.value)} /></label></div>{summary.isLoading ? <Loading /> : summary.isError ? <ErrorState error={summary.error} /> : <JsonPanel value={summary.data} />}<h3>طھظپط§طµï؟½? ظ„ ط¯ظپط¹ط©</h3><EntitySelect label={'\u0627\u062E\u062A\u0631 \u062F\u0641\u0639\u0629'} value={id} onChange={setId} data={payments.data} /><>{detail.isLoading ? <Loading /> : detail.isError ? <ErrorState error={detail.error} /> : detail.data !== undefined && <JsonPanel value={detail.data} />}</></section>
}
function OnboardingPage() {
  const [goals, setGoals] = useState('')
  const [healthInfo, setHealthInfo] = useState('')
  const [weight, setWeight] = useState('')
  const [measurements, setMeasurements] = useState('')
  const [message, setMessage] = useState('')
  const submit = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'OnboardingRequest'> = { goals, healthInfo, weight: weight ? Number(weight) : null, measurements: measurements || null }; try { await apiFetch<unknown>('/my/onboarding', { method: 'POST', body: JSON.stringify(body) }); setMessage("\u0627\u0643\u062A\u0645\u0644\u062A \u0627\u0644\u062A\u0647\u064A\u0626\u0629.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u062A\u0647\u064A\u0626\u0629.") } }
  return <Page title={'\u0627\u0644\u062A\u0647\u064A\u0626\u0629 \u0627\u0644\u0623\u0648\u0644\u0649'} subtitle={'\u0623\u062E\u0628\u0631 \u0645\u062F\u0631\u0628\u0643 \u0628\u0623\u0647\u062F\u0627\u0641\u0643 \u0648\u0628\u064A\u0627\u0646\u0627\u062A\u0643 \u0627\u0644\u0623\u0648\u0644\u064A\u0629'}><section className="panel"><form onSubmit={event => void submit(event)}><label>ط§ظ„ط£ظ‡ط¯ط§ظپ<textarea value={goals} onChange={event => setGoals(event.target.value)} /></label><label>ظ…ط¹ظ„ظˆظ…ط§طھ طµط­ï؟½? ط©<textarea value={healthInfo} onChange={event => setHealthInfo(event.target.value)} /></label><label>ط§ظ„ظˆط²ï؟½?  ط¨ط§ظ„ظƒط¬ظ…<input type="number" min="1" max="500" step="0.1" value={weight} onChange={event => setWeight(event.target.value)} /></label><label>ط§ظ„ظ‚ï؟½? ط§ط³ط§طھ<textarea value={measurements} onChange={event => setMeasurements(event.target.value)} /></label><button>ط­ظپط¸ ط§ظ„طھظ‡ï؟½? ط¦ط©</button></form>{message && <p>{message}</p>}</section></Page>
}
function ClientProfile() {
  const profile = useQuery({ queryKey: ['my', 'profile'], queryFn: () => apiFetch<unknown>('/my/profile') })
  const health = useQuery({ queryKey: ['my', 'health-info'], queryFn: () => apiFetch<unknown>('/my/health-info') })
  const cache = useQueryClient()
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [language, setLanguage] = useState('ar')
  const [healthInfo, setHealthInfo] = useState('')
  const [message, setMessage] = useState('')
  const saveProfile = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'ProfileUpdate'> = { name: name || stringField(profile.data, 'name'), phone: phone || stringField(profile.data, 'phone'), language: language || stringField(profile.data, 'language') }; try { await apiFetch<void>('/my/profile', { method: 'PUT', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['my', 'profile'] }); setMessage("\u062A\u0645 \u062D\u0641\u0638 \u0627\u0644\u0645\u0644\u0641.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u0645\u0644\u0641.") } }
  const saveHealth = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'HealthInfoUpdate'> = { healthInfo }; try { await apiFetch<void>('/my/health-info', { method: 'PUT', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['my', 'health-info'] }); setMessage("\u062A\u0645 \u062D\u0641\u0638 \u0627\u0644\u0645\u0639\u0644\u0648\u0645\u0627\u062A \u0627\u0644\u0635\u062D\u064A\u0629.") } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u0645\u0639\u0644\u0648\u0645\u0627\u062A \u0627\u0644\u0635\u062D\u064A\u0629.") } }
  return <Page title={'\u0645\u0644\u0641\u064A \u0627\u0644\u0634\u062E\u0635\u064A \u0648\u0627\u0644\u0635\u062D\u064A'} subtitle={'\u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u0639\u0645\u064A\u0644 \u0627\u0644\u0635\u062D\u064A\u0629 \u0644\u0627 \u062A\u0638\u0647\u0631 \u0641\u064A \u0627\u0644\u0642\u0648\u0627\u0626\u0645'}><section className="panel"><h2>ط§ظ„ظ…ظ„ظپ</h2>{profile.isLoading ? <Loading /> : profile.isError ? <ErrorState error={profile.error} /> : <JsonPanel value={profile.data} />}<form onSubmit={event => void saveProfile(event)}><label>ط§ظ„ط§ط³ظ…<input value={name} onChange={event => setName(event.target.value)} /></label><label>ط§ظ„ظ‡ط§طھظپ<input value={phone} onChange={event => setPhone(event.target.value)} /></label><label>ط§ظ„ظ„ط؛ط©<input value={language} onChange={event => setLanguage(event.target.value)} /></label><button>ط­ظپط¸ ط§ظ„ظ…ظ„ظپ</button></form></section><section className="panel"><h2>ط§ظ„ظ…ط¹ظ„ظˆظ…ط§طھ ط§ظ„طµط­ï؟½? ط©</h2>{health.isLoading ? <Loading /> : health.isError ? <ErrorState error={health.error} /> : <JsonPanel value={health.data} />}<form onSubmit={event => void saveHealth(event)}><label>ط§ظ„طھط­ط¯ï؟½? ط«<textarea value={healthInfo} onChange={event => setHealthInfo(event.target.value)} required /></label><button>ط­ظپط¸ ط§ظ„ظ…ط¹ظ„ظˆظ…ط§طھ ط§ظ„طµط­ï؟½? ط©</button></form>{message && <p>{message}</p>}</section></Page>
}
function CheckInsPage() {
  const cache = useQueryClient()
  const query = useQuery({ queryKey: ['my', 'checkins'], queryFn: () => apiFetch<unknown>('/my/checkins') })
  const [weight, setWeight] = useState('')
  const [measurements, setMeasurements] = useState('')
  const [message, setMessage] = useState('')
  const submit = async (event: React.FormEvent<HTMLFormElement>) => { event.preventDefault(); setMessage(''); const form = new FormData(); form.append('Weight', weight); form.append('Measurements', measurements); const photos = Array.from((event.currentTarget.elements.namedItem('photos') as HTMLInputElement | null)?.files ?? []).slice(0, 4); try { for (const photo of photos) { const data = new FormData(); data.append('File', photo); data.append('Purpose', 'checkin-photo'); const uploaded = await uploadFile<unknown>('/files/upload', data); const id = responseId(uploaded); if (!id) throw new Error("\u0627\u0633\u062A\u062C\u0627\u0628\u0629 \u0631\u0641\u0639 \u0627\u0644\u0635\u0648\u0631\u0629 \u0644\u0627 \u062A\u062D\u062A\u0648\u064A \u0645\u0639\u0631\u0651\u0641 \u0627\u0644\u0645\u0644\u0641."); form.append('PhotoFileIds', id) } await uploadFile<unknown>('/my/checkins', form); setWeight(''); setMeasurements(''); setMessage("\u062A\u0645 \u0625\u0631\u0633\u0627\u0644 Check-in."); await cache.invalidateQueries({ queryKey: ['my', 'checkins'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u0625\u0631\u0633\u0627\u0644 Check-in.") } }
  return <Page title="Check-ins" subtitle={'\u062A\u0631\u0641\u0639 \u0627\u0644\u0635\u0648\u0631 \u0625\u0644\u0649 Files API \u0642\u0628\u0644 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u0633\u062C\u0644'}><section className="panel"><form onSubmit={event => void submit(event)}><label>ط§ظ„ظˆط²ï؟½? <input type="number" min="1" max="500" step="0.1" value={weight} onChange={event => setWeight(event.target.value)} required /></label><label>ط§ظ„ظ‚ï؟½? ط§ط³ط§طھ<textarea value={measurements} onChange={event => setMeasurements(event.target.value)} /></label><label>طµظˆط± (ط­طھظ‰ 4)<input name="photos" type="file" accept="image/jpeg,image/png,image/webp" multiple /></label><button>ط¥ط±ط³ط§ظ„ Check-in</button></form>{message && <p>{message}</p>}</section>{query.isLoading ? <Loading /> : query.isError ? <ErrorState error={query.error} /> : <JsonPanel value={query.data} />}</Page>
}
function ClientSubscriptionPage() {
  const {t,language}=useLanguage(); const cache=useQueryClient()
  const [planId,setPlanId]=useState(''); const [subscriptionId,setSubscriptionId]=useState(''); const [receiptFileId,setReceiptFileId]=useState(''); const [referenceNumber,setReferenceNumber]=useState(''); const [amount,setAmount]=useState(''); const [message,setMessage]=useState(''); const [creating,setCreating]=useState(false); const [submitting,setSubmitting]=useState(false)
  const plans=useQuery({queryKey:['public','plans'],queryFn:()=>apiFetch<unknown>('/plans')})
  const history=useQuery({queryKey:['my','subscriptions'],queryFn:()=>apiFetch<unknown>('/my/subscriptions')})
  const currentQuery=useQuery({queryKey:['my','subscription'],queryFn:()=>apiFetch<unknown>('/my/subscription')})
  const payments=useQuery({queryKey:['my','payments'],queryFn:()=>apiFetch<unknown>('/my/payments?page=1&pageSize=20')})
  const instructions=useQuery({queryKey:['payments','instapay-instructions'],queryFn:()=>apiFetch<unknown>('/payments/instapay/instructions')})
  const rows=objectItems(plans.data); const selected=rows.find(x=>String(x.id??'')===planId); const paymentsRows=objectItems(payments.data)
  const pendingSubscription=objectItems(history.data).find(x=>String(x.planId??'')===planId&&String(x.status??'').toLowerCase()==='pending')
  const pending=Boolean(pendingSubscription)
  const money=(value:unknown,currency:unknown)=>{const code=typeof currency==='string'&&/^[A-Z]{3}$/.test(currency)?currency:'EGP';return new Intl.NumberFormat(language==='ar'?'ar-EG':'en-US',{style:'currency',currency:code}).format(Number(value??0))}
  const create=async()=>{if(!planId)return;setCreating(true);setMessage('');try{const result=await apiPost<unknown>('/my/subscriptions',{planId} satisfies Schema<'CreateSubscriptionRequest'>,true);setSubscriptionId(responseId(result)??'');setAmount(String(selected?.price??''));setMessage(t('Your request is ready. Choose a payment method below.'));await cache.invalidateQueries({queryKey:['my','subscriptions']})}catch(e){setMessage(e instanceof Error?e.message:t('Could not start the subscription.'))}finally{setCreating(false)}}
  const uploadReceipt=async(file?:File)=>{if(!file)return;const form=new FormData();form.append('File',file);form.append('Purpose','receipt');try{const result=await uploadFile<unknown>('/files/upload',form);setReceiptFileId(responseId(result)??'');setMessage(t('Receipt uploaded.'))}catch(e){setMessage(e instanceof Error?e.message:t('Could not upload the receipt.'))}}
  const submit=async(event:React.FormEvent)=>{event.preventDefault();setSubmitting(true);setMessage('');try{await apiPost('/payments/instapay/submit',{subscriptionId,receiptFileId,referenceNumber:referenceNumber.trim(),amount:Number(amount)} satisfies Schema<'InstaPaySubmitRequest'>,true);setMessage(t('Transfer submitted. The club will review it before activating your subscription.'));setSubscriptionId('');setReceiptFileId('');setReferenceNumber('');await cache.invalidateQueries({queryKey:['my','payments']});await cache.invalidateQueries({queryKey:['my','subscriptions']})}catch(e){setMessage(e instanceof Error?e.message:t('Could not submit the transfer.'))}finally{setSubmitting(false)}}
  const current=currentQuery.data&&typeof currentQuery.data==='object'?currentQuery.data as Record<string,unknown>:null
  return <Page title={t('Subscriptions & payments')} subtitle={t('Choose a plan, submit payment, then follow its review status.')}>
    <section className="panel subscription-flow-panel"><div className="subscription-step-heading"><span>1</span><div><h2>{t('Choose a plan')}</h2><p>{t('Compare the active plans offered by your club.')}</p></div></div>
      {plans.isLoading?<Loading/>:plans.isError?<ErrorState error={plans.error}/>:rows.length===0?<div className="empty-state subscription-empty"><strong>{t('No active plans are available')}</strong><p>{t('Ask your coach or club owner to publish a plan before you request a subscription.')}</p></div>:<div className="client-plan-options">{rows.map((plan,index)=>{const id=String(plan.id??'');const features=Array.isArray(plan.features)?plan.features.filter((x):x is string=>typeof x==='string'):[];return <button type="button" key={id||index} className={'client-plan-option'+(id===planId?' selected':'')} aria-pressed={id===planId} onClick={()=>{const existing=objectItems(history.data).find(x=>String(x.planId??'')===id&&String(x.status??'').toLowerCase()==='pending');setPlanId(id);setSubscriptionId(existing?String(existing.id??''):'');setAmount(String(existing?.snapshotPrice??plan.price??''));setMessage('')}}><span className="client-plan-radio" aria-hidden="true">{id===planId?'Selected':''}</span><strong>{String(plan.name??t('Training plan'))}</strong><span className="client-plan-price">{money(plan.price,plan.currency)}</span><small>{String(plan.durationDays??'?')} {t('days')}</small>{features.length>0&&<span className="client-plan-features">{features.slice(0,3).join(' | ')}</span>}</button>})}</div>}
      <button type="button" className="subscription-primary-action" disabled={!planId||creating||Boolean(subscriptionId)} onClick={()=>void create()}>{creating?t('Creating request...'):subscriptionId?t('Request ready'):t('Continue to payment')}</button>{pending&&<p className="subscription-inline-note">{t('You already have a pending request for this plan. Continue below to pay without creating another request.')}</p>}
    </section>
    <section className="panel subscription-flow-panel"><div className="subscription-step-heading"><span>2</span><div><h2>{t('Current subscription')}</h2><p>{t('Your plan becomes active after the payment is confirmed.')}</p></div></div>{currentQuery.isLoading?<Loading/>:currentQuery.isError&&!(currentQuery.error instanceof ApiError&&currentQuery.error.status===404)?<ErrorState error={currentQuery.error}/>:current?<div className="subscription-current-card"><div><strong>{String(current.planName??t('Training plan'))}</strong><span className={'subscription-status status-'+String(current.status??'').toLowerCase()}>{t(String(current.status??'Active'))}</span></div><div className="subscription-summary"><div><small>{t('Status')}</small><strong>{t(String(current.status??'Active'))}</strong></div><div><small>{t('Started')}</small><strong><DataValue value={current.startDate}/></strong></div><div><small>{t('Renews or ends')}</small><strong><DataValue value={current.endDate}/></strong></div></div></div>:<div className="empty-state subscription-empty"><strong>{t('No active subscription')}</strong><p>{t('Choose a plan above to get started. A request remains pending until your payment is confirmed.')}</p></div>}</section>
    {subscriptionId&&selected&&<section className="panel subscription-flow-panel subscription-payment-step"><div className="subscription-step-heading"><span>3</span><div><h2>{t('Choose how to pay')}</h2><p>{String(selected.name??t('Training plan'))} - {money(amount,selected.currency)}</p></div></div>
      <div className="subscription-payment-choice"><div><strong>{t('Pay online with Paymob')}</strong><p>{t('Continue to the secure checkout. Your subscription activates after the server confirms payment.')}</p></div><PaymobCheckoutAction planId={planId} disabled={!planId}/></div>
      <div className="subscription-payment-choice"><div><strong>{t('Pay by InstaPay')}</strong><p>{t('Transfer the exact plan amount, then upload the receipt for club review.')}</p></div>{instructions.isLoading?<Loading/>:instructions.isError&&instructions.error instanceof ApiError&&instructions.error.status===409?<span className="payment-unavailable">{t('InstaPay is not enabled by this club.')}</span>:instructions.isError?<ErrorState error={instructions.error}/>:<div className="instapay-instructions"><DataValue value={instructions.data}/><form onSubmit={event=>void submit(event)} className="instapay-submit-form"><label>{t('Transfer reference')}<input value={referenceNumber} onChange={e=>setReferenceNumber(e.target.value)} required/></label><label>{t('Amount')}<input type="number" min="0.01" step="0.01" value={amount} onChange={e=>setAmount(e.target.value)} required/></label><label>{t('Receipt')}<input type="file" accept="image/jpeg,image/png,image/webp,application/pdf" onChange={e=>void uploadReceipt(e.target.files?.[0])} required/></label><small>{receiptFileId?t('Receipt uploaded.'):t('Upload a screenshot or PDF of your transfer.')}</small><button disabled={submitting||!receiptFileId}>{submitting?t('Submitting?'):t('Submit transfer for review')}</button></form></div>}</div>
    </section>}
    {message&&<p role="status" className="form-feedback subscription-page-feedback">{message}</p>}
    <section className="panel subscription-flow-panel"><div className="subscription-step-heading"><span>4</span><div><h2>{t('Payment history')}</h2><p>{t('See payment status and download invoices for confirmed payments.')}</p></div></div>{payments.isLoading?<Loading/>:payments.isError?<ErrorState error={payments.error}/>:paymentsRows.length===0?<div className="empty-state subscription-empty">{t('No payments yet. Payments you submit will appear here.')}</div>:<div className="payment-history-list">{paymentsRows.map((payment,index)=><article className="payment-history-item" key={String(payment.id??index)}><div><strong>{String(payment.subscriptionId??t('Subscription payment'))}</strong><small><DataValue value={payment.createdAt??payment.createdAtUtc}/> ? {t(String(payment.method??''))}</small></div><strong>{money(payment.amount,payment.currency)}</strong><span className={'subscription-status status-'+String(payment.status??'').toLowerCase()}>{t(String(payment.status??'Pending'))}</span></article>)}</div>}</section>
  </Page>
}
function WorkoutLogsPage() {
  const cache = useQueryClient()
  const logs = useQuery({ queryKey: ['my', 'workout-logs'], queryFn: () => apiFetch<unknown>('/my/workout-logs?page=1&pageSize=20') })
  const program = useMyProgram()
  const programExerciseOptions = { items: objectItems(program.data && typeof program.data === 'object' ? (program.data as Record<string, unknown>).exercises : undefined).map(item => ({ programExerciseId: item.programExerciseId ?? item.id, exerciseName: item.exerciseName ?? item.name, sets: item.sets, reps: item.reps })) }
  const [programExerciseId, setProgramExerciseId] = useState('')
  const [setNumber, setSetNumber] = useState('1')
  const [reps, setReps] = useState('10')
  const [weightKg, setWeightKg] = useState('')
  const [performedAt, setPerformedAt] = useState(new Date().toISOString().slice(0, 16))
  const [logId, setLogId] = useState('')
  const [message, setMessage] = useState('')
  const add = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'WorkoutLogRequest'> = { programExerciseId, setNumber: Number(setNumber), reps: Number(reps), weightKg: Number(weightKg), performedAt: new Date(performedAt).toISOString() }; try { await apiFetch<unknown>('/my/workout-logs', { method: 'POST', body: JSON.stringify(body) }); setMessage("\u062A\u0645 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629."); await cache.invalidateQueries({ queryKey: ['my', 'workout-logs'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.") } }
  const update = async (event: React.FormEvent) => { event.preventDefault(); const body: Schema<'WorkoutLogUpdate'> = { reps: Number(reps), weightKg: Number(weightKg) }; try { await apiFetch<void>(`/my/workout-logs/${logId}`, { method: 'PUT', body: JSON.stringify(body) }); setMessage("\u062A\u0645 \u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u0633\u062C\u0644."); await cache.invalidateQueries({ queryKey: ['my', 'workout-logs'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u0633\u062C\u0644.") } }
  const remove = async () => { try { await apiFetch<void>(`/my/workout-logs/${logId}`, { method: 'DELETE' }); setMessage("\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u0633\u062C\u0644."); await cache.invalidateQueries({ queryKey: ['my', 'workout-logs'] }) } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u0633\u062C\u0644.") } }
  return <Page title={'\u0633\u062C\u0644 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646'} subtitle={'\u0633\u062C\u0651\u0644 \u0623\u062F\u0627\u0621\u0643 \u0648\u062A\u0627\u0628\u0639 \u0645\u062C\u0645\u0648\u0639\u0627\u062A\u0643 \u0627\u0644\u0633\u0627\u0628\u0642\u0629'}><section className="panel"><form onSubmit={event => void add(event)}><EntitySelect label={'\u0627\u062E\u062A\u0631 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0645\u0646 \u0628\u0631\u0646\u0627\u0645\u062C\u0643 \u0627\u0644\u0646\u0634\u0637'} value={programExerciseId} onChange={setProgramExerciseId} data={programExerciseOptions} /><label>ط±ظ‚ظ… ط§ظ„ظ…ط¬ظ…ظˆط¹ط©<input type="number" min="1" max="20" value={setNumber} onChange={event => setSetNumber(event.target.value)} /></label><label>ط§ظ„طھظƒط±ط§ط±ط§طھ<input type="number" min="1" value={reps} onChange={event => setReps(event.target.value)} /></label><label>ط§ظ„ظˆط²ï؟½?  ط¨ط§ظ„ظƒط¬ظ…<input type="number" min="0" max="1000" step="0.1" value={weightKg} onChange={event => setWeightKg(event.target.value)} /></label><label>ظˆظ‚طھ ط§ظ„ط£ط¯ط§ط،<input type="datetime-local" value={performedAt} onChange={event => setPerformedAt(event.target.value)} /></label><button disabled={!programExerciseId}>طھط³ط¬ï؟½? ظ„ ط§ظ„ظ…ط¬ظ…ظˆط¹ط©</button></form><form onSubmit={event => void update(event)}><EntitySelect label={'\u0627\u062E\u062A\u0631 \u0633\u062C\u0644\u0627\u064B \u0644\u062A\u0639\u062F\u064A\u0644\u0647'} value={logId} onChange={setLogId} data={logs.data} /><button disabled={!logId}>طھط­ط¯ï؟½? ط« ط§ظ„ط³ط¬ظ„</button><button type="button" className="danger" onClick={() => void remove()} disabled={!logId}>ط­ط°ظپ ط§ظ„ط³ط¬ظ„</button></form>{message && <p>{message}</p>}</section>{logs.isLoading ? <Loading /> : logs.isError ? <ErrorState error={logs.error} /> : <JsonPanel value={logs.data} />}</Page>
}
function ChatAttachment({ id }: { id: string }) {
  const query = useQuery({ queryKey: ['file-preview', id], queryFn: () => downloadFile('/files/' + id), staleTime: 5 * 60_000 })
  const [url, setUrl] = useState('')
  useEffect(() => {
    if (!query.data) return
    const objectUrl = URL.createObjectURL(query.data.blob)
    setUrl(objectUrl)
    return () => URL.revokeObjectURL(objectUrl)
  }, [query.data])
  if (query.isLoading) return <span className="chat-file-state">Loading attachment&</span>
  if (query.isError || !query.data || !url) return <span className="chat-file-state">{'Could not load attachment.'}</span>
  const type = query.data.blob.type
  if (type.startsWith('image/')) return <a className="chat-media-link" href={url} target="_blank" rel="noreferrer"><img className="chat-image-attachment" src={url} alt={query.data.filename} loading="lazy" /></a>
  if (type.startsWith('video/')) return <video className="chat-video-attachment" src={url} controls preload="metadata" />
  return <a className="chat-file-download" href={url} download={query.data.filename}><span aria-hidden="true">&#8595;</span>{query.data.filename}</a>
}
function ChatTranscript({ value, ownUserId }: { value: unknown; ownUserId?: string }) {
  const { t } = useLanguage()
  const rows = objectItems(value)
  const endRef = useRef<HTMLDivElement>(null)
  useEffect(() => { endRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' }) }, [rows.length])
  if (rows.length === 0) return <div className="chat-empty"><span aria-hidden="true">&#9993;</span><strong>{t('No messages yet')}</strong><small>{t('Send a message to start the conversation.')}</small></div>
  return <div className="chat-transcript" role="log" aria-label={t('Conversation history')}>
    {rows.map((row, index) => {
      const mine = Boolean(ownUserId && row.senderId === ownUserId)
      const timestamp = typeof row.timestamp === 'string' ? new Date(row.timestamp) : null
      const fileIds = Array.isArray(row.fileIds) ? row.fileIds.filter((id): id is string => typeof id === 'string') : []
      return <article key={stringField(row, 'id') ?? index} className={'chat-bubble ' + (mine ? 'mine' : 'theirs')}>
        <p>{typeof row.content === 'string' && row.content ? row.content : fileIds.length ? t('Attachment') : t('Message deleted')}</p>
        {fileIds.length > 0 && <div className="chat-attachments">{fileIds.map(id => <ChatAttachment key={id} id={id} />)}</div>}
      <footer>{timestamp && !Number.isNaN(timestamp.getTime()) ? new Intl.DateTimeFormat(document.documentElement.lang === 'ar' ? 'ar-EG' : 'en-US', { hour: '2-digit', minute: '2-digit', day: 'numeric', month: 'short' }).format(timestamp) : ''}{mine && Boolean(row.isRead) && <span> ط£â€ڑ |  {t('Read')}</span>}</footer>
      </article>
    })}
    <div ref={endRef} />
  </div>
}
function ClientMessages() {
  const cache = useQueryClient()
  const me = useMe()
  const coach = useQuery({ queryKey: ['my', 'coach'], queryFn: () => apiFetch<unknown>('/my/coach') })
  const messages = useQuery({ queryKey: ['my', 'messages'], queryFn: () => apiFetch<unknown>('/my/messages?limit=100') })
  const coachRecord = coach.data && typeof coach.data === 'object' ? coach.data as Record<string, unknown> : null
  const coachId = responseId(coach.data)
  const { t, language } = useLanguage()
  const [content, setContent] = useState('')
  const [attachments, setAttachments] = useState<File[]>([])
  const [sending, setSending] = useState(false)
  const [message, setMessage] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const [coachOnline, setCoachOnline] = useState<boolean | null>(null)
  const [hubConnected, setHubConnected] = useState(false)
  const chatConnection = useRef<import('@microsoft/signalr').HubConnection | null>(null)
  useEffect(() => {
    if (!coachId || !accessToken()) return
    const connection = new HubConnectionBuilder().withUrl('/hubs/chat', { accessTokenFactory: () => accessToken() || '' }).withAutomaticReconnect().configureLogging(LogLevel.Warning).build()
    const invalidate = () => void cache.invalidateQueries({ queryKey: ['my', 'messages'] })
    connection.on('ReceiveMessage', invalidate)
    connection.on('MessageRead', invalidate)
    let typingTimer: ReturnType<typeof setTimeout> | undefined
    connection.on('UserTyping', payload => {
      if (payload && typeof payload === 'object' && 'userId' in payload && payload.userId === coachId) {
        setIsTyping(true)
        if (typingTimer) clearTimeout(typingTimer)
        typingTimer = setTimeout(() => setIsTyping(false), 2500)
      }
    })
    connection.on('PresenceChanged', payload => {
      if (payload && typeof payload === 'object' && 'userId' in payload && payload.userId === coachId && 'isOnline' in payload && typeof payload.isOnline === 'boolean') setCoachOnline(payload.isOnline)
    })
    chatConnection.current = connection
    void connection.start().then(() => { setHubConnected(true); return connection.invoke('JoinConversation', coachId) }).catch(() => setHubConnected(false))
    connection.onreconnected(() => { setHubConnected(true); void connection.invoke('JoinConversation', coachId).catch(() => undefined) })
    connection.onclose(() => setHubConnected(false))
    return () => { if (typingTimer) clearTimeout(typingTimer); chatConnection.current = null; void connection.stop() }
  }, [cache, coachId])
  const send = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!content.trim() && attachments.length === 0) { setMessage(t('Write a message or attach a file.')); return }
    if (attachments.length > 10) { setMessage(t('You can attach up to 10 files per message.')); return }
    const tooLarge = attachments.find(file => file.size > (file.type.startsWith('video/') ? 100 : 10) * 1024 * 1024)
    if (tooLarge) { setMessage(t('File is too large. Videos can be up to 100 MB; other files up to 10 MB.')); return }
    const formElement = event.currentTarget
    setSending(true)
    try {
      const fileIds: string[] = []
      for (const file of attachments) {
        const form = new FormData(); form.append('File', file); form.append('Purpose', 'attachment')
        const uploaded = await uploadFile<unknown>('/files/upload', form)
        const id = responseId(uploaded)
        if (!id) throw new Error(t('The uploaded file could not be identified.'))
        fileIds.push(id)
      }
      const body: Schema<'SendMessageRequest'> = { content: content.trim() || null, fileIds }
      await apiFetch<unknown>('/my/messages', { method: 'POST', body: JSON.stringify(body) })
      setContent(''); setAttachments([]); formElement.reset(); setMessage(t('Message sent.')); await cache.invalidateQueries({ queryKey: ['my', 'messages'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : t('Could not send the message.')) }
    finally { setSending(false) }
  }
  const markRead = async () => {
    const latest = [...objectItems(messages.data)].reverse().find(item => item.receiverId === me.data?.id && typeof item.id === 'string')
    if (!latest || typeof latest.id !== 'string' || !coachId) { setMessage("\u0644\u0627 \u062A\u0648\u062C\u062F \u0631\u0633\u0627\u0626\u0644 \u0648\u0627\u0631\u062F\u0629 \u062C\u062F\u064A\u062F\u0629."); return }
    try {
      if (chatConnection.current?.state === 'Connected') await chatConnection.current.invoke('MarkAsRead', coachId, latest.id)
      else await apiFetch<void>('/my/messages/read', { method: 'PUT', body: JSON.stringify({ lastMessageId: latest.id } satisfies Schema<'ReadMessageRequest'>) })
      await cache.invalidateQueries({ queryKey: ['my', 'messages'] }); setMessage("\u062A\u0645 \u062A\u062D\u062F\u064A\u062F \u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u0627\u0644\u0648\u0627\u0631\u062F\u0629 \u0643\u0645\u0642\u0631\u0648\u0621\u0629.")
    } catch (error) { setMessage(error instanceof Error ? error.message : "\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u062D\u0627\u0644\u0629 \u0627\u0644\u0642\u0631\u0627\u0621\u0629.") }
  }
  const status = !coachId ? t('No coach assigned') : coachOnline === null ? (hubConnected ? t('Presence unknown') : t('Connecting&')) : coachOnline ? t('Online now') : t('Offline')
  return <Page title={t('Messages')} subtitle={t('Talk privately with your assigned coach.')}>
    <section dir={language === 'ar' ? 'rtl' : 'ltr'} className="panel chat-panel client-chat-panel">
      <header className="chat-partner"><div className="chat-avatar">{String(coachRecord?.name ?? t('Coach')).slice(0, 1)}</div><div><strong>{String(coachRecord?.name ?? t('Your coach'))}</strong><small>{typeof coachRecord?.specialty === 'string' ? coachRecord.specialty : t('Your assigned coach')}</small></div><span className={'chat-presence ' + (coachOnline ? 'online' : '')}><i />{status}</span></header>
      {coach.isError && !(coach.error instanceof ApiError && coach.error.status === 404) && <ErrorState error={coach.error} />}
      {!coachId && <div className="empty-state callout"><strong>{t('No coach assigned')}</strong><p>{t('Your club will connect you with a coach. Messaging becomes available after assignment.')}</p></div>}
      {coachId && <><div className="chat-message-heading"><h2>{t('Conversation history')}</h2>{isTyping && <span className="typing-indicator">{t('Coach is typing&')}</span>}</div>
      {messages.isLoading ? <Loading /> : messages.isError ? <ErrorState error={messages.error} /> : <ChatTranscript value={messages.data} ownUserId={me.data?.id} />}
      <button className="chat-read-button" type="button" onClick={() => void markRead()} disabled={messages.isLoading || messages.isError}>{t('Mark as read')}</button>
      {attachments.length > 0 && <div className="chat-selected-files" aria-live="polite">{attachments.map((file,index) => <span className="chat-selected-file" key={file.name + index}><span aria-hidden="true">{String.fromCharCode(file.type.startsWith('image/') ? 9639 : file.type.startsWith('video/') ? 9654 : 8613)}</span><span title={file.name}>{file.name}</span><button type="button" onClick={() => setAttachments(current => current.filter((_,itemIndex) => itemIndex !== index))} aria-label={t('Remove attachment')}>&times;</button></span>)}</div>}
      <form className="chat-compose client-chat-compose" onSubmit={event => void send(event)}><label>{t('Message')}<textarea value={content} onChange={event => { setContent(event.target.value); if (chatConnection.current?.state === 'Connected') void chatConnection.current.invoke('Typing', coachId).catch(() => undefined) }} maxLength={2000} rows={2} placeholder={t('Write to') + ' ' + String(coachRecord?.name ?? t('your coach'))} /></label><label className="chat-file-label"><span aria-hidden="true">&#43;</span> {t('Attach photos, videos or files')}<input name="attachments" type="file" multiple accept="image/jpeg,image/png,image/webp,video/mp4,video/webm,.pdf,.doc,.docx" onChange={event => setAttachments(Array.from(event.target.files ?? []).slice(0,10))} /></label><button disabled={sending || (!content.trim() && attachments.length===0)}>{sending ? t('Sending&') : t('Send message')}</button></form></>}
      {message && <p className="chat-feedback" role="status">{message}</p>}
    </section>
  </Page>
}function TenantSettings() {
  const me = useMe(); const cache = useQueryClient(); const tenant = useQuery({ queryKey: ['tenant'], queryFn: () => apiFetch<unknown>('/tenant') }); const usage = useQuery({ queryKey: ['tenant', 'usage'], queryFn: () => apiFetch<unknown>('/tenant/usage') }); const payments = useQuery({ queryKey: ['tenant', 'payment-settings'], queryFn: () => apiFetch<unknown>('/tenant/payment-settings') })
  const [currency, setCurrency] = useState('EGP'); const [timeZone, setTimeZone] = useState('Africa/Cairo'); const [reminderDays, setReminderDays] = useState(7); const [defaultLanguage, setDefaultLanguage] = useState('ar')
  const [name, setName] = useState(''); const [logoUrl, setLogoUrl] = useState(''); const [primaryColor, setPrimaryColor] = useState('#17324d'); const [secondaryColor, setSecondaryColor] = useState('#22a6a1'); const [paymobEnabled, setPaymobEnabled] = useState(false); const [instaPayEnabled, setInstaPayEnabled] = useState(false); const [instaPayId, setInstaPayId] = useState(''); const [instaPayAccountNumber, setInstaPayAccountNumber] = useState(''); const [message, setMessage] = useState('')
  useEffect(() => { if (tenant.data && typeof tenant.data === 'object') { const row = tenant.data as Record<string, unknown>; setCurrency(String(row.currency ?? 'EGP')); setTimeZone(String(row.timeZone ?? 'Africa/Cairo')); setReminderDays(Number(row.reminderDays ?? 7)); setDefaultLanguage(String(row.defaultLanguage ?? 'ar')); setName(String(row.name ?? '')); setLogoUrl(String(row.logoUrl ?? '')); setPrimaryColor(String(row.primaryColor ?? '#17324d')); setSecondaryColor(String(row.secondaryColor ?? '#22a6a1')) } }, [tenant.data])
  useEffect(() => { if (payments.data && typeof payments.data === 'object') { const row = payments.data as Record<string, unknown>; setPaymobEnabled(row.paymobEnabled === true); setInstaPayEnabled(row.instaPayEnabled === true); setInstaPayId(String(row.instaPayId ?? '')); setInstaPayAccountNumber(String(row.instaPayAccountNumber ?? '')) } }, [payments.data])
  const saveSettings = async (event: React.FormEvent) => { event.preventDefault(); try { const body: Schema<'UpdateTenantSettingsRequest'> = { currency, timeZone, reminderDays, defaultLanguage }; await apiFetch<void>('/tenant/settings', { method: 'PUT', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['tenant'] }); setMessage('Workspace settings saved.') } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save workspace settings.') } }
  const savePaymentSettings = async (event: React.FormEvent) => { event.preventDefault(); try { const body: Schema<'UpdatePaymentSettingsRequest'> = { paymobEnabled, instaPayEnabled, instaPayId: instaPayId || null, instaPayAccountNumber: instaPayAccountNumber || null }; await apiFetch<void>('/tenant/payment-settings', { method: 'PUT', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['tenant', 'payment-settings'] }); setMessage('Payment settings saved.') } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save payment settings.') } }
  const saveBranding = async (event: React.FormEvent) => { event.preventDefault(); try { const body: Schema<'UpdateBrandingRequest'> = { name, logoUrl: logoUrl || null, primaryColor: primaryColor || null, secondaryColor: secondaryColor || null }; await apiFetch<void>('/tenant/branding', { method: 'PUT', body: JSON.stringify(body) }); await cache.invalidateQueries({ queryKey: ['tenant'] }); setMessage('Workspace branding saved.') } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not save workspace branding.') } }
  if (me.data?.role !== 'TenantOwner') return <Page title="Workspace settings" subtitle="Only the workspace owner can change these settings."><ErrorState error={new Error('Only the workspace owner can change these settings.')} /></Page>
  return <Page title="Workspace settings" subtitle="Manage workspace details, branding and payment methods.">
    <section className="panel"><h2>Workspace details</h2>{tenant.isLoading ? <Loading /> : tenant.isError ? <ErrorState error={tenant.error} /> : <form onSubmit={event => void saveSettings(event)} className="form-grid"><label>Currency<input value={currency} maxLength={3} onChange={event => setCurrency(event.target.value.toUpperCase())} required /></label><label>Time zone<input value={timeZone} onChange={event => setTimeZone(event.target.value)} placeholder="Africa/Cairo" required /></label><label>Subscription reminder days<input type="number" min={1} max={14} value={reminderDays} onChange={event => setReminderDays(Number(event.target.value))} required /></label><label>Default language<select value={defaultLanguage} onChange={event => setDefaultLanguage(event.target.value)}><option value="ar">Arabic</option><option value="en">English</option></select></label><button type="submit">Save settings</button></form>}</section>
    <section className="panel"><h2>Branding</h2><p>ظ‡ط°ظ‡ ط§ظ„ظ‡ظˆï؟½? ط© طھط¸ظ‡ط± ظ„ظ„ط¹ظ…ظ„ط§ط، ظپï؟½?  طµظپط­ط© ط§ظ„ظ…ط³ط§ط­ط©.</p>{tenant.isLoading ? <Loading /> : tenant.isError ? <ErrorState error={tenant.error} /> : <form onSubmit={event => void saveBranding(event)} className="form-grid"><label>ط§ط³ظ… ط§ظ„ï؟½? ط§ط¯ï؟½? <input value={name} onChange={event => setName(event.target.value)} required /></label><label>ط±ط§ط¨ط· ط§ظ„ط´ط¹ط§ط±<input type="url" value={logoUrl} onChange={event => setLogoUrl(event.target.value)} placeholder="https://..." /></label><label>ط§ظ„ظ„ظˆï؟½?  ط§ظ„ط£ط³ط§ط³ï؟½? <input type="color" value={primaryColor} onChange={event => setPrimaryColor(event.target.value)} /></label><label>ط§ظ„ظ„ظˆï؟½?  ط§ظ„ط«ط§ï؟½? ظˆï؟½? <input type="color" value={secondaryColor} onChange={event => setSecondaryColor(event.target.value)} /></label><button type="submit">ط­ظپط¸ ط§ظ„ظ‡ظˆï؟½? ط©</button></form>}</section>    <section className="panel"><h2>Payment methods</h2>{payments.isLoading ? <Loading /> : payments.isError ? <ErrorState error={payments.error} /> : <form onSubmit={event => void savePaymentSettings(event)} className="form-grid"><label className="check-field"><input type="checkbox" checked={paymobEnabled} onChange={event => setPaymobEnabled(event.target.checked)} />Enable Paymob</label><label className="check-field"><input type="checkbox" checked={instaPayEnabled} onChange={event => setInstaPayEnabled(event.target.checked)} />Enable InstaPay</label><label>InstaPay ID<input value={instaPayId} onChange={event => setInstaPayId(event.target.value)} placeholder="InstaPay username or email" /></label><label>Account number<input inputMode="numeric" value={instaPayAccountNumber} onChange={event => setInstaPayAccountNumber(event.target.value)} /></label><button type="submit">Save payment methods</button></form>}</section>
    <section className="panel"><h2>Workspace usage</h2>{me.data?.role === 'TenantOwner' && (usage.isLoading ? <Loading /> : usage.isError ? <ErrorState error={usage.error} /> : <JsonPanel value={usage.data} />)}</section>{message && <p role="status">{message}</p>}
  </Page>
}
function TenantAnalytics() {
  const analytics = useQuery({ queryKey: ['tenant', 'analytics'], queryFn: async () => Promise.all([
    apiFetch<unknown>('/tenant/dashboard/pending-actions'), apiFetch<unknown>('/tenant/dashboard/expiring-subscriptions'),
    apiFetch<unknown>('/tenant/dashboard/clients-activity'), apiFetch<unknown>('/tenant/dashboard/revenue'),
    apiFetch<unknown>('/tenant/analytics/retention'), apiFetch<unknown>('/tenant/analytics/plans-performance'),
    apiFetch<unknown>('/tenant/analytics/coach-performance'), apiFetch<unknown>('/tenant/dashboard/churn-risk'),
  ]) })
  if (analytics.isLoading) return <Loading />
  if (analytics.isError) return <ErrorState error={analytics.error} />
  const titles = ["\u0627\u0644\u0625\u062C\u0631\u0627\u0621\u0627\u062A \u0627\u0644\u0645\u0639\u0644\u0642\u0629", "\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0642\u0627\u0631\u0628\u062A \u0639\u0644\u0649 \u0627\u0644\u0627\u0646\u062A\u0647\u0627\u0621", "\u0646\u0634\u0627\u0637 \u0627\u0644\u0639\u0645\u0644\u0627\u0621", "\u0627\u0644\u0625\u064A\u0631\u0627\u062F\u0627\u062A", "\u0627\u0644\u0627\u062D\u062A\u0641\u0627\u0638", "\u0623\u062F\u0627\u0621 \u0627\u0644\u0628\u0627\u0642\u0627\u062A", "\u0623\u062F\u0627\u0621 \u0627\u0644\u0645\u062F\u0631\u0628\u064A\u0646", "\u0645\u062E\u0627\u0637\u0631 \u0627\u0644\u0627\u0646\u0633\u062D\u0627\u0628"]
  return <Page title={'\u0627\u0644\u062A\u062D\u0644\u064A\u0644\u0627\u062A'} subtitle={'\u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u0645\u0633\u0627\u062D\u0629 \u0627\u0644\u062D\u0627\u0644\u064A\u0629'}><div className="cards">{analytics.data!.map((value, index) => <section className="panel" key={titles[index]}><h2>{titles[index]}</h2><JsonPanel value={value} /></section>)}</div></Page>
}
function TenantAudit() {
  const [page,setPage]=useState(1);const [action,setAction]=useState('');const [entityType,setEntityType]=useState('');const [actorId,setActorId]=useState('')
  const params=new URLSearchParams({page:String(page),pageSize:'20'});if(action.trim())params.set('action',action.trim());if(entityType.trim())params.set('entityType',entityType.trim());if(actorId.trim())params.set('actorId',actorId.trim())
  const audit=useQuery({queryKey:['tenant','audit-logs',page,action,entityType,actorId],queryFn:()=>apiFetch<unknown>(`/tenant/audit-logs?${params}`)})
  const rows=objectItems(audit.data);const total=typeof audit.data==='object'&&audit.data&&'total'in audit.data&&typeof audit.data.total==='number'?audit.data.total:rows.length
  return <Page title="Audit log" subtitle="Read-only activity history. The server limits results to your workspace."><section className="panel audit-workspace"><form className="form-grid audit-filters" onSubmit={event=>{event.preventDefault();setPage(1);void audit.refetch()}}><label>Action<input value={action} onChange={event=>setAction(event.target.value)} placeholder="Example: client.status.changed"/></label><label>Entity type<input value={entityType} onChange={event=>setEntityType(event.target.value)} placeholder="Client or Payment"/></label><label>Actor ID<input value={actorId} onChange={event=>setActorId(event.target.value)} placeholder="Optional"/></label><button>Apply filters</button><button type="button" className="secondary-button" onClick={()=>{setAction('');setEntityType('');setActorId('');setPage(1)}}>Clear filters</button></form>{audit.isLoading?<Loading/>:audit.isError?<ErrorState error={audit.error}/>:rows.length===0?<div className="empty-state">No events match these filters.</div>:<div className="table-wrap"><table><thead><tr><th>Time</th><th>Action</th><th>Actor</th><th>Role</th><th>Entity</th><th>Record</th><th>IP address</th></tr></thead><tbody>{rows.map((row,index)=><tr key={String(row.id??index)}><td>{formatAdminValue(row.createdAt,'createdAt')}</td><td><code>{String(row.action??'â€”')}</code></td><td><code>{String(row.actorUserId??'â€”')}</code></td><td>{String(row.actorRole??'â€”')}</td><td>{String(row.entityType??'â€”')}</td><td><code>{String(row.entityId??'â€”')}</code></td><td>{String(row.ipAddress??'â€”')}</td></tr>)}</tbody></table></div>}<div className="actions audit-pagination"><button type="button" onClick={()=>setPage(value=>Math.max(1,value-1))} disabled={page<=1}>Previous</button><span>Page {page} ط£â€ڑ |  {total} events</span><button type="button" onClick={()=>setPage(value=>value+1)} disabled={page*20>=total}>Next</button></div></section></Page>
}function AiWorkspace() {
  const me = useMe()
  const { language } = useLanguage()
  const cache = useQueryClient()
  const clients = useClients()
  const generated = useQuery({ queryKey: ['ai', 'generated-programs'], queryFn: () => apiFetch<unknown>('/ai/generated-programs?page=1&pageSize=20') })
  const usage = useQuery({ queryKey: ['ai', 'usage'], queryFn: () => apiFetch<unknown>('/ai/usage'), enabled: me.data?.role === 'TenantOwner' })
  const [clientId, setClientId] = useState('')
  const [goal, setGoal] = useState('Strength')
  const [level, setLevel] = useState('Beginner')
  const [generatedId, setGeneratedId] = useState('')
  const [churnId, setChurnId] = useState('')
  const [churn, setChurn] = useState<unknown>()
  const [churnLoading, setChurnLoading] = useState(false)
  const [message, setMessage] = useState('')
  const draftRows = objectItems(generated.data)
  const usageData = usage.data && typeof usage.data === 'object' ? usage.data as Record<string, unknown> : null
  const churnData = churn && typeof churn === 'object' ? churn as Record<string, unknown> : null
  const generate = async (event: React.FormEvent) => {
    event.preventDefault()
    const body: Schema<'AiGenerateApiRequest'> = { clientId, goal, level, daysPerWeek: 3, sessionMinutes: 45, durationWeeks: 4, equipment: [], injuries: [], dietaryRestrictions: [], caloriesTarget: null, language, extraNotes: '' }
    try {
      const result = await apiPost<unknown>('/ai/generate-program', body)
      const id = responseId(result) ?? ''
      setGeneratedId(id)
      setMessage(id ? 'Draft created and saved. Find it in the drafts list below.' : 'The draft was created. Refresh the draft list if it does not appear yet.')
      await cache.invalidateQueries({ queryKey: ['ai', 'generated-programs'] })
      await cache.invalidateQueries({ queryKey: ['ai', 'usage'] })
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not generate the training program.') }
  }
  const getChurn = async () => {
    if (!churnId) return
    setChurnLoading(true)
    try {
      const result = await apiFetch<unknown>(`/ai/churn/clients/${churnId}`)
      setChurn(result)
      setMessage(result == null ? 'No score exists yet. The nightly job or a club owner recompute will create one.' : 'Latest churn score loaded.')
    } catch (error) { setChurn(undefined); setMessage(error instanceof Error ? error.message : 'Could not load the churn score.') }
    finally { setChurnLoading(false) }
  }
  const recompute = async () => {
    setChurnLoading(true)
    try {
      const result = await apiPost<unknown>('/ai/churn/recompute', {})
      await cache.invalidateQueries({ queryKey: ['ai', 'churn'] })
      const refreshed = churnId ? await apiFetch<unknown>(`/ai/churn/clients/${churnId}`) : undefined
      if (refreshed !== undefined) setChurn(refreshed)
      const scored = result && typeof result === 'object' && 'scored' in result ? String((result as Record<string, unknown>).scored) : null
      setMessage(scored === null ? 'Churn scores recomputed.' : `Churn scores recomputed for ${scored} clients.`)
    } catch (error) { setMessage(error instanceof Error ? error.message : 'Could not recompute churn scores.') }
    finally { setChurnLoading(false) }
  }
  return <Page title="AI coaching tools" subtitle="Generate training drafts and review client retention risk.">
    <section className="panel">
      <div className="section-heading"><div><h2>Generate a training program</h2><p>Programs are saved as drafts in CoachPulse; no file is downloaded.</p></div></div>
      <form className="form-grid" onSubmit={event => void generate(event)}>
        <EntitySelect label="Client" value={clientId} onChange={setClientId} data={clients.data} placeholder="Choose a client" />
        <label>Training goal<select value={goal} onChange={event => setGoal(event.target.value)}><option value="Strength">Build strength</option><option value="FatLoss">Lose weight</option><option value="MuscleGain">Build muscle</option><option value="GeneralFitness">General fitness</option><option value="Rehabilitation">Rehabilitation</option></select></label>
        <label>Experience level<select value={level} onChange={event => setLevel(event.target.value)}><option value="Beginner">Beginner</option><option value="Intermediate">Intermediate</option><option value="Advanced">Advanced</option></select></label>
        <button disabled={!clientId}>Generate draft</button>
      </form>
    </section>
    <section className="panel">
      <div className="section-heading"><div><h2>AI quota and saved drafts</h2><p>Drafts stay here until approved or rejected.</p></div></div>
      {usage.isLoading ? <Loading /> : usage.isError ? <ErrorState error={usage.error} /> : usageData ? <div className="admin-metric-grid">
        <article className="admin-metric"><span>Daily limit</span><strong>{String(usageData.dailyLimit ?? 'â€”')}</strong></article>
        <article className="admin-metric"><span>Used today</span><strong>{String(usageData.used ?? 'â€”')}</strong></article>
        <article className="admin-metric"><span>Remaining</span><strong>{String(usageData.remaining ?? 'â€”')}</strong></article>
        <article className="admin-metric"><span>Quota resets</span><strong>{usageData.resetsAt ? new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Africa/Cairo' }).format(new Date(String(usageData.resetsAt))) : 'â€”'}</strong></article>
      </div> : me.data?.role !== 'TenantOwner' ? <p>Quota details are available to the club owner.</p> : <p>Quota details are unavailable.</p>}
      {generated.isLoading ? <Loading /> : generated.isError ? <ErrorState error={generated.error} /> : draftRows.length === 0 ? <div className="empty-state">No drafts yet. Generate a program above to create one.</div> : <div className="table-wrap"><table><thead><tr><th>Client</th><th>Status</th><th>Prompt version</th><th>Model version</th><th>Review</th></tr></thead><tbody>{draftRows.map((draft, index) => {
        const id = String(draft.id ?? '')
        const client = objectItems(clients.data).find(row => String(row.id) === String(draft.clientId))
        return <tr key={id || index}><td>{String(client?.name ?? 'Client')}</td><td>{String(draft.status ?? 'Draft')}</td><td>{String(draft.promptVersion ?? 'â€”')}</td><td>{String(draft.modelVersion ?? 'â€”')}</td><td>{id && draft.status === 'Draft' ? <Link className="primary-button" to={`/app/ai-review?draft=${encodeURIComponent(id)}`}>Review draft</Link> : <span>Already reviewed</span>}</td></tr>
      })}</tbody></table></div>}
      {generatedId && <p>Most recent draft: {generatedId}</p>}
    </section>
    <section className="panel">
      <div className="section-heading"><div><h2>Client churn risk</h2><p>Scores are created by the nightly job or a manual recompute by the club owner.</p></div></div>
      <EntitySelect label="Client" value={churnId} onChange={value => { setChurnId(value); setChurn(undefined); setMessage('') }} data={clients.data} placeholder="Choose a client" />
      <div className="actions"><button onClick={() => void getChurn()} disabled={!churnId || churnLoading}>{churnLoading ? 'Loadingâ€¦' : 'Load latest score'}</button>{me.data?.role === 'TenantOwner' && <button className="secondary-button" onClick={() => void recompute()} disabled={churnLoading}>{churnLoading ? 'Recomputingâ€¦' : 'Recompute scores'}</button>}</div>
      {churn !== undefined && (churnData ? <div className="admin-metric-grid">
        <article className="admin-metric"><span>Risk level</span><strong>{String(churnData.riskLevel ?? 'â€”')}</strong></article>
        <article className="admin-metric"><span>Risk score</span><strong>{typeof churnData.riskScore === 'number' ? `${Math.round(churnData.riskScore * 100)}%` : 'â€”'}</strong></article>
        <article className="admin-metric"><span>Model version</span><strong>{String(churnData.modelVersion ?? 'â€”')}</strong></article>
        <article className="admin-metric"><span>Last calculated</span><strong>{churnData.computedAt ? new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(String(churnData.computedAt))) : 'â€”'}</strong></article>
        {Array.isArray(churnData.topFactors) && <div><h3>Contributing factors</h3><div className="data-chips">{churnData.topFactors.map((factor, index) => <span className="data-chip" key={index}>{typeof factor === 'string' ? factor : JSON.stringify(factor)}</span>)}</div></div>}
      </div> : <div className="empty-state">No churn score is available for this client yet.</div>)}
    </section>
    {message && <p role="status" className="inline-notice">{message}</p>}
  </Page>
}function MyDashboard() {
  const { t, language } = useLanguage()
  const query=useMyDashboard()
  if(query.isLoading)return <Loading />
  if(query.isError)return <ErrorState error={query.error} />
  const data=query.data&&typeof query.data==='object'?query.data as Record<string,unknown>:{}
  const program=data.program&&typeof data.program==='object'?data.program as Record<string,unknown>:null
  const workout=data.todayWorkout&&typeof data.todayWorkout==='object'?data.todayWorkout as Record<string,unknown>:null
  const subscription=data.subscription&&typeof data.subscription==='object'?data.subscription as Record<string,unknown>:null
  const workoutCount=objectItems(workout?.exercises).length
  const nextCheckIn=data.nextCheckIn
  const unread=Number(data.unreadMessages??0)
  return <Page title={t('My dashboard')} subtitle={t('A quick look at your training and account.')}><DashboardHero title={t('Your next workout starts here')} subtitle={t('Keep your plan close, stay in touch with your coach, and celebrate each step forward.')} image="/media/client-workout.jpg" />
    <div className="cards"><Metric label={t("Today's training")} value={workout?workout.restDay===true?t('Rest day'):`${workoutCount} ${t('exercises')}`:program?t('Rest day'):t('No program')} /><Metric label={t('Subscription')} value={t(String(subscription?.status??'Not active'))} /><Metric label={t('Unread messages')} value={unread} /><Metric label={t('Next check-in')} value={nextCheckIn?new Intl.DateTimeFormat(language==='ar'?'ar-EG':'en-US',{dateStyle:'medium'}).format(new Date(String(nextCheckIn))):t('Not scheduled')} /></div>
    <section className="panel dashboard-shortcuts"><div className="section-heading"><div><span className="eyebrow">Your workspace</span><h2>Quick access</h2></div></div><div className="dashboard-shortcut-grid"><Link to="/my/program">View today's training</Link><Link to="/my/checkins">Record a check-in</Link><Link to="/my/meal-plan">Open meal plan</Link><Link to="/my/messages">Message your coach</Link></div></section>
  </Page>
}
function MyProgram() {
  const today = useMyProgram()
  const history = useQuery({ queryKey: ['my', 'programs'], queryFn: () => apiFetch<unknown>('/my/programs?page=1&pageSize=20') })
  if (today.isLoading || history.isLoading) return <Loading />
  if (today.isError && !(today.error instanceof ApiError && today.error.status === 404)) return <ErrorState error={today.error} />
  if (history.isError) return <ErrorState error={history.error} />
  const todayData = today.data && typeof today.data === 'object' ? today.data as Record<string, unknown> : {}
  const programs = objectItems(history.data)
  const activeProgram = programs.find(item => String(item.status ?? '').toLowerCase() === 'active')
  const exercises = objectItems(todayData.exercises)
  return <Page title="My program" subtitle="Your current plan, today's workout and training history">
    <section className="panel client-plan-summary"><div className="section-heading"><div><span className="eyebrow">Training plan</span><h2>Current program</h2></div><span className={activeProgram ? 'status-pill positive' : 'status-pill'}>{activeProgram ? 'Active' : 'Not assigned'}</span></div>
      {activeProgram ? <><h3>{String(activeProgram.title ?? 'Training program')}</h3><div className="data-grid"><div className="data-field"><dt>Status</dt><dd>{String(activeProgram.status ?? 'Active')}</dd></div>{Boolean(activeProgram.startDate) && <div className="data-field"><dt>Start date</dt><dd><DataValue value={activeProgram.startDate} /></dd></div>}</div></> : <p className="empty-state">Your coach has not assigned a program yet. Your training plan will appear here once it is assigned.</p>}
    </section>
    <section className="panel"><div className="section-heading"><div><span className="eyebrow">{todayData.dayNumber ? 'Training day ' + String(todayData.dayNumber) : 'Today'}</span><h2>Today's exercises</h2></div></div>
      {exercises.length ? <div className="client-exercise-list">{exercises.map((exercise,index)=><article className="client-exercise-card" key={String(exercise.id ?? index)}><div><h3>{String(exercise.exerciseName ?? exercise.name ?? 'Exercise')}</h3><p>{String(exercise.muscleGroup ?? '')}</p></div><span>{String(exercise.sets ?? '?')} sets</span><span>{String(exercise.reps ?? '?')} reps</span></article>)}</div> : <div className="empty-state callout"><strong>{activeProgram ? 'Rest day' : 'No training plan yet'}</strong><p>{activeProgram ? 'There are no exercises scheduled today. Take time to recover.' : 'Your workout schedule will appear here after your coach assigns a program.'}</p></div>}
    </section>
    <section className="panel"><div className="section-heading"><div><span className="eyebrow">Training history</span><h2>Past and current programs</h2></div></div>{programs.length ? <div className="client-program-list">{programs.map((item,index)=><article className="client-program-card" key={String(item.id ?? index)}><div><h3>{String(item.title ?? 'Training program')}</h3><small>{item.startDate ? new Intl.DateTimeFormat('en',{dateStyle:'medium'}).format(new Date(String(item.startDate))) : 'Start date unavailable'}</small></div><span className={String(item.status).toLowerCase()==='active'?'status-pill positive':'status-pill'}>{String(item.status ?? '?')}</span></article>)}</div> : <p className="empty-state">No training history yet.</p>}</section>
  </Page>
}
function MyProgress() {
  const query=useQuery({queryKey:['my','progress'],queryFn:()=>apiFetch<unknown>('/my/progress')})
  if(query.isLoading)return <Loading />
  if(query.isError)return <ErrorState error={query.error} />
  const data=query.data&&typeof query.data==='object'?query.data as Record<string,unknown>:{}
  const count=Number(data.totalCheckIns??0)
  if(!count)return <Page title="My progress" subtitle="Your measurements and progress"><section className="panel client-empty-plan"><span className="empty-plan-icon" aria-hidden="true">+</span><h2>Your progress starts with a check-in</h2><p>After you submit your first check-in, your measurements and changes over time will appear here.</p><Link className="primary-link" to="/my/checkins">Create a check-in</Link></section></Page>
  return <Page title="My progress" subtitle="Your measurements and progress"><div className="cards"><Metric label="Check-ins" value={count}/><Metric label="Starting weight" value={data.firstWeight==null?'?':String(data.firstWeight)+' kg'}/><Metric label="Current weight" value={data.currentWeight==null?'?':String(data.currentWeight)+' kg'}/><Metric label="Change" value={data.weightChange==null?'?':String(data.weightChange)+' kg'}/></div><section className="panel"><h2>Check-in history</h2><JsonPanel value={data.checkIns}/></section></Page>
}
function MyMealPlan() {
  const query = useMyMealPlan()
  if (query.isLoading) return <Loading />
  if (query.isError && !(query.error instanceof ApiError && query.error.status === 404)) return <ErrorState error={query.error} />
  const plan = query.data && typeof query.data === 'object' ? query.data as Record<string,unknown> : null
  if (!plan) return <Page title="My meal plan" subtitle="Your meals and nutrition"><section className="panel client-empty-plan"><span className="empty-plan-icon" aria-hidden="true">+</span><h2>No active meal plan</h2><p>Your meal plan will show here when your coach shares one with you.</p></section></Page>
  const meals=objectItems(plan.meals);const totals=plan.totals&&typeof plan.totals==='object'?plan.totals as Record<string,unknown>:{}
  return <Page title="My meal plan" subtitle="Your meals and nutrition"><section className="panel"><div className="section-heading"><div><span className="eyebrow">Your plan</span><h2>{String(plan.title ?? 'Nutrition plan')}</h2></div><span className="status-pill positive">{String(plan.status ?? 'Active')}</span></div><div className="meal-totals">{[['Calories',totals.calories,'kcal'],['Protein',totals.protein,'g'],['Carbohydrates',totals.carbs,'g'],['Fat',totals.fat,'g']].map(([label,value,unit])=><article key={String(label)}><span>{String(label)}</span><strong>{String(value ?? '?')}</strong><small>{String(unit)}</small></article>)}</div></section>{meals.length?<section className="meal-card-list">{meals.map((meal,index)=><article className="panel meal-card" key={String(meal.id ?? index)}><div className="meal-card-heading"><span className="meal-time">{String(meal.time ?? 'Meal')}</span><h2>{String(meal.name ?? 'Meal')}</h2></div><div className="meal-macros"><span>{String(meal.calories ?? 0)} kcal</span><span>Protein {String(meal.protein ?? 0)} g</span><span>Carbs {String(meal.carbs ?? 0)} g</span><span>Fat {String(meal.fat ?? 0)} g</span></div></article>)}</section>:<section className="panel"><p className="empty-state">No meals have been added to this plan yet.</p></section>}</Page>
}
function MyShell() {
  const navigate = useNavigate(); const me = useMe(); const { t } = useLanguage(); const [drawerOpen, setDrawerOpen] = useState(false); const closeDrawer = () => setDrawerOpen(false)
  const logout = async () => { try { await apiFetch<void>(endpoints.logout, { method: 'POST', body: JSON.stringify({ refreshToken: sessionStorage.getItem('coachpulse.refreshToken') }) }) } finally { clearSession(); navigate('/login', { replace: true }) } }
  return <div className={`shell workspace-shell client-shell${drawerOpen ? ' drawer-open' : ''}`}><button className="workspace-scrim" aria-label="Close navigation" onClick={closeDrawer} tabIndex={drawerOpen ? 0 : -1} /><aside id="workspace-navigation" className="workspace-drawer"><div className="brand">Coach<span>Pulse</span></div><div className="identity">{me.data?.name || 'My workspace'}<small>Client</small></div><nav onClick={closeDrawer}><NavLink to="/my/dashboard">{t('Dashboard')}</NavLink><NavLink to="/my/onboarding">{t('Getting started')}</NavLink><NavLink to="/my/profile">{t('My health')}</NavLink><NavLink to="/my/progress">{t('Progress')}</NavLink><NavLink to="/my/program">{t('My program')}</NavLink><NavLink to="/my/workouts">{t('Workout log')}</NavLink><NavLink to="/my/meal-plan">{t('Meal plan')}</NavLink><NavLink to="/my/checkins">{t('Check-ins')}</NavLink><NavLink to="/my/payments">{t('Subscription &amp; payments')}</NavLink><NavLink to="/my/messages">{t('Messages')}</NavLink><NavLink to="/my/notifications">{t('Notifications')}</NavLink></nav><button className="logout" onClick={() => void logout()}>Sign out</button></aside><main><div className="workspace-mobilebar"><button className="workspace-menu-toggle" aria-label={drawerOpen ? 'Close navigation' : 'Open navigation'} aria-expanded={drawerOpen} aria-controls="workspace-navigation" onClick={() => setDrawerOpen(value => !value)}>{drawerOpen ? <X size={19} /> : <Menu size={19} />}<span>Menu</span></button><span className="workspace-mobile-brand">Coach<span>Pulse</span></span><span className="workspace-mobile-role">Client</span></div><Routes><Route path="dashboard" element={<MyDashboard />} /><Route path="onboarding" element={<OnboardingPage />} /><Route path="profile" element={<ClientProfile />} /><Route path="progress" element={<MyProgress />} /><Route path="program" element={<MyProgram />} /><Route path="workouts" element={<WorkoutLogsPage />} /><Route path="meal-plan" element={<><MyMealPlan /><MealLogAction /></>} /><Route path="checkins" element={<><CheckInsPage /><CheckInManagement /></>} /><Route path="payments" element={<><ClientSubscriptionPage /><ClientSubscriptionLifecycle /><ClientPaymentDetails /></>} /><Route path="payment/return" element={<PaymobPaymentReturn />} /><Route path="messages" element={<ClientMessages />} /><Route path="notifications" element={<NotificationCenter />} /><Route path="*" element={<Navigate to="dashboard" replace />} /></Routes></main></div>
}
function RouteScrollReset() { const { pathname } = useLocation(); useEffect(() => { window.scrollTo({ top: 0, left: 0, behavior: 'auto' }); document.querySelector('.shell main')?.scrollTo({ top: 0, left: 0 }); }, [pathname]); return null }
function Table({ headers, rows }: { headers: string[]; rows: (string | number)[][] }) { return <section className="panel table-wrap"><table><thead><tr>{headers.map(header => <th key={header}>{header}</th>)}</tr></thead><tbody>{rows.length ? rows.map((row, index) => <tr key={index}>{row.map((cell, cellIndex) => <td key={cellIndex}>{cell}</td>)}</tr>) : <tr><td colSpan={headers.length}>ظ„ط§ طھظˆط¬ط¯ ط¨ï؟½? ط§ï؟½? ط§طھ</td></tr>}</tbody></table></section> }
function formatAdminValue(value: unknown, key = ''): string {
  const locale = document.documentElement.lang === 'ar' ? 'ar-EG' : 'en-US'
  if (value == null || value === '') return '\u2014'
  if (typeof value === 'number') return value.toLocaleString(locale)
  if (typeof value === 'boolean') return value ? (locale === 'ar-EG' ? '\u0646\u0639\u0645' : 'Yes') : (locale === 'ar-EG' ? '\u0644\u0627' : 'No')
  if (typeof value !== 'string') return Array.isArray(value) ? String(value.length) + ' ' + (locale === 'ar-EG' ? '\u0639\u0646\u0635\u0631' : 'items') : '\u2014'
  if (/createdAt|receivedAt|processedAt|deletionScheduledAt/i.test(key)) {
    const date = new Date(value)
    if (!Number.isNaN(date.getTime())) return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
  }
  return value.length > 52 ? `${value.slice(0, 8)}...${value.slice(-6)}` : value
}
function AdminDataTable({ value, columns }: { value: unknown; columns: [string, string][] }) {
  const rows = objectItems(value)
  if (rows.length === 0) return <p className="empty-state">ظ„ط§ طھظˆط¬ط¯ ط¨ï؟½? ط§ï؟½? ط§طھ ط­ط§ظ„ï؟½? ط§ظ‹.</p>
  return <div className="admin-table-scroll"><table className="admin-table"><thead><tr>{columns.map(([, label]) => <th key={label}>{label}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={String(row.id ?? row.tenantId ?? index)}>{columns.map(([key]) => <td key={key} title={typeof row[key] === 'string' ? String(row[key]) : undefined}>{formatAdminValue(row[key], key)}</td>)}</tr>)}</tbody></table></div>
}
function AdminQueryTable({ data, isLoading, isError, error, columns }: { data: unknown; isLoading: boolean; isError: boolean; error: unknown; columns: [string, string][] }) {
  if (isLoading) return <Loading />
  if (isError) return <ErrorState error={error} />
  return <AdminDataTable value={data} columns={columns} />
}
function Admin() {
  const tenants = useQuery({ queryKey: ['admin', 'tenants'], queryFn: () => apiFetch<unknown>('/superadmin/tenants?page=1&pageSize=100') })
  const metrics = useQuery({ queryKey: ['admin', 'metrics'], queryFn: () => apiFetch<Record<string, unknown>>('/superadmin/platform-analytics') })
  const aiUsage = useQuery({ queryKey: ['admin', 'ai-usage'], queryFn: () => apiFetch<unknown>('/superadmin/ai/usage') })
  const audit = useQuery({ queryKey: ['admin', 'audit'], queryFn: () => apiFetch<unknown>('/superadmin/audit-logs?page=1&pageSize=20') })
  const webhooks = useQuery({ queryKey: ['admin', 'webhooks'], queryFn: () => apiFetch<unknown>('/superadmin/webhook-events?page=1&pageSize=20') })
  return <AdminDashboard tenantRows={objectItems(tenants.data)} tenantLoading={tenants.isLoading} tenantError={tenants.isError ? tenants.error : undefined} metrics={metrics.data} metricsLoading={metrics.isLoading} metricsError={metrics.isError ? metrics.error : undefined} aiUsage={aiUsage} audit={audit} webhooks={webhooks} />
}
function AdminDashboard({ tenantRows, tenantLoading, tenantError, metrics, metricsLoading, metricsError, aiUsage, audit, webhooks }: { tenantRows: Record<string, unknown>[]; tenantLoading: boolean; tenantError: unknown; metrics: Record<string, unknown> | undefined; metricsLoading: boolean; metricsError: unknown; aiUsage: { data: unknown; isLoading: boolean; isError: boolean; error: unknown }; audit: { data: unknown; isLoading: boolean; isError: boolean; error: unknown }; webhooks: { data: unknown; isLoading: boolean; isError: boolean; error: unknown } }) {
  const navigate = useNavigate()
  const { language, t } = useLanguage()
  const { section = 'dashboard' } = useParams()
  const cache = useQueryClient()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [adminParams, setAdminParams] = useSearchParams()
  const [selectedTenantId, setSelectedTenantId] = useState(() => adminParams.get('tenantId') ?? '')
  const [tenantSearch, setTenantSearch] = useState('')
  const metricFields: [string, string][] = [['totalTenants', "\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0645\u0633\u0627\u062D\u0627\u062A"], ['activeTenants', "\u0627\u0644\u0645\u0633\u0627\u062D\u0627\u062A \u0627\u0644\u0646\u0634\u0637\u0629"], ['suspendedTenants', "\u0627\u0644\u0645\u0633\u0627\u062D\u0627\u062A \u0627\u0644\u0645\u0648\u0642\u0648\u0641\u0629"], ['totalUsers', "\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645\u064A\u0646"], ['totalClients', "\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0639\u0645\u0644\u0627\u0621"]]
  const filteredTenants = tenantRows.filter(tenant => `${String(tenant.name ?? '')} ${String(tenant.slug ?? '')}`.toLocaleLowerCase().includes(tenantSearch.trim().toLocaleLowerCase()))
  const logout = async () => {
    try { await apiFetch<void>(endpoints.logout, { method: 'POST', body: JSON.stringify({ refreshToken: sessionStorage.getItem('coachpulse.refreshToken') }) }) }
    finally { clearSession(); navigate('/login', { replace: true }) }
  }
  const navigateSection = (path: string, tenantId?: string) => { navigate(`/admin/${path}${tenantId ? `?tenantId=${encodeURIComponent(tenantId)}` : ''}`); setDrawerOpen(false) }
  const navItems = [
    { id: 'dashboard', label: "\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629", icon: LayoutDashboard },
    { id: 'tenants', label: "\u0645\u0633\u0627\u062D\u0627\u062A \u0627\u0644\u062A\u062F\u0631\u064A\u0628", icon: Building2 },
    { id: 'usage', label: "\u0627\u0633\u062A\u062E\u062F\u0627\u0645 AI", icon: Sparkles },
    { id: 'webhooks', label: "\u0623\u062D\u062F\u0627\u062B Webhook", icon: Activity },
    { id: 'audit', label: "\u0633\u062C\u0644 \u0627\u0644\u062A\u062F\u0642\u064A\u0642", icon: ClipboardList },
    { id: 'tenant-management', label: "\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0633\u0627\u062D\u0629", icon: Users },
    { id: 'exercises', label: "\u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0639\u0627\u0645\u0629", icon: Activity },
  ]
  return <div className="admin-dashboard" dir={language === 'ar' ? 'rtl' : 'ltr'}><div className="admin-topbar"><button className="admin-drawer-toggle" aria-label={drawerOpen ? "\u0625\u063A\u0644\u0627\u0642 \u0627\u0644\u0642\u0627\u0626\u0645\u0629" : "\u0641\u062A\u062D \u0642\u0627\u0626\u0645\u0629 \u0627\u0644\u0625\u062F\u0627\u0631\u0629"} aria-expanded={drawerOpen} onClick={() => setDrawerOpen(value => !value)}>{drawerOpen ? <X size={19} /> : <Menu size={19} />}<span>{t('Menu')}</span></button><Link to="/admin/dashboard" className="brand">Coach<span>Pulse</span></Link><span className="admin-topbar-caption"><ShieldCheck size={16} /> {t('Platform management')}</span><button className="admin-refresh-button" onClick={() => void cache.invalidateQueries({ queryKey: ['admin'] })}><RefreshCw size={15} /> {t('Refresh data')}</button><button className="secondary-button" onClick={() => void logout()}>{t('Sign out')}</button></div><div className={`admin-workspace${drawerOpen ? ' drawer-open' : ''}`}><button className="admin-drawer-scrim" aria-label="Close navigation" onClick={() => setDrawerOpen(false)} tabIndex={drawerOpen ? 0 : -1} /><aside className="admin-drawer" dir={language === 'ar' ? 'rtl' : 'ltr'}><div className="admin-drawer-heading"><span>{t('Administration')}</span><small>{t('Manage and monitor the platform')}</small></div><nav aria-label={'\u0623\u0642\u0633\u0627\u0645 \u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629'}>{navItems.map(({ id, label, icon: Icon }) => <NavLink key={id} to={`/admin/${id}`} end className={({ isActive }) => isActive ? 'active' : ''} onClick={() => setDrawerOpen(false)}><Icon size={17} /><span>{label}</span><ArrowUpLeft size={14} className="admin-drawer-arrow" /></NavLink>)}</nav><div className="admin-drawer-footer"><ShieldCheck size={16} /> {t('Platform administrator permissions')}</div></aside><div className="admin-main-content" dir={language === 'ar' ? 'rtl' : 'ltr'} data-section={section}><Page title={navItems.find(item => item.id === section)?.label ?? 'SuperAdmin'} subtitle={section === 'dashboard' ? t('Platform overview and service health') : `${t('Platform management')} / ${navItems.find(item => item.id === section)?.label ?? ''}`}>
    {section === 'exercises' ? <AdminExerciseWorkspace /> : section === 'audit' ? <section className="panel"><div className="section-heading"><div><h2>Platform audit log</h2><p>Read-only security and administrative events.</p></div></div><AdminQueryTable {...audit} columns={[["createdAt", "Time"], ["action", "Action"], ["actorRole", "Role"], ["entityType", "Entity"], ["tenantId", "Workspace"]]} /></section> : section === 'usage' ? <section className="panel"><h2>AI usage</h2><AdminQueryTable {...aiUsage} columns={[["usedToday", "Requests today"]]} /></section> : section === 'webhooks' ? <section className="panel"><h2>Webhook events</h2><AdminQueryTable {...webhooks} columns={[["status", "Status"], ["receivedAtUtc", "Received"], ["processedAtUtc", "Processed"]]} /></section> : section === 'tenant-management' ? <AdminTenantManager tenantRows={tenantRows} tenantLoading={tenantLoading} tenantError={tenantError} selectedTenantId={selectedTenantId} onSelectTenant={id => { setSelectedTenantId(id); setAdminParams(id ? { tenantId: id } : {}) }} /> : <><section className="admin-hero"><div className="admin-hero-copy"><span className="admin-hero-eyebrow"><Activity size={15} /> {t('Control center')}</span><h2>{t('Clearer platform management')}<br />{t('A smoother experience for everyone')}</h2><p>{t('Monitor club health, review recent activity, and open common tasks.')}</p><div className="admin-hero-actions"><button onClick={() => navigateSection('tenants')}>{t('Browse clubs')} <ArrowUpLeft size={16} /></button><button className="admin-hero-secondary" onClick={() => navigateSection('audit')}>{t('Audit log')}</button></div></div><div className="admin-hero-mark"><Sparkles size={34} /><span>CoachPulse</span><small>{t('Training and management platform')}</small></div></section>
    <section className="panel admin-overview" id="admin-overview"><div className="section-heading"><div><span className="admin-section-kicker">{t('Overview')}</span><h2>{t('Platform metrics')}</h2><p>{t('Aggregated figures for club growth and platform usage.')}</p></div><span className="admin-live-badge"><i /> {t('Platform summary')}</span></div>{metricsLoading ? <Loading /> : metricsError ? <ErrorState error={metricsError} /> : metrics ? <div className="admin-metric-grid">{metricFields.map(([key, label]) => <article className="admin-metric" key={key}><span>{t(label)}</span><strong>{formatAdminValue(metrics[key], key)}</strong></article>)}</div> : <p className="empty-state">{t('No metrics are available yet.')}</p>}</section>
    <section className="panel admin-tenants-panel" id="admin-tenants"><div className="section-heading"><div><span className="admin-section-kicker">ط¥ط¯ط§ط±ط© ط§ظ„ط¹ظ…ظ„ط§ط، ط§ظ„ظ…ط¤ط³ط³ï؟½? ï؟½? ï؟½? </span><h2>ظ…ط³ط§ط­ط§طھ ط§ظ„طھط¯ط±ï؟½? ط¨</h2><p>ط§ط¨ط­ط« ط¨ط§ظ„ط§ط³ظ… ط£ظˆ ط§ظ„ط±ط§ط¨ط·طŒ ط«ظ… ط§ظپطھط­ ط§ظ„ظ…ط³ط§ط­ط© ظ„ط¥ط¯ط§ط±ط© ط­ط§ظ„طھظ‡ط§ ظˆط§ط³طھط®ط¯ط§ظ…ظ‡ط§.</p></div><span className="count-badge">{tenantRows.length}  ظ…ط³ط§ط­ط©</span></div><div className="admin-tenant-toolbar"><label className="admin-search"><Search size={17} /><input value={tenantSearch} onChange={event => setTenantSearch(event.target.value)} placeholder={'\u0627\u0628\u062D\u062B \u0639\u0646 \u0645\u0633\u0627\u062D\u0629 \u062A\u062F\u0631\u064A\u0628'} aria-label={'\u0627\u0628\u062D\u062B \u0639\u0646 \u0645\u0633\u0627\u062D\u0629 \u062A\u062F\u0631\u064A\u0628'} /></label><span>{filteredTenants.length}  ï؟½? طھï؟½? ط¬ط©</span></div>{tenantLoading ? <Loading /> : tenantError ? <ErrorState error={tenantError} /> : filteredTenants.length === 0 ? <p className="empty-state">{tenantRows.length ? "\u0644\u0627 \u062A\u0648\u062C\u062F \u0645\u0633\u0627\u062D\u0629 \u062A\u0637\u0627\u0628\u0642 \u0627\u0644\u0628\u062D\u062B." : "\u0644\u0627 \u062A\u0648\u062C\u062F \u0645\u0633\u0627\u062D\u0627\u062A \u0644\u0639\u0631\u0636\u0647\u0627."}</p> : <div className="admin-table-scroll"><table className="admin-table"><thead><tr><th>ط§ظ„ظ…ط³ط§ط­ط©</th><th>ط§ظ„ط±ط§ط¨ط·</th><th>ط§ظ„ط­ط§ظ„ط©</th><th>طھط§ط±ï؟½? ط® ط§ظ„ط¥ï؟½? ط´ط§ط،</th><th></th></tr></thead><tbody>{filteredTenants.map((tenant, index) => <tr key={String(tenant.id ?? index)}><td><strong className="admin-tenant-name">{String(tenant.name ?? 'â€”')}</strong></td><td><code>{String(tenant.slug ?? 'â€”')}</code></td><td><span className={`tenant-status ${String(tenant.status ?? '').toLowerCase()}`}>{String(tenant.status ?? 'â€”')}</span></td><td>{formatAdminValue(tenant.createdAt, 'createdAt')}</td><td><button className="table-action" onClick={() => { const id = String(tenant.id ?? ''); setSelectedTenantId(id); navigateSection('tenant-management', id) }}>ط¥ط¯ط§ط±ط© ط§ظ„ظ…ط³ط§ط­ط©</button></td></tr>)}</tbody></table></div>}</section>
    <div className="admin-dashboard-grid"><section className="panel admin-feed-card" id="admin-usage"><div className="admin-card-title"><span className="admin-icon ai"><Sparkles size={17} /></span><div><h2>ط§ط³طھط®ط¯ط§ظ… ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ï؟½? ط§ط¹ï؟½? </h2><p>ط§ظ„ط·ظ„ط¨ط§طھ ط§ظ„ظ…ط³ط¬ظ„ط© ظ„ظ‡ط°ط§ ط§ظ„ï؟½? ظˆظ…</p></div></div><AdminQueryTable {...aiUsage} columns={[["usedToday", "\u0627\u0644\u0637\u0644\u0628\u0627\u062A \u0627\u0644\u064A\u0648\u0645"]]} /></section><section className="panel admin-feed-card" id="admin-webhooks"><div className="admin-card-title"><span className="admin-icon webhook"><Activity size={17} /></span><div><h2>ط£ع©ط¢آ£ط£ع©ط¢آ­ط£ع©ط¢آ¯ط£ع©ط¢آ§ط£ع©ط¢آ« Webhook</h2><p>ط£ط­ط¯ط« ط¹ظ…ظ„ï؟½? ط§طھ ط§ط³طھظ‚ط¨ط§ظ„ ظˆظ…ط¹ط§ظ„ط¬ط© ط§ظ„ط¯ظپط¹</p></div></div><AdminQueryTable {...webhooks} columns={[["status", "\u0627\u0644\u062D\u0627\u0644\u0629"], ["receivedAtUtc", "\u0648\u0642\u062A \u0627\u0644\u0627\u0633\u062A\u0644\u0627\u0645"], ["processedAtUtc", "\u0648\u0642\u062A \u0627\u0644\u0645\u0639\u0627\u0644\u062C\u0629"]]} /></section></div>
    <section className="panel admin-audit-panel" id="admin-audit"><div className="section-heading"><div><span className="admin-section-kicker">ظ…ط±ط§ظ‚ط¨ط© ط§ظ„ï؟½? ط´ط§ط·</span><h2>ط³ط¬ظ„ ط§ظ„طھط¯ظ‚ï؟½? ظ‚ ط§ظ„ط£ط®ï؟½? ط±</h2><p>ط£ط­ط¯ط§ط« ط¥ط¯ط§ط±ï؟½? ط© ظˆط£ظ…ï؟½? ï؟½? ط© ط¹ظ„ظ‰ ظ…ط³طھظˆظ‰ ط§ظ„ظ…ï؟½? طµط©.</p></div></div><AdminQueryTable {...audit} columns={[["createdAt", "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"], ["action", "\u0627\u0644\u0625\u062C\u0631\u0627\u0621"], ["actorRole", "\u0627\u0644\u062F\u0648\u0631"], ["entityType", "\u0646\u0648\u0639 \u0627\u0644\u0633\u062C\u0644"], ["tenantId", "\u0627\u0644\u0645\u0633\u0627\u062D\u0629"]]} /></section>
    <div id="admin-tenant-manager"><AdminTenantManager tenantRows={tenantRows} tenantLoading={tenantLoading} tenantError={tenantError} selectedTenantId={selectedTenantId} onSelectTenant={id => { setSelectedTenantId(id); setAdminParams(id ? { tenantId: id } : {}) }} /></div>
  </>}</Page></div></div></div>
}
function PublicTenant() {
  const { slug = '' } = useParams()
  const [input, setInput] = useState(slug)
  const [result, setResult] = useState<unknown>()
  const [plans, setPlans] = useState<unknown>()
  const [available, setAvailable] = useState<unknown>()
  const [error, setError] = useState('')
  const load = async (event: React.FormEvent) => { event.preventDefault(); setError(''); try { const [tenant, activePlans] = await Promise.all([apiFetch<unknown>(`/public/tenants/${encodeURIComponent(input)}`), apiFetch<unknown>(`/public/tenants/${encodeURIComponent(input)}/plans`)]); setResult(tenant); setPlans(activePlans) } catch (cause) { setError(cause instanceof Error ? cause.message : "\u0627\u0644\u0645\u0633\u0627\u062D\u0629 \u063A\u064A\u0631 \u0645\u062A\u0627\u062D\u0629.") } }
  const checkSlug = async () => { try { setAvailable(await apiFetch<unknown>(`/public/slug-available?slug=${encodeURIComponent(input)}`)) } catch (cause) { setError(cause instanceof Error ? cause.message : "\u062A\u0639\u0630\u0631 \u0627\u0644\u062A\u062D\u0642\u0642 \u0645\u0646 \u0627\u0644\u0631\u0627\u0628\u0637.") } }
  return <div className="login"><div className="login-card"><div className="brand">Coach<span>Pulse</span></div><h1>ط§ط³طھظƒط´ظپ ظ…ط³ط§ط­ط© طھط¯ط±ï؟½? ط¨</h1><form onSubmit={event => void load(event)}><label>ط±ط§ط¨ط· ط§ظ„ظ…ط³ط§ط­ط©<input value={input} onChange={event => setInput(event.target.value)} required /></label><button>ط¹ط±ط¶ ط§ظ„طµظپط­ط© ظˆط§ظ„ط¨ط§ظ‚ط§طھ</button><button type="button" onClick={() => void checkSlug()}>ط§ظ„طھط­ظ‚ظ‚ ظ…ï؟½?  طھظˆظپط± ط§ظ„ط±ط§ط¨ط·</button></form>{error && <p>{error}</p>}{result !== undefined && <><JsonPanel value={result} /><h2>ط§ظ„ط¨ط§ظ‚ط§طھ ط§ظ„ï؟½? ط´ط·ط©</h2><JsonPanel value={plans} /></>}{available !== undefined && <JsonPanel value={available} />}<Link to="/login">طھط³ط¬ï؟½? ظ„ ط§ظ„ط¯ط®ظˆظ„</Link></div></div>
}
function PublicInvitation() {
  const { token = '' } = useParams()
  const preview = useQuery({ queryKey: ['public', 'invitation', token], queryFn: () => apiFetch<unknown>(`/public/invitations/${encodeURIComponent(token)}`), enabled: Boolean(token) })
  return <div className="login"><div className="login-card"><h1>ط£ع©ط¢آ¯ط£ع©ط¢آ¹ط£â„¢ط«â€ ط£ع©ط¢آ© CoachPulse</h1>{preview.isLoading ? <Loading /> : preview.isError ? <ErrorState error={preview.error} /> : <><p>ط¨ï؟½? ط§ï؟½? ط§طھ ط§ظ„ط¯ط¹ظˆط© ط§ظ„ط¹ط§ظ…ط© (ط§ظ„ط¨ط±ï؟½? ط¯ ظ…ظ‚ï؟½? ظ‘ط¹):</p><JsonPanel value={preview.data} /><Link to={`/register/client?token=${encodeURIComponent(token)}`}>ط¥ظƒظ…ط§ظ„ ط§ظ„طھط³ط¬ï؟½? ظ„</Link></>}</div></div>
}

export default function App() {
  const { language } = useLanguage()
  useEffect(() => {
    const root = document.getElementById('root')
    if (!root) return
    const translate = (node: Node) => {
      if (node instanceof HTMLElement && !node.matches('[data-user-content]')) {
        for (const attribute of ['placeholder', 'title', 'aria-label']) {
          const value = node.getAttribute(attribute)
          if (value) {
            const translated = language === 'en' ? toEnglish(value) : toArabic(value)
            if (translated !== value) node.setAttribute(attribute, translated)
          }
        }
      }
      if (node.nodeType === Node.TEXT_NODE) {
        // Brand names are product identifiers, even when rendered as separate text nodes
        // ("Coach" + "Pulse"). Keep the name unchanged in every UI language.
        if (node.parentElement?.closest('pre,code,textarea,input,[data-user-content],.data-text,.brand,.workspace-mobile-brand')) return
        const original = node.textContent ?? ''
        const trimmed = original.trim()
        const translated = language === 'en' ? toEnglish(trimmed) : toArabic(trimmed)
        if (translated !== trimmed) node.textContent = original.replace(trimmed, translated)
        return
      }
      if (node instanceof HTMLElement && node.matches('pre,code,textarea,input,[data-user-content]')) return
      node.childNodes.forEach(translate)
    }
    const observer = new MutationObserver(records => records.forEach(record => {
      if (record.type === 'characterData') translate(record.target)
      else if (record.type === 'attributes') translate(record.target)
      else record.addedNodes.forEach(translate)
    }))
    translate(root)
    observer.observe(root, { childList: true, characterData: true, attributes: true, attributeFilter: ['placeholder', 'title', 'aria-label'], subtree: true })
    return () => observer.disconnect()
  }, [language])
  return <><div className="global-language-toggle"><LanguageToggle /></div><RouteScrollReset /><Routes>
    <Route path="/first-login-password" element={<FirstLoginPassword />} />
    <Route path="/login" element={<Login />} />
    <Route path="/tenant/:slug" element={<PublicTenant />} />
    <Route path="/invitation/:token" element={<PublicInvitation />} />
    <Route path="/register/tenant" element={<PublicAuth mode="tenant" />} />
    <Route path="/register/client" element={<PublicAuth mode="client" />} />
    <Route path="/accept-staff-invite" element={<PublicAuth mode="staff" />} />
    <Route path="/forgot-password" element={<PublicAuth mode="forgot" />} />
    <Route path="/reset-password" element={<PublicAuth mode="reset" />} />
    <Route path="/confirm-email" element={<PublicAuth mode="confirm" />} />
    <Route path="/resend-confirmation" element={<PublicAuth mode="resend" />} />
    <Route path="/app/*" element={<AppGuard roles={['TenantOwner', 'CoachStaff']}><Shell /></AppGuard>} />
    <Route path="/my/*" element={<AppGuard roles={['Client']}><MyShell /></AppGuard>} />
    <Route path="/admin/*" element={<AppGuard roles={['SuperAdmin']}><Routes><Route path="dashboard" element={<Admin />} /><Route path=":section" element={<Admin />} /><Route path="*" element={<Navigate to="dashboard" replace />} /></Routes></AppGuard>} />
    <Route path="/" element={<Navigate to={accessToken() ? '/app/dashboard' : '/login'} replace />} />
    <Route path="*" element={<Navigate to="/login" replace />} />
  </Routes></>
}
