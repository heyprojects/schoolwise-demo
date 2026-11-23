# Production Setup

## Read this before creating services

The requested zero-cost services are appropriate only for a non-commercial experiment:

- [Vercel Hobby terms](https://vercel.com/legal/terms) limit the plan to personal or non-commercial use.
- [Render Free services](https://render.com/docs/free) are explicitly not recommended for production. A free web service sleeps after inactivity and has ephemeral local storage.

For a commercial SchoolWise service, use a plan whose terms permit commercial use and whose uptime, support, and recovery guarantees meet the product's needs. The steps below describe a non-commercial/demo deployment; do not interpret them as production approval.

## 1. Create Neon PostgreSQL

1. Create an account at [Neon](https://neon.tech/) and create a project in an EU region close to the Render API region.
2. Create/select the `schoolwise` database, a least-privilege runtime role for the API, and a separately privileged migration role. The project is currently provisioned with database `schoolwise` and runtime role `schoolwise_app` scoped for `read_write` on `public`; the Neon owner role is used only for migrations. The API uses the pooled runtime URI; EF migrations use the direct migration URI and migration role.
3. In the connection details, copy both:
   - **Pooled connection URI** (host contains `-pooler`) for the running API.
   - **Direct connection URI** (no `-pooler`) for EF migrations.
4. Keep both URLs private. The application accepts Neon `postgresql://` URIs, percent-decodes credentials, and requires TLS by default.
5. Neon Free quotas currently include 1 GB of storage and 100 compute-hours per project/month; compute suspends after inactivity. Check the [current Neon Free quotas](https://neon.com/faqs/free-plan-limits-and-quotas).

To print the runtime URL for copying directly into Render's secret field (do not paste it in chat or commit it), use:

```powershell
& "$env:APPDATA\npm\neon.cmd" connection-string production --role-name schoolwise_app --database-name schoolwise --pooled
```

Before deploying the API, apply migrations from a trusted workstation using the direct URI:

```powershell
$env:MIGRATION_DATABASE_URL = "<Neon-direct-postgresql-uri>"
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet ef database update --project src/SchoolWise.Infrastructure --startup-project src/SchoolWise.Api
Remove-Item Env:\MIGRATION_DATABASE_URL
```

Take a backup before production schema changes. The design-time DbContext factory prioritizes `MIGRATION_DATABASE_URL`; it does not print the value.

## 2. Create the Render API service

1. In Render, choose **New → Web Service** and connect the GitHub repository.
2. Set:
   - **Runtime:** Docker
   - **Branch:** `main`
   - **Root Directory:** repository root (`.` / blank)
   - **Dockerfile Path:** `src/SchoolWise.Api/Dockerfile`
   - **Docker Build Context:** repository root (`.`)
   - **Build Command:** leave blank; the Dockerfile builds/publishes the API.
   - **Start Command:** leave blank; use the Docker image entrypoint.
   - **Health Check Path:** `/ready`
   - **Region:** same EU region as Neon when available.
   - **Instance:** Free only for non-commercial demo/pilot use.
3. In **Environment**, set:

| Key | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `10000` |
| `DATABASE_URL` | Neon pooled PostgreSQL URI |
| `JWT_SECRET` | New random secret, at least 32 UTF-8 bytes |
| `JWT_ISSUER` | Stable issuer identifier, e.g. `https://api.schoolwise.example` |
| `JWT_AUDIENCE` | Stable audience identifier, e.g. `https://schoolwise.example` |
| `FRONTEND_ORIGINS` | Exact comma-separated browser origins, e.g. `https://schoolwise-demo.vercel.app,https://schoolwise.example` |
| `Seed__AdminEmail` | The initial administrator's email |
| `Seed__AdminPassword` | One-time strong initial administrator password |

Render's default web-service port is `10000`; the Docker image listens on that port and binds to all interfaces. The compose setup overrides this to local port `8080`.

4. Deploy and wait for `/ready` to pass. Log in using the initial Admin account, then remove both `Seed__AdminEmail` and `Seed__AdminPassword` from Render's environment and redeploy. Removing only one causes startup to fail. The seeder only creates the account if that email does not already exist; it does not reset the password or delete the existing account.
5. Confirm the Render HTTPS URL works and note it for Vercel. Render provides managed TLS for its `onrender.com` hostname; custom API domains can be added in the Render dashboard.

The API only enables Swagger in Development. Production errors are returned through ProblemDetails and logs go to the console for the host to collect.

## 3. Create the Vercel frontend

1. In Vercel, choose **Add New → Project**, import the same GitHub repository, and configure:
   - **Root Directory:** `frontend`
   - **Framework Preset:** Next.js
   - **Install Command:** `npm ci`
   - **Build Command:** `npm run build`
   - **Output Directory:** leave at the Next.js default.
2. Add this **Production** environment variable:

| Key | Value |
|---|---|
| `NEXT_PUBLIC_API_URL` | `https://<render-service>.onrender.com/api/v1` |

`NEXT_PUBLIC_API_URL` is embedded into browser code at build time and is public, not a secret. Redeploy after changing it. `INTERNAL_API_URL` is optional; when set, use the same Render API URL so Next.js server-side requests can reach the API.
3. Deploy, then add a custom domain under **Project → Settings → Domains** if you own one. Apply the DNS records Vercel displays and wait for certificate provisioning. A custom domain has an external registration cost and is not included in the zero-monthly-infrastructure assumption.
4. Add both the generated Vercel hostname and custom frontend domain to Render's `FRONTEND_ORIGINS`, without trailing slashes, then redeploy the API.

Vercel and Render are different sites on their default hostnames. The production refresh cookie therefore uses `SameSite=None; Secure`; refresh and revoke endpoints also require an allowed `Origin`. Verify that the browser accepts `schoolwise_refresh` and that login/refresh work end-to-end.

## 4. Cloudflare R2

R2 setup is optional until upload support exists:

1. Create a private R2 bucket.
2. Create an S3 API token with **Object Read & Write** scoped only to that bucket.
3. Save its Access Key ID and Secret Access Key in a secret manager.
4. Set `R2_ENDPOINT` to `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`, `R2_BUCKET` to the bucket name, and the two R2 credentials to the token values.
5. Do not enable public bucket access or publish secrets to the frontend. If browser-direct uploads are implemented later, use short-lived, object-scoped presigned URLs and a restrictive bucket CORS policy.

R2's current free allowance is 10 GB-month storage, 1 million Class A operations, and 10 million Class B operations; egress is free. Check [R2 pricing](https://developers.cloudflare.com/r2/pricing/). **The current SchoolWise API has no R2 client, upload endpoint, or image migration path; these variables are not consumed yet.**

## 5. TLS, CORS, logging, and backups

- Vercel and Render terminate HTTPS and provide managed certificates. Use only their HTTPS URLs in production and keep the API HSTS header enabled.
- Configure exact frontend origins; do not use `*` with credentialed CORS. Keep `FRONTEND_ORIGINS` synchronized with Vercel domains.
- JWT issuer/audience and secret must be identical across API instances. Do not reuse development secrets.
- API logs are structured Serilog console events. Keep secrets, passwords, refresh tokens, and parent-provided content out of logs.
- API/frontend security headers include HSTS, `X-Content-Type-Options`, `X-Frame-Options`, Referrer-Policy, and Permissions-Policy. A Content Security Policy and login/register rate limiting are not configured yet; evaluate and test both before a public commercial launch.
- Before each migration, export a backup using the direct URL and retain it encrypted in a private location. Test restoration before trusting backups. Neon Free's recovery window and quotas are limited; see its current [free-plan limits](https://neon.com/faqs/free-plan-limits-and-quotas).
- No automatic database backup job or disaster-recovery workflow is configured in this repository.

