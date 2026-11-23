# SchoolWise Phase 2 MVP Cut

**Decision memo — 2026-11-22**  
**Status:** Product priority changed on 2026-11-23: the current sprint implements reviews, version history, Admin moderation and arithmetic ratings from all Published reviews, without a verification gate. The earlier code-only foundation remains experimental and is not expanded. Neither new schema has been deployed to production in this session. See [the implementation report](../IMPLEMENTATION_REPORT.md) for the current implemented scope.

> **This cut supersedes the broader single-release scope in [Phase2-MVP-Recommendation.md](Phase2-MVP-Recommendation.md) and [Phase2-Roadmap.md](Phase2-Roadmap.md) for a one-developer launch.** The other Phase 2 documents remain the target-state design, not an MVP implementation checklist.

> **Current sprint supersession:** The verified-only eligibility, score-weighting and five-parent threshold requirements below are the earlier trust-pilot proposal, not this sprint's acceptance checklist. Current reviews are explicitly unverified; all Published reviews count. Public verification badges, fraud engines, votes, notifications, AI and school claiming are not implemented. Real-parent legal/privacy and operational launch gates still apply.

## Implementation checkpoint: code-only eligibility

- Admin screen `/admin/verifications`: find an approved school, issue up to 50 codes, copy plaintext once, inspect paged metadata, and revoke with a reason.
- Parent screen `/verifications`: redeem a school-specific code and inspect own active/expired/revoked eligibility.
- Hash-only persistence, transactional single use, current-account role checks, one current grant per parent/school, renewal, audit metadata, paginated reads and per-account rate limits are implemented. The API and schema are documented in [ApiEndpoints.md](ApiEndpoints.md) and [DatabaseSchema.md](DatabaseSchema.md).
- No new packages, uploads, paid services or production schema changes. The implementation uses the existing EF Core/Npgsql stack.
- Validation: 15 application tests and 8 PostgreSQL/API tests passed against an isolated, expiring Neon branch, including simultaneous redemption, renewal, revocation, expired codes, role/ownership boundaries and rate limits. Frontend lint and production build passed. A browser completed Admin issuance -> new Parent registration/login -> redemption -> Active eligibility using the scoped runtime database role.
- **Still blocked for real users:** confirmed email, MFA/Moderator controls, versioned reviews and human moderation, legal/privacy/retention operations, partner agreements, production-capable hosting and backup/restore rehearsal. The UI explicitly labels this as a demo foundation; no public verified-parent badge or ratings are enabled.
- Rate limits are single-process/in-memory; restarts reset counters. Strengthen enrollment and abuse controls before a real pilot. The Admin must choose the authorized school-year cutoff; the server caps validity at 12 months but cannot infer enrollment periods.
- Before deploying this slice: back up the target database, review the additive `AddSchoolVerification` migration, apply it using a direct owner connection, verify runtime-role permissions on the new tables, then deploy API before frontend and repeat the two-role smoke test. Do not blindly run tests or migrations against production.
- The temporary test branch expires automatically; do not copy its credentials into Render/Vercel. Existing production environment files and resources are unchanged.

## Executive decision

**Yes—the completed Phase 2 design is too complex for an MVP built by one developer in under eight weeks.** It is a useful production target, but its original 6–8 week estimate assumes a multi-engineer team plus dedicated moderation, privacy/legal, and school-onboarding capacity. It includes a document-ingestion system, several verification methods, multiple moderation queues, privacy-rights automation, malware scanning, event processing, audit, retention, caching, and an expanded school-admin workflow before product demand has been validated.

Launch a **closed, partner-school pilot** instead:

