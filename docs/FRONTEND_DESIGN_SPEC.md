# CoachPulse frontend design system

This document defines the shared visual contract for every authenticated workspace and public/authentication route. It is the target spec for the route checklist in [`FRONTEND_CONSISTENCY_AUDIT.md`](./FRONTEND_CONSISTENCY_AUDIT.md). Keep role-based branding as CSS custom-property values; component geometry, typography, spacing, and control states remain shared. Do not change API, authentication, route, or workflow behavior as part of visual migration.

## Foundations

- Use the existing Inter/Arabic Cairo font stacks where available. Apply logical properties (`margin-inline`, `padding-inline`, `inset-inline`) so the same components work in LTR and RTL.
- Base body text is 16px with 1.5 line-height. Secondary text is 14px; captions can use 12px only for metadata. Do not set body copy below 14px. Inputs use at least 16px on mobile.
- Use spacing tokens in 4px increments: 4, 8, 12, 16, 20, 24, 32, 40, 48. Use semantic tokens instead of literal spacing values in page CSS.
- Use shared surface, border, ink, muted, primary, focus, success, warning, danger, and info tokens. Tenant colors may set primary brand variables, but status colors and neutral text must remain semantic and readable.
- Radii: control 8px, card 12px, dialog 16px, pill 999px. Shadows: one restrained card elevation and one popover/modal elevation. Prefer border and surface contrast over layered shadows.
- Content widths: dashboard 1200px, table/list 1280px, detail 1040px, focused form 720px, auth form 520px. At narrower widths use full available width with consistent 16px gutters.

## Typography roles

Use semantic classes/tokens, not tag-specific font declarations scattered across pages. Line heights below are unitless.

| Role | Size / line height / weight |
|---|---|
| Page title | 28px / 1.2 / 700; 24px on narrow screens |
| Section title | 20px / 1.3 / 650 |
| Card title | 16px / 1.4 / 650 |
| Body | 16px / 1.5 / 400 |
| Secondary text | 14px / 1.5 / 400 |
| Caption / metadata | 12px / 1.4 / 500 |
| Label | 14px / 1.4 / 600 |
| Table header | 12px / 1.4 / 650, uppercase only when appropriate to language |
| Table cell | 14px / 1.45 / 400 |
| Numeric / KPI | 28px / 1.15 / 700 with tabular numerals; compact KPI 22px |

## Buttons

- Variants: primary, secondary, ghost/tertiary, danger, and link. A view area has one primary action; related secondary actions use secondary or ghost.
- Sizes: sm 36px, md 44px, lg 48px minimum height. On touch devices every actionable button target is at least 44px, including icon-only buttons and segmented/button-group items.
- Use shared horizontal padding (12/16/20px by size), 8px radius, 14px medium-weight label, and a 1px border where applicable. Icon-only buttons are square and have an accessible name.
- Provide hover, active, focus-visible, disabled, and loading states. Loading preserves the original width and announces progress; disabled state does not rely on opacity alone.
- Button groups share borders/radii and do not collapse below usable touch targets on mobile.

## Form controls

- Shared medium control height 44px; small 36px and large 48px. On touch screens, fields remain at least 44px high and use 16px text to prevent browser zoom.
- Inputs, selects, search/combobox triggers, date/time/number fields, password fields, and file controls share 12px horizontal padding, 8px radius, semantic border/surface/text colors, and a 2px visible focus ring.
- Textareas have the same border and padding, minimum 96px height, and vertical resize unless the content is fixed-height by its workflow.
- Labels appear above fields; helper and error text sit immediately below. Errors use text and an icon/status association, not color alone. Disabled and read-only states remain distinguishable.
- Checkbox, radio, and switch targets have 44px surrounding hit areas on touch. Preserve native keyboard behavior and labels.
- Password show/hide controls must not change field width or move surrounding content. File upload uses a shared drop/select surface and displays filename/progress/error within it.
- Keep existing inline-validation timing and submission behavior; this spec only unifies appearance.

