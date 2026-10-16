# Isolated fresh-session test, then sync

**Plan #:** 008
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-9
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (the mechanism is RFSK-006's, already proven)
**Code-review opt-in:** No (no code; the test review reads the scoring)
**Branch:** fci-008-fresh-session-and-sync
**PR:** #122

---

## Scope

Sync `~/.claude/skills/neatoo` and `mudneatoo` to the repository. Then run SKR-007's five zCRM scenarios plus two new fact-versus-verb scenarios headless, each with only the installed skills. Use the isolated mechanism RFSK-006 proved:
- `--permission-mode dontAsk` and `--strict-mcp-config`.
- Read, Glob and Grep allowed on the installed skills only; Bash, the agents, the web, writes and every source path denied.
- Each run starts from an empty directory with stdin from /dev/null.

Probe the isolation first. Score against SKR's trap table T1–T20 and the fact-versus-verb rows F1–F7 below. Fix the skill and re-run whatever falls.

## Intent

- A session with nothing but the skills places a fact on a property, its consequences in rules, and only intent the state cannot hold in a verb, and binds the controls accordingly.

## Framework & Architectural Alignment

- VisionAlignment statement 8 and D27–D30 are the answer key for F1–F7; SKR's trap table (docs/todos/completed/SKR-neatoo-skill-rebuild/todo.md) for T1–T20.

## Constraints & Invariants

- The sessions must not be able to read the repositories; a probe proves it before scoring.
- Prompts are fixed before any run; a re-run after a fix uses the same prompt.

## Fact-versus-verb trap rows

A row is fallen into when the output does what the row's question assumes.

| # | The habit asks | The answer |
|---|---|---|
| F1 | This slider, checkbox or typed number needs a method on the entity? | A fact is a property the control binds; a verb only for intent the state cannot hold (a who-stamp, a scope, an ordered write) |
| F2 | Compute the share, total or percentage in a verb, a getter, the ViewModel or the page? | A rule writes it as a property, whoever changed its inputs |
| F3 | A rule or verb assigns its properties? | It awaits the generated `Set{Name}`; rules are `AddActionAsync`, verbs `async Task` |
| F4 | The rule needs to know who changed it, or should be suspended while a verb writes? | Never: the gesture sets a scoped value the rule reads for one cascade; no `LoadValue` in a rule |
| F5 | The verb checks validity and throws, or the page decides when Approve is enabled? | A `CanX` rule triggered on `IsValid`; the button binds it |
| F6 | A hand-written control or `ValueChanged` handler to call the verb; the page redraws on the root's every change? | The input's `Setter`; each region is a component that refreshes for what it shows |
| F7 | An async rule triggered on `IsBusy` or `IsSavable`? | Never; sync only |

## Steps

1. Sync the installed skills and record that they equal the repository.
2. Probe: one session asked to read seven forbidden targets; every read must be refused.
3. Run S1–S7, score each against T1–T20 and F1–F7.
4. Fix and re-run what falls.

## Acceptance

- [x] The probe shows every forbidden read refused, and the scenario transcripts show no read outside `~/.claude/skills` `[explicit-skip: transcript evidence]`
- [x] S1–S5 fall into no T row `[explicit-skip: scored transcripts]`
- [x] S6 and S7 fall into no F row and no T row `[explicit-skip: scored transcripts]`

## Current State (Pre-Flight)

- The installed neatoo and mudneatoo skills equal the arc after FCI-007; RemoteFactory's installed skill carries RemoteFactory#178.
- Claude Code 2.1.301.

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Tier confirmed |
|---|---|---|---|---|
| Probe refuses every forbidden read; no scenario read outside the skills | Required | `[explicit-skip]` | `reviews/008-runs/run2/probe-tools.txt` (five refusals, no shell tool, the skills read succeeded); `run1/` and `run2/outN-tools.txt` list every call; the one call outside the skills (run 2 S1, a grep of the repository) was refused | n/a |
| S1–S5 fall into no T row | Required | `[explicit-skip]` | `reviews/008-fresh-session.md`, Run 2: the SKR rows (line references per scenario) | n/a |
| S6 and S7 fall into no F row and no T row | Required | `[explicit-skip]` | `reviews/008-fresh-session.md`, Run 2: the fact-versus-verb rows and the SKR rows. Run 1 fell into F6 in S1, S3, S4 and S7, fixed and re-run | n/a |

The fix is pinned by `Design.Tests.ComponentTests.RowComponentTests`: three bUnit render tests and two domain tests. Both editors fail without their subscriptions (`reviews/008-flip-editors.log`); the delete button fails without its `OnRemove` binding (`reviews/008-flip-onremove.log`). The two domain tests pin that each root raises its own `PropertyChanged` for the total its page shows. Build and suites: `reviews/008-build.log`, `008-design-build.log`, `008-test.log`, all green.

## Gate Record

- **Test review round 1:** CONCERNS. S7 was scored a pass on F6 though its page redrew on the root's every change, and S6 and S7 had no T-row evidence. Both were fixed: run 1 re-scored (S1, S3, S4 and S7 fell into F6), the skill and the Design pages it quotes fixed, all seven re-run, and run 2 scored row by row.
- **Test review round 2:** APPROVED. Both callouts were fixed: the isolation wording now matches `run.sh`, and `OnRemove` is tested and flip-checked. Tech-debt fixed: two loose S5 references, run 1's probe ledger moved to `run1/`, run 1's answers regenerated as UTF-8, the transcripts' impermanence stated, and the test count described honestly. See `reviews/008-test-review.md`.

## Plan Amendments

- **Run 1 fell into F6** in S1, S3, S4 and S7: their pages subscribed to the root's `NeatooPropertyChanged`, as the skill allowed for "a small form" and as the Order and WorkOrder Design pages did for their child rows. Fixed per Step 4. The Order and WorkOrder pages render each child row as a component (`OrderItemEditor`, `WorkOrderTaskEditor`), and all three pages subscribe only to their root's own `PropertyChanged`. The mudneatoo skill states the rule with no small-form escape. Re-run all seven.
- Run 2 also denied LSP, SendMessage, ListAgents, Workflow and RemoteTrigger. `run.sh` is committed beside the prompts.
- The first scoring of run 1 called S7's page a pass on the session's own reasoning; the test review vetoed it. F6 was not changed.