- Recruit 5–10 schools that agree to distribute unique, single-use verification codes to their parents.
- Use one-time school-issued codes as the only parent-school proof in the pilot. Confirm account email, but do not treat email ownership as school/parent verification.
- Do not accept verification documents in the MVP. This removes Blob uploads, SAS handling, malware scanning, file-access audit, and document-deletion workflows from the launch path.
- Keep review versions, human moderation before publication, a report path, a clear appeal/contact path, and the privacy/legal launch gates. These are trust essentials, not optional automation.
- Aim for **100 approved reviews from 100 distinct verified parents across about 10 pilot schools**—roughly 10 per school—so each school can reach the five-review score threshold. A hundred reviews concentrated in one school is not a useful market test.
- Keep the existing API/PostgreSQL/Next.js modular monolith. Do not add microservices, Service Bus, Redis, or a generic workflow engine.

If suitable partner schools cannot be secured before implementation starts, **do not replace school-issued codes with email-domain checks or a casual spreadsheet of enrollment data**. Either delay the launch to find partners or explicitly fund a secure document/manual-verification phase.

## Answers to the CTO/PM questions

### 1. Is the proposed design too complex for an MVP?

**Yes.** The main overreach is not the review form or the 100,000-review database target; it is trying to launch a complete verification and trust-operations platform before proving that parents use reviews to choose schools.

The broad design puts these together in one launch: document uploads, private Azure Blob, user-delegation SAS, malware scanning/Event Grid, document access auditing/deletion, code/invitation/domain methods, separate verification and review queues, immutable versions, reports, appeals, school responses, advanced fraud signals, privacy export/erasure endpoints, aggregate projections/caching, and multiple moderator roles. A single developer can build a smaller slice, but cannot responsibly deliver and operate all of that in eight weeks without reducing testing and privacy review.

### 2. Which components should be postponed?

- Document uploads and all associated Azure services (Blob, SAS, Defender for Storage, Event Grid, quarantine/reconciliation workers).
- School-admin invitation portal, school-domain verification, and all but one verification method.
- Automated fraud scoring, device graphs, OCR, identity-document checks, face recognition, AI moderation, and sentiment analysis.
- Review votes, comments, public school replies, reviewer profiles, and a public “review quality” score.
- Redis, materialized rating projections, Service Bus, distributed workers, and high-scale analytics.
- Fully automated privacy-request/export portal. Rights handling remains required, but a secure manual privacy-request process is adequate for a small pilot.
- Routine dual-moderator approvals. Use a second person only for appeals, high-impact removals, suspected fraud campaigns, legal escalation, and moderator conflicts.

### 3. Minimum implementation to validate product-market fit

1. A small cohort of partner schools with one authorized contact and a documented code-distribution process.
2. Confirmed parent account email and an Admin-controlled way to create batches of unique, random, single-use codes. Store only code hashes; display/download raw codes once, then deliver them through an agreed secure channel. No reusable class-wide password.
3. Code redemption creates an auditable, expiring parent-school verification record. A code is valid for one school and one redemption; use a 12-month or school-year expiry, whichever is earlier. Rate-limit redemption and return generic errors.
4. One active review per parent/school, overall rating 1–5, optional category ratings, pros/cons/text, the requested lifecycle statuses, and immutable submitted versions.
5. One simple moderator page/queue to review, approve, reject with a reason, remove, and record an appeal/report outcome. Every published version must have been human-approved; there is no school veto.
6. Public school review list and transparent score calculated from published verified reviews only. Hide overall/category numeric scores until at least five distinct verified parents contribute to that score.
7. Privacy notice, review rules, explicit publication choice, report/contact route, deletion/unpublishing workflow, retention schedule, and legal/DPIA go/no-go decision before real submissions.
8. Basic audit logs, role/ownership enforcement, rate limits, database constraints, backup/restore test, and focused integration tests for code redemption and review publication.

This tests the core hypothesis: **parents trust and use verified parent experiences to shortlist or contact a school**. One hundred submitted reviews alone do not prove product-market fit. Track verification-to-publication conversion, review-page engagement, school shortlist/contact actions, reasons parents trust or reject the badge, and qualitative interviews. Also measure whether partner schools will keep distributing codes.

