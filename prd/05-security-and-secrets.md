# PRD — Security, Secrets & Data Protection

Secrets handling
- Never store API keys or secrets in the frontend build or `environment.*.ts` files.
- Backend-only secrets: store ABR API key (and any LLM/provider keys) as environment variables consumed by the ASP.NET Core app.

Local development
- Use `dotnet user-secrets` for local development secret storage, or a local `.env` file referenced by `docker-compose` (ensure `.env` is in `.gitignore`).

Production / CI
- CI secrets: store in GitHub Actions Secrets and inject into the build/deploy pipeline.  
- Runtime secrets: prefer a secrets manager like Azure Key Vault or GitHub Secrets + runner injection. Avoid baking secrets into images.

Backend proxy pattern
- Implement ``GET /api/enrich/abn/{abn}`` endpoint which reads `ABR__ApiKey` from configuration and forwards requests to ABR. The frontend only calls the backend endpoint.

CORS, HTTPS and access control
- Enable CORS only for allowed frontend origins.  
- Enforce HTTPS (redirect and HSTS for production).  
- Consider simple JWT auth if you want a gated demo; otherwise, keep public but rate-limited.

Rate-limiting & caching
- Rate-limit calls to third-party APIs (ABR) to avoid quota exhaustion. Use server-side cache (in-memory with TTL) for ABN lookup results.

Data protection & privacy
- Lease documents and uploaded CSVs may contain PII. For the demo:
  - Store uploaded files in a secure server directory or in the DB as blobs with access controls.
  - If including real data, seek consent and remove PII; prefer seeded anonymised demo data.
  - Encrypt backups of the SQLite file at rest (host-level) if deployed publicly.

Secrets naming conventions (examples)
- `ABR__ApiKey` — ABR service API key (backend-only).
- `OPENAI__ApiKey` or `AI__Provider__ApiKey` — AI provider key (backend-only).
