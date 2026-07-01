# Simplify clause flow: auto-extract on upload, remove manual Load/Extract UI

## Context
User asked what the "Load" button on the clauses page did (manual re-fetch of already-extracted clauses via GET, separate from the "Extract clauses" button). User then said that manual flow isn't needed — the only required feature is: user uploads an agreement and clauses + risk scores are extracted automatically.

## Tried
Single approach, no dead ends:
1. `upload.component.ts`: `upload()` now awaits `api.extractClauses(result.id)` right after `api.uploadLease(file)` succeeds, before navigating to `/clauses?leaseId=X`.
2. `clauses.component.html`: removed Lease ID input, Load button, and Extract clauses button — only Export CSV action remains.
3. `clauses.component.ts`: removed `extract()` method and `extracting` signal (no longer triggered from this page); removed unused `FormsModule` import (no more `ngModel`). `loadClauses()` stays as the internal fetch triggered by the `leaseId` query param on route load.
4. `upload.component.html`: loading label changed to "Uploading & analyzing…" since the button click now blocks through the Groq extraction call, not just the file upload.
5. Removed now-unused `.lease-id-field` sass rule.

## Result
`npx tsc --noEmit` passes clean; Angular dev server (watch mode) rebuilt successfully on each edit with no errors.

## Decisions
- Kept `loadClauses()` as a private-ish loader (still called from the query-param subscription) rather than removing it — it's the only way the clauses page populates itself now.
- Did not touch the backend; `POST /api/leases/{id}/extract-clauses` already existed and is now just called from the upload flow instead of a separate UI action.
- Export CSV button kept since it's a distinct, still-wanted feature.

## Next steps
None outstanding.
