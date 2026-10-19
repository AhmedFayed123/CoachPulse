# Frontend-unlinked API operations

This inventory distinguishes endpoints that intentionally have no browser caller from deprecated compatibility operations. Generated counts and per-operation call-site matches are maintained in [`API_INTEGRATION_AUDIT.md`](API_INTEGRATION_AUDIT.md).

| Operation | Why there is no direct frontend call |
| --- | --- |
| `POST /api/webhooks/paymob` | Paymob calls this provider callback; a browser must never invoke it. |
| Deprecated compatibility routes | Kept for older clients. The frontend uses the canonical route; see [`FRONTEND_ROUTE_INVENTORY.md`](FRONTEND_ROUTE_INVENTORY.md) for the exact list. |

The frontend includes role-appropriate screens/actions for trainer subscriptions, invoices, meal plans, workout logs and adherence, AI drafts, notifications, both SignalR hubs, and SuperAdmin operations. A matching call site does not establish that every route or failure case was exercised.

## Runtime verification

The Compose stack and a multi-user Playwright journey passed on 2026-10-18. Verified paths include registration and token refresh, invitation/onboarding, program assignment, InstaPay confirmation, Check-in feedback, cross-browser SignalR chat, and notification persistence. Payment-provider delivery and route-specific edge cases outside that journey still need dedicated verification; see [`API_INTEGRATION_AUDIT.md`](API_INTEGRATION_AUDIT.md).