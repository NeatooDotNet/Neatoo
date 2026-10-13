# Sync, then fresh-session test

**Plan #:** 007
**Date:** 2026-10-09
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-8, AC-9
**Status:** In Progress — waiting on the RemoteFactory skill sync
**Last Updated:** 2026-10-12 (drafted from the stub after SKR-006 merged)
**Plan-review opt-in:** No — no code; the deliverable is the eval and the fixes it produces
**Code-review opt-in:** No
**Branch:** skr-007-sync-and-verify — cut from the arc at Step 2
**PR:** —

---

## Scope

Copy `skills/neatoo` and `skills/mudneatoo` to `~/.claude/skills/`, confirm the copies are byte-identical, and run `dotnet mdsnippets` and the hand-written-block count one last time. Then test the skill on a fresh session, not on the orchestrator: launch a fresh-context agent from a directory that does not contain this repository's `CLAUDE.md` (so no doctrine leaks in beside the skill), give it only the two installed skills (neatoo and RemoteFactory, the latter once the sibling todo has synced it) and the five zCRM scenarios listed under the habit-trap table, and score its output row by row against that table. Every row it falls into becomes a punchlist fix to the skill; re-sync and re-run until it falls into none (AC-9). The orchestrator's own cold read is not evidence. A repeatable eval suite (`claude plugin eval`) is a sibling todo if wanted, not this plan.

---

## Intent

- The installed skills are the repository's skills, byte for byte.
- A session that knows only the two skills builds the five zCRM scenarios without reaching for any habit in the trap table.

---

## Framework & Architectural Alignment

- Owner split (2026-10-09, AC-4 narrowed 2026-10-11): a Neatoo-owned row the session falls into is fixed in `skills/neatoo` or `skills/mudneatoo` on this plan's branch; an RF-owned row is reported to the sibling todo (desktopRemoteFactory), not fixed here.
- Fixes follow the same rules as 004–006: compiled regions only, no ruling IDs in skill prose, the model before prohibitions.

---

## Constraints & Invariants

- The session under test sees no `src/Design`, no repository `CLAUDE.md`, no project memory, and no conversation with the orchestrator — only the installed skills and the scenario text.
- The scenario prompt states the business need, not the answer: it never names a trap, a Neatoo type or an attribute.
- The scorer is the habit-trap table as written in `todo.md`; a row is "fallen into" when the output does what the row's question assumes.

---

## Steps

1. Sync: copy both skills; `diff -rq` empty for each. (Held until the RemoteFactory skill is ready, so the installed pair is coherent for every session on this machine.)
2. Run: from a scratch directory outside the repository, a headless `claude -p` session per scenario, with the five scenarios' prompts recorded verbatim in the review file.
3. Score each output against T1–T20; record row → verdict → quoted evidence in `reviews/007-fresh-session.md`.
4. For each Neatoo-owned row fallen into: a punchlist fix on this branch, re-sync, re-run that scenario. RF-owned rows: one message to desktopRemoteFactory with the evidence.
5. Gate: the test-reviewer checks the scoring is honest (each verdict quotes the output it rests on).

---

## Acceptance

- [ ] `~/.claude/skills/neatoo` and `~/.claude/skills/mudneatoo` are byte-identical to `skills/` `[explicit-skip: diff -rq]` · Should
- [ ] A fresh session with only the two installed skills falls into no Neatoo-owned trap row on the five zCRM scenarios; any RF-owned row it falls into is reported to the sibling todo `[explicit-skip: scored transcripts in reviews/007-fresh-session.md]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-12 on `skr-arc` at `864ab78`.

- `dotnet mdsnippets` clean. Hand-written non-WRONG C#/Razor blocks across both skills: one, `skills/neatoo/references/lazy-loading.md:251` (pending removal, D24).
- Installed copies differ from `skills/`: 16 entries under `neatoo`, 4 under `mudneatoo`.
- RemoteFactory skill: the installed copy differs from `RemoteFactory/skills/RemoteFactory`; no RF skill-rebuild todo or branch is visible in the RF repository (its active todo is EXSET). Status asked of desktopRemoteFactory 2026-10-12.

---

## Punchlist

-

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Confirmed |
|---|---|---|---|---|
| Installed copies byte-identical | Should | `[explicit-skip]` | | |
| Fresh session falls into no Neatoo-owned row | Must | `[explicit-skip]` | | |

---

## Gate Record

-

---

## Plan Amendments

### 2026-10-12 — Drafted from the stub

- **Section affected:** Steps, Acceptance, Constraints
- **Original said:** sync, final checks, fresh-session test against both skills, fix and re-run until no row is fallen into.
- **What changed:** the run mechanism is a headless `claude -p` per scenario from a scratch directory (no repository `CLAUDE.md`, no project memory); the sync is held until the RF skill is ready, so other sessions on this machine never see the new neatoo skill pointing at RF answers the installed RF skill lacks; RF-owned rows are reported, not fixed.
- **Why:** pre-flight; the RF skill is not yet rebuilt.

---

## Notes

- Open question to the user/RF session: run now against the RF skill as installed and treat RF-owned misses as findings for the sibling, or wait for the RF rebuild.
