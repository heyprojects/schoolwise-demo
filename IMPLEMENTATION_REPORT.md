# SchoolWise: first usable review platform

## Delivery status

Implemented and verified locally against an isolated Neon test branch. See the release progress below for production deployment status.

### Release progress: 2026-11-23

- Revalidated backend build (zero warnings/errors), 18 application tests, 14 isolated PostgreSQL integration tests, frontend lint and a production-configured frontend build.
- Created production recovery branch `backup-before-reviews-20261123` (`br-dark-cloud-bggtwoqh`) before migration. It has no compute and expires **2026-11-30 at 20:00 UTC**. This is a temporary recovery point, not a long-term backup or a tested disaster-recovery procedure.
- Applied and verified `AddSchoolVerification` and `AddReviews` on production using the direct migration-role connection. No existing table/data was removed.
- Published release commit `47d94ed` to `main` on `heyprojects/schoolwise-demo`.
- Both [Backend CI](https://github.com/heyprojects/schoolwise-demo/actions/runs/45252148615) and [Frontend CI](https://github.com/heyprojects/schoolwise-demo/actions/runs/45252148699) completed successfully.
- [Vercel deployment](https://vercel.com/heyprojects-9865/schoolwise-demo/AD2hsFg4LSLHeFcouGDvnWLkcxzc) completed successfully. The new review API on Render and frontend routes were verified live.
- Live URLs: [SchoolWise](https://schoolwise-demo.vercel.app), [My Reviews](https://schoolwise-demo.vercel.app/my-reviews), [Admin moderation](https://schoolwise-demo.vercel.app/admin/reviews).
- Live smoke test passed: synthetic Parent registration/login/submission, pending review excluded from public ratings, approval through the authenticated Admin browser UI, anonymous filtered review response and exact score/count, then author deletion with score/count reset.
- The live smoke test used a clearly labeled fictional school, not the existing school. Its test review was withdrawn and the school was unapproved afterward; no synthetic listing/review remains public. The synthetic account, hidden school and withdrawn review audit remain in the database by design.
- Final live checks: API readiness, homepage, directory, My Reviews and Admin reviews all returned HTTP 200; the existing school's detail UI displayed zero Published reviews and no score, unchanged by testing. The public directory again contained only its original listing. Development sample reviews were not seeded in production.

The sprint prioritizes Parent registration, school discovery, review submission, Admin moderation and anonymous review consumption. Existing verification-code work from the earlier sprint remains in the working tree but was not expanded; it is not a prerequisite for reviews. No verified-parent badge or relationship guarantee is offered.

## Delivered

| Area | Implementation |
|---|---|
| Reviews | School/User identity, six required 1-5 ratings, optional Pros/Cons, required ReviewText, PendingModeration/Published/Rejected |
| Parent ownership | One review identity per Parent/school; own edits and deletion; private version history |
| Moderation | Admin pending queue, exact-version approval/rejection, parent-visible moderation notes; rejection requires a reason |
| Ratings | Published/non-deleted reviews only; overall and five category means, review count and 1-5 distribution |
| School details | Rating summary, statistics, paginated review list, exact overall-rating filter and newest/highest/lowest sorting |
| Parent dashboard | My Reviews, status filter, editing, deletion, moderation reason and version history |
| Test data | Clearly labeled fictional samples on five clearly named development-only Demo schools |
| Catalog bootstrap | Manual onboarding, import model, aligned CSV examples, required/optional fields, 100/1,000-school processes and future claiming strategy |

### Lifecycle and data integrity

- New submissions are PendingModeration.
- As explicitly agreed, editing a Published review immediately unpublishes it and removes its contribution to ratings until the new version is approved.
- Content snapshots are retained for every submitted version. Moderation decisions record their actor and timestamp.
- Deletion is a withdrawal, not physical erasure: the public review disappears and private history remains. Resubmission after deletion reuses the same review identity with a new version.
- Database uniqueness enforces one identity per Parent/school. Version/status concurrency checks prevent stale edits, deletions and moderation decisions; conflicts return 409.
- Review and version changes are saved atomically. School deletion is blocked when review history exists; unapprove the school instead.
- Public projections omit account identities, emails, moderation notes and history. History is available only to the author or Admin.
- Scores are arithmetic means of each rating field, not weighted scores or averages of category means. Empty scores are null; the UI displays one decimal place. Count, category means and distribution are calculated in one SQL aggregate.

## Implementation map

- Domain: [Review](src/SchoolWise.Domain/Entities/Review.cs), [ReviewContent](src/SchoolWise.Domain/Entities/ReviewContent.cs), [ReviewVersion](src/SchoolWise.Domain/Entities/ReviewVersion.cs), [ReviewStatus](src/SchoolWise.Domain/Enums/ReviewStatus.cs).
- Application: [review commands](src/SchoolWise.Application/Reviews/ReviewCommands.cs), [queries](src/SchoolWise.Application/Reviews/ReviewQueries.cs), DTOs, validation and repository contract.
- Persistence: [ReviewRepository](src/SchoolWise.Infrastructure/Persistence/Repositories/ReviewRepository.cs), EF configurations, DbContext/DI registration and [AddReviews migration](src/SchoolWise.Infrastructure/Persistence/Migrations/20261123152420_AddReviews.cs).
- API: [ReviewsController](src/SchoolWise.Api/Controllers/ReviewsController.cs), authorization, review-write limiter and explicit uniqueness/concurrency/FK error mappings.
- Frontend: [school review section](frontend/src/components/reviews/school-reviews.tsx), reusable form/body/history components, [My Reviews](frontend/src/app/my-reviews/page.tsx), [Admin moderation](frontend/src/app/admin/reviews/page.tsx), typed API client and updated navigation.
- Seeding: [DatabaseSeeder](src/SchoolWise.Infrastructure/Persistence/DatabaseSeeder.cs), Development only.
- Focused tests: [ReviewTests](tests/SchoolWise.Application.Tests/ReviewTests.cs) and [ReviewApiTests](tests/SchoolWise.Integration.Tests/ReviewApiTests.cs).

The final browser check also uncovered a school-search blocker: server-provided initial results were being assigned to every new filter query and treated as fresh. [SchoolsBrowser](frontend/src/app/schools/schools-browser.tsx) now seeds only the initial filter query, so changing a filter issues a new API request immediately. This directly supports the required school-discovery journey.

## API and screens

Full contracts, validation, response envelopes and error behavior are documented in [ApiEndpoints](docs/ApiEndpoints.md).

| Endpoint, relative to `/api/v1` | Access |
|---|---|
| `POST /reviews` | Parent |
| `PUT /reviews/{id}` | Owner Parent; expected version |
| `DELETE /reviews/{id}?version=N` | Owner Parent |
| `GET /reviews/mine` | Parent; optional school/status, pagination |
| `GET /reviews/{id}/history` | Owner or Admin; pagination |
| `GET /reviews/moderation` | Admin; status/pagination |
| `POST /reviews/{id}/moderation` | Admin; expected pending version |
| `GET /schools/{id}/reviews` | Anonymous; rating/sort/pagination |
| `GET /schools/{id}/ratings` | Anonymous |

Screens: `/schools`, `/schools/{slug}`, `/my-reviews`, `/admin/reviews`, plus existing `/register` and `/login`.

## Validation evidence

### Automated checks

| Check | Result |
|---|---|
| `dotnet build SchoolWise.sln --no-restore` | Passed: 0 warnings, 0 errors |
| `dotnet test SchoolWise.sln --no-restore`, isolated external PostgreSQL enabled | 18 application tests and 14 integration tests passed; no skips |
| EF pending-model-change check | No pending model changes |
| Frontend `npm run lint` | Passed, including the final school-filter fix |
| Frontend `npm run build` | Passed, including type checking and the final school-filter fix |
| Catalog CSV parsing | Both examples match all 23 header columns; date/provenance fields aligned |
| `git diff --check` | Passed |

Integration coverage includes ownership/roles, validation, duplicate submissions, moderation and version conflicts, edit/reject/delete/resubmit history, published-only calculations, exact distribution/category results, filters/sorting and school-deletion protection. Earlier verification tests still pass.

Schema changes and integration writes used an isolated Neon branch, not production. Runtime seeding and browser writes also succeeded through the scoped application role.

### Browser journey

Verified using the production Next.js build against the isolated local API:

1. Registered a new synthetic Parent and logged in.
2. Searched for SchoolWise Demo schools; confirmed an immediate filtered request to the isolated API.
3. Opened school details and submitted all six ratings plus review text.
4. Confirmed PendingModeration/version 1 in My Reviews.
5. Signed out: the submission was absent publicly and the school still had four Published reviews.
6. Signed in as synthetic Admin, approved that exact submission with a note.
7. Signed out: the review appeared publicly, count increased to five and overall score increased from 4.0 to 4.2 for the new 5-star review.
8. Verified rating filtering and highest-rating sort; the moderation note was not public.
9. Signed in as the author, edited the review and confirmed PendingModeration/version 2 with the original content retained in history.
10. Signed out: count returned to four and edited pending content was absent.
11. Admin rejected version 2 with a reason; the Parent dashboard displayed Rejected and the reason.
12. Parent confirmed deletion; the review disappeared from My Reviews. History retention after deletion is covered by integration tests.

### Seed verification

Verified five Demo schools with four Published samples each, plus five PendingModeration and five Rejected samples: **30 fictional reviews total**. Each sample text starts with `[SAMPLE - fictional test review]`. Samples use synthetic accounts and are not attached to real-school listings. Seeding checks existing Parent/school identities and does not resurrect deleted samples. Restart idempotence was not separately exercised in the final browser check.

## Documentation

- [SchoolCatalogBootstrap](docs/SchoolCatalogBootstrap.md): requested strategy and CSV contract. It is documentation only; no importer or claiming feature was added.
- [DatabaseSchema](docs/DatabaseSchema.md): implemented review tables, constraints and lifecycle.
- [ApiEndpoints](docs/ApiEndpoints.md): implemented HTTP contracts.
- [README](README.md), [LocalSetup](docs/deployment/LocalSetup.md) and [Phase2-MVP-Cut](docs/Phase2-MVP-Cut.md): current sprint usage and changed priorities.

## Deployment handoff

1. Review the working tree before committing: it includes the earlier verification implementation and pre-existing deployment-document edits as well as this sprint.
2. Back up production and verify the recovery procedure.
3. Apply EF migrations with a **direct migration-role connection**, never the pooled runtime connection. The current migration chain includes the earlier AddSchoolVerification migration followed by AddReviews.
4. Confirm the scoped runtime role can access the new tables; do not use owner credentials in the running API.
5. Deploy the API first, then the frontend. Keep Production mode enabled so fictional development samples are not seeded.
6. Rebuild the frontend with the intended `NEXT_PUBLIC_API_URL`; point `INTERNAL_API_URL` at the same deployed API base and verify CORS/login/refresh.
7. Repeat registration, submission, approval and anonymous rating/review checks on the deployment with controlled test content, then withdraw that content.

The initial local `.next` output was built for the isolated preview; it was subsequently rebuilt with the production API URL during release validation. It is not a hosting deployment artifact. Temporary preview processes were stopped after local verification. Production-linked environment files were not changed; production schema changes are recorded in the release progress above.

## Boundaries and real-parent launch gates

Not implemented: school dashboards, claiming, advanced verification, fraud engines, voting, notifications or AI. Verification work was not expanded.

Before inviting real parents, finalize privacy/review policies and moderator procedures; implement operational handling for published-content complaints/removal and data-erasure requests; agree retention for version history and backups; test backup restoration; and use hosting terms/capacity suitable for the service. Soft deletion currently retains history and is not a complete GDPR-erasure operation. Current request limits are in-memory/single-instance. Email confirmation and moderator MFA are not provided by this sprint.

Moderation notes are deliberately author-visible, not confidential staff notes. Moderation does not establish factual accuracy or a verified relationship with the school.

## Athens catalog bootstrap

Ten private kindergartens within the Municipality of Athens were researched and
published through the existing Admin API. Each description records its Ministry
school code, public sources, check date and any conflicting contact/address
information. Unconfirmed tuition, capacity and photos were left empty. No Google
review text, rating or count was imported; these schools initially have zero
SchoolWise reviews. The original listing was left unchanged.

The first Greek-slug listings exposed double encoding of Next route parameters
in the server-side API lookup. Normalize the slug before encoding the outgoing
API path; do not change existing school slugs. Verified ASCII, raw Greek and
encoded Greek lookups, missing-school handling and upstream error propagation.
Frontend lint, type checking and production build passed. The repaired local
production build rendered all ten school detail pages and the directory using
the public production API.
