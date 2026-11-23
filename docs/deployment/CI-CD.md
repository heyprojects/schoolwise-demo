# CI/CD

## GitHub Actions

Two workflows live in `.github/workflows/`:

| Workflow | Trigger scope | Checks |
|---|---|---|
| `backend.yml` | Push/PR to `main` when backend, tests, solution, or project files change; manual dispatch | .NET 10 restore, Release build, tests, API publish, API artifact and test-results artifacts |
| `frontend.yml` | Push/PR to `main` when `frontend/` changes; manual dispatch | Node 22 install (`npm ci`), ESLint, Next.js production build |

Both use read-only repository permissions and do not require deployment credentials. The backend integration tests that require PostgreSQL/Testcontainers are skipped unless `RUN_POSTGRES_INTEGRATION_TESTS=1`; GitHub-hosted Docker execution is not enabled by these workflows.

Frontend CI uses `https://api.example.invalid/api/v1` as a non-secret build placeholder. It validates compilation and does not contact a production API.

## Hosted deployments

For the first deployment, keep provider auto-deploy disabled and deploy manually after both required CI checks pass. Configure GitHub branch protection for `main` to require both workflows. Only then enable Render/Vercel automatic deployments if their branch protection/check integration is configured and tested.

- **Render:** connect the GitHub repository and use the Docker settings in [Production Setup](ProductionSetup.md). The workflow publishes a downloadable API artifact; Render separately builds from the Dockerfile. It does not currently deploy from the artifact.
- **Vercel:** import the repository with `frontend/` as the root. Vercel builds Next.js from source on push; this workflow runs an independent lint/build check.
- Never add Neon, JWT, Admin, or R2 credentials to GitHub workflow YAML or repository files. Store them only in the relevant provider's environment settings.
- Schema migrations are a deliberate manual release step using `MIGRATION_DATABASE_URL`; do not run them automatically from every API instance.

## Local checks

From the repository root:

```powershell
dotnet restore SchoolWise.sln
dotnet build SchoolWise.sln --configuration Release --no-restore
dotnet test SchoolWise.sln --configuration Release --no-build
dotnet publish src/SchoolWise.Api/SchoolWise.Api.csproj --configuration Release --no-restore
```

For frontend checks:

```powershell
Set-Location frontend
npm ci
npm run lint
npm run build
```

