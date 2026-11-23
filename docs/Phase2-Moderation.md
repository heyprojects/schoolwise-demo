# SchoolWise Phase 2 Moderation Design

**Status:** Policy and workflow design only.  
**Goal:** Publish useful parent experience without confusing verification with truth, silencing legitimate criticism, or exposing children and staff.

## 1. Operating principles

1. Verification proves a bounded parent-school relationship, not that a review is accurate or endorsed.
2. Apply the same written rules to positive and negative reviews. Schools cannot buy removal, prioritization, or a more favorable score.
3. Use human decisions for verification and publication. Automated classifiers and risk signals may prioritize cases, but cannot alone approve, reject, or remove content.
4. Separate the three decisions: **relationship verification**, **content moderation**, and **fraud/account enforcement**.
5. Moderate the exact immutable review version submitted. A later edit requires a new decision.
6. Collect the minimum identity/evidence needed. Moderators do not need a child's full name, student number, or medical information.
7. Give the author a specific, intelligible reason when a submission or published review is restricted. Maintain an appeal/contact route.
8. A school may report content or submit a factual correction, but may not see who submitted a review or approve/reject a parent's review.

## 2. Roles and separation of duties

| Role | Allowed | Explicitly not allowed |
|---|---|---|
| **Parent** | Create/edit their own eligible review, check their verification status, delete their review, report content, appeal a decision | See another parent's draft, evidence, account identity, or moderator notes |
| **Moderator** | Review assigned submissions, review evidence, make bounded decisions, remove policy-violating content, triage reports, add internal notes | Change ratings/content, alter school facts, review their own submission, access unrelated evidence, issue credentials for a school without scoped authorization |
| **Admin** | Assign/revoke Moderator role, configure policy/reason codes, handle escalations and audited overrides | Silently alter published reviews, erase decision history, access all documents without a case/purpose |
| **SchoolAdmin** | Phase 1 school management only; future school-scoped issuance of codes/invitations if approved | Access parent documents, identify reviewers, moderate content about their school, change reviews or ratings |

Add a `Moderator` role; do not make all Admins moderators by default. Require MFA for Moderator/Admin accounts, short sessions for evidence access, least-privilege endpoints, and audit every override. A moderator must not decide a request they submitted or a case with a disclosed personal/financial conflict. Use a second reviewer for high-impact exceptions (fraud suspensions, legal escalation, school-wide action, or removal after publication), not for every routine case.

## 3. Separate queues

Do not combine evidence review with text review in one vague “approve” action. Each queue has its own fields, roles, decision codes, and service target.

### Verification queue

**States:** Pending upload/scan → Pending moderation → Approved, Rejected, or Expired. A pending request with malware or failed scanning stays quarantined and is not shown to a reviewer as a safe file.

**Queue row:** submission age, school, method, file count/type/size/scan status, previous attempts for this account/school, and a coarse operational risk flag. Avoid child names, raw evidence text, full IP history, or public review content.

**Reviewer checklist:**

1. Confirm the request is for an approved school and the uploader owns the request.
2. Check that the proof is school-issued/reasonably attributable and refers to a current or recent school year.
3. Match only the minimum parent/guardian identifier needed to the account, where present. Do not copy document text into notes.
4. Reject illegible, unsupported, altered, expired, or mismatched evidence with a neutral reason and safe resubmission path.
5. Approve only the parent-school relationship. Set an expiry (recommended: 12 months or school-year end, whichever is earlier).
6. Record moderator, timestamp, reason code, and outcome; schedule evidence deletion after the appeal window.

**Target:** 90% of ordinary requests decided within two business days during the pilot. Escalate unresolved/ambiguous evidence rather than making an unsupported assumption.

### Review queue

**Queue row:** school, age, locale, version number, publication/edit state, and risk-priority reason. Default to identity-blind review. For a revision, show the prior published version only when necessary to understand a material change; do not reveal verification documents or parent email.

**Decision checklist:**

- Is the author eligible and the submitted version stable?
- Does the text describe first-hand parent/guardian experience, rather than rumor or a commercial pitch?
- Does it contain a child's name, contact details, class/teacher identifiers, personal data, allegations stated as fact, threats, hate, harassment, or irrelevant sensitive information?
- Does it appear to be a duplicate, coordinated campaign, or conflict-of-interest review? If so, escalate; do not infer fraud solely from sentiment, rating, or IP.
- Does the same written standard apply to praise and criticism?
- Is the action limited to the offending content, with a specific reason and appeal/contact path?

