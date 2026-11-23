# SchoolWise Phase 2 MVP Recommendation

**Decision memo for Product and Engineering leadership**  
**Status:** Recommendation only; no code or production policy has been implemented.

**Scope note:** The single-developer, eight-week launch cut is in [Phase2-MVP-Cut.md](Phase2-MVP-Cut.md) and supersedes this broader document for initial implementation scope.

## Executive decision

Build a **verified, manually moderated, versioned review pilot** inside the existing modular monolith. Do not build a generic trust platform.

The smallest trustworthy release needs:

1. One proof method: manual review of a narrowly scoped, redacted school document.
2. One parent-school verification grant with expiry and audit.
3. One active review per parent/school, immutable versions, and the five requested statuses.
4. A human review queue and clear rejection/appeal/removal reasons.
5. A public score based only on approved verified reviews, suppressed until at least five distinct reviewers.
6. Private, short-lived, scanned, audited document handling and tested deletion.
7. A clear policy, privacy notice, report route, and operational staffing.

This is deliberately a limited pilot, not an instant “all Greek schools” launch. A code-based method has lower data risk but depends on school operations and cannot provide broad coverage without partnerships. Manual documents cover more schools but create privacy and moderator burden. Launching with no one method that works for the selected cohort is less trustworthy than limiting coverage.

## Feature classification

| Feature | Classification | MVP decision |
|---|---|---|
| Parent confirms account email | **Must Have** | Baseline account-abuse control only; explicitly not proof of parenthood |
| Parent-school relationship verification | **Must Have** | One human-reviewed evidence path with a bounded validity period |
| Document upload and private Azure storage | **Must Have** for the selected document method | Quarantine, short SAS, malware scan, access audit, retention/deletion; no OCR |
| School-issued verification code | **Should Have** | Add for partner schools after scoped issuance and one-time/revocation controls; can reduce document retention |
| School-admin invitation | **Future Phase** | Requires school-scoped admin identity, invitation lifecycle, and recovery/abuse controls |
| School-email verification | **Future/Corroborating only** | Email verification by itself never grants parent status |
| Create/edit/delete own review | **Must Have** | Draft, submit, edit, archive; immutable versions |
| One active review per parent per school | **Must Have** | Database-enforced partial uniqueness; rejected and draft reviews still occupy the slot |
| Review history and audit | **Must Have** | Preserve versions/decisions for a defined period; erase or anonymize when retention basis ends |
| All five category ratings | **Must Have, simplified** | Overall required; category ratings nullable when unknown rather than forcing unreliable numbers |
| Human moderation before publication | **Must Have** | Every first review and material edit in the pilot |
| Parent appeal and report flow | **Must Have** | Small set of reason codes; human escalation; no school veto |
| Moderator role and queues | **Must Have** | Separate review and verification permissions; MFA and case-level evidence access |
| Public reviews and verified badge | **Must Have** | Pseudonymous public presentation; badge says verified at time of review and not endorsed |
| Rating aggregate | **Must Have, minimal** | Simple mean of published verified reviews only; no unverified weight or opaque confidence model |
| Minimum cohort threshold | **Must Have** | Hide numeric school/category score below five distinct verified reviewers |
| Review helpfulness votes | **Future Phase** | Not required for initial trust or core submission; can be brigaded |
| Public comments / school replies | **Future Phase** | Adds new moderation and retaliation surface before the review workflow is proven |
| Automated fraud score / AI moderation | **Future Phase** | Human queue first; use future automation only as triage with validation and appeal |
| OCR, ID checks, face recognition | **Do not build for Phase 2 MVP** | Excessive child/identity data and false-assurance risk |
| Weighted unverified ratings (weight 0.3) | **Do not build** | Contradicts a “verified reviews” proposition and allows unverified manipulation |
| Review “quality score” badge | **Do not build** | Opaque and likely to imply truth; use transparent verification and moderation labels instead |
| School analytics/recommendations/communities | **Phase 3+** | Outside the review trust pilot |

## Verification recommendation

**Method A (document upload + manual review) is the initial broad-pilot method.** Accept a small allowlist such as a school-issued enrollment confirmation or redacted tuition receipt. Ask only for proof of parent/guardian-school association and the relevant school year. Do not collect government IDs, student numbers, child names, grades, medical details, or bank/card information. Have a trained moderator approve/reject, grant a limited validity period, and delete the source document after the approved appeal window.

**Method B (one-time school-issued code) is the preferred lower-data partner option, but not the only MVP path.** A code is trustworthy only when the school distributes it through a verified channel. Store only a token hash, make it high-entropy/one-use/expiring/revocable, and audit the issuer. Do not ship reusable class-wide passwords.

Do not rely on email domain verification for parenthood. It proves mailbox control, and in many schools it may identify staff rather than parents.

## Rating recommendation

Choose **Option A: only verified published reviews count**. An unverified weight such as 0.3 creates a second lower-trust review class and gives fraudsters a low-friction path to alter school scores. Keep the formula simple and published: arithmetic mean, one contribution per active review, only the approved version, category-specific counts, and a visible sample size. Do not invent Bayesian priors, hidden recency decay, or composite “quality” rankings in the MVP.

## Why not build more?

- **A service per domain is unjustified.** The expected scale fits the current API/PostgreSQL stack.
- **Four verification methods create more policy than coverage.** Ship one method, then add school credentials when operations exist.
- **Votes/comments invite coordinated manipulation** before SchoolWise has an established community or abuse team.
- **Automatic review truth scoring is not an achievable promise.** The platform can verify a relationship and moderate policy; it cannot certify subjective truth.
- **Permanent immutable personal histories conflict with data minimization.** Preserve auditable version/decision history for a documented period and support erasure.
- **A high score is not the goal.** Honest coverage, low privacy harm, fair moderation, and useful school choice are the product outcomes.

## Team and launch readiness

Before public pilot, assign:

- a product owner accountable for eligibility and review policy;
- a privacy/legal owner to approve lawful basis, DPIA decision, retention, Greek/EU legal risks, and DSA applicability;
- a security owner for Blob, Managed Identity, scanning, deletion, and incident response; and
- trained moderators with capacity to meet the queue targets.

If these owners or deletion/appeal operations are absent, do not collect documents or publish scores. Use a smaller closed pilot until the operational trust system is ready.

See [Phase2-Roadmap.md](Phase2-Roadmap.md) for gates and [Phase2-GDPR.md](Phase2-GDPR.md) for privacy/legal decisions.

