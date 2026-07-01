# PRD — Backend APIs & Integration Details

API surface (MVP)
- `POST /api/import/listings` — upload CSV of listings. Validates and inserts `Listing` records.
- `GET /api/compare/suburb?suburb={}&postcode={}` — returns suburb median, comparison metrics, and historical trend (if available).
- `POST /api/leases/upload` — upload lease document (PDF/TXT). Returns lease id.
- `POST /api/leases/{id}/extract-clauses` — runs clause extraction (rule-based + LLM) and stores `Clause` records.
- `GET /api/enrich/abn/{abn}` — ABR enrichment proxy endpoint. Reads `ABR__ApiKey` from backend config.

ABR proxy details
- Endpoint: `GET /api/enrich/abn/{abn}`
- Backend reads env var `ABR__ApiKey` (or `ABR_API_KEY`) and performs authenticated call to ABR service.
- Cache ABR responses in-memory or DB with TTL (e.g., 24 hours) to reduce API usage.

Sample ABR response shape (normalized)
```json
{
  "abn": "12345678901",
  "name": "Example Pty Ltd",
  "status": "Active",
  "mainBusinessLocation": "NSW",
  "lastUpdated": "2026-05-01"
}
```

Backend configuration
- EF Core SQLite connection string example (appsettings.Development.json):
  - `ConnectionStrings:DefaultConnection = "Data Source=Data/rental.db"`
- ABR key config: `ABR__ApiKey` (set in environment or secrets manager).

Caching & rate-limiting
- Use `IMemoryCache` for short-term ABR results. Consider persistent DB caching for long-term.
- Apply rate-limiting middleware for endpoints that proxy third-party APIs.

LLM / AI integration
- LLM connectors must be configured via environment variables (e.g., `AI__Provider`, `AI__ApiKey`) and live only in backend.
- Provide an abstraction layer so rules-based extraction can be swapped for LLM-enhanced extraction.

Logging & observability
- Log third-party API calls (sanitised) and cache hits/misses.
- Add health endpoint `GET /health` for container orchestration readiness checks.

Security
- Validate ABN inputs and sanitise responses.
- Do not echo provider API keys to logs.
