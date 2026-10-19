/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

export type Language = 'ar' | 'en'
type LanguageContextValue = { language: Language; setLanguage: (language: Language) => void; t: (text: string) => string }
const LanguageContext = createContext<LanguageContextValue | null>(null)

const arabic: Record<string, string> = {
  'Club': 'ط§ظ„ظ†ط§ط¯ظٹ', 'Clubs': 'ط§ظ„ط£ظ†ط¯ظٹط©', 'Workspace': 'ط§ظ„ظ†ط§ط¯ظٹ', 'Workspaces': 'ط§ظ„ط£ظ†ط¯ظٹط©', 'My club': 'ظ†ط§ط¯ظٹظٹ',
  'Search by club name': 'ط§ط¨ط­ط« ط¨ط§ط³ظ… ط§ظ„ظ†ط§ط¯ظٹ', 'Available clubs': 'ط§ظ„ط£ظ†ط¯ظٹط© ط§ظ„ظ…طھط§ط­ط©',
  ...{ Overview: 'ظ†ط¸ط±ط© ط¹ط§ظ…ط©', Clients: 'ط§ظ„ط¹ظ…ظ„ط§ط،', 'Coaching team': 'ظپط±ظٹظ‚ ط§ظ„طھط¯ط±ظٹط¨', 'Check-in reviews': 'ظ…ط±ط§ط¬ط¹ط© ط§ظ„ظ…طھط§ط¨ط¹ط§طھ', Programs: 'ط§ظ„ط¨ط±ط§ظ…ط¬', Exercises: 'ط§ظ„طھظ…ط§ط±ظٹظ†', 'Meal plans': 'ط®ط·ط· ط§ظ„ظˆط¬ط¨ط§طھ', Plans: 'ط§ظ„ط¨ط§ظ‚ط§طھ', Conversations: 'ط§ظ„ظ…ط­ط§ط¯ط«ط§طھ', Payments: 'ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ', Subscriptions: 'ط§ظ„ط§ط´طھط±ط§ظƒط§طھ', Notifications: 'ط§ظ„ط¥ط´ط¹ط§ط±ط§طھ', 'AI tools': 'ط£ط¯ظˆط§طھ ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ', 'AI review': 'ظ…ط±ط§ط¬ط¹ط© ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ', 'Workspace settings': 'ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ظ†ط§ط¯ظٹ', Analytics: 'ط§ظ„طھط­ظ„ظٹظ„ط§طھ', 'Audit log': 'ط³ط¬ظ„ ط§ظ„طھط¯ظ‚ظٹظ‚', Account: 'ط§ظ„ط­ط³ط§ط¨', 'Sign out': 'طھط³ط¬ظٹظ„ ط§ظ„ط®ط±ظˆط¬', Menu: 'ط§ظ„ظ‚ط§ط¦ظ…ط©' },
  ...{ 'Training programs': 'ط§ظ„ط¨ط±ط§ظ…ط¬ ط§ظ„طھط¯ط±ظٹط¨ظٹط©', 'Create a program, add its exercises, then assign it to a client.': 'ط£ظ†ط´ط¦ ط¨ط±ظ†ط§ظ…ط¬ط§ظ‹طŒ ط£ط¶ظپ ط§ظ„طھظ…ط§ط±ظٹظ†طŒ ط«ظ… ط§ط³ظ†ط¯ظ‡ ط¥ظ„ظ‰ ط¹ظ…ظٹظ„.', 'Meal plans': 'ط®ط·ط· ط§ظ„ظˆط¬ط¨ط§طھ', 'Create a meal plan, add nutrition details, and assign it to a client.': 'ط£ظ†ط´ط¦ ط®ط·ط© ظˆط¬ط¨ط§طھطŒ ط£ط¶ظپ طھظپط§طµظٹظ„ ط§ظ„طھط؛ط°ظٹط©طŒ ط«ظ… ط§ط³ظ†ط¯ظ‡ط§ ط¥ظ„ظ‰ ط¹ظ…ظٹظ„.', 'Workspace settings': 'ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ظ†ط§ط¯ظٹ', 'Only the workspace owner can change these settings.': 'ظٹظ…ظƒظ† ظ„ظ…ط§ظ„ظƒ ط§ظ„ظ†ط§ط¯ظٹ ظپظ‚ط· طھط¹ط¯ظٹظ„ ظ‡ط°ظ‡ ط§ظ„ط¥ط¹ط¯ط§ط¯ط§طھ.', 'Manage workspace details, branding and payment methods.': 'ط£ط¯ط± ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ†ط§ط¯ظٹ ظˆظ‡ظˆظٹطھظ‡ ظˆط·ط±ظ‚ ط§ظ„ط¯ظپط¹.' },
  ...{ 'The page': 'ط§ظ„طµظپط­ط©', 'Items per page': 'ط¹ط¯ط¯ ط§ظ„ط¹ظ†ط§طµط± ظپظٹ ط§ظ„طµظپط­ط©', Items: 'ط§ظ„ط¹ظ†ط§طµط±', 'Total count': 'ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ†طھط§ط¦ط¬', 'No data available': 'ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ ظ…طھط§ط­ط©', 'No items found': 'ظ„ط§ طھظˆط¬ط¯ ط¹ظ†ط§طµط±', 'Loading data...': 'ط¬ط§ط±ظچ طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ...', 'Select from list': 'ط§ط®طھط± ظ…ظ† ط§ظ„ظ‚ط§ط¦ظ…ط©', 'No items available': 'ظ„ط§ طھظˆط¬ط¯ ط¹ظ†ط§طµط± ظ…طھط§ط­ط©', 'No messages yet': 'ظ„ط§ طھظˆط¬ط¯ ط±ط³ط§ط¦ظ„ ط¨ط¹ط¯', 'Send message': 'ط¥ط±ط³ط§ظ„ ط±ط³ط§ظ„ط©', 'Search clients': 'ط§ط¨ط­ط« ط¹ظ† ط¹ظ…ظٹظ„', 'Unread only': 'ط؛ظٹط± ط§ظ„ظ…ظ‚ط±ظˆط،ط© ظپظ‚ط·', Inbox: 'طµظ†ط¯ظˆظ‚ ط§ظ„ظˆط§ط±ط¯', 'Select a conversation': 'ط§ط®طھط± ظ…ط­ط§ط¯ط«ط©', 'Back to conversations': 'ط§ظ„ط¹ظˆط¯ط© ظ„ظ„ظ…ط­ط§ط¯ط«ط§طھ' },
  ...{ 'My dashboard': 'ظ„ظˆط­ط© ط§ظ„ط¹ظ…ظٹظ„', 'My program': 'ط¨ط±ظ†ط§ظ…ط¬ظٹ', 'My progress': 'طھظ‚ط¯ظ…ظٹ', 'My meal plan': 'ط®ط·ط© ظˆط¬ط¨ط§طھظٹ', 'My messages': 'ط±ط³ط§ط¦ظ„ظٹ', 'My check-ins': 'ظ…طھط§ط¨ط¹ط§طھظٹ', 'My payments': 'ظ…ط¯ظپظˆط¹ط§طھظٹ', 'My subscription': 'ط§ط´طھط±ط§ظƒظٹ', 'My profile': 'ظ…ظ„ظپظٹ ط§ظ„ط´ط®طµظٹ', 'My coach': 'ظ…ط¯ط±ط¨ظٹ', 'Workout logs': 'ط³ط¬ظ„ ط§ظ„طھظ…ط§ط±ظٹظ†', 'Client details': 'ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¹ظ…ظٹظ„', 'Tenant plans': 'ط¨ط§ظ‚ط§طھ ط§ظ„ظ†ط§ط¯ظٹ', 'Payment settings': 'ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ط¯ظپط¹' },
  ...{ 'Start date': 'طھط§ط±ظٹط® ط§ظ„ط¨ط¯ط،', 'Program name': 'ط§ط³ظ… ط§ظ„ط¨ط±ظ†ط§ظ…ط¬', 'Duration in days': 'ط§ظ„ظ…ط¯ط© ط¨ط§ظ„ط£ظٹط§ظ…', 'Create draft': 'ط¥ظ†ط´ط§ط، ظ…ط³ظˆط¯ط©', 'Workspace programs': 'ط¨ط±ط§ظ…ط¬ ط§ظ„ظ†ط§ط¯ظٹ', 'Open a program to review or edit it. Status and assignment are shown below.': 'ط§ظپطھط­ ط§ظ„ط¨ط±ظ†ط§ظ…ط¬ ظ„ظ…ط±ط§ط¬ط¹طھظ‡ ط£ظˆ طھط¹ط¯ظٹظ„ظ‡. طھط¸ظ‡ط± ط­ط§ظ„طھظ‡ ظˆط¥ط³ظ†ط§ط¯ظ‡ ط£ط¯ظ†ط§ظ‡.', 'No programs yet. Create your first draft above.': 'ظ„ط§ طھظˆط¬ط¯ ط¨ط±ط§ظ…ط¬ ط¨ط¹ط¯. ط£ظ†ط´ط¦ ط£ظˆظ„ ظ…ط³ظˆط¯ط© ط£ط¹ظ„ط§ظ‡.', 'Manage plan': 'ط¥ط¯ط§ط±ط© ط§ظ„ط®ط·ط©', 'Manage': 'ط¥ط¯ط§ط±ط©', 'Create a meal plan': 'ط¥ظ†ط´ط§ط، ط®ط·ط© ظˆط¬ط¨ط§طھ', 'Plan name': 'ط§ط³ظ… ط§ظ„ط®ط·ط©', 'Create meal draft': 'ط¥ظ†ط´ط§ط، ظ…ط³ظˆط¯ط©', 'Workspace meal plans': 'ط®ط·ط· ظˆط¬ط¨ط§طھ ط§ظ„ظ†ط§ط¯ظٹ', 'Open a plan to manage meals or assign it.': 'ط§ظپطھط­ ط§ظ„ط®ط·ط© ظ„ط¥ط¯ط§ط±ط© ط§ظ„ظˆط¬ط¨ط§طھ ط£ظˆ ط¥ط³ظ†ط§ط¯ظ‡ط§.', 'No meal plans yet.': 'ظ„ط§ طھظˆط¬ط¯ ط®ط·ط· ظˆط¬ط¨ط§طھ ط¨ط¹ط¯.' },
  ...{ 'Exercise library': 'ظ…ظƒطھط¨ط© ط§ظ„طھظ…ط§ط±ظٹظ†', 'AI progress review': 'ظ…ط±ط§ط¬ط¹ط© ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ ظˆط§ظ„طھظ‚ط¯ظ…', 'Tenant plans': 'ط§ظ„ط¨ط§ظ‚ط§طھ', 'Client details': 'طھظپط§طµظٹظ„ ط§ظ„ط¹ظ…ظٹظ„', 'Coach team': 'ظپط±ظٹظ‚ ط§ظ„طھط¯ط±ظٹط¨', 'Check-in review': 'ظ…ط±ط§ط¬ط¹ط© ط§ظ„ظ…طھط§ط¨ط¹ط§طھ', 'My workout log': 'ط³ط¬ظ„ طھظ…ط§ط±ظٹظ†ظٹ', 'Payment history': 'ط³ط¬ظ„ ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ', 'Notification center': 'ظ…ط±ظƒط² ط§ظ„ط¥ط´ط¹ط§ط±ط§طھ', 'Account settings': 'ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ط­ط³ط§ط¨', 'Billing and subscriptions': 'ط§ظ„ظپظˆطھط±ط© ظˆط§ظ„ط§ط´طھط±ط§ظƒط§طھ', 'Platform dashboard': 'ظ„ظˆط­ط© ط¥ط¯ط§ط±ط© ط§ظ„ظ…ظ†طµط©', 'Dashboard': 'ظ„ظˆط­ط© ط§ظ„طھط­ظƒظ…', 'Getting started': 'ط§ط¨ط¯ط£ ظ‡ظ†ط§', 'My health': 'ط¨ظٹط§ظ†ط§طھظٹ ط§ظ„طµط­ظٹط©', 'Workout log': 'ط³ط¬ظ„ ط§ظ„طھظ…ط§ط±ظٹظ†', 'Check-ins': 'ط§ظ„ظ…طھط§ط¨ط¹ط§طھ', 'Subscription & payments': 'ط§ظ„ط§ط´طھط±ط§ظƒ ظˆط§ظ„ظ…ط¯ظپظˆط¹ط§طھ', 'Messages': 'ط§ظ„ط±ط³ط§ط¦ظ„' },
  ...{ page: 'ط§ظ„طµظپط­ط©', pageSize: 'ط¹ط¯ط¯ ط§ظ„ط¹ظ†ط§طµط± ظپظٹ ط§ظ„طµظپط­ط©', totalCount: 'ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ†طھط§ط¦ط¬', items: 'ط§ظ„ط¹ظ†ط§طµط±', id: 'ط§ظ„ظ…ط¹ط±ظ‘ظپ', name: 'ط§ظ„ط§ط³ظ…', title: 'ط§ظ„ط¹ظ†ظˆط§ظ†', email: 'ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ', status: 'ط§ظ„ط­ط§ظ„ط©', role: 'ط§ظ„ط¯ظˆط±', amount: 'ط§ظ„ظ…ط¨ظ„ط؛', currency: 'ط§ظ„ط¹ظ…ظ„ط©', price: 'ط§ظ„ط³ط¹ط±', createdAt: 'طھط§ط±ظٹط® ط§ظ„ط¥ظ†ط´ط§ط،', updatedAt: 'ط¢ط®ط± طھط­ط¯ظٹط«', clientId: 'ط§ظ„ط¹ظ…ظٹظ„', coachId: 'ط§ظ„ظ…ط¯ط±ط¨', tenantId: 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ظ†ط§ط¯ظٹ', planId: 'ط§ظ„ط¨ط§ظ‚ط©', subscriptionId: 'ط§ظ„ط§ط´طھط±ط§ظƒ', paymentId: 'ط§ظ„ط¯ظپط¹ط©', programId: 'ط§ظ„ط¨ط±ظ†ط§ظ…ط¬', exerciseId: 'ط§ظ„طھظ…ط±ظٹظ†', purpose: 'ط§ظ„ط؛ط±ط¶', originalName: 'ط§ط³ظ… ط§ظ„ظ…ظ„ظپ', contentType: 'ظ†ظˆط¹ ط§ظ„ظ…ظ„ظپ', sizeBytes: 'ط­ط¬ظ… ط§ظ„ظ…ظ„ظپ', riskLevel: 'ظ…ط³طھظˆظ‰ ط§ظ„ظ…ط®ط§ط·ط±', riskScore: 'ط¯ط±ط¬ط© ط§ظ„ظ…ط®ط§ط·ط±', unreadCount: 'ط±ط³ط§ط¦ظ„ ط؛ظٹط± ظ…ظ‚ط±ظˆط،ط©', durationDays: 'ط§ظ„ظ…ط¯ط© ط¨ط§ظ„ط£ظٹط§ظ…', durationWeeks: 'ط§ظ„ظ…ط¯ط© ط¨ط§ظ„ط£ط³ط§ط¨ظٹط¹', startDate: 'طھط§ط±ظٹط® ط§ظ„ط¨ط¯ط،', endDate: 'طھط§ط±ظٹط® ط§ظ„ط§ظ†طھظ‡ط§ط،', expiresAt: 'طھط§ط±ظٹط® ط§ظ„ط§ظ†طھظ‡ط§ط،', features: 'ط§ظ„ظ…ظ…ظٹط²ط§طھ', meals: 'ط§ظ„ظˆط¬ط¨ط§طھ', exercises: 'ط§ظ„طھظ…ط§ط±ظٹظ†', workoutPlan: 'ط®ط·ط© ط§ظ„طھظ…ط±ظٹظ†', alerts: 'ط§ظ„طھظ†ط¨ظٹظ‡ط§طھ', message: 'ط§ظ„ط±ط³ط§ظ„ط©', body: 'ط§ظ„طھظپط§طµظٹظ„', type: 'ط§ظ„ظ†ظˆط¹', action: 'ط§ظ„ط¥ط¬ط±ط§ط،', entityType: 'ظ†ظˆط¹ ط§ظ„ط³ط¬ظ„', entityId: 'ط§ظ„ط³ط¬ظ„', actorRole: 'ط¯ظˆط± ط§ظ„ظ…ظ†ظپظ‘ط°', calories: 'ط§ظ„ط³ط¹ط±ط§طھ ط§ظ„ط­ط±ط§ط±ظٹط©', protein: 'ط¨ط±ظˆطھظٹظ†', carbs: 'ظƒط±ط¨ظˆظ‡ظٹط¯ط±ط§طھ', fat: 'ط¯ظ‡ظˆظ†', muscleGroup: 'ط§ظ„ظ…ط¬ظ…ظˆط¹ط© ط§ظ„ط¹ط¶ظ„ظٹط©', equipment: 'ط§ظ„ظ…ط¹ط¯ط§طھ', rowVersion: 'ط¥طµط¯ط§ط± ط§ظ„ط³ط¬ظ„', frequency: 'ط§ظ„طھظƒط±ط§ط±', dayOfWeek: 'ظٹظˆظ… ط§ظ„ط£ط³ط¨ظˆط¹' },
  ...{ 'Exercise library': 'ظ…ظƒطھط¨ط© ط§ظ„طھظ…ط§ط±ظٹظ†', Clients: 'ط§ظ„ط¹ظ…ظ„ط§ط،', 'Coach team': 'ظپط±ظٹظ‚ ط§ظ„ظ…ط¯ط±ط¨ظٹظ†', 'Check-ins review': 'ظ…ط±ط§ط¬ط¹ط© Check-ins', 'Paymob return': 'ط¹ظˆط¯ط© Paymob', 'Getting started': 'ط§ظ„طھظ‡ظٹط¦ط© ط§ظ„ط£ظˆظ„ظ‰', 'My profile': 'ظ…ظ„ظپظٹ ط§ظ„ط´ط®طµظٹ ظˆط§ظ„طµط­ظٹ', 'My check-in': 'Check-ins', 'My subscriptions and payments': 'ط§ظ„ط§ط´طھط±ط§ظƒط§طھ ظˆط§ظ„ظ…ط¯ظپظˆط¹ط§طھ', 'Conversation with coach': 'ط§ظ„ظ…ط­ط§ط¯ط«ط© ظ…ط¹ ط§ظ„ظ…ط¯ط±ط¨', Analytics: 'ط§ظ„طھط­ظ„ظٹظ„ط§طھ', 'AI workspace': 'ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ', 'My dashboard': 'ظ„ظˆط­ط© ط§ظ„ط¹ظ…ظٹظ„', 'My program': 'ط¨ط±ظ†ط§ظ…ط¬ظٹ', 'My progress': 'طھظ‚ط¯ظ…ظٹ', 'My meal plan': 'ط®ط·ط© ظˆط¬ط¨ط§طھظٹ', 'A quick look at your training and account.': 'ظ†ط¸ط±ط© ط³ط±ظٹط¹ط© ط¹ظ„ظ‰ طھط¯ط±ظٹط¨ظƒ ظˆط­ط³ط§ط¨ظƒ.', 'Your current plan, today\'s workout and training history': 'ط®ط·طھظƒ ط§ظ„ط­ط§ظ„ظٹط© ظˆطھظ…ط±ظٹظ† ط§ظ„ظٹظˆظ… ظˆط³ط¬ظ„ طھط¯ط±ظٹط¨ظƒ.', 'Your measurements and progress': 'ظ‚ظٹط§ط³ط§طھظƒ ظˆطھظ‚ط¯ظ…ظƒ.', 'Your meals and nutrition': 'ظˆط¬ط¨ط§طھظƒ ظˆطھط؛ط°ظٹطھظƒ.', 'Keep client conversations and new messages organized in one place.': 'طھط§ط¨ط¹ ط±ط³ط§ط¦ظ„ ط¹ظ…ظ„ط§ط¦ظƒ ظˆط±ط¯ ط¹ظ„ظٹظ‡ظ… ظ…ظ† ظ…ظƒط§ظ† ظˆط§ط­ط¯.', 'Your updates, reminders and account activity.': 'طھط­ط¯ظٹط«ط§طھظƒ ظˆطھط°ظƒظٹط±ط§طھظƒ ظˆظ†ط´ط§ط· ط­ط³ط§ط¨ظƒ.', 'Read-only activity history. The server limits results to your workspace.': 'ط³ط¬ظ„ ظ†ط´ط§ط· ظ„ظ„ظ‚ط±ط§ط،ط© ظپظ‚ط·. طھظ‚طھطµط± ط§ظ„ظ†طھط§ط¦ط¬ ط¹ظ„ظ‰ ظ†ط§ط¯ظٹظƒ.', 'Review each client\'s plan and status, then take the appropriate action.': 'ط±ط§ط¬ط¹ ط¨ط§ظ‚ط© ظƒظ„ ط¹ظ…ظٹظ„ ظˆط­ط§ظ„طھظ‡طŒ ط«ظ… ط§طھط®ط° ط§ظ„ط¥ط¬ط±ط§ط، ط§ظ„ظ…ظ†ط§ط³ط¨.', 'A workspace summary': 'ظ…ظ„ط®طµ ط§ظ„ظ†ط§ط¯ظٹ' },
  ...{ Message: 'ط§ظ„ط±ط³ط§ظ„ط©', 'Write to': 'ط§ظƒطھط¨ ط¥ظ„ظ‰', 'Attach photos, videos or files': 'ط£ط±ظپظ‚ طµظˆط±ط§ظ‹ ط£ظˆ ظپظٹط¯ظٹظˆظ‡ط§طھ ط£ظˆ ظ…ظ„ظپط§طھ', 'Uploading filesâ€¦': 'ط¬ط§ط±ظچ ط±ظپط¹ ط§ظ„ظ…ظ„ظپط§طھâ€¦', 'Sendingâ€¦': 'ط¬ط§ط±ظچ ط§ظ„ط¥ط±ط³ط§ظ„â€¦', 'Send message': 'ط¥ط±ط³ط§ظ„ ط§ظ„ط±ط³ط§ظ„ط©', 'Manage my messages': 'ط¥ط¯ط§ط±ط© ط±ط³ط§ط¦ظ„ظٹ', 'Select a message to delete': 'ط§ط®طھط± ط±ط³ط§ظ„ط© ظ„ط­ط°ظپظ‡ط§', 'Delete my message': 'ط­ط°ظپ ط±ط³ط§ظ„طھظٹ', 'Client conversation': 'ظ…ط­ط§ط¯ط«ط© ط§ظ„ط¹ظ…ظٹظ„', 'Presence unknown': 'ط­ط§ظ„ط© ط§ظ„ط§طھطµط§ظ„ ط؛ظٹط± ظ…ط¹ط±ظˆظپط©', 'Online now': 'ظ…طھطµظ„ ط§ظ„ط¢ظ†', Offline: 'ط؛ظٹط± ظ…طھطµظ„', 'Typingâ€¦': 'ظٹظƒطھط¨ ط§ظ„ط¢ظ†â€¦', Unread: 'ط؛ظٹط± ظ…ظ‚ط±ظˆط،ط©', 'Mark as read': 'طھط­ط¯ظٹط¯ ظƒظ…ظ‚ط±ظˆط،ط©', 'Back to conversations': 'ط§ظ„ط¹ظˆط¯ط© ظ„ظ„ظ…ط­ط§ط¯ط«ط§طھ', Inbox: 'طµظ†ط¯ظˆظ‚ ط§ظ„ظˆط§ط±ط¯', 'No messages yet': 'ظ„ط§ طھظˆط¬ط¯ ط±ط³ط§ط¦ظ„ ط¨ط¹ط¯', 'Send a message to start the conversation.': 'ط£ط±ط³ظ„ ط±ط³ط§ظ„ط© ظ„ط¨ط¯ط، ط§ظ„ظ…ط­ط§ط¯ط«ط©.', 'Conversation history': 'ط³ط¬ظ„ ط§ظ„ظ…ط­ط§ط¯ط«ط©', Attachment: 'ظ…ط±ظپظ‚', 'Message deleted': 'طھظ… ط­ط°ظپ ط§ظ„ط±ط³ط§ظ„ط©', Read: 'ظ…ظ‚ط±ظˆط،ط©', 'You can attach up to 10 files per message.': 'ظٹظ…ظƒظ†ظƒ ط¥ط±ظپط§ظ‚ 10 ظ…ظ„ظپط§طھ ظƒط­ط¯ ط£ظ‚طµظ‰ ظپظٹ ط§ظ„ط±ط³ط§ظ„ط©.', 'The uploaded file could not be identified.': 'طھط¹ط°ط± ط§ظ„طھط¹ط±ظپ ط¹ظ„ظ‰ ط§ظ„ظ…ظ„ظپ ط¨ط¹ط¯ ط±ظپط¹ظ‡.', 'Could not send the message.': 'طھط¹ط°ط± ط¥ط±ط³ط§ظ„ ط§ظ„ط±ط³ط§ظ„ط©.', 'Delete your message?': 'ظ‡ظ„ طھط±ظٹط¯ ط­ط°ظپ ط±ط³ط§ظ„طھظƒطں', 'Could not delete the message.': 'طھط¹ط°ط± ط­ط°ظپ ط§ظ„ط±ط³ط§ظ„ط©.', 'Could not update read status.': 'طھط¹ط°ط± طھط­ط¯ظٹط« ط­ط§ظ„ط© ط§ظ„ظ‚ط±ط§ط،ط©.', 'Loading attachmentâ€¦': 'ط¬ط§ط±ظچ طھط­ظ…ظٹظ„ ط§ظ„ظ…ط±ظپظ‚â€¦', 'Could not load attachment.': 'طھط¹ط°ط± طھط­ظ…ظٹظ„ ط§ظ„ظ…ط±ظپظ‚.', 'Open attachment': 'ظپطھط­ ط§ظ„ظ…ط±ظپظ‚', New: 'ط¬ط¯ظٹط¯', 'All notifications': 'ظƒظ„ ط§ظ„ط¥ط´ط¹ط§ط±ط§طھ', 'Mark all as read': 'طھط­ط¯ظٹط¯ ط§ظ„ظƒظ„ ظƒظ…ظ‚ط±ظˆط،', 'Notification preferences': 'طھظپط¶ظٹظ„ط§طھ ط§ظ„ط¥ط´ط¹ط§ط±ط§طھ', 'Delivery settings': 'ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ط¥ط±ط³ط§ظ„', 'Choose where each type of update is delivered.': 'ط§ط®طھط± ط·ط±ظٹظ‚ط© ط§ط³طھظ„ط§ظ… ظƒظ„ ظ†ظˆط¹ ظ…ظ† ط§ظ„طھط­ط¯ظٹط«ط§طھ.' },


}

Object.assign(arabic, {
  'Menu': '\u0627\u0644\u0642\u0627\u0626\u0645\u0629',
  'Protein': 'ط¨ط±ظˆطھظٹظ†', 'Carbs': 'ظƒط±ط¨ظˆظ‡ظٹط¯ط±ط§طھ', 'Fat': 'ط¯ظ‡ظˆظ†', 'Weekly': 'ط£ط³ط¨ظˆط¹ظٹ', 'BiWeekly': 'ظƒظ„ ط£ط³ط¨ظˆط¹ظٹظ†', 'Monthly': 'ط´ظ‡ط±ظٹط§ظ‹',
  'INBOX': 'طµظ†ط¯ظˆظ‚ ط§ظ„ظˆط§ط±ط¯', 'Your notifications': 'ط¥ط´ط¹ط§ط±ط§طھظƒ', 'Important updates from your coaching workspace.': 'طھط­ط¯ظٹط«ط§طھ ظ…ظ‡ظ…ط© ظ…ظ† ظ†ط§ط¯ظٹظƒ ط§ظ„طھط¯ط±ظٹط¨ظٹ.',
  'unread': 'ط؛ظٹط± ظ…ظ‚ط±ظˆط،', 'Previous': 'ط§ظ„ط³ط§ط¨ظ‚', 'Page': 'طµظپط­ط©', 'of': 'ظ…ظ†', 'Next': 'ط§ظ„طھط§ظ„ظٹ', 'Notification type': 'ظ†ظˆط¹ ط§ظ„ط¥ط´ط¹ط§ط±',
  'In app': 'ط¯ط§ط®ظ„ ط§ظ„طھط·ط¨ظٹظ‚', 'Email': 'ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ', 'Security and payment critical alerts are always enabled.': 'ط¥ط´ط¹ط§ط±ط§طھ ط§ظ„ط£ظ…ط§ظ† ظˆط§ظ„ظ…ط¯ظپظˆط¹ط§طھ ط§ظ„ظ…ظ‡ظ…ط© ظ…ظپط¹ظ‘ظ„ط© ط¯ط§ط¦ظ…ط§ظ‹.',
  'Save preferences': 'ط­ظپط¸ ط§ظ„طھظپط¶ظٹظ„ط§طھ', 'No check-ins recorded yet. Your progress updates will appear here.': 'ظ„ظ… طھط³ط¬ظ„ ط£ظٹ ظ…طھط§ط¨ط¹ط© ط¨ط¹ط¯. ط³طھط¸ظ‡ط± طھط­ط¯ظٹط«ط§طھ طھظ‚ط¯ظ…ظƒ ظ‡ظ†ط§.',
  'No subscription history yet.': 'ظ„ط§ ظٹظˆط¬ط¯ ط³ط¬ظ„ ط§ط´طھط±ط§ظƒط§طھ ط¨ط¹ط¯.', 'InstaPay is not enabled': 'InstaPay ط؛ظٹط± ظ…ظپط¹ظ‘ظ„',
  'Ask the club owner to enable it, or use another available payment method.': 'ط§ط·ظ„ط¨ ظ…ظ† ظ…ط§ظ„ظƒ ط§ظ„ظ†ط§ط¯ظٹ طھظپط¹ظٹظ„ظ‡ ط£ظˆ ط§ط³طھط®ط¯ظ… ط·ط±ظٹظ‚ط© ط¯ظپط¹ ط£ط®ط±ظ‰.',
  'InstaPay instructions': 'طھط¹ظ„ظٹظ…ط§طھ InstaPay', 'Activate a subscription manually': 'طھظپط¹ظٹظ„ ط§ط´طھط±ط§ظƒ ظٹط¯ظˆظٹط§ظ‹',
  'This activates access immediately. Use Payments to record a cash collection.': 'ط³ظٹط¤ط¯ظٹ ظ‡ط°ط§ ط¥ظ„ظ‰ طھظپط¹ظٹظ„ ط§ظ„ظˆطµظˆظ„ ظپظˆط±ط§ظ‹. ط§ط³طھط®ط¯ظ… ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ ظ„طھط³ط¬ظٹظ„ ط§ظ„طھط­طµظٹظ„ ط§ظ„ظ†ظ‚ط¯ظٹ.',
  'Note (optional)': 'ظ…ظ„ط§ط­ط¸ط© (ط§ط®طھظٹط§ط±ظٹ)', 'Activate subscription': 'طھظپط¹ظٹظ„ ط§ظ„ط§ط´طھط±ط§ظƒ', 'Workspace subscriptions': 'ط§ط´طھط±ط§ظƒط§طھ ط§ظ„ظ†ط§ط¯ظٹ',
  'Select a row to view details and management actions.': 'ط§ط®طھط± ط§ط´طھط±ط§ظƒط§ظ‹ ظ„ط¹ط±ط¶ طھظپط§طµظٹظ„ظ‡ ظˆط¥ط¬ط±ط§ط،ط§طھظ‡.', 'subscriptions': 'ط§ط´طھط±ط§ظƒط§طھ',
  'No subscriptions yet.': 'ظ„ط§ طھظˆط¬ط¯ ط§ط´طھط±ط§ظƒط§طھ ط¨ط¹ط¯.', 'Plan': 'ط§ظ„ط¨ط§ظ‚ط©', 'Status': 'ط§ظ„ط­ط§ظ„ط©', 'Price': 'ط§ظ„ط³ط¹ط±', 'End date': 'طھط§ط±ظٹط® ط§ظ„ط§ظ†طھظ‡ط§ط،',
  'Subscription details': 'طھظپط§طµظٹظ„ ط§ظ„ط§ط´طھط±ط§ظƒ', 'Close': 'ط¥ط؛ظ„ط§ظ‚', 'Reason': 'ط§ظ„ط³ط¨ط¨', 'Extension days': 'ط£ظٹط§ظ… ط§ظ„طھظ…ط¯ظٹط¯', 'Extend': 'طھظ…ط¯ظٹط¯',
  'Pause': 'ط¥ظٹظ‚ط§ظپ ظ…ط¤ظ‚طھ', 'Resume': 'ط§ط³طھط¦ظ†ط§ظپ', 'Cancel': 'ط¥ظ„ط؛ط§ط،', 'No active subscription yet. Choose a plan to get started.': 'ظ„ط§ ظٹظˆط¬ط¯ ط§ط´طھط±ط§ظƒ ظ†ط´ط·. ط§ط®طھط± ط¨ط§ظ‚ط© ظ„ظ„ط¨ط¯ط،.',
  'Pay by InstaPay': 'ط§ظ„ط¯ظپط¹ ط¹ط¨ط± InstaPay', 'Your subscription request is ready. Submit your transfer details below.': 'ط·ظ„ط¨ ط§ط´طھط±ط§ظƒظƒ ط¬ط§ظ‡ط². ط£ط¯ط®ظ„ طھظپط§طµظٹظ„ ط§ظ„طھط­ظˆظٹظ„ ط£ط¯ظ†ط§ظ‡.',
  'InstaPay is turned off for this club': 'InstaPay ط؛ظٹط± ظ…ظپط¹ظ‘ظ„ ظ„ظ‡ط°ط§ ط§ظ„ظ†ط§ط¯ظٹ', 'Ask the club owner to enable InstaPay, or choose Paymob if it is available.': 'ط§ط·ظ„ط¨ ظ…ظ† ظ…ط§ظ„ظƒ ط§ظ„ظ†ط§ط¯ظٹ طھظپط¹ظٹظ„ InstaPay ط£ظˆ ط§ط®طھط± Paymob ط¥ط°ط§ ظƒط§ظ† ظ…طھط§ط­ط§ظ‹.',
  'Reference number': 'ط±ظ‚ظ… ط§ظ„ظ…ط±ط¬ط¹', 'Amount': 'ط§ظ„ظ…ط¨ظ„ط؛', 'Payment receipt': 'ط¥ظٹطµط§ظ„ ط§ظ„ط¯ظپط¹', 'Submit transfer': 'ط¥ط±ط³ط§ظ„ ط§ظ„طھط­ظˆظٹظ„',
  'Workspace details': 'ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ†ط§ط¯ظٹ', 'Currency': 'ط§ظ„ط¹ظ…ظ„ط©', 'Time zone': 'ط§ظ„ظ…ظ†ط·ظ‚ط© ط§ظ„ط²ظ…ظ†ظٹط©', 'Subscription reminder days': 'ط£ظٹط§ظ… ط§ظ„طھط°ظƒظٹط± ظ‚ط¨ظ„ ط§ظ†طھظ‡ط§ط، ط§ظ„ط§ط´طھط±ط§ظƒ',
  'Default language': 'ط§ظ„ظ„ط؛ط© ط§ظ„ط§ظپطھط±ط§ط¶ظٹط©', 'Arabic': 'ط§ظ„ط¹ط±ط¨ظٹط©', 'English': 'ط§ظ„ط¥ظ†ط¬ظ„ظٹط²ظٹط©', 'Branding': 'ط§ظ„ظ‡ظˆظٹط© ط§ظ„ط¨طµط±ظٹط©', 'Payment methods': 'ط·ط±ظ‚ ط§ظ„ط¯ظپط¹',
  'Enable Paymob': 'طھظپط¹ظٹظ„ Paymob', 'Enable InstaPay': 'طھظپط¹ظٹظ„ InstaPay', 'InstaPay ID': 'ظ…ط¹ط±ظ‘ظپ InstaPay', 'Account number': 'ط±ظ‚ظ… ط§ظ„ط­ط³ط§ط¨',
  'Action': 'ط§ظ„ط¥ط¬ط±ط§ط،', 'Entity type': 'ظ†ظˆط¹ ط§ظ„ط³ط¬ظ„', 'Actor ID': 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ظ…ظ†ظپظ‘ط°', 'Apply filters': 'طھط·ط¨ظٹظ‚ ط§ظ„ظپظ„ط§طھط±', 'Clear filters': 'ظ…ط³ط­ ط§ظ„ظپظ„ط§طھط±',
  'No events match these filters.': 'ظ„ط§ طھظˆط¬ط¯ ط£ط­ط¯ط§ط« طھط·ط§ط¨ظ‚ ظ‡ط°ظ‡ ط§ظ„ظپظ„ط§طھط±.', 'Time': 'ط§ظ„ظˆظ‚طھ', 'Actor': 'ط§ظ„ظ…ظ†ظپظ‘ط°', 'Role': 'ط§ظ„ط¯ظˆط±', 'Entity': 'ط§ظ„ط³ط¬ظ„',
  'Record': 'ط§ظ„ط³ط¬ظ„', 'IP address': 'ط¹ظ†ظˆط§ظ† IP', 'events': 'ط£ط­ط¯ط§ط«', 'View todayâ€™s training': 'ط¹ط±ط¶ طھظ…ط±ظٹظ† ط§ظ„ظٹظˆظ…',
  'Your coach has not assigned a program yet. Your training plan will appear here once it is assigned.': 'ظ„ظ… ظٹط¹ظٹظ‘ظ† ظ…ط¯ط±ط¨ظƒ ط¨ط±ظ†ط§ظ…ط¬ط§ظ‹ ط¨ط¹ط¯. ط³طھط¸ظ‡ط± ط®ط·طھظƒ ط§ظ„طھط¯ط±ظٹط¨ظٹط© ظ‡ظ†ط§ ط¹ظ†ط¯ ط¥ط³ظ†ط§ط¯ظ‡ط§.',
  'Todayâ€™s exercises': 'طھظ…ط§ط±ظٹظ† ط§ظ„ظٹظˆظ…', 'sets': 'ظ…ط¬ظ…ظˆط¹ط§طھ', 'reps': 'طھظƒط±ط§ط±ط§طھ', 'Workspace': 'ط§ظ„ظ†ط§ط¯ظٹ', 'Workspace usage': 'ط§ط³طھط®ط¯ط§ظ… ط§ظ„ظ†ط§ط¯ظٹ',
  'Platform audit log': 'ط³ط¬ظ„ طھط¯ظ‚ظٹظ‚ ط§ظ„ظ…ظ†طµط©', 'Read-only security and administrative events.': 'ط³ط¬ظ„ ظ„ظ„ظ‚ط±ط§ط،ط© ظپظ‚ط· ظ„ظ„ط£ط­ط¯ط§ط« ط§ظ„ط£ظ…ظ†ظٹط© ظˆط§ظ„ط¥ط¯ط§ط±ظٹط©.',
  'AI usage': 'ط§ط³طھط®ط¯ط§ظ… ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ', 'Webhook events': 'ط£ط­ط¯ط§ط« Webhook', 'COACHPULSE / WORKSPACE': 'COACHPULSE / ط§ظ„ظ†ط§ط¯ظٹ',
  'COACHPULSE / TRAINING SPACE': 'COACHPULSE / ط§ظ„ظ†ط§ط¯ظٹ ط§ظ„طھط¯ط±ظٹط¨ظٹ', 'MOVEMENT LIBRARY': 'ظ…ظƒطھط¨ط© ط§ظ„ط­ط±ظƒط©', 'Open exercise demonstration': 'ط¹ط±ط¶ ط§ظ„طھظ…ط±ظٹظ†',
  'Add a secure demo video URL to this exercise to show it here.': 'ط£ط¶ظپ ط±ط§ط¨ط· ظپظٹط¯ظٹظˆ ط¢ظ…ظ† ظ„ظ‡ط°ط§ ط§ظ„طھظ…ط±ظٹظ† ظ„ط¹ط±ط¶ظ‡ ظ‡ظ†ط§.', 'Next step': 'ط§ظ„ط®ط·ظˆط© ط§ظ„طھط§ظ„ظٹط©',
  'Add exercises to the draft, then choose the client and start date.': 'ط£ط¶ظپ ط§ظ„طھظ…ط§ط±ظٹظ† ط¥ظ„ظ‰ ط§ظ„ظ…ط³ظˆط¯ط©طŒ ط«ظ… ط§ط®طھط± ط§ظ„ط¹ظ…ظٹظ„ ظˆطھط§ط±ظٹط® ط§ظ„ط¨ط¯ط،.', 'Assign program': 'ط¥ط³ظ†ط§ط¯ ط§ظ„ط¨ط±ظ†ط§ظ…ط¬',
  'programs': 'ط¨ط±ط§ظ…ط¬', 'days': 'ط£ظٹط§ظ…', 'plans': 'ط¨ط§ظ‚ط§طھ', 'Retry loading exercises': 'ط¥ط¹ط§ط¯ط© طھط­ظ…ظٹظ„ ط§ظ„طھظ…ط§ط±ظٹظ†', '&times;': 'أ—'
});

Object.assign(arabic, {
  Messages: 'ط§ظ„ط±ط³ط§ط¦ظ„',
  'Follow your client conversations and reply from one place.': 'طھط§ط¨ط¹ ظ…ط­ط§ط¯ط«ط§طھ ط¹ظ…ظ„ط§ط¦ظƒ ظˆط£ط¬ط¨ ط¹ظ† ط±ط³ط§ط¦ظ„ظ‡ظ… ظ…ظ† ظ…ظƒط§ظ† ظˆط§ط­ط¯.',
  Inbox: 'طµظ†ط¯ظˆظ‚ ط§ظ„ظˆط§ط±ط¯',
  conversations: 'ظ…ط­ط§ط¯ط«ط§طھ',
  'Unread messages': 'ط§ظ„ط±ط³ط§ط¦ظ„ ط؛ظٹط± ط§ظ„ظ…ظ‚ط±ظˆط،ط©',
  'Search clients': 'ط§ط¨ط­ط« ط¹ظ† ط¹ظ…ظٹظ„',
  'Type a client name': 'ط§ظƒطھط¨ ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„',
  'Show all conversations': 'ط¹ط±ط¶ ظƒظ„ ط§ظ„ظ…ط­ط§ط¯ط«ط§طھ',
  'Unread only': 'ط؛ظٹط± ط§ظ„ظ…ظ‚ط±ظˆط،ط© ظپظ‚ط·',
  Client: 'ط¹ظ…ظٹظ„',
  'No messages yet. Start the conversation.': 'ظ„ط§ طھظˆط¬ط¯ ط±ط³ط§ط¦ظ„ ط¨ط¹ط¯. ط§ط¨ط¯ط£ ط§ظ„ظ…ط­ط§ط¯ط«ط©.',
  'No conversations match your search.': 'ظ„ط§ طھظˆط¬ط¯ ظ…ط­ط§ط¯ط«ط§طھ طھط·ط§ط¨ظ‚ ط§ظ„ط¨ط­ط«.',
  'No conversations yet. Client messages will appear here.': 'ظ„ط§ طھظˆط¬ط¯ ظ…ط­ط§ط¯ط«ط§طھ ط¨ط¹ط¯. ط³طھط¸ظ‡ط± ط±ط³ط§ط¦ظ„ ط§ظ„ط¹ظ…ظ„ط§ط، ظ‡ظ†ط§.',
  'Select a conversation': 'ط§ط®طھط± ظ…ط­ط§ط¯ط«ط©',
  'Messages and client updates will appear here.': 'ط³طھط¸ظ‡ط± ط§ظ„ط±ط³ط§ط¦ظ„ ظˆطھط­ط¯ظٹط«ط§طھ ط§ظ„ط¹ظ…ظٹظ„ ظ‡ظ†ط§.',
  'Back to conversations': 'ط§ظ„ط¹ظˆط¯ط© ظ„ظ„ظ…ط­ط§ط¯ط«ط§طھ',
  'Client conversation': 'ظ…ط­ط§ط¯ط«ط© ط§ظ„ط¹ظ…ظٹظ„',
  'Presence unknown': 'ط­ط§ظ„ط© ط§ظ„ط§طھطµط§ظ„ ط؛ظٹط± ظ…ط¹ط±ظˆظپط©',
  'Online now': 'ظ…طھطµظ„ ط§ظ„ط¢ظ†', Offline: 'ط؛ظٹط± ظ…طھطµظ„',
  'Typing&': 'ظٹظƒطھط¨ ط§ظ„ط¢ظ†', Unread: 'ط؛ظٹط± ظ…ظ‚ط±ظˆط،ط©',
  'Mark as read': 'طھط­ط¯ظٹط¯ ظƒظ…ظ‚ط±ظˆط،ط©',
  Message: 'ط§ظ„ط±ط³ط§ظ„ط©', 'Write to': 'ط§ظƒطھط¨ ط¥ظ„ظ‰',
  'Attach photos, videos or files': 'ط¥ط±ظپط§ظ‚ طµظˆط± ط£ظˆ ظپظٹط¯ظٹظˆ ط£ظˆ ظ…ظ„ظپط§طھ',
  'Uploading files&': 'ط¬ط§ط±ظچ ط±ظپط¹ ط§ظ„ظ…ظ„ظپط§طھ...', 'Sending&': 'ط¬ط§ط±ظچ ط§ظ„ط¥ط±ط³ط§ظ„...',
  'Send message': 'ط¥ط±ط³ط§ظ„ ط§ظ„ط±ط³ط§ظ„ط©',
  'Manage my messages': 'ط¥ط¯ط§ط±ط© ط±ط³ط§ط¦ظ„ظٹ',
  'Select a message to delete': 'ط§ط®طھط± ط±ط³ط§ظ„ط© ظ„ط­ط°ظپظ‡ط§',
  'Delete my message': 'ط­ط°ظپ ط±ط³ط§ظ„طھظٹ',
  'No messages yet': 'ظ„ط§ طھظˆط¬ط¯ ط±ط³ط§ط¦ظ„ ط¨ط¹ط¯',
  'Send a message to start the conversation.': 'ط£ط±ط³ظ„ ط±ط³ط§ظ„ط© ظ„ط¨ط¯ط، ط§ظ„ظ…ط­ط§ط¯ط«ط©.',
  'Conversation history': 'ط³ط¬ظ„ ط§ظ„ظ…ط­ط§ط¯ط«ط©', Attachment: 'ظ…ط±ظپظ‚',
  'Message deleted': 'طھظ… ط­ط°ظپ ط§ظ„ط±ط³ط§ظ„ط©', Read: 'ظ…ظ‚ط±ظˆط،ط©',
  'You can attach up to 10 files per message.': 'ظٹظ…ظƒظ†ظƒ ط¥ط±ظپط§ظ‚ 10 ظ…ظ„ظپط§طھ ظƒط­ط¯ ط£ظ‚طµظ‰ ظپظٹ ط§ظ„ط±ط³ط§ظ„ط©.',
  'File is too large. Videos can be up to 100 MB; other files up to 10 MB.': 'ط­ط¬ظ… ط§ظ„ظ…ظ„ظپ ظƒط¨ظٹط±. ط­ط¯ ط§ظ„ظپظٹط¯ظٹظˆ 100 ظ…ظٹط¬ط§ط¨ط§ظٹطھطŒ ظˆ10 ظ…ظٹط¬ط§ط¨ط§ظٹطھ ظ„ط¨ط§ظ‚ظٹ ط§ظ„ظ…ظ„ظپط§طھ.',
  'Remove attachment': 'ط¥ط²ط§ظ„ط© ط§ظ„ظ…ط±ظپظ‚',
  'The uploaded file could not be identified.': 'طھط¹ط°ط± ط§ظ„طھط¹ط±ظپ ط¹ظ„ظ‰ ط§ظ„ظ…ظ„ظپ ط¨ط¹ط¯ ط±ظپط¹ظ‡.',
  'Could not send the message.': 'طھط¹ط°ط± ط¥ط±ط³ط§ظ„ ط§ظ„ط±ط³ط§ظ„ط©.',
  'Delete your message?': 'ظ‡ظ„ طھط±ظٹط¯ ط­ط°ظپ ط±ط³ط§ظ„طھظƒطں',
  'Could not delete the message.': 'طھط¹ط°ط± ط­ط°ظپ ط§ظ„ط±ط³ط§ظ„ط©.',
  'Could not update read status.': 'طھط¹ط°ط± طھط­ط¯ظٹط« ط­ط§ظ„ط© ط§ظ„ظ‚ط±ط§ط،ط©.',
  'Loading attachment&': 'ط¬ط§ط±ظچ طھط­ظ…ظٹظ„ ط§ظ„ظ…ط±ظپظ‚...',
  'Could not load attachment.': 'طھط¹ط°ط± طھط­ظ…ظٹظ„ ط§ظ„ظ…ط±ظپظ‚.',
  'Open attachment': 'ظپطھط­ ط§ظ„ظ…ط±ظپظ‚',
  'No coach assigned': '\u0644\u0627 \u064a\u0648\u062c\u062f \u0645\u062f\u0631\u0628 \u0645\u0639\u064a\u0646',
  'Your club will connect you with a coach. Messaging becomes available after assignment.': '\u0633\u064a\u0631\u0628\u0637\u0643 \u0627\u0644\u0646\u0627\u062f\u064a \u0628\u0645\u062f\u0631\u0628. \u0633\u062a\u062a\u0627\u062d \u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u0628\u0639\u062f \u0627\u0644\u062a\u0639\u064a\u064a\u0646.',
  'Talk privately with your assigned coach.': '\u062a\u0648\u0627\u0635\u0644 \u0628\u062e\u0635\u0648\u0635\u064a\u0629 \u0645\u0639 \u0645\u062f\u0631\u0628\u0643 \u0627\u0644\u0645\u0633\u0624\u0648\u0644.',
  Coach: '\u0627\u0644\u0645\u062f\u0631\u0628', 'Your coach': '\u0645\u062f\u0631\u0628\u0643', 'your coach': '\u0645\u062f\u0631\u0628\u0643',
  'Your assigned coach': '\u0645\u062f\u0631\u0628\u0643 \u0627\u0644\u0645\u0633\u0624\u0648\u0644',
  'Coach is typing&': '\u0627\u0644\u0645\u062f\u0631\u0628 \u064a\u0643\u062a\u0628 \u0627\u0644\u0622\u0646',
  'Connecting&': '\u062c\u0627\u0631\u064d \u0627\u0644\u0627\u062a\u0635\u0627\u0644...',
  'Write a message or attach a file.': '\u0627\u0643\u062a\u0628 \u0631\u0633\u0627\u0644\u0629 \u0623\u0648 \u0623\u0631\u0641\u0642 \u0645\u0644\u0641\u064b\u0627.',
  'Message sent.': '\u062a\u0645 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u0631\u0633\u0627\u0644\u0629.'
});

Object.assign(arabic, {
  'Protein': 'ط¨ط±ظˆطھظٹظ†', 'Carbs': 'ظƒط±ط¨ظˆظ‡ظٹط¯ط±ط§طھ', 'Fat': 'ط¯ظ‡ظˆظ†', 'Weekly': 'ط£ط³ط¨ظˆط¹ظٹ', 'BiWeekly': 'ظƒظ„ ط£ط³ط¨ظˆط¹ظٹظ†', 'Monthly': 'ط´ظ‡ط±ظٹط§ظ‹',
  'INBOX': 'طµظ†ط¯ظˆظ‚ ط§ظ„ظˆط§ط±ط¯', 'Your notifications': 'ط¥ط´ط¹ط§ط±ط§طھظƒ', 'Important updates from your coaching workspace.': 'طھط­ط¯ظٹط«ط§طھ ظ…ظ‡ظ…ط© ظ…ظ† ظ†ط§ط¯ظٹظƒ ط§ظ„طھط¯ط±ظٹط¨ظٹ.',
  'unread': 'ط؛ظٹط± ظ…ظ‚ط±ظˆط،', 'Previous': 'ط§ظ„ط³ط§ط¨ظ‚', 'Page': 'طµظپط­ط©', 'of': 'ظ…ظ†', 'Next': 'ط§ظ„طھط§ظ„ظٹ', 'Notification type': 'ظ†ظˆط¹ ط§ظ„ط¥ط´ط¹ط§ط±',
  'In app': 'ط¯ط§ط®ظ„ ط§ظ„طھط·ط¨ظٹظ‚', 'Email': 'ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ', 'Security and payment critical alerts are always enabled.': 'ط¥ط´ط¹ط§ط±ط§طھ ط§ظ„ط£ظ…ط§ظ† ظˆط§ظ„ظ…ط¯ظپظˆط¹ط§طھ ط§ظ„ظ…ظ‡ظ…ط© ظ…ظپط¹ظ‘ظ„ط© ط¯ط§ط¦ظ…ط§ظ‹.',
  'Save preferences': 'ط­ظپط¸ ط§ظ„طھظپط¶ظٹظ„ط§طھ', 'No check-ins recorded yet. Your progress updates will appear here.': 'ظ„ظ… طھط³ط¬ظ„ ط£ظٹ ظ…طھط§ط¨ط¹ط© ط¨ط¹ط¯. ط³طھط¸ظ‡ط± طھط­ط¯ظٹط«ط§طھ طھظ‚ط¯ظ…ظƒ ظ‡ظ†ط§.',
  'No subscription history yet.': 'ظ„ط§ ظٹظˆط¬ط¯ ط³ط¬ظ„ ط§ط´طھط±ط§ظƒط§طھ ط¨ط¹ط¯.', 'InstaPay is not enabled': 'InstaPay ط؛ظٹط± ظ…ظپط¹ظ‘ظ„',
  'Ask the club owner to enable it, or use another available payment method.': 'ط§ط·ظ„ط¨ ظ…ظ† ظ…ط§ظ„ظƒ ط§ظ„ظ†ط§ط¯ظٹ طھظپط¹ظٹظ„ظ‡ ط£ظˆ ط§ط³طھط®ط¯ظ… ط·ط±ظٹظ‚ط© ط¯ظپط¹ ط£ط®ط±ظ‰.',
  'InstaPay instructions': 'طھط¹ظ„ظٹظ…ط§طھ InstaPay', 'Activate a subscription manually': 'طھظپط¹ظٹظ„ ط§ط´طھط±ط§ظƒ ظٹط¯ظˆظٹط§ظ‹',
  'This activates access immediately. Use Payments to record a cash collection.': 'ط³ظٹط¤ط¯ظٹ ظ‡ط°ط§ ط¥ظ„ظ‰ طھظپط¹ظٹظ„ ط§ظ„ظˆطµظˆظ„ ظپظˆط±ط§ظ‹. ط§ط³طھط®ط¯ظ… ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ ظ„طھط³ط¬ظٹظ„ ط§ظ„طھط­طµظٹظ„ ط§ظ„ظ†ظ‚ط¯ظٹ.',
  'Note (optional)': 'ظ…ظ„ط§ط­ط¸ط© (ط§ط®طھظٹط§ط±ظٹ)', 'Activate subscription': 'طھظپط¹ظٹظ„ ط§ظ„ط§ط´طھط±ط§ظƒ', 'Workspace subscriptions': 'ط§ط´طھط±ط§ظƒط§طھ ط§ظ„ظ†ط§ط¯ظٹ',
  'Select a row to view details and management actions.': 'ط§ط®طھط± ط§ط´طھط±ط§ظƒط§ظ‹ ظ„ط¹ط±ط¶ طھظپط§طµظٹظ„ظ‡ ظˆط¥ط¬ط±ط§ط،ط§طھظ‡.', 'subscriptions': 'ط§ط´طھط±ط§ظƒط§طھ',
  'No subscriptions yet.': 'ظ„ط§ طھظˆط¬ط¯ ط§ط´طھط±ط§ظƒط§طھ ط¨ط¹ط¯.', 'Plan': 'ط§ظ„ط¨ط§ظ‚ط©', 'Status': 'ط§ظ„ط­ط§ظ„ط©', 'Price': 'ط§ظ„ط³ط¹ط±', 'End date': 'طھط§ط±ظٹط® ط§ظ„ط§ظ†طھظ‡ط§ط،',
  'Subscription details': 'طھظپط§طµظٹظ„ ط§ظ„ط§ط´طھط±ط§ظƒ', 'Close': 'ط¥ط؛ظ„ط§ظ‚', 'Reason': 'ط§ظ„ط³ط¨ط¨', 'Extension days': 'ط£ظٹط§ظ… ط§ظ„طھظ…ط¯ظٹط¯', 'Extend': 'طھظ…ط¯ظٹط¯',
  'Pause': 'ط¥ظٹظ‚ط§ظپ ظ…ط¤ظ‚طھ', 'Resume': 'ط§ط³طھط¦ظ†ط§ظپ', 'Cancel': 'ط¥ظ„ط؛ط§ط،', 'No active subscription yet. Choose a plan to get started.': 'ظ„ط§ ظٹظˆط¬ط¯ ط§ط´طھط±ط§ظƒ ظ†ط´ط·. ط§ط®طھط± ط¨ط§ظ‚ط© ظ„ظ„ط¨ط¯ط،.',
  'Pay by InstaPay': 'ط§ظ„ط¯ظپط¹ ط¹ط¨ط± InstaPay', 'Your subscription request is ready. Submit your transfer details below.': 'ط·ظ„ط¨ ط§ط´طھط±ط§ظƒظƒ ط¬ط§ظ‡ط². ط£ط¯ط®ظ„ طھظپط§طµظٹظ„ ط§ظ„طھط­ظˆظٹظ„ ط£ط¯ظ†ط§ظ‡.',
  'InstaPay is turned off for this club': 'InstaPay ط؛ظٹط± ظ…ظپط¹ظ‘ظ„ ظ„ظ‡ط°ط§ ط§ظ„ظ†ط§ط¯ظٹ', 'Ask the club owner to enable InstaPay, or choose Paymob if it is available.': 'ط§ط·ظ„ط¨ ظ…ظ† ظ…ط§ظ„ظƒ ط§ظ„ظ†ط§ط¯ظٹ طھظپط¹ظٹظ„ InstaPay ط£ظˆ ط§ط®طھط± Paymob ط¥ط°ط§ ظƒط§ظ† ظ…طھط§ط­ط§ظ‹.',
  'Reference number': 'ط±ظ‚ظ… ط§ظ„ظ…ط±ط¬ط¹', 'Amount': 'ط§ظ„ظ…ط¨ظ„ط؛', 'Payment receipt': 'ط¥ظٹطµط§ظ„ ط§ظ„ط¯ظپط¹', 'Submit transfer': 'ط¥ط±ط³ط§ظ„ ط§ظ„طھط­ظˆظٹظ„',
  'Workspace details': 'ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ†ط§ط¯ظٹ', 'Currency': 'ط§ظ„ط¹ظ…ظ„ط©', 'Time zone': 'ط§ظ„ظ…ظ†ط·ظ‚ط© ط§ظ„ط²ظ…ظ†ظٹط©', 'Subscription reminder days': 'ط£ظٹط§ظ… ط§ظ„طھط°ظƒظٹط± ظ‚ط¨ظ„ ط§ظ†طھظ‡ط§ط، ط§ظ„ط§ط´طھط±ط§ظƒ',
  'Default language': 'ط§ظ„ظ„ط؛ط© ط§ظ„ط§ظپطھط±ط§ط¶ظٹط©', 'Arabic': 'ط§ظ„ط¹ط±ط¨ظٹط©', 'English': 'ط§ظ„ط¥ظ†ط¬ظ„ظٹط²ظٹط©', 'Branding': 'ط§ظ„ظ‡ظˆظٹط© ط§ظ„ط¨طµط±ظٹط©', 'Payment methods': 'ط·ط±ظ‚ ط§ظ„ط¯ظپط¹',
  'Enable Paymob': 'طھظپط¹ظٹظ„ Paymob', 'Enable InstaPay': 'طھظپط¹ظٹظ„ InstaPay', 'InstaPay ID': 'ظ…ط¹ط±ظ‘ظپ InstaPay', 'Account number': 'ط±ظ‚ظ… ط§ظ„ط­ط³ط§ط¨',
  'Action': 'ط§ظ„ط¥ط¬ط±ط§ط،', 'Entity type': 'ظ†ظˆط¹ ط§ظ„ط³ط¬ظ„', 'Actor ID': 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ظ…ظ†ظپظ‘ط°', 'Apply filters': 'طھط·ط¨ظٹظ‚ ط§ظ„ظپظ„ط§طھط±', 'Clear filters': 'ظ…ط³ط­ ط§ظ„ظپظ„ط§طھط±',
  'No events match these filters.': 'ظ„ط§ طھظˆط¬ط¯ ط£ط­ط¯ط§ط« طھط·ط§ط¨ظ‚ ظ‡ط°ظ‡ ط§ظ„ظپظ„ط§طھط±.', 'Time': 'ط§ظ„ظˆظ‚طھ', 'Actor': 'ط§ظ„ظ…ظ†ظپظ‘ط°', 'Role': 'ط§ظ„ط¯ظˆط±', 'Entity': 'ط§ظ„ط³ط¬ظ„',
  'Record': 'ط§ظ„ط³ط¬ظ„', 'IP address': 'ط¹ظ†ظˆط§ظ† IP', 'events': 'ط£ط­ط¯ط§ط«', 'View todayâ€™s training': 'ط¹ط±ط¶ طھظ…ط±ظٹظ† ط§ظ„ظٹظˆظ…',
  'Your coach has not assigned a program yet. Your training plan will appear here once it is assigned.': 'ظ„ظ… ظٹط¹ظٹظ‘ظ† ظ…ط¯ط±ط¨ظƒ ط¨ط±ظ†ط§ظ…ط¬ط§ظ‹ ط¨ط¹ط¯. ط³طھط¸ظ‡ط± ط®ط·طھظƒ ط§ظ„طھط¯ط±ظٹط¨ظٹط© ظ‡ظ†ط§ ط¹ظ†ط¯ ط¥ط³ظ†ط§ط¯ظ‡ط§.',
  'Todayâ€™s exercises': 'طھظ…ط§ط±ظٹظ† ط§ظ„ظٹظˆظ…', 'sets': 'ظ…ط¬ظ…ظˆط¹ط§طھ', 'reps': 'طھظƒط±ط§ط±ط§طھ', 'Workspace': 'ط§ظ„ظ†ط§ط¯ظٹ', 'Workspace usage': 'ط§ط³طھط®ط¯ط§ظ… ط§ظ„ظ†ط§ط¯ظٹ',
  'Platform audit log': 'ط³ط¬ظ„ طھط¯ظ‚ظٹظ‚ ط§ظ„ظ…ظ†طµط©', 'Read-only security and administrative events.': 'ط³ط¬ظ„ ظ„ظ„ظ‚ط±ط§ط،ط© ظپظ‚ط· ظ„ظ„ط£ط­ط¯ط§ط« ط§ظ„ط£ظ…ظ†ظٹط© ظˆط§ظ„ط¥ط¯ط§ط±ظٹط©.',
  'AI usage': 'ط§ط³طھط®ط¯ط§ظ… ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ', 'Webhook events': 'ط£ط­ط¯ط§ط« Webhook', 'COACHPULSE / WORKSPACE': 'COACHPULSE / ط§ظ„ظ†ط§ط¯ظٹ',
  'COACHPULSE / TRAINING SPACE': 'COACHPULSE / ط§ظ„ظ†ط§ط¯ظٹ ط§ظ„طھط¯ط±ظٹط¨ظٹ', 'MOVEMENT LIBRARY': 'ظ…ظƒطھط¨ط© ط§ظ„ط­ط±ظƒط©', 'Open exercise demonstration': 'ط¹ط±ط¶ ط§ظ„طھظ…ط±ظٹظ†',
  'Add a secure demo video URL to this exercise to show it here.': 'ط£ط¶ظپ ط±ط§ط¨ط· ظپظٹط¯ظٹظˆ ط¢ظ…ظ† ظ„ظ‡ط°ط§ ط§ظ„طھظ…ط±ظٹظ† ظ„ط¹ط±ط¶ظ‡ ظ‡ظ†ط§.', 'Next step': 'ط§ظ„ط®ط·ظˆط© ط§ظ„طھط§ظ„ظٹط©',
  'Add exercises to the draft, then choose the client and start date.': 'ط£ط¶ظپ ط§ظ„طھظ…ط§ط±ظٹظ† ط¥ظ„ظ‰ ط§ظ„ظ…ط³ظˆط¯ط©طŒ ط«ظ… ط§ط®طھط± ط§ظ„ط¹ظ…ظٹظ„ ظˆطھط§ط±ظٹط® ط§ظ„ط¨ط¯ط،.', 'Assign program': 'ط¥ط³ظ†ط§ط¯ ط§ظ„ط¨ط±ظ†ط§ظ…ط¬',
  'programs': 'ط¨ط±ط§ظ…ط¬', 'days': 'ط£ظٹط§ظ…', 'plans': 'ط¨ط§ظ‚ط§طھ', 'Retry loading exercises': 'ط¥ط¹ط§ط¯ط© طھط­ظ…ظٹظ„ ط§ظ„طھظ…ط§ط±ظٹظ†', '&times;': 'أ—'
});

Object.assign(arabic, {
  'Better coaching.': 'طھط¯ط±ظٹط¨ ط£ظپط¶ظ„.', 'Clearer progress.': 'ظ…طھط§ط¨ط¹ط© ط£ظˆط¶ط­.', 'Welcome back': 'ظ…ط±ط­ط¨ط§ظ‹ ط¨ط¹ظˆط¯طھظƒ', 'Sign in': 'طھط³ط¬ظٹظ„ ط§ظ„ط¯ط®ظˆظ„',
  'Club': 'ط§ظ„ظ†ط§ط¯ظٹ', 'Clubs': 'ط§ظ„ط£ظ†ط¯ظٹط©', 'Workspace': 'ط§ظ„ظ†ط§ط¯ظٹ', 'Workspaces': 'ط§ظ„ط£ظ†ط¯ظٹط©', 'Club settings': 'ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ظ†ط§ط¯ظٹ',
  'Club name': 'ط§ط³ظ… ط§ظ„ظ†ط§ط¯ظٹ', 'Club link': 'ط±ط§ط¨ط· ط§ظ„ظ†ط§ط¯ظٹ', 'Choose your club, then enter your sign-in details.': 'ط§ط®طھط± ط§ظ„ظ†ط§ط¯ظٹ ط§ظ„ظ…ط±طھط¨ط· ط¨ط­ط³ط§ط¨ظƒطŒ ط«ظ… ط£ط¯ط®ظ„ ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¯ط®ظˆظ„.',
  'Choose a club': 'ط§ط®طھط± ط§ظ„ظ†ط§ط¯ظٹ', 'Search by club name': 'ط§ط¨ط­ط« ط¨ط§ط³ظ… ط§ظ„ظ†ط§ط¯ظٹ', 'Available clubs': 'ط§ظ„ط£ظ†ط¯ظٹط© ط§ظ„ظ…طھط§ط­ط©', 'Loading clubs...': 'ط¬ط§ط±ظچ طھط­ظ…ظٹظ„ ط§ظ„ط£ظ†ط¯ظٹط©...',
  'Could not load clubs right now.': 'طھط¹ط°ط± طھط­ظ…ظٹظ„ ط§ظ„ط£ظ†ط¯ظٹط© ط§ظ„ط¢ظ†.', 'Club code from your invitation': 'ط±ظ…ط² ط§ظ„ظ†ط§ط¯ظٹ ظپظٹ ط§ظ„ط¯ط¹ظˆط©',
  'Type the first two letters of the club name to search.': 'ط§ط¨ط¯ط£ ط¨ظƒطھط§ط¨ط© ط£ظˆظ„ ط­ط±ظپظٹظ† ظ…ظ† ط§ط³ظ… ط§ظ„ظ†ط§ط¯ظٹ ظ„ظ„ط¨ط­ط«.', 'No results. Try another name.': 'ظ„ط§ طھظˆط¬ط¯ ظ†طھط§ط¦ط¬. ط¬ط±ظ‘ط¨ ط§ط³ظ…ط§ظ‹ ط¢ط®ط±.',
  'Canâ€™t find your club? Open the invitation link you received.': 'ظ„ط§ طھط¬ط¯ ظ†ط§ط¯ظٹظƒطں ط§ظپطھط­ ط±ط§ط¨ط· ط§ظ„ط¯ط¹ظˆط© ط§ظ„ط°ظٹ ظˆطµظ„ظƒ.', 'Forgot password?': 'ظ†ط³ظٹطھ ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±طں',
  'First time here?': 'ط£ظˆظ„ ظ…ط±ط© ظ‡ظ†ط§طں', 'Create a club': 'ط£ظ†ط´ط¦ ظ†ط§ط¯ظٹط§ظ‹', 'For coaches starting a new club': 'ظ„ظ„ظ…ط¯ط±ط¨ظٹظ† ط§ظ„ط±ط§ط؛ط¨ظٹظ† ظپظٹ ط¨ط¯ط، ظ†ط§ط¯ظچ ط¬ط¯ظٹط¯',
  'Join as a client': 'ط§ظ†ط¶ظ… ظƒط¹ظ…ظٹظ„', 'Have an invitation from a coach? Finish registration here.': 'ظ„ط¯ظٹظƒ ط¯ط¹ظˆط© ظ…ظ† ظ…ط¯ط±ط¨طں ط£ظƒظ…ظ„ ط§ظ„طھط³ط¬ظٹظ„ ظ‡ظ†ط§.',
  'Have a staff invitation?': 'ظ„ط¯ظٹظƒ ط¯ط¹ظˆط© ظ„ظ„ط§ظ†ط¶ظ…ط§ظ… ظƒظ…ط¯ط±ط¨طں', 'Other account options': 'ط®ظٹط§ط±ط§طھ ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط£ط®ط±ظ‰', 'Reset password with a code': 'ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط¨ط±ظ…ط²',
  'Better training and client management': 'ظ…ظ†طµط© ط§ظ„طھط¯ط±ظٹط¨ ظˆط¥ط¯ط§ط±ط© ط§ظ„ط¹ظ…ظ„ط§ط،', 'Organized programs and exercises': 'ط¨ط±ط§ظ…ط¬ ظˆطھظ…ط§ط±ظٹظ† ظ…ظ†ط¸ظ…ط©',
  'Progress tracking and communication': 'ظ…طھط§ط¨ط¹ط© ط§ظ„طھظ‚ط¯ظ… ظˆط§ظ„طھظˆط§طµظ„', 'Designed for coaches and clients': 'طھط¬ط±ط¨ط© ظ…ظ†ط§ط³ط¨ط© ظ„ظ„ظ…ط¯ط±ط¨ ظˆط§ظ„ط¹ظ…ظٹظ„',
  'Manage training, clients, and progress in one place.': 'ظƒظ„ ظ…ط§ طھط­طھط§ط¬ظ‡ ظ„ط¥ط¯ط§ط±ط© ط§ظ„طھظ…ط§ط±ظٹظ† ظˆط§ظ„ط¹ظ…ظ„ط§ط، ظˆط§ظ„طھظ‚ط¯ظ… ظپظٹ ظ…ظƒط§ظ† ظˆط§ط­ط¯.',
  'Your club starts here.': 'ظ†ط§ط¯ظٹظƒ ط§ظ„طھط¯ط±ظٹط¨ظٹ ظٹط¨ط¯ط£ ظ…ظ† ظ‡ظ†ط§.', 'Your account and club data are protected by your access permissions.': 'ط¨ظٹط§ظ†ط§طھ ط­ط³ط§ط¨ظƒ ظˆظ†ط§ط¯ظٹظƒ ظ…ط­ظ…ظٹط© ط¨طµظ„ط§ط­ظٹط§طھ ط§ظ„ظˆطµظˆظ„ ط§ظ„ط®ط§طµط© ط¨ظƒ.',
  'Reason for activation or suspension': 'ط³ط¨ط¨ ط§ظ„طھظپط¹ظٹظ„ ط£ظˆ ط§ظ„طھط¹ظ„ظٹظ‚', 'Activate club': 'طھظپط¹ظٹظ„ ط§ظ„ظ†ط§ط¯ظٹ', 'Suspend club': 'طھط¹ظ„ظٹظ‚ ط§ظ„ظ†ط§ط¯ظٹ',
  'Schedule club deletion': 'ط¬ط¯ظˆظ„ط© ط­ط°ظپ ط§ظ„ظ†ط§ط¯ظٹ', 'Selected club': 'ط§ظ„ظ†ط§ط¯ظٹ ط§ظ„ظ…ط­ط¯ط¯', 'Choose a club to manage its status and usage.': 'ط§ط®طھط± ط§ظ„ظ†ط§ط¯ظٹ ظ„ط¥ط¯ط§ط±ط© ط­ط§ظ„طھظ‡ ظˆط§ط³طھط®ط¯ط§ظ…ظ‡.',
  'No clubs to display.': 'ظ„ط§ طھظˆط¬ط¯ ط£ظ†ط¯ظٹط© ظ„ط¹ط±ط¶ظ‡ط§.', 'Choose a club from the list or use Manage club in the clubs table.': 'ط§ط®طھط± ظ†ط§ط¯ظٹط§ظ‹ ظ…ظ† ط§ظ„ظ‚ط§ط¦ظ…ط© ط£ظˆ ط§ط³طھط®ط¯ظ… ط¥ط¯ط§ط±ط© ط§ظ„ظ†ط§ط¯ظٹ ظپظٹ ط¬ط¯ظˆظ„ ط§ظ„ط£ظ†ط¯ظٹط©.',
  'Training clubs': 'ط§ظ„ط£ظ†ط¯ظٹط© ط§ظ„طھط¯ط±ظٹط¨ظٹط©', 'Club management': 'ط¥ط¯ط§ط±ط© ط§ظ„ظ†ط§ط¯ظٹ', 'Platform administration': 'ط¥ط¯ط§ط±ط© ط§ظ„ظ…ظ†طµط©',
  'Create a program': 'ط¥ظ†ط´ط§ط، ط¨ط±ظ†ط§ظ…ط¬', 'Program details': 'طھظپط§طµظٹظ„ ط§ظ„ط¨ط±ظ†ط§ظ…ط¬', 'Program exercises': 'طھظ…ط§ط±ظٹظ† ط§ظ„ط¨ط±ظ†ط§ظ…ط¬', 'New title': 'ط¹ظ†ظˆط§ظ† ط¬ط¯ظٹط¯',
  'Rest seconds': 'ط§ظ„ط±ط§ط­ط© ط¨ط§ظ„ط«ظˆط§ظ†ظٹ', 'Save exercise changes': 'ط­ظپط¸ طھط¹ط¯ظٹظ„ ط§ظ„طھظ…ط±ظٹظ†', 'Reorder todayâ€™s exercises': 'طھط±طھظٹط¨ طھظ…ط§ط±ظٹظ† ط§ظ„ظٹظˆظ…',
  'Save order': 'ط­ظپط¸ ط§ظ„طھط±طھظٹط¨', 'Duplicate': 'ظ†ط³ط®', 'Save as template': 'ط­ظپط¸ ظƒظ‚ط§ظ„ط¨', 'Templates': 'ط§ظ„ظ‚ظˆط§ظ„ط¨', 'Create program from template': 'ط¥ظ†ط´ط§ط، ط¨ط±ظ†ط§ظ…ط¬ ظ…ظ† ط§ظ„ظ‚ط§ظ„ط¨',
  'Muscle groups and equipment': 'ط§ظ„ظ…ط¬ظ…ظˆط¹ط§طھ ط§ظ„ط¹ط¶ظ„ظٹط© ظˆط§ظ„ظ…ط¹ط¯ط§طھ', 'New exercise': 'طھظ…ط±ظٹظ† ط¬ط¯ظٹط¯', 'Edit details': 'طھظپط§طµظٹظ„ ط§ظ„طھط¹ط¯ظٹظ„',
  'Plan details': 'طھظپط§طµظٹظ„ ط§ظ„ط®ط·ط©', 'Plan title': 'ط¹ظ†ظˆط§ظ† ط§ظ„ط®ط·ط©', 'Meal name': 'ط§ط³ظ… ط§ظ„ظˆط¬ط¨ط©', 'Calories': 'ط§ظ„ط³ط¹ط±ط§طھ ط§ظ„ط­ط±ط§ط±ظٹط©',
  'Add meal': 'ط¥ط¶ط§ظپط© ظˆط¬ط¨ط©', 'Update meal': 'طھط­ط¯ظٹط« ط§ظ„ظˆط¬ط¨ط©', 'Delete meal': 'ط­ط°ظپ ط§ظ„ظˆط¬ط¨ط©', 'Create plan': 'ط¥ظ†ط´ط§ط، ط¨ط§ظ‚ط©', 'Manage plan': 'ط¥ط¯ط§ط±ط© ط§ظ„ط¨ط§ظ‚ط©',
  'Archive plan': 'ط£ط±ط´ظپط© ط§ظ„ط¨ط§ظ‚ط©', 'Invite a client': 'ط¯ط¹ظˆط© ط¹ظ…ظٹظ„', 'Send invitation': 'ط¥ط±ط³ط§ظ„ ط¯ط¹ظˆط©', 'Invitations': 'ط§ظ„ط¯ط¹ظˆط§طھ',
  'Coaches in this club': 'ط§ظ„ظ…ط¯ط±ط¨ظˆظ† ظپظٹ ط§ظ„ظ†ط§ط¯ظٹ', 'Resend invitation': 'ط¥ط¹ط§ط¯ط© ط¥ط±ط³ط§ظ„ ط§ظ„ط¯ط¹ظˆط©', 'Cancel invitation': 'ط¥ظ„ط؛ط§ط، ط§ظ„ط¯ط¹ظˆط©',
  'Public platform exercises': 'طھظ…ط§ط±ظٹظ† ط§ظ„ظ…ظ†طµط© ط§ظ„ط¹ط§ظ…ط©', 'Choose an exercise to edit or delete. Deletion may be rejected if it is in use.': 'ط§ط®طھط± طھظ…ط±ظٹظ†ط§ظ‹ ظ„طھط¹ط¯ظٹظ„ظ‡ ط£ظˆ ط­ط°ظپظ‡. ظ‚ط¯ ظٹظڈط±ظپط¶ ط§ظ„ط­ط°ظپ ط¥ط°ط§ ظƒط§ظ† ط§ظ„طھظ…ط±ظٹظ† ظ…ط³طھط®ط¯ظ…ط§ظ‹.',
  'Show health information (audited)': 'ط¹ط±ط¶ ط§ظ„ط¨ظٹط§ظ†ط§طھ ط§ظ„طµط­ظٹط© (ظ…ط³ط¬ظ‘ظ„ طھط¯ظ‚ظٹظ‚ظٹط§ظ‹)', 'Save assigned coach': 'ط­ظپط¸ ط§ظ„ظ…ط¯ط±ط¨ ط§ظ„ظ…ط³ط¤ظˆظ„', 'Check-in schedule': 'ظ…ظˆط¹ط¯ ط§ظ„ظ…طھط§ط¨ط¹ط©',
  'Pending payments': 'ط§ظ„ط¯ظپط¹ط§طھ ط§ظ„ظ…ط¹ظ„ظ‘ظ‚ط©', 'Record cash payment': 'طھط³ط¬ظٹظ„ ط¯ظپط¹ط© ظ†ظ‚ط¯ظٹط©', 'Refund payment': 'ط§ط³طھط±ط¯ط§ط¯ ط¯ظپط¹ط©', 'Export CSV': 'طھطµط¯ظٹط± CSV',
  'Save profile': 'ط­ظپط¸ ط§ظ„ظ…ظ„ظپ ط§ظ„ط´ط®طµظٹ', 'Delete account and hide its data': 'ط­ط°ظپ ط§ظ„ط­ط³ط§ط¨ ظˆط¥ط®ظپط§ط، ط¨ظٹط§ظ†ط§طھظ‡', 'Export my data': 'طھطµط¯ظٹط± ط¨ظٹط§ظ†ط§طھظٹ',
  'My subscriptions': 'ط§ط´طھط±ط§ظƒط§طھظٹ', 'Current subscription': 'ط§ط´طھط±ط§ظƒظٹ ط§ظ„ط­ط§ظ„ظٹ', 'Request subscription': 'ط·ظ„ط¨ ط§ط´طھط±ط§ظƒ', 'Payment details and status': 'طھظپط§طµظٹظ„ ط§ظ„ط¯ظپط¹ ظˆط­ط§ظ„طھظ‡',
  'Download PDF invoice': 'طھظ†ط²ظٹظ„ ط§ظ„ظپط§طھظˆط±ط© PDF', 'Pay by card through Paymob': 'ط§ظ„ط¯ظپط¹ ط¨ط§ظ„ط¨ط·ط§ظ‚ط© ط¹ط¨ط± Paymob', 'Generate program': 'طھظˆظ„ظٹط¯ ط¨ط±ظ†ط§ظ…ط¬',
  'Review check-ins': 'ظ…ط±ط§ط¬ط¹ط© ط§ظ„ظ…طھط§ط¨ط¹ط§طھ', 'Client details': 'طھظپط§طµظٹظ„ ط§ظ„ط¹ظ…ظٹظ„', 'Health information': 'ط§ظ„ظ…ط¹ظ„ظˆظ…ط§طھ ط§ظ„طµط­ظٹط©', 'Save settings': 'ط­ظپط¸ ط§ظ„ط¥ط¹ط¯ط§ط¯ط§طھ',
  'Save branding': 'ط­ظپط¸ ط§ظ„ظ‡ظˆظٹط©', 'Save payment methods': 'ط­ظپط¸ ط·ط±ظ‚ ط§ظ„ط¯ظپط¹', 'Workspace usage': 'ط§ط³طھط®ط¯ط§ظ… ط§ظ„ظ†ط§ط¯ظٹ', 'Audit log': 'ط³ط¬ظ„ ط§ظ„طھط¯ظ‚ظٹظ‚',
  'Your workspace': 'ظ†ط§ط¯ظٹظƒ', 'Quick access': 'ظˆطµظˆظ„ ط³ط±ظٹط¹', 'View todayâ€™s training': 'ط¹ط±ط¶ طھظ…ط±ظٹظ† ط§ظ„ظٹظˆظ…', 'Record a check-in': 'طھط³ط¬ظٹظ„ ظ…طھط§ط¨ط¹ط©',
  'Open meal plan': 'ظپطھط­ ط®ط·ط© ط§ظ„ظˆط¬ط¨ط§طھ', 'Message your coach': 'ظ…ط±ط§ط³ظ„ط© ظ…ط¯ط±ط¨ظƒ', 'Training plan': 'ط§ظ„ط®ط·ط© ط§ظ„طھط¯ط±ظٹط¨ظٹط©', 'Current program': 'ط§ظ„ط¨ط±ظ†ط§ظ…ط¬ ط§ظ„ط­ط§ظ„ظٹ',
  'Todayâ€™s exercises': 'طھظ…ط§ط±ظٹظ† ط§ظ„ظٹظˆظ…', 'Training history': 'ط³ط¬ظ„ ط§ظ„طھط¯ط±ظٹط¨', 'Past and current programs': 'ط§ظ„ط¨ط±ط§ظ…ط¬ ط§ظ„ط³ط§ط¨ظ‚ط© ظˆط§ظ„ط­ط§ظ„ظٹط©',
  'No training history yet.': 'ظ„ط§ ظٹظˆط¬ط¯ ط³ط¬ظ„ طھط¯ط±ظٹط¨ ط¨ط¹ط¯.', 'Your progress starts with a check-in': 'ظٹط¨ط¯ط£ طھظ‚ط¯ظ…ظƒ ط¨طھط³ط¬ظٹظ„ ظ…طھط§ط¨ط¹ط©',
  'Create a check-in': 'ط¥ظ†ط´ط§ط، ظ…طھط§ط¨ط¹ط©', 'Check-in history': 'ط³ط¬ظ„ ط§ظ„ظ…طھط§ط¨ط¹ط§طھ', 'No active meal plan': 'ظ„ط§ طھظˆط¬ط¯ ط®ط·ط© ظˆط¬ط¨ط§طھ ظ†ط´ط·ط©',
  'Your meal plan will show here when your coach shares one with you.': 'ط³طھط¸ظ‡ط± ط®ط·ط© ظˆط¬ط¨ط§طھظƒ ظ‡ظ†ط§ ط¹ظ†ط¯ظ…ط§ ظٹط´ط§ط±ظƒظ‡ط§ ظ…ط¯ط±ط¨ظƒ.', 'No meals have been added to this plan yet.': 'ظ„ظ… طھطھظ… ط¥ط¶ط§ظپط© ظˆط¬ط¨ط§طھ ط¥ظ„ظ‰ ظ‡ط°ظ‡ ط§ظ„ط®ط·ط© ط¨ط¹ط¯.'
});
Object.assign(arabic, {
  "Today's training": 'طھظ…ط±ظٹظ† ط§ظ„ظٹظˆظ…', "Today's exercises": 'طھظ…ط§ط±ظٹظ† ط§ظ„ظٹظˆظ…', 'After you submit your first check-in, your measurements and changes over time will appear here.': 'ط¨ط¹ط¯ ط¥ط±ط³ط§ظ„ ط£ظˆظ„ ظ…طھط§ط¨ط¹ط©طŒ ط³طھط¸ظ‡ط± ظ‚ظٹط§ط³ط§طھظƒ ظˆطھط؛ظٹط±ط§طھظƒ ط¨ظ…ط±ظˆط± ط§ظ„ظˆظ‚طھ.', 'Your plan': 'ط®ط·طھظƒ', 'kcal': 'ط³ط¹ط±ط© ط­ط±ط§ط±ظٹط©', 'total)': 'ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ†طھط§ط¦ط¬)',
  'Client details': 'طھظپط§طµظٹظ„ ط§ظ„ط¹ظ…ظٹظ„', 'Delete': 'ط­ط°ظپ', 'Import CSV': 'ط§ط³طھظٹط±ط§ط¯ CSV', 'Invitation details (email partially hidden):': 'ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¯ط¹ظˆط© ط§ظ„ط¹ط§ظ…ط© (ط§ظ„ط¨ط±ظٹط¯ ظ…ظ‚ظ†ظ‘ط¹):', 'Complete registration': 'ط¥ظƒظ…ط§ظ„ ط§ظ„طھط³ط¬ظٹظ„'
});
Object.assign(arabic, {
  'Not available': 'ط؛ظٹط± ظ…طھط§ط­', 'No items': 'ظ„ط§ طھظˆط¬ط¯ ط¹ظ†ط§طµط±', 'No details available': 'ظ„ط§ طھظˆط¬ط¯ طھظپط§طµظٹظ„', 'Yes': 'ظ†ط¹ظ…', 'No': 'ظ„ط§',
  'SuperAdmin': 'ظ…ط³ط¤ظˆظ„ ط§ظ„ظ…ظ†طµط©', 'TenantOwner': 'ظ…ط§ظ„ظƒ ط§ظ„ظ†ط§ط¯ظٹ', 'CoachStaff': 'ظ…ط¯ط±ط¨', 'Client': 'ط¹ظ…ظٹظ„', 'Active': 'ظ†ط´ط·', 'Paused': 'ظ…طھظˆظ‚ظپ ظ…ط¤ظ‚طھط§ظ‹',
  'Archived': 'ظ…ط¤ط±ط´ظپ', 'Pending': 'ظ‚ظٹط¯ ط§ظ„ط§ظ†طھط¸ط§ط±', 'Accepted': 'ظ…ظ‚ط¨ظˆظ„', 'Expired': 'ظ…ظ†طھظ‡ظٹ', 'Confirmed': 'ظ…ط¤ظƒط¯', 'Rejected': 'ظ…ط±ظپظˆط¶',
  'PendingVerification': 'ط¨ط§ظ†طھط¸ط§ط± ط§ظ„طھط­ظ‚ظ‚', 'Cancelled': 'ظ…ظ„ط؛ظٹ', 'Draft': 'ظ…ط³ظˆط¯ط©', 'Template': 'ظ‚ط§ظ„ط¨', 'Completed': 'ظ…ظƒطھظ…ظ„', 'Low': 'ظ…ظ†ط®ظپط¶',
  'Medium': 'ظ…طھظˆط³ط·', 'High': 'ظ…ط±طھظپط¹', 'Weekly': 'ط£ط³ط¨ظˆط¹ظٹ', 'BiWeekly': 'ظƒظ„ ط£ط³ط¨ظˆط¹ظٹظ†', 'Monthly': 'ط´ظ‡ط±ظٹ', 'Cash': 'ظ†ظ‚ط¯ظٹ', "View today's training": 'ط¹ط±ط¶ طھظ…ط±ظٹظ† ط§ظ„ظٹظˆظ…'
});
const english: Record<string, string> = Object.fromEntries(Object.entries(arabic).map(([key, value]) => [value, key]))

const englishUi: Record<string, string> = {
    'ظ„ظˆط­ط© SuperAdmin': 'SuperAdmin dashboard',
    'ط¥ط¯ط§ط±ط© ط§ظ„ظ…ط³ط§ط­ط§طھ': 'Club management',
    'ط§ظ„ط£ظ†ط¯ظٹط© ط§ظ„طھط¯ط±ظٹط¨ظٹط©': 'Training clubs', 'ظ…ط³ط§ط­ط§طھ ط§ظ„طھط¯ط±ظٹط¨': 'Training clubs',
    'ظ…ط³ط§ط­ط© ط§ظ„ط¥ط¯ط§ط±ط©': 'Platform administration',
    'ط¥ط¯ط§ط±ط© ظˆظ…ط±ط§ظ‚ط¨ط© ط§ظ„ظ…ظ†طµط©': 'Manage and monitor the platform',
    'ط¥ط¯ط§ط±ط© ط§ظ„ظ†ط§ط¯ظٹ': 'Manage club', 'ط¥ط¯ط§ط±ط© ظ…ط³ط§ط­ط©': 'Manage club',
    'ط§ط®طھط± ظ…ط³ط§ط­ط©': 'Choose a club', 'ظ…ط³ط§ط­ط© ط§ظ„طھط¯ط±ظٹط¨': 'Training club', 'ط§ظ„ظ†ط§ط¯ظٹ': 'Club', 'ط§ظ„ط£ظ†ط¯ظٹط©': 'Clubs',
    'ط§ط³ظ… ط§ظ„ظ…ط³ط§ط­ط©': 'Club name',
    'ط±ط§ط¨ط· ط§ظ„ظ…ط³ط§ط­ط©': 'Club link',
    'ط§ظ„ط­ط§ظ„ط©': 'Status',
    'طھط§ط±ظٹط® ط§ظ„ط¥ظ†ط´ط§ط،': 'Created',
    'ط§ظ„ط§ط³طھط®ط¯ط§ظ…': 'Usage',
    'ط§ظ„ط¹ظ…ظ„ط§ط،': 'Clients',
    'ط·ظ„ط¨ط§طھ AI ط§ظ„ظٹظˆظ…': 'AI requests today',
    'ط§ظ„طھط®ط²ظٹظ†': 'Storage',
    'ط³ط¨ط¨ ط§ظ„طھظپط¹ظٹظ„ ط£ظˆ ط§ظ„طھط¹ظ„ظٹظ‚': 'Reason for activation or suspension',
    'طھظپط¹ظٹظ„ ط§ظ„ظ…ط³ط§ط­ط©': 'Activate club',
    'طھط¹ظ„ظٹظ‚ ط§ظ„ظ…ط³ط§ط­ط©': 'Suspend club',
    'ط¬ط¯ظˆظ„ط© ط§ظ„ط­ط°ظپ': 'Schedule deletion',
    'ط·ظ„ط¨ط§طھ ط¥ظ†ط´ط§ط، ط§ظ„ظ…ط³ط§ط­ط§طھ': 'Club requests',
    'ظ„ط§ طھظˆط¬ط¯ ط·ظ„ط¨ط§طھ طھظ†طھط¸ط± ط§ظ„ظ…ظˆط§ظپظ‚ط©.': 'No workspace requests are waiting for approval.',
    'ظ…ط¤ط´ط±ط§طھ ط§ظ„ظ…ظ†طµط©': 'Platform metrics',
    'ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط³ط§ط­ط§طھ': 'Total clubs',
    'ط§ظ„ظ…ط³ط§ط­ط§طھ ط§ظ„ظ†ط´ط·ط©': 'Active clubs',
    'ط§ظ„ظ…ط³ط§ط­ط§طھ ط§ظ„ظ…ظˆظ‚ظˆظپط©': 'Suspended clubs',
    'ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط³طھط®ط¯ظ…ظٹظ†': 'Total users',
    'ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ط¹ظ…ظ„ط§ط،': 'Total clients',
    'ط§ط³طھط®ط¯ط§ظ… AI ط§ظ„ظٹظˆظ…': 'AI usage today',
    'ط¢ط®ط± ط£ط­ط¯ط§ط« ط§ظ„طھط¯ظ‚ظٹظ‚': 'Recent audit events',
    'ط­ط§ظ„ط© Webhook': 'Webhook status',
    'ط³ط¬ظ„ ط§ظ„طھط¯ظ‚ظٹظ‚': 'Audit log',
    'ط§ظ„طھظ…ط§ط±ظٹظ† ط§ظ„ط¹ط§ظ…ط©': 'Platform exercises',
    'طھط³ط¬ظٹظ„ ط§ظ„ط®ط±ظˆط¬': 'Sign out',
    'ط§ظ„ظ‚ط§ط¦ظ…ط©': 'Menu',
    'طھط­ط¯ظٹط« ط§ظ„ط¨ظٹط§ظ†ط§طھ': 'Refresh data',
    'ظ†ط¸ط±ط© ط¹ط§ظ…ط©': 'Overview',
    'ط¥ط؛ظ„ط§ظ‚ ط§ظ„ظ‚ط§ط¦ظ…ط©': 'Close menu',
    'ظپطھط­ ظ‚ط§ط¦ظ…ط© ط§ظ„ط¥ط¯ط§ط±ط©': 'Open platform menu',
    'ط¥ط¯ط§ط±ط© ط§ظ„ظ…ظ†طµط©': 'Platform administration',
    'طµظ„ط§ط­ظٹط§طھ ظ…ط³ط¤ظˆظ„ ط§ظ„ظ…ظ†طµط©': 'Platform administrator access',
    'ط·ظ„ط¨ط§طھ طھظ†طھط¸ط± ط§ظ„ظ…ظˆط§ظپظ‚ط©': 'Requests awaiting approval',
    'ط£ط­ط¯ط« ط§ظ„ظ…ط³ط§ط­ط§طھ ظˆط­ط§ظ„طھظ‡ط§': 'Recent workspaces and their status',
    'ظ„ط§ طھظˆط¬ط¯ ظ…ط³ط§ط­ط§طھ ط¨ط¹ط¯.': 'No workspaces yet.',
    'ط£ط­ط¯ط§ط« ط§ظ„ظ…ظ†طµط© ظˆط§ظ„ظ…ط³ط§ط­ط§طھ ظ„ظ„ظ‚ط±ط§ط،ط© ظپظ‚ط·.': 'Read-only platform and workspace events.',
    'ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ': 'Email address',
    'ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±': 'Password',
    'ط§ط®طھط± ط¹ظ…ظٹظ„ط§ظ‹': 'Choose a client',
    'ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ': 'No data available',
    'ظ„ط§ طھظˆط¬ط¯ ظ†طھط§ط¦ط¬': 'No results',
    'طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ': 'Loading data',
    'طھط¹ط°ط± طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ': 'Could not load data',
    'ط¥ط±ط³ط§ظ„': 'Send',
    'ط­ظپط¸': 'Save',
    'ط¥ظ„ط؛ط§ط،': 'Cancel',
    'ط§ظ„ط§ط³ظ…': 'Name', 'ط§ظ„ط¨ط±ظٹط¯': 'Email', 'ط§ظ„ظ‡ط§طھظپ': 'Phone', 'ط§ظ„ط¯ظˆط±': 'Role',
    'ظ†ط´ط·': 'Active', 'ظ…ط¹ظ„ظ‘ظ‚': 'Paused', 'ظ…ط¤ط±ط´ظپ': 'Archived',
    'ط§ظ„ظ…ط¨ظ„ط؛': 'Amount', 'ط§ظ„ط³ط¹ط±': 'Price', 'ط§ظ„ظ…ط¯ط© ط¨ط§ظ„ط£ظٹط§ظ…': 'Duration in days',
    'ط§ظ„ظ…ط¯ط© ط¨ط§ظ„ط£ط³ط§ط¨ظٹط¹': 'Duration in weeks', 'طھط§ط±ظٹط® ط§ظ„ط¨ط¯ط،': 'Start date', 'طھط§ط±ظٹط® ط§ظ„ط§ظ†طھظ‡ط§ط،': 'End date',
    'ط§ظ„ظ…ط¬ظ…ظˆط¹ط© ط§ظ„ط¹ط¶ظ„ظٹط©': 'Muscle group', 'ط§ظ„ظ…ط¹ط¯ط§طھ': 'Equipment', 'ط±ط§ط¨ط· ط§ظ„ظپظٹط¯ظٹظˆ': 'Video URL',
    'ط§ظ„طھظƒط±ط§ط±ط§طھ': 'Repetitions', 'ط§ظ„ظ…ط¬ظ…ظˆط¹ط§طھ': 'Sets', 'ط§ظ„ظٹظˆظ…': 'Day', 'ظٹظˆظ… ط§ظ„ط£ط³ط¨ظˆط¹': 'Day of week',
    'ط§ظ„ظˆط²ظ†': 'Weight', 'ط§ظ„ظˆط²ظ† ط¨ط§ظ„ظƒط¬ظ…': 'Weight (kg)', 'ط§ظ„ظ‚ظٹط§ط³ط§طھ': 'Measurements',
    'ط§ظ„ط£ظ‡ط¯ط§ظپ': 'Goals', 'ط§ظ„طھظپط§طµظٹظ„': 'Details', 'ط§ظ„ط¥ط¬ط±ط§ط،': 'Action', 'ظ†ظˆط¹ ط§ظ„ط³ط¬ظ„': 'Record type',
    'ظ…ط³طھظˆظ‰ ط§ظ„ظ…ط®ط§ط·ط±': 'Risk level', 'ط§ظ„طھظ†ط¨ظٹظ‡ط§طھ': 'Alerts', 'ط§ظ„ط·ط±ظٹظ‚ط©': 'Method', 'ط§ظ„ظ„ط؛ط©': 'Language',
    'ط­ظپط¸ ط§ظ„طھط¹ط¯ظٹظ„': 'Save changes', 'طھط­ط¯ظٹط«': 'Update', 'ط¥ط¶ط§ظپط© طھظ…ط±ظٹظ†': 'Add exercise',
    'ط­ط°ظپ ط§ظ„طھظ…ط±ظٹظ†': 'Delete exercise', 'ط­ط°ظپ ط§ظ„ط³ط¬ظ„': 'Delete record', 'ط£ط±ط´ظپط©': 'Archive',
    'ط­ط°ظپ ظ…ط³ظˆط¯ط©': 'Delete draft', 'ط¥ط±ط³ط§ظ„ ط§ظ„طھط¹ظ„ظٹظ‚': 'Send feedback', 'ظ…ظ„ط§ط­ط¸ط§طھ ط§ظ„ظ…ط¯ط±ط¨': 'Coach notes',
    'طھط³ط¬ظٹظ„ ط§ظ„ط¯ط®ظˆظ„': 'Sign in', 'طھط³ط¬ظٹظ„ ط¯ط®ظˆظ„ ظ…ط³ط¤ظˆظ„ ط§ظ„ظ…ظ†طµط©': 'Platform administrator sign in',
    'ظ…ط³ط¤ظˆظ„ ط§ظ„ظ…ظ†طµط©': 'Platform administrator', 'طھط£ظƒظٹط¯ ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ': 'Confirm email',
    'ط¥ط¹ط§ط¯ط© ط¥ط±ط³ط§ظ„ ط±ط³ط§ظ„ط© ط§ظ„طھط£ظƒظٹط¯': 'Resend confirmation email', 'ط§ظ„ط¹ظˆط¯ط© ظ„طھط³ط¬ظٹظ„ ط§ظ„ط¯ط®ظˆظ„': 'Back to sign in',
    'ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط­ط§ظ„ظٹط©': 'Current password', 'ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط¬ط¯ظٹط¯ط©': 'New password',
    'طھط؛ظٹظٹط± ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±': 'Change password', 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ط¹ظ…ظٹظ„': 'Client ID', 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ظ…ط¯ط±ط¨': 'Coach ID',
    'ظ…ط¹ط±ظ‘ظپ ط§ظ„ظ†ط§ط¯ظٹ': 'Club ID', 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ط¨ط§ظ‚ط©': 'Plan ID', 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ط§ط´طھط±ط§ظƒ': 'Subscription ID',
    'ظ…ط¹ط±ظ‘ظپ ط§ظ„ط¯ظپط¹ط©': 'Payment ID', 'ظ…ط¹ط±ظ‘ظپ ط§ظ„ط¨ط±ظ†ط§ظ…ط¬': 'Program ID', 'ط§ظ„ظ†ط§ط¯ظٹ ط§ظ„ظ…ط­ط¯ط¯': 'Selected club', 'ط§ظ„ظ…ط³ط§ط­ط© ط§ظ„ظ…ط­ط¯ط¯ط©': 'Selected club',
    'ط§ظ„ظ…ط³ط§ط­ط©': 'Club', 'ظ…ط³ط§ط­ط©': 'Club', 'ط§ظ„ظ…ط³ط§ط­ط§طھ': 'Clubs', 'ظ…ط³ط§ط­ط§طھ': 'Clubs', 'ظ…ط³ط§ط­طھظƒ': 'Your club', 'ظ…ط³ط§ط­طھظٹ': 'My club', 'ط§ظ„ط±ط§ط¨ط·': 'URL', 'ط§ظ„ط±ط§ط¨ط· ط§ظ„ظ…ط®طھطµط±': 'Slug', 'ط§ظ„ط¹ظ…ظ„ط©': 'Currency',
    'ط¢ط®ط± طھط­ط¯ظٹط«': 'Last updated', 'ظ…ظ‚ط±ظˆط،': 'Read', 'ظ†ط¹ظ…': 'Yes', 'ظ„ط§': 'No',
    'ط§ظ„طھظ…ط±ظٹظ†': 'Exercise', 'ط§ظ„ط¨ط±ظ†ط§ظ…ط¬': 'Program', 'ط®ط·ط© ط§ظ„طھظ…ط±ظٹظ†': 'Workout plan',
    'ط®ط·ط© ط§ظ„ظˆط¬ط¨ط§طھ': 'Meal plan', 'ط§ظ„ط¨ط§ظ‚ط©': 'Plan', 'ط§ظ„ط§ط´طھط±ط§ظƒ': 'Subscription', 'ط§ظ„ط¯ظپط¹ط©': 'Payment',
    'ط§ظ„ط¹ظ…ظٹظ„': 'Client', 'ط§ظ„ظ…ط¯ط±ط¨': 'Coach', 'ط§ظ„طھط§ط±ظٹط®': 'Date', 'ط§ظ„ظ…ظ„ط§ط­ط¸ط§طھ': 'Notes',
    'ط³ط¨ط¨ ط§ظ„ط¥ط¬ط±ط§ط،': 'Reason', 'ط³ط¨ط¨ ط§ظ„ط±ظپط¶': 'Rejection reason', 'ط§ظƒطھط¨ ط³ط¨ط¨ط§ظ‹ ظˆط§ط¶ط­ط§ظ‹ ظ„ظ„طھظپط¹ظٹظ„ ط£ظˆ ط§ظ„طھط¹ظ„ظٹظ‚': 'Enter a clear reason for activation or suspension', 'ط¥ط±ط³ط§ظ„ ط±ط³ط§ظ„ط©': 'Send message',
    'ط¥ظ†ط´ط§ط،': 'Create', 'ط¥ظ†ط´ط§ط، ط¨ط§ظ‚ط©': 'Create plan', 'ط¥ظ†ط´ط§ط، ط®ط·ط©': 'Create plan', 'ط¥ظ†ط´ط§ط، ط¨ط±ظ†ط§ظ…ط¬': 'Create program',
    'ط­ظپط¸ ط§ظ„ط¨ظٹط§ظ†ط§طھ': 'Save details', 'ط­ظپط¸ ط§ظ„ظ…ظ„ظپ': 'Save profile', 'ط¥ط³ظ†ط§ط¯': 'Assign', 'ط¥ط³ظ†ط§ط¯ ط§ظ„ط®ط·ط©': 'Assign plan',
    'ط¥ط¹ط§ط¯ط© ط§ظ„ظ…ط­ط§ظˆظ„ط©': 'Try again', 'ط¨ط­ط«': 'Search', 'طھطµظپظٹط©': 'Filter', 'ظ…ط³ط­ ط§ظ„ظپظ„ط§طھط±': 'Clear filters',
    'ط§ظ„ط³ط§ط¨ظ‚': 'Previous', 'ط§ظ„طھط§ظ„ظٹ': 'Next', 'طµظپط­ط©': 'Page', 'ط§ظ„ظ†طھط§ط¦ط¬': 'Results',
    'ط¬ط§ط±ظچ طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ...': 'Loading data...',
    'ظ„ط§ طھظˆط¬ط¯ ط¹ظ†ط§طµط±': 'No items', 'ظ„ط§ طھظˆط¬ط¯ ط±ط³ط§ط¦ظ„ ط¨ط¹ط¯': 'No messages yet',
    'ط§ظ„ط¨ظٹط§ظ†ط§طھ ط؛ظٹط± ظ…طھط§ط­ط©': 'Data is unavailable', 'طھط¹ط°ظ‘ط± طھط­ظ…ظٹظ„ ط§ظ„ط¨ظٹط§ظ†ط§طھ': 'Could not load data',
    'ط§ظ„طµظ„ط§ط­ظٹط© ط؛ظٹط± ظ…طھط§ط­ط©': 'Access unavailable', 'ط­ط³ط§ط¨ظƒ ظ„ط§ ظٹظ…ظ„ظƒ طµظ„ط§ط­ظٹط© طھظ†ظپظٹط° ظ‡ط°ط§ ط§ظ„ط¥ط¬ط±ط§ط،.': 'Your account is not allowed to perform this action.',
    'ط§ظ„ط®ط¯ظ…ط© ط؛ظٹط± ظ…طھط§ط­ط© ظ…ط¤ظ‚طھط§ظ‹': 'Service temporarily unavailable', 'ط§ظ„ط®ط¯ظ…ط© ط§ظ„ظ…ط·ظ„ظˆط¨ط© ظ…طھظˆظ‚ظپط© ظ…ط¤ظ‚طھط§ظ‹. ط­ط§ظˆظ„ ظ…ط±ط© ط£ط®ط±ظ‰ ط¨ط¹ط¯ ظ‚ظ„ظٹظ„.': 'The requested service is temporarily unavailable. Please try again shortly.',
    'ط§ظ†طھظ‡طھ ط¬ظ„ط³ط© ط§ظ„ط¯ط®ظˆظ„': 'Your session has expired', 'ط³ط¬ظ‘ظ„ ط§ظ„ط¯ط®ظˆظ„ ظ…ط±ط© ط£ط®ط±ظ‰ ظ„ظ„ظ…طھط§ط¨ط¹ط©.': 'Sign in again to continue.',
    'طھط¹ط°ط± ط§ظ„ط§طھطµط§ظ„ ط¨ط§ظ„ط®ط§ط¯ظ…. طھط­ظ‚ظ‚ ظ…ظ† طھط´ط؛ظٹظ„ ط§ظ„ط®ط¯ظ…ط© ظˆط§طھطµط§ظ„ ط§ظ„ط´ط¨ظƒط©.': 'Could not connect to the server. Check that the service is running and your network is available.',
    'ط·ظ„ط¨ط§طھ ظƒط«ظٹط±ط© ط®ظ„ط§ظ„ ظˆظ‚طھ ظ‚طµظٹط±': 'Too many requests', 'ط§ظ†طھط¸ط± ظ‚ظ„ظٹظ„ط§ظ‹ ظ‚ط¨ظ„ ط¥ط¹ط§ط¯ط© ط§ظ„ظ…ط­ط§ظˆظ„ط©.': 'Please wait before trying again.',
    'طھط¹ط°ظ‘ط± ط¥طھظ…ط§ظ… ط§ظ„طھط؛ظٹظٹط±': 'Could not save changes', 'طھط؛ظٹظ‘ط±طھ ط§ظ„ط¨ظٹط§ظ†ط§طھ ظ…ظ†ط° ط¢ط®ط± ظ…ط±ط© ظپطھط­طھظ‡ط§. ط­ط¯ظ‘ط« ط§ظ„طµظپط­ط© ط«ظ… ط­ط§ظˆظ„ ظ…ط¬ط¯ط¯ط§ظ‹.': 'This data changed since you opened it. Refresh the page and try again.',
    'ط±ط§ط¬ط¹ ط§ظ„ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ط¯ط®ظ„ط©': 'Check the information you entered', 'طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ط­ظ‚ظˆظ„ ط§ظ„ظ…ط·ظ„ظˆط¨ط© ظˆط§ظ„ظ‚ظٹظ… ط«ظ… ط£ط¹ط¯ ط§ظ„ظ…ط­ط§ظˆظ„ط©.': 'Check the required fields and values, then try again.',
    'ظ‚ط¯ ظٹظƒظˆظ† ط§ظ„ط¹ظ†طµط± ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ط£ظˆ ظ„ظ… ظٹط¹ط¯ ظ…طھط§ط­ط§ظ‹ ظ„ط­ط³ط§ط¨ظƒ.': 'This item may not exist or may no longer be available to your account.',
    'ط±ظ‚ظ… ط§ظ„طھطھط¨ط¹:': 'Trace ID:', 'ظ…ط®ط§ط·ط± ط§ظ„ط§ظ†ط³ط­ط§ط¨': 'Churn risk', 'ط£ط­ط¯ط§ط« Webhook': 'Webhook events',
    'ط§ط´طھط±ط§ظƒط§طھ ظ‚ط§ط±ط¨طھ ط¹ظ„ظ‰ ط§ظ„ط§ظ†طھظ‡ط§ط،': 'Subscriptions nearing expiry', 'ط§ظ„ط®ط·ط© ط§ظ„ط­ط§ظ„ظٹط©': 'Current plan',
    'ط§ظ„ط¨ط±ظ†ط§ظ…ط¬ ط§ظ„ط­ط§ظ„ظٹ': 'Current program', 'ظ„ط§ ظٹظˆط¬ط¯ ط¨ط±ظ†ط§ظ…ط¬ ط­ط§ظ„ظٹ': 'No current program',
    'ظ…ظ„ط®طµ ط§ظ„طھظ‚ط¯ظ…': 'Progress summary', 'ط³ط¬ظ„ ط§ظ„طھظ…ط§ط±ظٹظ†': 'Workout log', 'ط³ط¬ظ„ ط§ظ„ظ…طھط§ط¨ط¹ط§طھ': 'Check-in history',
    'ط§ظ„ظ…ط¹ظ„ظˆظ…ط§طھ ط§ظ„طµط­ظٹط©': 'Health information', 'ظ…ظ„ظپظٹ ط§ظ„ط´ط®طµظٹ': 'My profile', 'ظ„ظˆط­طھظٹ': 'My dashboard',
    'ظ†ط§ط¯ظٹظٹ': 'My club', 'ط§ظ„ظ…ط­ط§ط¯ط«ط§طھ': 'Conversations', 'ط§ظ„ط¥ط´ط¹ط§ط±ط§طھ': 'Notifications',
    'ط§ظ„ط§ط´طھط±ط§ظƒط§طھ ظˆط§ظ„ظ…ط¯ظپظˆط¹ط§طھ': 'Subscriptions and payments', 'ظ…ظƒطھط¨ط© ط§ظ„طھظ…ط§ط±ظٹظ†': 'Exercise library',
    'ط§ظ„ط®ط·ط· ط§ظ„ط؛ط°ط§ط¦ظٹط©': 'Meal plans', 'ط¥ط¯ط§ط±ط© ط§ظ„ط§ط´طھط±ط§ظƒط§طھ': 'Subscription management',
    'ط¥ط¯ط§ط±ط© ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ': 'Payment management', 'ظپط±ظٹظ‚ ط§ظ„طھط¯ط±ظٹط¨': 'Coaching team'
}

Object.assign(englishUi, {
  'ط؛ظٹط± ظ…طھط§ط­': 'Not available', 'ظ„ط§ طھظˆط¬ط¯ طھظپط§طµظٹظ„': 'No details available', 'Check-ins ظ„ظ„ظ…ط±ط§ط¬ط¹ط©:': 'Check-ins to review:', 'آ· ط¯ظپط¹ط§طھ ظ„ظ„طھط£ظƒظٹط¯:': 'آ· Payments to confirm:', 'آ· ط±ط³ط§ط¦ظ„ ط؛ظٹط± ظ…ظ‚ط±ظˆط،ط©:': 'آ· Unread messages:',
  'طھط­ط¯ظٹط« ط§ظ„ط®ط·ط©': 'Update plan', 'ط¥ط¶ط§ظپط© ظ…ط¯ط±ط¨': 'Add coach', 'ظƒظ„ظ…ط© ظ…ط±ظˆط± ظ…ط¤ظ‚طھط©': 'Temporary password', 'ط¥ط¶ط§ظپط©': 'Add', 'ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ط¯ط±ط¨': 'Coach details', 'ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¬ط¯ظٹط¯': 'New email', 'طھظپط¹ظٹظ„': 'Activate', 'ط¥ظٹظ‚ط§ظپ': 'Deactivate', 'ط§ظ„ط¹ظ…ظ„ط§ط، ط§ظ„ظ…ط®طµطµظˆظ†': 'Assigned clients',
  'Check-ins ظˆطھط¹ظ„ظٹظ‚ ط§ظ„ظ…ط¯ط±ط¨': 'Check-ins and coach feedback', 'ط§ظ„طھط¹ظ„ظٹظ‚': 'Feedback', 'ط§ظ„ط§ظ„طھط²ط§ظ… ظˆط§ظ„طھظ…ط§ط±ظٹظ†': 'Adherence and workouts', 'طھط­ظ…ظٹظ„ ط§ظ„ظ…ظ„ط§ط­ط¸ط§طھ': 'Load notes', 'ظ…ظ„ط§ط­ط¸ط©': 'Note', 'ط¥ط¶ط§ظپط© ظ…ظ„ط§ط­ط¸ط©': 'Add note', 'طھط£ظƒظٹط¯': 'Confirm', 'ط±ظپط¶': 'Reject', 'ط§ظ„ط³ط¨ط¨': 'Reason',
  'طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨': 'Profile photo', 'طھظ… ط§ط®طھظٹط§ط± طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨.': 'Profile photo selected.', 'ط­ظپط¸ ط§ظ„ط­ط³ط§ط¨': 'Save account', 'طھط³ط¬ظٹظ„ ط§ظ„ط®ط±ظˆط¬ ظ…ظ† ظƒظ„ ط§ظ„ط£ط¬ظ‡ط²ط©': 'Sign out of all devices', 'ظ…ظ„ظپط§طھ ط§ظ„ط­ط³ط§ط¨': 'Account files', 'طھظ†ط²ظٹظ„ طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨': 'Download profile photo', 'ط­ط°ظپ طµظˆط±ط© ط§ظ„ط­ط³ط§ط¨': 'Delete profile photo',
  'ط§ظ„ظ…ظ„ظپ ظٹط¶ظ… ط¨ظٹط§ظ†ط§طھ ط­ط³ط§ط¨ظƒ ظپظ‚ط·طŒ ظˆط±ط§ط¨ط· ط§ظ„طھظ†ط²ظٹظ„ طµط§ظ„ط­ ظ„ظ„ط§ط³طھط®ط¯ط§ظ… ظ…ط±ط© ظˆط§ط­ط¯ط© ظˆظٹظ†طھظ‡ظٹ ط¨ط¹ط¯ ط³ط§ط¹ط©. ظٹظ…ظƒظ† ط·ظ„ط¨ طھطµط¯ظٹط± ط¬ط¯ظٹط¯ ظƒظ„ 24 ط³ط§ط¹ط©.': 'The export contains only your account data. Its download link works once, expires in one hour, and a new export can be requested every 24 hours.',
  'ط·ظ„ط¨ طھطµط¯ظٹط± ط§ظ„ط¨ظٹط§ظ†ط§طھ': 'Request data export', 'ط§ظ„ط­ط§ظ„ط©:': 'Status:', 'طھظ†ط²ظٹظ„ ط§ظ„ظ…ظ„ظپ ط§ظ„ط¬ط§ظ‡ط²': 'Download completed file', 'ط³ط¬ظ„ط§طھ Check-in': 'Check-in records', 'طھط³ط¬ظٹظ„ ظˆط¬ط¨ط© ظ…ظ† ط®ط·طھظٹ': 'Log a meal from my plan', 'طھط³ط¬ظٹظ„ ط§ظ„ط§ظ„طھط²ط§ظ…': 'Log meal adherence',
  'ط³ط¬ظ„ ط§ظ„ط§ط´طھط±ط§ظƒط§طھ': 'Subscription history', 'ط·ظ„ط¨ طھط¬ط¯ظٹط¯ ط§ظ„ط§ط´طھط±ط§ظƒ': 'Request subscription renewal', 'ط³ط¨ط¨ ط§ظ„ط¥ظ„ط؛ط§ط،': 'Cancellation reason', 'ط¥ظ„ط؛ط§ط، ط§ظ„ط§ط´طھط±ط§ظƒ': 'Cancel subscription', 'طھط­ط¯ظٹط« ط§ظ„ط­ط§ظ„ط©': 'Refresh status', 'ط­ط§ظ„ط© ط§ظ„ط¯ظپط¹': 'Payment status',
  'ط³ظٹطھظ… طھط­ظˆظٹظ„ظƒ ط¥ظ„ظ‰ طµظپط­ط© Paymob ط§ظ„ط¢ظ…ظ†ط©. طھط£ظƒظٹط¯ ط§ظ„ط¯ظپط¹ ظٹط¹طھظ…ط¯ ط¹ظ„ظ‰ ط¥ط´ط¹ط§ط± ط§ظ„ط®ط§ط¯ظ… ط§ظ„ظ…ظˆط«ظ‚.': 'You will be redirected to Paymobâ€™s secure checkout. Payment is confirmed by the verified server notification.',
  'ظ„ط§ طھظˆط¬ط¯ ظ…ط­ط§ظˆظ„ط© ط¯ظپط¹ ظ…ط­ظپظˆط¸ط© ظ„ظ‡ط°ظ‡ ط§ظ„ط¬ظ„ط³ط©. ط§ظپطھط­ طµظپط­ط© ط§ظ„ط§ط´طھط±ط§ظƒط§طھ ظ„ط¨ط¯ط، ط§ظ„ط¯ظپط¹.': 'No payment attempt is saved for this session. Open subscriptions to start a payment.', 'ط§ظ„ط§ط´طھط±ط§ظƒ:': 'Subscription:', 'ط­ظپط¸ ط§ظ„طھط¹ط¯ظٹظ„ط§طھ': 'Save changes', 'ط¥ط¹ط§ط¯ط© ط§ظ„طھظˆظ„ظٹط¯ ط¨ظ…ظ„ط§ط­ط¸ط§طھ ط§ظ„ظ…ط¯ط±ط¨': 'Regenerate with coach notes',
  'ط§ط¹طھظ…ط§ط¯ ط§ظ„ظ…ط³ظˆط¯ط©': 'Approve draft', 'ط±ظپط¶ ط§ظ„ظ…ط³ظˆط¯ط©': 'Reject draft', 'ط¥ظ†ط´ط§ط، ظ…ظ„ط®طµ': 'Generate summary', 'ط³ط¬ظ„ ظ…ط®ط§ط·ط± ط§ظ„ط§ظ†ط³ط­ط§ط¨': 'Churn risk history', 'طھط¹ط¯ظٹظ„ ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¹ظ…ظٹظ„': 'Edit client details', 'ط­ط°ظپ ظ…ظ„ط§ط­ط¸ط©': 'Delete note', 'ط­ط°ظپ ط§ظ„ظ…ظ„ط§ط­ط¸ط©': 'Delete note',
  'ط¥ط¬ط±ط§ط،ط§طھ ط§ظ„ط¥ط¯ط§ط±ط©': 'Management actions', 'ط¥ط¯ط§ط±ط© ظ…ط³ط§ط­ط© طھط¯ط±ظٹط¨': 'Manage training club', 'ط§ط®طھط± ظ…ط³ط§ط­ط© ظ…ظ† ط§ظ„ظ‚ط§ط¦ظ…ط© ط£ظˆ ط§ط³طھط®ط¯ظ… ط²ط± آ«ط¥ط¯ط§ط±ط© ط§ظ„ظ…ط³ط§ط­ط©آ» ظپظٹ ط¬ط¯ظˆظ„ ط§ظ„ظ…ط³ط§ط­ط§طھ.': 'Choose a club or select Manage club from the clubs table.',
  'ط¥ط¬ط±ط§ط،ط§طھ ط­ط°ظپ ط§ظ„ظ…ط³ط§ط­ط©': 'Club deletion actions', 'ط³ظٹطھظ… ط¬ط¯ظˆظ„ط© ط§ظ„ط­ط°ظپ ط¨ط¹ط¯ ظپطھط±ط© ط§ظ„ط³ظ…ط§ط­. ط§ظƒطھط¨ ط±ط§ط¨ط· ط§ظ„ظ…ط³ط§ط­ط© ظƒظ…ط§ ظ‡ظˆ ظ„طھط£ظƒظٹط¯ ط§ظ„ط¹ظ…ظ„ظٹط©.': 'Deletion will be scheduled after the grace period. Enter the club link exactly to confirm.', 'ط¬ط¯ظˆظ„ط© ط§ظ„ط­ط°ظپ ط¨ط¹ط¯ ظپطھط±ط© ط§ظ„ط³ظ…ط§ط­': 'Schedule deletion after the grace period',
  'ظ„ط§ طھظˆط¬ط¯ ط³ط¬ظ„ط§طھ ط¨ط§ظ†طھط¸ط§ط± ط§ظ„ظ…ط±ط§ط¬ط¹ط©.': 'No records are waiting for review.', 'طھط¹ظ„ظٹظ‚ ط§ظ„ظ…ط¯ط±ط¨': 'Coach feedback', 'ط³ط¬ظ„': 'Record', 'ظ…ظ„ط®طµ ط§ظ„ظ…ط¯ظپظˆط¹ط§طھ': 'Payment summary', 'ظ…ظ†': 'From', 'ط¥ظ„ظ‰': 'To', 'طھظپط§طµظٹظ„ ط¯ظپط¹ط©': 'Payment details',
  'ظ…ط¹ظ„ظˆظ…ط§طھ طµط­ظٹط©': 'Health information', 'ط­ظپط¸ ط§ظ„طھظ‡ظٹط¦ط©': 'Save onboarding', 'ط§ظ„ظ…ظ„ظپ': 'File', 'ط§ظ„طھط­ط¯ظٹط«': 'Update', 'ط­ظپط¸ ط§ظ„ظ…ط¹ظ„ظˆظ…ط§طھ ط§ظ„طµط­ظٹط©': 'Save health information', 'طµظˆط± (ط­طھظ‰ 4)': 'Photos (up to 4)', 'ط¥ط±ط³ط§ظ„ Check-in': 'Submit check-in',
  'ط±ظ‚ظ… ط§ظ„ظ…ط¬ظ…ظˆط¹ط©': 'Set number', 'ظˆظ‚طھ ط§ظ„ط£ط¯ط§ط،': 'Performed at', 'طھط³ط¬ظٹظ„ ط§ظ„ظ…ط¬ظ…ظˆط¹ط©': 'Log set', 'طھط­ط¯ظٹط« ط§ظ„ط³ط¬ظ„': 'Update log', 'ظ‡ط°ظ‡ ط§ظ„ظ‡ظˆظٹط© طھط¸ظ‡ط± ظ„ظ„ط¹ظ…ظ„ط§ط، ظپظٹ طµظپط­ط© ط§ظ„ظ…ط³ط§ط­ط©.': 'Clients see this branding on the club page.', 'ط§ط³ظ… ط§ظ„ظ†ط§ط¯ظٹ': 'Club name', 'ط±ط§ط¨ط· ط§ظ„ط´ط¹ط§ط±': 'Logo URL', 'ط§ظ„ظ„ظˆظ† ط§ظ„ط£ط³ط§ط³ظٹ': 'Primary color', 'ط§ظ„ظ„ظˆظ† ط§ظ„ط«ط§ظ†ظˆظٹ': 'Secondary color', 'ط­ظپط¸ ط§ظ„ظ‡ظˆظٹط©': 'Save branding',
  'طھظˆظ„ظٹط¯ ط¨ط±ظ†ط§ظ…ط¬': 'Generate program', 'ط§ظ„ظ‡ط¯ظپ': 'Goal', 'ط²ظٹط§ط¯ط© ط§ظ„ظ‚ظˆط©': 'Build strength', 'ط®ط³ط§ط±ط© ط§ظ„ظˆط²ظ†': 'Weight loss', 'ط²ظٹط§ط¯ط© ط§ظ„ط¹ط¶ظ„ط§طھ': 'Build muscle', 'ظ„ظٹط§ظ‚ط© ط¹ط§ظ…ط©': 'General fitness', 'ط¥ط¹ط§ط¯ط© ط§ظ„طھط£ظ‡ظٹظ„': 'Rehabilitation', 'ط§ظ„ظ…ط³طھظˆظ‰': 'Level', 'ظ…ط¨طھط¯ط¦': 'Beginner', 'ظ…طھظˆط³ط·': 'Intermediate', 'ظ…طھظ‚ط¯ظ…': 'Advanced', 'طھظˆظ„ظٹط¯ ط§ظ„ط¨ط±ظ†ط§ظ…ط¬': 'Generate program',
  'طھظ… ط¥ظ†ط´ط§ط، ظ…ط³ظˆط¯ط© ط§ظ„ط¨ط±ظ†ط§ظ…ط¬.': 'Program draft created.', 'ط§ط¹طھظ…ط§ط¯': 'Approve', 'ط§ظ„ط­طµط© ظˆط§ظ„ظ…ط³ظˆط¯ط§طھ': 'Quota and drafts', 'ط¹ط±ط¶ ط¯ط±ط¬ط© ط§ظ„ظ…ط®ط§ط·ط±': 'View risk score', 'ط¥ط¹ط§ط¯ط© ط§ظ„ط§ط­طھط³ط§ط¨': 'Recompute', 'ظ„ط§ طھظˆط¬ط¯ ط¨ظٹط§ظ†ط§طھ ط­ط§ظ„ظٹط§ظ‹.': 'No data available.',
  'ظ…ط±ظƒط² ط§ظ„طھط­ظƒظ…': 'Control center', 'ط¨طµظˆط±ط© ط£ظˆط¶ط­': 'At a glance', 'طھط§ط¨ط¹ طµط­ط© ط§ظ„ظ…ط³ط§ط­ط§طھطŒ ط±ط§ط¬ط¹ ط§ظ„ظ†ط´ط§ط· ط§ظ„ط£ط®ظٹط±طŒ ظˆط§ظ†طھظ‚ظ„ ظ…ط¨ط§ط´ط±ط©ظ‹ ط¥ظ„ظ‰ ط§ظ„ط¥ط¬ط±ط§ط،ط§طھ ط§ظ„طھظٹ طھط­طھط§ط¬ظ‡ط§.': 'Monitor club health, review recent activity, and open the actions you need.',
  'ط§ط³طھط¹ط±ط§ط¶ ط§ظ„ظ…ط³ط§ط­ط§طھ': 'Browse clubs', 'ظ…ظ†طµط© ط§ظ„طھط¯ط±ظٹط¨ ظˆط§ظ„ط¥ط¯ط§ط±ط©': 'Training and club management', 'ط£ط±ظ‚ط§ظ… ظ…ط¬ظ…ظ‘ط¹ط© ظ„ظ…طھط§ط¨ط¹ط© ظ†ظ…ظˆ ط§ظ„ظ…ط³ط§ط­ط§طھ ظˆط§ط³طھط®ط¯ط§ظ… ط§ظ„ظ…ظ†طµط©.': 'Aggregated metrics for club growth and platform usage.', 'ظ…ظ„ط®طµ ط§ظ„ظ…ظ†طµط©': 'Platform summary', 'ظ„ط§ طھظˆط¬ط¯ ظ…ط¤ط´ط±ط§طھ ط­ط§ظ„ظٹط§ظ‹.': 'No metrics available.',
  'ط§ط¨ط­ط« ط¨ط§ظ„ط§ط³ظ… ط£ظˆ ط§ظ„ط±ط§ط¨ط·طŒ ط«ظ… ط§ظپطھط­ ط§ظ„ظ…ط³ط§ط­ط© ظ„ط¥ط¯ط§ط±ط© ط­ط§ظ„طھظ‡ط§ ظˆط§ط³طھط®ط¯ط§ظ…ظ‡ط§.': 'Search by name or link, then open a club to manage its status and usage.', 'ظ†طھظٹط¬ط©': 'Result', 'ط¥ط¯ط§ط±ط© ط§ظ„ظ…ط³ط§ط­ط©': 'Manage club', 'ط§ط³طھط®ط¯ط§ظ… ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ': 'AI usage', 'ط§ظ„ط·ظ„ط¨ط§طھ ط§ظ„ظ…ط³ط¬ظ„ط© ظ„ظ‡ط°ط§ ط§ظ„ظٹظˆظ…': 'Requests recorded today',
  'ط£ط­ط¯ط« ط¹ظ…ظ„ظٹط§طھ ط§ط³طھظ‚ط¨ط§ظ„ ظˆظ…ط¹ط§ظ„ط¬ط© ط§ظ„ط¯ظپط¹': 'Latest payment notifications received and processed', 'ظ…ط±ط§ظ‚ط¨ط© ط§ظ„ظ†ط´ط§ط·': 'Activity monitoring', 'ط³ط¬ظ„ ط§ظ„طھط¯ظ‚ظٹظ‚ ط§ظ„ط£ط®ظٹط±': 'Recent audit log', 'ط£ط­ط¯ط§ط« ط¥ط¯ط§ط±ظٹط© ظˆط£ظ…ظ†ظٹط© ط¹ظ„ظ‰ ظ…ط³طھظˆظ‰ ط§ظ„ظ…ظ†طµط©.': 'Platform security and administrative events.',
  'ط§ط³طھظƒط´ظپ ظ…ط³ط§ط­ط© طھط¯ط±ظٹط¨': 'Explore a training club', 'ط¹ط±ط¶ ط§ظ„طµظپط­ط© ظˆط§ظ„ط¨ط§ظ‚ط§طھ': 'View club and plans', 'ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† طھظˆظپط± ط§ظ„ط±ط§ط¨ط·': 'Check link availability', 'ط§ظ„ط¨ط§ظ‚ط§طھ ط§ظ„ظ†ط´ط·ط©': 'Active plans', 'ط¯ط¹ظˆط© CoachPulse': 'CoachPulse invitation'
});

Object.assign(englishUi, {
  'ظ…ظ†طµط© ط§ظ„طھط¯ط±ظٹط¨ ظˆط¥ط¯ط§ط±ط© ط§ظ„ط¹ظ…ظ„ط§ط،': 'Training and client management', 'طھط¯ط±ظٹط¨ ط£ظپط¶ظ„.': 'Better coaching.', 'ظ…طھط§ط¨ط¹ط© ط£ظˆط¶ط­.': 'Clearer progress.',
  'ظƒظ„ ظ…ط§ طھط­طھط§ط¬ظ‡ ظ„ط¥ط¯ط§ط±ط© ط§ظ„طھظ…ط§ط±ظٹظ† ظˆط§ظ„ط¹ظ…ظ„ط§ط، ظˆط§ظ„طھظ‚ط¯ظ… ظپظٹ ظ…ط³ط§ط­ط© ط¹ظ…ظ„ ظˆط§ط­ط¯ط©.': 'Manage training, clients, and progress in one place.',
  'ط¨ط±ط§ظ…ط¬ ظˆطھظ…ط§ط±ظٹظ† ظ…ظ†ط¸ظ…ط©': 'Organized programs and exercises', 'ظ…طھط§ط¨ط¹ط© ط§ظ„طھظ‚ط¯ظ… ظˆط§ظ„طھظˆط§طµظ„': 'Progress tracking and communication', 'طھط¬ط±ط¨ط© ظ…ظ†ط§ط³ط¨ط© ظ„ظ„ظ…ط¯ط±ط¨ ظˆط§ظ„ط¹ظ…ظٹظ„': 'Designed for coaches and clients',
  'ظ…ط³ط§ط­طھظƒ ط§ظ„طھط¯ط±ظٹط¨ظٹط© طھط¨ط¯ط£ ظ…ظ† ظ‡ظ†ط§': 'Your club starts here.', 'ظ…ط±ط­ط¨ط§ظ‹ ط¨ط¹ظˆط¯طھظƒ': 'Welcome back', 'ط§ط®طھط± ظ…ط³ط§ط­ط© ط§ظ„طھط¯ط±ظٹط¨ ط§ظ„ظ…ط±طھط¨ط·ط© ط¨ط­ط³ط§ط¨ظƒطŒ ط«ظ… ط£ط¯ط®ظ„ ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¯ط®ظˆظ„.': 'Choose your club, then enter your sign-in details.',
  'ظ…ط³ط§ط­ط© ط§ظ„طھط¯ط±ظٹط¨': 'Club', 'ط¬ط§ط±ظچ طھط­ظ…ظٹظ„ ط§ظ„ط£ظ†ط¯ظٹط©': 'Loading clubs...', 'طھط¹ط°ط± طھط­ظ…ظٹظ„ ط§ظ„ط£ظ†ط¯ظٹط© ط§ظ„ط¢ظ†.': 'Could not load clubs right now.', 'ط±ظ…ط² ط§ظ„ظ†ط§ط¯ظٹ ظ…ظ† ط§ظ„ط¯ط¹ظˆط©': 'Club code from your invitation',
  'ط§ط¨ط¯ط£ ط¨ظƒطھط§ط¨ط© ط£ظˆظ„ ط­ط±ظپظٹظ† ظ…ظ† ط§ط³ظ… ط§ظ„ظ†ط§ط¯ظٹ ظ„ظ„ط¨ط­ط«.': 'Type the first two letters of the club name to search.', 'ظ„ظ„ط¯ط®ظˆظ„ ط¨ط­ط³ط§ط¨ SuperAdmin': 'For SuperAdmin sign-in',
  'ظ„ط§ طھظˆط¬ط¯ ظ†طھط§ط¦ط¬. ط¬ط±ظ‘ط¨ ط§ط³ظ…ط§ظ‹ ط¢ط®ط±.': 'No results. Try another name.', 'ظ„ط§ طھط¬ط¯ ظ†ط§ط¯ظٹظƒطں ط§ظپطھط­ ط±ط§ط¨ط· ط§ظ„ط¯ط¹ظˆط© ط§ظ„ط°ظٹ ظˆطµظ„ظƒ.': 'Canâ€™t find your club? Open the invitation link you received.',
  'ظ†ط³ظٹطھ ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±طں': 'Forgot password?', 'ظ…ط³ط¤ظˆظ„ ط§ظ„ظ…ظ†طµط©طں ط§ط®طھط± آ«ظ…ط³ط¤ظˆظ„ ط§ظ„ظ…ظ†طµط©آ» ظ…ظ† ظ‚ط§ط¦ظ…ط© ط§ظ„ظ…ط³ط§ط­ط© ظˆط§طھط±ظƒ ط§ظ„ظ†ط§ط¯ظٹ ط¯ظˆظ† ط§ط®طھظٹط§ط±.': 'SuperAdmin? Choose Platform administrator and leave the club unselected.',
  'ط£ظˆظ„ ظ…ط±ط© ظ‡ظ†ط§طں': 'First time here?', 'ط£ظ†ط´ط¦ ظ…ط³ط§ط­ط© طھط¯ط±ظٹط¨': 'Create a club', 'ظ„ظ„ظ…ط¯ط±ط¨ظٹظ† ط§ظ„ط±ط§ط؛ط¨ظٹظ† ظپظٹ ط¨ط¯ط، ظ…ط³ط§ط­ط© ط¬ط¯ظٹط¯ط©': 'For coaches starting a new club',
  'ط§ظ†ط¶ظ… ظƒط¹ظ…ظٹظ„': 'Join as a client', 'ظ„ط¯ظٹظƒ ط¯ط¹ظˆط© ظ…ظ† ظ…ط¯ط±ط¨طں ط£ظƒظ…ظ„ ط§ظ„طھط³ط¬ظٹظ„ ظ‡ظ†ط§': 'Have an invitation from a coach? Finish registration here.',
  'ظ„ط¯ظٹظƒ ط¯ط¹ظˆط© ظ„ظ„ط§ظ†ط¶ظ…ط§ظ… ظƒظ…ط¯ط±ط¨طں': 'Have a staff invitation?', 'ط®ظٹط§ط±ط§طھ ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط£ط®ط±ظ‰': 'Other account options', 'ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط¨ط±ظ…ط²': 'Reset password with a code',
  'ط¨ظٹط§ظ†ط§طھ ط­ط³ط§ط¨ظƒ ظˆظ…ط³ط§ط­ط© ط§ظ„طھط¯ط±ظٹط¨ ظ…ط­ظ…ظٹط© ط¨طµظ„ط§ط­ظٹط§طھ ط§ظ„ظˆطµظˆظ„ ط§ظ„ط®ط§طµط© ط¨ظƒ.': 'Your account and club data are protected by your access permissions.',
  'ط³ط¬ظ‘ظ„ ط§ظ„ط¯ط®ظˆظ„ ط§ظ„ط¢ظ† ط¨ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط¬ط¯ظٹط¯ط©.': 'Sign in with your new password.', 'ظٹظ„ط²ظ… طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ظ…ط±ظˆط± ط¬ط¯ظٹط¯ط© ظ‚ط¨ظ„ ط§ط³طھط®ط¯ط§ظ… ظ„ظˆط­ط© ط§ظ„طھط­ظƒظ….': 'Set a new password before using the dashboard.',
  'طھط£ظƒظٹط¯ ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط¬ط¯ظٹط¯ط©': 'Confirm new password', 'ط§ظ„ط±ظ…ط²': 'Code', 'طھظپط§طµظٹظ„ ط§ظ„ط¨ط±ظ†ط§ظ…ط¬': 'Program details', 'طھظ…ط§ط±ظٹظ† ط§ظ„ط¨ط±ظ†ط§ظ…ط¬': 'Program exercises',
  'ط¹ظ†ظˆط§ظ† ط¬ط¯ظٹط¯': 'New title', 'ط±ط§ط­ط© ط¨ط§ظ„ط«ظˆط§ظ†ظٹ': 'Rest seconds', 'ط­ظپط¸ طھط¹ط¯ظٹظ„ ط§ظ„طھظ…ط±ظٹظ†': 'Save exercise changes', 'طھط±طھظٹط¨ طھظ…ط§ط±ظٹظ† ط§ظ„ظٹظˆظ…': 'Reorder todayâ€™s exercises',
  'ط­ظپط¸ ط§ظ„طھط±طھظٹط¨': 'Save order', 'ظ†ط³ط®': 'Duplicate', 'ط­ظپط¸ ظƒظ‚ط§ظ„ط¨': 'Save as template', 'ط§ظ„ظ‚ظˆط§ظ„ط¨': 'Templates', 'ط¥ظ†ط´ط§ط، ط¨ط±ظ†ط§ظ…ط¬ ظ…ظ† ط§ظ„ظ‚ط§ظ„ط¨': 'Create program from template',
  'ط§ظ„ظ…ط¬ظ…ظˆط¹ط§طھ ظˆط§ظ„ط£ط¯ظˆط§طھ': 'Muscle groups and equipment', 'طھظ…ط±ظٹظ† ط¬ط¯ظٹط¯': 'New exercise', 'طھظپط§طµظٹظ„ ط£ظˆ طھط¹ط¯ظٹظ„': 'Edit details', 'طھظپط§طµظٹظ„ ط§ظ„ط®ط·ط©': 'Plan details',
  'ط¹ظ†ظˆط§ظ† ط§ظ„ط®ط·ط©': 'Plan title', 'ط§ط³ظ… ط§ظ„ظˆط¬ط¨ط©': 'Meal name', 'ط§ظ„ط³ط¹ط±ط§طھ': 'Calories', 'ط¥ط¶ط§ظپط© ظˆط¬ط¨ط©': 'Add meal', 'طھط­ط¯ظٹط« ط§ظ„ظˆط¬ط¨ط©': 'Update meal', 'ط­ط°ظپ ط§ظ„ظˆط¬ط¨ط©': 'Delete meal',
  'ط§ط³ظ… ط§ظ„ط¨ط§ظ‚ط©': 'Plan name', 'ط¥ط¯ط§ط±ط© ط¨ط§ظ‚ط©': 'Manage plan', 'ط£ط±ط´ظپط© ط§ظ„ط¨ط§ظ‚ط©': 'Archive plan', 'ط¯ط¹ظˆط© ط¹ظ…ظٹظ„': 'Invite a client', 'ط¥ط±ط³ط§ظ„ ط¯ط¹ظˆط©': 'Send invitation',
  'ط±ظ…ط² ط§ظ„ط¯ط¹ظˆط© ظ„ظ„طھط·ظˆظٹط±:': 'Development invitation token:', 'ظپطھط­ طھط³ط¬ظٹظ„ ط§ظ„ط¹ظ…ظٹظ„': 'Open client registration', 'ط§ظ„ط¯ط¹ظˆط§طھ': 'Invitations', 'ط§ظ„ظ…ط¯ط±ط¨ظˆظ† ظپظٹ ط§ظ„ظ…ط³ط§ط­ط©': 'Coaches in this club',
  'ط¥ط¹ط§ط¯ط© ط§ظ„ط¥ط±ط³ط§ظ„': 'Resend invitation', 'ط¥ظ„ط؛ط§ط، ط§ظ„ط¯ط¹ظˆط©': 'Cancel invitation', 'طھظ…ط§ط±ظٹظ† ط§ظ„ظ…ظ†طµط© ط§ظ„ط¹ط§ظ…ط©': 'Public platform exercises',
  'ط§ط®طھط± طھظ…ط±ظٹظ†ط§ظ‹ ظ„طھط¹ط¯ظٹظ„ظ‡ ط£ظˆ ط­ط°ظپظ‡. ط§ظ„ط­ط°ظپ ظ‚ط¯ ظٹظڈط±ظپط¶ ط¥ط°ط§ ظƒط§ظ† ط§ظ„طھظ…ط±ظٹظ† ظ…ط³طھط®ط¯ظ…ط§ظ‹.': 'Choose an exercise to edit or delete. Deletion may be rejected if it is in use.',
  'ط¥ظٹظ‚ط§ظپ ظ…ط¤ظ‚طھ': 'Paused', 'ط¹ط±ط¶ ط§ظ„ط¨ظٹط§ظ†ط§طھ ط§ظ„طµط­ظٹط© (ظ…ط³ط¬ظ‘ظ„ طھط¯ظ‚ظٹظ‚ظٹط§ظ‹)': 'View health information (audited)', 'ط­ظپط¸ ط§ظ„ظ…ط¯ط±ط¨ ط§ظ„ظ…ط³ط¤ظˆظ„': 'Save assigned coach',
  'طھظƒط±ط§ط± Check-in': 'Check-in frequency', 'ظٹظˆظ… ط§ظ„ط£ط³ط¨ظˆط¹': 'Day of week', 'ط­ظپط¸ ط§ظ„ظ…ظˆط¹ط¯': 'Save schedule', 'ط§ظ„ط¯ظپط¹ط§طھ ط§ظ„ظ…ط¹ظ„ظ‘ظ‚ط©': 'Pending payments',
  'طھط³ط¬ظٹظ„ ط¯ظپط¹ط© ظƒط§ط´': 'Record cash payment', 'طھط³ط¬ظٹظ„ ط§ظ„ط¯ظپط¹ط©': 'Record payment', 'ط§ط³طھط±ط¯ط§ط¯ ط¯ظپط¹ط©': 'Refund payment', 'طھظ†ظپظٹط° ط§ظ„ط§ط³طھط±ط¯ط§ط¯': 'Process refund',
  'طھطµط¯ظٹط± CSV': 'Export CSV', 'ط­ط°ظپ ط§ظ„ط­ط³ط§ط¨ ظˆط¥ط®ظپط§ط، ط¨ظٹط§ظ†ط§طھظ‡': 'Delete account and hide its data', 'طھطµط¯ظٹط± ط¨ظٹط§ظ†ط§طھظٹ': 'Export my data',
  'ط§ظ„ط¨ط§ظ‚ط§طھ': 'Plans', 'ط§ط´طھط±ط§ظƒظٹ ط§ظ„ط­ط§ظ„ظٹ': 'Current subscription', 'ط¥ظ†ط´ط§ط، ط·ظ„ط¨ ط§ط´طھط±ط§ظƒ': 'Request subscription', 'ظ…ط¯ظپظˆط¹ط§طھظٹ': 'My payments',
  'طھظپط§طµظٹظ„ ط§ظ„ط¯ظپط¹ ظˆط­ط§ظ„طھظ‡': 'Payment details and status', 'طھظ†ط²ظٹظ„ ط§ظ„ظپط§طھظˆط±ط© PDF': 'Download PDF invoice', 'ط§ظ„ط¯ظپط¹ ط¨ط§ظ„ط¨ط·ط§ظ‚ط© ط¹ط¨ط± Paymob': 'Pay by card through Paymob',
  'ظ„ط§ طھظˆط¬ط¯ ط®ط·ط© ظˆط¬ط¨ط§طھ ظ†ط´ط·ط©': 'No active meal plan', 'ط­طµط© ط§ظ„ط§ط³طھط®ط¯ط§ظ…': 'Usage quota', 'ط¥ط¯ط§ط±ط© ط§ظ„ط¹ظ…ظ„ط§ط، ط§ظ„ظ…ط¤ط³ط³ظٹظٹظ†': 'Club management',
  'ط¥ط¯ط§ط±ط© ط§ظ„ظ…ط³ط§ط­ط©': 'Manage club', 'ط§ط®طھط± ط§ظ„ظ…ط³ط§ط­ط© ط«ظ… ظ†ظپظ‘ط° ط§ظ„ط¥ط¬ط±ط§ط، ط§ظ„ظ…ظ†ط§ط³ط¨. طھط¸ظ‡ط± طھظپط§طµظٹظ„ظ‡ط§ ظˆط§ط³طھط®ط¯ط§ظ…ظ‡ط§ ظ‡ظ†ط§.': 'Choose a club to manage its status and usage.',
  'ط§ط®طھط± ظ…ط³ط§ط­ط© ط§ظ„طھط¯ط±ظٹط¨': 'Choose a club', 'ظ„ط§ طھظˆط¬ط¯ ظ…ط³ط§ط­ط§طھ ظ„ط¹ط±ط¶ظ‡ط§.': 'No clubs to display.', 'ط§ط³طھط®ط¯ط§ظ… ط§ظ„ط°ظƒط§ط، ط§ظ„ط§طµط·ظ†ط§ط¹ظٹ': 'AI usage',
  'ظ…ط±ط§ظ‚ط¨ط© ط§ظ„ظ†ط´ط§ط·': 'Activity monitoring', 'ط³ط¬ظ„ ط§ظ„طھط¯ظ‚ظٹظ‚ ط§ظ„ط£ط®ظٹط±': 'Recent audit log', 'ط£ط­ط¯ط§ط« ط¥ط¯ط§ط±ظٹط© ظˆط£ظ…ظ†ظٹط© ط¹ظ„ظ‰ ظ…ط³طھظˆظ‰ ط§ظ„ظ…ظ†طµط©.': 'Platform security and administrative events.',
  'ط§ط³طھظƒط´ظپ ظ…ط³ط§ط­ط© طھط¯ط±ظٹط¨': 'Explore a training club', 'ط¹ط±ط¶ ط§ظ„طµظپط­ط© ظˆط§ظ„ط¨ط§ظ‚ط§طھ': 'View club and plans', 'ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† طھظˆظپط± ط§ظ„ط±ط§ط¨ط·': 'Check link availability',
  'ط§ظ„ط¨ط§ظ‚ط§طھ ط§ظ„ظ†ط´ط·ط©': 'Active plans', 'ط¯ط¹ظˆط© CoachPulse': 'CoachPulse invitation'
});
Object.assign(englishUi, {
  'ط­ط°ظپ': 'Delete', 'ط§ط³طھظٹط±ط§ط¯ CSV': 'Import CSV', 'طھظپط§طµظٹظ„ ط§ظ„ط¹ظ…ظٹظ„': 'Client details', 'ط¨ظٹط§ظ†ط§طھ ط§ظ„ط¯ط¹ظˆط© ط§ظ„ط¹ط§ظ…ط© (ط§ظ„ط¨ط±ظٹط¯ ظ…ظ‚ظ†ظ‘ط¹):': 'Invitation details (email partially hidden):', 'ط¥ظƒظ…ط§ظ„ ط§ظ„طھط³ط¬ظٹظ„': 'Complete registration'
});
Object.assign(arabic, {
  conversations: '\u0645\u062d\u0627\u062f\u062b\u0627\u062a', conversation: '\u0645\u062d\u0627\u062f\u062b\u0629',
  'Your conversations': '\u0645\u062d\u0627\u062f\u062b\u0627\u062a\u0643',
  'Follow your client conversations and reply from one place.': '\u062a\u0627\u0628\u0639 \u0645\u062d\u0627\u062f\u062b\u0627\u062a \u0639\u0645\u0644\u0627\u0626\u0643 \u0648\u0631\u062f \u0639\u0644\u064a\u0647\u0645 \u0645\u0646 \u0645\u0643\u0627\u0646 \u0648\u0627\u062d\u062f.',
  'Messages and client updates will appear here.': '\u0633\u062a\u0638\u0647\u0631 \u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u0648\u062a\u062d\u062f\u064a\u062b\u0627\u062a \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0647\u0646\u0627.',
  'A quick look at your training and account.': '\u0646\u0638\u0631\u0629 \u0633\u0631\u064a\u0639\u0629 \u0639\u0644\u0649 \u062a\u062f\u0631\u064a\u0628\u0643 \u0648\u062d\u0633\u0627\u0628\u0643.',
  'Your next workout starts here': '\u062a\u0645\u0631\u064a\u0646\u0643 \u0627\u0644\u0642\u0627\u062f\u0645 \u064a\u0628\u062f\u0623 \u0645\u0646 \u0647\u0646\u0627',
  'Keep your plan close, stay in touch with your coach, and celebrate each step forward.': '\u062a\u0627\u0628\u0639 \u062e\u0637\u062a\u0643\u060c \u0648\u062a\u0648\u0627\u0635\u0644 \u0645\u0639 \u0645\u062f\u0631\u0628\u0643\u060c \u0648\u0627\u062d\u062a\u0641\u0644 \u0628\u0643\u0644 \u062e\u0637\u0648\u0629.',
  "Today's training": '\u062a\u0645\u0631\u064a\u0646 \u0627\u0644\u064a\u0648\u0645', Subscription: '\u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643',
  'Unread messages': '\u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u063a\u064a\u0631 \u0627\u0644\u0645\u0642\u0631\u0648\u0621\u0629',
  'Next check-in': '\u0627\u0644\u0645\u062a\u0627\u0628\u0639\u0629 \u0627\u0644\u0642\u0627\u062f\u0645\u0629',
  'No program': '\u0644\u0627 \u062a\u0648\u062c\u062f \u062e\u0637\u0629 \u062a\u062f\u0631\u064a\u0628', 'Not active': '\u063a\u064a\u0631 \u0646\u0634\u0637',
  'Not scheduled': '\u063a\u064a\u0631 \u0645\u062d\u062f\u062f\u0629', 'Rest day': '\u064a\u0648\u0645 \u0631\u0627\u062d\u0629',
  exercises: '\u062a\u0645\u0627\u0631\u064a\u0646', 'Online now': '\u0645\u062a\u0635\u0644 \u0627\u0644\u0622\u0646', Offline: '\u063a\u064a\u0631 \u0645\u062a\u0635\u0644',
  'Type a client name': '\u0627\u0628\u062d\u062b \u0628\u0627\u0633\u0645 \u0627\u0644\u0639\u0645\u064a\u0644',
  'Unread only': '\u063a\u064a\u0631 \u0627\u0644\u0645\u0642\u0631\u0648\u0621\u0629 \u0641\u0642\u0637',
  'Show all conversations': '\u0639\u0631\u0636 \u0643\u0644 \u0627\u0644\u0645\u062d\u0627\u062f\u062b\u0627\u062a',
  'Select a conversation': '\u0627\u062e\u062a\u0631 \u0645\u062d\u0627\u062f\u062b\u0629',
  'Search clients': '\u0627\u0628\u062d\u062b \u0639\u0646 \u0639\u0645\u064a\u0644', Inbox: '\u0635\u0646\u062f\u0648\u0642 \u0627\u0644\u0648\u0627\u0631\u062f',
  'Client conversation': '\u0645\u062d\u0627\u062f\u062b\u0629 \u0627\u0644\u0639\u0645\u064a\u0644',
  'Mark as read': '\u062a\u0639\u0644\u064a\u0645 \u0643\u0645\u0642\u0631\u0648\u0621\u0629', Unread: '\u063a\u064a\u0631 \u0645\u0642\u0631\u0648\u0621\u0629'
});

Object.assign(arabic, {
  'Subscriptions & payments': '\u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643\u0627\u062a \u0648\u0627\u0644\u0645\u062f\u0641\u0648\u0639\u0627\u062a',
  'Choose a plan, submit payment, then follow its review status.': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629\u060c \u0623\u0631\u0633\u0644 \u0627\u0644\u062f\u0641\u0639\u0629\u060c \u0648\u062a\u0627\u0628\u0639 \u062d\u0627\u0644\u062a\u0647\u0627 \u062d\u062a\u0649 \u0627\u0644\u062a\u0623\u0643\u064a\u062f.',
  'Choose a plan': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629', 'Compare the active plans offered by your club.': '\u0642\u0627\u0631\u0646 \u0627\u0644\u0628\u0627\u0642\u0627\u062a \u0627\u0644\u0646\u0634\u0637\u0629 \u0627\u0644\u0645\u062a\u0627\u062d\u0629 \u0644\u062f\u064a\u0643.',
  'No active plans are available': '\u0644\u0627 \u062a\u0648\u062c\u062f \u0628\u0627\u0642\u0627\u062a \u0646\u0634\u0637\u0629 \u062d\u0627\u0644\u064a\u0627\u064b',
  'Ask your coach or club owner to publish a plan before you request a subscription.': '\u0627\u0637\u0644\u0628 \u0645\u0646 \u0645\u062f\u0631\u0628\u0643 \u0623\u0648 \u0645\u0627\u0644\u0643 \u0627\u0644\u0646\u0627\u062f\u064a \u0625\u0636\u0627\u0641\u0629 \u0628\u0627\u0642\u0629 \u0642\u0628\u0644 \u0625\u0646\u0634\u0627\u0621 \u0637\u0644\u0628.',
  'Training plan': '\u0628\u0627\u0642\u0629 \u062a\u062f\u0631\u064a\u0628', days: '\u064a\u0648\u0645', 'Continue to payment': '\u0627\u0644\u0645\u062a\u0627\u0628\u0639\u0629 \u0644\u0644\u062f\u0641\u0639',
  'Creating request?': '\u062c\u0627\u0631\u064d \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0637\u0644\u0628...', 'Request already pending': '\u0644\u062f\u064a\u0643 \u0637\u0644\u0628 \u0645\u0639\u0644\u0642',
  'You already have a pending request for this plan. Check payment history below or choose another plan.': '\u0644\u062f\u064a\u0643 \u0637\u0644\u0628 \u0645\u0639\u0644\u0642 \u0644\u0647\u0630\u0647 \u0627\u0644\u0628\u0627\u0642\u0629. \u0631\u0627\u062c\u0639 \u0633\u062c\u0644 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0623\u0648 \u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0623\u062e\u0631\u0649.',
  'Your request is ready. Choose a payment method below.': '\u062c\u0627\u0647\u0632\u064a\u0646 \u0637\u0644\u0628 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643. \u0627\u062e\u062a\u0631\u064a \u0637\u0631\u064a\u0642\u0629 \u0627\u0644\u062f\u0641\u0639 \u0623\u062f\u0646\u0627\u0647.',
  'Could not start the subscription.': '\u062a\u0639\u0630\u0631 \u0625\u0646\u0634\u0627\u0621 \u0637\u0644\u0628 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643.',
  'Current subscription': '\u0627\u0634\u062a\u0631\u0627\u0643\u064a \u0627\u0644\u062d\u0627\u0644\u064a', 'Your plan becomes active after the payment is confirmed.': '\u064a\u062a\u0641\u0639\u0644 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643 \u0628\u0639\u062f \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062f\u0641\u0639.',
  'No active subscription': '\u0644\u0627 \u064a\u0648\u062c\u062f \u0627\u0634\u062a\u0631\u0627\u0643 \u0646\u0634\u0637', 'Choose a plan above to get started. A request remains pending until your payment is confirmed.': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0644\u0644\u0628\u062f\u0621. \u064a\u0638\u0644 \u0627\u0644\u0637\u0644\u0628 \u0645\u0639\u0644\u0642\u0627\u064b \u062d\u062a\u0649 \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062f\u0641\u0639.',
  Status: '\u0627\u0644\u062d\u0627\u0644\u0629', Started: '\u0628\u062f\u0623 \u0641\u064a', 'Renews or ends': '\u0627\u0644\u062a\u062c\u062f\u064a\u062f \u0623\u0648 \u0627\u0644\u0627\u0646\u062a\u0647\u0627\u0621',
  'Choose how to pay': '\u0627\u062e\u062a\u0631 \u0637\u0631\u064a\u0642\u0629 \u0627\u0644\u062f\u0641\u0639', 'Pay online with Paymob': '\u0627\u062f\u0641\u0639 \u0625\u0644\u0643\u062a\u0631\u0648\u0646\u064a\u0627\u064b \u0639\u0628\u0631 Paymob',
  'Continue to the secure checkout. Your subscription activates after the server confirms payment.': '\u0623\u0643\u0645\u0644 \u0627\u0644\u062f\u0641\u0639 \u0639\u0628\u0631 \u0627\u0644\u0635\u0641\u062d\u0629 \u0627\u0644\u0622\u0645\u0646\u0629. \u064a\u062a\u0641\u0639\u0644 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643 \u0628\u0639\u062f \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062e\u0627\u062f\u0645.',
  'Pay by InstaPay': '\u0627\u062f\u0641\u0639 \u0639\u0628\u0631 InstaPay', 'Transfer the exact plan amount, then upload the receipt for club review.': '\u062d\u0648\u0651\u0644 \u0642\u064a\u0645\u0629 \u0627\u0644\u0628\u0627\u0642\u0629 \u0643\u0627\u0645\u0644\u0629\u060c \u062b\u0645 \u0627\u0631\u0641\u0639 \u0627\u0644\u0625\u064a\u0635\u0627\u0644 \u0644\u0645\u0631\u0627\u062c\u0639\u0629 \u0627\u0644\u0646\u0627\u062f\u064a.',
  'InstaPay is not enabled by this club.': '\u062e\u062f\u0645\u0629 InstaPay \u063a\u064a\u0631 \u0645\u0641\u0639\u0644\u0629 \u0644\u062f\u0649 \u0627\u0644\u0646\u0627\u062f\u064a.',
  'Transfer reference': '\u0631\u0642\u0645 \u0645\u0631\u062c\u0639 \u0627\u0644\u062a\u062d\u0648\u064a\u0644', Amount: '\u0627\u0644\u0645\u0628\u0644\u063a', Receipt: '\u0625\u064a\u0635\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644',
  'Upload a screenshot or PDF of your transfer.': '\u0627\u0631\u0641\u0639 \u0635\u0648\u0631\u0629 \u0623\u0648 \u0645\u0644\u0641 PDF \u0644\u0644\u062a\u062d\u0648\u064a\u0644.', 'Receipt uploaded.': '\u062a\u0645 \u0631\u0641\u0639 \u0627\u0644\u0625\u064a\u0635\u0627\u0644.', 'Could not upload the receipt.': '\u062a\u0639\u0630\u0631 \u0631\u0641\u0639 \u0627\u0644\u0625\u064a\u0635\u0627\u0644.',
  'Submitting?': '\u062c\u0627\u0631\u064d \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644...', 'Submit transfer for review': '\u0623\u0631\u0633\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644 \u0644\u0644\u0645\u0631\u0627\u062c\u0639\u0629',
  'Transfer submitted. The club will review it before activating your subscription.': '\u062a\u0645 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644. \u0633\u064a\u0631\u0627\u062c\u0639\u0647 \u0627\u0644\u0646\u0627\u062f\u064a \u0642\u0628\u0644 \u062a\u0641\u0639\u064a\u0644 \u0627\u0634\u062a\u0631\u0627\u0643\u0643.',
  'Could not submit the transfer.': '\u062a\u0639\u0630\u0631 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644.', 'Payment history': '\u0633\u062c\u0644 \u0627\u0644\u062f\u0641\u0639\u0627\u062a',
  'See payment status and download invoices for confirmed payments.': '\u062a\u0627\u0628\u0639 \u062d\u0627\u0644\u0629 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0648\u062d\u0645\u0651\u0644 \u0641\u0648\u0627\u062a\u064a\u0631 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0627\u0644\u0645\u0624\u0643\u062f\u0629.',
  'No payments yet. Payments you submit will appear here.': '\u0644\u0627 \u062a\u0648\u062c\u062f \u062f\u0641\u0639\u0627\u062a \u0628\u0639\u062f. \u0633\u062a\u0638\u0647\u0631 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0627\u0644\u062a\u064a \u062a\u0631\u0633\u0644\u0647\u0627 \u0647\u0646\u0627.',
  'Subscription payment': '\u062f\u0641\u0639\u0629 \u0627\u0634\u062a\u0631\u0627\u0643', 'Subscription history & changes': '\u0633\u062c\u0644 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643 \u0648\u0625\u062f\u0627\u0631\u062a\u0647',
  'Review past requests, request a renewal, or cancel an active subscription.': '\u0631\u0627\u062c\u0639 \u0637\u0644\u0628\u0627\u062a\u0643\u060c \u0627\u0637\u0644\u0628 \u062a\u062c\u062f\u064a\u062f\u0627\u064b\u060c \u0623\u0648 \u0623\u0644\u063a\u0650 \u0627\u0634\u062a\u0631\u0627\u0643\u0627\u064b \u0646\u0634\u0637\u0627\u064b.',
  'No subscription requests yet. Choose a plan above to begin.': '\u0644\u0627 \u062a\u0648\u062c\u062f \u0637\u0644\u0628\u0627\u062a \u0627\u0634\u062a\u0631\u0627\u0643 \u0628\u0639\u062f. \u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0644\u0644\u0628\u062f\u0621.',
  'Request submitted': '\u062a\u0627\u0631\u064a\u062e \u0627\u0644\u0637\u0644\u0628', 'Request renewal': '\u0637\u0644\u0628 \u062a\u062c\u062f\u064a\u062f', 'Choose a plan to renew or change your subscription': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0644\u0644\u062a\u062c\u062f\u064a\u062f \u0623\u0648 \u062a\u063a\u064a\u064a\u0631 \u0627\u0634\u062a\u0631\u0627\u0643\u0643',
  'Renewal request created. Complete payment to extend your access.': '\u062a\u0645 \u0625\u0646\u0634\u0627\u0621 \u0637\u0644\u0628 \u0627\u0644\u062a\u062c\u062f\u064a\u062f. \u0623\u0643\u0645\u0644 \u0627\u0644\u062f\u0641\u0639 \u0644\u062a\u0645\u062f\u064a\u062f \u0627\u0644\u0648\u0635\u0648\u0644.',
  'Could not request renewal.': '\u062a\u0639\u0630\u0631 \u0637\u0644\u0628 \u0627\u0644\u062a\u062c\u062f\u064a\u062f.', 'Why are you cancelling?': '\u0645\u0627 \u0633\u0628\u0628 \u0627\u0644\u0625\u0644\u063a\u0627\u0621\u061f',
  'Confirm cancellation': '\u062a\u0623\u0643\u064a\u062f \u0627\u0644\u0625\u0644\u063a\u0627\u0621', 'Keep subscription': '\u0627\u0644\u0627\u062d\u062a\u0641\u0627\u0638 \u0628\u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643',
  'Subscription cancelled.': '\u062a\u0645 \u0625\u0644\u063a\u0627\u0621 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643.', 'Could not cancel subscription.': '\u062a\u0639\u0630\u0631 \u0625\u0644\u063a\u0627\u0621 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643.',
  'Cancelling stops renewal; your current access remains subject to the club?s subscription policy.': '\u064a\u0648\u0642\u0641 \u0627\u0644\u0625\u0644\u063a\u0627\u0621 \u0627\u0644\u062a\u062c\u062f\u064a\u062f\u060c \u0648\u064a\u0638\u0644 \u0627\u0644\u0648\u0635\u0648\u0644 \u0627\u0644\u062d\u0627\u0644\u064a \u062e\u0627\u0636\u0639\u0627\u064b \u0644\u0633\u064a\u0627\u0633\u0629 \u0627\u0644\u0646\u0627\u062f\u064a.',
  'Payment details': '\u062a\u0641\u0627\u0635\u064a\u0644 \u0627\u0644\u062f\u0641\u0639\u0629', 'Select a payment to see its verified status and invoice options.': '\u0627\u062e\u062a\u0631 \u062f\u0641\u0639\u0629 \u0644\u0639\u0631\u0636 \u062d\u0627\u0644\u062a\u0647\u0627 \u0627\u0644\u0645\u0624\u0643\u062f\u0629 \u0648\u062e\u064a\u0627\u0631\u0627\u062a \u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629.',
  'There are no payments to review yet.': '\u0644\u0627 \u062a\u0648\u062c\u062f \u062f\u0641\u0639\u0627\u062a \u0644\u0639\u0631\u0636\u0647\u0627 \u062d\u0627\u0644\u064a\u0627\u064b.', 'Verified payment status': '\u062d\u0627\u0644\u0629 \u0627\u0644\u062f\u0641\u0639 \u0627\u0644\u0645\u0624\u0643\u062f\u0629',
  'This status comes from the server; a return-page message alone does not confirm payment.': '\u0645\u0635\u062f\u0631 \u0627\u0644\u062d\u0627\u0644\u0629 \u0647\u0648 \u0627\u0644\u062e\u0627\u062f\u0645. \u0639\u0648\u062f\u0629 \u0635\u0641\u062d\u0629 \u0627\u0644\u062f\u0641\u0639 \u0644\u0627 \u062a\u0639\u0646\u064a \u062a\u0623\u0643\u064a\u062f\u0647.',
  'Refresh status': '\u062a\u062d\u062f\u064a\u062b \u0627\u0644\u062d\u0627\u0644\u0629', 'Download invoice (PDF)': '\u062a\u0646\u0632\u064a\u0644 \u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629 (PDF)', 'Invoice is not available yet.': '\u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629 \u063a\u064a\u0631 \u0645\u062a\u0627\u062d\u0629 \u062d\u062a\u0649 \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062f\u0641\u0639.',
  'The payment provider returned an invalid checkout link.': '\u0631\u0627\u0628\u0637 \u0627\u0644\u062f\u0641\u0639 \u0627\u0644\u0645\u0633\u062a\u0644\u0645 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d.', 'Could not start Paymob checkout.': '\u062a\u0639\u0630\u0631 \u0628\u062f\u0621 \u0627\u0644\u062f\u0641\u0639 \u0639\u0628\u0631 Paymob.',
  'Opening secure checkout?': '\u062c\u0627\u0631\u064d \u0641\u062a\u062d \u0635\u0641\u062d\u0629 \u0627\u0644\u062f\u0641\u0639 \u0627\u0644\u0622\u0645\u0646\u0629...', 'Continue to Paymob': '\u0627\u0644\u0645\u062a\u0627\u0628\u0639\u0629 \u0625\u0644\u0649 Paymob'
});

// Keep complete translations for the English-first sign-in and AI review flows.
// These are intentionally plain text keys because the global language switch
// translates rendered text nodes as well as React labels.
Object.assign(arabic, {
  'TRAINING AND CLIENT MANAGEMENT': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u0627\u0644\u0639\u0645\u0644\u0627\u0621',
  'Manage training plans, client progress and communication in one place.': '\u0623\u062F\u0631 \u062E\u0637\u0637 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u062A\u0642\u062F\u0645 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0648\u0627\u0644\u062A\u0648\u0627\u0635\u0644 \u0645\u0646 \u0645\u0643\u0627\u0646 \u0648\u0627\u062D\u062F.',
  'Coaching plans and exercise tracking': '\u062E\u0637\u0637 \u062A\u062F\u0631\u064A\u0628 \u0648\u0645\u062A\u0627\u0628\u0639\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Progress reviews and client messaging': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u062A\u0642\u062F\u0645 \u0648\u0627\u0644\u062A\u0631\u0627\u0633\u0644 \u0645\u0639 \u0627\u0644\u0639\u0645\u0644\u0627\u0621',
  'Clear tools for coaches and club owners': '\u0623\u062F\u0648\u0627\u062A \u0648\u0627\u0636\u062D\u0629 \u0644\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0648\u0645\u0627\u0644\u0643\u064A \u0627\u0644\u0646\u0648\u0627\u062F\u064A',
  'Your coaching club starts here.': '\u0628\u062F\u0627\u064A\u062A\u0643 \u0641\u064A \u0631\u062D\u0644\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0645\u0646 \u0647\u0646\u0627.',
  'Choose the club linked to your account, then enter your sign-in details.': '\u0627\u062E\u062A\u0631 \u0627\u0644\u0646\u0627\u062F\u064A \u0627\u0644\u0645\u0631\u062A\u0628\u0637 \u0628\u062D\u0633\u0627\u0628\u0643\u060C \u062B\u0645 \u0623\u062F\u062E\u0644 \u0628\u064A\u0627\u0646\u0627\u062A \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u062F\u062E\u0648\u0644.',
  'Select a club': '\u0627\u062E\u062A\u0631 \u0627\u0644\u0646\u0627\u062F\u064A',
  'Select your club': '\u0627\u062E\u062A\u0631 \u0646\u0627\u062F\u064A\u0643',
  'Choose the club you use to sign in': '\u0627\u062E\u062A\u0631 \u0627\u0644\u0646\u0627\u062F\u064A \u0627\u0644\u0630\u064A \u062A\u0633\u062C\u0644 \u0627\u0644\u062F\u062E\u0648\u0644 \u0645\u0646 \u062E\u0644\u0627\u0644\u0647',
  'Email address': '\u0627\u0644\u0628\u0631\u064A\u062F \u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A',
  'Password': '\u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631',
  'Forgot password?': '\u0646\u0633\u064A\u062A \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631\u061F',
  'New to CoachPulse?': '\u0647\u0644 \u0623\u0646\u062A \u062C\u062F\u064A\u062F \u0641\u064A CoachPulse\u061F',
  'Request a new club': '\u0627\u0637\u0644\u0628 \u0625\u0646\u0634\u0627\u0621 \u0646\u0627\u062F\u064D \u062C\u062F\u064A\u062F',
  'For coaches who want to set up a club': '\u0644\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0627\u0644\u0631\u0627\u063A\u0628\u064A\u0646 \u0641\u064A \u0625\u0646\u0634\u0627\u0621 \u0646\u0627\u062F\u064D',
  'Join as a client': '\u0627\u0646\u0636\u0645 \u0643\u0639\u0645\u064A\u0644',
  'Complete registration with your coachâ€™s invitation': '\u0623\u0643\u0645\u0644 \u0627\u0644\u062A\u0633\u062C\u064A\u0644 \u0628\u062F\u0639\u0648\u0629 \u0645\u062F\u0631\u0628\u0643',
  'Have a staff invitation? Accept it here.': '\u0644\u062F\u064A\u0643 \u062F\u0639\u0648\u0629 \u0644\u0644\u0627\u0646\u0636\u0645\u0627\u0645 \u0643\u0645\u062F\u0631\u0628\u061F \u0627\u0642\u0628\u0644\u0647\u0627 \u0645\u0646 \u0647\u0646\u0627.',
  'Other account options': '\u062E\u064A\u0627\u0631\u0627\u062A \u0623\u062E\u0631\u0649 \u0644\u0644\u062D\u0633\u0627\u0628',
  'Your account and club information are protected by role-based access controls.': '\u062A\u062D\u0645\u064A \u0636\u0648\u0627\u0628\u0637 \u0627\u0644\u0648\u0635\u0648\u0644 \u062D\u0633\u0627\u0628\u0643 \u0648\u0628\u064A\u0627\u0646\u0627\u062A \u0646\u0627\u062F\u064A\u0643.',
  'Generate a training program': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0631\u0646\u0627\u0645\u062C \u062A\u062F\u0631\u064A\u0628\u064A',
  'Programs are saved as drafts in CoachPulse; no file is downloaded.': '\u062A\u064F\u062D\u0641\u0638 \u0627\u0644\u0628\u0631\u0627\u0645\u062C \u0643\u0645\u0633\u0648\u062F\u0627\u062A \u0641\u064A CoachPulse\u060C \u0648\u0644\u0627 \u064A\u062A\u0645 \u062A\u0646\u0632\u064A\u0644 \u0645\u0644\u0641.',
  'Training goal': '\u0647\u062F\u0641 \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Build strength': '\u0628\u0646\u0627\u0621 \u0627\u0644\u0642\u0648\u0629', 'Lose weight': '\u062E\u0633\u0627\u0631\u0629 \u0627\u0644\u0648\u0632\u0646', 'Build muscle': '\u0628\u0646\u0627\u0621 \u0627\u0644\u0639\u0636\u0644\u0627\u062A', 'General fitness': '\u0644\u064A\u0627\u0642\u0629 \u0639\u0627\u0645\u0629', 'Rehabilitation': '\u0625\u0639\u0627\u062F\u0629 \u062A\u0623\u0647\u064A\u0644',
  'Experience level': '\u0645\u0633\u062A\u0648\u0649 \u0627\u0644\u062E\u0628\u0631\u0629', 'Beginner': '\u0645\u0628\u062A\u062F\u0626', 'Intermediate': '\u0645\u062A\u0648\u0633\u0637', 'Advanced': '\u0645\u062A\u0642\u062F\u0645', 'Generate draft': '\u0625\u0646\u0634\u0627\u0621 \u0645\u0633\u0648\u062F\u0629',
  'AI quota and saved drafts': '\u062D\u0635\u0629 \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A \u0648\u0627\u0644\u0645\u0633\u0648\u062F\u0627\u062A', 'Drafts stay here until approved or rejected.': '\u062A\u0628\u0642\u0649 \u0627\u0644\u0645\u0633\u0648\u062F\u0627\u062A \u0647\u0646\u0627 \u062D\u062A\u0649 \u0627\u0639\u062A\u0645\u0627\u062F\u0647\u0627 \u0623\u0648 \u0631\u0641\u0636\u0647\u0627.',
  'Daily limit': '\u0627\u0644\u062D\u062F \u0627\u0644\u064A\u0648\u0645\u064A', 'Used today': '\u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645 \u0627\u0644\u064A\u0648\u0645', 'Remaining': '\u0627\u0644\u0645\u062A\u0628\u0642\u064A', 'Quota resets': '\u062A\u062A\u062C\u062F\u062F \u0627\u0644\u062D\u0635\u0629',
  'Client': '\u0627\u0644\u0639\u0645\u064A\u0644', 'Status': '\u0627\u0644\u062D\u0627\u0644\u0629', 'Prompt version': '\u0646\u0633\u062E\u0629 \u0627\u0644\u062A\u0639\u0644\u064A\u0645\u0627\u062A', 'Model version': '\u0646\u0633\u062E\u0629 \u0627\u0644\u0646\u0645\u0648\u0630\u062C', 'Review': '\u0645\u0631\u0627\u062C\u0639\u0629', 'Review draft': '\u0631\u0627\u062C\u0639 \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Client churn risk': '\u0645\u062E\u0627\u0637\u0631 \u0627\u0646\u0633\u062D\u0627\u0628 \u0627\u0644\u0639\u0645\u064A\u0644', 'Scores are created by the nightly job or a manual recompute by the club owner.': '\u062A\u064F\u062D\u0633\u0628 \u0627\u0644\u062F\u0631\u062C\u0627\u062A \u0644\u064A\u0644\u0627\u064B \u0623\u0648 \u064A\u0639\u064A\u062F \u0645\u0627\u0644\u0643 \u0627\u0644\u0646\u0627\u062F\u064A \u0627\u062D\u062A\u0633\u0627\u0628\u0647\u0627 \u064A\u062F\u0648\u064A\u0627\u064B.',
  'Load latest score': '\u0639\u0631\u0636 \u0622\u062E\u0631 \u062F\u0631\u062C\u0629', 'Recompute scores': '\u0625\u0639\u0627\u062F\u0629 \u0627\u062D\u062A\u0633\u0627\u0628 \u0627\u0644\u062F\u0631\u062C\u0627\u062A', 'Risk level': '\u0645\u0633\u062A\u0648\u0649 \u0627\u0644\u062E\u0637\u0631', 'Risk score': '\u062F\u0631\u062C\u0629 \u0627\u0644\u062E\u0637\u0631', 'Last calculated': '\u0622\u062E\u0631 \u0627\u062D\u062A\u0633\u0627\u0628',
})


const legacyArabicByteMap = new Map<string, number>()
const cp1256Decoder = new TextDecoder('windows-1256')
const cp1252Decoder = new TextDecoder('windows-1252')
for (let byte = 0; byte < 256; byte++) {
  const character = cp1252Decoder.decode(Uint8Array.of(byte))
  if (character.length === 1 && !legacyArabicByteMap.has(character)) legacyArabicByteMap.set(character, byte)
}
// Some Arabic Windows-1256 bytes in the 0x80–0x9F range were previously
// mistaken for Windows-1252 punctuation (for example, ٹ represents byte 0x8A).
for (let byte = 0; byte < 256; byte++) {
  const character = cp1256Decoder.decode(Uint8Array.of(byte))
  if (character.length === 1 && /[\u0600-\u06ff]/.test(character)) legacyArabicByteMap.set(character, byte)
}

function repairLegacyArabic(text: string) {
  // Several older translations were UTF-8 Arabic decoded as Windows-1256/1252.
  // Reverse that byte mapping before displaying or looking up translated labels.
  if (!/(?:ط[\u00A7\u00A9\u00A3\u00A5\u00AE\u00AF\u00AA\u06BE\u061B]|ظ[\u201E\u201C\u201D\u201A]|ï؟½)/.test(text)) return text
  const bytes: number[] = []
  for (const character of text) {
    const byte = legacyArabicByteMap.get(character)
    if (byte === undefined) return text
    bytes.push(byte)
  }
  const repaired = new TextDecoder('utf-8').decode(Uint8Array.from(bytes))
  return /[\u0600-\u06ff]/.test(repaired) ? repaired : text
}

function repairBrokenUiArabic(text: string) {
  const repairs: Array<[RegExp, string]> = [
    [/\u062A\u062D\u0645\uFFFD\??\s*\u0644/g, '\u062A\u062D\u0645\u064A\u0644'],
    [/\u0627\u0644\u0628\uFFFD\??\s*\u0627\uFFFD\??\s*\u0627\u062A/g, '\u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A'],
    [/\u0627\u0644\u0622\uFFFD\??/g, '\u0627\u0644\u0622\u0646'],
    [/\u0627\u0644\u062C\u062F\uFFFD\??\s*\u062F\u0629/g, '\u0627\u0644\u062C\u062F\u064A\u062F\u0629'],
    [/\u062A\u0633\u062C\uFFFD\??\s*\u0644/g, '\u062A\u0633\u062C\u064A\u0644'],
    [/\u0627\u0644\u062D\u0627\u0644\uFFFD\??\s*\u0629/g, '\u0627\u0644\u062D\u0627\u0644\u064A\u0629'],
    [/\u062A\u0623\u0643\uFFFD\??\s*\u062F/g, '\u062A\u0623\u0643\u064A\u062F'],
    [/\u0627\u0644\u0628\u0631\uFFFD\??\s*\u062F/g, '\u0627\u0644\u0628\u0631\u064A\u062F'],
    [/\u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\uFFFD\??/g, '\u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A'],
    [/\u0627\u0644\u062A\uFFFD\??\s*\u0628\uFFFD\??\s*\u0647\u0627\u062A/g, '\u0627\u0644\u062A\u0646\u0628\u064A\u0647\u0627\u062A'],
    [/\u063A\uFFFD\??\s*\u0631/g, '\u063A\u064A\u0631'],
    [/\u062A\u0641\u0627\u0635\uFFFD\??\s*\u0644/g, '\u062A\u0641\u0627\u0635\u064A\u0644'],
    [/\u062A\u0645\u0627\u0631\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u062A\u0645\u0627\u0631\u064A\u0646'],
    [/\u0639\uFFFD\??\s*\u0648\u0627\uFFFD\??/g, '\u0639\u0646\u0648\u0627\u0646'],
    [/\u0628\u0627\u0644\u0623\uFFFD\??\s*\u0627\u0645/g, '\u0628\u0627\u0644\u0623\u064A\u0627\u0645'],
    [/\u0627\u0644\uFFFD\??\s*\u0648\u0645/g, '\u0627\u0644\u064A\u0648\u0645'],
    [/\u0628\u0627\u0644\u062B\u0648\u0627\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0628\u0627\u0644\u062B\u0648\u0627\u0646\u064A'],
    [/\u062A\u0639\u062F\uFFFD\??\s*\u0644/g, '\u062A\u0639\u062F\u064A\u0644'],
    [/\u0627\u0644\u062A\u0631\u062A\uFFFD\??\s*\u0628/g, '\u0627\u0644\u062A\u0631\u062A\u064A\u0628'],
    [/\u0625\uFFFD\??\s*\u0634\u0627\u0621/g, '\u0625\u0646\u0634\u0627\u0621'],
    [/\u0628\u0631\uFFFD\??\s*\u0627\u0645\u062C/g, '\u0628\u0631\u0646\u0627\u0645\u062C'],
    [/\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629'],
    [/\u062A\u062D\u062F\uFFFD\??\s*\u062B/g, '\u062A\u062D\u062F\u064A\u062B'],
    [/\u0625\u0633\uFFFD\??\s*\u0627\u062F/g, '\u0625\u0633\u0646\u0627\u062F'],
    [/\u062A\u0641\u0639\uFFFD\??\s*\u0644/g, '\u062A\u0641\u0639\u064A\u0644'],
    [/\u0625\uFFFD\??\s*\u0642\u0627\u0641/g, '\u0625\u064A\u0642\u0627\u0641'],
    [/\u0627\u0644\u0645\u062E\u0635\u0635\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062E\u0635\u0635\u0648\u0646'],
    [/\u0627\u0644\u0641\uFFFD\??\s*\u062F\uFFFD\??\s*\u0648/g, '\u0627\u0644\u0641\u064A\u062F\u064A\u0648'],
    [/\u0627\u062E\u062A\uFFFD\??\s*\u0627\u0631/g, '\u0627\u062E\u062A\u064A\u0627\u0631'],
    [/\u062A\u063A\uFFFD\??\s*\u064A\u0631/g, '\u062A\u063A\u064A\u064A\u0631'],
    [/\u0627\u0644\u0635\u062D\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0635\u062D\u064A\u0629'],
    [/\u0627\u0644\u0648\u0632\uFFFD\??/g, '\u0627\u0644\u0648\u0632\u0646'],
    [/\u0627\u0644\u0642\uFFFD\??\s*\u0627\u0633\u0627\u062A/g, '\u0627\u0644\u0642\u064A\u0627\u0633\u0627\u062A'],
    [/\u0627\u0644\u062A\u0627\u0631\uFFFD\??\s*\u062E/g, '\u0627\u0644\u062A\u0627\u0631\u064A\u062E'],
    [/\u0627\u0644\u0647\u0648\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0647\u0648\u064A\u0629'],
    [/\u0627\u0644\u0645\u0646\u0635\u0641\u0629/g, '\u0627\u0644\u0645\u0646\u0635\u0629'],
    [/\u0645\u0646\u0635\u0641\u0629/g, '\u0645\u0646\u0635\u0629'],
    [/\u0645\u0624\u0634\u0631\u0627\u062A \u0627\u0644\u0645\uFFFD\??\s*\u0635\u0629/g, '\u0645\u0624\u0634\u0631\u0627\u062A \u0627\u0644\u0645\u0646\u0635\u0629'],
    [/\u062C\u062F\uFFFD\??\s*\u062F/g, '\u062C\u062F\u064A\u062F'],
    [/\u062A\u0645\u0631\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u062A\u0645\u0631\u064A\u0646'],
    [/\u0627\u0644\u062A\u0645\u0631\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0627\u0644\u062A\u0645\u0631\u064A\u0646'],
    [/\u062A\u0631\u062A\uFFFD\??\s*\u0628/g, '\u062A\u0631\u062A\u064A\u0628'],
    [/\u0625\uFFFD\??\s*\u0634\u0627\u0621/g, '\u0625\u0646\u0634\u0627\u0621'],
    [/\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629'],
    [/\u0627\u0644\u0645\u062F\u0631\u0628\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062F\u0631\u0628\u0648\u0646'],
    [/\u0627\u0644\u0645\u062E\u0635\u0635\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062E\u0635\u0635\u0648\u0646'],
    [/\u0641\uFFFD\??\s*/g, '\u0641\u064A '],
    [/\u0627\u0644\u0641\uFFFD\??\s*\u062F\uFFFD\??\s*\u0648/g, '\u0627\u0644\u0641\u064A\u062F\u064A\u0648'],
    [/\u062A\u0641\u0639\uFFFD\??\s*\u0644/g, '\u062A\u0641\u0639\u064A\u0644'],
    [/\u0625\uFFFD\??\s*\u0642\u0627\u0641/g, '\u0625\u064A\u0642\u0627\u0641'],
    [/\u0627\u0644\u062A\u0637\u0648\uFFFD\??\s*\u0631/g, '\u0627\u0644\u062A\u0637\u0648\u064A\u0631'],
    [/\u0639\u0645\uFFFD\??\s*\u0644/g, '\u0639\u0645\u064A\u0644'],
    [/\u0627\u0644\u062A\u0635\u062D\u064A\uFFFD\??/g, '\u0627\u0644\u062A\u0635\u062D\u064A\u062D'],
    [/\u062A\u0635\u062F\uFFFD\??\s*\u0631/g, '\u062A\u0635\u062F\u064A\u0631'],
    [/\u0627\u0644\u062A\u0646\u0632\uFFFD\??\s*\u0644/g, '\u0627\u0644\u062A\u0646\u0632\u064A\u0644'],
    [/\u0627\u0644\u0645\u0644\u0641 \uFFFD\??\s*\u0636\u0645/g, '\u0627\u0644\u0645\u0644\u0641 \u064A\u0636\u0645'],
    [/\u0645\u0643\uFFFD\??\s*\u0646/g, '\u064A\u0645\u0643\u0646'],
    [/\u0627\u0644\u062A\u0641\u0627\u0635\uFFFD\??\s*\u0644/g, '\u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644'],
    [/\u0627\u0644\u0642\uFFFD\??\s*\u0627\u0633\u0627\u062A/g, '\u0627\u0644\u0642\u064A\u0627\u0633\u0627\u062A'],
    [/\u0627\u0644\u062E\u0637\u062A\uFFFD\??/g, '\u0627\u0644\u062E\u0637\u062A\u064A'],
    [/\u0627\u0644\u062A\u062F\u0631\uFFFD\??\s*\u0628/g, '\u0627\u0644\u062A\u062F\u0631\u064A\u0628'],
    [/\u0627\u0644\u0645\u0624\u0633\u0633\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0627\u0644\u0645\u0624\u0633\u0633\u0648\u0646'],
    [/\u0627\u0644\u0627\u0635\u0637\uFFFD\??\s*\u0646\u0627\u0639\uFFFD\??/g, '\u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A'],
    [/\u0639\u0645\u0644\uFFFD\??\s*\u0627\u062A/g, '\u0639\u0645\u0644\u064A\u0627\u062A'],
    [/\u0625\u062F\u0627\u0631\uFFFD\??\s*\u0629/g, '\u0625\u062F\u0627\u0631\u064A\u0629'],
    [/\u0627\u0644\u0623\u062E\uFFFD\??\s*\u0631/g, '\u0627\u0644\u0623\u062E\u064A\u0631'],
    [/\u0627\u0644\u062A\u062D\u0642\u0642 \u0645\uFFFD\??/g, '\u0627\u0644\u062A\u062D\u0642\u0642 \u0645\u0646'],
    [/\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\uFFFD\??\s*\u0634\u0637\u0629/g, '\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0646\u0634\u0637\u0629'],
    [/\u0627\u0644\u0628\u0631\uFFFD\??\s*\u0627\u0645\u062C/g, '\u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C'],
    [/\u0627\u0644\u0639\u0645\u0644\uFFFD\??\s*\u0627\u0621/g, '\u0627\u0644\u0639\u0645\u0644\u0627\u0621'],
    [/\u0627\u0644\u0648\u062C\u0628\u0627\uFFFD\??\s*\u062A/g, '\u0627\u0644\u0648\u062C\u0628\u0627\u062A'],
    [/\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\uFFFD\??\s*\u0634\u0637\u0629/g, '\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0646\u0634\u0637\u0629'],
    [/\u0627\u0644\u0645\u0648\u0642\u0648\u0641\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u0648\u0642\u0648\u0641\u0648\u0646'],
    [/\u0627\u0644\u0645\u0624\u0633\u0633\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0627\u0644\u0645\u0624\u0633\u0633\u0648\u0646'],
    [/\u0627\u0644\u0645\u062F\u0631\u0628\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062F\u0631\u0628\u0648\u0646'],
    [/\u0627\u0644\u0639\u0645\u064A\u0644\u0627\u062A/g, '\u0627\u0644\u0639\u0645\u064A\u0644\u0627\u062A'],
    [/\u0627\u0644\u0645\u0631\u0627\u062C\u0639\u0629 \u0644\u0644\u062A\uFFFD\??\s*\u0628\uFFFD\??\s*\u0647\u0627\u062A/g, '\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u0645\u062A\u0627\u0628\u0639\u0627\u062A'],
    [/\u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u063A\uFFFD\??\s*\u0631 \u0645\u0642\u0631\u0648\u0621\u0629/g, '\u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u063A\u064A\u0631 \u0645\u0642\u0631\u0648\u0621\u0629'],
    [/\u0627\u0644\u062A\u0645\u0631\u064A\u0646\uFFFD\??/g, '\u0627\u0644\u062A\u0645\u0631\u064A\u0646'],
    [/\u0627\u0644\u0645\u0642\u0627\u0633\u0627\u062A/g, '\u0627\u0644\u0642\u064A\u0627\u0633\u0627\u062A'],
    [/\u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A\uFFFD\??/g, '\u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A'],
    [/\u0627\u0644\u062A\u0639\u0644\u064A\u0642\uFFFD\??/g, '\u0627\u0644\u062A\u0639\u0644\u064A\u0642'],
    [/\u0627\u0644\u062A\u0642\u064A\u064A\u0645\uFFFD\??/g, '\u0627\u0644\u062A\u0642\u064A\u064A\u0645'],
    [/\u0627\u0644\u0645\u0633\u0627\u062D\u0629 \u0627\u0644\u0639\u0627\u0645\u0629/g, '\u0627\u0644\u0645\u0646\u0635\u0629 \u0627\u0644\u0639\u0627\u0645\u0629'],
    [/\u0627\u0644\u0648\u0642\u062A \u0627\u0644\u0645\u062A\u0627\u062D/g, '\u0627\u0644\u0648\u0642\u062A \u0627\u0644\u0645\u062A\u0627\u062D'],
    [/\u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0645\u0633\u0627\u062D\u0629/g, '\u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0646\u0627\u062F\u064A'],
    [/\u0645\u0631\u0643\u0632 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062A/g, '\u0645\u0631\u0643\u0632 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062A'],
    [/\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u0623\u0633\u0627\u0633\uFFFD\??/g, '\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u0623\u0633\u0627\u0633\u064A'],
    [/\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u062B\u0627\u0646\u0648\uFFFD\??/g, '\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u062B\u0627\u0646\u0648\u064A'],
    [/\u0627\u0633\u0645 \u0627\u0644\u0646\u0627\u062F\uFFFD\??/g, '\u0627\u0633\u0645 \u0627\u0644\u0646\u0627\u062F\u064A'],
    [/\u062D\u0641\u0638 \u0627\u0644\u0647\u0648\uFFFD\??\s*\u0629/g, '\u062D\u0641\u0638 \u0627\u0644\u0647\u0648\u064A\u0629'],
    [/\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\uFFFD\??\s*\u0627\u0646\u0627\u062A/g, '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u064A\u0627\u0646\u0627\u062A'],
    [/\u062D\u0627\u0644\uFFFD\??\s*\u0627/g, '\u062D\u0627\u0644\u064A\u0627'],
    [/\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0627\u0642\u0629 \u0646\u0634\u0637\u0629/g, '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0627\u0642\u0629 \u0646\u0634\u0637\u0629'],
    [/\u0627\u0644\u0628\u0631\uFFFD\??\s*\u064A\u062F \u0627\u0644\u062C\u062F\uFFFD\??\s*\u062F/g, '\u0627\u0644\u0628\u0631\u064A\u062F \u0627\u0644\u062C\u062F\u064A\u062F'],
    [/\u0631\u0627\u0628\u0637 \u0627\u0644\u0641\uFFFD\??\s*\u062F\uFFFD\??\s*\u0648/g, '\u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648'],
    [/\u0627\u0644\u062A\u0642\u062F\u0645 \u0648\u0627\u0644\u062A\u0648\u0627\u0635\u0644/g, '\u0627\u0644\u062A\u0642\u062F\u0645 \u0648\u0627\u0644\u062A\u0648\u0627\u0635\u0644'],
    [/\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629/g, '\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629']
  ]
  return repairs.reduce((value, [pattern, replacement]) => value.replace(pattern, replacement), text)
}

Object.assign(arabic, { 'Loading data...': '\u062C\u0627\u0631\u064D \u062A\u062D\u0645\u064A\u0644 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A...', 'Reference': '\u0627\u0644\u0645\u0631\u062C\u0639', 'Coach team': '\u0641\u0631\u064A\u0642 \u0627\u0644\u0645\u062F\u0631\u0628\u064A\u0646', 'Manage staff access and assign clients to their responsible coach.': '\u0623\u062F\u0631 \u0627\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0648\u0623\u0633\u0646\u062F \u0643\u0644 \u0639\u0645\u064A\u0644 \u0644\u0645\u062F\u0631\u0628\u0647 \u0627\u0644\u0645\u0633\u0624\u0648\u0644.', 'coaches': '\u0645\u062F\u0631\u0628\u0648\u0646', 'Manage coaches and assignments': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0648\u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0639\u0645\u0644\u0627\u0621' })
const repairedEnglish = Object.fromEntries(Object.entries(arabic).map(([key, value]) => [repairBrokenUiArabic(repairLegacyArabic(value)), key]))
const repairedEnglishUi = Object.fromEntries(Object.entries(englishUi).map(([key, value]) => [repairLegacyArabic(key), value]))

export function toEnglish(text: string) {
  const repaired = repairBrokenUiArabic(repairLegacyArabic(text))
  const exact = englishUi[text] ?? repairedEnglishUi[repaired] ?? english[text] ?? repairedEnglish[repaired]
  if (exact) return exact
  if (repaired !== text) return repaired
  return text
}

export function toArabic(text: string) {
  return repairBrokenUiArabic(repairLegacyArabic(arabic[text] ?? text))
}
export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>(() => localStorage.getItem('coachpulse.language.v5') === 'ar' ? 'ar' : 'en')
  const setLanguage = useCallback((next: Language) => { localStorage.setItem('coachpulse.language.v5', next); setLanguageState(next) }, [])
  const t = useCallback((text: string) => language === 'ar' ? toArabic(text) : toEnglish(text), [language])
  useEffect(() => { document.documentElement.lang = language; document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr' }, [language])
  const value = useMemo(() => ({ language, setLanguage, t }), [language, setLanguage, t])
  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}

export function useLanguage() {
  const value = useContext(LanguageContext)
  if (!value) throw new Error('LanguageProvider is missing.')
  return value
}

export function LanguageToggle() {
  const { language, setLanguage } = useLanguage()
  return <label className="language-toggle"><span>Language</span><select aria-label="Choose language" value={language} onChange={event => setLanguage(event.target.value as Language)}><option value="en">English</option><option value="ar">&#x627;&#x644;&#x639;&#x631;&#x628;&#x64A;&#x629;</option></select></label>
}
Object.assign(arabic, {
  'Plans': '\u0627\u0644\u0628\u0627\u0642\u0627\u062A', 'Create clear membership offers and manage what clients can request.': '\u0623\u0646\u0634\u0626 \u0628\u0627\u0642\u0627\u062A \u0648\u0627\u0636\u062D\u0629 \u0648\u0623\u062F\u0631 \u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0645\u062A\u0627\u062D\u0629 \u0644\u0644\u0639\u0645\u0644\u0627\u0621.',
  'STEP 1': '\u0627\u0644\u062E\u0637\u0648\u0629 1', 'STEP 2': '\u0627\u0644\u062E\u0637\u0648\u0629 2', 'STEP 3': '\u0627\u0644\u062E\u0637\u0648\u0629 3', 'Create a membership plan': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0627\u0642\u0629 \u0627\u0634\u062A\u0631\u0627\u0643', 'Set the price and access period. Clients can request an active plan from their subscription page.': '\u062D\u062F\u062F \u0627\u0644\u0633\u0639\u0631 \u0648\u0645\u062F\u0629 \u0627\u0644\u0648\u0635\u0648\u0644. \u064A\u0633\u062A\u0637\u064A\u0639 \u0627\u0644\u0639\u0645\u064A\u0644 \u0637\u0644\u0628 \u0627\u0644\u0628\u0627\u0642\u0629 \u0627\u0644\u0646\u0634\u0637\u0629 \u0645\u0646 \u0635\u0641\u062D\u0629 \u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A\u0647.', 'Plan name': '\u0627\u0633\u0645 \u0627\u0644\u0628\u0627\u0642\u0629', 'Example: 12-week coaching': '\u0645\u062B\u0627\u0644: \u062A\u062F\u0631\u064A\u0628 \u0644\u0645\u062F\u0629 12 \u0623\u0633\u0628\u0648\u0639\u064B\u0627', 'Price': '\u0627\u0644\u0633\u0639\u0631', 'Duration in days': '\u0627\u0644\u0645\u062F\u0629 \u0628\u0627\u0644\u0623\u064A\u0627\u0645', 'Create plan': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0627\u0642\u0629',
  'Plan created. Clients can now request it.': '\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0627\u0642\u0629. \u064A\u0633\u062A\u0637\u064A\u0639 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0637\u0644\u0628\u0647\u0627 \u0627\u0644\u0622\u0646.', 'Could not create the plan.': '\u062A\u0639\u0630\u0631 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0627\u0642\u0629.', 'Plan updated. Existing subscriptions keep their original price and duration.': '\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0628\u0627\u0642\u0629. \u062A\u062D\u062A\u0641\u0638 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629 \u0628\u0627\u0644\u0633\u0639\u0631 \u0648\u0627\u0644\u0645\u062F\u0629 \u0627\u0644\u0623\u0635\u0644\u064A\u064A\u0646.', 'Could not update the plan.': '\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0628\u0627\u0642\u0629.', 'Archive this plan? Clients can no longer request it.': '\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629\u061F \u0644\u0646 \u064A\u062A\u0645\u0643\u0646 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0645\u0646 \u0637\u0644\u0628\u0647\u0627.', 'Plan archived. Existing subscriptions are unchanged.': '\u062A\u0645\u062A \u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629. \u0644\u0645 \u062A\u062A\u063A\u064A\u0631 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629.', 'Could not archive the plan.': '\u062A\u0639\u0630\u0631 \u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629.',
  'Your membership plans': '\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643', 'Active plans appear to clients. Existing subscriptions keep their original price and duration.': '\u062A\u0638\u0647\u0631 \u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0646\u0634\u0637\u0629 \u0644\u0644\u0639\u0645\u0644\u0627\u0621. \u062A\u062D\u062A\u0641\u0638 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629 \u0628\u0627\u0644\u0633\u0639\u0631 \u0648\u0627\u0644\u0645\u062F\u0629 \u0627\u0644\u0623\u0635\u0644\u064A\u064A\u0646.', 'plans': '\u0628\u0627\u0642\u0627\u062A', 'Active': '\u0646\u0634\u0637\u0629', 'Archived': '\u0645\u0624\u0631\u0634\u0641\u0629', 'Membership plan': '\u0628\u0627\u0642\u0629 \u0627\u0634\u062A\u0631\u0627\u0643', 'days of access': '\u064A\u0648\u0645 \u0645\u0646 \u0627\u0644\u0648\u0635\u0648\u0644', 'Edit plan': '\u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u0628\u0627\u0642\u0629', 'No plans yet': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0627\u0642\u0627\u062A \u0628\u0639\u062F', 'Create your first plan above. Then clients can request it and pay online, by InstaPay or through a recorded cash payment.': '\u0623\u0646\u0634\u0626 \u0623\u0648\u0644 \u0628\u0627\u0642\u0629 \u0623\u0639\u0644\u0627\u0647. \u0628\u0639\u062F\u0647\u0627 \u064A\u0637\u0644\u0628\u0647\u0627 \u0627\u0644\u0639\u0645\u064A\u0644 \u0648\u064A\u062F\u0641\u0639 \u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A\u064B\u0627 \u0623\u0648 \u0639\u0628\u0631 InstaPay \u0623\u0648 \u0628\u062A\u0633\u062C\u064A\u0644 \u062F\u0641\u0639\u0629 \u0646\u0642\u062F\u064A\u0629.', 'Edit or archive a plan': '\u062A\u0639\u062F\u064A\u0644 \u0628\u0627\u0642\u0629 \u0623\u0648 \u0623\u0631\u0634\u0641\u062A\u0647\u0627', 'Archiving prevents new requests but preserves existing subscriptions.': '\u062A\u0645\u0646\u0639 \u0627\u0644\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0637\u0644\u0628\u0627\u062A \u0627\u0644\u062C\u062F\u064A\u062F\u0629 \u0648\u062A\u0628\u0642\u064A \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629.', 'Choose a plan': '\u0627\u062E\u062A\u0631 \u0628\u0627\u0642\u0629', 'Save changes': '\u062D\u0641\u0638 \u0627\u0644\u062A\u063A\u064A\u064A\u0631\u0627\u062A', 'Archive plan': '\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629',
  'Next: build your coaching offer': '\u0627\u0644\u062E\u0637\u0648\u0629 \u0627\u0644\u062A\u0627\u0644\u064A\u0629: \u0623\u0639\u062F\u062F \u0628\u0631\u0646\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Add exercises and create training programs before assigning a program to a client.': '\u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0648\u0623\u0646\u0634\u0626 \u0628\u0631\u0627\u0645\u062C \u062A\u062F\u0631\u064A\u0628\u064A\u0629 \u0642\u0628\u0644 \u0625\u0633\u0646\u0627\u062F \u0628\u0631\u0646\u0627\u0645\u062C \u0644\u0639\u0645\u064A\u0644.', 'Open exercise library': '\u0641\u062A\u062D \u0645\u0643\u062A\u0628\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646', 'Manage subscriptions': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A',
  'A clear path from published plan to active client access.': '\u0645\u0633\u0627\u0631 \u0648\u0627\u0636\u062D \u0645\u0646 \u0646\u0634\u0631 \u0627\u0644\u0628\u0627\u0642\u0629 \u0625\u0644\u0649 \u062A\u0641\u0639\u064A\u0644 \u0627\u0634\u062A\u0631\u0627\u0643 \u0627\u0644\u0639\u0645\u064A\u0644.', 'Publish a plan': '\u0627\u0646\u0634\u0631 \u0628\u0627\u0642\u0629', 'Set a price and duration before clients can request membership.': '\u062D\u062F\u062F \u0627\u0644\u0633\u0639\u0631 \u0648\u0627\u0644\u0645\u062F\u0629 \u0642\u0628\u0644 \u0623\u0646 \u064A\u0637\u0644\u0628 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Invite the client': '\u0627\u062F\u0639 \u0627\u0644\u0639\u0645\u064A\u0644', 'The client chooses a plan and submits a payment request from their account.': '\u064A\u062E\u062A\u0627\u0631 \u0627\u0644\u0639\u0645\u064A\u0644 \u0628\u0627\u0642\u0629 \u0648\u064A\u0631\u0633\u0644 \u0637\u0644\u0628 \u0627\u0644\u062F\u0641\u0639 \u0645\u0646 \u062D\u0633\u0627\u0628\u0647.', 'Review payment and activate': '\u0631\u0627\u062C\u0639 \u0627\u0644\u062F\u0641\u0639 \u0648\u0641\u0639\u0651\u0644 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643', 'Confirm InstaPay, record a pending cash payment, or activate access manually.': '\u0623\u0643\u062F \u062A\u062D\u0648\u064A\u0644 InstaPay \u0623\u0648 \u0633\u062C\u0644 \u062F\u0641\u0639\u0629 \u0646\u0642\u062F\u064A\u0629 \u0645\u0639\u0644\u0642\u0629 \u0623\u0648 \u0641\u0639\u0644 \u0627\u0644\u0648\u0635\u0648\u0644 \u064A\u062F\u0648\u064A\u0627.',
  'Activate without online payment': '\u062A\u0641\u0639\u064A\u0644 \u0628\u062F\u0648\u0646 \u062F\u0641\u0639 \u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A', 'Use this when payment was collected outside the platform. This grants access immediately and does not create a payment record. To record cash in the ledger, first have the client submit a subscription request, then record that pending request under Payments.': '\u0627\u0633\u062A\u062E\u062F\u0645 \u0647\u0630\u0627 \u0625\u0630\u0627 \u062A\u0645 \u0627\u0644\u062A\u062D\u0635\u064A\u0644 \u062E\u0627\u0631\u062C \u0627\u0644\u0645\u0646\u0635\u0629. \u064A\u0641\u0639\u0644 \u0627\u0644\u0648\u0635\u0648\u0644 \u0641\u0648\u0631\u0627 \u0648\u0644\u0627 \u064A\u0633\u062C\u0644 \u062F\u0641\u0639\u0629. \u0644\u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0646\u0642\u062F\u064A\u0629 \u0641\u064A \u0627\u0644\u062D\u0633\u0627\u0628\u0627\u062A\u060C \u0644\u064A\u0642\u062F\u0645 \u0627\u0644\u0639\u0645\u064A\u0644 \u0637\u0644\u0628\u0627 \u0623\u0648\u0644\u0627 \u062B\u0645 \u0633\u062C\u0644\u0647 \u0645\u0646 \u0627\u0644\u0645\u062F\u0641\u0648\u0639\u0627\u062A.', 'Subscription activated.': '\u062A\u0645 \u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Could not activate subscription.': '\u062A\u0639\u0630\u0631 \u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Reason (required)': '\u0627\u0644\u0633\u0628\u0628 (\u0625\u062C\u0628\u0627\u0631\u064A)', 'Example: payment received in person': '\u0645\u062B\u0627\u0644: \u062A\u0645 \u0627\u0633\u062A\u0644\u0627\u0645 \u0627\u0644\u0645\u0628\u0644\u063A \u0645\u0628\u0627\u0634\u0631\u0629', 'Client subscriptions': '\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0639\u0645\u0644\u0627\u0621', 'Review status and dates. Select a subscription to manage it.': '\u0631\u0627\u062C\u0639 \u0627\u0644\u062D\u0627\u0644\u0629 \u0648\u0627\u0644\u062A\u0648\u0627\u0631\u064A\u062E. \u0627\u062E\u062A\u0631 \u0627\u0634\u062A\u0631\u0627\u0643\u0627 \u0644\u0625\u062F\u0627\u0631\u062A\u0647.', 'Manage clients': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0639\u0645\u0644\u0627\u0621', 'Review payments': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u0645\u062F\u0641\u0648\u0639\u0627\u062A', 'No subscriptions yet.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0628\u0639\u062F.', 'Publish a plan, then invite clients to request it.': '\u0627\u0646\u0634\u0631 \u0628\u0627\u0642\u0629 \u062B\u0645 \u0627\u062F\u0639 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0644\u0637\u0644\u0628\u0647\u0627.', 'Reason for this change': '\u0633\u0628\u0628 \u0627\u0644\u062A\u063A\u064A\u064A\u0631', 'Cancel this subscription?': '\u0625\u0644\u063A\u0627\u0621 \u0647\u0630\u0627 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u061F', 'Subscription updated.': '\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Could not update the subscription.': '\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Client': '\u0627\u0644\u0639\u0645\u064A\u0644', 'days': '\u064A\u0648\u0645'
})

Object.assign(arabic, {
  'Platform management': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629', 'Refresh data': '\u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A', 'Sign out': '\u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u062E\u0631\u0648\u062C',
  'Administration': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629', 'Manage and monitor the platform': '\u0625\u062F\u0627\u0631\u0629 \u0648\u0645\u0631\u0627\u0642\u0628\u0629 \u0627\u0644\u0645\u0646\u0635\u0629', 'Platform administrator permissions': '\u0635\u0644\u0627\u062D\u064A\u0627\u062A \u0645\u0633\u0624\u0648\u0644 \u0627\u0644\u0645\u0646\u0635\u0629',
  'Clearer platform management': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629 \u0628\u0648\u0636\u0648\u062D \u0623\u0643\u0628\u0631', 'A smoother experience for everyone': '\u062A\u062C\u0631\u0628\u0629 \u0623\u0633\u0647\u0644 \u0644\u0644\u062C\u0645\u064A\u0639',
  'Monitor club health, review recent activity, and open common tasks.': '\u062A\u0627\u0628\u0639 \u062D\u0627\u0644\u0629 \u0627\u0644\u0623\u0646\u062F\u064A\u0629\u060C \u0648\u0631\u0627\u062C\u0639 \u0623\u062D\u062F\u062B \u0627\u0644\u0623\u0646\u0634\u0637\u0629\u060C \u0648\u0627\u0646\u062A\u0642\u0644 \u0645\u0628\u0627\u0634\u0631\u0629 \u0625\u0644\u0649 \u0627\u0644\u0625\u062C\u0631\u0627\u0621\u0627\u062A \u0627\u0644\u0645\u0637\u0644\u0648\u0628\u0629.',
  'Control center': '\u0645\u0631\u0643\u0632 \u0627\u0644\u062A\u062D\u0643\u0645', 'Browse clubs': '\u0627\u0633\u062A\u0639\u0631\u0627\u0636 \u0627\u0644\u0623\u0646\u062F\u064A\u0629', 'Audit log': '\u0633\u062C\u0644 \u0627\u0644\u062A\u062F\u0642\u064A\u0642', 'Training and management platform': '\u0645\u0646\u0635\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u0627\u0644\u0625\u062F\u0627\u0631\u0629', 'Platform overview and service health': '\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629 \u0644\u0644\u0645\u0646\u0635\u0629 \u0648\u062D\u0627\u0644\u0629 \u0627\u0644\u062E\u062F\u0645\u0627\u062A',
  'Overview': '\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629', 'Platform metrics': '\u0645\u0624\u0634\u0631\u0627\u062A \u0627\u0644\u0645\u0646\u0635\u0629', 'Aggregated figures for club growth and platform usage.': '\u0623\u0631\u0642\u0627\u0645 \u0645\u062C\u0645\u0639\u0629 \u0644\u0645\u062A\u0627\u0628\u0639\u0629 \u0646\u0645\u0648 \u0627\u0644\u0623\u0646\u062F\u064A\u0629 \u0648\u0627\u0633\u062A\u062E\u062F\u0627\u0645 \u0627\u0644\u0645\u0646\u0635\u0629.', 'Platform summary': '\u0645\u0644\u062E\u0635 \u0627\u0644\u0645\u0646\u0635\u0629', 'No metrics are available yet.': '\u0644\u0627 \u062A\u062A\u0648\u0641\u0631 \u0645\u0624\u0634\u0631\u0627\u062A \u062D\u0627\u0644\u064A\u0627\u064B.',
  'Total clubs': '\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0623\u0646\u062F\u064A\u0629', 'Active clubs': '\u0627\u0644\u0623\u0646\u062F\u064A\u0629 \u0627\u0644\u0646\u0634\u0637\u0629', 'Suspended clubs': '\u0627\u0644\u0623\u0646\u062F\u064A\u0629 \u0627\u0644\u0645\u0648\u0642\u0648\u0641\u0629', 'Total users': '\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645\u064A\u0646', 'Total clients': '\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0639\u0645\u0644\u0627\u0621'
})

Object.assign(arabic, {
  'Training program': '\u0628\u0631\u0646\u0627\u0645\u062C \u062A\u062F\u0631\u064A\u0628\u064A',
  'Program details': '\u062A\u0641\u0627\u0635\u064A\u0644 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Program exercises': '\u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'No exercises added yet.': '\u0644\u0645 \u062A\u064F\u0636\u0641 \u062A\u0645\u0627\u0631\u064A\u0646 \u0628\u0639\u062F.',
  'Edit program': '\u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Program name': '\u0627\u0633\u0645 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Duration in days': '\u0627\u0644\u0645\u062F\u0629 \u0628\u0627\u0644\u0623\u064A\u0627\u0645',
  'Save changes': '\u062D\u0641\u0638 \u0627\u0644\u062A\u063A\u064A\u064A\u0631\u0627\u062A',
  'Add an exercise': '\u0625\u0636\u0627\u0641\u0629 \u062A\u0645\u0631\u064A\u0646',
  'Exercise': '\u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Sets': '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0627\u062A',
  'Reps': '\u0627\u0644\u062A\u0643\u0631\u0627\u0631\u0627\u062A',
  'Day': '\u0627\u0644\u064A\u0648\u0645',
  'Day number': '\u0631\u0642\u0645 \u0627\u0644\u064A\u0648\u0645',
  'Add exercise': '\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Edit a program exercise': '\u062A\u0639\u062F\u064A\u0644 \u062A\u0645\u0631\u064A\u0646 \u0641\u064A \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Choose an exercise to edit': '\u0627\u062E\u062A\u0631 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0627\u0644\u0645\u0631\u0627\u062F \u062A\u0639\u062F\u064A\u0644\u0647',
  'Rest seconds': '\u0648\u0642\u062A \u0627\u0644\u0631\u0627\u062D\u0629 \u0628\u0627\u0644\u062B\u0648\u0627\u0646\u064A',
  'Save exercise changes': '\u062D\u0641\u0638 \u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Delete exercise': '\u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Reorder exercises': '\u062A\u0631\u062A\u064A\u0628 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Exercise order': '\u062A\u0631\u062A\u064A\u0628 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Save order': '\u062D\u0641\u0638 \u0627\u0644\u062A\u0631\u062A\u064A\u0628',
  'Archive program': '\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Duplicate program': '\u0646\u0633\u062E \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Delete draft': '\u062D\u0630\u0641 \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Save as template': '\u062D\u0641\u0638 \u0643\u0642\u0627\u0644\u0628',
  'Templates': '\u0627\u0644\u0642\u0648\u0627\u0644\u0628',
  'No templates available.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0642\u0648\u0627\u0644\u0628 \u0645\u062A\u0627\u062D\u0629.',
  'Choose a template': '\u0627\u062E\u062A\u0631 \u0642\u0627\u0644\u0628\u0627\u064B',
  'Client (optional)': '\u0627\u0644\u0639\u0645\u064A\u0644 (\u0627\u062E\u062A\u064A\u0627\u0631\u064A)',
  'Create program from template': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0631\u0646\u0627\u0645\u062C \u0645\u0646 \u0642\u0627\u0644\u0628',
  'Draft created. Add exercises, then assign it to a client.': '\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0645\u0633\u0648\u062F\u0629. \u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u062B\u0645 \u0623\u0633\u0646\u062F\u0647\u0627 \u0644\u0644\u0639\u0645\u064A\u0644.',
  'Program assigned.': '\u062A\u0645 \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Draft deleted.': '\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u0645\u0633\u0648\u062F\u0629.',
  'Program archived.': '\u062A\u0645\u062A \u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Program duplicated.': '\u062A\u0645 \u0646\u0633\u062E \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Assigning…': '\u062C\u0627\u0631\u064D \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C...',
  'Creating…': '\u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0646\u0634\u0627\u0621...',
  'Duration not set': '\u0627\u0644\u0645\u062F\u0629 \u063A\u064A\u0631 \u0645\u062D\u062F\u062F\u0629',
  'Assigned to': '\u0645\u0633\u0646\u062F \u0625\u0644\u0649',
  'Record': '\u0633\u062C\u0644',
  'Item': '\u0639\u0646\u0635\u0631',
  'No items available': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0639\u0646\u0627\u0635\u0631 \u0645\u062A\u0627\u062D\u0629'
})

Object.assign(arabic, {
  'Training programs': '\u0627\u0644\u0628\u0631\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628\u064A\u0629',
  'Create a program, add its exercises, then assign it to a client.': '\u0623\u0646\u0634\u0626 \u0628\u0631\u0646\u0627\u0645\u062C\u0627\u064B\u060C \u0648\u0623\u0636\u0641 \u062A\u0645\u0627\u0631\u064A\u0646\u0647\u060C \u062B\u0645 \u0623\u0633\u0646\u062F\u0647 \u0644\u0639\u0645\u064A\u0644.',
  'Create a program': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0631\u0646\u0627\u0645\u062C',
  'Example: Strength foundation': '\u0645\u062B\u0627\u0644: \u0628\u0631\u0646\u0627\u0645\u062C \u062A\u0623\u0633\u064A\u0633 \u0627\u0644\u0642\u0648\u0629',
  'Create draft': '\u0625\u0646\u0634\u0627\u0621 \u0645\u0633\u0648\u062F\u0629',
  'Add exercises to the draft, then choose the client and start date.': '\u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0625\u0644\u0649 \u0627\u0644\u0645\u0633\u0648\u062F\u0629\u060C \u062B\u0645 \u0627\u062E\u062A\u0631 \u0627\u0644\u0639\u0645\u064A\u0644 \u0648\u062A\u0627\u0631\u064A\u062E \u0627\u0644\u0628\u062F\u0621.',
  'Workspace programs': '\u0628\u0631\u0627\u0645\u062C \u0627\u0644\u0646\u0627\u062F\u064A',
  'Open a program to review or edit it. Status and assignment are shown below.': '\u0627\u0641\u062A\u062D \u0628\u0631\u0646\u0627\u0645\u062C\u0627\u064B \u0644\u0645\u0631\u0627\u062C\u0639\u062A\u0647 \u0623\u0648 \u062A\u0639\u062F\u064A\u0644\u0647. \u062A\u0638\u0647\u0631 \u0627\u0644\u062D\u0627\u0644\u0629 \u0648\u0627\u0644\u0625\u0633\u0646\u0627\u062F \u0623\u062F\u0646\u0627\u0647.',
  'programs': '\u0628\u0631\u0627\u0645\u062C',
  'No programs yet. Create your first draft above.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0631\u0627\u0645\u062C \u0628\u0639\u062F. \u0623\u0646\u0634\u0626 \u0623\u0648\u0644 \u0645\u0633\u0648\u062F\u0629 \u0645\u0646 \u0627\u0644\u0623\u0639\u0644\u0649.',
  'Draft created. Add exercises, then assign it to a client.': '\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0645\u0633\u0648\u062F\u0629. \u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u062B\u0645 \u0623\u0633\u0646\u062F\u0647\u0627 \u0644\u0644\u0639\u0645\u064A\u0644.',
  'Program assigned.': '\u062A\u0645 \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Manage': '\u0625\u062F\u0627\u0631\u0629',
  'Not assigned': '\u063A\u064A\u0631 \u0645\u0633\u0646\u062F',
  'days': '\u0623\u064A\u0627\u0645',
  'Record': '\u0633\u062C\u0644',
  'Item': '\u0639\u0646\u0635\u0631',
  'No items available': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0639\u0646\u0627\u0635\u0631 \u0645\u062A\u0627\u062D\u0629',
  'Choose an item': '\u0627\u062E\u062A\u0631 \u0639\u0646\u0635\u0631\u0627\u064B'
})

Object.assign(arabic, {
  'Program setup': '\u0625\u0639\u062F\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Program exercises are ready. Assign this program when you are ready.': '\u062A\u0645\u062A \u0625\u0636\u0627\u0641\u0629 \u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C. \u064A\u0645\u0643\u0646\u0643 \u0625\u0633\u0646\u0627\u062F\u0647 \u0644\u0639\u0645\u064A\u0644 \u0627\u0644\u0622\u0646.',
  'Step 1 of 2: add at least one exercise. Then assign the program to a client.': '\u0627\u0644\u062E\u0637\u0648\u0629 1 \u0645\u0646 2: \u0623\u0636\u0641 \u062A\u0645\u0631\u064A\u0646\u0627\u064B \u0648\u0627\u062D\u062F\u0627\u064B \u0639\u0644\u0649 \u0627\u0644\u0623\u0642\u0644\u060C \u062B\u0645 \u0623\u0633\u0646\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C \u0644\u0639\u0645\u064A\u0644.',
  'Step 2: assign to a client': '\u0627\u0644\u062E\u0637\u0648\u0629 2: \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C \u0644\u0639\u0645\u064A\u0644',
  'Close details': '\u0625\u063A\u0644\u0627\u0642 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644',
  'Loading program exercises...': '\u062C\u0627\u0631\u064D \u062A\u062D\u0645\u064A\u0644 \u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C…',
  'Add an exercise above to unlock assignment.': '\u0623\u0636\u0641 \u062A\u0645\u0631\u064A\u0646\u0627\u064B \u0623\u0639\u0644\u0627\u0647 \u0644\u0644\u0627\u0646\u062A\u0642\u0627\u0644 \u0625\u0644\u0649 \u062E\u0637\u0648\u0629 \u0627\u0644\u0625\u0633\u0646\u0627\u062F.',
  'AI draft review': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0645\u0633\u0648\u062F\u0627\u062A \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A',
  'Review generated training and meal plans before adding them to your workspace.': '\u0631\u0627\u062C\u0639 \u062E\u0637\u062A\u064A \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u0627\u0644\u0648\u062C\u0628\u0627\u062A \u0627\u0644\u0645\u0648\u0644\u062F\u062A\u064A\u0646 \u0642\u0628\u0644 \u0625\u0636\u0627\u0641\u062A\u0647\u0645\u0627 \u0625\u0644\u0649 \u0627\u0644\u0646\u0627\u062F\u064A.',
  'Saved AI drafts': '\u0645\u0633\u0648\u062F\u0627\u062A \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A \u0627\u0644\u0645\u062D\u0641\u0648\u0638\u0629',
  'Choose a draft to inspect its plan, resolve unmatched exercises, and approve it.': '\u0627\u062E\u062A\u0631 \u0645\u0633\u0648\u062F\u0629 \u0644\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u062E\u0637\u0629\u060C \u0648\u062D\u0644 \u0645\u0634\u0643\u0644\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u063A\u064A\u0631 \u0627\u0644\u0645\u0637\u0627\u0628\u0642\u0629\u060C \u062B\u0645 \u0627\u0639\u062A\u0645\u0627\u062F\u0647\u0627.',
  'Back to AI tools': '\u0627\u0644\u0639\u0648\u062F\u0629 \u0625\u0644\u0649 \u0623\u062F\u0648\u0627\u062A \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A',
  'No saved drafts yet. Generate a program first.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0645\u0633\u0648\u062F\u0627\u062A \u0645\u062D\u0641\u0648\u0638\u0629. \u0623\u0646\u0634\u0626 \u0628\u0631\u0646\u0627\u0645\u062C\u0627\u064B \u0623\u0648\u0644\u0627\u064B.',
  'Generated with': '\u0627\u0644\u0646\u0645\u0648\u0630\u062C', 'AI model': '\u0646\u0645\u0648\u0630\u062C \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A',
  'Review draft for': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0645\u0633\u0648\u062F\u0629 \u0627\u0644\u0639\u0645\u064A\u0644', 'Prompt version': '\u0646\u0633\u062E\u0629 \u0627\u0644\u062A\u0639\u0644\u064A\u0645\u0627\u062A', 'Model': '\u0627\u0644\u0646\u0645\u0648\u0630\u062C',
  'Exercises need attention before approval': '\u062A\u062D\u062A\u0627\u062C \u0628\u0639\u0636 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0625\u0644\u0649 \u0645\u0631\u0627\u062C\u0639\u0629 \u0642\u0628\u0644 \u0627\u0644\u0627\u0639\u062A\u0645\u0627\u062F',
  'Replace unmatched exercises with items from the exercise library, then save the draft.': '\u0627\u0633\u062A\u0628\u062F\u0644 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u063A\u064A\u0631 \u0627\u0644\u0645\u0637\u0627\u0628\u0642\u0629 \u0628\u062A\u0645\u0627\u0631\u064A\u0646 \u0645\u0646 \u0627\u0644\u0645\u0643\u062A\u0628\u0629\u060C \u062B\u0645 \u0627\u062D\u0641\u0638 \u0627\u0644\u0645\u0633\u0648\u062F\u0629.',
  'Training plan preview': '\u0645\u0639\u0627\u064A\u0646\u0629 \u062E\u0637\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Meal plan preview': '\u0645\u0639\u0627\u064A\u0646\u0629 \u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A',
  'Advanced draft editing': '\u062A\u0639\u062F\u064A\u0644 \u0645\u062A\u0642\u062F\u0645 \u0644\u0644\u0645\u0633\u0648\u062F\u0629', 'Edit structured plan JSON only when needed. Invalid JSON will not be saved.': '\u0644\u0627 \u062A\u0639\u062F\u0651\u0644 \u0628\u064A\u0627\u0646\u0627\u062A JSON \u0625\u0644\u0627 \u0639\u0646\u062F \u0627\u0644\u062D\u0627\u062C\u0629. \u0644\u0646 \u064A\u064F\u062D\u0641\u0638 \u0627\u0644\u062A\u0646\u0633\u064A\u0642 \u063A\u064A\u0631 \u0627\u0644\u0635\u0627\u0644\u062D.',
  'Training plan JSON': 'JSON \u062E\u0637\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Meal plan JSON': 'JSON \u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A', 'Unmatched exercise names, one per line': '\u0623\u0633\u0645\u0627\u0621 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u063A\u064A\u0631 \u0627\u0644\u0645\u0637\u0627\u0628\u0642\u0629\u060C \u0627\u0633\u0645 \u0641\u064A \u0643\u0644 \u0633\u0637\u0631', 'Save draft changes': '\u062D\u0641\u0638 \u062A\u0639\u062F\u064A\u0644\u0627\u062A \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Request a revision': '\u0637\u0644\u0628 \u062A\u0639\u062F\u064A\u0644', 'A revised draft is created separately. This draft remains available.': '\u0633\u064A\u064F\u0646\u0634\u0623 \u0625\u0635\u062F\u0627\u0631 \u0645\u0639\u062F\u0651\u0644 \u0643\u0645\u0633\u0648\u062F\u0629 \u062C\u062F\u064A\u062F\u0629\u060C \u0648\u0633\u062A\u0628\u0642\u0649 \u0647\u0630\u0647 \u0627\u0644\u0645\u0633\u0648\u062F\u0629 \u0645\u062A\u0627\u062D\u0629.',
  'Notes for the coach assistant': '\u0645\u0644\u0627\u062D\u0638\u0627\u062A \u0644\u0644\u0645\u0633\u0627\u0639\u062F \u0627\u0644\u062A\u062F\u0631\u064A\u0628\u064A', 'Create revised draft': '\u0625\u0646\u0634\u0627\u0621 \u0645\u0633\u0648\u062F\u0629 \u0645\u0639\u062F\u0651\u0644\u0629',
  'Approve and create program': '\u0627\u0639\u062A\u0645\u0627\u062F \u0648\u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C', 'Reject draft': '\u0631\u0641\u0636 \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Created successfully': '\u062A\u0645 \u0627\u0644\u0625\u0646\u0634\u0627\u0621 \u0628\u0646\u062C\u0627\u062D', 'The training program and meal plan are saved as drafts. Open them to review details, add exercises if needed, and assign them to a client.': '\u062D\u064F\u0641\u0638 \u0628\u0631\u0646\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A \u0643\u0645\u0633\u0648\u062F\u062A\u064A\u0646. \u0627\u0641\u062A\u062D\u0647\u0645\u0627 \u0644\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644\u060C \u0648\u0623\u0636\u0641 \u062A\u0645\u0627\u0631\u064A\u0646 \u0625\u0630\u0627 \u0644\u0632\u0645\u060C \u062B\u0645 \u0623\u0633\u0646\u062F\u0647\u0645\u0627 \u0644\u0639\u0645\u064A\u0644.',
  'Manage training program': '\u0625\u062F\u0627\u0631\u0629 \u0628\u0631\u0646\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Manage meal plan': '\u0625\u062F\u0627\u0631\u0629 \u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A',
  'Delete this draft?': '\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0647\u0630\u0647 \u0627\u0644\u0645\u0633\u0648\u062F\u0629\u061F',   'Draft changes saved.': '\\u062A\\u0645 \\u062D\\u0641\\u0638 \\u062A\\u0639\\u062F\\u064A\\u0644\\u0627\\u062A \\u0627\\u0644\\u0645\\u0633\\u0648\\u062F\\u0629.', 'A new draft was created. The original draft is preserved.': '\\u062A\\u0645 \\u0625\\u0646\\u0634\\u0627\\u0621 \\u0645\\u0633\\u0648\\u062F\\u0629 \\u062C\\u062F\\u064A\\u062F\\u0629\\u060C \\u0648\\u0627\\u0644\\u0645\\u0633\\u0648\\u062F\\u0629 \\u0627\\u0644\\u0623\\u0635\\u0644\\u064A\\u0629 \\u0645\\u0627\\u0632\\u0627\\u0644\\u062A \\u0645\\u062D\\u0641\\u0648\\u0645\\u0629.', 'Approved. A draft training program and meal plan were created. They still need to be assigned to a client.': '\\u062A\\u0645 \\u0627\\u0644\\u0627\\u0639\\u062A\\u0645\\u0627\\u062F. \\u0623\\u064F\\u0646\\u0634\\u0626\\u062A \\u0645\\u0633\\u0648\\u062F\\u0629 \\u0644\\u0644\\u062A\\u062F\\u0631\\u064A\\u0628 \\u0648\\u0623\\u062E\\u0631\\u0649 \\u0644\\u0644\\u0648\\u062C\\u0628\\u0627\\u062A. \\u0623\\u0633\\u0646\\u062F\\u0647\\u0645\\u0627 \\u0644\\u0639\\u0645\\u064A\\u0644 \\u0644\\u0628\\u062F\\u0621 \\u0627\\u0644\\u0627\\u0633\\u062A\\u062E\\u062F\\u0627\\u0645.', 'Draft rejected.': '\\u062A\\u0645 \\u0631\\u0641\\u0636 \\u0627\\u0644\\u0645\\u0633\\u0648\\u062F\\u0629.', 'Progress summary created.': '\\u062A\\u0645 \\u0625\\u0646\\u0634\\u0627\\u0621 \\u0645\\u0644\\u062E\\u0635 \\u0627\\u0644\\u062A\\u0642\\u062F\\u0645.',   'Select a draft above.': '\u0627\u062E\u062A\u0631 \u0645\u0633\u0648\u062F\u0629 \u0645\u0646 \u0627\u0644\u0623\u0639\u0644\u0649.', 'Client progress summary': '\u0645\u0644\u062E\u0635 \u062A\u0642\u062F\u0645 \u0627\u0644\u0639\u0645\u064A\u0644', 'Choose a client': '\u0627\u062E\u062A\u0631 \u0639\u0645\u064A\u0644\u0627\u064B', 'Coach notes': '\u0645\u0644\u0627\u062D\u0638\u0627\u062A \u0627\u0644\u0645\u062F\u0631\u0628', 'Generate summary': '\u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0645\u0644\u062E\u0635', 'Churn risk history': '\u0633\u062C\u0644 \u0645\u062E\u0627\u0637\u0631 \u0627\u0644\u0645\u063A\u0627\u062F\u0631\u0629', 'Select a client to see historical scores.': '\u0627\u062E\u062A\u0631 \u0639\u0645\u064A\u0644\u0627\u064B \u0644\u0639\u0631\u0636 \u062A\u0627\u0631\u064A\u062E \u0627\u0644\u062E\u0637\u0631.'
})
Object.assign(arabic, {
  'Exercise library': '\u0645\u0643\u062A\u0628\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Find, add, and maintain the exercises used in your training programs.': '\u0627\u0628\u062D\u062B \u0639\u0646 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645\u0629 \u0641\u064A \u0628\u0631\u0627\u0645\u062C\u0643 \u0648\u0623\u0636\u0641\u0647\u0627 \u0648\u062D\u062F\u0651\u062B\u0647\u0627.',
  'Filter exercises': '\u062A\u0635\u0641\u064A\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646', 'Narrow the library by muscle group, equipment, or name.': '\u062D\u062F\u062F \u0627\u0644\u0646\u062A\u0627\u0626\u062C \u0628\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629 \u0623\u0648 \u0627\u0644\u0645\u0639\u062F\u0627\u062A \u0623\u0648 \u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.',
  'Muscle groups': '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0627\u062A \u0627\u0644\u0639\u0636\u0644\u064A\u0629', 'Equipment types': '\u0623\u0646\u0648\u0627\u0639 \u0627\u0644\u0645\u0639\u062F\u0627\u062A',
  'Search exercises': '\u0627\u0644\u0628\u062D\u062B \u0639\u0646 \u062A\u0645\u0631\u064A\u0646', 'Search by exercise name': '\u0627\u0628\u062D\u062B \u0628\u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Muscle group': '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629', 'All muscle groups': '\u0643\u0644 \u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0627\u062A \u0627\u0644\u0639\u0636\u0644\u064A\u0629', 'Equipment': '\u0627\u0644\u0645\u0639\u062F\u0627\u062A', 'All equipment': '\u0643\u0644 \u0627\u0644\u0645\u0639\u062F\u0627\u062A',
  'Add an exercise': '\u0625\u0636\u0627\u0641\u0629 \u062A\u0645\u0631\u064A\u0646', 'Add a name and muscle group. An HTTPS video link is optional.': '\u0623\u062F\u062E\u0644 \u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0648\u0645\u062C\u0645\u0648\u0639\u062A\u0647 \u0627\u0644\u0639\u0636\u0644\u064A\u0629. \u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648 \u0627\u062E\u062A\u064A\u0627\u0631\u064A \u0648\u064A\u062C\u0628 \u0623\u0646 \u064A\u0628\u062F\u0623 \u0628\u0640 HTTPS.',
  'Exercise name': '\u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646', 'Video link (optional)': '\u0631\u0627\u0628\u0637 \u0641\u064A\u062F\u064A\u0648 (\u0627\u062E\u062A\u064A\u0627\u0631\u064A)', 'Add exercise': '\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Exercise added to your library.': '\u0623\u064F\u0636\u064A\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0625\u0644\u0649 \u0627\u0644\u0645\u0643\u062A\u0628\u0629.', 'Could not add the exercise.': '\u062A\u0639\u0630\u0631\u062A \u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Exercise details saved.': '\u062A\u0645 \u062D\u0641\u0638 \u062A\u0639\u062F\u064A\u0644\u0627\u062A \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Could not save the exercise.': '\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Exercise deleted.': '\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Could not delete the exercise.': '\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.',
  'Exercises': '\u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646', 'Select an exercise to view its video and edit its details.': '\u0627\u062E\u062A\u0631 \u062A\u0645\u0631\u064A\u0646\u0627\u064B \u0644\u0645\u0634\u0627\u0647\u062F\u0629 \u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648 \u0648\u062A\u0639\u062F\u064A\u0644 \u062A\u0641\u0627\u0635\u064A\u0644\u0647.', 'exercises': '\u062A\u0645\u0627\u0631\u064A\u0646', 'No exercises match these filters.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u062A\u0645\u0627\u0631\u064A\u0646 \u062A\u0637\u0627\u0628\u0642 \u0647\u0630\u0647 \u0627\u0644\u0641\u0644\u0627\u062A\u0631.', 'No muscle group selected': '\u0644\u0645 \u062A\u062D\u062F\u062F \u0645\u062C\u0645\u0648\u0639\u0629 \u0639\u0636\u0644\u064A\u0629', 'No equipment specified': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0645\u0639\u062F\u0627\u062A \u0645\u062D\u062F\u062F\u0629', 'Video available': '\u064A\u062A\u0648\u0641\u0631 \u0641\u064A\u062F\u064A\u0648', 'Previous': '\u0627\u0644\u0633\u0627\u0628\u0642', 'Next': '\u0627\u0644\u062A\u0627\u0644\u064A',
  'Exercise details': '\u062A\u0641\u0627\u0635\u064A\u0644 \u0627\u0644\u062A\u0645\u0631\u064A\u0646', 'Update its information, video link, or remove it from the library.': '\u062D\u062F\u0651\u062B \u0628\u064A\u0627\u0646\u0627\u062A\u0647 \u0623\u0648 \u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648\u060C \u0623\u0648 \u0627\u062D\u0630\u0641\u0647 \u0645\u0646 \u0627\u0644\u0645\u0643\u062A\u0628\u0629.', 'Close details': '\u0625\u063A\u0644\u0627\u0642 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644', 'Delete this exercise?': '\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0647\u0630\u0627 \u0627\u0644\u062A\u0645\u0631\u064A\u0646\u061F'
})
Object.assign(arabic, {
  'No video link': '\u0644\u0627 \u064A\u0648\u062C\u062F \u0631\u0627\u0628\u0637 \u0641\u064A\u062F\u064A\u0648',
  'Selected': '\u0645\u062D\u062F\u062F',
  'View details': '\u0639\u0631\u0636 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644'
})
Object.assign(arabic, {
  'Muscle': '\u0627\u0644\u0639\u0636\u0644\u0629',
  'Not specified': '\u063A\u064A\u0631 \u0645\u062D\u062F\u062F'
})
// Include all dictionaries appended below the translation helpers.
Object.assign(repairedEnglish, Object.fromEntries(Object.entries(arabic).map(([key, value]) => [repairLegacyArabic(value), key])))
