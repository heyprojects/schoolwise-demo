# Local Setup

## Prerequisites

- .NET 10 SDK.
- Node.js 22 (the frontend requires Node `>=20.19`).
- Docker Desktop with Docker Compose for the containerized path.
- Git, if cloning from GitHub.

## Option A: Docker Compose

From the repository root in PowerShell:

```powershell
Copy-Item .env.example .env
```

Set unique local values for `POSTGRES_PASSWORD`, `JWT_SECRET` (at least 32 UTF-8 bytes), and `ADMIN_PASSWORD`. A Windows PowerShell-safe random-secret generator is:

```powershell
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
[BitConverter]::ToString($bytes).Replace("-", "").ToLowerInvariant()
$rng.Dispose()
```

Copy the printed value into `.env`; do not commit `.env` or use these local secrets in a hosted service. Then:

```powershell
docker compose up --build
```

Compose starts PostgreSQL, runs the one-shot `migrate` service, then starts the API and frontend. Open:

- Frontend: <http://localhost:3000>
- API liveness: <http://localhost:8080/health>
- API readiness/database check: <http://localhost:8080/ready>
- Development Swagger: <http://localhost:8080/swagger>

Stop with `Ctrl+C`; use `docker compose down` to stop containers. Do not use `docker compose down -v` unless you intentionally want to delete the local database volume.

## Option B: Run API and frontend separately

Start PostgreSQL locally (or start only the `postgres` Compose service), then set the API's environment in PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
# For Neon, load only the ignored runtime URL generated in the repository root.
$line = Get-Content -LiteralPath ".env.local" | Where-Object { $_ -match "^DATABASE_URL=" } | Select-Object -First 1
if (-not $line) { throw "DATABASE_URL is missing from .env.local." }
$env:DATABASE_URL = $line.Substring($line.IndexOf("=") + 1).Trim().Trim('"')
# For local PostgreSQL instead, set DATABASE_URL to its Npgsql connection string here.
$env:JWT_SECRET = "<unique-random-secret-at-least-32-bytes>"
$env:JWT_ISSUER = "SchoolWise"
$env:JWT_AUDIENCE = "SchoolWise.Web"
$env:FRONTEND_ORIGINS = "http://localhost:3000"
$env:Seed__AdminPassword = "<unique-local-admin-password>"
```

The API accepts either an Npgsql key/value connection string as above or a PostgreSQL URI. Install the EF tool if needed, apply migrations, then run:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet ef database update --project src/SchoolWise.Infrastructure --startup-project src/SchoolWise.Api
dotnet run --project src/SchoolWise.Api
```

The Neon CLI's root `.env.local` file is git-ignored; ASP.NET Core does not load dotenv files automatically, so export `DATABASE_URL` into the API process as above. Keep the direct/owner migration URL out of the running API environment.

In another terminal:

```powershell
Set-Location frontend
npm ci
npm run dev
```

The frontend defaults to `http://localhost:8080/api/v1`; override it in `frontend/.env.local` if the API runs elsewhere. The API seeds clearly labeled demo schools only in Development.

## Verification foundation checks

Apply migrations before opening `/admin/verifications` (Admin) or `/verifications` (Parent). Use separate synthetic accounts: an Admin cannot redeem Parent codes. Issue a code for an approved school, redeem it as the Parent, then revoke its ID as Admin and reload the Parent status. Do not use real enrollment data or documents.

Run application tests with `dotnet test tests\SchoolWise.Application.Tests`. PostgreSQL/API tests normally use a disposable Docker Testcontainer:

```powershell
$env:RUN_POSTGRES_INTEGRATION_TESTS = "1"
dotnet test tests\SchoolWise.Integration.Tests
```

Without Docker, set `TEST_DATABASE_URL` in the same process to a **direct owner URL for a dedicated, disposable test database/Neon branch** before that command. Tests apply migrations and insert synthetic records. Never point it at production or a shared database. Do not print the value or commit it; the opt-in flag is still required. Test configuration is supplied before API service registration and does not change the app's production settings.

## Review sprint smoke test

1. Apply all migrations, including `AddReviews`, to a local/disposable database before starting the API. Development seeding creates five clearly labeled demo school listings if none exist, with six fictional reviews per demo school (four Published, one PendingModeration, one Rejected).
2. Register a fresh Parent; open a school page and submit a review with all six ratings. No verification code is required.
3. Confirm it is PendingModeration in `/my-reviews` and absent from public ratings/list.
4. Log in as the configured development Admin, open `/admin/reviews` and approve the displayed version.
5. As a visitor, verify the review and score on that school's page. Filter by overall rating and change sorting.
6. Edit as the Parent; confirm it disappears pending approval. Reject with a reason; confirm that reason in My Reviews. Delete and confirm public count/scores update immediately.

Sample accounts have random inaccessible passwords; register your own synthetic Parent to test editing. Sample reviews contain `[SAMPLE - fictional test review]` and are never seeded outside Development or onto real-school listings. Production startup only bootstraps an Admin when both bootstrap variables are supplied.

