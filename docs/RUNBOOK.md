# Operations runbook

## Start and readiness

Development Compose binds `PROXY_HTTP_PORT` (default `8080`) on localhost. If occupied, set a free port such as `PROXY_HTTP_PORT=8088` in `.env`. Production uses Caddy automatic HTTPS; set `SITE_DOMAIN`, `ACME_EMAIL`, and `CORS_ALLOWED_ORIGIN` before startup.

```powershell
docker compose up --build -d
docker compose ps
Invoke-RestMethod http://localhost:8080/healthz
Invoke-RestMethod http://localhost:8080/readyz
# Replace 8080 with the configured PROXY_HTTP_PORT when needed.
```

`/healthz` confirms API liveness. `/readyz` requires reachable PostgreSQL, completed startup initialization, registered recurring jobs, and Python AI readiness when configured as required.

## Migrations and seed data

Compose runs the one-shot `migrator` before starting the API. Check `docker compose ps -a migrator` and its logs if API readiness reports a migration failure. Local development uses `dotnet ef database update --project CoachPulse.Infrastructure --startup-project CoachPulse.Api`.

The environment creates the initial SuperAdmin. The shared exercise catalog is a separate idempotent task: `docker compose --profile tools run --rm exercise-seed`. Development demo data is disabled in production.

## Logs and service status

```powershell
docker compose ps
docker compose logs --tail 100 api
docker compose logs --tail 100 coachpulse-ai
docker compose logs --tail 100 migrator
```

Review logs for startup errors and trace IDs. Do not enable request-body logging for authentication, health, payment, webhook, or file endpoints.

## Hangfire and recurring jobs

The dashboard is available at `/hangfire` only to a SuperAdmin. Jobs are registered after the database is migrated. Confirm the server and recurring-job records after startup. Jobs are expected to scope tenant processing explicitly and continue when one tenant fails; investigate repeated failures through application logs before retrying manually.

## Paymob callback diagnostics

The Paymob dashboard notification endpoint is `https://<public-host>/api/webhooks/paymob`; the return page is `https://<public-host>/my/payment/return`. For local work expose API port 5006 through ngrok or Cloudflare Tunnel and use its HTTPS hostname for the webhook. A return redirect is not payment confirmation; verify the webhook event and authenticated payment status. Details: [`PAYMOB.md`](PAYMOB.md).

## Backups

1. Stop or quiesce writes and create an encrypted PostgreSQL dump.
2. Snapshot the `uploads` and `data_protection` volumes from the same recovery point.
3. Store the artifacts in access-controlled off-host storage and record the image/config version.
4. Restore all artifacts into an isolated environment and verify migrations, uploaded file reads, JWT operation, idempotency replay, and `/readyz` before production cutover.

## Restore / rollback

Never delete the database volume to resolve a failed migration. Restore a tested backup into a fresh volume or follow a reviewed forward migration. Preserve payment and audit history. If the API deploy is rolled back, ensure its code supports the current schema and retain the matching Data Protection key volume.

## Common incidents

- **Readiness fails:** inspect `postgres`, `migrator`, API startup state, and AI `/ready` (which requires the internal API key).
- **API redirects unexpectedly:** check Caddy `X-Forwarded-Proto` propagation and API forwarded-header configuration.
- **Hub reconnect loop:** check `/hubs/*` reverse proxy WebSocket support, bearer token freshness, and tenant/partner membership.
- **Uploads unavailable after restart:** confirm the named `uploads` volume is mounted and writable by the API's non-root UID.
- **Idempotency replay cannot decrypt:** restore the matching Data Protection keys; do not remove stored payment records as a workaround.
# Workspace request approval

Public workspace registration creates a `PendingApproval` workspace and owner record, returns HTTP 202, and does not issue a login session. A SuperAdmin opens the platform dashboard, reviews **Workspace requests**, enters an approval reason, and selects **Approve**. The workspace becomes active and its owner can then sign in. Pending workspaces are not returned by the public workspace lookup endpoints.

## Development demo data

The demo seeder is restricted to the Development environment and refuses to run in Production. Set `DemoSeed__Enabled=true` and set `DemoSeed__Password` using .NET user-secrets or a local environment variable before starting the API. It adds representative clients, staff, plans, programs/exercises, meal plans/meals, a pending subscription and payment, check-ins, a workout log, chat messages, and notifications to `coachpulse-demo`. Re-running it fills missing records without overwriting existing ones. It does not seed fake data into a Production deployment.
