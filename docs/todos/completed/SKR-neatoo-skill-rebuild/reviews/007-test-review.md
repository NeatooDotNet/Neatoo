## Test Review — SKR-007 — [2026-10-12] — Round 1

**Verdict: CLEAN**
**Logs:** build PASSED, 0 warnings / 0 errors, Design.App, Design.Server and Design.Tests built — `reviews/007-build.log`; tests 206 passed, 0 failed, 0 skipped (Design.Tests, net9.0) — `reviews/007-test.log`

The question was whether the scoring is honest. It is. Every verdict I checked points at output that supports it. No session fell into a trap row that the table marks pass or "—". The prompts name no trap, no Neatoo type and no attribute. The fix keeps the keyed `HttpClient` registration out of the Neatoo region.

**Sync bullet (Should, `[explicit-skip]`).** I ran `diff -rq skills/neatoo ~/.claude/skills/neatoo` and `diff -rq skills/mudneatoo ~/.claude/skills/mudneatoo` myself. Both printed nothing and exited 0.

**Fresh-session bullet (Must, `[explicit-skip]`).**

- **Line references.** I checked every pass cell for T1, T7, T10, T12 and T17 against the output:
  - T1: out1:542–565, out2:280–303, out3:414–437, out4:532–555, out5:216–236 and 486–508. Each one waits, reads `IsSavable` or the fact, and calls no `RunRules`.
  - T7: out5:176, 205–237.
  - T10: out5:110, 277–289. `LastVisitUtc` is loaded from the row.
  - T12: out3:57–112. The rule takes the `[Remote, Execute]` delegate.
  - T17: out1:33, out4:72. Both child interfaces extend `IEntityBase`.

  I also checked at least two more cells in each scenario. S1: T2 63–76, T9 108–113, T11 276–299, T13 559, T15 219–238. S2: T2 52–56, T8 58–64/354–357, T16 1/91/100, T5 135–141. S3: T2 96–111, T9 173–177, T14 145–152, T16 34, T19 61–62. S4: T2 105/299, T3 123–137, T4 141–170, T9 163–168, T11 255–278. S5: T2 44–57/114–117, T9 105–110, T6 185–196, T13 221/502. All of them hold.
- **Missed traps.** I read all six outputs against T1–T20 as `todo.md:38–57` phrases them, and grepped all of them for `RunRules`, `LoadValue`, `MarkModified`, `MarkUnmodified`, `PauseAllActions`, `throw`, `Save(` and `ValidateBase`.
  - **No validate-before-save.** No ViewModel, page or job calls `RunRules` before `Save` or before reading `IsValid`.
  - **`RunRules` appears only in three places.** In a form-producing `[Create]` (the review's observation 2). In the root's `[Insert]`/`[Update]` server gate, which `SKILL.md:302–304` teaches and no caller mirrors. And in S5, which the review covers already.
  - **No other row is fallen into.** No `LoadValue`. No `RunRules(All)` at the end of a `[Fetch]`. Every `throw` is an application failure or the gate. No `ValidateBase` value object, since S2's criteria are an input model. No `[Remote]` on a child operation. Every save result is reassigned.
- **Prompt leaks.** None under the plan's definition (`007-sync-and-verify.md:39`). "Neatoo and RemoteFactory libraries", "Save button", "ViewModel" and "entity" (the EF entity) name no trap, Neatoo type or attribute. See Theoretical for S5.
- **The fix.**
  - `ClientServices.cs:41–45` and `blazor.md:526` now say `AddRemoteFactoryTransport` is the app's own helper.
  - The `AddKeyedScoped(RemoteFactoryServices.HttpClientKey, ...)` registration stays in the private helper at `ClientServices.cs:62–67`, outside `#region skill-client-composition` (27–56). The SKR-002 veto holds.
  - The rendered snippet's source range was updated to L27–L56.
  - The re-run writes the keyed client inline at out5b:633–635.

### Veto-tier
None. The build and tests are green. `git diff --stat 864ab78..HEAD` touches no test file.

### Must-cover (plan-related)
None. The one Must bullet is pinned by the scored review, and the scoring holds up against the outputs.

### Should-cover (Should / Could bullets; plan-introduced reachable paths)
None. The Should bullet (sync) is confirmed by the `diff -rq` above. The plan introduces no code path, only a comment change.

### Tech-debt (untiered, ≤ 5, one line each)
- 14 cells have no line reference, although Test Evidence says "a line reference per verdict": T8 for S1/S3/S4/S5, T20 for all five, T5 for S4/S5, T14 for S1/S4/S5, and T18 for S5. I verified the T8 cells: out1:540,632; out3:412; out4:530,625; out5:484,560.
- Observation 4 says S1, S3, S4 and S5 "cite Neatoo#103". Only out3:378 (and out5b:485) cite it. out1:493, out4:546 and out5:499 describe the double-click gap without the issue number.
- Observation 3 cites out5b:150 for the Guid assignment in `[Insert]`. The line is out5b:151.
- The table scores out5, not out5b, which is the final S5 run. I read out5b against T1–T20 and it falls into no row: T1 at 225–246 and 526–541, T9 at 137–142, T10 at 297–299, T13 at 230 and 535.

### Theoretical (not triaged)
- S5's prompt says the rule "also applies when a person edits a contact on screen". That removes T7's "nobody edits this" premise, so S5's T7 pass is weaker evidence than the other cells. It is not a leak under the plan's definition, and scenario choice is out of scope.
- The fix's region comment names `RemoteFactoryServices.HttpClientKey` inside the Neatoo region. It names the key but does not register it, so it is not the registration the SKR-002 veto excluded.
- out5b's page drops the page-level `NeatooPropertyChanged` subscription that out1, out3, out4 and out5 keep. Without it, the Save button may not re-render when an async rule finishes. This is not a trap row.

### Read report
- Read beyond the brief: `skills/neatoo` grep for `RunRules` / `SaveOperationException`, to confirm the server gate in every output is skill-taught and not a T1 fall. `git diff 864ab78..HEAD` for the fix files and to check for test changes. The previous `006-test-review.md`, for format.
- Named but unused: none.
