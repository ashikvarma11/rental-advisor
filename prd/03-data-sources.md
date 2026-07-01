# PRD — Data Sources & Integration Strategy

Authoritative data sources (MVP)
- ABS / data.gov.au CSVs — median rent and regional rental statistics.  
  - Approach: include curated CSVs in `repo/data/` and provide an importer to seed `SuburbStats`.
- ABR (ABN lookup) — live enrichment API for landlord/agent entity metadata.
  - Approach: backend-only ABR client; store `ABR__ApiKey` in server env; implement caching and rate-limiting.

State tenancy guidance (informational snippets)
- NSW: Fair Trading tenancy pages (use as canonical summaries).  
- VIC: Consumer Affairs Victoria — rent and tenancy guidance.  
- QLD: Residential Tenancies Authority (RTA) pages + web services for bond lookups (specific endpoints available).
  - Approach: do not attempt to scrape entire legislation. Instead, curate short canonical snippets and link to official pages. Store snippets in `repo/prd/legal-snippets/*.md` or as DB seed rows.

Tax guidance
- ATO pages for GST and rental property guidance: use static links and short excerpts for advisor hints.
- Developer portal `developer.ato.gov.au` may expose APIs; out of scope for MVP except as a future integration.

Commercial/third-party providers
- Domain, realestate.com.au, SQM: richer listing and suburb analytics, but commercial agreements required — out of scope for one-month demo.

Data licensing and reproducibility
- Use only openly licensed government datasets in the repo (ABS/data.gov.au) or provide linking to public pages.
- Keep a `data/README.md` describing sources, retrieval dates, and citations.

Seed data location (recommended)
- `repo/data/abs_median_rent.csv` — curated CSV used by importer.
- `repo/data/sample_listings.csv` — example listing uploads for demo flow.

Notes on reliability
- ABS releases may be periodic; design importer to accept manual CSV updates and clearly indicate source/time in UI.
