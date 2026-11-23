# SchoolWise Phase 2 User Flows

**Status:** UX design only. These screens and flows are not implemented.

## 1. Parent: verification status

**Purpose:** Explain exactly what “verified parent” means, collect the minimum acceptable evidence, and show a useful status without exposing internal risk signals.

**Screen components**

- Selected school name/type and eligibility period.
- Plain-language explanation of verification purpose, who reviews the evidence, and planned deletion time.
- Method selector limited to methods actually enabled for that school; do not show unsupported methods as usable.
- Document instructions, accepted file types/size, redaction checklist, child privacy warning, upload progress, and remove/replace action.
- Request status/timeline: Draft → Pending scan → Pending review → Approved/Rejected/Expired.
- User-facing rejection reason, correction instructions, expiry date, and secure help/contact action.

**Parent actions**

1. Select the school and confirm they are a parent/legal guardian with current or recent experience.
2. Read the notice and upload one supported document, redacting unnecessary child/payment information.
3. Confirm that the evidence is theirs and submit the request.
4. See a receipt and return later; do not repeatedly resubmit while pending.
5. On rejection, correct the issue or use the appeal/contact route. On expiry, request renewal if they want to submit another review.

**States:** no request, upload in progress, pending scan, pending moderator, approved with expiry, rejected/retry, expired/renew, revoked/contact support, and safe service failure. Never display a raw SAS URL or staff note. Email confirmation verifies account inbox control only; it is not shown as parent verification.

## 2. Parent: write a review

**Purpose:** Collect a first-hand, useful account of the school experience without inviting disclosure of children's identity or sensitive details.

**Screen components**

- School context and verified-eligibility indicator.
- Overall 1–5 rating.
- Optional category ratings: teaching, safety, communication, facilities, activities; each can be “I can't assess this.”
- Pros, cons, review text, and language.
- Guidance with examples of specific, fair, first-hand feedback; no names of children, teachers, or other families; no medical/diagnostic details or unsupported allegations.
- Save-draft, preview, submit, and final visibility/retention explanation.

**Parent actions**

1. Start only after the parent-school verification is approved.
2. Complete overall rating and optional category ratings; add text.
3. Save a draft or preview the exact public content. The UI can warn about possible identifiers but must not silently rewrite text.
4. Confirm the review is first-hand, disclose a material employment/financial conflict, and affirm the publication choice.
5. Submit the immutable version for moderation and view its queue status.

**Statuses:** Draft, Pending moderation, Published, Rejected, Archived. A parent cannot edit the exact version currently in moderation. If a published review is edited, the last approved version remains public until the new version is approved; the page should make this clear. A rejected edit does not erase the old published version. Delete immediately unpublishes the review.

## 3. Parent: My Reviews

**Purpose:** Give the parent control and a clear audit of their own content.

**Components:** School, current workflow status, last update, current-version preview, public-version preview (if any), decision reason, edit/resubmit, appeal, and delete controls.

**Actions:** Open/edit only Draft or Rejected content; view but do not mutate a Pending version; edit a Published review by creating a new version; appeal a decision; delete/archive a review. The page never reveals moderator-only notes or internal fraud signals.

## 4. Public: school reviews and rating

**Purpose:** Help families compare experience while setting honest expectations about sample size and verification.

**Components**

- “Verified parent reviews” section and the definition of the badge.
- Overall score (one decimal) only after at least five distinct verified reviewers; otherwise a “Not enough reviews for a score” message plus an accurate count.
- Category score and category-specific response count; suppress scores with inadequate sample size.
- Recent approved reviews, overall rating, available category ratings, publication date, language, and report link.
- “Verified parent at time of review” label and note that moderation checks policy and eligibility, not factual truth or school quality.
- Empty states distinguishing no reviews, reviews pending, and too few reviews for a score. Do not reveal pending-review counts or author identities.

**Visitor actions:** Browse newest first, open a public review, and report an issue after authentication. No public comments, replies, reviewer profiles, or unverified star ratings in the MVP.

## 5. Moderator: review queue

**Purpose:** Process review versions consistently, without exposing unnecessary reviewer identity.

**Queue components**

- Pending age, school, locale, version, new submission versus edit, queue priority, and assigned moderator.
- Filters by age, school, language, and policy risk category; stable cursor pagination.
- Review detail with exact text/ratings, only relevant prior approved version, conflict flags, and policy checklist.
- Approve, reject/request edit, escalate, and internal-note actions. User-facing decision reason is a separate required field.
- Clear empty/loading/error states and stale-version conflict handling.

**Moderator actions:** Review the exact version; choose a controlled outcome/reason; add concise internal note only when needed; commit and notify the parent. Do not edit the parent's content or decide a related verification they personally handled.

## 6. Moderator: verification queue

**Purpose:** Decide whether evidence supports a parent-school relationship and maintain a limited expiry.

**Queue components**

- Request age, school, method, file count/type/size, scan state, previous request count, and case assignment.
- Evidence viewer only after authorization, clean scan, recent authentication, and access-audit write.
- Checklist for school attribution, academic period, minimum parent/guardian match, document quality, and expiry.
- Approve/reject/escalate actions, safe reason templates, and automatic document deletion deadline.

**Moderator actions:** Approve only the relationship/period evidenced; reject with a user-safe explanation; escalate uncertain or high-risk cases. Never copy child names or financial values into notes; never send proof to a school for confirmation without a separately designed lawful basis and notice.

## 7. Moderator: reports and appeals

**Purpose:** Handle post-publication privacy, fraud, policy, and legal concerns with a traceable route.

**Components:** Report category, date, reported version, reporter context minimized, prior decisions, evidence (if any), priority/SLA, and decision/appeal history.

**Actions:** Dismiss, request more information, temporarily restrict/hide when needed, remove/restore via a new decision, or escalate to legal/safeguarding. Notify each affected party with the appropriate level of detail; never disclose the reporter to the review author by default.

## 8. End-to-end flows

### A. Verify, then publish first review

1. Parent signs in and confirms email.
2. Parent requests verification for an approved school and uploads redacted proof.
3. File remains in private quarantine until scan is clean and metadata/size are validated.
4. Moderator reviews the request; approval creates an expiring parent-school grant. Parent sees the status and expiry.
5. Parent drafts and previews a review, affirms first-hand experience and public visibility, then submits.
6. Moderator reviews the exact version and either approves, rejects with a reason, or escalates.
7. On approval, publish the version and update the school summary/cache atomically. Show “Verified parent at time of review.”
8. Delete the document after the decision/appeal retention window.

### B. Edit an approved review

1. Parent opens their own public review and creates a new version.
2. The old approved version stays visible while the edit is pending.
3. Moderator approves: atomically switch public version and mark old version superseded.
4. Moderator rejects: preserve the old approved version and give the parent a correction/appeal route.

### C. Report and remove

1. Parent or school contact files a report with a reason and optional evidence.
2. Queue prioritizes exposed personal data, credible threats, and legal notices.
3. Moderator checks the exact published version; if necessary, temporarily hides it and records why.
4. Moderator decides and sends the affected author a reasoned notice and appeal/contact path.
5. Cache is invalidated and the decision/report retention policy applies.

### D. Delete account/review

1. Parent requests deletion or deletes an individual review.
2. Unpublish immediately, invalidate cached pages, cancel pending verification, revoke grants/tokens, and enqueue document deletion.
3. Apply the approved erasure/retention schedule; keep only any narrowly justified, restricted audit record.
4. Confirm completion or explain a specific legal retention exception.

