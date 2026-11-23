# SchoolWise

SchoolWise helps parents discover and compare schools in Greece. Phase 1 provides account authentication, school search/details, and administrator school management. **Reviews and review verification are intentionally not implemented.**

## Stack

- ASP.NET Core 10 LTS / C# / Clean Architecture / CQRS / MediatR
- PostgreSQL / Entity Framework Core / FluentValidation / AutoMapper
- JWT access tokens with rotating, HttpOnly-cookie refresh tokens
- Next.js 15 App Router / TypeScript / Tailwind CSS / shadcn/ui / React Query / Axios / Zod
- Docker Compose

## Run locally with Docker

Requirements: Docker Desktop with Compose.

Before starting, copy `.env.example` to `.env` and set unique values for `POSTGRES_PASSWORD`, `JWT_SECRET` (at least 32 bytes), and `ADMIN_PASSWORD`. Do not commit `.env`. PostgreSQL is published on loopback only.

```powershell
Copy-Item .env.example .env
# Edit .env and replace the empty secret values before continuing.
docker compose up --build
```

Open:

- Web app: <http://localhost:3000>
- API Swagger: <http://localhost:8080/swagger>
- API health: <http://localhost:8080/health>

Compose runs a one-shot `migrate` service before the API starts; API replicas do not apply migrations during startup. In the separate-service workflow, run `dotnet ef database update` before starting the API. The Development API seeds clearly labeled demo listings for Athens, kindergartens, elementary schools, and both public/private school types.

Development administrator:

- Email: `admin@schoolwise.local`
- Password: the `ADMIN_PASSWORD` value from your local `.env`

Use unique local credentials and never reuse development values in production.

## Run services separately

Requirements: .NET 10 SDK. Start PostgreSQL first (or use the Compose `postgres` service). Then:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=schoolwise;Username=schoolwise;Password=<your-local-db-password>" --project src/SchoolWise.Api
dotnet user-secrets set "Jwt:Secret" "<a-unique-random-secret-at-least-32-bytes>" --project src/SchoolWise.Api
dotnet user-secrets set "Seed:AdminPassword" "<a-unique-development-admin-password>" --project src/SchoolWise.Api
dotnet ef database update --project src/SchoolWise.Infrastructure --startup-project src/SchoolWise.Api
dotnet run --project src/SchoolWise.Api
```

In another terminal:

```powershell
Set-Location frontend
npm ci
npm run dev
```

Use Node.js 20.19 or later. Copy `frontend/.env.example` to `frontend/.env.local` if the API is hosted at a non-default URL. The default local API URL is `http://localhost:8080/api/v1`.

## Solution layout

```text
SchoolWise.sln
src/
  SchoolWise.Api/             HTTP controllers, composition root, middleware
  SchoolWise.Application/     CQRS requests/handlers, validation, DTOs, abstractions
  SchoolWise.Domain/          Entities, enums, domain invariants
  SchoolWise.Infrastructure/  EF Core, PostgreSQL, repositories, auth services, migrations
frontend/                     Next.js App Router application
tests/
  SchoolWise.Application.Tests/
  SchoolWise.Integration.Tests/
docs/
  Architecture.md
  DatabaseSchema.md
  ApiEndpoints.md
  DevelopmentRoadmap.md
  Phase2-*.md                 Phase 2 design only; not implemented
REVIEW.md
```

## Useful commands

```powershell
dotnet restore SchoolWise.sln
dotnet build SchoolWise.sln
dotnet test SchoolWise.sln
dotnet ef migrations list --project src/SchoolWise.Infrastructure --startup-project src/SchoolWise.Api
```

To create a migration after changing the model:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.15
dotnet ef migrations add MigrationName --project src/SchoolWise.Infrastructure --startup-project src/SchoolWise.Api --output-dir Persistence/Migrations
```

If `dotnet-ef` is not installed yet, use `dotnet tool install --global dotnet-ef --version 10.0.15`.

Frontend:

```powershell
Set-Location frontend
npm ci
npm run lint
npm run build
```

PostgreSQL-backed API integration tests use Testcontainers. With Docker available:

```powershell
$env:RUN_POSTGRES_INTEGRATION_TESTS = "1"
dotnet test tests/SchoolWise.Integration.Tests
```

## Configuration and security notes

- Production must provide `DATABASE_URL`, `JWT_SECRET` (at least 32 bytes), `JWT_ISSUER`, `JWT_AUDIENCE`, and `FRONTEND_ORIGINS` through a secret manager/environment.
- `Seed__AdminEmail` and `Seed__AdminPassword` can create the initial production Admin once; remove the password secret after the account is created. Demo schools are seeded only in Development.
- `AutoMapper:LicenseKey` can be provided by the deployment secret manager; do not commit a commercial license key.
- Refresh tokens are stored as SHA-256 hashes and rotated on use. The browser receives the refresh token only as an HttpOnly cookie.
- Production refresh cookies use `SameSite=None; Secure` for the cross-site Vercel/Render default domains. Refresh and revoke requests are restricted to the configured frontend origins.
- Public school endpoints expose approved listings only; `/api/v1/schools/admin` and all school write endpoints require the Admin role.
- Photo and logo URLs are externally hosted in this phase and must be absolute HTTP/HTTPS URLs.

## Deployment

Deployment and launch instructions are in [`docs/deployment/`](docs/deployment/DeploymentGuide.md), with a time-boxed [Go-Live runbook](GO_LIVE.md). The zero-cost Vercel Hobby + Render Free combination is suitable only for a non-commercial, synthetic-data demo, not a production startup service; see the production and risk notes before handling real parent data.