### 4. Which phase has the biggest implementation risk?

In the full design, the **document verification pipeline** is the highest implementation-risk workstream: direct browser upload, short-lived SAS permissions, quarantine, scan-result processing, state transitions, moderator access, deletion, and backup reconciliation cross several security boundaries.

After cutting documents, the main MVP correctness risk is the **review version/publication state machine**. A stale moderator decision must never publish a different edit; an edit must not erase the previous approved version; deletion/removal must immediately stop public reads and scores. Keep this explicit, transactional, and covered by focused tests.

### 5. Which phase has the biggest legal risk?

The **public user-generated review and moderation phase** has the largest overall legal exposure: potentially defamatory claims, identifying details about children/teachers, sensitive personal information, and EU hosting/intermediary-service duties. Document verification adds a separate high GDPR risk, which is why this MVP cut removes document collection rather than weakening its security.

Before launch, Greek/EU counsel must review the review policy, privacy notice/lawful bases, DSA classification and applicable obligations, statements/reasons and complaints process, retention, and any partner-school code process. A small pilot is not an exemption from applicable law.

### 6. Which phase has the biggest operational cost?

**Human operations, not Azure compute.** Every review needs policy moderation, every report may need investigation, codes require partner coordination, and appeals/privacy requests need timely handling. At roughly 3–5 minutes per review, 100 reviews require about 5–9 direct review hours, before escalation, appeals, support, training, and school onboarding. Plan a practical initial budget of **15–25 staff hours** for the first 100 reviews, then replace this estimate with measured pilot data.

In the document-based design, verification review adds another 5–9 hours per 100 documents at the same assumption, plus secure-file exceptions and deletion checks. The code-only pilot removes that per-document review work but shifts effort to onboarding partner schools and distributing codes.

### 7. Which features can be launched manually?

- Recruit schools, validate the contact, issue code batches, and coordinate distribution manually.
- Human-moderate every submitted review; use a basic queue and a short reason-code guide.
- Handle appeals, reports, school complaints, and legal escalations through a monitored support workflow, recording decisions in the product.
- Process access/erasure requests with a restricted staff runbook and a tracked case; do not postpone the right itself just because a self-service portal is deferred.
- Calculate school aggregates with a bounded SQL query at this volume; do not build a projection/cache first.
- Review abuse signals and school coverage manually. Do not automate fraud verdicts.
- Send a small number of operational notifications manually if transactional email is not already configured; never send codes or evidence through unapproved channels.

## Feature cut: classify every Phase 2 capability

“Must Have” means required for this partner pilot, not that every long-term design feature is included. “Nice To Have” can be added only if it does not threaten the eight-week launch. “Future” is explicitly outside the first pilot.

