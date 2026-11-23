# SchoolWise School Catalog Bootstrap

## Goal and scope

Build a useful, accurate Greek school directory before building an importer or a school portal. Start with 100 independently checked school units in a small geographic cohort. This sprint delivers the process and CSV specification only: there is **no CSV upload endpoint, automated crawler, claiming UI or school dashboard**.

Prefer factual details from the school itself or an official directory whose reuse terms have been checked. Public availability does not establish permission to republish text, photos or logos. Record source URLs and dates, write original descriptions, and never invent fees, capacity, facilities or enrollment information.

## 1. Manual onboarding

1. Choose a cohort: Athens and nearby municipalities, covering public/private kindergarten and elementary units.
2. Record a candidate in the preparation CSV below, including its source and education stage.
3. Verify name, address, city, type and ownership with an official source or an authorized school contact. If sources disagree, keep the candidate out of the published catalog until resolved.
4. Check duplicates against the existing catalog by registry identifier where available, then normalized name + address + stage. Do not merge separate education units merely because they share a brand or campus.
5. Use `/admin/schools` -> Add school. Save as **Draft**, with the canonical factual fields; do not copy import-only provenance into the public description.
6. Save the returned SchoolWise ID and generated slug in the preparation register. The current API does not store an external import key.
7. A second check, or an explicit separate review pass by the same operator, confirms accuracy, permitted media and no personal information. Mark Approved only then.
8. Open the public detail page; test search/type/ownership filters and contact links. Record the result and the next check date.
9. Accept correction requests through an accountable manual support contact. Verify the source before editing; publication is not an endorsement or a school claim.

Do not include staff personal contacts, child details or parent enrollment records. Use a publicly designated school office contact. A catalog entry must never create synthetic reviews for a real school.

## 2. Import data model

Keep two clearly separate layers:

| Layer | Fields | Storage now |
|---|---|---|
| Canonical school | Existing School DTO fields below; `photos` if permitted | Existing `schools` / `school_photos` tables, via Admin CRUD |
| Intake/provenance | `importKey`, `sourceSchoolKey`, `sourceUrl`, `checkedAt`, `reuseBasis`, `operator`, `intakeStatus`, `schoolwiseId`, `schoolwiseSlug`, `nextCheckAt`, `notes` | Restricted preparation CSV/register; not public API fields |

Suggested intake statuses: `Candidate`, `NeedsCheck`, `Ready`, `ImportedDraft`, `Approved`, `Duplicate`, `Rejected`. These are operational preparation labels, not new SchoolWise enums.

The domain has one `SchoolType` per school record. Use one row per official education unit/stage (for example, kindergarten and elementary sections are distinct rows even at one campus). Prefer `importKey = source + registry-unit-id + stage`; if no registry key exists, issue a stable local intake ID after a human identity check. Do not deduplicate solely by slug: the API may append a suffix for duplicate names.

For a later importer, persist a restricted source-to-SchoolWise mapping with unique `(source, sourceSchoolKey, stage)` and batch ID. This enables idempotent updates without changing the public School DTO. No such mapping table is implemented in this sprint.

## 3. CSV format

- UTF-8, comma-separated, header row, RFC 4180 quoting, one school unit per row.
- Booleans are `true` / `false`; decimal amounts are euros with a dot and no thousands separator.
- Empty optional cells map to null or the DTO's empty string; never substitute invented values or zero for unknown tuition.
- `schoolType` must be exactly `Nursery`, `Kindergarten`, `Elementary`, `MiddleSchool`, or `HighSchool`.
- Postal codes and phone numbers are strings, so retain leading zeros.
- `photoUrls` is an optional pipe-separated list of permitted absolute HTTP(S) URLs, ordered as entered (max 30).
- `isApproved` starts false. Operator approval is a separate step, not a value accepted blindly from an external source.

```csv
importKey,name,schoolType,address,city,isPrivate,isApproved,postalCode,phone,email,website,description,tuitionFrom,tuitionTo,studentCapacity,establishedYear,logoUrl,photoUrls,sourceSchoolKey,sourceUrl,checkedAt,reuseBasis,schoolwiseId
demo-001,Sample Athens Kindergarten,Kindergarten,1 Demo Street,Athens,true,false,11527,,,,Fictional format example only,,,,,,,,,2026-11-23,synthetic example,
demo-002,Sample Public Elementary,Elementary,2 Demo Street,Athens,false,false,10431,,,,Fictional format example only,,,,,,,,,2026-11-23,synthetic example,
```

These rows are fictional examples, not schools to publish. Greek names/addresses are supported in UTF-8. In manual onboarding, the CSV is a preparation aid; copy canonical fields into the Admin form and retain provenance separately.

