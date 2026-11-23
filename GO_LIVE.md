# SchoolWise Go-Live Runbook

**Purpose:** Get a non-commercial, synthetic-data demo online and smoke-tested in about 30 minutes once accounts and the GitHub repository are ready. This is not production approval.

## 1. Deployment success estimate

“Successful” means Vercel serves the frontend, Render reports the API ready against Neon, and a browser can load school data and complete an Admin login/refresh check.

| Starting conditions | Estimated chance of success within 30 minutes |
|---|---:|
| GitHub repo/`main` and CI are ready; provider accounts work; Neon, Render, and Vercel names/secrets are at hand | **About 60%** |
| Starting from the current workspace, where there is no Git repository or hosted service configured | **About 25%** |
| Commercial production on Vercel Hobby + Render Free | **Do not proceed**; those plans are not suitable for this commercial service |

These are subjective planning estimates, not measured provider statistics. Local builds passed, but the report records no Docker image build, hosted migration, live TLS, or browser cookie test. Remote builds, provider verification, and cold starts can exceed the time budget.

## 2. Go/no-go and prerequisites

### Go only for a demo

Vercel Hobby is limited to personal/non-commercial use, and Render says Free services are not for production. Use this runbook only for an internal/non-commercial demo with synthetic accounts and data. Do **not** invite real parents, accept real reviews, or present the service as a production launch. See [the deployment report](DEPLOYMENT_REPORT.md) and [production setup notes](docs/deployment/ProductionSetup.md).

### Have these ready before starting the 30-minute timer

- [ ] GitHub repository exists, contains the latest code on `main`, and both GitHub Actions workflows have passed.
- [ ] Vercel, Render, and Neon accounts are verified and you are already signed in.
- [ ] Neon, Render, and Vercel resource names are chosen and available. Skip custom domains; DNS and registration are outside this timebox.
- [ ] Neon database `schoolwise` and runtime role `schoolwise_app` exist; pooled runtime and direct migration URLs can be copied securely.
- [ ] Local .NET 10 SDK and EF tool are installed. **Currently verified:** .NET SDK `10.0.401`, `dotnet-ef` `10.0.15`.
- [ ] A password manager can generate/store a JWT secret and a temporary Admin password. Do not put either in Git, command history, or this file.
- [ ] Use only synthetic demo data. Skip R2: the app has no upload implementation.

**Current workspace blocker:** `git rev-parse` currently reports that this folder is not a Git repository. The source must first be committed and pushed to GitHub before Vercel or Render can import it. Git setup and provider account verification are not included in the 30-minute estimate. Docker is also unavailable locally, so the remote Docker build is an unverified step.

## 3. Common first-deploy failures

| Symptom | Likely cause | Check/fix |
|---|---|---|
| Vercel cannot import/build | No GitHub remote, wrong branch, or root directory | Push `main`; set Vercel root to `frontend`; use `npm ci` and `npm run build`. |
| Render Docker build fails | Wrong context or Dockerfile path | Context is repository root; Dockerfile is `src/SchoolWise.Api/Dockerfile`; leave Build and Start commands blank. |
| Render service never becomes healthy | Wrong port or missing environment | Set `ASPNETCORE_HTTP_PORTS=10000`; health path `/ready`; set `ASPNETCORE_ENVIRONMENT=Production`. |
| `/ready` is `503` | Database URL/permissions are wrong, migrations did not run, or Neon is waking | Check Render logs, pooled `DATABASE_URL`, and confirm the direct-URL migration succeeded. Retry once for Neon cold start. |
| API exits on startup | JWT or origin configuration missing/invalid | Set `JWT_SECRET` (at least 32 bytes), `JWT_ISSUER`, `JWT_AUDIENCE`, and HTTPS-only `FRONTEND_ORIGINS` with no trailing slash. |
| Login works but refresh fails | Browser cookie blocked or origin differs | Use HTTPS; check `HttpOnly; Secure; SameSite=None`; exact Vercel origin must be in Render `FRONTEND_ORIGINS`. |
| Frontend loads but school data fails | API URL was omitted or stale at build time | Set `NEXT_PUBLIC_API_URL=https://<render-service>.onrender.com/api/v1`, then redeploy Vercel. |
| Admin login fails | Seed was omitted, database migration did not finish, or wrong credentials | Set `Seed__AdminEmail` and `Seed__AdminPassword` for first initialization; verify the account once, then remove the password secret. |
| First request is slow | Render Free sleeps; Neon may scale to zero | Wait for health/readiness and retry. Do not mask a persistent failure by weakening health checks. |

