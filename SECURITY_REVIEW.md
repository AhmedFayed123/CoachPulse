# Security review

Review run: 2026-10-18. A passing integration check validates only the exercised path and is not a penetration test.

## Results

| Area | Result | Evidence / limitation |
|---|---|---|
| .NET dependency vulnerability scan | Pass | `dotnet list package --vulnerable --include-transitive`: no vulnerable packages reported for all five projects. |
| npm vulnerability scan | Pass after remediation | `npm audit` found seven high issues under the unused `shadcn` CLI dependency. The CLI was not imported or used by build scripts; it was removed and both npm and pnpm lockfiles refreshed. A repeat install audit reported zero vulnerabilities. |
| Python vulnerability scan | Pass | `pip-audit -r requirements.txt`: no known vulnerabilities found. |
| Git history secret scan | No confirmed secret from available checks | `.env` and `appsettings.Production.json` do not appear in tracked history. Pattern matches were credential configuration identifiers in code, tests, and examples; matching values were deliberately suppressed from output. Rotate any credentials ever committed outside these patterns and review provider dashboards. |
| Production configuration | Partial | API startup rejects weak/missing JWT, internal API key and SuperAdmin values, and wildcard/invalid CORS origins. Compose requires the corresponding values. Production-specific appsettings file is absent; environment variables are the configured source. |
| CORS / forwarded headers / transport | Pass by inspection | Explicit origins and credentials are configured; production wildcard origins are rejected. Forwarded headers trust configured proxy addresses only. Production enables HSTS and HTTPS redirection. |
| Error responses / headers | Pass by inspection | Global exception handler returns generic ProblemDetails with traceId; status-code errors also use ProblemDetails. X-Content-Type-Options, X-Frame-Options, and Referrer-Policy are set. |
| Swagger | Pass by inspection | Swagger middleware is only enabled in Development. |
| Rate limiting | Partial | Global IP limiter (300/min), login (5/min), register (10/hour), forgot-password (3/hour), upload (30/min), and webhook-specific limiter are configured. No complete section-0 route-by-route rate-limit test has been run. |
| Authorization policy coverage | Pass in unit suite | `PlatformRoutePolicyTests` is included in the passing .NET suite (114/114). This checks explicit route authorization metadata; it does not prove every domain authorization outcome. |
| Tenant isolation | Partial | Tenant-aware filters and per-feature isolation tests exist; the requested exhaustive cross-tenant matrix over every ID-based GET/PUT/DELETE has not been run against PostgreSQL. |
| Logging | Partial | Paymob credentials and hosted checkout URL are not logged by the Paymob client; tested previously. A comprehensive automated log-redaction scan for passwords, access tokens, health data, and raw payment payloads remains outstanding. |
| SignalR | Partial | Both hubs require authorization, receive payload is capped at 64 KiB, and hub membership checks are implemented. Production origin behavior was not exercised through the proxy. |
| File delivery | Pass by inspection | Upload storage is outside `wwwroot`; downloads set attachment disposition and `X-Content-Type-Options: nosniff`. |
| Docker image hardening | Pass by inspection | API, AI and web images use non-root runtime users; AI has no published host port and sits on the private network. |
| Docker runtime / readiness | Pass for local stack | `docker compose up --build -d` completed. `/healthz` and `/readyz` both returned 200; readiness reported DB, startup initialization, and Python AI ready. `docker compose ps` showed no published host port for the AI service. |
| .NET build and tests | Pass | Latest `dotnet build --no-restore`: 0 errors (NU1900 vulnerability-feed warnings occurred before the successful feed scan). `dotnet test --no-restore`: 114 passed, 0 failed. |
| Python tests | Pass | Python 3.12 via `uv`; `pytest -q`: 6 passed, 1 third-party deprecation warning. |
| Frontend checks | Pass | `npm run typecheck`, `npm run lint`, `npm run build`, and route audit passed after removing the vulnerable unused CLI. The API inventory contains 218 operations. |
| Browser E2E | Pass for covered journey | Playwright passed 2/2 against the live Compose stack, including tenant registration/refresh rotation, invitation acceptance, onboarding, program assignment, InstaPay confirmation, Check-in feedback, cross-browser SignalR chat, and notification persistence. Remaining API routes still need dedicated security tests. |

## Release actions still required

1. Repeat all three dependency scans in CI on every dependency update; the scans passed for the current lockfiles.
2. Add automated network-level assertions that the AI service is unreachable from host/public networks, beyond checking that Compose publishes no AI port.
3. Extend PostgreSQL-backed E2E coverage to every route class, especially cross-tenant known-ID reads and mutations.
4. Complete route-by-route rate-limit coverage, exhaustive cross-tenant ID access tests, and automated sensitive-log assertions.
5. Ensure any real credentials that may have been exposed historically are rotated with Paymob, JWT, internal AI and LLM providers; no secret values are reproduced in this report.
