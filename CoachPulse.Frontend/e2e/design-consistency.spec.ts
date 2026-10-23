import { expect, test, type Page } from '@playwright/test'
import { mkdir } from 'node:fs/promises'
import path from 'node:path'

const viewports = [{ width: 360, height: 800 }, { width: 768, height: 1024 }, { width: 1280, height: 900 }]
const workspaceRoutes = ['/dashboard', '/clients', '/coaches', '/team', '/checkins-review', '/programs', '/exercises', '/meal-plans', '/plans', '/conversations', '/payments', '/subscriptions', '/notifications', '/ai', '/ai-review', '/settings', '/analytics', '/audit', '/account']
const clientRoutes = ['/dashboard', '/onboarding', '/profile', '/progress', '/program', '/workouts', '/meal-plan', '/checkins', '/payments', '/payment/return', '/messages', '/notifications']
const adminRoutes = ['/dashboard', '/tenants', '/usage', '/webhooks', '/audit', '/tenant-management', '/exercises']
const authRoutes = ['/login', '/register/tenant', '/register/client', '/accept-staff-invite', '/forgot-password', '/reset-password', '/confirm-email', '/resend-confirmation', '/first-login-password', '/tenant/northstar', '/invitation/preview-token']
const roles = [
  { name: 'owner', role: 'TenantOwner', base: '/app', routes: workspaceRoutes },
  { name: 'coach', role: 'CoachStaff', base: '/app', routes: workspaceRoutes },
  { name: 'client', role: 'Client', base: '/my', routes: clientRoutes },
  { name: 'superadmin', role: 'SuperAdmin', base: '/admin', routes: adminRoutes },
] as const

async function fixture(page: Page, role: string) {
  await page.addInitScript(() => sessionStorage.setItem('coachpulse.accessToken', 'design-consistency-preview'))
  await page.route(url => new URL(url).pathname.startsWith('/hubs/'), async route => {
    if (new URL(route.request().url()).pathname.endsWith('/negotiate')) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ connectionId: 'design-consistency', connectionToken: 'design-consistency', negotiateVersion: 1, availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] }) })
    } else await route.fulfill({ status: 200, body: '' })
  })
  await page.routeWebSocket('**/hubs/**', socket => {
    socket.onMessage(message => { if (String(message).includes('"protocol"')) socket.send('{}\x1e') })
  })
  await page.route(url => new URL(url).pathname.startsWith('/api/'), async route => {
    const pathname = new URL(route.request().url()).pathname
    let body: unknown = { items: [], totalCount: 0, page: 1, pageSize: 20 }
    if (pathname.endsWith('/account/me')) body = { id: 'preview-user', name: 'Preview account', role, tenantId: role === 'SuperAdmin' ? null : 'preview-tenant' }
    else if (pathname.endsWith('/tenant')) body = { id: 'preview-tenant', name: 'Northstar Studio', primaryColor: '#0f766e', secondaryColor: '#38a890' }
    else if (pathname.endsWith('/public/tenants')) body = { items: [{ id: 'preview-tenant', name: 'Northstar Studio', slug: 'northstar' }] }
    else if (pathname.endsWith('/tenant/dashboard/summary')) body = { activeClients: 0, monthlyRevenue: 0, expiringSubscriptionsCount: 0, alerts: {} }
    else if (pathname.endsWith('/my/dashboard')) body = {}
    else if (pathname.endsWith('/superadmin/platform-analytics')) body = { totalTenants: 0, activeTenants: 0, suspendedTenants: 0, totalUsers: 0, totalClients: 0 }
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) })
  })
}

