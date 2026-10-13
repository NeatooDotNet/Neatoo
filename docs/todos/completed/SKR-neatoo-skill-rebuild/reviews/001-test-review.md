# Test Review — SKR-001 — Design.Domain example per Neatoo type choice

**Reviewer:** test-reviewer
**Commit reviewed:** `11acfb8` on `skr-001-type-choice-examples`
**Logs:** `001-build.log` (0 errors, 0 warnings), `001-test.log` (187 passed, 0 failed, 0 skipped)

---

## Round 1 — 2026-10-10

**Verdict: CLEAN**

### Veto-tier

None.

- **Sacred tests:** `git diff skr-arc..HEAD -- src/Design/Design.Tests/BaseClassTests` changes identifiers only (`IDemoValueObjectFactory` → `IDemoInputModelFactory`, `IDemoValueObjectListFactory` → `IDemoInputModelListFactory`, in field types and `GetRequiredService` calls). No assertion, case or setup removed or changed.
- **TestInfrastructure.cs:** one existing line changed (the `typeof(IDemoInputModel)` rename); everything else is additions (`IShippingRateService` registration, `IDualUseRepository` registration, `MockDualUseRepository`). No existing mock edited.

### Must-cover

None. Every Must bullet is pinned.

- **Job saves / reports / skips.** `SubmitDraftOrdersTests.Run_ValidInvalidAndUnchangedOrders_SavesReportsAndSkipsWithoutThrowing` is a real integration test (Server-mode DI, real `IOrderFactory`, scoped `MockOrderRepository`). Saved: `Submitted == [valid]` and the stored row became `"Submitted"` (only reachable through `[Update]`'s `MapTo`). Reported: `Rejected` holds the invalid order with an "at least one item" message and its stored row stays `"Draft"`. Skipped: `Unchanged == [unchanged]`. Each outcome is reached the way the bullet describes (`Submit()` on the empty draft fires the Status-triggered rule; `Submit()` on an already-Submitted order sets the same value, so it stays unmodified). Anything thrown would fail the test; `Run_UnknownOrder_Throws` pins the not-found throw.
- **No `RunRules` in the job (explicit-skip).** Grep claim true: one hit, `SubmitDraftOrders.cs:16`, the DESIGN DECISION comment.
- **Rule message reflects the service.** Integration tier confirmed: `ShippingQuoteRule` registered in `DomainRegistration.cs:31`; `IShippingRateService` → `ShippingRateService` via `TestInfrastructure.cs:108`. `Rule_DestinationServed_QuotesFromTheService` asserts 13.00 (6.50/kg × 2); `Rule_DestinationNotServed_ReportsTheServiceAnswer` asserts invalid, "do not ship to AQ", quote 0. Both depend on the service's answer; neither is vacuous.
- **No `ValidateBase` value object (explicit-skip).** Grep claim true over `src/Design/**/*.cs` excluding `Generated/`. Widening to `value-object` finds only the two region names the plan keeps on purpose (Alignment, C5). No `*ValueObject*` type remains.
- **Regions and mdsnippets (explicit-skip).** 150 regions outside `Generated/`, `bin`, `obj`; none duplicated; all five new `skill-*` regions exist. Reviewer did not run `mdsnippets` (it rewrites files); instead checked every snippet placeholder a doc references matches a region — the non-matches are all in todo/audit/completed-plan files or inline backtick text. Note: `mdsnippets.json` sets `TreatMissingAsWarning: true`, so exit 0 alone would not prove nothing is missing; the map's "no missing-snippet lines" wording is the right test.
- **Build and tests green.** Confirmed from the logs.
- **Duality punchlist row (D23).** Integration tier confirmed. `RootRole_FetchByIdAndSave_WritesThroughTheRepository` goes through `[Remote] Fetch(id)` and `Save()`, asserts the repository store updated and the entity no longer modified. `ChildRole_FetchFromRowAndSave_WritesIntoTheRow` goes through the internal `Fetch(row)` and `Save(entity, row)`, asserts the row updated, the entity no longer modified, and the repository never touched.

### Should-cover

None. All the job's branches and both of the rule's service branches are exercised. The rule's early return for an empty destination or zero weight runs in both tests (destination is set before weight), though no test asserts it directly.

### Tech-debt (untiered)

- Test Evidence regions row said the five new regions have "their `-test` partners"; only three do. The other two are pinned by the job test and the rule tests. Wording fix, not a coverage gap. **Triage: fixed inline in the plan's Test Evidence map, 2026-10-10.**

### Theoretical (not triaged)

- `DualUseEntity.Insert(DualUseRow)` (child-role insert, `RemoteBoundary.cs:327-332`) has no live caller; no parent list exists for `DualUseEntity` in Design.Domain.
- The `0m` fallback in `ShippingRateService.Quote` for an unknown destination cannot be reached through the rule, because `ShipsTo` checks first.

### Read report

- Beyond the brief: `mdsnippets.json`; `git show skr-arc` of `RemoteBoundary.cs` (root-role CRUD on `DualUseEntity` pre-dates this plan); the todo's Dismissed section (its one entry is unrelated).
- Named but unused: `VisionAlignment.md` §0.

**Closing:** CLEAN. Every Must bullet is pinned at its declared tier and the explicit-skip grep claims hold. The sacred-test diff changes identifiers only. Logs green.
