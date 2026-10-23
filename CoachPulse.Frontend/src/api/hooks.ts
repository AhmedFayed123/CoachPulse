import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { accessToken, apiFetch, apiPost, endpoints, HUB_BASE } from './client'
import type { Client, ClientsPage, Conversation, ConversationsPage, DashboardSummary, Payment, PaymentsPage, Profile, Schema, SendMessageRequest } from './types'

export const queryKeys = {
  me: ['account', 'me'], dashboard: ['tenant', 'dashboard', 'summary'], clients: ['clients'],
  conversations: ['conversations'], payments: ['tenant', 'payments'], myDashboard: ['my', 'dashboard'],
  myProgram: ['my', 'program', 'today'], myMealPlan: ['my', 'meal-plan'], myMessages: ['my', 'messages'],
  notifications: ['notifications'],
} as const

export function useMe() {
  return useQuery({ queryKey: queryKeys.me, queryFn: () => apiFetch<Profile>('/account/me') })
}
export function useDashboard() {
  return useQuery({ queryKey: queryKeys.dashboard, queryFn: () => apiFetch<DashboardSummary>(endpoints.dashboard) })
}
export function useClients(page = 1, pageSize = 20) {
  return useQuery({ queryKey: [...queryKeys.clients, page, pageSize], queryFn: () => apiFetch<ClientsPage>(`${endpoints.clients}?page=${page}&pageSize=${pageSize}`), staleTime: 0 })
}
export function useConversations(page = 1, pageSize = 20) {
  return useQuery({ queryKey: [...queryKeys.conversations, page, pageSize], queryFn: () => apiFetch<ConversationsPage>(`${endpoints.conversations}?page=${page}&pageSize=${pageSize}`) })
}
export function usePayments(page = 1, pageSize = 20) {
  return useQuery({ queryKey: [...queryKeys.payments, page, pageSize], queryFn: () => apiFetch<PaymentsPage>(`${endpoints.payments}?page=${page}&pageSize=${pageSize}`) })
}
export function useMyDashboard(enabled = true) {
  return useQuery({ queryKey: queryKeys.myDashboard, queryFn: () => apiFetch<unknown>(endpoints.myDashboard), enabled })
}
export function useMyProgram(enabled = true) {
  return useQuery({ queryKey: queryKeys.myProgram, queryFn: () => apiFetch<unknown>(endpoints.myProgram), enabled })
}
export function useMyMealPlan(enabled = true) {
  return useQuery({ queryKey: queryKeys.myMealPlan, queryFn: async () => (await apiFetch<unknown | null>(endpoints.myMealPlan)) ?? null, enabled })
}
export function useMyMessages(enabled = true) {
  return useQuery({ queryKey: queryKeys.myMessages, queryFn: () => apiFetch<unknown>(endpoints.myMessages), enabled })
}
export function useNotifications(page = 1, pageSize = 20, unread = false) {
  return useQuery({ queryKey: [...queryKeys.notifications, page, pageSize, unread], queryFn: () => apiFetch<unknown>(`${endpoints.notifications}?page=${page}&pageSize=${pageSize}&unread=${unread}`), refetchInterval: 30_000 })
}

export function useUpdateProfile() {
  const cache = useQueryClient()
  return useMutation({
    mutationFn: (body: Schema<'UpdateAccountRequest'>) => apiFetch<void>('/account/me', { method: 'PUT', body: JSON.stringify(body) }),
    onSuccess: () => cache.invalidateQueries({ queryKey: queryKeys.me }),
  })
}

export function useSendClientMessage(clientId: string) {
  const cache = useQueryClient()
  return useMutation({
    mutationFn: (body: SendMessageRequest) => apiPost<unknown>(`/messages/${clientId}`, body),
    onSuccess: () => cache.invalidateQueries({ queryKey: queryKeys.conversations }),
  })
}

export function useChatConversation(clientId: string | null) {
  const cache = useQueryClient()
  const queryKey = useMemo(() => ['messages', clientId], [clientId])
  const connectionRef = useRef<import('@microsoft/signalr').HubConnection | null>(null)
  const [isTyping, setIsTyping] = useState(false)
  const [partnerOnline, setPartnerOnline] = useState<boolean | null>(null)
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'reconnecting' | 'offline'>('connecting')
  const messages = useQuery({ queryKey, queryFn: () => apiFetch<unknown>(`/messages/${clientId}?limit=100`), enabled: Boolean(clientId) })
  useEffect(() => {
    if (!clientId || !accessToken()) { setConnectionStatus('offline'); return }
    setConnectionStatus('connecting')
    const connection = new HubConnectionBuilder()
      .withUrl(`${HUB_BASE}/hubs/chat`, { accessTokenFactory: () => accessToken() || '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    connection.on('ReceiveMessage', () => { void cache.invalidateQueries({ queryKey }); void cache.invalidateQueries({ queryKey: queryKeys.conversations }); void cache.invalidateQueries({ queryKey: ['messages', 'unread-count'] }) })
    connection.on('MessageRead', () => { void cache.invalidateQueries({ queryKey }); void cache.invalidateQueries({ queryKey: queryKeys.conversations }); void cache.invalidateQueries({ queryKey: ['messages', 'unread-count'] }) })
    let typingTimer: ReturnType<typeof setTimeout> | undefined
    connection.on('UserTyping', payload => {
      if (payload && typeof payload === 'object' && 'conversationId' in payload && payload.conversationId === clientId) {
        setIsTyping(true)
        if (typingTimer) clearTimeout(typingTimer)
        typingTimer = setTimeout(() => setIsTyping(false), 2500)
      }
    })
    connection.on('PresenceChanged', payload => {
      if (payload && typeof payload === 'object' && 'isOnline' in payload && 'userId' in payload && payload.userId === clientId && typeof payload.isOnline === 'boolean') setPartnerOnline(payload.isOnline)
    })
    let active = true
    connectionRef.current = connection
    void connection.start().then(() => { if (active) { setConnectionStatus('connected'); return connection.invoke('JoinConversation', clientId) } }).catch(() => { if (active) setConnectionStatus('offline') })
    connection.onreconnecting(() => setConnectionStatus('reconnecting'))
    connection.onreconnected(() => { setConnectionStatus('connected'); void connection.invoke('JoinConversation', clientId).catch(() => undefined) })
    connection.onclose(() => setConnectionStatus('offline'))
    return () => { active = false; if (typingTimer) clearTimeout(typingTimer); connectionRef.current = null; void connection.stop() }
  }, [cache, clientId, queryKey])
  const sendTyping = useCallback(() => { if (clientId) void connectionRef.current?.invoke('Typing', clientId).catch(() => undefined) }, [clientId])
  return { ...messages, isTyping, partnerOnline, connectionStatus, sendTyping }
}

export function useNotificationsHub() {
  const cache = useQueryClient()
  useEffect(() => {
    if (!accessToken()) return
    const connection = new HubConnectionBuilder()
      .withUrl(`${HUB_BASE}/hubs/notifications`, { accessTokenFactory: () => accessToken() || '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    const refresh = () => void cache.invalidateQueries({ queryKey: queryKeys.notifications })
    connection.on('NotificationReceived', refresh)
    connection.on('UnreadCountUpdated', refresh)
    let active = true
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    const start = async () => {
      try { await connection.start() }
      catch { if (active) retryTimer = setTimeout(() => void start(), 5000) }
    }
    void start()
    return () => { active = false; if (retryTimer) clearTimeout(retryTimer); void connection.stop() }
  }, [cache])
}

export type { Client, Conversation, Payment }
