# Facts, Consequences, Intents: Rules-First Across the Three Skills

**ID:** FCI
**Type:** Enhancement
**Status:** In Progress
**Created:** 2026-10-16
**Last Updated:** 2026-10-16
**Initial split:** 9 plans
**Plan cap:** 14 — counts plan numbers issued, including Abandoned and Retired. Issuing a number above the cap is a stop-and-ask.
**Arc branch:** fci-arc — cut from main at Step 1; every plan and punchlist branch PRs into it; it PRs into main at Step 8.
**Related:** Successor to SKR (complete 2026-10-12), whose model statement 3 this todo replaces. Adopts #112 (the principle and the fluency case study), #114, #87, #88, #94; #113, #93, #95 under the Optional AC-10. RemoteFactory#173 is the RF half, worked by the desktopRemoteFactory session from the wording FCI-001 sends; RFSK (RemoteFactory's skill rebuild) merged as RemoteFactory#175 and #177. No other arc is open; LIST is not started and touches EntityListBase, not these files. Decided by Keith, 2026-10-16: the architecture page in the session of that date; the analyzer covers rules and verbs, so Design.Domain's verbs become `async Task`; `SetX` goes on the partial interface, because no type is handed a concrete.
**Standing orders:** Unattended, in Keith's words: "Run the FCI todo right up to marking the todo completed. Take all recommendations but review them at the end. Merge all PRs yourself. Stop if you need my feedback." (Keith, 2026-10-16.) Claude merges every plan and punchlist PR into the arc, takes every reviewer recommendation and lists each one under Recommendations Taken for Keith's end review, and stops at the arc PR into main, which carries that review. It stops for Keith, never decides: a priority change, a Goal or Out of Scope edit, a plan number beyond the initial split, a sentence attributed to Keith, and any public API change beyond the three decided above. It may overwrite `~/.claude/skills/neatoo` and `~/.claude/skills/mudneatoo` with the repository copies and run the headless fresh-session sessions, only with the isolated mechanism recorded in FCI-008. It messages desktopRemoteFactory; it never edits the RemoteFactory repository. A test change the documented intent justifies is made and reported in the PR; an out-of-scope test failure, or a test-versus-code disagreement no documented intent covers, is a `proposed` Parked row and stops the loop (Keith, 2026-10-16).

---

## Goal

Someone building on Neatoo, human or Claude, decides for every piece of behaviour whether it is a fact, a consequence or an intent, and builds it so. A fact is a property a later reader needs; a consequence is a rule, code that runs inside the cascade whoever wrote the fact; an intent is a verb, kept only for what state cannot hold. The framework makes the standard mechanical: rules and verbs await a generated `SetX` on the entity's interface, an analyzer flags the forking setter where it loses async, and rules can trigger on meta state. One compiled aggregate at the size of zTreatment's fluency tab is the example all three skills quote, and the Neatoo, MudNeatoo and RemoteFactory skills state one architecture between them.

## Acceptance Criteria

- [ ] **AC-1** · Required — VisionAlignment, the Neatoo skill and the MudNeatoo skill state facts, consequences and intents in place of "logic is placed by who is driving it".
- [ ] **AC-2** · Required — The generator emits an awaitable `SetX` for every settable partial property on the class, and on its partial interface for every public-set one.
- [ ] **AC-3** · Required — An analyzer with a code fix warns on assignment to a Neatoo partial property inside a rule or a verb and stays silent inside factory operations.
- [ ] **AC-4** · Required — A rule can trigger on `IsValid`, `IsSelfValid`, `IsBusy` and `IsModified`, and an analyzer refuses an async rule triggered on `IsBusy`.
- [ ] **AC-5** · Required — Design.Domain holds a tested aggregate at fluency-tab size that every rules-first example in the three skills quotes.
- [ ] **AC-6** · Required — `skills/neatoo` teaches facts first from compiled regions and closes #112, #114, #87 and #88.
- [ ] **AC-7** · Required — `skills/mudneatoo` teaches the component as the unit of refresh and the verb-bound control from compiled regions, and closes #94.
- [ ] **AC-8** · Required — The RemoteFactory skill carries the "where does business logic go" wording this todo sends, closing RemoteFactory#173.
- [ ] **AC-9** · Required — An isolated fresh-session run falls into no trap row, new fact-versus-verb scenarios included.
- [ ] **AC-10** · Optional — MudNeatoo ships #113's boundary components, render throttle and slider gaps, and closes #93 and #95.
- [ ] **AC-11** · Optional — Design.Domain compiles an open that finishes inside a non-remote seam, and the skill names `LoadValue` inside a rule as the smell it replaces.

