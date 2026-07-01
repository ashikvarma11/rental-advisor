**Consider yourself as a 10 year experienced Angular developer. While implementing code, make the absolutely necessary changes only.**
**DO NOT Over engineer or over-abstract. Use short-hand notations.Use minimal readable expressions.Favor clarity over cleverness. Focus on the specific task at hand and make only the necessary changes to achieve the desired outcome.**

Check if tasks can be done in parallel to reduce time. If so, spawn sub-agents to handle them. Each sub-agent should be assigned a specific task and report back with their findings. The main agent should then compile the results and make the final decision.

## Session logging

Location: store all session files in `.claude/session-decisions/`.
Naming: `YYYY-MM-DD-<short-topic-slug>.md` (e.g. `2026-06-16-auth-refactor.md`).

At the START of any task:
1. Read `.claude/session-decisions/INDEX.md` if it exists. It contains a one-line summary per session file with links.
2. If the current task relates to an existing entry, open that session file and read it fully before making changes.
3. If no related entry exists, this is a new session.

During and after the task, write/update the session file with these sections:
- Context: what was asked, why
- Tried: each approach attempted, in order
- Result: outcome of each attempt (worked / failed / partial, with error messages if relevant)
- Decisions: choices made and the reasoning
- Next steps: what remains, open questions

## Important
- starting and stopping frontend and backend servers should be automatically done by yourself.
- only ask if you are in dilemma. If you need answers, check the code first.
If you are working on the same issue for 10 minutes, then stop and ask for confirmation.
**Use compact when context usage hits 200k**