| Capability from the Phase 2 design | Classification | MVP scope / cut |
|---|---|---|
| Confirmed Parent account email | **Must Have** | Establish account mailbox control; never label this as school verification |
| Parent-school relationship proof | **Must Have** | Single-use code distributed by a participating school |
| Method B: school-issued one-time verification code | **Must Have** | The only parent-school verification method enabled in the pilot |
| Code issuance, hash-at-rest, one redemption, expiry/revocation, rate limits, audit | **Must Have** | Minimal Admin-only batch creation and parent redemption; no school portal |
| School onboarding and code distribution | **Must Have** | Manual operations with 5–10 named partner schools |
| Manual document verification | **Future** | No document upload in MVP; do not accept documents by ordinary email |
| Azure Blob private quarantine, SAS, Managed Identity for documents, Defender scanning, Event Grid, document-access audit, lifecycle cleanup | **Future** | Not needed when no evidence files are collected |
| Method C: school-admin invitation portal | **Future** | Requires school identity/authorization, recovery, and audit design |
| Method D: school-domain email verification | **Future** | A domain/mailbox is not proof of parenthood; never grant verification from email alone |
| One active review per parent/school | **Must Have** | Enforce with a database unique constraint |
| Overall rating, five optional category ratings, pros/cons, text, locale | **Must Have** | Overall required; category ratings may be “not enough experience” |
| Review statuses: Draft, PendingModeration, Published, Rejected, Archived | **Must Have** | Keep the requested states and explicit transition rules |
| Immutable submitted versions; current and last-published version distinction | **Must Have** | Prevent stale approval and preserve prior approved content during an edit |
| Parent create/edit/delete own review | **Must Have** | Owner-only; delete immediately unpublishes/archives |
| Human moderation before publishing and after material edits | **Must Have** | One review queue; no automatic publication |
| Moderator role, least privilege, audited decisions, MFA | **Must Have** | Minimal Moderator/Admin capability; no generic workflow engine |
| Parent verification-status and My Reviews screens | **Must Have** | Parents need to see their own verification expiry, review state, and decisions |
| Separate human verification queue for uploaded evidence | **Future** | Codes grant eligibility automatically; no document review in the pilot |
| Internal notes | **Nice To Have** | One bounded note on a decision; no threaded case-management system |
| User-facing rejection/removal reasons and appeal/contact path | **Must Have** | Small reason-code set and manual appeal route |
| Review reports and DSA-ready notice/contact process | **Must Have** | Basic report capture, monitored contact, human triage; counsel confirms exact DSA duties |
| Parent self-service appeal portal | **Nice To Have** | A tracked email/support appeal is acceptable for a small pilot if reasoned decisions are recorded |
| Parent account deletion, unpublish, data-access/erasure handling, retention | **Must Have** | Manual operational workflow is acceptable; deletion and privacy rights are not optional |
| Self-service privacy export/erasure portal | **Nice To Have** | Use a restricted, tracked support workflow first; honor the rights themselves |
| Automated review-status email notifications | **Nice To Have** | Start with in-product status and essential transactional messages |
| Review votes/helpfulness sorting | **Future** | No evidence this is needed to validate review usefulness |
| Public comments, school replies, reviewer profiles | **Future** | Expands moderation and retaliation surface |
| Verified-review-only arithmetic score | **Must Have** | Transparent formula; each active published review contributes once |
| Unverified ratings with 0.3 weight / Bayesian or recency weighting | **Future** | Do not mix trust classes or introduce opaque weighting |
| Minimum five distinct reviewers before numeric score | **Must Have** | Enforce overall and per-category; threshold is not a claim of anonymity |
| Recent public reviews and “verified at time of review” badge | **Must Have** | No author identity, evidence method, or guarantee of factual truth |
| Public “Review Quality Indicator” | **Future** | Opaque score could imply truth; use transparent labels only |
| Basic indexes and parameterized, paged school review queries | **Must Have** | Support the pilot; no specialized search index |
| `school_rating_summaries` projection, Redis, CDN/cache layers | **Future** | SQL aggregation is sufficient at 100 reviews; add only after measurement |
| Outbox, Event Grid, Service Bus, distributed worker fleet | **Future** | No asynchronous file scanning in the pilot; use simple transactional updates and bounded email handling |
| Automated fraud graph, device fingerprinting, OCR, AI moderation, sentiment analysis | **Future** | Human investigation only; signals must never be an unreviewable verdict |
| Manual abuse monitoring, code/review rate limits, duplicate constraints | **Must Have** | Proportional, minimally logged controls |
| Two-person approval for every case | **Future** | Require a second reviewer only for appeals/high-impact or conflicted cases |
| School analytics, recommendations, communities, matching | **Future** | Phase 3 or later |
| 100,000-review distributed architecture | **Future** | Design the schema/indexes sensibly, but do not build for unobserved scale |

## Revised one-developer implementation plan

### Non-negotiable preconditions (before the eight-week clock)