**Target:** 90% of ordinary first submissions decided within two business days; privacy/safety reports triaged within one business day; urgent credible threats or exposed child-identifying information escalated immediately. These are operating targets, not a substitute for statutory deadlines.

## 4. Review decision rubric

| Outcome | Use when | User-facing next step |
|---|---|---|
| **Approve** | First-hand experience, within scope, no disallowed identifiers/claims, readable and policy-compliant | Publish the approved version; explain that SchoolWise checked eligibility and policy compliance, not factual truth |
| **Reject and invite edit** | Fixable issue such as identifying information, unsupported allegation, abusive wording, or insufficient first-hand context | Give a specific reason category and safe instructions; allow correction/resubmission |
| **Reject** | Clearly fabricated, threatening, discriminatory, promotional, or repeated policy evasion | Explain the policy reason and appeal route; preserve the case record under retention rules |
| **Remove/limit** | A published review is reported and appears to expose personal data, be unlawful, be fraudulent, or otherwise violate policy | Unpublish promptly when risk warrants; notify the author with reasons and appeal path |
| **Escalate** | Credible safety threat, legal demand, possible child harm, serious defamation, conflict of interest, or uncertain legality | Restrict access if necessary, preserve only evidence needed, and escalate to designated legal/safeguarding contact |

Do not reject solely because a review is negative, gives a low rating, criticizes a school's facilities, or has imperfect grammar. Do not edit the parent's wording into a more favorable statement. If only one sentence is problematic, ask for a user revision or remove the minimum necessary text under policy and record why.

## 5. Lifecycle and version behavior

The database design in [Phase2-Database.md](Phase2-Database.md) is authoritative:

- A Draft is not public.
- A submitted version is immutable and PendingModeration.
- First approval makes that version public.
- Editing a published review creates a new pending version; the existing approved version remains public.
- Approval atomically makes the new version public and marks the old one superseded.
- Rejection of an edit leaves the previous approved version public; rejection of a first version publishes nothing.
- Parent deletion immediately hides the review and archives it. Moderator removal immediately hides the affected content and records a separate decision.

Every decision references a specific version/target and records a controlled reason code, user-facing reason, internal note (if needed), actor, timestamp, and correlation ID. A decision is never silently overwritten; an appeal or correction adds a new event.

## 6. Appeals, reports, and school responses

- The parent can appeal a rejection or removal from the decision notice. Route it to a different moderator when practical; target a response within five business days.
- A parent report creates a private case. The reporter receives a receipt, not the author's private identity or a promise of removal.
- During the pilot, school representatives use the documented support/legal contact to report a review and submit verifiable factual corrections. They may not demand removal solely because content is unfavorable.
- Do not offer a paid “verified” badge or paid moderation outcome.
- A school response is not required for Phase 2 MVP. If introduced later, label it clearly as a school response, cap its length, moderate it, and prevent it from revealing a parent's identity.
- Publish a concise, stable review policy and prohibited-content list before accepting submissions.

## 7. Legal/DSA operational readiness

Before launch, counsel should classify SchoolWise under the Digital Services Act (DSA) and confirm which obligations apply to its exact service size and features. Design the intake and decision record to support a notice-and-action channel, reasoned moderation notices, and an internal complaint process where required. Treat the DSA as a product workflow requirement, not merely a terms-of-service paragraph. The EU Commission notes that hosting services have notice/action and statement-of-reasons obligations; applicability of additional online-platform duties depends on classification and exemptions.

Keep:

- an easy-to-find report form with categories and a way to submit relevant grounds/evidence;
- a timestamped record of notice, triage, action, reason, and user notification;
- a human contact route for moderation complaints;
- moderation metrics by reason and outcome, with no unnecessary reporter data; and
- an escalation contact for legal requests and child-safety concerns.

Do not publish another user's personal data in a DSA statement of reasons or transparency report. See the [DSA and transparency references](Phase2-GDPR.md#references).

## 8. Moderator quality and safeguards

- Onboard moderators to Greek-language nuance, school terminology, privacy, defamation risk, and child-safety escalation.
- Maintain a reason-code guide with examples for Greek and English; sample decisions monthly for consistency.
- Track reversal rate, appeal overturn rate, queue age, evidence-access volume, and school/locale disparities.
- Review a small random sample of approvals and rejections. Do not measure moderator performance by rejection/publishing quotas.
- Keep review decisions and verification decisions separate in access control, training, and audit.
- Retain internal notes only when they materially explain a decision. Do not use subjective labels such as “difficult parent” or “problem school.”

