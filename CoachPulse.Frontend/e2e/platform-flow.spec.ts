import { expect, test, type Page } from '@playwright/test'
import { randomUUID } from 'node:crypto'

const apiOrigin = process.env.PLAYWRIGHT_API_URL || 'http://localhost:5006/api'
const receipt = '%PDF-1.4\n%%EOF'

test('Vite client renders the sign-in screen', async ({ page }) => {
  await page.goto('/login')
  await expect(page.locator('.auth-layout')).toBeVisible()
  await expect(page.locator('.login-tenant-field')).toBeVisible()
  await expect(page.locator('input[type="email"]')).toBeVisible()
  await expect(page.locator('input[autocomplete="current-password"]')).toBeVisible()
  await expect(page.locator('.auth-paths')).toHaveCSS('display', 'grid')
  await expect(page.locator('.auth-path-card')).toHaveCount(2)
})

test('bootstrap SuperAdmin is routed through the required password change', async ({ page }) => {
  const payload = btoa(JSON.stringify({ mustChangePassword: 'true' })).replaceAll('+', '-').replaceAll('/', '_').replaceAll('=', '')
  await page.route('**/api/auth/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ accessToken: `header.${payload}.signature`, refreshToken: 'refresh', expiresIn: 900, user: { id: 'admin-id', name: 'Admin', role: 'SuperAdmin' }, tenant: null }),
  }))
  await page.route('**/api/public/tenants', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([]) }))
  await page.route('**/api/account/change-password', route => route.fulfill({ status: 204 }))

  await page.goto('/login')
  await page.locator('.club-picker-trigger').click()
  await page.getByRole('option', { name: /SuperAdmin/ }).click()
  await page.locator('input[autocomplete="username"]').fill('admin@example.test')
  await page.locator('input[autocomplete="current-password"]').fill('Bootstrap-Password1!')
  await page.locator('.login-card button').filter({ hasText: 'Sign in' }).click()
  await expect(page).toHaveURL(/\/first-login-password$/)

  await page.locator('input[autocomplete="current-password"]').fill('Bootstrap-Password1!')
  await page.locator('input[autocomplete="new-password"]').nth(0).fill('New-Admin-Password2!')
  await page.locator('input[autocomplete="new-password"]').nth(1).fill('New-Admin-Password2!')
  await page.locator('.login-card form button').first().click()
  await expect(page.locator('.login-card h1')).toBeVisible()
  await expect(page.locator('input[autocomplete="new-password"]')).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('coachpulse.accessToken'))).toBeNull()
})

test('SuperAdmin dashboard renders summaries and tables without horizontal overflow', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await page.addInitScript(() => sessionStorage.setItem('coachpulse.accessToken', 'test-token'))
  await page.route('**/api/account/me', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ id: 'admin', name: 'Admin', role: 'SuperAdmin', tenantId: null }) }))
  await page.route('**/api/superadmin/platform-analytics', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ totalTenants: 3, activeTenants: 2, suspendedTenants: 1, totalUsers: 12, totalClients: 8 }) }))
  await page.route('**/api/superadmin/tenants**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ id: 'tenant-1', name: 'Studio North', slug: 'studio-north', status: 'Active', createdAt: '2026-10-17T12:00:00Z' }]) }))
  await page.route('**/api/superadmin/ai/usage', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ tenantId: 'tenant-1', usedToday: 4 }]) }))
  await page.route('**/api/superadmin/audit-logs**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [{ id: 'audit-1', tenantId: 'tenant-1', action: 'tenant.activated', actorRole: 'SuperAdmin', entityType: 'Tenant', createdAt: '2026-10-17T12:00:00Z' }], page: 1, pageSize: 20, total: 1 }) }))
  await page.route('**/api/superadmin/webhook-events**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], page: 1, pageSize: 20, total: 0 }) }))

  await page.goto('/admin/dashboard')
  await expect(page.locator('.admin-dashboard .page-header h1')).toBeVisible()
  await page.getByRole('button', { name: 'Browse clubs' }).click()
  await expect(page.getByRole('cell', { name: 'Studio North' }).first()).toBeVisible()
  await expect(page.locator('.admin-metric-grid .admin-metric')).toHaveCount(5)
  await expect(page.locator('.admin-dashboard pre')).toHaveCount(0)
  await page.locator('.admin-table .table-action').first().click()
  await expect(page.locator('.admin-tenant-manager select')).toBeVisible()
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
})

