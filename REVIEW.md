# SchoolWise Code Review

**Review date:** 2026-11-22  
**Scope:** Architecture, security, database, authentication, API, frontend, maintainability, and Phase 1 MVP readiness. Findings below record the review and the subsequent fixes requested by the user.

Findings describe the code as reviewed before remediation; each resolution note records the resulting change.

## Summary

The solution has a coherent Phase 1 structure and clear feature boundaries. The review identified unsupported .NET 8, unsafe Compose defaults, Admin workflow gaps, and missing integration coverage. Remediation has upgraded the runtime, secured local Compose configuration, completed the affected workflows, and added opt-in PostgreSQL integration tests.

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| 1 | 🟠 HIGH | [SchoolWise.Api.csproj](src/SchoolWise.Api/SchoolWise.Api.csproj) / [Dockerfile](src/SchoolWise.Api/Dockerfile) | 4; 1, 13 | .NET 8 is out of support | 10/10 |
| 2 | 🟠 HIGH | [docker-compose.yml](docker-compose.yml) | 6-9 | PostgreSQL exposed with known default credentials | 9/10 |
| 3 | 🟡 MEDIUM | [SchoolsController.cs](src/SchoolWise.Api/Controllers/SchoolsController.cs) / [admin schools page](frontend/src/app/admin/schools/page.tsx) | 35-36; 65-66 | Admin cannot edit unapproved school drafts | 10/10 |
| 4 | 🟡 MEDIUM | [RegisterCommand.cs](src/SchoolWise.Application/Auth/RegisterCommand.cs) / [GlobalExceptionHandler.cs](src/SchoolWise.Api/Infrastructure/GlobalExceptionHandler.cs) | 35-43; 17-26 | Concurrent duplicate registration becomes HTTP 500 | 9/10 |
| 5 | 🟡 MEDIUM | [Program.cs](src/SchoolWise.Api/Program.cs) / [DatabaseSeeder.cs](src/SchoolWise.Infrastructure/Persistence/DatabaseSeeder.cs) | 112-115; 18-20 | Every API instance runs migrations at startup | 8/10 |
| 6 | 🟡 MEDIUM | [api.ts](frontend/src/lib/api.ts) | 81-84 | Admin catalogue is capped at 50 schools | 10/10 |
| 7 | 🟡 MEDIUM | [School details page](<frontend/src/app/schools/[slug]/page.tsx>) / [home client](frontend/src/app/home-client.tsx) / [schools browser](frontend/src/app/schools/schools-browser.tsx) | 1, 13; 1, 26 | Public catalogue content was fetched only after client hydration | 8/10 |
| 8 | 🟡 MEDIUM | [Application tests](tests/SchoolWise.Application.Tests/) | — | Critical API, authentication, and PostgreSQL flows lack integration coverage | 10/10 |
| 9 | ⚪ LOW | [Program.cs](src/SchoolWise.Api/Program.cs) / `frontend/src/components/ui/select.tsx` (removed) | 58; 1 | Unused authorization/UI scaffolding adds maintenance surface | 9/10 |

## Findings

### 1. .NET 8 is out of support — 🟠 HIGH

**Evidence:** The solution targets `net8.0`, and the API container uses .NET 8 SDK/runtime images. Microsoft ended .NET 8 support on **November 10, 2026**. Supported applications continue to run, but no further security servicing is provided.

**Why it matters:** A new platform launched on an unsupported runtime will miss future runtime and framework security fixes. Updating only the target framework is insufficient if the EF Core and Npgsql packages remain on version 8.

