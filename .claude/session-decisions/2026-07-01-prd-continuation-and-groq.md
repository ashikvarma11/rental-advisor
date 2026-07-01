## Context
User asked to check the PRD and continue implementation. Weeks 1-3 (data model, CSV import, comparator API, lease upload/clause extraction, ABR stub, basic Angular pages) were already substantially built but uncommitted. User chose to fix bugs/gaps before UI polish. Later asked to integrate a free third-party LLM for clause analysis.

## Tried
1. Audited backend/frontend against PRD weekly plan; found 5 gaps: no listings CSV upload+validation endpoint, docker-compose pointed at nonexistent `./frontend` (real dir is `rental-advisor-frontend/`), ABR enrichment was a hardcoded stub, frontend API URL hardcoded to localhost, naive CSV parsing (no quoted-field support).
2. Fixed all 5: added `POST /api/import/listings/upload` with per-row validation, `CsvParser` helper, frontend Dockerfile + nginx reverse proxy + docker-compose fix, real ABR JSON API call in `EnrichController`, Angular `environment.ts`/`environment.prod.ts` with `fileReplacements`.
3. Chose Groq (free tier, OpenAI-compatible) for LLM clause analysis per user's provider preference. Built `GroqClauseAnalyzer` service, wired into `LeasesController.ExtractClauses` with fallback to existing keyword heuristic when unconfigured/failing.
4. User pasted a live Groq API key directly in chat — refused to echo it into files/commands; had them set it via `dotnet user-secrets` instead (repo-root `appsettings.json` was explicitly rejected as a storage option per PRD's `05-security-and-secrets.md`, which forbids secrets in committed config).
5. End-to-end test of clause extraction kept failing/falling back. Root-caused three separate pre-existing/newly-introduced bugs (see Decisions).

## Result
- All 5 gap fixes: done, `dotnet build` and `npm run build` both pass.
- Groq integration: working end-to-end, confirmed via live API call in logs and a risk score (0.2) that differs from the rule-based fallback (which always scores this sample lease at 1.0 with fixed text).

## Decisions
- **Broken migration bug (pre-existing, unrelated to today's work but blocking testing):** `backend/Migrations/20260613060000_AddIsResolvedToClauses.cs` was hand-written without the `[Migration]`/`[DbContext]` attributes and no matching `.Designer.cs`, so EF Core never recognized it — `Database.Migrate()` silently no-op'd on it while the model snapshot had been hand-edited to pretend it was applied. Fixed by reverting the snapshot to git HEAD, deleting the broken file, and using `dotnet ef migrations add` to scaffold it correctly (had to do this twice — first attempt used `--no-build` against a stale assembly and produced an empty migration; second attempt with a fresh build produced the real `AddColumn` operation). Cleaned up a stray `__EFMigrationsHistory` row left by the empty-migration attempt.
- **Config key bug:** code read `_config["Groq__ApiKey"]` / `_config["ABR__ApiKey"]` (double-underscore, the env-var-only nesting convention). `dotnet user-secrets` stores colon-separated keys (`Groq:ApiKey`), which a literal double-underscore lookup never matches. Fixed both to use colon syntax (`Groq:ApiKey`, `ABR:ApiKey`), which works for appsettings.json, user-secrets, and env vars alike (the env var provider auto-translates `__` to `:` internally).
- **Missing launchSettings.json:** no `Properties/launchSettings.json` existed, so bare `dotnet run` defaulted to the Production environment, and ASP.NET Core only auto-loads user-secrets in Development — so the Groq key was silently invisible regardless of the two fixes above. Added a standard `launchSettings.json` defaulting to Development on port 5050.
- User declined to rotate the pasted Groq key ("its fine, continue with existing key") — noted, not pursuing further, but this is a live credential that was pasted in plaintext chat.
- Same-machine process control: confirmed that even though the user starts `dotnet run` in their own terminal, this agent can see/stop/restart it via `tasklist`/`taskkill` since it's the same OS process table — used this repeatedly to unblock file locks during migration fixes and to test with the user's own user-secrets-scoped key.

## Next steps
- UI/UX pass deferred (frontend still Angular CLI placeholder page, no nav, no CSV-upload UI) — user explicitly chose bugs-first over this earlier.
- No automated tests exist yet (Week 4 PRD deliverable) — not started.
- Frontend Dockerfile/nginx config added but not yet tested via actual `docker-compose up`.
- Groq key should eventually be rotated (user's call, declined for now).