async function api(page: Page, method: 'GET' | 'PUT' | 'POST', path: string, data?: unknown) {
  const response = await page.evaluate(async ({ url, requestMethod, requestData }) => {
    const token = sessionStorage.getItem('coachpulse.accessToken')
    const result = await fetch(url, {
      method: requestMethod,
      headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}), ...(requestData ? { 'Content-Type': 'application/json' } : {}) },
      ...(requestData ? { body: JSON.stringify(requestData) } : {}),
    })
    return { status: result.status, body: await result.text() }
  }, {
    url: `${apiOrigin}${path}`,
    requestMethod: method,
    requestData: data,
  })
  return {
    ok: () => response.status >= 200 && response.status < 300,
    status: () => response.status,
    text: async () => response.body,
    json: async () => JSON.parse(response.body) as unknown,
  }
}

async function readJson(response: Awaited<ReturnType<typeof api>>) {
  expect(response.ok(), await response.text()).toBeTruthy()
  if (response.status() === 204) return null
  return response.json()
}

async function loginTenant(page: Page, slug: string, email: string, password: string) {
  await page.goto('/login')
  await page.getByLabel('رابط النادي').fill(slug)
  await page.getByLabel('البريد الإلكتروني').fill(email)
  await page.getByLabel('كلمة المرور').fill(password)
  await page.getByRole('button', { name: 'دخول' }).click()
  await expect(page).toHaveURL(/\/app\/dashboard/)
}

