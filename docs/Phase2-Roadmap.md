# SchoolWise Phase 2 Roadmap

**Status:** Recommended implementation sequence; design only. Phase 1 remains the current implemented product.

**Scope note:** For a one-developer launch in under eight weeks, use [Phase2-MVP-Cut.md](Phase2-MVP-Cut.md). This broader roadmap is the target-state design, not the MVP backlog.

## Phase 2 goal

Launch a small, trustworthy verified-review pilot that demonstrates the full chain:

**Parent-school proof → moderated version → transparent published score → useful school choice.**

Do not launch a public rating score before verification, moderation, deletion, reporting, and appeals work end to end.

## Workstreams and sequence

| Stage | Scope | Exit criteria |
|---|---|---|
| **0. Policy and launch readiness** | Choose pilot schools and eligibility window; review terms, privacy notice, verification instructions, prohibited content, retention, lawful bases, DPIA screening, DSA applicability, moderator staffing and escalation | Legal/privacy sign-off; named controller/contact; documented DPIA decision; trained review/verification moderators; deletion and incident runbooks |
| **1. Trust foundation** | Add Moderator role and least-privilege policies; verification request/grant lifecycle; private Blob quarantine; upload authorization, scanning, access audit, deletion lifecycle; account email confirmation | Parent cannot submit without a valid school grant; no public document access; unclean/failed scans stay quarantined; role/ownership tests pass; deletion job is observable |
| **2. Review and version lifecycle** | One active review per parent/school; draft/edit/submit; immutable versions; moderation decisions; published-version pointer; parent-owned list/delete/appeal | Concurrent duplicate requests fail safely; stale decisions cannot publish changed versions; edit/reject preserves prior published version; deletion unpublishes immediately |
| **3. Moderation and reporting operations** | Separate review and verification queues; reason codes; internal notes; public reports; appeals; notifications; DSA-ready notices/contact workflow as legally applicable | Queue SLAs visible; every action audited; two-person escalation for high-impact exceptions; sample decisions consistent across positive/negative reviews |
| **4. Public trust presentation** | School review list, verified badge language, score summary, minimum-cohort threshold, cache invalidation | Only approved verified reviews affect scores; aggregates match source versions; small-cohort score suppression and no-PII checks pass |
| **5. Controlled pilot** | Release to a limited cohort of schools and parents; daily queue review, weekly policy/false-positive review, incident and deletion drills | Pilot meets quality gates below for at least one full review cycle; no unresolved critical privacy/security finding; leadership approves expansion |

### Indicative delivery shape

For a small team (two backend-capable engineers, one frontend engineer, product/design support, privacy/legal review, and staffed human moderators), engineering is roughly **6–8 weeks**, excluding legal/procurement delays and school onboarding. A smaller team should reduce the pilot scope, not skip privacy controls or moderation. Azure region, security review, Defender for Storage cost/availability, and counsel approval are schedule dependencies.

## Pilot boundaries

- Begin with a limited set of schools representing public/private and multiple school types, but only where evidence can be processed responsibly.
- Use one verification method (manual document review) for the broad pilot; do not promise every Greek school is immediately supported.
- No review comments, school replies, helpfulness voting, AI review scoring, recommendation ranking, child accounts, or staff identity profiles.
- Publish no unverified rating count. Pending and rejected submissions remain private.
- Cap pilot submissions per account and staff queue capacity; pause intake rather than silently relaxing verification/moderation standards.

## Launch quality gates

### Trust and content

- 100% of public reviews have an approved version and an approved parent-school grant at submission time.
- Zero public DTOs/cache entries include account IDs, child names, verification method, evidence locators, moderation notes, or reporter identity.
- Published review and score aggregates reconcile with approved current versions after edits, removals, deletion, and retries.
- The minimum score cohort threshold is enforced on every public path, including SSR/cache/API.

### Security and privacy

- Threat-model and DPIA actions are closed or accepted by accountable owners.
- Private storage, access policy, SAS scope/expiry, scan failure handling, audit, retention, and deletion are tested.
- Moderators can access only assigned/scoped case evidence; access is logged and alertable.
- Account deletion and backup-restore suppression are exercised.

### Moderation operations

- Review and verification queues have separate ownership and audit.
- Targets: 90% of routine cases within two business days during pilot; urgent child-privacy/safety reports triaged within one business day or faster according to severity.
- Every rejection/removal has a reason and an appeal/contact path.
- Appeal overturns, queue aging, and school/language/rating disparities are reviewed weekly.

## Product success metrics

Measure trust and usefulness, not just review volume or a high average rating:

- share of eligible parents who complete verification;
- verification completion/drop-off and rejection appeal rate by method;
- moderation turnaround time and appeal overturn rate;
- report rate and confirmed privacy/fraud violations per 1,000 published reviews;
- percentage of reviews families find useful in post-task feedback;
- catalogue-to-school-detail engagement and review-section interaction; and
- score sample size/coverage across public and private school types.

Do not optimize the average score, rejection count, or number of documents collected.

## Expansion gates

1. Add school-issued one-time codes only after school-scoped administration, key lifecycle, and abuse/revocation controls are ready.
2. Add invitations only after school staff identity/authorization, audit, email matching, and parent notice are designed.
3. Add helpfulness voting only if research shows reviews are hard to navigate; evaluate brigading before public sorting.
4. Add machine assistance only for prioritization or user warnings, with validation, human decision, appeal, and DPIA review.
5. Revisit school-level rating thresholds only with privacy evidence and sufficient cohort diversity.

