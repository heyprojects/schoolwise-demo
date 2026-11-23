# SchoolWise Phase 2 Fraud Prevention

**Status:** Product/security design only. No risk-scoring code or fraud models are implemented.

## 1. Threat model

The objective is to prevent fabricated or coordinated claims from driving school selection while preserving honest criticism. SchoolWise cannot prove that every sentence is true. It can establish a reasonable parent-school relationship, preserve provenance, reduce easy abuse, and act on credible reports.

### Core trust boundaries

- Account email confirms mailbox control, not parenthood.
- Verification confirms evidence of an association with one school at a point in time, not the truth or fairness of every opinion.
- Moderation checks policy and privacy, not the factual truth of subjective experiences.
- A review's public badge must never imply that SchoolWise endorses its content.
- A school can be both the subject of criticism and a potential source of reports; it is not the final arbiter of its own reviews.

## 2. Threats, controls, and residual risk

| Abuse scenario | Risk | Preventive controls | Detection/response | Residual risk |
|---|---|---|---|---|
| Fake parent account / duplicate account | High | Confirm email; unique normalized email; one active review per account/school; document or partner-code verification; rate limits; secure account recovery | Repeated attempts, account clusters, overlapping verification evidence; hold for human review; revoke grant for substantiated fraud | A person can create multiple email addresses or collude with real parents |
| Forged or reused proof document | High | Accept narrow evidence types; private upload; short upload authorization; scan and validate server-side; one active grant per user/school | Check prior document digest/request outcomes and inconsistent dates; trained human decision; revoke grant and preserve minimal case evidence | A convincing forged document may pass manual review; hash equality alone does not establish fraud |
| Review farm / coordinated rating attack | High | Require verified eligibility before publication; one active review per parent/school; no anonymous score submissions; restrict rapid account creation | Detect unusual school-specific bursts, shared invite batches, repeated phrasing, timing/device/IP clusters; temporarily prioritize queue, investigate samples | Families may legitimately coordinate; correlations are leads, not proof |
| Current/former staff posing as parents | High | Policy requires disclosure of relevant employment/financial relationship; prohibit reviews about an employer where conflict is material; school-issued verification alone does not remove conflict | User reports, public affiliations, repeated insider-only details; second-person review | Employment cannot be reliably inferred from email domain or profile |
| Competitor/parent with a commercial interest | High | Conflict-of-interest policy; no school-sponsored review incentives; no compensation tied to rating | Campaign patterns, repeated language, abnormal timing; investigate and notify affected users | Motive is difficult to prove; do not remove on suspicion alone |
| Retaliation against a school/teacher | High | No staff names, child names, contact details, threats, or unsubstantiated criminal allegations; report/removal flow | Fast triage for doxxing/threats; preserve only necessary evidence; escalate credible threats | Anonymous narratives may still be identifiable in a small school |
| School or staff attempting to suppress criticism | High | No school veto; transparent review rules; paid placement cannot affect moderation; independent appeals | Track report/removal rates by school and successful appeal rate; require evidence for legal claims | Repeated legal threats may create operational pressure; escalation must be independent |
| Child identity or sensitive information disclosed in review/proof | Critical | Explicit warnings before upload/text submission; do not request child identifiers; field minimization; private evidence; reviewer training | Automated PII hints may prioritize but human review decides; restrict/hide immediately when credible | Users can reveal information indirectly, especially in small communities |
| Compromised moderator or insider access | High | MFA, least privilege, case-scoped evidence access, separation of duties, append-only decision/audit records | Alert on bulk access, out-of-hours access, repeated downloads, role changes | Authorized access can still be misused; periodic access reviews and incident response remain necessary |

Risk labels describe product impact, not certainty that a user is fraudulent. Never expose a private risk score to a school, reviewer, or public profile.

## 3. Prevention controls for the MVP

### Identity and eligibility

- Require confirmed email for all accounts. Do not require a telephone number or government ID by default; both add friction and personal data without proving school association.
- Require a valid Parent-school verification before a first review can be submitted.
- Define eligibility narrowly: a parent or legal guardian with current or recent (recommended within the previous 24 months) experience at the school. Store only the time period/coarse school year needed; do not collect the child's name or class.
- Grant verification for a limited term. A parent can keep the historical “verified at time of review” label on an already-approved review after the grant expires; expiry blocks new submissions.
- Enforce one active review per parent/school with a database unique constraint. A second account does not become eligible merely by using a different email.

### Content and moderation

- Require first-hand experience and a school selection; disallow paid/incentivized reviews, promotional links, threats, slurs, and identifying details.
- Require overall rating 1–5; keep unexperienced category ratings nullable. Do not treat neutral/missing categories as zero.
- Publish a privacy reminder immediately before submission; permit reports by parents and school contacts.
- Use manual moderation for every first review and material edit during the pilot. No unverified or pending reviews affect public ratings.
- Give a specific rejection/removal reason and appeal route. Do not shadow-ban or suppress low ratings.

