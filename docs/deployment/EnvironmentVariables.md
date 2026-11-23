# Environment Variables

Set hosted values in Render/Vercel secret/environment settings, not in Git. Root `.env.example` contains placeholders for local Docker Compose. Frontend `NEXT_PUBLIC_*` values are public and are compiled into the browser bundle.

## API and database

| Variable | Required | Purpose / format |
|---|---:|---|
| `DATABASE_URL` | Yes in hosted API | Neon pooled URI (`postgresql://...`) or Npgsql key/value string. Neon URIs are normalized to an SSL-required Npgsql string. Never expose it to frontend code. |
| `MIGRATION_DATABASE_URL` | One-off EF migrations | Direct Neon URI (no `-pooler`) for `dotnet ef database update`; the design-time factory prefers this over `DATABASE_URL`. Do not normally configure it on the running API. |
| `JWT_SECRET` | Yes | Random secret of at least 32 UTF-8 bytes. JWT tokens are signed using this value. Never commit it or reuse the local value. |
| `JWT_ISSUER` | Yes | Exact issuer string validated on incoming JWTs. Keep stable across API deploys. |
| `JWT_AUDIENCE` | Yes | Exact audience string validated on incoming JWTs. Keep stable across API deploys. |
| `FRONTEND_ORIGINS` | Yes in production | Exact comma-separated origins, such as `https://app.example.gr,https://schoolwise-demo.vercel.app`; no trailing slash, no wildcard. Used for CORS and refresh/revoke Origin validation. |
| `ASPNETCORE_ENVIRONMENT` | Yes in hosted API | Set to `Production`; development demo data and Swagger are disabled outside Development. |
| `ASPNETCORE_HTTP_PORTS` | Render Docker service | Set to `10000` (the Render web-service port). Compose overrides it to `8080`. |
| `Seed__AdminEmail` | Initial bootstrap only | Email for creating the first Admin account if it does not exist. |
| `Seed__AdminPassword` | Initial bootstrap only | Strong one-time password for the first Admin. Remove both `Seed__AdminEmail` and `Seed__AdminPassword` from the host after successful initialization; leaving only one causes startup failure. It is not used to change an existing password. |
| `POSTGRES_USER` | Local Compose only | Local PostgreSQL username. |
| `POSTGRES_PASSWORD` | Local Compose only | Local PostgreSQL password; Compose fails fast if empty. |
| `ADMIN_EMAIL` | Local Compose only | Development seed email, mapped to `Seed:AdminEmail`. |
| `ADMIN_PASSWORD` | Local Compose only | Development seed password, mapped to `Seed:AdminPassword`. |

For compatibility, the API also recognizes the existing hierarchical .NET keys `ConnectionStrings__Default`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, and `Frontend__Origins`.

## Frontend

| Variable | Required | Purpose / format |
|---|---:|---|
| `NEXT_PUBLIC_API_URL` | Yes in Vercel | Public API base URL including version path, e.g. `https://schoolwise-api.onrender.com/api/v1`. Build-time value; redeploy after changing. |
| `INTERNAL_API_URL` | Optional | Server-only API URL used by Next.js SSR. Set to the same API URL in Vercel if configured; never prefix it with `NEXT_PUBLIC_` unless intended for browser exposure. |

## Cloudflare R2

| Variable | Required | Purpose / format |
|---|---:|---|
| `R2_ACCESS_KEY` | Only after storage integration | S3-compatible R2 access key; keep secret. |
| `R2_SECRET_KEY` | Only after storage integration | R2 secret access key; keep secret. |
| `R2_BUCKET` | Only after storage integration | Private bucket name. |
| `R2_ENDPOINT` | Only after storage integration | `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`. |

R2 variables are listed for future setup but are currently unused by the application. No photo/document upload is available in this MVP.

