# SchoolWise Launch Checklist

Do not treat a green build as production approval. Complete and record each item before exposing the service to real families.

## Provider and access

- [ ] Confirm Vercel/Render plan terms allow the intended commercial use and service level; free Hobby/Free tiers are demo-only for this product.
- [ ] Restrict GitHub `main` to reviewed changes and require Backend CI and Frontend CI checks.
- [ ] Keep provider secrets out of GitHub, `.env.example`, browser variables, and logs.
- [ ] Assign an owner for provider billing/quotas, incident response, and account recovery.

## Database and migrations

- [ ] Create Neon PostgreSQL in an EU region close to the API.
- [ ] Store pooled application URI and direct migration URI separately.
- [ ] Apply reviewed EF migrations before deploying the matching API.
- [ ] Confirm PostgreSQL is not publicly exposed with unnecessary permissions; use TLS and a least-privilege app role.
- [ ] Take an encrypted backup and perform a restore test before accepting real data.
- [ ] Set storage/compute/bandwidth alerts and document the plan for quota exhaustion or suspended compute.

## Authentication and Admin

- [ ] Verify `JWT_SECRET` is a unique random value of at least 32 UTF-8 bytes; set stable issuer and audience.
- [ ] Bootstrap the first Admin through `Seed__AdminEmail` / `Seed__AdminPassword`; log in; then remove both variables together to avoid startup failure.
- [ ] Confirm public registration creates only Parent accounts and Admin routes reject non-Admin users.
- [ ] Test login, refresh, logout/revoke, expiry, duplicate accounts, and wrong credentials from the deployed frontend.
- [ ] Verify refresh cookie is `HttpOnly; Secure; SameSite=None` and refresh/revoke reject unknown origins.
- [ ] Confirm exact `FRONTEND_ORIGINS` includes only production frontend hostnames.

## School Search and Details

- [ ] Confirm catalogue accuracy, public/private school types, location, filters, sorting, pagination, and no demo data.
- [ ] Verify public search/details show only approved schools.
- [ ] Verify detail page works for valid and unknown slugs and all images/links are authorized to use.
- [ ] Test slow/cold API responses and show a useful error/empty state on the frontend.

## Admin CRUD

- [ ] Create, edit, approve/unapprove, and delete a school as Admin.
- [ ] Confirm a Parent cannot read unpublished admin data or invoke write endpoints.
- [ ] Verify validation, conflict responses, and changes persist after API restart.

## Reviews readiness

- [ ] Keep reviews disabled; Phase 2 reviews, verified-parent codes, moderation, and review storage are not implemented in this MVP.
- [ ] Do not advertise a verified-review badge, score, or review count until the Phase 2 trust controls and legal review are complete.
- [ ] Before Phase 2 launch, approve the verification evidence, moderation/appeal policy, retention/deletion process, privacy notice, and school-facing process.

## Monitoring, backups, and SSL

- [ ] Verify `GET /health` returns `200` and `GET /ready` returns `200` only when PostgreSQL is reachable.
- [ ] Confirm Render/Vercel HTTPS certificates are active and HTTP is redirected or rejected at the edge.
- [ ] Confirm HSTS and frontend/API security headers in a real browser/network trace.
- [ ] Confirm logs are available, redacted, and do not include passwords, tokens, database URLs, or review content.
- [ ] Add and test login/register rate limits and a Content Security Policy before a public commercial launch.
- [ ] Define a human response target for outages, data access/deletion requests, account lockout, and security reports.
- [ ] Run and restore a database backup; record the tested procedure and retention.
- [ ] Confirm current free-tier quotas and the response to a quota limit; no free-tier uptime or recovery SLA is assumed.