### Account and endpoint abuse

- Normalize email and apply per-account, IP, and endpoint rate limits; use CAPTCHA only when abuse signals justify it and provide an accessible alternative.
- Use short-lived sessions, existing secure refresh-token practices, MFA for Moderator/Admin, secure recovery, and alerting for privilege changes.
- Add idempotency keys and concurrency constraints to review submission, verification requests, and decisions.
- Require high-entropy one-use school codes if/when introduced. Store only hashes/HMACs; expire and revoke unused codes; rate-limit redemption without revealing valid codes.

### Evidence integrity

- Upload only to a private quarantine blob key generated by the API; do not accept arbitrary storage paths or public URLs.
- Verify actual file size and content signature; scan before moderator access; reject executable/unsupported content.
- Strip image metadata on copies used for review and never publish verification files.
- Store a cryptographic digest only to detect exact reuse/integrity issues; do not use it as an identity signal. A matching digest prompts human review, not automatic rejection.
- Delete files promptly after the decision/appeal window. Do not retain source documents as a permanent fraud database.

## 4. Detection and risk handling

Signals can only prioritize manual review. Use a documented purpose, access restriction, and short retention for any device/IP-related signal. Avoid invasive browser fingerprinting, third-party identity enrichment, school scraping, or covert location tracking.

Potential signals include:

- multiple accounts submitting from a shared device or network in a short period;
- clusters of newly confirmed accounts targeting one school;
- repeated or near-identical review wording, timing, rating patterns, or suspiciously synchronized activity;
- multiple requests using the same evidence digest or a credential redeemed unusually often;
- sudden review/report volume inconsistent with the school's normal baseline;
- account recovery, email changes, and submissions close together; and
- repeated policy violations or moderator decisions overturned on appeal.

Do not treat a shared household IP, school Wi-Fi, low rating, negative sentiment, language quality, disability-related accommodation request, or use of a shared device as proof of fraud. Do not build a single cross-school “trustworthiness” score from these signals in Phase 2.

### Operational risk levels

| Level | Example | Action |
|---|---|---|
| **Low** | Normal request; valid evidence; no conflicting signal | Standard human queue and normal SLA |
| **Medium** | Duplicate wording, multiple attempts, inconclusive proof, or a small burst | Temporarily prioritize/hold the specific case; request clarifying evidence; second reviewer if needed |
| **High** | Strong evidence of a forged credential, coordinated paid campaign, compromised moderator, or repeated intentional privacy abuse | Restrict only the implicated workflow/account as necessary; preserve minimal evidence; two-person review and escalation |
| **Critical** | Credible imminent threat, doxxing of a child, active account compromise, or exposed private evidence | Immediately restrict access/content, notify security/safeguarding lead, preserve only legally necessary evidence, follow incident and legal response plans |

Risk level is a case-management tool, not a verdict. Tell affected users what actionable information they need to resolve the case, without disclosing detection logic that would enable simple evasion.

## 5. Staff and competitor review policy

No system can reliably infer whether a reviewer is employed by, owns, contracts with, or competes with a school. Set an explicit policy:

- Reviewers must disclose a current material employment, ownership, paid consulting, or direct competitive relationship.
- The default MVP rule is to reject a review about a school where the author has a material professional/financial conflict, even if the person is also a parent. If product/legal leadership later permits parent reviews by school employees, require a prominent conflict disclosure and distinct moderation handling.
- Do not classify someone as staff solely because they use a school-domain email; school email is not a parent-verification method.
- Schools may flag a concrete conflict and provide evidence; a moderator, not the school, decides.
- Record only the conflict category and decision needed for the case. Avoid collecting employment history broadly.

## 6. Monitoring without biased ratings

Track operational integrity, not a preferred sentiment:

- verification approval/rejection/appeal rates by method and school type;
- median and 90th-percentile verification/moderation queue age;
- reports per published review and proportion actioned;
- appeal overturn rate and decision reason distribution;
- unusual bursts and duplicate attempts; and
- moderation outcome disparity by school type, language, and rating band.

Review these measures for false positives, especially against public schools, small schools, Greek-language content, and users with accessibility needs. Do not optimize the platform for a higher average school rating.

## 7. Deferred anti-fraud features

Defer automated graph/device reputation, OCR/document authenticity models, face/ID verification, AI sentiment scoring, cross-platform identity matching, and automated coordinated-campaign takedowns. They increase privacy, bias, explainability, and operating risks without being necessary for a manually moderated pilot. Reconsider only with measured abuse data, a DPIA/legal review, validation data, human appeal, and a demonstrated proportional benefit.

