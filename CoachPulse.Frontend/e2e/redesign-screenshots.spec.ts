import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import path from 'node:path'

const screenshotDirectory = path.resolve('docs/screenshots/redesign')
const productScreenshotDirectory = path.resolve('../docs/screenshots')
const viewports = [
  { width: 360, height: 800 },
  { width: 768, height: 1024 },
  { width: 1440, height: 1000 },
]

async function mockWorkspace(page: Page, role: 'TenantOwner' | 'CoachStaff' | 'Client' | 'SuperAdmin', primaryColor = '#0f766e') {
  await page.addInitScript(() => sessionStorage.setItem('coachpulse.accessToken', 'redesign-preview-token'))
  await page.route('**/*', async route => {
    const url = new URL(route.request().url())
    if (!url.pathname.startsWith('/api/')) return route.continue()
    let body: unknown = { items: [], totalCount: 0, page: 1, pageSize: 20 }
    if (url.pathname.endsWith('/account/me')) body = { id: 'preview-user', name: role === 'Client' ? 'Samira Hassan' : 'Northstar Studio', role, tenantId: 'preview-tenant' }
    else if (url.pathname.endsWith('/tenant')) body = { id: 'preview-tenant', name: 'Northstar Studio', logoUrl: '', primaryColor, secondaryColor: '#38a890' }
    else if (url.pathname.endsWith('/tenant/dashboard/summary')) body = { activeClients: 34, monthlyRevenue: 42500, expiringSubscriptionsCount: 3, alerts: { pendingCheckIns: 4, pendingPayments: 2, unreadMessages: 5 } }
    else if (url.pathname.endsWith('/my/dashboard')) body = { program: { title: 'Strength foundations' }, todayWorkout: { exercises: [{ name: 'Goblet squat' }, { name: 'Dumbbell row' }] }, subscription: { status: 'Active' }, unreadMessages: 2, nextCheckIn: '2026-10-24T10:00:00Z' }
    else if (url.pathname.endsWith('/superadmin/platform-analytics')) body = { totalTenants: 128, activeTenants: 112, suspendedTenants: 4, totalUsers: 4230, totalClients: 3810 }
    else if (url.pathname.endsWith('/superadmin/tenants')) body = { items: [{ id: 'tenant-1', name: 'Northstar Studio', slug: 'northstar', status: 'Active', createdAt: '2026-10-17T12:00:00Z' }], totalCount: 1, page: 1, pageSize: 100 }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) })
  })
}

test('captures public sign-in at mobile, tablet and desktop sizes', async ({ page }) => {
  await mkdir(screenshotDirectory, { recursive: true })
  await mkdir(productScreenshotDirectory, { recursive: true })
  await page.route('**/api/public/tenants', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [{ id: 'preview-tenant', name: 'Northstar Studio', slug: 'northstar' }] }) }))
  for (const viewport of viewports) {
    await page.setViewportSize(viewport)
    await page.goto('/login')
    await expect(page.locator('.auth-layout')).toBeVisible()
    await page.screenshot({ path: path.join(screenshotDirectory, `login-${viewport.width}.png`), fullPage: true })
    if (viewport.width === 1440) await page.screenshot({ path: path.join(productScreenshotDirectory, 'auth-login.png'), fullPage: true })
  }
})

test('shows the pending review state when the API accepts a club registration', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('coachpulse.language.v5', 'ar'))
  await page.route('**/api/auth/register-tenant', route => route.fulfill({
    status: 202,
    contentType: 'application/json',
    body: JSON.stringify({ tenantId: 'accepted-tenant', status: 'PendingApproval' }),
  }))
  await page.goto('/register/tenant')
  await page.getByLabel('الاسم').fill('Preview coach')
  await page.getByLabel('البريد الإلكتروني').fill('preview@example.test')
  await page.getByLabel('اسم النادي').fill('Preview Club')
  await page.getByLabel('رابط النادي').fill('preview-club')
  await page.getByLabel('كلمة المرور').fill('CoachPulse-preview!2026')
  await page.getByRole('button', { name: 'متابعة' }).click()
  await expect(page.getByRole('heading', { name: 'تم استلام طلبك' })).toBeVisible()
  await expect(page.getByText(/سيراجع مسؤول المنصة/)).toBeVisible()
  await expect(page.getByRole('link', { name: 'العودة لتسجيل الدخول' })).toBeVisible()
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('coachpulse.accessToken'))).toBeNull()
})

