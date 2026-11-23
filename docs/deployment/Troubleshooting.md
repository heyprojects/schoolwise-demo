# Troubleshooting

## Render deployment or health check fails

- Confirm Dockerfile path is `src/SchoolWise.Api/Dockerfile`, build context is the repository root, and Build/Start commands are blank.
- Confirm `ASPNETCORE_HTTP_PORTS=10000`. Render expects a web service to bind to `0.0.0.0` on its configured port; the image defaults to `10000`.
- A `/health` success with `/ready` returning `503` means the process is up but PostgreSQL cannot be reached. Check `DATABASE_URL`, Neon compute status, TLS, and the API's Render logs.
- Render Free instances sleep after inactivity. The first request can be delayed; this is a free-tier limitation, not a reason to remove readiness checks.

## Neon connection or migration errors

- Use the pooled (`-pooler`) URI for the running API and a direct URI for migrations.
- For EF migrations set `MIGRATION_DATABASE_URL`, then run:

  ```powershell
  dotnet ef database update --project src/SchoolWise.Infrastructure --startup-project src/SchoolWise.Api
  ```

- Ensure URL-escaped characters in username/password remain encoded in the URI. The API supports the common Neon `sslmode` and `channel_binding` query parameters and requires SSL.
- Check that the selected Neon role can connect to the target database and perform the migration.

## Login works but refresh does not

- Use HTTPS for both hosted frontend and API.
- Browser developer tools should show `schoolwise_refresh` with `HttpOnly`, `Secure`, and `SameSite=None` in Production.
- Set `FRONTEND_ORIGINS` to the exact browser origin (`https://host`, no path or trailing slash). Include both Vercel's production hostname and every custom frontend domain.
- Refresh/revoke requests without an allowed `Origin` are rejected outside Development. Do not work around this by allowing every origin.
- If `JWT_SECRET` changes, existing access/refresh-session behavior may break; keep it stable and rotate intentionally.

## JWT returns 401

- Confirm `JWT_ISSUER` and `JWT_AUDIENCE` exactly match the values used to issue the token.
- `JWT_SECRET` must be at least 32 UTF-8 bytes. Set the same value on all API instances.
- Confirm the API clock is correct and that the token has not expired.

## Browser reports CORS errors

- Configure only exact HTTPS frontend origins in `FRONTEND_ORIGINS`, separated by commas.
- Do not add a trailing slash and do not use `*`; CORS is credentialed for the refresh-token cookie.
- After changing Render environment variables, restart/redeploy the API.

## Vercel pages fail to load school data

- Set `NEXT_PUBLIC_API_URL` to the full Render URL including `/api/v1` and redeploy; the public value is baked in at build time.
- If `INTERNAL_API_URL` is set, ensure it is reachable from Vercel server functions and includes the same `/api/v1` prefix.
- Verify API endpoints `/api/v1/schools`, `/api/v1/schools/slug/<slug>`, `/health`, and `/ready` directly over HTTPS.

## Admin page has no administrator

- The API creates an initial Admin only when both `Seed__AdminEmail` and `Seed__AdminPassword` are present and that email is not already registered.
- After successful first creation, remove the password environment variable. It does not reset or recover an existing Admin account.
- Public registration always creates a Parent, not an Admin. Do not promote accounts by changing public request data.

## Docker or CI issues

- Docker Compose errors about `POSTGRES_PASSWORD`, `JWT_SECRET`, or `ADMIN_PASSWORD` mean the local `.env` is incomplete.
- Docker daemon/image-build failures require Docker Desktop to be running. GitHub Actions CI does not build Docker images.
- If the frontend engine check fails, use Node 22 (the project declares `>=20.19`).
- R2 credentials currently have no effect: uploads and bucket access are not implemented.