## 4. Required canonical fields

| Field | Rules |
|---|---|
| `name` | Nonblank, <=200 characters; current official/common unit name |
| `schoolType` | Supported enum; one stage per record |
| `address` | Nonblank, <=300 characters; verify actual school location |
| `city` | Nonblank, <=120 characters; use a consistent municipality spelling |
| `isPrivate` | Explicit boolean; verify ownership, do not infer from tuition |
| `isApproved` | Explicit operator decision; false at intake |

When using the API, provide `photos: []` if there are no permitted photos. The server generates UUID, slug and creation timestamp. Input has no authority to choose an existing internal ID.

Provenance is additionally required by this process (`importKey`, `sourceUrl` or a documented school contact, `checkedAt`, `reuseBasis`), although it is not enforced by the current School API.

## 5. Optional canonical fields

| Field | Notes |
|---|---|
| `description` | Original factual prose, <=5,000 characters; no unsupported rankings |
| `postalCode`, `phone`, `email` | Verified office contact; valid email when populated |
| `website` | Absolute HTTP(S), <=500 characters; confirm it belongs to the unit |
| `tuitionFrom`, `tuitionTo` | Non-negative annual euros, lower <= upper; verify year and inclusions with school; otherwise leave blank |
| `studentCapacity` | Positive integer if known, not an estimate |
| `establishedYear` | 1800 through current year if sourced |
| `logoUrl` | Absolute HTTP(S), <=1,000 characters, permitted use only |
| `photoUrls` | Up to 30 permitted images; translated to `{url, sortOrder}` DTO items |

The current schema has no tuition currency/year/inclusions fields, coordinates or import source fields. Record caveats in the restricted register; omit ambiguous fees rather than misrepresent them. Do not add unimplemented columns to API requests.

## 6. Importing the first 100 manually

1. Assemble 120-150 candidates to allow duplicate/incomplete records to be rejected.
2. Clean municipality names and identify education units. Collect only verified minimal fields first.
3. Work in five batches of 20. Mark each record as ImportedDraft only after recording its internal ID.
4. Review each batch and publish independently; do not wait for a complete 100-school batch to launch useful search.
5. Spot-check every public record's name/location/type and office links; independently re-check a sample for consistency. Never publish records still marked NeedsCheck.
6. Track coverage by municipality, stage and ownership; avoid 100 records concentrated in one campus/brand.
7. Final acceptance: 100 distinct units, all required fields checked, source/date present, no duplicate internal identities, no fabricated media/fees/reviews, and all public pages usable.

Budget roughly 6-10 minutes per already sourced row (10-17 operator hours for 100), plus source research, contact delays and a final QA pass. Measure the first batch before committing to a deadline. Do not onboard 100 by copying unchecked directory data.

## 7. Importing 1,000 later

Build a small Admin-only batch import tool only after the manual format and sources are stable:

- Validate encoding, headers, values, URL formats and ownership before any writes; reject unknown columns rather than guessing.
- Stage rows, record provenance and show a **dry-run** of creates, updates, duplicates and errors.
- Apply the same validators and school handlers as Admin CRUD; do not bypass approval rules with direct SQL.
- Use the restricted source mapping for idempotency and stable IDs. Never overwrite an operator correction without showing the proposed change.
- Process bounded batches (for example, 50-100 rows), with explicit per-row outcomes and resumable batch IDs. A failed row must not be reported as imported.
- Save new entries as Draft; retain manual approval. Preserve reviews and their school IDs on updates.
- Back up and test restore before import; keep a change log and a plan to revert imported field changes without deleting review history.
- Rate-limit writes, cap file size/rows and restrict the tool to authorized staff. No school dashboard, claiming or scraping engine is needed for this step.

Choose licensed bulk/open data or agreements with school networks only after verifying completeness and reuse terms. Do not assume an official source supplies a stable API or a redistribution license.

## 8. Future school claiming (not implemented)

1. An applicant selects the existing canonical school; claiming never creates a duplicate listing.
2. A platform operator verifies authority through the school's independently obtained office contact and suitable organizational evidence. Email-domain ownership alone is insufficient.
3. An Admin grants a **school-scoped** management assignment with an audit trail, expiry/review date, MFA and a recovery/revocation process.
4. School users can propose factual listing corrections/media through review; they cannot approve, delete, identify or veto parent reviews.
5. Keep ownership disputes and role changes separate from public catalog approval. Preserve independent moderation, source history and existing school/review IDs.

Do not reuse Parent verification codes for school claiming or grant the global Admin role to a school representative.
