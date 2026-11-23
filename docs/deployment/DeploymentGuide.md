# SchoolWise Deployment Guide

**Target:** Vercel Hobby frontend, Render Free API, Neon Free PostgreSQL, Cloudflare R2, GitHub.  
**Verified:** 2026-11-22. Platform limits and terms can change; re-check the linked official pages before launch.

## Important: zero-cost is not production-ready

This combination can avoid a monthly infrastructure bill for a small **non-commercial demo or closed pilot**, subject to provider limits. It is not a suitable production commitment for a commercial SchoolWise launch:

- Vercel's Hobby plan is restricted to personal or non-commercial use by its [Terms of Service](https://vercel.com/legal/terms).
- Render says Free services are not for production, and its free web services sleep after inactivity. See [Render free services](https://render.com/docs/free).
- Free database, compute, bandwidth, and storage quotas can be exhausted or changed; no provider combination here guarantees uptime or recovery.

Do not collect real parent reviews or rely on this stack for a public commercial service until the plan terms, privacy obligations, backups, and support expectations are addressed. The lowest-risk zero-cost use is a non-commercial, synthetic-data demo. A real pilot needs explicit review of provider terms and production continuity.

## Deployment shape

```text
Browser ── HTTPS ──> Vercel Next.js
                         │
                         └── HTTPS + credentialed CORS ──> Render ASP.NET Core API
                                                               │
                                                               └── TLS ──> Neon PostgreSQL

Cloudflare R2: bucket/account setup is documented, but application uploads are not implemented.
GitHub: source, CI checks, and deployment integration.
```

The API listens on port `10000` in its container and exposes:

- `GET /health` — process liveness.
- `GET /ready` — readiness; returns `503` if PostgreSQL cannot be reached.

Production schema changes are applied explicitly with EF Core before deploying the API. Render Free does not provide a production-grade release/migration pipeline; do not make application replicas race to migrate on startup.

## Recommended order

1. Review [Production Setup](ProductionSetup.md) and resolve the commercial-use/production-plan caveat.
2. Create Neon and save its pooled application URL and direct migration URL separately.
3. Run EF Core migrations from a trusted workstation using the direct URL.
4. Create the Render Docker web service and configure secrets, CORS, port, and `/ready`.
5. Create the Vercel project with `frontend/` as the root and set `NEXT_PUBLIC_API_URL`.
6. Add the final Vercel hostname (and any custom hostname) to `FRONTEND_ORIGINS` on Render.
7. Bootstrap one Admin, then remove the one-time password secret.
8. Complete [Launch Checklist](LaunchChecklist.md), including real-browser authentication and recovery checks.

## Related documents

- [30-minute Go-Live runbook](../../GO_LIVE.md)
- [Local Setup](LocalSetup.md)
- [Production Setup](ProductionSetup.md)
- [Environment Variables](EnvironmentVariables.md)
- [CI/CD](CI-CD.md)
- [Troubleshooting](Troubleshooting.md)
- [Launch Checklist](LaunchChecklist.md)