**Suggested fix:** Upgrade to a currently supported .NET LTS release (for example, .NET 10) and align the ASP.NET Core, EF Core, Npgsql, Docker images, SDK/tooling, and CI matrix. See [Microsoft's .NET support lifecycle](https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core).

**Resolution:** Upgraded the solution, compatible persistence/API packages, API container images, and tooling guidance to .NET 10 LTS.

### 2. PostgreSQL is published with known default credentials — 🟠 HIGH

**Evidence:** [docker-compose.yml](docker-compose.yml#L6) supplied a predictable database password default and published PostgreSQL without restricting the host interface.

**Why it matters:** Docker normally publishes this port on all host interfaces. If the machine is reachable from an untrusted network, anyone who can reach the port can access or modify the database using the documented defaults. This is a development Compose file, so the risk depends on where it is run.

**Suggested fix:** Remove the database port mapping when host access is unnecessary; otherwise bind it to `127.0.0.1`. Require non-default credentials for any shared or deployed environment and keep them in a secret manager.

**Resolution:** Bound PostgreSQL to loopback, removed Compose password/JWT/admin-password defaults, required local `.env` values, and removed committed Development secrets in favor of .NET User Secrets.

### 3. Admin cannot edit unapproved school drafts — 🟡 MEDIUM

**Evidence:** The public detail endpoint in [SchoolsController.cs](src/SchoolWise.Api/Controllers/SchoolsController.cs#L35) constructs `GetSchoolByIdQuery` with its default `IncludeUnapproved = false`. The admin editor fetches school details through the public `getSchool` call in [admin/schools/page.tsx](frontend/src/app/admin/schools/page.tsx#L65).

**Why it matters:** The admin list intentionally includes unapproved schools, but selecting a draft for editing calls an endpoint that filters it out and returns not found. Drafts created by the admin cannot be reopened for editing.

**Suggested fix:** Add an Admin-authorized detail endpoint/query that includes unapproved records, and have the admin editor use it. Keep the public detail endpoint restricted to approved schools.

**Resolution:** Added an Admin-only detail route and changed the editor to use it.

### 4. Concurrent duplicate registration can return HTTP 500 — 🟡 MEDIUM

**Evidence:** [RegisterCommand.cs](src/SchoolWise.Application/Auth/RegisterCommand.cs#L35) checks for an existing email, then adds and saves the user at lines 42-43. The unique database index is the final arbiter, while [GlobalExceptionHandler.cs](src/SchoolWise.Api/Infrastructure/GlobalExceptionHandler.cs#L17) maps unrecognized exceptions to 500.

**Why it matters:** Two requests for the same normalized email can both pass the pre-check. One insert then violates the unique index and is reported as an unexpected server failure rather than the documented conflict.

**Suggested fix:** Preserve the unique constraint, but handle PostgreSQL unique-violation error `23505` for the email index and return a stable `409 email_taken` response. Apply the same pattern to concurrent slug creation.

**Resolution:** The exception handler now maps unique violations for user email and school slug indexes to a stable `409 unique_conflict` response.

### 5. Each API instance runs migrations during startup — 🟡 MEDIUM

**Evidence:** [Program.cs](src/SchoolWise.Api/Program.cs#L112) resolves `DatabaseSeeder` before serving requests; [DatabaseSeeder.cs](src/SchoolWise.Infrastructure/Persistence/DatabaseSeeder.cs#L20) calls `MigrateAsync` in every environment.

**Why it matters:** This is convenient for a single local instance, but production replicas can attempt schema changes simultaneously, and the runtime database identity must have DDL privileges. Startup also becomes dependent on migration completion. The current [Architecture.md](docs/Architecture.md#L51) describes migrations as an explicit deployment step, which does not match the implementation.

**Suggested fix:** Run migrations once in a deployment/release job, then start API replicas with a least-privilege runtime database role. If automatic migration is retained for the single-container MVP, document that limitation and prevent multiple instances from migrating concurrently.

**Resolution:** Added a one-shot Compose migration job and a `--migrate-only` API mode; regular API startup no longer applies migrations. Development seeding remains separate.

### 6. Admin catalogue silently stops at 50 records — 🟡 MEDIUM

**Evidence:** [searchAdminSchools](frontend/src/lib/api.ts#L81) always requests `pageSize: 50`; [admin/schools/page.tsx](frontend/src/app/admin/schools/page.tsx#L40) calls it without a page control.

**Why it matters:** The API supports pagination, but the admin interface can only see and manage the first 50 alphabetically sorted schools. Later records become inaccessible from the UI.

**Suggested fix:** Add admin pagination and pass the current page to the query, or use a server-side searchable table that can navigate all pages.

**Resolution:** Added page navigation to the Admin catalogue.

### 7. Public school pages are client-data-driven, limiting discovery SEO — 🟡 MEDIUM

**Evidence:** The homepage, catalogue, and detail route initially fetched school content only after hydration. The catalogue and detail routes are [server-rendered](frontend/src/app/schools/page.tsx) and [slug-based](<frontend/src/app/schools/[slug]/page.tsx>); homepage and catalogue initial results are now fetched on the server.

**Why it matters:** School names and descriptions are not in the initial server-rendered page, and the UUID detail route has only generic layout metadata. Search engines and social previews may not get useful per-school content, which is a material acquisition limitation for a school-discovery product.

**Suggested fix:** Render public school details and initial catalogue results on the server, generate school-specific metadata, and use a stable slug-based canonical route. Keep React Query for interactive filtering if useful.

**Resolution:** Added server-side initial results for the homepage and catalogue, server-rendered school details, per-school metadata, and canonical slug URLs. React Query remains for client-side interactions.

### 8. Critical workflows lack integration tests — 🟡 MEDIUM

**Evidence:** The only test project is [SchoolWise.Application.Tests](tests/SchoolWise.Application.Tests/). Its three test files cover validators and a small set of entity behaviors; there are no API, infrastructure, PostgreSQL, or browser integration tests.

**Why it matters:** Unit tests do not verify JWT/refresh rotation through HTTP, role-policy enforcement, approved-versus-draft visibility, EF query translation, or applying migrations to a clean database. The review environment also could not run Docker/PostgreSQL, so those paths remain unverified.

**Suggested fix:** Add focused integration tests using a real PostgreSQL service (for example, a CI service container or Testcontainers) for migrations, auth/session rotation, school filtering, and Admin authorization. Add a small frontend smoke test for draft editing and pagination.

**Resolution:** Added an opt-in Testcontainers/PostgreSQL API integration suite covering fresh migrations, registration conflicts, public draft visibility, and authenticated Admin draft access. It is build-verified but skipped unless Docker is available and `RUN_POSTGRES_INTEGRATION_TESTS=1` is set.

### 9. Unused authorization/UI scaffolding adds maintenance surface — ⚪ LOW

**Evidence:** The `SchoolAdminOrAdmin` policy in [Program.cs](src/SchoolWise.Api/Program.cs) had no current endpoint use, and `frontend/src/components/ui/select.tsx` had no imports elsewhere.

**Why it matters:** Dead policy and UI code add maintenance surface without current behavior. The Unit of Work abstraction is retained because it was an explicit architecture requirement.

**Suggested fix:** Remove or defer the unused policy and Select component. No direct package was confirmed unused: Zod and `@base-ui/react` are used elsewhere.

**Resolution:** Removed the unused `SchoolAdminOrAdmin` policy and unreferenced Select component. Kept the Unit of Work abstraction as required.

## Coverage by review area

- **Architecture:** Clear Domain/Application/Infrastructure/API separation and feature slices. The requested repository/UoW/CQRS stack is heavier than a minimal CRUD MVP, but the clearest unnecessary pieces are called out in finding 10.
- **Security:** The Compose exposure/default-credential risk is addressed in finding 2. The security pass found no additional actionable vulnerability in API authorization, JWT/refresh handling, EF query construction, frontend token/session handling, or dependencies. Runtime support is addressed in finding 1.
- **Database design:** Useful uniqueness, range constraints, and search indexes are present. Migration execution belongs in deployment orchestration (finding 5). School photo mapping already orders the response by `SortOrder`; no change was needed.
- **Authentication:** Registration/login/refresh are separated and refresh tokens are stored hashed. Concurrent duplicate registration lacks conflict translation (finding 4). Flow-level integration tests are missing (finding 8).
- **API design:** Versioned routes, validation, pagination, and ProblemDetails are in place. Admin draft details are inaccessible (finding 3); concurrent uniqueness failures are misreported (finding 4).
- **Frontend design:** Core pages and query state are present. Admin coverage and SEO shortcomings are listed in findings 6 and 7.
- **Maintainability:** No confirmed unused package was found. The unreferenced UI component and authorization policy were removed (finding 9).
- **Startup MVP suitability:** The solution is a credible local Phase 1 foundation, but runtime support, database exposure, draft-edit behavior, catalog pagination, integration-test coverage, and migration orchestration should be addressed before public production use.

## Validation notes

During remediation, `SchoolMappingProfile` was found to already sort photos by `SortOrder`; the photo-order concern from the initial summary was a false positive and was not retained as a finding.

Post-remediation validation: .NET 10 Release publish and build pass; 11 backend unit tests pass; the two PostgreSQL integration tests compile and are skipped by default; frontend lint and production build pass; SSR smoke requests for homepage, catalogue, and slug details returned HTTP 200 with mocked API data; EF reports no pending model changes and generates the expected PostgreSQL SQL; Compose YAML parses; NuGet and npm vulnerability audits report no vulnerable packages. Docker/PostgreSQL are unavailable here, so running the integration tests, applying the migration in a live database, and starting the Compose stack remain unverified.
