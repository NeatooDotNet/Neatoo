# Plan Review — SKR-004 — SKILL.md: model, habit traps, decisions, UI contract

**Reviewer:** plan-reviewer
**Draft reviewed:** `71fdf33` on `skr-004-skill-model-and-decisions`

---

## Round 1 — 2026-10-12

**Verdict: CONCERNS**

### Pass A

Docs consulted: VisionAlignment §0 (D1–D25), §2, §2.1; todo (Goal, ACs, T1–T20, Out of Scope, Punchlist, Dismissed, Discovery Log); plans 003, 005, 007; `design-snippets.md`; `.claude/rules/skills-neatoo.md`; RemoteFactory `docs/VisionAlignment.md` §4.

**Veto-tier**

- **V1 — The plan stated D18 as a ban.** "Never a validate step" at Scope and Alignment; D18 says validating a returned object is the user's decision and rare; T10 says "never as a *routine* validate step"; SKR-003 left the rare case stated in `collections.md`, `testing.md`, `blazor.md`; the plan's own constraint forbids contradicting those; the 2026-10-11 Discovery Log entry records this exact misunderstanding. Written literally, T1/T10 would repeat the over-correction from the other side. **Triage: fixed** — three legs stated in Scope, Alignment and the memory line.

Pass A also found: no ruling IDs in prose matches current practice (`skills/` has none; the installed skill is read where VisionAlignment does not exist).

### Pass B

Files examined: all 863 lines of `SKILL.md` plus placeholders and headings across `references/*`; `Jobs/SubmitDraftOrders.cs`, `WorkOrder.cs`, `Order*.cs`, `IOrderInterfaces.cs`, `ApproveEmployee.cs`, `EmployeeDirectory.cs`, `RemoteBoundary.cs`, `AsyncRules.cs`, `DomainServiceRule.cs`, `AllBaseClasses.cs`, `ErrorPatterns.cs`; `OrderEditViewModel.cs`, `WorkOrderViewModel.cs`, `WorkOrderPage.razor`; `EntityRootInterfaceTests.cs`; RF VisionAlignment §4 and a grep of the RF skill.