test('captures each role dashboard responsively and checks shell overflow', async ({ browser }) => {
  test.setTimeout(180_000)
  await mkdir(screenshotDirectory, { recursive: true })
  await mkdir(productScreenshotDirectory, { recursive: true })
  const roles = [
    { role: 'TenantOwner', path: '/app/dashboard', name: 'owner' },
    { role: 'CoachStaff', path: '/app/dashboard', name: 'coach' },
    { role: 'Client', path: '/my/dashboard', name: 'client' },
    { role: 'SuperAdmin', path: '/admin/dashboard', name: 'superadmin' },
  ] as const

  for (const { role, path: route, name } of roles) {
    const page = await browser.newPage()
    await mockWorkspace(page, role)
    for (const viewport of viewports) {
      await page.setViewportSize(viewport)
      await page.goto(route)
      await expect(page.locator(role === 'SuperAdmin' ? '.admin-topbar' : '.workspace-topbar')).toBeVisible()
      await page.waitForTimeout(250)
      await page.screenshot({ path: path.join(screenshotDirectory, `${name}-${viewport.width}.png`), timeout: 60_000 })
      if (viewport.width === 1440) {
        const productName = name === 'owner' ? 'tenant-owner.png' : name === 'coach' ? 'coach-workspace.png' : name === 'client' ? 'client-workspace.png' : 'superadmin-dashboard.png'
        await page.screenshot({ path: path.join(productScreenshotDirectory, productName), timeout: 60_000 })
      }
      const fits = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)
      expect(fits, `${name} overflows at ${viewport.width}px`).toBeTruthy()
      if (name === 'owner' && viewport.width === 1440) {
        await page.getByRole('button', { name: 'Collapse sidebar' }).last().click()
        await expect(page.locator('.workspace-shell')).toHaveClass(/sidebar-collapsed/)
      }
    }
    await page.close()
  }
})

test('keeps account, sign-out and notifications in the topbar outside the navigation drawer', async ({ page }) => {
  await mockWorkspace(page, 'Client')
  await page.setViewportSize({ width: 360, height: 800 })
  await page.goto('/my/dashboard')
  await page.locator('.workspace-user-menu summary').click()
  await expect(page.locator('.workspace-user-popover a')).toHaveCount(1)
  await expect(page.locator('.workspace-user-popover button')).toHaveCount(1)
  await page.locator('.workspace-user-menu summary').click()
  await page.getByRole('button', { name: 'Open navigation' }).click()
  const drawer = page.locator('#workspace-navigation')
  await expect(drawer).toBeVisible()
  await expect(drawer.locator('.logout')).toHaveCount(0)
  await expect(drawer.locator('a[href="/my/notifications"]')).toHaveCount(0)
  await expect(page.locator('.workspace-notification-link')).toBeVisible()
})

test('falls back to an accessible accent when tenant branding has poor contrast', async ({ page }) => {
  await mockWorkspace(page, 'TenantOwner', '#777777')
  await page.goto('/app/dashboard')
  const accent = await page.locator('.workspace-shell').evaluate(element => getComputedStyle(element).getPropertyValue('--tenant-primary').trim())
  expect(accent).toBe('#0f766e')
})

test('keeps Arabic RTL readable without horizontal overflow on mobile and tablet', async ({ page }) => {
  await mockWorkspace(page, 'Client')
  for (const width of [360, 768]) {
    await page.setViewportSize({ width, height: width === 360 ? 800 : 1024 })
    await page.goto('/my/dashboard')
    await page.getByLabel('Choose language').selectOption('ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('.client-shell > main')).toHaveAttribute('dir', 'rtl')
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy()
    if (width === 360) await page.screenshot({ path: path.join(screenshotDirectory, 'client-arabic-360.png'), fullPage: true })
  }
})

test('client dashboard renders the API nextCheckIn date', async ({ page }) => {
  await mockWorkspace(page, 'Client')
  await page.goto('/my/dashboard')
  await expect(page.locator('.metric').filter({ hasText: 'Next check-in' })).toContainText('Oct 24, 2026')
  await expect(page.locator('.dashboard-welcome')).toBeVisible()
  await expect(page.locator('.dashboard-welcome a')).toHaveAttribute('href', '/my/program')
})

test('client can expand an exercise and watch its demonstration video', async ({ page }) => {
  await mockWorkspace(page, 'Client')
  await page.route('**/api/my/program/today', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ dayNumber: 1, exercises: [{ id: 'program-exercise-1', exerciseId: 'exercise-1', exerciseName: 'Goblet squat', muscleGroup: 'Legs', videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ', sets: 3, reps: '10', restSeconds: 60 }] }) }))
  await page.goto('/my/program')
  const exerciseDetails = page.locator('.client-exercise-details')
  await expect(exerciseDetails).toBeVisible()
  await exerciseDetails.locator('summary').click()
  await expect(exerciseDetails.locator('iframe')).toHaveAttribute('src', 'https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ')
  await expect(exerciseDetails.getByText('Legs')).toBeVisible()
  await expect(exerciseDetails.getByText('60 seconds')).toBeVisible()
})
