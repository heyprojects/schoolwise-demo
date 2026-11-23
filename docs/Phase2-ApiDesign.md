# SchoolWise Phase 2 API Design

**Status:** Contract proposal only; endpoints are not implemented.  
**Base path:** Existing `/api/v1`; preserve Phase 1 conventions: JSON, UUIDs, UTC timestamps, ProblemDetails, role authorization, and cursor/offset pagination as appropriate.

## 1. API rules

- Registration continues to create only a Parent. `Moderator` is assigned by an existing Admin through a controlled administrative process, never by client-supplied role.
- A verification decision is not a review decision. Keep independent endpoints, permissions, audit records, and queue service levels.
- Public reads return only approved published versions and aggregate fields. Never return user IDs, verification documents/methods, moderation notes, or reporter identity.
- Parent commands are owner-scoped. The API derives the actor from the access token; clients cannot choose `userId`, status, moderator, verified flag, or blob key.
- Use idempotency keys for request creation/upload completion and moderation decisions. Use optimistic concurrency on review edits and moderation.
- Enforce per-account, per-IP, and per-school rate limits. Return stable `409` codes for duplicate active reviews/pending verification and stale writes.
- Bound all text and file sizes. Validate URLs/content types only where accepted; review text is plain text, never trusted HTML.
- All mutations and decisions generate append-only audit records in the same transaction as the state change, with outbox work committed atomically.

## 2. Public endpoints

| Verb | Path | Access | Purpose and response |
|---|---|---|---|
| GET | `/schools/{schoolId}/rating-summary` | Anonymous | Published verified count, total published count, score visibility flag, overall mean, category means/counts, and last-updated time. The score is withheld below the configured five-review threshold. |
| GET | `/schools/{schoolId}/reviews` | Anonymous | Cursor-paged published reviews, newest first; returns approved version content, ratings, publication date, and “Verified parent at time of review.” No author identity. |
| GET | `/reviews/{reviewId}` | Anonymous | Public detail only if an approved published version exists and the review is not archived/removed; otherwise `404`. |
| POST | `/reviews/{reviewId}/reports` | Authenticated Parent | Report category and bounded explanation; returns a report receipt ID and `Open` status. Does not disclose moderation progress or reporter to the author. School representatives use the documented support/legal contact during the pilot. |

Review-list query parameters: `cursor`, `pageSize` (default 10, maximum 50), and an allowlisted `sort` (initially `recent`). Do not expose sorting by “helpful” until voting is implemented. A school with no public score still returns an explicit withheld/not-enough-reviews state rather than a misleading `0`.

## 3. Parent review endpoints

| Verb | Path | Access | Purpose and rules |
|---|---|---|---|
| POST | `/schools/{schoolId}/reviews` | Verified Parent | Create a Draft for an approved school. Enforce one active review per parent/school. Return review ID and current version number. |
| GET | `/me/reviews` | Authenticated Parent | List only the caller's reviews and statuses. Cursor-paged. |
| GET | `/reviews/{reviewId}/versions/current` | Owner Parent | Return the caller's current editable version and validation state; never expose another parent's draft. |
| PUT | `/reviews/{reviewId}` | Owner Parent | Replace an editable Draft/Rejected version by creating a new immutable version. Editing a Published review creates a new pending version and preserves the old public version until moderation completes. |
| POST | `/reviews/{reviewId}/submit` | Owner Parent | Submit the current version for moderation. Requires a valid parent-school grant for first publication, accepted review rules, and a valid current version. Idempotent for the same version. |
| DELETE | `/reviews/{reviewId}` | Owner Parent | Archive and immediately unpublish. This is a soft workflow action, followed by retention/erasure rules; return `204`. |

Review body fields: `overallRating` (required integer 1–5), the five category ratings (nullable integer 1–5), `pros`, `cons`, `reviewText`, and allowlisted `locale`. Category ratings may be omitted when the parent cannot fairly assess that dimension. Prohibit personally identifying information about children, parents, or staff in the UI and moderation policy.

## 4. Parent verification endpoints

| Verb | Path | Access | Purpose and rules |
|---|---|---|---|
| POST | `/schools/{schoolId}/verification-requests` | Authenticated Parent | Create a Pending request for an approved school and an allowlisted method. Enforce one pending request per user/school. |
| GET | `/me/verifications` | Authenticated Parent | List the caller's current school grants and historical request status/reason. Never return internal fraud signals or another user's data. |
| GET | `/verification-requests/{requestId}` | Owner Parent | Return status, submission/review timestamps, expiry, and safe user-facing rejection reason. |
| POST | `/verification-requests/{requestId}/upload-sessions` | Owner Parent | Create a short-lived write-only user-delegation SAS for one generated blob key. Request contains declared size/type only as hints; the server enforces policy and never trusts client claims. |
| POST | `/verification-requests/{requestId}/documents/complete` | Owner Parent | Confirm upload completion. Server checks blob ownership, actual size, detected type, digest, and scan state. Returns `PendingScan` or `PendingModeration`; it does not mark the request approved. |
| DELETE | `/verification-requests/{requestId}/documents/{documentId}` | Owner Parent | Delete/replace evidence before the moderator decision, subject to request state; audit the action. |
| POST | `/verification-requests/{requestId}/cancel` | Owner Parent | Cancel a Pending request and schedule document deletion. |

The SAS is a bearer credential: do not log it, persist it, return it in analytics, or allow it into error messages. Return only the minimum required URL/headers to the authorized browser; keep it short-lived and scoped to one blob and write/create operations. A completed upload remains private until scan state is `Clean`.

