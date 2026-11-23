# SchoolWise Phase 2 GDPR and Legal Design

**Status:** Product and engineering design, not legal advice. A Greek/EU privacy lawyer and the controller's DPO/privacy lead must approve the lawful bases, notices, retention schedule, and moderation policy before real parent or school data is processed.

## 1. Data-protection posture

Phase 2 processes account identifiers, parent-school verification evidence, potentially public opinions, moderation reports, and information that may reveal facts about children or staff. Public school reviews can unintentionally expose special-category or other sensitive information. Treat the launch as a **high privacy-risk feature** and complete a DPIA screening before the pilot; complete a DPIA before launch if the assessment indicates likely high risk.

The controller is the SchoolWise legal entity. Azure, email, analytics, support, and moderation vendors may be processors or separate controllers depending on their actual roles and contract. Maintain a processing inventory and Article 28 processor terms where applicable. Do not assume hosting in an EU region alone resolves international transfer, support-access, or telemetry questions.

## 2. Purpose and lawful-basis design

Do not use one broad “I consent to everything” checkbox. Have counsel document a lawful basis and necessity for each purpose:

| Purpose | Design candidate to assess with counsel | Product safeguard |
|---|---|---|
| Account, authentication, and parent-requested review service | Contract necessity for core service may apply | Collect only account fields needed to operate the service |
| Verify parent-school association and prevent fraud | Legitimate interests or another applicable basis may be appropriate; document the balancing test | Offer a clear verification notice, minimized proof, human review, appeal, expiry, and deletion schedule |
| Publish a parent's review to the public | Assess whether the requested publication is necessary to deliver the chosen feature and whether a separate, freely given publication choice/consent is needed | Separate, affirmative publication choice; explain pseudonymous public display and limits of anonymity; no pre-ticked boxes |
| Security logs and abuse investigation | Assess legitimate interests/legal obligations as applicable | Minimize fields, restrict access, set short retention, and avoid using logs for unrelated analytics |
| Optional product analytics/marketing | Separate consent where required, especially non-essential cookies/communications | Keep optional analytics out of the review/verification decision path |

If publication depends on consent, withdrawal must stop future public display without blocking account access or creating a penalty. Do not call verification consent if the data is necessary for an eligibility decision. Record notice/version accepted, action, and timestamp, but not a consent record as a substitute for a lawful-basis assessment.

## 3. Data minimization and special care for children

- Do not ask for a child's full name, date of birth, student number, AMKA, grades, diagnosis, disability status, attendance record, or photograph.
- Before upload, tell the parent to redact child identifiers, payment amounts, bank/card details, unrelated family information, and any nonessential document content. Explain what must remain visible for verification.
- Do not use face recognition, government-ID checks, school record scraping, or OCR extraction in the MVP.
- Review text must not identify children, individual teachers, or other families; prohibit health, special educational needs, religion, ethnicity, sexual orientation, and other highly sensitive personal details unless counsel approves a narrow and lawful exception. Prefer removal/redaction rather than collecting the information for moderation.
- Public content should describe the school's services and the parent's experience, not a named child's individual circumstances.
- Treat child-related information as especially sensitive operationally even where a particular item does not meet the GDPR Article 9 definition of a special category.

## 4. Proposed retention schedule

These are **starting product limits**, not statutory retention periods. Counsel must check litigation, consumer, safeguarding, and limitation requirements and document any longer period. When the purpose ends, delete or irreversibly anonymize; suspend deletion only for a documented legal hold with access controls and review date.

| Data | Proposed retention |
|---|---|
| Unsubmitted review draft | Delete after 90 days of inactivity, with a reminder before deletion |
| Uploaded verification document | Delete within 30 days after final decision/appeal; hard cap 90 days from upload unless a documented legal hold applies |
| Abandoned, failed-scan, or orphan upload | Delete as soon as the request is cancelled/closed; automated cleanup target within 7 days |
| Pending/rejected verification request metadata | Retain only the decision/status, school, method, timestamps, and minimal reason needed for audit; proposed 24 months after closure, then delete/anonymize subject to counsel |
| Approved parent-school grant | Retain while valid and while needed to support linked reviews; review for deletion 24 months after expiry/account closure |
| Published review and current public version | Retain while the author chooses to keep it public and the account remains eligible under policy; allow immediate user deletion/unpublishing |
| Replaced review versions | Keep while needed for user-visible history/moderation and dispute handling; proposed 24 months after replacement/archival, then erase or irreversibly anonymize unless a specific legal hold applies |
| Review report and moderation decision | Proposed 24 months after case closure, subject to necessity/claims review; avoid retaining raw reporter evidence longer than needed |
| Document-access/security audit | Proposed 12 months, longer only for a documented incident/investigation or legal requirement |
| Backups | Use a documented short rolling window (target 35 days); restored backups must replay deletion/suppression records before serving traffic |

Do not retain raw verification files for the lifetime of an account. Deleting the Blob must include replicas/backups according to the provider's documented lifecycle, and the platform must reconcile failed deletion jobs. A five-review threshold is a risk reduction for small cohorts, not a claim that either the scores or review text are anonymous.

## 5. Data-subject rights and account deletion

Provide a privacy-request contact and authenticated self-service where safe. Route access, rectification, erasure, restriction, objection, and portability requests to an operational queue. The GDPR response deadline is generally one month, subject to permitted extension and notification rules; staff need an escalation process to meet it.

### Suggested erasure workflow

