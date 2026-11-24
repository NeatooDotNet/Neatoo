# FCI-008 — Isolated fresh-session test, scored

**Date:** 2026-10-16
**Skills under test:** the installed `~/.claude/skills/neatoo`, `mudneatoo` and `RemoteFactory`.
- Run 1: neatoo and mudneatoo were byte-identical to the arc after FCI-007.
- Run 2: they were identical to this branch after the fix below.
- RemoteFactory carries RemoteFactory#178 in both runs.

**Mechanism:** `008-runs/run.sh`, one headless `claude -p` per scenario (Claude Code 2.1.301), run from an empty scratch directory with stdin from /dev/null.
- Modes: `--permission-mode dontAsk` and `--strict-mcp-config`.
- Allowed: `Skill`, plus Read, Glob and Grep on `~/.claude/skills/**`.
- Denied: Bash, PowerShell, Agent, Task, the web tools, Write, Edit, and Read on `~/source`, `C:\src`, `AppData` and `~/.claude/projects`, `todos` and `plans`.
- Run 2 also denied LSP, SendMessage, ListAgents, Workflow, RemoteTrigger, CronCreate, Monitor, EnterWorktree, PushNotification and SendUserFile, which run 1's sessions listed. Run 1's command line was the same without those; `run.sh` is run 2's.
- The user-global `~/.claude/CLAUDE.md` loads in every session. It holds working rules and documentation conventions, no Neatoo doctrine.

**Files in `008-runs/`:**
- `s1.txt`–`s7.txt`: the prompts. S1–S5 are verbatim from SKR-007; S6 and S7 are new.
- `run1/` and `run2/`: each session's answer (`outN.md`) and its tool ledger (`outN-tools.txt`). A ledger lists the tools the session had and every call it made, with whether the call errored.
- `probe.txt`, with `run1/probe-tools.txt` and `run2/probe-tools.txt`: the probe prompt and each run's probe ledger.

The full stream-json transcripts are 3.8 MB a run, mostly echoed skill text. They are kept outside the repository, in this session's scratchpad (`fci008-transcripts/`), which does not outlive the session. The committed answers and ledgers, extracted from them, are the durable evidence.

**Scorer:** SKR's trap table T1–T20 and the plan's F1–F7. A row is fallen into when the output does what the row's question assumes. The rows were not changed between runs.

## Isolation

- **The probe** asked a session to read seven targets, in both runs.
  - Refused: the Neatoo `CLAUDE.md`, `TrainingPlan.cs`, a Glob and a Grep over `~/source`, and `~/.claude/projects`.
  - Not attempted: the shell command, because there was no shell tool.
  - Succeeded: the installed skill read.
- **Run 2's sessions had twelve tools:** CronDelete, CronList, DesignSync, ExitWorktree, Glob, Grep, Read, ReportFindings, ScheduleWakeup, Skill, TaskStop and ToolSearch. Every tool `run.sh` denies is gone from that list. The eight that remain are not denied: none of them reads files, and no session called one.
- **Scenario calls:** every scenario call in both runs went to `~/.claude/skills`, with one exception. Run 2's S1 tried to grep the Neatoo repository's `src` for `EmailAddressAttribute`, and the call was refused (`run2/out1-tools.txt`). The isolation held against a session that tried.

## Verdict

- **Run 1:** S1, S3, S4 and S7 fell into F6.
- **Run 2,** after the fix: no scenario falls into any T row or F row. AC-9 is met on run 2.

## Run 1: F6 fell, and the skill taught it

F6 asks whether "the page redraws on the root's every change". In run 1, four pages did:
- S7 subscribed its page to the sprint's `NeatooPropertyChanged` (`run1/out7:775`) and showed per-row completion details, the percentage and the Close button from that one subscription. It reasoned "fine for a screen this small".
- S1, S3 and S4 did the same for their Save buttons (`run1/out1:653`, `run1/out3:451`, `run1/out4:716`).

S6 had no page subscription and passed. The first scoring called S7 a pass and S1–S5 "—" on the session's own reasoning. That was wrong as F6 is written, and the test review caught it.

The four sessions were following the skill. It said a page-wide `NeatooPropertyChanged` subscription was "acceptable for a small form". It also quoted Design pages that did exactly that, child rows included: Order's `LineTotal` and WorkOrder's `Cost` re-rendered only because their pages subscribed to the graph.

**The fix (this branch):**
- Design.App's Order and WorkOrder pages render each child row as a component, `OrderItemEditor` and `WorkOrderTaskEditor`, that subscribes to its own child.
- All three pages subscribe only to their root's own `PropertyChanged`.
- The mudneatoo skill states the rule without an escape: a page never subscribes to the root's `NeatooPropertyChanged`, and every child row that shows a child's fact is a component.
- Design.Tests pins both halves with bUnit:
  - each editor re-renders for its child, and fails without its subscription;
  - each root raises its own `PropertyChanged` for the total its page shows.

## Run 2: the fact-versus-verb rows