test('coach and client complete onboarding, subscription, payment, check-in, feedback and chat', async ({ browser }) => {
  test.setTimeout(240_000)
  const suffix = randomUUID().replaceAll('-', '').slice(0, 12)
  const slug = `e2e-${suffix}`
  const coachEmail = `coach-${suffix}@example.test`
  const clientEmail = `client-${suffix}@example.test`
  const password = 'CoachPulse-e2e!2026'
  const coach = await browser.newPage()
  await coach.addInitScript(() => localStorage.setItem('coachpulse.language.v5', 'ar'))

  await coach.goto('/register/tenant')
  await coach.getByLabel('الاسم').fill('E2E Coach')
  await coach.getByLabel('البريد الإلكتروني').fill(coachEmail)
  await coach.getByLabel('اسم النادي').fill(`E2E ${suffix}`)
  await coach.getByLabel('رابط النادي').fill(slug)
  await coach.getByLabel('كلمة المرور').fill(password)
  await coach.getByRole('button', { name: 'متابعة' }).click()
  await expect(coach).toHaveURL(/\/app\/dashboard/)
  const originalRefresh = await coach.evaluate(() => sessionStorage.getItem('coachpulse.refreshToken'))
  await coach.evaluate(() => sessionStorage.setItem('coachpulse.accessToken', 'expired-access-token'))
  await coach.goto('/app/dashboard') // account + dashboard requests race through the shared refresh queue.
  await expect(coach.getByRole('heading', { name: 'نظرة عامة' })).toBeVisible()
  await expect.poll(() => coach.evaluate(() => sessionStorage.getItem('coachpulse.refreshToken'))).not.toBe(originalRefresh)

  await coach.getByRole('link', { name: 'الباقات' }).click()
  await coach.getByLabel('اسم الباقة').fill('E2E Monthly')
  await coach.getByLabel('السعر').fill('25')
  await coach.getByLabel('المدة بالأيام').fill('30')
  await coach.getByRole('button', { name: 'إنشاء باقة' }).click()
  await expect(coach.getByText('تم إنشاء الباقة.')).toBeVisible()
  const plans = await readJson(await api(coach, 'GET', '/tenant/plans')) as { items?: { id?: string }[] }
  const plan = plans.items?.[0]
  expect(plan?.id, `Expected a created tenant plan in /tenant/plans response: ${JSON.stringify(plans)}`).toBeTruthy()
  const planId = plan!.id as string

  await readJson(await api(coach, 'PUT', '/tenant/payment-settings', {
    paymobEnabled: false, instaPayEnabled: true, instaPayId: `e2e${suffix}`, instaPayAccountNumber: null,
  }))
  await coach.getByRole('link', { name: 'العملاء' }).click()
  await coach.getByLabel('الاسم').fill('E2E Client')
  await coach.getByLabel('البريد الإلكتروني').fill(clientEmail)
  await coach.getByRole('button', { name: 'إرسال دعوة' }).click()
  const token = await coach.locator('code').innerText()
  expect(token.length).toBeGreaterThan(10)
  const clientPage = await browser.newPage()
  await clientPage.addInitScript(() => localStorage.setItem('coachpulse.language.v5', 'ar'))
  await clientPage.goto(`/register/client?token=${encodeURIComponent(token)}`)
  await clientPage.getByLabel('الاسم').fill('E2E Client')
  await clientPage.getByLabel('البريد الإلكتروني').fill(clientEmail)
  await clientPage.getByLabel('كلمة المرور').fill(password)
  await clientPage.getByRole('button', { name: 'متابعة' }).click()
  await expect(clientPage).toHaveURL(/\/my\/dashboard/)

  const clients = await readJson(await api(coach, 'GET', '/clients?page=1&pageSize=20')) as { items: { id?: string; email?: string }[] }
  const client = clients.items.find((item: { email?: string }) => item.email === clientEmail)
  expect(client?.id).toBeTruthy()
  const clientId = client!.id as string
  const owner = await readJson(await api(coach, 'GET', '/account/me')) as { id?: string }
  expect(owner.id).toBeTruthy()
  await readJson(await api(coach, 'PUT', `/clients/${clientId}/assign-coach`, { coachId: owner.id }))
  const assignedClient = await readJson(await api(coach, 'GET', `/clients/${clientId}`)) as { coachId?: string }
  expect(assignedClient.coachId).toBe(owner.id)

  await coach.getByRole('link', { name: 'البرامج' }).click()
  await coach.getByLabel('عنوان البرنامج').fill('E2E Program')
  await coach.getByRole('button', { name: 'إنشاء مسودة' }).click()
  const programText = await coach.getByText(/معرّف المسودة:/).innerText()
  const programId = programText.match(/[0-9a-f]{8}-[0-9a-f-]{27,}/i)?.[0]
  expect(programId).toBeTruthy()
  const assignProgramForm = coach.locator('form').filter({ has: coach.getByRole('button', { name: 'إسناد' }) })
  await assignProgramForm.getByLabel('معرّف العميل').fill(clientId)
  await assignProgramForm.getByRole('button', { name: 'إسناد' }).click()
  await expect(coach.getByText('تم إسناد البرنامج للعميل.')).toBeVisible()

  await clientPage.goto('/app/dashboard')
  await expect(clientPage).toHaveURL(/\/my\/dashboard/)

  await clientPage.getByRole('link', { name: 'التهيئة' }).click()
  await clientPage.getByLabel('الأهداف').fill('Strength and mobility')
  await clientPage.getByLabel('معلومات صحية').fill('No known restrictions')
  await clientPage.getByRole('button', { name: 'حفظ التهيئة' }).click()
  await expect(clientPage.getByText('اكتملت التهيئة.')).toBeVisible()

  await clientPage.getByRole('link', { name: 'الاشتراك والمدفوعات' }).click()
  const subscriptionForm = clientPage.locator('form').filter({ hasText: 'إنشاء طلب اشتراك' })
  await subscriptionForm.getByLabel('معرّف الباقة').fill(planId)
  await subscriptionForm.getByRole('button', { name: 'إنشاء طلب اشتراك' }).click()
  const subscriptionLabel = await clientPage.getByText(/معرّف طلب الاشتراك:/).innerText()
  const subscriptionId = subscriptionLabel.match(/[0-9a-f]{8}-[0-9a-f-]{27,}/i)?.[0]
  expect(subscriptionId).toBeTruthy()
  await clientPage.getByLabel('رقم المرجع').fill(`REF-${suffix}`)
  await clientPage.getByLabel('المبلغ').fill('25')
  await clientPage.getByLabel('إيصال الدفع').setInputFiles({ name: 'receipt.pdf', mimeType: 'application/pdf', buffer: Buffer.from(receipt) })
  await expect.poll(async () => clientPage.locator('small').filter({ hasText: 'ملف إيصال:' }).innerText()).not.toContain('—')
  await clientPage.getByRole('button', { name: 'إرسال التحويل' }).click()
  await expect(clientPage.getByText('تم إرسال التحويل للمراجعة.')).toBeVisible()
  await coach.getByRole('link', { name: 'المدفوعات' }).click()
  await coach.getByRole('button', { name: 'تأكيد' }).first().click()

  await clientPage.getByRole('link', { name: 'Check-ins' }).click()
  await clientPage.getByLabel('الوزن').fill('75')
  await clientPage.getByRole('button', { name: 'إرسال Check-in' }).click()
  await expect(clientPage.getByText('تم إرسال Check-in.')).toBeVisible()
  const checkIns = await readJson(await api(clientPage, 'GET', '/my/checkins')) as { items?: { id?: string }[] }
  const checkInId = checkIns.items?.[0]?.id as string
  expect(checkInId).toBeTruthy()
  await coach.getByRole('link', { name: 'العملاء' }).click()
  await coach.getByText('E2E Client').first().click()
  const feedbackForm = coach.locator('form').filter({ has: coach.getByRole('button', { name: 'إرسال التعليق' }) })
  await coach.getByLabel('معرّف Check-in للتفاصيل والتعليق').fill(checkInId)
  await feedbackForm.getByLabel('التعليق').fill('Great progress; keep going.')
  await feedbackForm.getByRole('button', { name: 'إرسال التعليق' }).click()
  await expect(coach.getByText('تم تسجيل تعليق المدرب.')).toBeVisible()

  await coach.getByRole('link', { name: 'المحادثات' }).click()
  const open = coach.getByRole('button', { name: 'فتح' }).first()
  await expect(open).toBeVisible()
  await open.click()
  await clientPage.getByRole('link', { name: 'المحادثة' }).click()
  await expect(coach.getByText('متصل الآن', { exact: true })).toBeVisible({ timeout: 15_000 })
  const chatForm = clientPage.locator('form').filter({ has: clientPage.getByRole('button', { name: 'إرسال' }) })
  await chatForm.getByLabel('رسالة', { exact: true }).fill('First live message')
  const firstMessageSent = clientPage.waitForResponse(response => new URL(response.url()).pathname === '/api/my/messages' && response.request().method() === 'POST' && response.status() === 201)
  await chatForm.getByRole('button', { name: 'إرسال' }).click()
  await firstMessageSent
  await chatForm.getByLabel('رسالة', { exact: true }).fill('Realtime confirmation')
  const secondMessageSent = clientPage.waitForResponse(response => new URL(response.url()).pathname === '/api/my/messages' && response.request().method() === 'POST' && response.status() === 201)
  await chatForm.getByRole('button', { name: 'إرسال' }).click()
  await secondMessageSent
  await expect(coach.getByText('Realtime confirmation')).toBeVisible({ timeout: 15_000 })

  await coach.getByRole('link', { name: 'الإشعارات' }).click()
  const notifications = await readJson(await api(coach, 'GET', '/notifications?page=1&pageSize=20')) as { items?: { type?: string }[] }
  expect(notifications.items?.some(item => item.type === 'MessageReceived')).toBeTruthy()
})
