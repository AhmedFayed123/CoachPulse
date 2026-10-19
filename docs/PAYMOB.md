# Paymob Unified Checkout

CoachPulse creates payment intentions server-side and redirects the client to Paymob's hosted Unified Checkout. The browser receives `checkoutUrl`; it never receives the Paymob API secret. The intention `client_secret` is embedded in the hosted checkout URL as required by Paymob's Unified Checkout flow, so treat the URL as short-lived sensitive data and never log or persist it in plaintext. The idempotency response snapshot is protected with ASP.NET Core Data Protection.

## Configuration

Configure these values in a secret store, .NET user-secrets for local development, or environment variables. Never commit real values:

| Setting | Purpose |
| --- | --- |
| `Paymob__PublicKey` | Public key included in the hosted checkout URL |
| `Paymob__SecretKey` | Server-side Authorization token for creating intentions |
| `Paymob__IntegrationId` | Paymob payment integration identifier |
| `Paymob__BaseUrl` | `https://accept.paymob.com/` or the configured Paymob regional host |
| `Paymob__ReturnUrl` | Public frontend URL, ending in `/my/payment/return` |
| `Paymob__NotificationUrl` | Public API URL, ending in `/api/webhooks/paymob` |
| `Paymob__HmacSecret` | Webhook signature validation secret from the Paymob dashboard |

For an environment file, the equivalent names are `PAYMOB_PUBLIC_KEY`, `PAYMOB_SECRET_KEY`, `PAYMOB_INTEGRATION_ID`, `PAYMOB_BASE_URL`, `PAYMOB_RETURN_URL`, `PAYMOB_NOTIFICATION_URL`, and `PAYMOB_HMAC_SECRET`.

## Paymob dashboard setup

1. Use Sandbox credentials and a Sandbox integration for local testing. Do not mix Sandbox and live public keys, secrets, or integration IDs.
2. Set the Return/Redirection URL to the public frontend origin plus `/my/payment/return`. Set the notification/webhook endpoint to the public API origin plus `/api/webhooks/paymob`.
3. Set the same URLs in `Paymob__ReturnUrl` and `Paymob__NotificationUrl`. The API includes them when creating the intention.
4. Configure the webhook HMAC secret as `Paymob__HmacSecret`, then send a test payment and confirm that the webhook is accepted and its event recorded.

The return page polls the authenticated `GET /api/payments/{id}/status` operation every three seconds for at most two minutes. Query-string values on the redirect are not proof of payment. The verified Paymob webhook is the source of truth for confirmation and subscription activation.

## Local callback tunnel

The frontend can remain at `http://localhost:5173` and the API at `http://localhost:5006`; Paymob cannot call a private localhost address. Start a tunnel to the API port, for example:

```powershell
ngrok http 5006
```

or:

```powershell
cloudflared tunnel --url http://localhost:5006
```

Use the generated public HTTPS API hostname for `/api/webhooks/paymob`. Use a separately exposed frontend hostname for `/my/payment/return` if Paymob requires the return page to be public; otherwise the browser can return to localhost in the same browser session. Set these URLs in the Paymob dashboard and local user-secrets. Tunnel URLs can change between runs, so update both locations when they change. Never add tunnel credentials or API keys to Git.
