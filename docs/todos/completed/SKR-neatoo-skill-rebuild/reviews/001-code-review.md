# Code Review — SKR-001 — Design.Domain example per Neatoo type choice

**Reviewer:** code-reviewer (opt-in, per-plan)
**Commit reviewed:** `11acfb8` on `skr-001-type-choice-examples` (diff against `skr-arc`)
**Logs:** `001-build.log` (0 errors, 0 warnings), `001-test.log` (187 passed, 0 failed)

---

## Round 1 — 2026-10-10

**Verdict: CLEAN**

### Direction & shape

The work sits in the right layers and seams.

- **Submit verb** (`Order.cs`): only sets `Status`, which the existing rule already triggers on.
- **Job** (`Jobs/SubmitDraftOrders.cs`): matches D20 exactly — verb, `WaitForTasks()`, then `IsSavable` saves and reassigns / `!IsValid` reports `PropertyMessages` / else skipped. No `RunRules`, catches nothing, throws on a missing row. Each of the three test outcomes follows from the code, not from naming.
- **Domain service** (`Services/ShippingRateService.cs`): `[Factory]` on the interface, none on the implementation; implementation registered only in `DesignTestServices`; `AddDesignDomainRules` registers only the rule (plan-review C6 satisfied).
- **Rule** (`ShippingQuoteRule`): follows `CheckUsernameAvailabilityRule` conventions; sits on a new input model, so no existing region or test changed.
- **`AsyncRules.cs` header:** one doctrine, no preference between the two rule shapes.
- **`DualUseEntity`:** matches D23 and RemoteFactory Anti-Pattern 7 — root operations `[Remote]` and service-taking; child operations `internal`, not `[Remote]`, row-taking. Both roles pinned by `DualUseEntityTests`.
- **Rename:** complete; no `ValueObject` / "value object" in any `.cs` under `src/Design` outside `Generated/`.
- **Interface-first:** holds for every new entity and input model.

### Veto-tier

None.

### Callouts (all comment text; all affect AC-3 · Must)

1. **The job's reason for `[Remote]` did not fit its own parameter.** `SubmitDraftOrders.cs` header said "[Remote] is on the command because it takes a [Service]" and that a server-only `[Service]` "would break the client build". The only `[Service]` is `IOrderFactory`, a generated factory on both tiers; and per the RF skill such a command compiles and fails on the client at call time. Reachable by anyone copying the job. **Triage: fixed 2026-10-10.** The comment now gives the real reason — the job is server work, one call from wherever it is triggered, every factory call inside it local; without `[Remote]` a client would pay a round trip per Fetch and per Save — and states the call-time failure correctly. `[Remote]` stays (plan Alignment, model statement 4).
2. **The duality comment called saving a list-fetched child as a root "harmless", contradicting T17.** `RemoteBoundary.cs` duality block. `EntityBase.Save()` has no child guard, so the call routes to the root `[Update]` against the repository, bypassing the parent's row and leaving the list holding a stale instance. Reachable by a consumer holding `IDualUseEntity` (extends `IEntityRoot`) from a list. **Triage: fixed 2026-10-10.** Restated in T17's terms: saved as a root only when fetched as one; the direct `Save()` is the cost of the both-roles choice and the compiler cannot catch it.
3. **Ruling IDs (Dn), trap IDs (Tn) and direct owner quotes appeared in the example set, one `(D4)` inside `skill-job-over-entities`.** `skr-arc` Design.Domain has none; D20–D25 are not in this branch's `VisionAlignment.md`; the quotes read oddly in a framework example. Reachable by plan 004 rendering the region into the installed skill. **Triage: fixed 2026-10-10.** All Dn/Tn references and both quotes removed from `src/Design` `.cs` files (verified by grep); the comments carry their own reasoning. `VisionAlignment.md` points at Design.Domain, not the reverse.

### Should / Could

None.

### Theoretical (not triaged)

- Test Evidence "-test partners" wording (also raised by the test-reviewer; fixed in the map).
- `RemoteBoundary.cs` opening line "child-only, or both root and child" omitted the plain root case. Reworded to "a root, child-only, or both" while editing the block.

### Read report

- Beyond the brief: `src/Neatoo/EntityBase.cs:455-514` (no child guard on `Save()`); D20–D25 from `skr-rulings-d20-d25`; RemoteFactory `CLAUDE-DESIGN.md:567` (Anti-Pattern 7); RF skill `static-factory.md:91`, `interface-factory.md`; `Commands/ApproveEmployee.cs`; `MockOrderRepository`; `reviews/001-plan-review.md` (C1, C6).
- Named but unused: `src/Design/CLAUDE-DESIGN.md` (its lines 77 and 87 still say value object — the todo's punchlist row, outside this plan).

**Closing:** CLEAN. No veto-tier findings, build and all 187 tests green. Three Must-affecting callouts on comment text, none blocking; all three fixed before the PR.
