# SchoolWise API Endpoints

Base path: `/api/v1`. JSON over HTTP. Swagger UI is available at `/swagger` in development. Authentication uses a JWT bearer access token and rotating refresh token.

## Authentication

| Verb | Path | Access | Purpose |
|---|---|---|---|
| POST | `/auth/register` | Anonymous | Register Parent account |
| POST | `/auth/login` | Anonymous | Authenticate and issue tokens |
| POST | `/auth/refresh` | Anonymous + HttpOnly refresh cookie | Rotate refresh token and issue access token |
| POST | `/auth/revoke` | Authenticated + refresh cookie | Revoke refresh token and clear cookie |
| GET | `/auth/me` | Authenticated | Return current profile and role |

Registration never accepts a role from the client; it always creates a Parent.

## School discovery

| Verb | Path | Access | Purpose |
|---|---|---|---|
| GET | `/schools` | Anonymous | Paged catalogue with filters/sort |
| GET | `/schools/{id:guid}` | Anonymous | Approved school detail and ordered photos |
| GET | `/schools/slug/{slug}` | Anonymous | Approved school detail for canonical public URLs |
| GET | `/schools/admin` | Admin | Paged list including unapproved schools |
| GET | `/schools/admin/{id:guid}` | Admin | School detail including unapproved records |
| POST | `/schools` | Admin | Create school |
| PUT | `/schools/{id:guid}` | Admin | Replace editable school fields |
| DELETE | `/schools/{id:guid}` | Admin | Delete school and its photos |

### `GET /schools` parameters

`page` (1-based, default 1), `pageSize` (default 12, max 100), `sortBy` (`name`, `city`, `tuitionFrom`, `createdAt`), `sortDirection` (`asc`/`desc`), `keyword`, `city`, `type`, `isPrivate`, `minTuition`, `maxTuition`.

Example: `/api/v1/schools?city=Athens&type=Kindergarten&isPrivate=true&minTuition=2000&page=1&pageSize=12`

### Response envelope

Successful collections return `{ "items": [...], "page": 1, "pageSize": 12, "totalCount": 42 }`. Errors use RFC 7807 ProblemDetails with stable extensions such as `code` and `errors`. `401` means unauthenticated, `403` forbidden, `404` not found, and `400` invalid query/body.

### School list item

`id`, `name`, `slug`, `schoolType`, `city`, `address`, `isPrivate`, `tuitionFrom`, `tuitionTo`, `logoUrl`.

### School detail

All school properties plus `photos: [{ id, url, sortOrder }]`, ordered by `sortOrder`.

### Create/update body

`name`, `description`, `schoolType`, `address`, `city`, `postalCode`, `phone`, `email`, `website`, `tuitionFrom`, `tuitionTo`, `studentCapacity`, `establishedYear`, `isPrivate`, `logoUrl`, `isApproved`, `photos`.

## Authentication examples

Login returns `accessToken`, `expiresAt`, and `user`. The refresh token is set only as an HttpOnly, SameSite=Lax cookie (Secure outside Development), scoped to `/api/v1/auth`; it is never returned in JSON. `POST /auth/refresh` takes no body, consumes that cookie, and rotates it using `Set-Cookie`. `POST /auth/revoke` requires a valid access token, revokes the cookie's token, and clears the cookie. Refresh tokens are stored as SHA-256 hashes; reuse of a revoked token is rejected.

`GET /auth/me` returns `{ id, firstName, lastName, email, role, isActive, createdAt }`.

## API conventions

- UUID IDs; ISO-8601 UTC timestamps.
- Invalid model/query input returns `400` ProblemDetails.
- List filters execute in SQL; sorting is allowlisted to avoid arbitrary expressions.
- Only approved records are returned by public list/detail queries.
- School write operations require the `Admin` role.
- API versioning is URL-segment based; a new incompatible contract uses `/api/v2`.

## Phase 2, first slice: school-issued verification codes

These endpoints implement the earlier demo eligibility foundation, not a launch-ready verified-review program. The current review sprint below is implemented independently and does not require these grants. Confirmed email, moderator MFA, public verification badges, legal/privacy operations and partner onboarding remain future verified-program gates. No document upload is implemented.

All paths below are relative to `/api/v1/verifications`; all require bearer authentication. The API also checks the actor's current active account and role in the database, not just the JWT role.

| Verb | Path | Access | Purpose |
|---|---|---|---|
| POST | `/codes` | Admin | Issue 1-50 independent single-use codes for an approved school |
| GET | `/codes?schoolId={id}&page=1` | Admin | Paged metadata; never returns plaintext codes or hashes |
| POST | `/codes/{id}/revoke` | Admin | Revoke with `{ "reason": "..." }`, 1-500 characters; invalidates the grant sourced from this code |
| POST | `/redeem` | Parent | Redeem `{ "schoolId": "...", "code": "..." }` for the signed-in Parent only |
| GET | `/me?page=1` | Parent | Own current grants with `Active`, `Expired` or `Revoked` status |

Issuance body:

```json
{
  "schoolId": "<approved-school-uuid>",
  "count": 10,
  "expiresAt": "<future ISO-8601 timestamp within 30 days>",
  "validUntil": "<ISO-8601 timestamp at or after expiresAt, within 12 months>"
}
```

`validUntil` is chosen by the Admin for the agreed school-year cutoff; the UI defaults to August 31. The API enforces the 12-month maximum, but cannot infer a school's agreed enrollment period. Do not select a date beyond the authorized school-year cutoff.