| Row | S6 Training plan | S7 Sprint tasks | S1–S5 |
|---|---|---|---|
| F1 fact as a method | pass. Effort, lock and approved minutes are bound properties; the nudges write the next marks into `Effort` | pass. `IsDone` is a property. The who-and-when stamp makes the write an intent, so `MarkDone(done, user)` writes the stamp and then the fact (367) | pass. S5's job writes the `IsDormant` fact (193) |
| F2 consequence outside a rule | pass. The fit, `PlannedMinutes` and the next marks are rules | pass. `PercentDone`, `AllTasksDone` and `CanClose` are rules (195–211) | pass |
| F3 un-awaited write | pass. Every rule that writes is `AddActionAsync` with awaited `Set{Name}`. The validations are `AddValidation` and one `RuleBase` (284) | pass. `MarkDone` awaits each write | pass. No assigning `AddAction` or sync multi-write verb |
| F4 caller in the rule, suspension, `LoadValue` | pass. A scoped anchor in `SlideMinutes` (374) | pass | pass |
| F5 admission checked and thrown | pass. `CanApprove` triggers on `IsValid` (154) | pass. `CanClose` triggers on `IsValid` (210); `Close` sets state (236) | pass |
| F6 verb control; page redraw | pass. The minutes slider uses `Setter="@Interval.SlideMinutes"` (588). The rows and summary subscribe to their own entity (537, 623) | pass. The checkbox uses `Setter` (733). Each `SprintTaskRow` subscribes to its task (776). The page subscribes to the sprint's own `PropertyChanged` (898) | pass. Each page subscribes to its own entity's `PropertyChanged` (S1 603, S3 488, S4 740, S5 492). S4's action items are `ActionItemEditor` rows (669). S1's phone rows hold only inputs, so they need no component. S2's input model subscribes to its own `PropertyChanged` (402), and its hand-bound fields write facts through `SetValue` (342–364), the documented input-model binding (Neatoo#95) |
| F7 async rule on `IsBusy`/`IsSavable` | pass | pass | pass |

## Run 2: the SKR rows

Line references are into `run2/outN.md`. "—" means the scenario gives the row no occasion.

| Row | S1 | S2 | S3 | S4 | S5 | S6 | S7 |
|---|---|---|---|---|---|---|---|
| T1 validate before Save | pass 488–495 | pass 287 | pass 392–398 | pass 615–622 | pass 194–198 | — | pass 706–713 |
| T2 throw on bad input | pass, rules 61–65; throws are the server gate 111–114 | pass, rule 57; the `[Fetch]` refuses a caller that bypassed the gated button 126–130 | pass 141–154 | pass 335 | pass 25–68; gate 100–104 | pass 126; 460 is an application failure (no signed-in coach) | pass 158–219; gate 281–284 |
| T3 load-or-create | — | — | — | pass, `[Remote, Execute] OpenToday` 132–139 | — | — | — |
| T5 client calls the repository | pass, `[Service]` 77–90 | pass 123 | pass 120–122 | pass 136–137 | pass 82, 98 | — | pass 242, 254 |
| T6 application service | — | — | — | — | pass, a static `[Remote, Execute]` command 156–167 | — | — |
| T8 derived values and visibility | pass | pass | pass | pass | pass | pass 154 | pass 195–211 |
| T9 set in Fetch without marking modified | pass 204 | — | — | pass 162 | — | — | pass 389 |
| T10 computed values after Fetch | — | — | — | — | — | — | pass, `RunRules(nameof(AllTasksDone))` 273 |
| T11 parent saves children | pass, the list walks `DeletedList` 260–264 | — | — | pass 283–287 | — | — | pass 449 |
| T12 a rule can't call the server | — | — | pass 107–121 | — | — | — | — |
| T13 Save updates my object | pass 495 | — | pass 398 | pass 622 | pass 198 | — | pass 713 |
| T14 constructor services | pass | — | pass | pass | pass | pass | pass, `TimeProvider` on both tiers |
| T15 child `[Insert]` `[Remote]` | pass, internal 213 | — | — | pass 355 | — | — | pass 400 |
| T16 value object, so ValidateBase | — | pass, the criteria are an input model, the rows a record 103 | — | — | pass, a record 219 | — | pass 136 |
| T17 Save on a child | pass 35 | — | — | pass 88 | — | pass 69 | pass 108 |
| T18–T20 (RF) | pass | pass | pass | pass | pass | pass | pass |
| T4, T7 | T4 pass in S4 (Create takes what a constructor would, Fetch keyed); T7 pass in S5 (the job reuses the aggregate and its rule) |

## Observations

1. **S7 worked around the read-only limitation.** It gave `IsDone` an `internal` setter so that the checkbox would not render read-only (`run2/out7:329, 357`). That is the limitation recorded as a proposed decision row for Keith: a `Setter` does not lift read-only, and a `private set` property is read-only. A fresh session hit it on its first verb-bound checkbox.
2. **S6 follows the compiled TrainingPlan closely,** as it says. S7 transfers the doctrine to a shape the example lacks, a who-stamp on a checkbox, and passes.
3. **S1, S3, S6 and S7 `[Create]`** run `RunRules(RunRulesFlag.All)` for a form, each with the per-operation reason. This is SKR-007's observation 2, still open for Keith's wording call. Not a trap row.
