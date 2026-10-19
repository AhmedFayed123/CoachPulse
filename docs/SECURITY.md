# Security notes

## Secrets and rotation

Never commit `.env`, provider credentials, production JWT keys, internal API keys, or real SMTP passwords. Supply secrets through a deployment secret store or environment injection. `.env.example` contains empty credential values and non-secret placeholders only.

Rotate a compromised secret immediately in its issuing system, deploy the replacement, then revoke the old value. Rotating `JWT_KEY` invalidates access tokens. Rotating `INTERNAL_API_KEY` requires updating both the API and AI service together. Rotating Paymob credentials also requires updating the Paymob dashboard and checking webhook HMAC configuration. Do not print resolved Compose configuration or process environments into build logs.

The Paymob intention `client_secret` is necessarily present in the hosted checkout URL. The API does not return it as a separate field, log it, or store an idempotency response snapshot in plaintext. Treat a checkout URL as short-lived sensitive data and keep it out of telemetry.

## Runtime boundaries

- Caddy is the only production service publishing internet-facing ports. PostgreSQL and AI are attached to an internal network and have no published production ports.
- API/AI/nginx processes run as non-root users. File uploads and Data Protection keys use persistent private volumes; upload content is outside the public web root.
- Production JWT keys and internal API keys are validated at startup. Production CORS rejects wildcard origins. HTTPS/HSTS and security headers are enabled at the proxy/API boundary.
- Public tenant and invitation endpoints return limited public data. Tenant/resource ownership and roles are checked by API policies and query filters.
- Hangfire dashboard authorization is enforced by the API's SuperAdmin filter. Caddy only routes it.

## Operational controls

Keep backups encrypted. Database backups must be coordinated with the uploads and Data Protection key volumes. Restrict shell access to Docker hosts because container inspection can expose environment-injected secrets. Use a managed secret store for production deployments and audit access to it.

## Review limits

See `SECURITY_REVIEW.md` for the latest dependency, route, tenant isolation, logging, SignalR, file, and Docker checks. A passing build or static route inventory does not establish a successful security penetration test. Browser and live database checks must be run against the deployed stack before release.