Success is `200` with `[{ "id": "...", "code": "..." }]`. Plaintext is returned only by issuance, with `Cache-Control: no-store`; no recovery endpoint exists. Distribute codes privately through the school, never through logs/chat or the public directory. Losing the response requires revoking the affected IDs and issuing replacements. Redemption returns `200 true`; it atomically consumes a code and creates/renews eligibility. An existing active grant rejects another code without consuming it. Renewals replace the current pointer while the original issuance/redemption/revocation audit remains on the source code rows.

Code matching ignores surrounding whitespace and case. Unknown, wrong-school, used, expired, revoked or otherwise unavailable codes all return `400` with `invalid_verification_code` and the same message. Invalid body shapes return validation ProblemDetails. Issuance is limited to 10 requests/10 minutes/Admin; redemption to 5 requests/10 minutes/Parent. Excess requests return `429` ProblemDetails and `Retry-After`. Limits are in-memory and reset on restart; multi-instance/pilot hardening is not implemented. Concurrency conflicts on revocation return `409` and require reloading/retrying.

Both collections use the existing paged envelope with fixed `pageSize=50`. Parent responses expose school, validity and status, never codes, other parents or issuer identity. Expiry/revocation checks are evaluated from current timestamps/source records rather than a background task. These grants alone must not enable public ratings or a verified-parent badge before the remaining Phase 2 gates.

Deleting a school with verification audit history returns `409 school_has_verifications`; unapprove it instead. This preserves issued/redeemed code evidence and existing grants.

## Review sprint (current product priority)

Reviews require an active **Parent** account, not a verification code. They are never represented as verified-parent reviews. Admins cannot submit reviews using an Admin account. The API checks the current active account/role in the database as well as endpoint authorization.

| Verb | Path relative to `/api/v1` | Access | Purpose |
|---|---|---|---|
| POST | `/reviews` | Parent | Create/submit a review; `201`, initially PendingModeration |
| PUT | `/reviews/{id}` | Owner Parent | Submit an edited version; `200`, returns to PendingModeration |
| DELETE | `/reviews/{id}?version=N` | Owner Parent | Immediately withdraw; `204`, preserves private history |
| GET | `/reviews/mine?status=Published&schoolId={id}&page=1` | Parent | Own current, non-deleted reviews; filters optional |
| GET | `/reviews/{id}/history?page=1` | Owner or Admin | Private content/version/decision history, including withdrawn versions |
| GET | `/reviews/moderation?status=PendingModeration&page=1` | Admin | Moderation queue; optional status Published/Rejected |
| POST | `/reviews/{id}/moderation` | Admin | Approve or reject exactly the displayed pending version |
| GET | `/schools/{id}/reviews?page=1&pageSize=10&rating=5&sort=newest` | Anonymous | Published-only list, optional exact overall rating |
| GET | `/schools/{id}/ratings` | Anonymous | Whole-school scores/count/distribution, independent of list filters |

Create body:

```json
{
  "schoolId": "<approved-school-uuid>",
  "content": {
    "overallRating": 4,
    "teachingRating": 4,
    "safetyRating": 5,
    "communicationRating": 4,
    "facilitiesRating": 3,
    "activitiesRating": 4,
    "pros": "Clear weekly classroom updates.",
    "cons": "Collection parking can be busy.",
    "reviewText": "The daily routine was clearly explained and questions received helpful answers."
  }
}
```

All six ratings are required integers 1-5. `pros`/`cons` are optional (max 1,000 characters); `reviewText` has 20-5,000 trimmed characters. Content is plain text and rendered escaped, never as HTML.

Edit uses `{ "version": 1, "content": { ...same fields... } }`. Moderation uses `{ "version": 1, "approve": true, "notes": "Optional note" }`; `approve=false` requires a nonblank parent-visible reason (max 1,000 characters). Notes are **not confidential internal notes**; the moderation UI states that the author can see them. The database retains the decision actor and time.

One review identity per Parent/school is enforced by a unique database constraint. Duplicate submission returns `409 review_exists`. After deletion, a fresh POST reuses that identity with a new version and preserves earlier snapshots. All edits temporarily remove any published content/score until the new version is approved. Stale edits/deletes/decisions or concurrent writes return `409`; reload before retrying. Other parents receive `404` for inaccessible IDs; anonymous writes receive `401`, wrong roles `403`. Review create/edit is limited to 20 requests per account per ten minutes (single-process limiter).

Public reviews return `id`, `content` and `publishedAt`, never author/account IDs, emails, notes or version history. Private managed rows additionally contain school ID/name/slug, version, status, updatedAt and the current moderation note. Private queues/history use the existing paged envelope with fixed page size 20.

Public `pageSize` is 1-50. Sort is `newest`, `highest` or `lowest`, with deterministic ID tie-breaking. School approval, Published status and absence of deletion are enforced in SQL. Public ratings return:

```json
{
  "reviewCount": 0,
  "overallScore": null,
  "teachingScore": null,
  "safetyScore": null,
  "communicationScore": null,
  "facilitiesScore": null,
  "activitiesScore": null,
  "distribution": [
    { "rating": 1, "count": 0 }, { "rating": 2, "count": 0 },
    { "rating": 3, "count": 0 }, { "rating": 4, "count": 0 },
    { "rating": 5, "count": 0 }
  ]
}
```

Each score is the arithmetic mean of that field over Published, non-deleted reviews, computed with the count/distribution in one SQL aggregate query. No verification weight or five-review suppression applies in this sprint. The UI rounds display to one decimal place. Review endpoints use `Cache-Control: no-store`; the browser invalidates affected query caches after writes.

Deleting a school with review history returns `409 school_has_reviews`; unapprove the school instead. Public ratings/reviews for unapproved schools return `404`. A Parent can still delete their review when a school is unavailable.