test('every auth and role route follows shared responsive control and typography rules', async ({ browser }) => {
  test.setTimeout(600_000)
  const output = path.resolve('docs/screenshots/consistency')
  await mkdir(output, { recursive: true })

  const routes = [
    ...authRoutes.map(route => ({ role: 'public', path: route })),
    ...roles.flatMap(({ name, role, base, routes: items }) => items.map(route => ({ role: name, identity: role, path: base + route }))),
  ].filter(route => (!process.env.DESIGN_ROUTE_FILTER || route.path === process.env.DESIGN_ROUTE_FILTER) && (!process.env.DESIGN_ROLE_FILTER || route.role === process.env.DESIGN_ROLE_FILTER))

  for (const route of routes) {
    const page = await browser.newPage()
    const pageErrors: string[] = []
    page.on('pageerror', error => pageErrors.push(error.message))
    page.on('console', async message => {
      if (message.type() === 'error') {
        const details = await Promise.all(message.args().map(argument => argument.jsonValue().catch(() => '')))
        const location = message.location()
        pageErrors.push(`${message.text()} ${details.filter(Boolean).join(' ')} at ${location.url}:${location.lineNumber}:${location.columnNumber}`)
      }
    })
    const roleFixture = roles.find(item => item.name === route.role)
    await fixture(page, roleFixture?.role ?? '')

    for (const viewport of viewports) {
      await page.setViewportSize(viewport)
      await page.goto(route.path, { waitUntil: 'domcontentloaded', timeout: 15_000 })
      await page.locator('main, .admin-dashboard, .login').first().waitFor({ state: 'visible', timeout: 10_000 })
      await page.waitForTimeout(100)
      const dimensions = await page.evaluate(() => ({ width: document.documentElement.scrollWidth, viewport: window.innerWidth }))
      expect(dimensions.width, `${route.path} horizontal overflow at ${viewport.width}px`).toBeLessThanOrEqual(dimensions.viewport)

      const sizes = await page.evaluate(() => {
        const fontSize = (element: Element) => Number.parseFloat(getComputedStyle(element).fontSize)
        const headings = [...document.querySelectorAll('h1,h2,h3')].filter(element => (element as HTMLElement).offsetParent !== null).map(element => ({ text: element.textContent?.trim(), size: fontSize(element) }))
        const body = Number.parseFloat(getComputedStyle(document.body).fontSize)
        const controls = [...document.querySelectorAll('button:not([aria-hidden="true"]),input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]),select,textarea')]
          .filter(element => (element as HTMLElement).offsetParent !== null)
          .map(element => ({ tag: element.tagName, type: (element as HTMLInputElement).type, className: (element as HTMLElement).className, parent: (element as HTMLElement).parentElement?.className, inShell: Boolean(element.closest('.workspace-shell')), parentFont: getComputedStyle(element.parentElement!).fontSize, height: (element as HTMLElement).getBoundingClientRect().height, minHeight: getComputedStyle(element).minHeight, transform: getComputedStyle(element).transform, font: fontSize(element) }))
        const choiceTargets = [...document.querySelectorAll('label:has(input[type="checkbox"]),label:has(input[type="radio"])')].filter(element => (element as HTMLElement).offsetParent !== null).map(element => (element as HTMLElement).getBoundingClientRect().height)
        return { headings, body, controls, choiceTargets }
      })
      expect([14, 16]).toContain(sizes.body)
      for (const heading of sizes.headings) expect([16, 20, 24, 28, 32], `${route.path}: heading ${heading.text} has ${heading.size}px`).toContain(heading.size)
      for (const control of sizes.controls) {
        expect(control.height, `${route.path}: ${control.tag}[type=${control.type}].${control.className} in ${control.parent} is ${control.height}px (min ${control.minHeight}, transform ${control.transform})`).toBeGreaterThanOrEqual(viewport.width <= 768 ? 44 : 36)
        if (control.tag !== 'BUTTON' && !['checkbox', 'radio', 'file', 'color'].includes(control.type)) expect(control.font, `${route.path}: ${control.tag}[type=${control.type}].${control.className} in ${control.parent} (shell=${control.inShell}, parentFont=${control.parentFont}) font is ${control.font}px`).toBeGreaterThanOrEqual(16)
      }
      if (viewport.width <= 768) for (const height of sizes.choiceTargets) expect(height, `${route.path}: checkbox/radio label touch target`).toBeGreaterThanOrEqual(44)

      const fileName = route.path.replaceAll('/', '_').replace(/^_/, '') || 'root'
      await page.screenshot({ path: path.join(output, `${route.role}-${fileName}-${viewport.width}.png`), fullPage: true, timeout: 30_000 })
    }

    // The routed SignalR preview is closed on each viewport navigation; its cancellation diagnostic is fixture teardown, not a page/runtime error.
    const unexpectedErrors = pageErrors.filter(error => !error.includes('connection was stopped during negotiation'))
    expect(unexpectedErrors, `${route.path} browser errors`).toEqual([])
    await page.close()
  }

  for (const entry of [
    { role: 'TenantOwner', path: '/app/programs', name: 'owner' },
    { role: 'CoachStaff', path: '/app/checkins-review', name: 'coach' },
    { role: 'Client', path: '/my/checkins', name: 'client' },
    { role: 'SuperAdmin', path: '/admin/tenants', name: 'superadmin' },
  ]) {
    const page = await browser.newPage({ viewport: { width: 360, height: 800 } })
    await page.addInitScript(() => localStorage.setItem('coachpulse.language.v5', 'ar'))
    await fixture(page, entry.role)
    await page.goto(entry.path, { waitUntil: 'domcontentloaded' })
    await expect(page.locator('main[dir="rtl"],.admin-dashboard[dir="rtl"]')).toBeVisible()
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
    await page.screenshot({ path: path.join(output, `${entry.name}-rtl-key-360.png`), fullPage: true })
    await page.close()
  }
})
