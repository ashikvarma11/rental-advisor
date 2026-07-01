# Clause extraction collapsing whole lease into a single clause

## Context
User uploaded `Lease_Copy1_Whitfield.pdf` and ran clause extraction; the entire document came back as one clause instead of being split into individual lease terms. Asked to find root cause, then to have Groq (not backend logic) generate the clause split + suggestions.

## Root cause
`LeasesController.ExtractClauses` (backend/Controllers/LeasesController.cs) split PDF text on `"\n\n"`. PdfPig's `page.Text` extraction (used in `ExtractTextFromFileAsync`) never emits blank-line paragraph breaks — it's a single continuous string per page joined with single `\n`s based on glyph position, especially for form-style PDFs with lots of table cells. So `Split("\n\n")` returned exactly one "paragraph" = the whole document.

## Tried
1. Regex-based fallback: split on numbered clause headings (`^\s*(\d{1,2})\.\s+\S`), matching the lease's actual "1. Application of the Act..." structure, with fallback chain (numbered headings → blank-line split → line split). Verified via `dotnet build`.
2. User then asked to send the PDF text to Groq and have the LLM do both the splitting and the risk scoring, rather than backend regex logic.

## Result
- Added `GroqClauseAnalyzer.ExtractClausesAsync` — sends full document text to Groq in one call, model returns `[{text, riskScore, suggestion}, ...]` JSON array (clause segmentation + scoring combined), with one retry on failure.
- `LeasesController.ExtractClauses` now tries `_aiAnalyzer.ExtractClausesAsync(content)` first when Groq is configured; only falls back to the regex/blank-line splitter + `AnalyzeBatchAsync`/rule-based scoring if Groq extraction fails or isn't configured.
- Tested end-to-end against the real Whitfield PDF (doc id 22): Groq extraction produced 30 well-formed clauses with varied risk scores (0.0–0.8) and suggestions, vs. 1 clause before the fix. Confirmed via backend log (`POST https://api.groq.com/...`) and clause API response.

## Decisions
- Kept the regex/heading-based splitter (`SplitIntoClauses`) as a fallback path rather than removing it — needed when `Groq:ApiKey` isn't configured or the API call fails, so the feature degrades gracefully instead of breaking entirely.
- Did not change `ExtractTextFromFileAsync`/PdfPig usage — the paragraph-break assumption was only wrong in the splitting logic, not the extraction itself.

## Next steps
None outstanding. Feature verified working with live Groq calls against a real lease PDF.
