# CoachPulse frontend consistency audit

Route and control audit for `CoachPulse.Frontend/src`. The original baseline counts were collected before migration with source regex counts; the current counts below reflect the completed shared primitive migration. The checklist records responsive browser coverage separately from behavior-state coverage.

## Route and view checklist

The route components are currently concentrated in `src/App.tsx`. The shared `/app` routes serve both TenantOwner and CoachStaff, with guards/conditional redirects for role-specific destinations. SuperAdmin uses one section-driven component. All entries start **not migrated**.

| Role / route | Page or view | Nested overlays, panels, and states to migrate | Status |
|---|---|---|---|
| Auth `/login` | Sign in / club selection | searchable club picker popover; loading and retry states; alternate account links | Shared primitives; responsive route sweep passed |
| Auth `/register/tenant` | Request a club | submitted/pending-approval confirmation; validation and error states | Shared primitives; responsive route sweep passed |
| Auth `/register/client` | Client registration | invitation token form; validation and error states | Shared primitives; responsive route sweep passed |
| Auth `/accept-staff-invite` | Accept staff invitation | token/password form; validation and error states | Shared primitives; responsive route sweep passed |
| Auth `/forgot-password` | Request password reset | form success/error states | Shared primitives; responsive route sweep passed |
| Auth `/reset-password` | Reset password | token/password form; success/error states | Shared primitives; responsive route sweep passed |
| Auth `/confirm-email` | Confirm email | token form; success/error states | Shared primitives; responsive route sweep passed |
| Auth `/resend-confirmation` | Resend confirmation | form success/error states | Shared primitives; responsive route sweep passed |
| Auth `/first-login-password` | Required password change | current/new/confirm password form; completion state | Shared primitives; responsive route sweep passed |
| Auth `/tenant/:slug` | Public tenant entry | loading/error/tenant state | Shared primitives; responsive route sweep passed |
| Auth `/invitation/:token` | Public invitation entry | loading/error/invitation state | Shared primitives; responsive route sweep passed |
| Auth `/` and unknown path | Redirect/fallback | redirect to authenticated workspace or sign-in | Shared primitives; responsive route sweep passed |
| Auth guarded paths | Forbidden/unauthenticated access | role guard and redirect states (no dedicated forbidden route component found) | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/dashboard` | Workspace dashboard | KPI/actions/alerts; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/clients` | Client list | search/filter/table or cards; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/coaches` | Coach team | coach selection/detail; access/profile/assignment forms; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/team` | Coach team alias | same coach team view; role-gated | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/checkins-review` | Check-in review | per-entry feedback textarea/actions; pagination; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/programs` | Programs | create/edit/assign/archive/duplicate actions; builder/detail panels; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/exercises` | Exercise library | filters, exercise detail/video, create/edit states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/meal-plans` | Meal plans | create and manage plan actions; detail panel | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/plans` | Tenant plans | role-gated plan management; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/conversations` | Conversations | conversation list and chat panel; mobile conversation switch; composer/attachment states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/payments` | Payments and payment insights | receipt expand/download; filters/tables; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/subscriptions` | Subscription management | lifecycle actions; query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/notifications` | Notification center | unread filter, read/delete actions, pagination | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/ai` | AI workspace | prompt/forms, generated content, loading/error states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/ai-review` | AI review | draft review/approval actions and content panels | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/settings` | Tenant settings | branding/payment/configuration forms; color inputs | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/analytics` | Tenant analytics | charts, filters, metrics and query states | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/audit` | Tenant audit log | filters/table/pagination | Shared primitives; responsive route sweep passed |
| TenantOwner `/app/account` | Account/profile | avatar upload/profile forms | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/dashboard` | Workspace dashboard | KPI/actions/alerts; query states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/clients` | Assigned clients | client list/detail affordances; query states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/coaches` | Coach team | shared team view where available | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/team` | Redirect to dashboard | role-gated redirect | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/checkins-review` | Check-in review | feedback forms; pagination; query states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/programs` | Programs | program builder/assignment actions and states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/exercises` | Exercise library | filters, detail/video, forms | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/meal-plans` | Meal plans | create/manage/assign forms | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/plans` | Redirect to dashboard | role-gated redirect | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/conversations` | Conversations | list/chat/composer/mobile switch | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/payments` | Payments | payment view and query states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/subscriptions` | Subscription management | actions and query states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/notifications` | Notification center | filter/actions/pagination | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/ai` | AI workspace | prompt/generated content states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/ai-review` | AI review | review actions and draft panels | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/settings` | Tenant settings | role permissions and configuration forms | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/analytics` | Tenant analytics | charts and query states | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/audit` | Tenant audit log | filters/table/pagination | Shared primitives; responsive route sweep passed |
| CoachStaff `/app/account` | Account/profile | avatar upload/profile forms | Shared primitives; responsive route sweep passed |
| Client `/my/dashboard` | Client dashboard | KPI/progress/actions; query states | Shared primitives; responsive route sweep passed |
| Client `/my/onboarding` | Onboarding | onboarding steps/forms (confirm exact step count during migration) | Shared primitives; responsive route sweep passed |
| Client `/my/profile` | Client profile | profile/avatar forms | Shared primitives; responsive route sweep passed |
| Client `/my/progress` | Progress | metrics/charts and query states | Shared primitives; responsive route sweep passed |
| Client `/my/program` | Assigned program | workout/program content and query states | Shared primitives; responsive route sweep passed |
| Client `/my/workouts` | Workout logs | log form/list states | Shared primitives; responsive route sweep passed |
| Client `/my/meal-plan` | Meal plan | meal logging form/actions | Shared primitives; responsive route sweep passed |
| Client `/my/checkins` | Check-ins | check-in management form/actions | Shared primitives; responsive route sweep passed |
| Client `/my/payments` | Subscription and payment details | lifecycle controls, payment details | Shared primitives; responsive route sweep passed |
| Client `/my/payment/return` | Payment return | success/failure/pending states | Shared primitives; responsive route sweep passed |
| Client `/my/messages` | Client messages | conversation/chat composer and states | Shared primitives; responsive route sweep passed |
| Client `/my/notifications` | Notification center | read/delete actions and pagination | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/dashboard` | Platform overview | tenant table; KPI, AI usage and webhook panels | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/tenants` | Workspaces | search/table and workspace action | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/usage` | AI usage | query table and states | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/webhooks` | Webhook events | query table and states | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/audit` | Platform audit log | query table and states | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/tenant-management` | Workspace management | tenant selection and detail/management panels | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/exercises` | Public exercise library | exercise management forms/list/video states | Shared primitives; responsive route sweep passed |
| SuperAdmin `/admin/:section` unknown | Section fallback | generic dashboard-like fallback; verify behavior while preserving route contract | Shared primitives; responsive route sweep passed |

### Overlay and interaction inventory

- Workspace navigation drawers for `/app/*`, `/my/*`, and `/admin/*`, each with scrim and collapse/toggle controls.
- Login club selector popover with search/listbox and empty/loading/error states.
- Conversations switch between the list and chat panel on narrow screens; this is an in-page panel, not a modal.
- Payment receipt disclosure expands inline; it is not a modal.
- No `<dialog>`, `role="dialog"`, `aria-modal`, or `role="tablist"`/`role="tab"` implementation was found in `src/App.tsx`. Tabs, wizards, and true modals must be verified against the actual conditional panels during each route migration; do not invent new flows.

## Leak counts by source file

The counts below are current raw intrinsic tags across JSX/TSX. Route pages no longer emit these elements directly; the shared primitive module owns one implementation of each. Inline style is limited to the validated tenant theme helper.

| File | `<button>` | `<input>` | `<select>` | `<textarea>` | `<table>` | dynamic `style` |
|---|---:|---:|---:|---:|---:|---:|
| `src/App.tsx` | 0 | 0 | 0 | 0 | 0 | 1 tenant theme |
| `src/i18n.tsx` | 0 | 0 | 0 | 0 | 0 | 0 |
| `src/components/ui/primitives.tsx` | 1 | 1 | 1 | 1 | 1 | 0 |
| `src/main.tsx` | 0 | 0 | 0 | 0 | 0 | 0 |
| **Pages** | **0** | **0** | **0** | **0** | **0** | **1 approved theme** |

Inline style is present in the alternative JSX expression form: the workspace shell passes `style={tenantThemeStyle(tenant.data)}`. This is one dynamic tenant-branding exception and should remain backed by CSS custom properties, not arbitrary inline layout styles.

### Hardcoded styles and stylesheet conflicts

Measured in the single `src/styles.css` file (global stylesheet; includes both legacy rules and subsequent redesign layers):

| Pattern | Occurrences |
|---|---:|
| Hex color literals | 1,605 |
| Pixel-valued declarations/values | 3,096 |
| `font-size` declarations containing px | 357 |
| `!important` | 85 |
| CSS declaration blocks (rough brace count; includes nested at-rules) | 2,113 |

The TSX files also contain several literal brand color defaults for the tenant color editor and contrast helper; these are data defaults, not layout declarations. `src/styles.css` is the only stylesheet under `src`; it combines dark legacy base rules, workspace/admin/auth-specific rules, and appended light redesign/token overrides. The CSS contains repeated selector families targeting the same shared controls and containers. The newer rules use selectors such as `.workspace-shell .page-sections :is(input,select,textarea)` and `.admin-shell .admin-table`, while earlier global and role-specific rules still style those elements. The 85 `!important` declarations confirm specificity escalation is part of the current cascade. A full selector-to-token replacement is required; changing only the final overrides would leave conflicting declarations active.

## Root causes

1. **Pages were never migrated to primitives.** The repository has a mostly monolithic `App.tsx` with 174 buttons and 159 form controls/tables rendered intrinsically. The prior redesign added shared shell/page styling without replacing page-level markup.
2. **One global stylesheet accumulated multiple design generations.** Legacy dark selectors remain alongside a light redesign layer at the end of the same file, producing cascade and specificity dependence rather than an authoritative component contract.
3. **Role-specific and local classes duplicate shared patterns.** Controls are restyled through generic tags, `.admin-*`, `.login-*`, `.workspace-*`, and numerous page classes, so pages that share semantics can render differently.
4. **Tokens are incomplete and inconsistently applied.** Tokens coexist with thousands of pixel values and hex colors, including font sizes and geometry. A token declaration by itself does not constrain page styles.
5. **The audit tooling has no UI consistency guard.** Existing scripts inventory routes/API calls, but no rule prevents new raw controls, inline presentation, or hardcoded typography values.

## Migration status

Page views now consume shared `Button`, `Input`, `Select`, `Textarea`, and `Table` primitives. ESLint blocks raw control tags outside the primitive module, and the source guard allows only the validated tenant theme style on the shell. The Playwright design sweep renders each listed auth and role route at 360, 768, and 1280px, checks document overflow, visible typography and controls, and browser errors, and saves screenshots. Four representative RTL routes are additionally rendered at 360px. Workflow loading/empty/error and permission behavior remain the existing application implementation; this visual sweep does not claim exhaustive state-transition coverage. Role guards, auth, API contracts, and business behavior were not changed for this migration.
