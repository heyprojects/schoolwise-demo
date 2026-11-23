# SchoolWise Deployment Preparation Report

**Prepared:** 2026-11-22 · **Updated:** 2026-11-23  
**Scope:** Zero-cost deployment preparation for Vercel, Render, Neon, Cloudflare R2, and GitHub. Neon was provisioned and tested; Render and Vercel were not provisioned.

## Files created

- `GO_LIVE.md`
- `docs/deployment/DeploymentGuide.md`
- `docs/deployment/LocalSetup.md`
- `docs/deployment/ProductionSetup.md`
- `docs/deployment/EnvironmentVariables.md`
- `docs/deployment/CI-CD.md`
- `docs/deployment/Troubleshooting.md`
- `docs/deployment/LaunchChecklist.md`
- `.github/workflows/backend.yml`
- `.github/workflows/frontend.yml`
- `tests/SchoolWise.Integration.Tests/DatabaseConnectionStringResolverTests.cs`

## Changes made

- Added `/ready` database connectivity and retained `/health` as liveness.
- Added production validation for JWT issuer/audience and HTTPS-only, exact frontend origins.
- Added credentialed CORS for multiple configured frontend origins, production `SameSite=None; Secure` refresh cookies, and `Origin` checks for refresh/revoke.
- Added response security headers and limited Swagger to Development.
- Added Neon PostgreSQL URI normalization for the runtime API and EF design-time migrations.
- Added idempotent first-Admin bootstrap for a separately configured one-time production credential.
- Updated `.env.example` and Compose variables for the requested API, JWT, frontend, and R2 settings. R2 values are explicitly documented as unused until uploads are implemented.
- Kept both Dockerfiles multi-stage; added non-root runtime users, smaller build contexts, and a Render-compatible API port/health check.
- Added backend and frontend GitHub Actions validation/publish workflows.
- Provisioned Neon project `schoolwise-demo` in Frankfurt, `production` branch, `schoolwise` database, and a scoped `schoolwise_app` runtime role. Applied the EF Core initial migration and pulled the pooled runtime URL into ignored `.env.local`.

## Verification

| Check | Result |
|---|---|
| `dotnet test SchoolWise.sln --configuration Release` | Passed: 11 application tests and 2 configuration tests; 3 PostgreSQL integration tests skipped because Docker is unavailable |
| `dotnet publish src/SchoolWise.Api/SchoolWise.Api.csproj --configuration Release --no-restore` | Passed |
| `npm run lint` | Passed |
| `npm run build` | Passed |
| YAML parse of `docker-compose.yml` and both GitHub workflows | Passed |
| Local Markdown links | Passed across 27 Markdown files |
| Local Production-mode API probe with unreachable PostgreSQL | `/health` returned `200`, `/ready` returned `503`, `/swagger` returned `404`, and HSTS was present |
| Neon configuration | `neon config plan`: no changes; `neon deploy`: succeeded and pulled Neon environment variables |
| Neon schema | `schoolwise_app` connected to `schoolwise`; 1 migration applied; tables `refresh_tokens`, `school_photos`, `schools`, `users`, and `__EFMigrationsHistory` present |
| API against Neon | `/ready=200`, school search `200`, registration `201`, login `200`, authenticated `/me=200` |
| Frontend SSR against API backed by Neon | Production build passed; `/` and `/schools` returned `200` with SSR API URL configured |
| Neon runtime credential | Rotated after credential output during setup; updated local ignored `.env.local`; credential value not recorded in this report |
| Docker image build / `docker compose config` | Not run: Docker CLI is not installed in this environment. YAML was parsed, but Dockerfile/runtime behavior remains unverified here. |
| Render/Vercel deployment, browser cross-site cookie flow, backup/restore | Not run: no Render/Vercel services or backup procedure are configured. |

## Risks before production

1. **The requested free plans are not suitable for a commercial production launch.** Vercel Hobby terms restrict use to personal/non-commercial projects; Render says Free services are not for production and may sleep. Free limits and terms can change. See [Vercel terms](https://vercel.com/legal/terms) and [Render free services](https://render.com/docs/free).
2. The current workspace is not a Git repository and no GitHub remote is configured. The workflows have been YAML-validated but have not run on GitHub.
3. No Docker image build or Testcontainers integration run was possible; Dockerfile/runtime-in-container behavior remains unverified.
4. There is no automated backup/restore job or tested disaster-recovery procedure. Neon Free recovery and quota limits are not an SLA.
5. R2 credentials are placeholders only; photo/document uploads are not implemented.
6. Authentication rate limiting and a Content Security Policy still need assessment before a public commercial launch.
7. Reviews and verified-parent moderation are Phase 2 and are not implemented. Do not collect or display reviews/ratings on this deployment.
8. The first-Admin bootstrap secret must be removed after successful initialization; an existing Admin password is not reset by the bootstrap.
9. A synthetic smoke-test Parent account remains in the Neon database; no real user data was used.

## Recommended next step

First establish a private GitHub repository, push `main`, and verify both workflows. Use [GO_LIVE.md](GO_LIVE.md) only for a non-commercial, synthetic-data demo. Before inviting real parents or launching SchoolWise commercially, select provider plans/alternatives that permit commercial use and meet uptime/recovery needs, then deploy a staging environment, run direct-URL migrations, and test authentication, database restore, and health checks end-to-end.

