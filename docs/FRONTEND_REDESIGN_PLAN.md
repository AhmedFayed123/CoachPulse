# CoachPulse Frontend Redesign Plan

## Scope and current state

The frontend is a React 19, TypeScript, Vite SPA. React Router holds role and auth routes; TanStack Query owns server state; the API client owns refresh/session behavior; SignalR owns chat and notification events. Those contracts and paths remain stable. No new runtime dependency is planned: the repository already has Base UI, CVA, Lucide, Recharts, and Tailwind v4, though existing screens mainly use global CSS and native controls.

The implementation is concentrated in `src/App.tsx` (roughly two thousand lines) with a long accumulated global `src/styles.css`. Existing styles include a legacy dark base, RTL-specific layout rules, and later light-theme overrides, making cascade behavior difficult to predict. A shared shell and Page header now cover the owner/coach/client and SuperAdmin workspaces, with bilingual direction handling, loading/error patterns, message and notification entry points, and mobile navigation. Playwright visual checks capture login and role dashboards at 360, 768, and 1440 pixels. The authenticated SignalR scenario lives in `e2e/platform-flow.spec.ts` and requires a tenant that has completed the API's approval and email-confirmation steps.

The existing tenant branding settings save primary and secondary colors. The workspace derives a runtime primary token from the tenant primary color, validates hex input, computes foreground contrast, and safely uses its base palette when the value is missing or malformed. English and Arabic are already supported with document-level `lang` and `dir`, Cairo for Arabic, and logical-property styles in the shared redesign layer. No full i18n or dark-mode package is needed.

## Findings and design direction

Current UX risks are duplicated screens and controls in the monolithic route module, many page-specific styles, native controls with inconsistent focus/disabled states, long owner/coach/client navigation, uneven empty/loading/error states, and desktop tables that can be difficult on small screens. Some older content has inconsistent Arabic/English copy and encoding artifacts. Chat currently exposes connection state but does not make sending failure/retry and message grouping sufficiently clear. AI review needs persistent draft/provenance status. Payment status must remain based only on the existing server polling contract.

Direction: calm, light, health-oriented B2B UI with deep teal and mint accents, cool neutral surfaces, restrained semantic status colors, consistent type, whitespace and elevation. Use one icon family (Lucide), a Cairo/Inter-system type pairing, and no generic decorative gradients outside the existing restrained dashboard hero. Preserve current API calls, auth/session and role guards, route paths, form names, test IDs, labels used by tests, and payment-return polling semantics.

## Design tokens

- Brand: `--color-primary` (deep teal), `--color-primary-dark`, `--color-primary-soft`, `--color-on-primary`; runtime tenant primary overrides are contrast-checked against white and dark text.
- Neutrals: `--color-ink`, `--color-muted`, `--color-line`, `--color-canvas`, `--color-surface`, `--color-soft`.
- Semantic: success, warning, danger, and info foreground/background pairs with readable contrast.
- Type: 12/14/16/20/24/32/40 px scale, strong page title and section heading hierarchy, Cairo for Arabic and Inter/system for Latin.
- Spacing: 4/8/12/16/20/24/32/40 px; radii 8/12/16/22 px; low-elevation card and popover shadows.
- Layer and motion: named drawer, topbar and popover z-indexes; 150/220 ms interactions, disabled under `prefers-reduced-motion`.

## Component inventory

| Existing | Refactor / reuse |
|---|---|
| Native buttons, one Base UI Button | Preserve Button API; shared primary, secondary, danger, icon, disabled and focus treatments |
| Native labels and inputs | Shared field, input, select, textarea, checkbox and switch styling with inline error/help slots |
| `.panel`, `.metric`, role-specific status classes | Token-backed Card, KPI tile and semantic Badge/StatusPill treatments |
| Repeated tables | Shared sticky header, readable density, horizontal overflow and mobile row handling |
| Existing empty/error/loading markup | Standard EmptyState, ErrorState with retry, and skeleton loading patterns |
| Role-specific headers/nav | Shared PageHeader/breadcrumb, topbar and role shell; client navigation stays task-focused |
| `details` user menu and mobile drawer | Retain native accessible behavior and focus rings; test keyboard and mobile interactions |
| Existing chart and forms | Keep Recharts and current API-backed form contracts; improve labels, empty states and semantic legends |
| Existing avatar, pagination, chat, wizard and timeline markup | Apply consistent tokens and responsive styles without introducing unused abstractions |

## Page-by-page migration order

1. Shared foundation: tokens, font and direction rules, focus/reduced-motion, buttons/forms/cards/tables/state patterns, tenant contrast fallback.
2. App shells: public/auth frame; owner/coach productivity shell; client mobile-first shell; SuperAdmin dense shell and drawer.
3. Auth routes: sign-in, tenant/client registration/request, invitation, pending approval, first-login password change, password recovery/confirmation.
4. Client: dashboard, current program/workout logging, check-in, meal plan, messages, subscription/payment/invoices and payment return, notifications, onboarding/profile.
5. Coach/Staff: client list/detail, programs/editor, check-in review, subscriptions where authorized, AI draft review and chat.
6. TenantOwner: dashboard/analytics, clients/invitations, team, plans/subscriptions/payments, settings/branding, audit and AI usage.
7. SuperAdmin: workspace requests with reason, tenant/usage lifecycle, exercise catalog, platform audit and webhook events.
8. Cross-route QA: real API E2E, screenshots, RTL/LTR, loading/empty/error/permission states, and all required commands.

## Risks and verification

- Keep session refresh and role redirects unchanged; route changes are out of scope.
- Never infer payment success from URL query parameters; preserve polling and only restyle states.
- Do not present a mock AI provider as real-model output; do not invent API fields or data.
- Keep tenant color scoped to the tenant shell and ensure foreground contrast; invalid branding must fall back safely.
- API-mocked screenshot tests validate rendering, not live API workflows. Run the authenticated SignalR journey against the repository's configured live API/Compose stack and distinguish any infrastructure blocker from a frontend regression.
- Tenant registration is `202 PendingApproval`, not an authenticated session. The frontend now renders that accepted state and does not store tokens. The existing full platform-flow test assumes immediate login and therefore cannot reach later payment/chat steps; its fixture needs the real approval and confirmation workflow before it can validate live SignalR.
- Dependency installation was slow but completed: Node 22.17.1, npm 10.8.3, public npm registry, no npm proxy configured. A stale Node/Vite process held the Lightning CSS native file, and writes to the global npm cache were denied; after stopping the holder and using a temporary workspace cache with longer fetch limits, `npm ci` installed 284 packages successfully. Registry tarballs were very slow and one request required retry. `package-lock.json` stayed unchanged. The Docker image build also completed after slow NuGet restores.
- `npm run typecheck`, `npm run lint`, and `npm run build` pass. The redesign screenshot/UI spec passes 8/8. Full `npm run test:e2e` reports 11 passed and 1 failed: the existing authenticated platform-flow reaches the old registration contract assumption; the API trace confirms `POST /auth/register-tenant` returns 202 and later refresh returns 401 because no session was issued.
- The current production entry is about 721.7 kB minified (196.2 kB gzip) and triggers Vite's 500 kB chunk warning. Route splitting is constrained by the existing monolithic `App.tsx`; extraction requires careful preservation of shared state and selectors.
- Dark mode is deferred unless it can use the same tokens with little risk. No backend gaps should be inferred from static API call-site matching.