1. Verify the requester's account without collecting extra identity documents unless necessary.
2. Immediately stop new verification/review activity and unpublish the user's review(s) when requested or account deletion is initiated.
3. Revoke active sessions, grants, credentials, and pending uploads.
4. Delete raw verification documents and unnecessary contact data.
5. Delete or anonymize review text, versions, reports, and identifiers according to the approved legal basis and retention schedule. Do not promise that every record can always be erased if a narrow legal obligation or legal-claims exception applies; document the specific exception, scope, and expiry.
6. Preserve only minimal, access-restricted audit metadata where lawful and necessary. Hashes or pseudonymous IDs may still be personal data if they can be linked back.
7. Invalidate public caches/search results and propagate erasure to processors; ensure restored backups replay the erasure event.
8. Tell the requester what was done and what, if anything, was retained and why.

“Review versions must be preserved” is an audit requirement during their justified retention period, not permission to keep personal narrative forever. Support redaction/anonymization of historical versions when erasure is required.

### Access and portability

An access export should include the user's own verification statuses, uploaded evidence still retained, reviews/versions, decisions about their content, reports they filed, and relevant recipients/purposes, subject to third-party rights and lawful restrictions. Portability applies only where the GDPR's conditions are met; do not promise portability for every inferred fraud signal or moderation assessment.

## 6. Transparency and consent UX

At registration and before each sensitive action, explain:

- why verification evidence is requested, what exact proof is accepted, who reviews it, and when it is deleted;
- that verification is a point-in-time relationship check, not an endorsement of review truth;
- what is public, what is pseudonymous, and that free text can reveal the author even when the name is hidden;
- how a review is moderated, the likely wait, appeal/report routes, and what happens to a published review after edits or deletion;
- contact details for the controller/privacy requests and any relevant processor categories; and
- any automated risk signal used to prioritize a case, its human-review role, and how to contest an adverse outcome.

Collect a separate, explicit publication action for the review. Do not make public display the default for saved drafts. Keep the privacy notice version and publication action in an audit record. Never expose verification evidence to the school.

## 7. Security and vendor controls

- Private Azure Blob container; disable public access and Shared Key where operationally supported; use HTTPS, Azure RBAC, Managed Identity, short-lived user-delegation SAS for direct upload, server-side content validation, malware scanning, and access audit.
- Encrypt in transit and at rest; use customer-managed keys only where a documented risk/contract requirement justifies them.
- Keep the database and storage account in a selected EU region where supported; review Azure data-residency, support, backup, diagnostic, and cross-region disaster-recovery behavior contractually.
- Keep verification documents out of logs, traces, analytics, and support tickets. Use correlation IDs that do not encode personal data.
- Restrict moderator evidence access by role and case; MFA for Moderator/Admin; alert on bulk or anomalous access.
- Use private endpoints/network restrictions for production where feasible, least-privilege managed identities, key rotation, backups, restore tests, and an incident-response plan.
- Record subprocessors and data flows; review Data Processing Agreements, international-transfer mechanisms, deletion behavior, breach notification duties, and security evidence.

## 8. DPIA, DPO, and incident readiness

Before the pilot:

1. Map every data flow, purpose, recipient, transfer, and deletion path.
2. Perform a DPIA screening and have the DPO/privacy lead assess whether a full DPIA is required, considering children's information, education context, public user-generated content, documents, fraud signals, and moderation effects.
3. If residual high risk remains after mitigation, consult the supervisory authority before processing as required by the GDPR.
4. Test data-subject access/erasure, blob deletion, backup restore, moderator access revocation, and breach triage.
5. Establish a breach workflow, processor notification channel, and the applicable supervisory-authority/data-subject notification decision process.

## 9. Legal risks requiring Greek/EU counsel

| Risk | Product response |
|---|---|
| Defamation/reputation harm from false claims about a school or identifiable teacher | First-hand experience rule, no unsupported criminal allegations, prompt report route, reasoned decisions, legal escalation and preservation only where necessary |
| Child or staff privacy and special-category information | Prohibit identifiers/sensitive details, minimize evidence, fast hide/redact process, trained Greek-language moderators |
| DSA obligations for hosting/online-platform services | Confirm service classification and applicable size exemptions; implement notice/action, reasoned notices, user contact/complaints, and any reporting obligations that apply |
| Consumer-protection risk from fake/incentivized reviews or misleading “verified” labels | No paid reviews; disclose ranking/score method; define verified badge precisely; do not present score as a quality guarantee |
| School interference, discrimination, or retaliation | Independent moderation, no school veto, audited overrides, transparent policy, legal escalation |
| Cross-border vendor/support/telemetry transfers | Data-flow inventory, processor contract review, regional configuration, transfer mechanism and access assessment |
| Inaccurate or excessive fraud profiling | No automated rejection, no opaque trust score, purpose limitation, short retention, human review and appeal |

These controls reduce risk; they do not replace a legal opinion, DPO review, or jurisdiction-specific advice.

## References

- [GDPR official text (Regulation (EU) 2016/679)](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng)
- [European Data Protection Board — DPIA](https://www.edpb.europa.eu/topics/accountability-and-compliance-tools/data-protection-impact-assessment_en)
- [Digital Services Act official text (Regulation (EU) 2022/2065)](https://eur-lex.europa.eu/eli/reg/2022/2065/oj/eng)
- [European Commission — DSA transparency and statements of reasons](https://digital-strategy.ec.europa.eu/en/policies/dsa-brings-transparency)
- [European Commission — Digital Services Coordinators](https://digital-strategy.ec.europa.eu/en/policies/dsa-dscs)
- [Greek Ministry of Digital Governance — DSA implementation information](https://digi.gov.gr/praksi-gia-tis-psifiakes-ipiresies/)
- Azure security and malware-scanning references are listed in [Phase2-Architecture.md](Phase2-Architecture.md#references).

**Source review date:** 2026-11-22. Confirm current legislation, Greek implementation, regulatory guidance, and Azure service behavior immediately before launch.

