# SchoolWise Development Roadmap

## Phase 1 — Core foundation (current)

- Clean Architecture solution and CQRS feature slices.
- User registration/login, password hashing, JWT access tokens, rotating refresh tokens, role policies.
- School CRUD for Admin.
- Approved school catalogue, keyword/filter/sort/pagination, school detail/photos.
- PostgreSQL EF Core schema, migrations, representative Athens seed data.
- Next.js 15 public homepage, login/register, school list/details, admin school CRUD.
- Docker Compose local environment, Swagger, ProblemDetails, Serilog, API versioning, automated tests.
- **Reviews are not part of this phase.**

## Phase 2 — Verified reviews

- Parent eligibility and school relationship verification workflow.
- Review submission, moderation, reporting, and immutable verification audit.
- Abuse prevention, privacy controls, and transparent review guidelines.
- Detailed design (not implementation): [Phase 2 Architecture](Phase2-Architecture.md), [Database](Phase2-Database.md), [API](Phase2-ApiDesign.md), [Moderation](Phase2-Moderation.md), [Fraud Prevention](Phase2-FraudPrevention.md), [GDPR](Phase2-GDPR.md), [User Flows](Phase2-UserFlows.md), [Roadmap](Phase2-Roadmap.md), and [MVP Recommendation](Phase2-MVP-Recommendation.md).
- One-developer, under-eight-week cut: [Phase2-MVP-Cut.md](Phase2-MVP-Cut.md).
- First-100 verified-review acquisition: [ColdStartStrategy.md](ColdStartStrategy.md).

## Phase 3 — Discovery intelligence

- Saved searches and school comparisons.
- Parent preferences and explainable school matching.
- Recommendation quality evaluation and privacy safeguards.

## Phase 4 — Community and analytics

- School-specific parent communities and moderation.
- Aggregated school analytics with minimum cohort/privacy thresholds.
- Operational dashboards and data export controls.

## Quality gates for Phase 1

1. `dotnet build SchoolWise.sln` succeeds.
2. Unit/integration tests cover domain validation, auth flows, school filters, and role access.
3. EF migrations apply to a clean PostgreSQL database and seed sample records.
4. `npm run build` succeeds for the frontend.
5. Docker Compose health checks show API, database, and frontend ready.
6. Swagger documents all v1 endpoints.
7. Public access never exposes unapproved schools; Admin routes reject non-Admin roles.
