import { readFile, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const frontend = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const root = path.resolve(frontend, '..')
const swagger = JSON.parse(await readFile(path.join(root, 'docs/swagger.json'), 'utf8'))
const inventory = await readFile(path.join(root, 'docs/FRONTEND_ROUTE_INVENTORY.md'), 'utf8')
const present = new Set()
for (const line of inventory.split('\n')) {
  const match = line.match(/^\| (GET|POST|PUT|DELETE|PATCH) \| `([^`]+)` \| \u2705 \|/)
  if (match) present.add(`${match[1]} ${match[2]}`)
}

const rows = []
const totals = { present: 0, duplicate: 0, legacy: 0, backendOnly: 0, missing: 0 }
for (const [route, operations] of Object.entries(swagger.paths)) {
  for (const [method, operation] of Object.entries(operations)) {
    if (!['get', 'post', 'put', 'delete', 'patch'].includes(method)) continue
    const key = `${method.toUpperCase()} ${route}`
    let status
    let rationale
    if (present.has(key)) {
      status = 'Frontend call site found; runtime coverage varies'
      rationale = 'A matching frontend call exists. See the runtime verification section for covered Playwright paths; other operations need route-specific acceptance checks.'
      totals.present++
    } else if (key === 'GET /api/my/program') {
      status = 'Duplicate/superseded frontend operation'
      rationale = 'The client workspace uses `/api/my/program/today` for the daily workout and `/api/my/programs` for program history. The older all-days response is not needed by the current workflow.'
      totals.duplicate++
    } else if (key === 'GET /api/tenant/plans') {
      status = 'Duplicate frontend operation'
      rationale = 'Owner plan management already loads the same tenant-scoped plan fields through `/api/plans`, which is also used by clients and coaches. No additional workflow depends on this owner-only alias.'
      totals.duplicate++
    } else if (operation.deprecated) {
      status = 'Intentional legacy compatibility route'
      rationale = 'The canonical route is the UI contract; this deprecated operation remains for existing clients.'
      totals.legacy++
    } else if (route.startsWith('/api/webhooks/')) {
      status = 'Intentional backend-only provider callback'
      rationale = 'Payment providers call this endpoint; the browser must not call it.'
      totals.backendOnly++
    } else {
      status = 'Missing frontend workflow'
      rationale = 'No matching literal frontend call was found. Add a role-appropriate UI flow or document a specific product reason to keep this operation backend-only.'
      totals.missing++
    }
    rows.push(`| ${method.toUpperCase()} | \`${route}\` | ${status} | ${rationale} |`)
  }
}

const report = [
  '# API Integration Audit',
  '',
  'Generated from `docs/swagger.json` and `docs/FRONTEND_ROUTE_INVENTORY.md` by `CoachPulse.Frontend/scripts/api-integration-audit.mjs`.',
  '',
  'A matching call site is not equivalent to an end-to-end verified workflow. The runtime section records the covered Playwright journey; remaining operations need route-specific acceptance checks.',
  '',
  `- Swagger operations: ${rows.length}`,
  `- Frontend call sites found: ${totals.present}`,
  `- Duplicate/superseded operations: ${totals.duplicate}`,
  `- Deprecated compatibility operations: ${totals.legacy}`,
  `- Intentional backend-only callbacks: ${totals.backendOnly}`,
  `- Missing frontend workflows: ${totals.missing}`,
  `- Reconciled total: ${totals.present + totals.duplicate + totals.legacy + totals.backendOnly + totals.missing}`,
  '',
  '| Method | Backend operation | Classification | Reason / follow-up |',
  '|---|---|---|---|',
  ...rows,
  '',
  '## Frontend verification (2026-10-23)',
  '',
  '- `npm ci` completed with Node v22.17.1 and npm 10.8.3 using a writable temporary cache and increased registry fetch timeout/retries; the lockfile was preserved.',
  '- Frontend typecheck, lint, production build, translation source check, API audit, and `git diff --check` passed. Vite reports a 716.54 kB minified JavaScript chunk warning.',
  '- Playwright rendered login and all four role dashboards at 360, 768, and 1440 px; all five tests passed. Dashboard shell overflow checks passed at each viewport, with owner sidebar collapse, mobile topbar/drawer, client dashboard data, and exercise video interaction exercised.',
  '- Playwright used mocked API responses for visual and workflow checks. No backend runtime integration was performed during this frontend pass.',
  '',
  '## Previous integrated runtime verification (2026-10-18)',
  '',
  'Docker Compose ran at `http://127.0.0.1:8088` because the default host port 8080 was occupied. `/healthz` and `/readyz` both returned 200; readiness reported PostgreSQL, startup initialization, and Python AI ready. The AI container had no published host port.',
  '',
  '- Verified: .NET build and 114 tests; frontend typecheck, lint, and build; Python tests (6 passed); Playwright (2/2), including registration/refresh rotation, plans, invitation/onboarding, program assignment, InstaPay confirmation, Check-in feedback, cross-browser SignalR chat, and notification persistence.',
  '- Static matching shows where a request is wired, not whether every route and failure case was runtime-tested.',
  '- Remaining checks: exhaustive known-ID tenant-isolation tests, full rate-limit coverage, log-redaction assertions, and route-specific acceptance tests.',
  '',
].join('\n')

await writeFile(path.join(root, 'docs/API_INTEGRATION_AUDIT.md'), report)
console.log(`Wrote audit for ${rows.length} Swagger operations; ${totals.missing} active workflows missing.`)
