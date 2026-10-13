# Test Review — SKR-002 — Design.App and Design.Server in CI

**Reviewer:** test-reviewer
**Logs:** `002-build.log` (Design.sln incl. App, Server, Infrastructure: 0 errors, 0 warnings), `002-test.log`

---

## Round 1 — 2026-10-11 (HEAD `c5fa284`, 194 tests)

**Verdict: CONCERNS**

### Veto-tier

None. `git diff skr-arc..HEAD` over Design.Tests changes only new files and two lines in `WorkOrderAggregateTests.cs` (`ShowHoldBanner` → `IsHeld` at :119, :126); both assertions still check the same direction; 6 `[TestMethod]` before and after. BaseClassTests and `TestInfrastructure.cs` untouched.

### Must-cover

1. **Bullet 1 (Order round-trip): the test did a different thing from the bullet.** The bullet says fetched → modified → saved; the test did Create → Save (Insert) → Fetch and never ran `[Update]` through the pair. **Triage: fixed** — the test now modifies the fetched root and child and saves again, asserting the store row changed.
2. **Bullet 4 (WorkOrder ViewModel): `HasUnschedulableTasks` had no test on the client scope**, and the map overstated what the over-budget test asserts (entity only; the ViewModel has no over-budget member). **Triage: fixed** — new `HasUnschedulableTasks_FollowsTheChildrensRule_OnTheClient`; map row reworded.

### Should-cover

1. WorkOrder never went through the pair (`WorkOrderViewModel.Save`/`Load`, inside a copied region). **Triage: fixed** — new `Save_SavableWorkOrder_CrossesToTheServerAndHoldsTheReturnedInstance` (Save reassigns, store row, Load back).
2. `OrderEditViewModel.Load` untested. **Triage: fixed** — the savable test calls `Load` after the save (second round trip).

### Tech-debt (untiered)

- `InMemoryUnitOfWork<TRow>` holds plain collections in a singleton; concurrent requests or `[Parallelize]` would interleave them. (Also code-review callout 4; accepted with a header note.)
- The map's `RunRules` grep claim held only with a source-file filter. **Fixed:** the map states the include filter.

### Theoretical (not triaged)

- ~35 of Design.Infrastructure's services have no live caller in Design.App.
- A Server-mode client seam would fail `Client_IsInRemoteMode_…` by throwing (cannot resolve `IOrderRepository` on the client), not at the `Calls == 1` assertion. Bullet 2 wording corrected to say so.

### Verification questions answered

- (a) Remote mode: the generated factory has `(IServiceProvider)` and `(IServiceProvider, IMakeRemoteDelegateRequest)` constructors; DI picks the remote one only when `IMakeRemoteDelegateRequest` resolves, which `AddNeatooRemoteFactory` registers only in Remote mode. The later `AddKeyedScoped` wins; `ServerLink` is scoped so `Calls` starts at 0 per test. The test tells the seams apart.
- (b) Save handler: the remote path returns a freshly deserialized instance; dropping the result leaves `ReferenceEquals` true and `IsNew` true — both assertions fail. A handler that skipped `IsSavable` would throw `SaveOperationException` in the invalid test.
- (c) Grep claims all true; 160 markers, 0 duplicates; `skill-visibility-rules` gone from code and docs.
- (d) Shared store: every assertion keyed by a fresh id; no `[Parallelize]`.

### Read report

Beyond the brief: RF `AddRemoteFactoryServices.cs`, generated `OrderFactory.g.cs`, `EntityBase.Save`, `InMemoryUnitOfWork.cs`, `Order.cs` Fetch, the pages' callers. Named but unused: `reviews/002-plan-review.md`.

---

## Round 2 — 2026-10-11 (HEAD `d5d5fcc`, 196 tests)

**Verdict: CLEAN**

- Must 1 closed: the round-trip test routes to `[Update]` (order no longer new), asserts `updated.IsModified == false`, `TotalAmount == 30.00`, and the server row's new `CustomerName` and `Items[0].Quantity` — the client holds only deserialized copies, so the row can change only if the Update ran on the server. Map row accurate.
- Must 2 closed: `HasUnschedulableTasks_FollowsTheChildrensRule_OnTheClient` asserts the fact false then true across `IsOnHold`, with the child's `IsSchedulable` at both steps. Map row says which tests are entity-only and why.
- Should 1 covered by the WorkOrder Save test. Should 2 and the wording fixes taken on the orchestrator's word (round 2 confirms Musts only).
- Sacred tests intact (same two-line rename). Logs green. Nothing new.
