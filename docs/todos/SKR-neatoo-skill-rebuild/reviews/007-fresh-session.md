# SKR-007 — Fresh-session test, scored

**Date:** 2026-10-12
**Skills under test:** `~/.claude/skills/neatoo` and `~/.claude/skills/mudneatoo` (byte-identical to `skills/` at the run), `~/.claude/skills/RemoteFactory` (the RF repository copy at `3e3823b`, per the user's choice).
**Mechanism:** one headless `claude -p` per scenario (Claude Code 2.1.296), started in an empty scratch directory outside the repository, so there was no repository `CLAUDE.md`, no project memory and no `src/Design`. `--allowedTools "Skill Read Glob Grep"`: the sessions could read the installed skills and nothing else, and could not write files. The user-global `~/.claude/CLAUDE.md` loads in every session; it holds no Neatoo doctrine.
**Prompts and outputs:** `007-runs/s1.txt`–`s5.txt` (verbatim) and `007-runs/out1.md`–`out5.md`, plus `out5b.md` (scenario 5 re-run after the fix below). Line references below are into those files.
**Scorer:** the habit-trap table in `todo.md`. A row is fallen into when the output does what the row's question assumes. "—" means the scenario gives the row no occasion.

## Verdict

**No scenario falls into any trap row, Neatoo-owned or RF-owned.** AC-9, narrowed to the Neatoo-owned rows on 2026-10-12, is met. No RF-owned row was fallen into, so desktopRemoteFactory gets no findings, only this result.

One Neatoo-side defect surfaced outside the table and was fixed on this branch (Findings, 1). Three observations go to the user (Findings, 2–4).

## Row by row

| Row | Owner | S1 Contact + Save | S2 Search form | S3 Duplicate check | S4 Open-or-create visit | S5 Nightly job |
|---|---|---|---|---|---|---|
| T1 validate before Save | Neatoo | pass — `WaitForTasks` then `IsSavable` then `Save`, no `RunRules` in the handler (out1:542–565) | pass — the search gesture waits, then reads the `CanSearch` fact (out2:280–303) | pass (out3:414–437) | pass (out4:532–555) | pass — the job does verb, `WaitForTasks`, `IsSavable`, no `RunRules` (out5:216–236); screen handler (out5:486–508) |
| T2 throw on bad input | Neatoo | pass — attributes on the properties (out1:63–76) | pass — `AddValidation` message (out2:52–56) | pass — the async rule returns a message (out3:96–111) | pass (out4:105, 299) | pass — `RuleBase` message on `IsDormant`, the verb never throws (out5:44–57, 114–117) |
| T7 nobody edits, plain EF | Neatoo | — | — | — | — | pass — the job reuses the aggregate, its verb and its rule; failures are reported with the rule's messages (out5:176, 205–237) |
| T8 derived values and visibility | Neatoo | pass — the Save button binds `IsSavable` through the VM | pass — `CanSearch` is an `AddAction` fact the button binds (out2:58–64, 354–357) | pass | pass | pass |
| T9 set in Fetch without marking modified | Neatoo | pass — plain assignment, paused (out1:108–113) | — | pass (out3:173–177) | pass (out4:163–168) | pass (out5:105–110) |
| T10 computed values after Fetch | Neatoo | — | — | — | — | pass — `LastVisitUtc` loaded from the row (out5:110, 277–289) |
| T11 parent saves children (Neatoo half) | Neatoo | pass — the list walks live items and `DeletedList` and hands each child its row (out1:276–299) | — | — | pass (out4:255–278) | — |
| T12 a rule can't call the server | Neatoo | — | — | pass — the async rule takes a `[Remote, Execute]` delegate, never the repository (out3:57–112) | — | — |
| T16 value object, so ValidateBase | Neatoo | — | pass — the criteria are an input model; the request and rows are records (out2:1, 91, 100) | pass — `ContactData` is a record (out3:34) | — | pass (out5:156) |
| T17 Save on a child | Neatoo | pass — the child interface extends `IEntityBase` (out1:33) | — | — | pass (out4:72) | — |
| T3 load-or-create | RF | — | — | — | pass — a `[Remote, Execute]` that calls Fetch, then Create (out4:123–137) | — |
| T4 Create from loaded data | RF | — | — | — | pass — Create takes what a constructor would; Fetch is keyed (out4:141–170) | — |
| T5 client calls the repository | RF | pass — the repository is `[Service]` on server operations (out1:98–100) | pass (out2:135–141) | pass (out3:61–69) | pass | pass |
| T6 where the application service goes | RF | — | — | — | — | pass — a static `[Remote, Execute]` command (out5:185–196) |
| T11 who writes a child's row (RF half) | RF | pass — the child maps itself (out1:227–245) | — | — | pass (out4:325–343) | — |
| T13 Save updates my object | RF | pass — reassigned (out1:559) | — | pass (out3:431) | pass (out4:549) | pass (out5:221, 502) |
| T14 which services a constructor takes | RF | pass | — | pass — the rule's dependency is a delegate on both tiers (out3:145–152) | pass | pass |
| T15 child Insert `[Remote]` | RF | pass — `internal`, no `[Remote]` (out1:219–238) | — | — | pass (out4:317–336) | — |
| T18 `[Remote]` means runs on the server | RF | pass — "the call crosses to the server" (out1:46) | pass (out2:115) | pass (out3:50) | pass (out4:84) | pass |
| T19 command needs the server, plain `[Execute]` | RF | — | — | pass (out3:61–62) | pass (out4:123) | pass (out5:190–191) |
| T20 DTO from a controller | RF | pass — no controller; the factory crosses | pass | pass | pass | pass |

## Findings

1. **Fixed — the client composition called a helper the skill does not show.** S5's client calls `builder.Services.AddRemoteFactoryTransport(...)` (out5:642) and never defines it. The call comes from `skill-client-composition` in `references/blazor.md`, which calls Design.App's private helper, deliberately kept outside the region (the SKR-002 plan review vetoed RemoteFactory's keyed `HttpClient` inside a Neatoo region). S1–S4 wrote the keyed client inline and were right. Fix: the region's comment and the section's prose now say that `AddRemoteFactoryTransport` is the app's own helper, not a library method, and point at the RemoteFactory skill for the registration. Re-synced, S5 re-run: out5b writes the registration inline (out5b:634). Not a trap row; Neatoo-owned (AC-4, AC-2).
2. **Observation — every form's `[Create]` runs its rules.** S1, S3, S4 (the action item) and S5 all end a form-producing `[Create]` with `RunRules(RunRulesFlag.All)` and give the per-operation reason (out1:90–93, out3:157–160, out4:310–315, out5:92). The skill frames this as the rare validate case, decided by the operation, with `Order.Create` as its example. The sessions treat a form a person fills in as that case every time. This matches the ruling (the developer's decision, made by the operation). It is listed so the user can say whether "rare" in the skill's wording should change.
3. **Observation — S5's first run set the Guid key in `[Create]`** (out5:91). `pitfalls.md` says to set it in `[Insert]`. The re-run set it in `[Insert]` (out5b:150), so this is run-to-run variation, not a skill gap. Not a trap row.
4. **Observation — the double-click guard was applied everywhere it applied.** S1, S3, S4 and S5 hold an in-flight flag in the save handler and cite Neatoo#103 (out1:493, out3:378, out4:546, out5:499): the pain note is being read and acted on.

## For the RemoteFactory sibling

No RF-owned row was fallen into, against the RF repository copy at `3e3823b`. The audit's open RF findings (the "persistence routing" framing, N+1 as the reason for internal child operations, repository-named interface factories, `[Remote, Create]` in the lead example) did not show up as trap-row behavior in these five scenarios. They may show up in scenarios these five do not exercise.
