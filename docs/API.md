# API reference

The checked-in OpenAPI 3 document is [`swagger.json`](swagger.json). TypeScript declarations are generated from it at `CoachPulse.Frontend/src/api/generated.ts`.

For local interactive Swagger UI, run the API in the `Development` environment and open `/swagger`. Production Swagger UI is disabled. The API uses `/api` paths, JWT bearer auth except explicitly public callbacks, string-valued enums, camelCase JSON, and ProblemDetails errors with a trace ID.

The route inventories and integration classifications are:

- [`FRONTEND_ROUTE_INVENTORY.md`](FRONTEND_ROUTE_INVENTORY.md)
- [`API_INTEGRATION_AUDIT.md`](API_INTEGRATION_AUDIT.md)
- [`FRONTEND_UNLINKED_ENDPOINTS.md`](FRONTEND_UNLINKED_ENDPOINTS.md)

SignalR endpoints: `/hubs/chat` and `/hubs/notifications`. Browser clients pass the access token as `access_token` in the hub connection query string; the API accepts it only for those hub paths.