## Cards, containers, and page structure

- Page frame: one `PageHeader` (title, optional description, optional actions), followed by sections with 24px vertical rhythm. Use consistent 24px page gutters on desktop and 16px on mobile.
- Card variants: default, interactive, stat/KPI, list item, empty state, and danger zone. Default cards use 20px padding desktop / 16px mobile, 12px radius, 1px semantic border, and shared surface. Interactive cards add hover/focus affordance without implying that noninteractive cards are links.
- Dashboard grids use responsive `minmax(0,1fr)` columns; table pages have a wrapping filters bar and full-width table container; form pages use a focused max-width; detail pages use a 2-column layout that collapses at tablet widths.
- Maintain stable card sizes within the same component family. Long content wraps instead of forcing page overflow.

## Tables and lists

- Table header: 44px minimum height, muted surface, semantic text role, sticky only inside a bounded scrolling region. Comfortable rows are 56px; compact rows are 44px.
- Row hover/selection has a clear surface treatment and keyboard focus. Do not communicate status with color alone.
- Place filters, search, and bulk actions in a consistent toolbar. Pagination has accessible labels and 44px touch targets.
- On mobile, use a labeled card-list when the columns cannot be understood in a horizontal viewport; otherwise contain horizontal scrolling within the table wrapper so the document itself does not overflow.

## Shared interaction patterns

- Badges/status pills use semantic status tokens and consistent 12px type, 20px line-height, 8px horizontal padding, and pill radius.
- Tabs use a 44px minimum hit area, visible selected indicator, keyboard navigation, and `aria-selected`/panel relationships. Do not convert existing non-tab navigation into tabs.
- Dialogs: small 420px, medium 600px, large 800px max width; viewport-constrained with internal scrolling, 16px radius, labeled title, and footer actions aligned consistently. Drawers use the same surface and focus treatment, with a full scrim on mobile.
- Toasts are brief, semantic, and nonblocking; tooltips are supplementary and never the sole source of a label. Dropdowns/search popovers align to their trigger and stay within the viewport.
- Breadcrumbs, section headers, and dividers use the same muted text, spacing, and border tokens. Avatars use shared sizes 32/40/48px with initials fallback and descriptive accessible names where needed.
- Loading uses a consistent skeleton/spinner pattern with reserved layout space. Empty states include a concise explanation and only the relevant next action. Errors explain the issue and expose retry only where retry is supported. Permission-denied states use the established auth guard behavior.
- Chat bubbles distinguish sender and delivery state using surface, alignment, and text/icon cues. The composer is a 44px-minimum control with an accessible send action; attachments and retry states remain inside the transcript flow.
- Check-in, workout, and program-builder forms use the common form grid and field validation; repeatable sections are visually grouped as cards with consistent add/remove controls.

## Responsive and RTL behavior

- Breakpoints: 640px (small layout), 768px (tablet), 1024px (desktop shell). Avoid fixed page widths. At 360px, page content must fit without horizontal document overflow.
- Navigation drawers, filters, table wrappers, card grids, forms, dialogs, and action groups must have intentional narrow-screen behavior. Keep primary action visible and full-width only when the existing workflow/layout calls for it.
- Test 360px, 768px, and 1280px widths. Test Arabic on auth, dashboard, a data-entry form, a table/list, and chat. Use logical CSS properties and verify icons/order where direction has meaning.

## Exceptions and enforcement intent

- Dynamic tenant-brand CSS variables are an allowed inline style source only when passed through the existing validated theme helper. Chart geometry may use dynamic sizing only when its library requires it and should be documented at the callsite.
- Raw native controls are allowed only inside the primitive implementation (and semantic exceptions documented next to the rule). Pages must consume the shared primitives.
- Hardcoded colors and pixel typography/spacing belong in token definitions only. ESLint should prohibit raw controls and inline `style` outside the primitive folder; a focused source checker should reject hex colors and px font sizes outside token/theme files.