For later partner-school methods, add a dedicated code redemption or invitation acceptance endpoint. Never overload the document-upload route or accept an arbitrary `verified=true` flag.

## 5. Moderator endpoints

All routes below require the `Moderator` role (or a narrowly controlled Admin override), recent authentication for evidence access, and audit logging. `SchoolAdmin` has no moderation or evidence access.

| Verb | Path | Access | Purpose and response |
|---|---|---|---|
| GET | `/moderation/reviews` | Moderator | Pending review-version queue with cursor, submission time, school, risk priority, and conflict flags. Default response hides the author's identity. |
| GET | `/moderation/review-versions/{versionId}` | Moderator | Retrieve the exact version under review, prior moderation outcome, and minimal relevant context. |
| POST | `/moderation/review-versions/{versionId}/decision` | Moderator | Approve or reject a specific version with controlled reason code and user-facing explanation. Atomic with version/review/public pointer update. |
| POST | `/moderation/reviews/{reviewId}/removal` | Moderator | Remove an existing public version for policy violation, safety, privacy, or legal request; requires reason code and notifies the author. |
| POST | `/moderation/reviews/{reviewId}/restoration` | Senior Moderator/Admin override | Restore a previously removed approved version after a successful appeal or corrected decision; requires a new reasoned decision and audit entry. |
| GET | `/moderation/verifications` | Moderator | Cursor-paged Pending requests; prioritize oldest and risk/escalation needs. Evidence status is shown; no direct blob URL. |
| GET | `/moderation/verification-requests/{requestId}` | Moderator | View request metadata and safe evidence metadata. Parent identity is shown only when needed to compare proof; never expose child details beyond the minimum. |
| POST | `/moderation/verification-requests/{requestId}/decision` | Moderator | Approve or reject with controlled reason; on approval create/update the current parent-school grant and expiry atomically. |
| POST | `/moderation/verification-documents/{documentId}/access` | Moderator | Authorize and audit a short-lived read path for one document. Re-check role, case state, scan state, and purpose; no list/download endpoint for arbitrary blob keys. |
| GET | `/moderation/reports` | Moderator | Cursor-paged open reports with category and age; reporter identity hidden unless necessary for a documented abuse investigation. |
| POST | `/moderation/reports/{reportId}/decision` | Moderator | Dismiss, action, or escalate a report; if content changes, create the corresponding review decision as well. |

## 6. Privacy-rights endpoints

These are account-scoped and must invoke the documented retention/erasure workflow, not bypass moderation/legal holds:

| Verb | Path | Access | Purpose and response |
|---|---|---|---|
| POST | `/me/privacy-requests` | Authenticated user | Request access, rectification, restriction, or erasure; returns a request ID and receipt. Erasure immediately unpublishes requested review content while the remaining work is assessed. |
| GET | `/me/privacy-requests/{requestId}` | Request owner | Return status, next step, and completion/retention explanation without internal notes. |
| GET | `/me/privacy-requests/{requestId}/export` | Request owner | Download a completed access export through a short-lived authenticated path; do not include third-party data or internal fraud signals. |

Decision request fields: `outcome`, `reasonCode`, `userFacingReason`, optional bounded `internalNote`, and expected version/concurrency token. The moderator cannot submit a decision against a version they authored or a request they personally verified. Do not permit a user-facing reason to contain internal risk signals, another user's information, or raw evidence details.

## 7. Responses and errors

Continue the existing collection envelope for compatibility, but use stable cursors for growing moderation/review lists. Public review DTO contains only:

- review ID and school ID;
- published version ratings and text;
- published date/locale;
- a fixed verified-parent indicator and its precise “at time of review” meaning.

It does not contain parent/user ID, email, verification method, request ID, document ID, staff notes, risk score, or raw moderation decision.

Use RFC 7807 ProblemDetails with stable extensions/codes:

| HTTP | Example code | Meaning |
|---|---|---|
| 400 | `validation_failed` | Invalid rating, text, cursor, or upload declaration |
| 401 / 403 | standard authentication/authorization | Missing identity or role/ownership failure |
| 404 | `not_found` | Resource absent or intentionally concealed from a non-owner |
| 409 | `active_review_exists`, `verification_pending`, `version_changed`, `invalid_transition` | State/uniqueness/concurrency conflict |
| 413 | `document_too_large` | Upload exceeds configured maximum |
| 422 | `verification_required`, `scan_not_clean` | Valid request shape but unmet trust prerequisite |
| 429 | `rate_limited` | Rate limit reached; include `Retry-After` |

Do not return `404` for malware or moderation findings if it would disclose sensitive case state to an unauthorized caller; scope responses by ownership.

## 8. Rate limits and idempotency

- Limit login/account/email challenge and report endpoints independently.
- Limit verification request creation per account/school and bound outstanding uploads.
- Rate-limit code redemption by account, IP, and credential without revealing whether a code exists.
- Require idempotency keys for request creation, upload completion, and moderator decisions. Store the key with actor/operation/result and a bounded expiry.
- Apply per-user review edit/submit limits and a cooldown after repeated rejected submissions; provide an appeal path and do not silently shadow-ban.
- Do not use IP address as identity proof. Minimize retention of IP/device signals and keep them out of public and routine moderator responses.

## 9. Versioning and rollout

These are additions under `/api/v1`; no incompatible Phase 1 school/auth contract changes are needed. Add schema/API version compatibility tests before deployment. If later adding helpfulness votes, school-admin tools, or automated translation, add explicit endpoints and authorization review rather than expanding current DTOs implicitly.

