# Isolated fresh-session test, then sync

**Plan #:** 008
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-9
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

Sync `~/.claude/skills/neatoo` and `mudneatoo` to the repository, then run SKR-007's five zCRM scenarios plus new fact-versus-verb scenarios headless, each with only the installed skills, using the isolated mechanism RFSK-006 proved: `--permission-mode dontAsk`, `--strict-mcp-config`, skills-only Read/Glob/Grep allowed, Bash and every source path denied, from an empty directory with stdin from /dev/null. Probe the isolation first. Score against the trap table and the fact-intent-consequence choice; fix and re-run what falls.