- 5–10 schools have agreed to participate, named an authorized contact, and accepted a secure one-time-code distribution process.
- Product/legal owner has approved eligibility, review rules, privacy notice, publication choice, retention, complaint route, and the DSA applicability check.
- A moderator/operator is available for review queues and support. The developer is not the only person able to respond to safety/legal reports.
- The team agrees to a closed pilot, Greek-first content, no document uploads, no school self-service portal, and no feature expansion during the build.

If these conditions are not met, an eight-week public launch is not a credible commitment. Code alone does not establish a safe operating model.

### Weeks 1–2: eligibility and data foundation

- Add only the necessary review/version, code, verification-request, moderation-decision, and report persistence.
- Add one-time code batch creation/redemption with no plaintext code persistence, single use, expiry, revocation, per-account rate limits, and generic failure responses.
- Add the expiring parent-school grant/audit outcome and verify the actor/school relationship at each review submission.
- Add Moderator authorization, ownership checks, concurrency/version tokens, essential database constraints, and unit/integration tests.

**Exit:** expired/redeemed/wrong-school codes cannot grant access; duplicate active reviews are rejected by the database; an unverified Parent cannot submit.

### Weeks 3–4: reviews and manual moderation

- Implement draft, edit, submit, delete/archive, immutable versions, required ratings/text validation, and requested status transitions.
- Build one small Moderator queue for review versions with approve/reject, reason, timestamp, actor, and minimal internal note.
- Preserve the previous published version during edits; atomically switch only after approval.
- Record reports and removals; provide an appeal/support route and notify users with a reason.

**Exit:** only the exact approved version is public; retries/stale decisions cannot publish the wrong version; removal immediately stops public reads and score contribution.

### Weeks 5–6: parent and public experience

- Add the parent code redemption/status flow and review form/My Reviews screen.
- Add the school review section, recent reviews, transparent “verified at time” indicator, score counts, and five-review suppression.
- Keep calculations in SQL; no cache/projection until performance data proves it is needed.
- Add accessible policy reminders and explicit publication confirmation.

**Exit:** a parent can complete the end-to-end journey on mobile; public responses expose no account, child, moderation, or code data.

### Week 7: privacy, security, and operational rehearsal

- Complete legal/DPIA actions and privacy notices; test account deletion/unpublish, access request handling, backup/restore suppression, and moderator access controls.
- Test abuse controls, role boundaries, code revocation, PostgreSQL constraints, and concurrent edit/approval behavior.
- Run a tabletop report for child-identifying content, suspected fraud, school complaint, and urgent legal request.
- Train the operator and document the support/appeal/review policy.

**Exit:** no unresolved critical privacy/security issue; operator can process the queue without developer-only DB edits.

### Week 8: closed pilot and first 100

- Start with 20–30 parents across partner schools; observe the entire verification-to-review flow and fix blocking issues.
- Expand code distribution toward 100 distinct verified parents and target roughly 10 published reviews per 10 schools.
- Review trust, conversion, moderation time, school coverage, and parent interviews weekly. Pause enrollment if queue SLA or privacy controls fail.

This schedule assumes a developer already familiar with the repository and Azure deployment. It does not include school sales cycles, legal turnaround, or procurement. If code distribution is delayed, keep the application private rather than collecting unverified reviews to meet a date.

## Azure cost plan

### Recommended low-cost pilot footprint

1. **One region** selected for operational and legal suitability.
2. **One Azure Database for PostgreSQL Flexible Server** on the smallest supported Burstable tier that passes load tests; single zone/no HA/read replica for the pilot, minimum practical storage, required backups, and budget/usage alerts. Do not use production stop/start as a cost-control workaround.
3. **Cost-first application hosting:** Azure Container Apps Consumption for the Next.js and API containers, both configured to scale to zero when idle, with no dedicated workload profile. This is the lowest compute-cost option for a sparse pilot if the developer can configure it safely in a short infrastructure spike. Accept cold starts and cap replicas. Keep secure PostgreSQL connectivity and logging cost in the estimate.
4. Use the existing external URLs for school logos/photos; **do not provision Blob Storage for parent documents in this pilot**.
5. Use one transactional email provider only for account confirmation and essential notices; no marketing platform, SMS verification, analytics warehouse, or paid identity vendor.
6. Keep application logs sampled and short-retention; retain decision/audit events needed for trust in PostgreSQL. Set Azure Cost Management budget alerts before inviting users.

