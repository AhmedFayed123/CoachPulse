# CoachPulse

## Run with Docker Compose

1. Copy `.env.example` to `.env` and replace every `replace-*` value with a unique secret. Generate `JWT_KEY` and `INTERNAL_API_KEY` from a cryptographically secure random source. Use live Paymob credentials only when Paymob is enabled. The Python service has no published host port and is attached to the private `ai-private` network.
2. Set `CORS_ALLOWED_ORIGIN` to the exact HTTPS origin of the deployed client.
3. Start PostgreSQL and apply migrations from a development shell with the same connection string:

   ```powershell
   docker compose up -d postgres
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>" --project CoachPulse.Api
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   dotnet ef database update --project CoachPulse.Infrastructure --startup-project CoachPulse.Api
   ```

4. Start the application:

   ```powershell
   docker compose up --build -d
   ```

The first API startup creates a SuperAdmin only when `SUPERADMIN_EMAIL` and `SUPERADMIN_PASSWORD` are present. The password must be unique, at least 16 characters, and include uppercase, lowercase, numeric, and symbol characters. The seeder does not create a built-in account or password. The values are not written into the database except for the password hash.

## Local development

The API requires `Jwt:Key` (at least 32 UTF-8 bytes), `ConnectionStrings:DefaultConnection`, and JWT issuer/audience. Store local values using .NET user secrets; do not put them in tracked `appsettings*.json` files:

```powershell
dotnet user-secrets set "Jwt:Key" "<random local development key>" --project CoachPulse.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<local PostgreSQL connection string>" --project CoachPulse.Api
dotnet user-secrets set "SUPERADMIN_EMAIL" "<local admin email>" --project CoachPulse.Api
dotnet user-secrets set "SUPERADMIN_PASSWORD" "<unique local password>" --project CoachPulse.Api
```

`appsettings.Production.json` contains no credential values. Production uses environment variables or a secret store. Access tokens are configured for 15 minutes by default and must remain between 5 and 30 minutes. Swagger is enabled only in Development.

## Tests

```powershell
dotnet build CoachPulse.sln
dotnet test CoachPulse.sln
pytest CoachPulse.AI/tests
```

The churn model is trained from documented synthetic data during the Python image build. It is not a customer-trained model.