## Out of Scope

- Edits to the RemoteFactory repository; desktopRemoteFactory makes them from the wording FCI sends.
- The framework defects not named above: #103, #109, #96, #89, #90, #97.
- Removing the lazy-load surface; moving the RemoteFactory pin.
- zTreatment's FLUR code; it adopts the framework changes by package update.
- Passing the trigger path into `IRule.RunRule`; ruled out in the design, because a rule that branches on its caller is the caller-knowledge the property model removes.

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-architecture-page](./plans/001-architecture-page.md) | The architecture page in VisionAlignment | AC-1, AC-8 | Done | #115 |
| 002 | [002-generated-setx](./plans/002-generated-setx.md) | Generated awaitable SetX on class and interface | AC-2 | Done | — |
| 003 | [003-assignment-analyzer](./plans/003-assignment-analyzer.md) | Assignment analyzer, code fix, Design sweep | AC-3 | Draft | — |
| 004 | [004-meta-state-triggers](./plans/004-meta-state-triggers.md) | Meta-state triggers and the IsBusy analyzer | AC-4 | Draft | — |
| 005 | [005-fluency-size-example](./plans/005-fluency-size-example.md) | Compiled fluency-size aggregate in Design.Domain | AC-5, AC-11 | Draft | — |
| 006 | [006-neatoo-skill](./plans/006-neatoo-skill.md) | Neatoo skill: facts first | AC-1, AC-6 | Draft | — |
| 007 | [007-mudneatoo-skill](./plans/007-mudneatoo-skill.md) | MudNeatoo skill: components and verb-bound controls | AC-1, AC-7 | Draft | — |
| 008 | [008-fresh-session-and-sync](./plans/008-fresh-session-and-sync.md) | Isolated fresh-session test, then sync | AC-9 | Draft | — |
| 009 | [009-mudneatoo-components](./plans/009-mudneatoo-components.md) | MudNeatoo boundaries, throttle, slider, converter | AC-10 | Draft | — |

---

## Punchlist

- [x] Send the "where does business logic go" wording to desktopRemoteFactory · RemoteFactory#173 · done when FCI-001 has merged into the arc and the message is sent · AC-8 · sent 2026-10-16, applied as RemoteFactory#178
- [ ] Confirm the wording landed in both #173 sites and the installed RF skill · `~/.claude/skills/RemoteFactory` · done when the RF PR has merged and the installed copy shows it · AC-8

## Recommendations Taken

Every reviewer recommendation taken under the standing orders, one line each, for Keith's review at the end: plan · source review · what was recommended · what was done.

- FCI-002 · code review · skip SetX on an interface that declares the property get-only · done, with a generator test

## Dismissed

- FCI-002 · Neatoo.UnitTest.Demo reports no tests · pre-existing, serves no criterion

## Parked

---

## Discovery Log

---

## Skipped Steps

- FCI-001 test review: prose-only plan, no behaviour to pin.
- Step 1 Explore fan-out: the design session of 2026-10-15/16 read every load-bearing seam this todo touches; the recon is recorded in each stub's Scope and is re-walked at each plan's Step 3.

---

## Sibling Todos

---

## Close-Out Audit

---

## Docs & Retro

---

## Results / Conclusions
