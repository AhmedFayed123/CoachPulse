# CoachPulse Frontend UI Audit

## Scope and approach

Audit performed on branch `ui-redesign` before implementation changes. The frontend is a React 19 + TypeScript + Vite single-page application. Most screens are composed in `src/App.tsx`; styling is plain global CSS in `src/styles.css`, with one Base UI/CVA button primitive under `components/ui/button.tsx`. Routing uses React Router, server state uses TanStack Query, Arabic/English copy and direction are provided by `src/i18n.tsx`, and icons already use `lucide-react`. No new UI framework is warranted.

Reviewed the five baseline references under `../docs/screenshots/`, route and role declarations in `src/App.tsx`, the existing Playwright scenarios, shared styles, and the available shared button component. Route-level observations below combine those references and source inspection; dynamic authenticated records are not represented in the screenshots.

## Existing route coverage

- **Public and authentication:** `/login`, `/first-login-password`, `/tenant/:slug`, `/invitation/:token`, `/register/tenant`, `/register/client`, `/accept-staff-invite`, `/forgot-password`, `/reset-password`, `/confirm-email`, `/resend-confirmation`.
- **TenantOwner and CoachStaff (`/app/*`):** dashboard, clients, coach team (owner), check-in reviews, programs, exercises, meal plans, plans (owner), conversations, payments, subscriptions, notifications, AI tools, AI review, workspace settings, analytics, audit log, account; `/app/team` aliases team management for owners.
- **Client (`/my/*`):** dashboard, onboarding, profile/health, progress, program, workout log, meal plan/log, check-ins, subscriptions/payments, payment return, messages, notifications.
- **SuperAdmin (`/admin/*`):** dashboard, tenants, AI usage, webhook events, audit, tenant management, public exercises.

## Baseline findings

### Shared system and shell

- `styles.css` has accumulated layers of broad selectors and appended overrides. Legacy dark palette rules, light-workspace overrides, role-specific rules, and responsive patches coexist; later selectors often compensate for earlier ones.
- Visual tokens exist as `--cp-*`, but the primary palette is effectively fixed in several overrides. Tenant branding fields are persisted in the settings UI, yet the application shell does not consistently derive its accent from an accessible runtime tenant token.
- Owner/coach and client workspaces share a shell, but navigation is long and dense. Desktop navigation consumes substantial width; mobile uses a drawer, while account/tenant context and notification access are not consistently surfaced in a compact topbar.
- Reusable Button is present, but the majority of the application still uses native controls and CSS classes. Card, badge, table, form-field, avatar, empty/error/loading patterns are not consistently centralized.
- Arabic RTL is supported and Cairo is loaded, but English/Arabic labels are mixed in some screens. Several legacy strings and encoding artifacts are visible in the reference captures and source.

### Authentication and public pages

- The login reference has a strong two-column concept but its form and call-to-action hierarchy is cramped and small relative to the available card area; switching between login and registration/help actions adds visual density.
- Registration, invitation, reset/confirmation and public-tenant pages share a minimal card rather than a consistent public/auth layout. Loading, error and pending-approval states need a clearer visual hierarchy.
- Forced password change and submit/loading/error states need the same spacing and field treatment as sign-in.

### TenantOwner and CoachStaff

- The coach/owner dashboard reference has a prominent hero, yet generic English hero copy competes with Arabic page context. KPIs and alert/action cards feel like separate visual systems; quick access, current workload and recent activity are not prioritized consistently.
- Client, team, program, exercise, meal-plan, check-in and billing operations are implemented as many independently styled panels and tables. Dense lists and long forms need stronger section hierarchy, filter/action grouping, status pills, and responsive table treatment.
- AI drafting/review pages need a consistent draft state, provenance badge, and clear review-action hierarchy without changing their existing business flow.
- Settings, analytics, audit, notifications, payments, subscriptions, and chat contain useful states, but card spacing, error/empty/loading presentation, and mobile layouts vary between pages.

### Client

- The reference client workspace reuses owner/coach navigation density despite a smaller, task-focused client journey. The current program, today's workout, progress and next action should be easier to scan.
- Meal plans, check-ins, workout logs, billing, messages and notifications need more consistent summary/detail hierarchy and mobile data presentation.
- Payment-return and account/onboarding states need a clear status-first presentation while preserving existing polling and API logic.

### SuperAdmin

- The dashboard reference uses a separate visual treatment from the app workspace. Its data tables, metrics, tenant-management controls and panels need consistent tokens and status semantics while retaining the role-specific navigation and responsive drawer.
- Audit and webhook data need compact but readable tabular hierarchy and clear loading/empty/error/retry treatment.

### Cross-cutting UX and accessibility

- CSS does not consistently apply spacing, radius, shadow, typography, or semantic colors from one scale. Some controls/buttons and cards use local overrides or magic values.
- Mobile behavior exists, but tables and complex multi-column forms need consistent overflow/reflow rules from 360px upward.
- Focus visibility, disabled/loading feedback, semantic state badges, reduced-motion handling, and accessible names should be standardized without changing routes, API calls, or existing test selectors.
- Tenant color customization must remain a runtime override and should not make text/background contrast unsafe; base readable light surfaces should remain stable if branding data is invalid or absent.

## Design direction

Establish a restrained light B2B design system with neutral surfaces, a deep teal primary, accessible semantic state colors, a single spacing/radius/shadow scale, and Cairo/system typography. Centralize tokens and make the primary accent overrideable from validated tenant branding at runtime. Preserve existing route and data behavior; improve layout and styling through shared shell/card/form/table/state patterns, then verify Arabic/English and RTL/LTR at mobile, tablet, and desktop widths.

## Verification plan

- Run `npm run typecheck`, `npm run lint`, `npm run build`, and `npm run test:e2e` after each major UI area.
- Use the existing Playwright setup and authenticated API-mocked flows to capture representative login, owner/coach, client, and SuperAdmin screens at 360px, 768px, and 1440px when test/runtime prerequisites permit.
- Keep all work and generated audit/screenshot artifacts under `CoachPulse.Frontend` as requested.