## 4. Exact 30-minute sequence

The clock assumes the prerequisites above are already satisfied. Times are targets, not guarantees.

### 00:00–00:03 — Confirm the GitHub release

1. Open the GitHub repository and confirm the latest commit is on `main`.
2. Confirm **Backend CI** and **Frontend CI** are green.
3. If either is missing or red, stop here; do not deploy an unverified commit.

### 00:03–00:07 — Create Neon and apply migrations

1. Create a Neon project in an EU region near the Render region and select/create the `schoolwise` database.
2. Copy the **pooled** URI for the API and the **direct** URI for migrations. Keep them out of source control.
3. From the repository root, enter the direct URI without echoing it to command history, then run migrations:

   ```powershell
   $secure = Read-Host "Neon direct migration URI" -AsSecureString
   $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
   try {
       $env:MIGRATION_DATABASE_URL = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
       dotnet ef database update --project src/SchoolWise.Infrastructure --startup-project src/SchoolWise.Api
       if ($LASTEXITCODE -ne 0) { throw "Database migration failed; stop deployment." }
   }
   finally {
       Remove-Item Env:\MIGRATION_DATABASE_URL -ErrorAction SilentlyContinue
       [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
   }
   ```

4. Do not proceed unless the command reports success.

### 00:07–00:17 — Deploy the API to Render

1. Create a **Web Service** from the GitHub repo:
   - Runtime: **Docker**
   - Branch: `main`
   - Root directory/build context: repository root
   - Dockerfile: `src/SchoolWise.Api/Dockerfile`
   - Build command: blank
   - Start command: blank
   - Health check path: `/ready`
   - Region: as close as possible to Neon.
2. Add these environment values in Render; keep secrets in its secret fields:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ASPNETCORE_HTTP_PORTS=10000`
   - `DATABASE_URL=<pooled schoolwise_app URL from the neon connection-string command>`
   - `JWT_SECRET=<fresh random secret, at least 32 UTF-8 bytes>`
   - `JWT_ISSUER=https://<your-render-service>.onrender.com`
   - `JWT_AUDIENCE=https://<your-vercel-project>.vercel.app`
   - `FRONTEND_ORIGINS=https://<your-vercel-project>.vercel.app`
   - `Seed__AdminEmail=<demo-admin email>`
   - `Seed__AdminPassword=<temporary strong password>`
3. Deploy. Wait for `/ready` to pass. Copy the final Render URL.

### 00:17–00:24 — Deploy the frontend to Vercel

1. Import the same GitHub repository as a Vercel project:
   - Root directory: `frontend`
   - Framework: Next.js
   - Install: `npm ci`
   - Build: `npm run build`
2. Set the Production variable `NEXT_PUBLIC_API_URL` to:

   ```text
   https://<your-render-service>.onrender.com/api/v1
   ```

3. Deploy and note the exact Vercel production origin. If it differs from the origin configured on Render, update Render `FRONTEND_ORIGINS` to the exact origin (no trailing slash) and redeploy the API.

### 00:24–00:30 — Smoke-test and remove bootstrap secret

1. Open `https://<your-render-service>.onrender.com/health` and `/ready`; both must return `200`.
2. Open the Vercel site; verify homepage, school search, and a school detail page load.
3. Log in to the demo Admin; verify school administration loads and refresh survives a page reload. Confirm the browser stores the refresh cookie as `HttpOnly`, `Secure`, and `SameSite=None`.
4. Remove `Seed__AdminPassword` from Render and redeploy. Confirm the existing Admin still logs in. If the cleanup deploy is still running at 30 minutes, keep the demo private until it finishes.
5. Do not add a custom domain, set up R2, enable reviews, or invite real users during this timebox.

## 5. After the demo is online

- Complete [the launch checklist](docs/deployment/LaunchChecklist.md); in particular, test backup restoration and review the current provider terms before any real-user launch.
- Use [troubleshooting](docs/deployment/Troubleshooting.md) for failed health, CORS, or authentication checks.
- Do not treat a successful demo deployment as evidence of production readiness. The [deployment report](DEPLOYMENT_REPORT.md) lists the unverified items and risks.