**Reality check:** direction right (model first, trap table as spine, RF pointed at, decisions and UI contract added). All 17 Step-4 regions exist. The pointer-by-number idea holds at the record level (RF's VisionAlignment keeps the T-numbers "so a fresh-session test is one list"; T11-RF at :159; T18–T20 kept) but the current RF skill files carry no T-numbers — until the sibling syncs only the question text resolves, so phrase pointer questions as RF §4 does. 007 needs the Test Evidence map (row → line) and that wording.

**Callouts (Must-affecting)**

1. **T1's region contradicts itself on the New path; an open Must punchlist row decides the fix.** `skill-save-handler`'s `New()` calls `Order.Create`, which runs no rules, so an empty form is `IsSavable`; the comment "an invalid aggregate never reaches Save" is false for New; `skill-server-gate-refuses` beside it demonstrates the failure; T1's "Nowhere" would score the correct fix as a trap. **Triage: user decided — `Order.Create` runs its rules** (the `[Create]` decides its form must not come back unvalidated). Step 1; the Order-form row pulled onto this plan; T1's text names the rare case.
2. **~400 lines cannot hold the committed blocks**: the 17 regions render to ~830 lines; one block per Neatoo row alone ~460; `.claude/rules/skills-neatoo.md` forbids "see link" source-file references. **Triage: user decided — blocks in references, `SKILL.md` points**; four blocks render inline (the largest incident clusters); budget stands.
3. **Three trap rows had no fitting block**: T16 (no `record` value object anywhere in `src/Design`; `EmployeeSummary` is a read-model row; `skill-value-object` is the input model) — **user decided: add one small record** (`skill-value-object-record`); T11-Neatoo (`skill-list-update` exists, unlisted) — **amended**; T2 (`skill-rule-that-throws` would invert the answer) — **amended**: verb/admission pair + `skill-validation-attributes-and-rules`.
4. **Step 6's handoff dropped content no reference carries** (Three-Phase Pattern and its tables, Aggregate Is a Graph, Rules for Open Mutation, Parent's rules of use and three regions, Quick Start's four regions; Key Properties, Base Class Quick Reference, Save Routing unclassified); `domain-logic-placement.md:3,5` point at sections this plan deletes; 005's stub expects to receive what 004 "moves out". **Triage: amended** — Step 7 records each removed section with base-commit lines and target reference; fixes the two pointers.
5. **Frontmatter already ~1,285 chars, over the 1,024 cap; Step 1 adds twenty questions.** **Triage: amended** — under the cap, compact questions, check Claude Code's handling first.
6. **Statement 7's "does a person edit it" test is superseded by D4 and nothing states the replacement.** **Triage: user decided** — D26 ruled (rules or tracking something relies on — a person or a job; plain RF when nothing is tracked or validated) and written into §0 and §2.
7. **T12's rule block references a type it does not show** (`UsernameAvailability.IsAvailable` is in `skill-rule-command`). **Triage: amended** — both regions.

**Callouts (Should/Could)**

1. "Rename pending 005" untraced (005's scope says nothing; touches `AllBaseClasses.cs:93`, `IBaseClassInterfaces.cs:12`, `base-classes.md:35,50`). **Amended** — Step 7 traces it.
2. Reference index vs no-LazyLoad: `lazy-loading.md` survives 005 (its removal is its own todo). **Amended** — indexed as "pending removal, do not copy".

### Theoretical (not triaged)

- `skill-command` (a side-effect email) shows none of what defines the seam rung; `skill-job-over-entities` illustrates it better. **Taken** (Step 5).
- Bullet 6 (frontmatter) is backed by AC-9, which belongs to 007.
- Rendering a region twice is not an mdsnippets error.

### Read report

Beyond the brief: RF `docs/VisionAlignment.md` §4; `.claude/rules/skills-neatoo.md`; `Order.cs`, `OrderItem.cs`, `OrderItemList.cs`, `ErrorPatterns.cs`, `AllBaseClasses.cs`; plan 003 and its reviews (grep); the description length (wc). Named but unused: none.

**Closing:** CONCERNS. One amendment for the veto; callouts 1, 2, 3 (T16) and 6 needed user decisions — all four made 2026-10-12 and recorded above.

---

## Round 2 — 2026-10-12 (amended plan at `a2e19d1`)

**Verdict: CONCERNS** (one wording veto; five Must callouts, two of which priced the user's decisions)

**Dispositions checked:** V1, C2 (budget — four inline blocks ≈ 200 lines of code, so ~400 is realistic), C4, C5, C6/D26, C7, Should 1, Should 2 resolved. C1 and C3 resolved in direction, with costs below. C2's linking had a gap (callout 3). The D17 row was marked pulled but carried nowhere (callout 5).

**Veto (Pass A)** — the third D18 leg said "caller-decided validation", but D26 says the `[Create]` decides for itself (`Order.Create`), and Step 1 makes it that example; `blazor.md:150` states the operation-decided form. **Triage: fixed** — "the developer's decision, made by the operation or by the caller".

**Callouts (Must)**

1. **`Order.Create` running its rules makes the factory async** (`RunRules` is async-only): `Create` returns `Task<IOrder>`; ~24 sync call sites across `OrderAggregateTests` ×8, `DeletedListTests` ×6, `EntityRootInterfaceTests` ×4, `AggregateLifecycleTests` ×2, `AggregateBoundaryTests`, `AggregateCoverageGapTests`, `ClientServerPairTests`; `OrderEditViewModel.New()` async (inside `skill-save-handler`), `OrderEdit.razor:86`; ~20 render sites across six regions re-render. Mechanical, but touches out-of-scope tests and docs — user's go-ahead needed. Alternative offered: caller-side `RunRules` in `New()`. **Triage: user chose to proceed** (async Create; the `[Create]` decides).
2. **One out-of-scope test cannot be kept as is**: `DeletedListTests.AddThenRemoveNewItem_LeavesOrderCleanButSavable` asserts `IsSavable` on an untouched new Order; setting `CustomerName` would break its `IsFalse(IsModified)`. Its intent (IsNew/IsModified split) survives only on another entity. `AggregateLifecycleTests:192-195` ("still reports valid") is in scope — a direct `factory.Save` still bypasses `IsSavable`. Everything else sets `CustomerName` already; `SubmitDraftOrdersTests` seeds via the repository. **Triage: user approved moving the test's intent to an entity with no required fields, assertions unchanged.**
3. **Five regions the plan links to render in no reference file**: `skill-job-over-entities`, `skill-rule-with-domain-service`, `skill-entity-both-roles`, `skill-viewmodel-over-aggregate`, `skill-value-object-record`. **Triage: amended** — their placeholders are added in this plan (Step 4).
4. **Which aggregate carries the record** (answered under (c)). **Triage: amended** — `Employee`, a new property outside existing regions.
5. **The D17 row was untraced.** **Triage: amended** — Step 7 and the plan Punchlist.

**(c) Record home:** not a typed `EmailAddress` (a constructor that throws on typed input contradicts D2 and T2; D25: "if you need rules it's not a value object anymore") — a value code builds (amount+currency, a date range from a verb, a closed-set code). Not `Order` (`skill-aggregate-interfaces` renders in five files; Order already takes the async ripple). Not `Product` (`skill-quick-start*` render in README, getting-started, entities). Not converting `Employee.Email` (typed, `[Required]`/`[EmailAddress]`, six test sites). **Least disruptive: a new property on `Employee`** — its only region is `skill-plain-computed-getter`; `IEmployee`, `EmployeeRow` mapping and `EmployeeAggregateLifecycleTests` are region-free.

**(d) New:** Should — bullet 2's round-trip should cross the client/server pair (JSON constructor binding and trimming are the risk). **Amended.** Theoretical: reference links may rot until 005 reconciles; CLAUDE.md's generic "after `[Create]`: `IsSavable=true`" gains Order as counterexample; bullet 1's "messages" plural — only `CustomerName`'s remains (fixed to singular).

Read: the amended plan; VisionAlignment:47, :197; `Order.cs`; `Employee.cs`/`IEntityInterfaces.cs` (region scan); the Order-creating tests; `OrderEditViewModelTests`; `ClientServerPairTests`; `ValidateBase` `RunRules` signatures; render-site greps.