At the source-review date, Microsoft's Container Apps pricing page lists a subscription-level monthly free grant of 180,000 vCPU-seconds, 360,000 GiB-seconds, and 2 million requests for Consumption usage. Verify current grant eligibility, applicable environment/workload profile, network, log, and storage charges before assuming app compute will be free. The database and backups add a separate fixed cost, so use the Azure Pricing Calculator for a region-specific total; no credible all-in monthly total can be stated without the chosen region, SKU, backup policy, and subscription.

**One-day fallback gate:** If Container Apps networking, deployment, or diagnostics takes more than one day or threatens the schedule, host both apps on one shared Linux App Service plan instead. App Service charges for the plan once, and apps in that plan share compute. Microsoft currently displays Linux B1 around **US$13/month** and B2 around **US$25.55/month** in its pricing context; these are not Greek-region quotes and the B1 memory/CPU may be too small for both SSR and API. Benchmark before selecting B1; use B2 rather than accepting OOMs. App Service is the schedule-safe fallback, not the absolute-lowest idle-compute option.

The managed database is the fixed-cost floor in either option. Do not stop a production pilot database while parents expect access. Keep one region, no HA/read replicas, minimal supported storage, required backups, and budget alerts. Avoid Front Door/CDN, Redis, a separate cache, Service Bus, separate always-on worker, and paid analytics until usage justifies them.

No evidence files means no Defender for Storage per-GB scan charges, Event Grid events, private file access paths, or blob lifecycle jobs. If document verification is later added, do not save money by making uploads public or allowing unscanned files to reach moderators; estimate and fund that path as a separate phase.

## Go/no-go and product-market-fit test

**Go:** legal/privacy approval; partner schools distribute codes; moderation/report capacity is staffed; all public ratings derive solely from approved verified versions; deletion and appeal procedures work.

**Pause:** fewer than five cooperating schools, no accountable content moderator, unresolved child-data/legal risk, code sharing/fraud beyond operational capacity, or repeated queue delays.

**PMF evidence to collect:** 100 distinct verified parents, approximately 10 published reviews at each of 10 pilot schools, parents using reviews during shortlisting/contact decisions, repeated parent willingness to recommend/use the flow, and school partners agreeing to continue. Treat the numbers as a focused discovery cohort—not statistically conclusive proof of PMF.

## Cost and compliance references

Prices and quotas vary by region, subscription, and date. Re-check the calculator immediately before provisioning.

- [Azure App Service for Linux pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/)
- [App Service plans and shared compute](https://learn.microsoft.com/en-us/azure/app-service/overview-hosting-plans)
- [Azure Database for PostgreSQL Flexible Server pricing](https://azure.microsoft.com/en-us/pricing/details/postgresql/flexible-server/)
- [PostgreSQL Flexible Server cost optimization](https://learn.microsoft.com/en-us/azure/postgresql/configure-maintain/how-to-cost-optimization)
- [Azure Container Apps pricing and scale-to-zero](https://azure.microsoft.com/en-us/pricing/details/container-apps/)
- [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/)
- [Defender for Storage malware scan cost controls](https://learn.microsoft.com/en-us/azure/defender-for-cloud/on-upload-malware-scanning)
- [Phase 2 GDPR/legal readiness](Phase2-GDPR.md)
- [Cold-start and first-100-review acquisition strategy](ColdStartStrategy.md)

**Bottom line:** To reach the first 100 verified reviews quickly, cut document collection and automation—not verification, human review, privacy, appeals, or audit. The fastest credible MVP is partner-limited, code-verified, human-moderated, and operationally small.

