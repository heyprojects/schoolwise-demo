# SchoolWise Reviews MVP Implementation Report

Date: 2026-11-23 UTC  
Status: **Implemented, tested, deployed and verified**

Application: [SchoolWise](https://schoolwise-demo.vercel.app)  
API: [API readiness](https://schoolwise-demo.onrender.com/ready)

## 1. Delivered scope

The existing moderated-review platform was extended rather than rebuilt. No additional verification, claiming, school dashboards, voting, notifications, fraud engine or AI features were introduced.

| Requirement | Delivered behavior |
|---|---|
| Create Review | An active Parent can submit a complete review or save an incomplete, private Draft for an approved school. |
| Edit Review | Only its author can save a new version. Saving changes immediately withdraws any previously published review and its rating contribution. |
| Delete Review | Owner-only, version-checked soft deletion; public content/counts disappear and history is retained. |
| Moderation | Admin queue, approve, reject and author-visible notes. Only the exact current PendingModeration version can be moderated. Rejection requires a reason. |
| School Ratings | Review count, overall average, five category averages and rating distribution calculated from Published, non-deleted reviews of approved schools only. |
| Parent Dashboard | My Reviews, all four status filters, Draft continuation, editing, deletion and version history. |
| Public Review Listing | Published-only reviews, overall-rating filter, newest/highest/lowest sorting and pagination on school profiles. |
| One review per Parent/school | Existing unique database identity also covers Drafts and deleted reviews. Recreation reuses the same review ID with a higher version. |
| Version history | Each save/submission creates a content snapshot. Moderation and withdrawal metadata are retained. |
| Test samples | Development-only, explicitly fictional samples for all four statuses; no sample seeding on the 100 real production schools. |

The homepage now advertises available moderated reviews instead of saying that reviews are unavailable or implying independently verified parent relationships.

## 2. Draft lifecycle and contract

Supported statuses: **Draft, PendingModeration, Published, Rejected**.

Create and edit accept optional `saveAsDraft`, defaulting to `false` for existing clients:

- `true`: save Draft, allowing incomplete content.
- Omitted or `false`: validate complete content and enter PendingModeration.
- Clients cannot directly choose Published or Rejected.

Submission requires all six integer ratings from 1 through 5 and at least 20 trimmed characters of review text. Review text remains limited to 5,000 input characters; optional Pros/Cons to 1,000 each.

Drafts retain the existing nonnullable content fields:

- Rating **0 means not selected**, not a zero-star rating. Selected Draft ratings remain 1-5.
- Missing review text is `""`; short text is allowed.
- Negative ratings, ratings above 5, null text and excessive text lengths remain invalid.
- The UI displays missing ratings as "Not selected", never "0 / 5".
- "Save draft" bypasses full-submission browser requirements, but runs Draft validation. "Submit review" retains full validation.

An invalid submission does not replace the Draft or append a version. Saving a Published review as Draft immediately removes it from public listings and ratings; complete resubmission still requires Admin approval.

### Privacy policy enforced by the implementation

Draft content and Draft snapshots are author-only:

- Parent lists include only that Parent's reviews.
- Admin-managed queries exclude Drafts before counting/pagination, including unfiltered queries.
- Asking for `status=Draft` on the Admin moderation endpoint returns `400`.
- Admin history access to a currently Draft review returns `404`, including a deleted current Draft.
- Once the current review is submitted, Admin history contains submitted snapshots only; all historical Draft snapshots remain excluded before counting/pagination.
- Owners retain all their versions, including withdrawn/deleted Drafts.
- Public responses never include author/account IDs, emails, notes or private history.

Original version numbers remain intact; filtered Admin history may therefore have gaps. The existing `submittedAt` history field represents save time for Draft snapshots and is displayed as "Saved".

Full contracts: [API endpoints](docs/ApiEndpoints.md).  
Persistence details: [database schema](docs/DatabaseSchema.md).

## 3. Persistence and migration

Generated EF Core migration:

`20261123210550_AddReviewDrafts`

The migration updates five existing check constraints across `reviews` and `review_versions`:

1. Draft is an allowed status in both tables.
2. Ratings 0-5 are allowed only for Draft; all submitted statuses retain 1-5 checks.
3. Draft/PendingModeration snapshots have no decision actor/time; Published/Rejected snapshots require them.

No columns, school records or existing review content were rewritten. Existing enum values, unique identities, foreign keys, indexes and optimistic concurrency checks were preserved.

### Isolation and production protection

- Migration/testing branch: `dev-review-drafts`, `br-misty-sun-bgfmiom6`, cloned from production; expires **2026-11-25 18:00 UTC**.
- Current recovery branch created before the production migration: `backup-before-review-drafts-20261123`, `br-sweet-bonus-bgi3oitt`; no compute; expires **2026-11-30 23:59:59 UTC**.
- EF migrations used direct owner connections captured only into process environment. Existing scoped runtime database access was not changed.
- Migration was applied to the isolated production-like branch first. Its original school checksum and existing review/version counts were unchanged.
- Integration tests and Development seeding ran only against the isolated branch, never production.
- The same migration was then applied to production before API deployment.

The recovery branch is temporary protection, **not a tested permanent backup/restore program**. The generated Down migration cannot reinstate the old checks while current or historical Draft rows exist. Do not deploy a Draft-unaware API or downgrade after accepting private Draft content without a reviewed recovery procedure.

## 4. Validation results

| Validation | Result |
|---|---|
| Focused review application tests | 5 passed |
| Focused review API integration tests | 10 passed |
| Complete Release application tests | 20 passed, 0 skipped |
| Complete Release PostgreSQL integration tests | 18 passed, 0 skipped |
| Complete suite | **38 passed, 0 failed, 0 skipped** |
| API Release publish | Passed |
| Frontend ESLint | Passed |
| Frontend production build/type checks | Passed |
| Isolated migration and original-data preservation | Passed |

Six focused tests were added for Draft validation/snapshots, lifecycle/privacy, withdrawal/deletion/history, concurrent save versus submission and actual database rating constraints. Existing registration, ownership, rejection, duplicate submission, stale moderation, published-only aggregates and public filtering tests also passed.

Validation commands used the existing tooling:

```powershell
dotnet test tests\SchoolWise.Application.Tests\SchoolWise.Application.Tests.csproj --filter FullyQualifiedName~ReviewTests --no-restore
dotnet test tests\SchoolWise.Integration.Tests\SchoolWise.Integration.Tests.csproj --filter FullyQualifiedName~ReviewApiTests --no-restore
dotnet test SchoolWise.sln --configuration Release --no-restore
dotnet publish src\SchoolWise.Api\SchoolWise.Api.csproj --configuration Release --no-restore
```

PostgreSQL test commands used `RUN_POSTGRES_INTEGRATION_TESTS=1` and `TEST_DATABASE_URL` targeting the isolated branch. In the frontend working directory, `npm.cmd run lint` and `npm.cmd run build` passed.

Backend CI now explicitly enables PostgreSQL integration tests instead of skipping them; hosted runs use the existing Testcontainers setup.

### Local browser acceptance

Using the isolated API and actual frontend:

- Registered/logged in a Parent.
- Saved a five-character Draft with missing category ratings.
- Reloaded and confirmed the Draft persisted.
- Filtered My Reviews to Draft and continued editing.
- Confirmed incomplete submission was blocked; completed all ratings/text and submitted.
- Verified owner history retained the Draft and displayed its save timestamp.
- Logged in as a synthetic Admin; verified no Draft filter or historical Draft content appeared.
- Approved the submitted version with a moderation note.
- Signed out and verified public review display, overall/category scores and rating filtering.
- Measured the Demo school's count from **4 to 5** and overall mean from **4.0 to 4.2** after approval; filtering to five-star reviews returned two rows without changing the whole-school summary.
- Saved the Published review as an incomplete Draft; count returned to **4**.
- Deleted it and confirmed all three private versions, including two Draft snapshots, remained accessible to the owner.

## 5. Development samples

Verified on the isolated Development database:

| Status | Sample reviews |
|---|---:|
| Published | 20 |
| PendingModeration | 5 |
| Rejected | 5 |
| Draft | 5 |
| Total | **35** |

Samples belong only to five explicitly named fictional `SchoolWise ... Demo` schools and synthetic sample accounts. Draft samples have missing ratings. Content is marked `[SAMPLE - fictional test review]`. Existing identity guards are preserved, including not recreating deleted sample reviews during seeding.

Production startup does not run Development sample seeding.

## 6. Deployment and live acceptance

Deployment was deliberately sequenced: **migration -> Draft-aware API -> frontend**. This avoids an older API silently ignoring the new save flag.

| Release | Commit | Evidence |
|---|---|---|
| API, migration, tests and contracts | `874778bee921c9a9695a02d95841eb49d122fedb` | [Backend CI passed](https://github.com/heyprojects/schoolwise-demo/actions/runs/45268066700); live API returned the new author-private Draft validation response. |
| Parent/Admin UI and homepage copy | `3751bc33c13347cb198266628802e0b96176f53d` | [Frontend CI passed](https://github.com/heyprojects/schoolwise-demo/actions/runs/45268713855); Vercel reported success and live homepage/Admin UI showed the new behavior. |

Live API readiness returned `200`.

### Controlled live smoke test

No fictional reviews were added to any real catalog school. A separately marked fictional school was temporarily approved for a bounded release test:

- School ID: `217d014f-9709-4c22-9339-e31787fb6ea5`.
- Review ID: `cb19a077-90fe-42a7-bc3b-7a562341ff17`.

A synthetic Parent used an isolated Node HTTP session; the existing real Admin browser session was preserved. Live results:

| Check | Verified outcome |
|---|---|
| Register/login | Parent account created; authentication succeeded. |
| Empty Draft | `201`, Draft version 1; visible in the owner's Draft filter. |
| Duplicate creation | `409`; one review identity. |
| Incomplete submission | `400`; Draft/history unchanged. |
| Complete submission | `200`, PendingModeration version 2. |
| Admin history/UI | Only submitted version 2 appeared; Draft version 1 was hidden. |
| Admin approval | Approved through the live moderation UI with notes. |
| Anonymous API / live public page | One published review; all six scores 5; UI showed `5.0 / 5`. Public row contained only `id`, `content`, `publishedAt`. |
| Published-to-Draft edit | Draft version 3; public count became 0 and score null. |
| Owner deletion/history | `204`; all three withdrawn snapshots retained. |
| Admin access after deletion | Current Draft history returned `404`. |
| Test cleanup | School unapproved; school/reviews/ratings public APIs returned `404`; synthetic Parent refresh session revoked. |

Final database and browser checks confirmed **100 published real schools** and no published sample reviews in that catalog. All pre-existing school records, including the earlier private release-test school, retained their original checksum. The new fictional school and withdrawn review history remain private audit records; history was not destroyed.

### Limits of live browser coverage

The browser host did not permit a separate isolated browser storage context. To avoid replacing the user's authenticated Admin session, the production Parent lifecycle was tested through isolated HTTP cookies, while production Admin moderation/public rendering and the complete local Parent/Admin/Visitor browser flow were verified separately. PowerShell script-file execution was also disabled; the smoke runner used the already available Node runtime without changing execution policy.

The temporary local services, synthetic session and smoke helper were cleaned up.

## 7. Operational boundaries

This is a deployed functional Reviews MVP, not certification of legal compliance, verified school relationships or fraud resistance:

- Reviews are moderated, not independently fact-checked or verified-parent reviews.
- Moderation notes are visible to the author, not confidential internal notes.
- Soft-deletion history requires an operational retention/erasure policy; it is not complete GDPR erasure.
- Existing hosting/plan restrictions, cold starts, review/privacy policies, complaints/removal ownership and tested long-term backups remain launch considerations described in the [README](README.md).
- The existing write limiter is per account and per API process.
- No dependencies or frameworks were added/upgraded for this implementation.

Previously created catalog exports/report and catalog-bootstrap edits were preserved and intentionally excluded from the Reviews implementation commits.
