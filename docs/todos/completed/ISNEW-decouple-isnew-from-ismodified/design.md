# ISNEW: Decouple IsNew from IsModified — Create Means Savable, Not Modified

**Priority:** High
**Category:** Core Semantics
**Effort:** Medium-Large (three-plan arc; includes baseline fixes)
**Status:** Design decided — ready to plan. Implementation not started.
**Decided:** 2026-08-19 — Keith: Neatoo should not weld IsNew into IsModified. Create means
savable but not modified. Surfaced during zTreatmentFluency E2E work (the fluency Treatment
tab's unsaved-changes guard); the tension has appeared repeatedly before that.
**Design finalized:** 2026-08-21 — Keith + Claude, full design session. Opt-IN dirty create via
`MarkModified()` in the `[Create]` body; attach-marking replaces the weld's child-flow job;
pre-existing lifecycle findings fixed first in the same arc; ships as **0.31.0 (breaking)**.

---

## The Tension

`IsModified` is asked two different questions, and today it can only answer one of them:

1. **"Does this need persistence?"** — drives `IsSavable`, Save-button enablement, and the
   `Save()` guard.
2. **"Would navigating away lose the user's work?"** — drives unsaved-changes prompts and
   navigation guards.

For a fetched-then-edited entity the answers coincide. For a **Create'd** entity they diverge:
a freshly created object *needs persistence* (it must be savable) but holds *no user work*.
Today Neatoo forces both answers to `true`, structurally:

```csharp
// EntityBase.cs:159 — the weld
public virtual bool IsModified =>
    this.PropertyManager.IsModified || this.IsDeleted || this.IsNew || this.IsSelfModified;

// EntityBase.cs:174 — why the weld exists: savability rides on IsModified
public virtual bool IsSavable => this.IsModified && this.IsValid && !this.IsBusy && !this.IsChild;
```

Because `IsModified` is a live derivation (not state), the `IsNew=true / IsModified=false`
state is **unreachable** — no API call can produce it. Whether an untouched new object
represents "user work" is application semantics the framework cannot decide (a user-clicked
"New Invoice" arguably is work; a system-derived prescription is not), so the framework's job
is to make both states reachable and both policies expressible.

### How it bites in practice

- **Rich Create is the intended pattern.** Factory-op writes run paused, so a `[Create]` that
  fully populates a domain model lands with clean properties. The only structural dirt is the
  welded `IsNew` — the one term the domain cannot remove.
- **Every UI that binds an unsaved-changes prompt to `IsModified` cries wolf on new
  entities.** In zTreatment the fluency Treatment tab warned about unsaved changes on a
  freshly derived, untouched prescription — including immediately after a successful save.
- **App code is already hand-splitting the flags**: zTreatment's `SignsViewModel` guards with
  `a.IsNew ? a.HasData : a.IsModified` — a per-callsite reinvention of "new-but-untouched
  isn't dirty."

---

## Decided Design

### The Why — canonical explanation (Keith, 2026-08-21: document this in code, plan, skill, docs, and briefly in the README with a link — "even I might forget the why")

> `IsModified` answers one question: *does this object graph differ from its baseline — the
> state the factory operation left it in?* It drives unsaved-changes guards: true means
> discarding the object loses work. `IsNew` answers a different question: *does persistence
> not know this object yet?* It drives insert-vs-update routing. Savability needs **either**:
> `IsSavable = (IsModified || IsNew) && IsValid && !IsBusy && !IsChild`. An untouched new
> object is savable — inserting it is meaningful — but it is not modified, because nothing is
> lost by walking away. A `[Create]` whose result *is itself user work* (a "New" button)
> declares that by calling `MarkModified()` in its body. And it is dirt — never `IsNew` —
> that aggregates up the object graph (same as CSLA's `ITrackStatus` split), which is why
> attaching a child to a live graph marks the child modified.

**COMMON MISTAKE (document in skill + docs):** calling `MarkModified()` in a `[Create]` "so
it can be saved." Savability is automatic for new objects via the `IsNew` term in
`IsSavable` — `MarkModified()` is *only* for creates that represent user work worth guarding.
Sprinkling it into every create re-welds the two meanings and makes unsaved-changes guards
cry wolf again — the exact bug ISNEW removes.

### Target semantics

| State | IsNew | IsModified | IsSavable |
|---|---|---|---|
| Fetched, untouched | false | false | false |
| Fetched, edited | false | true | true |
| **Created, untouched — incl. factory-populated children** | **true** | **false** | **true** |
| Created with `MarkModified()` in the `[Create]` body | true | true | true |
| Created, then edited | true | true | true |
| Fetched root + user attaches a new child | false (root) | true | true |

### Core changes (EntityBase)

```csharp
public virtual bool IsModified  => PropertyManager.IsModified || IsDeleted || IsSelfModified;  // IsNew term removed
public virtual bool IsSavable   => (IsModified || IsNew) && IsValid && !IsBusy && !IsChild;
// Save() guard (EntityBase.cs:450): NotModified only when !(IsModified || IsNew)
//   (current guard's IsSelfModified term is redundant — IsModified already includes it; simplify)
// MarkNew() stays pure: IsNew = true. Nothing else.
```

### Why the weld can go — its two hidden jobs, each replaced

The `IsNew` term in `IsModified` was secretly doing two jobs. (This is why prior attempts to
remove it kept getting reverted — each removal broke one of them.)

1. **Making creates savable.** Replaced by `IsSavable = (IsModified || IsNew) && …` and the
   same admission in the `Save()` guard. This removes the *reason* the weld ever existed.
   Note: adding `|| IsNew` to `IsSavable` changes nothing observable on its own, because
   today `IsNew` already implies `IsModified`.
2. **Making new children dirty their parents.** Today's only upward channel is
   `child.IsNew → child.IsModified → list cache → Items property → PropertyManager →
   parent.IsModified` (pinned verbatim in `EntityListBaseStateTransitionTests.cs:126`).
   **`IsNew` itself never propagates upward** — it is per-object routing state; only
   `IsModified`/`IsValid`/`IsBusy` aggregate. Cutting the weld kills this chain at its first
   link. Replaced by **attach-marking** (below), which re-feeds the same chain from its
   second link using state instead of derivation.

### Opt-in dirty create — `MarkModified()`, no new API

A `[Create]` that represents real user work opts in by calling the existing
`MarkModified()` in its body:

```csharp
[Create]   // derivation / computed initial state — the common case
public void Create([Service] ...) { /* populate everything */ }   // lands IsNew=true, IsModified=false, savable

[Create]   // "New" button where the record itself is the user's work
public void Create() { MarkModified(); }                          // lands IsNew=true, IsModified=true
```

Why opt-IN (not an opt-out like `MarkClean()`): the `[Create]` body runs **before**
`FactoryComplete(Create)`/`MarkNew()`, so an opt-out must be a suppression flag consulted by
dirt that hasn't arrived yet — pre-state racing the lifecycle. Opt-in is ordinary state
written in the body: `IsMarkedModified` already survives `FactoryComplete`, already rides the
wire (serialized via `IEntityMetaProperties`), and is already cleared by `MarkUnmodified()`
after save. The whole lifecycle stays inside the existing four marks: `MarkNew`/`MarkOld`
for routing, `MarkModified`/`MarkUnmodified` for dirt. Per-call granularity puts the policy
exactly where the knowledge lives — the create method knows whether it represents user intent.

### Attach-marking — the child-flow replacement

`EntityListBase.InsertItem`'s un-paused branch already calls `itemInternal.MarkModified()` on
attach — it just **exempts new items** (`if (!item.IsNew)`, `EntityListBase.cs:242-245`)
because the weld made marking them redundant. The change is removing the exemption:

- **`InsertItem` (un-paused): mark every attached item.** Paused adds (the canonical
  factory-population path and deserialization) do not mark — so factory-built children stay
  baseline-clean and rich Create with children lands `IsModified=false`.
- **`SetItem` (un-paused): same treatment** for the incoming item (today it marks nothing —
  pre-existing gap; the plan should also decide what happens to the replaced item).
- **Entity-child property assignment (un-paused): `MarkModified()` the assigned child** in
  `EntityProperty.OnPropertyChanged`'s Value branch.

  **CORRECTED 2026-08-21 (ISNEW-004 plan review, veto B1).** An earlier version of this
  section claimed assigning a Neatoo child "never dirties the parent at all — a pre-existing
  quirk this fixes." That is **false for new children, which is the common case**, and the
  error made the property channel look like a bonus when it is in fact mandatory. What is
  true: the *property* never self-dirties (`EntityPropertyManager.cs:46`, "Never consider
  ourself modified if holding a Neatoo object"), but the *parent* is dirtied through the
  child, because `EntityProperty.IsModified => IsSelfModified || EntityChild?.IsModified`
  and a new child's `IsModified` is true **only via the weld**. So assigning a created child
  to a live parent dirties it today, and cutting the weld without property attach-marking
  would break that — the same silent-data-loss shape as the list case, not a quirk fix.
  This channel is therefore mandatory on equal footing with the list channel.

  **List-valued child properties need no marking.** `EntityChild` is typed
  `IEntityMetaProperties`, which entity *lists* also implement — and a list has no
  `MarkModified` (`EntityListBase.IsMarkedModified => false`). It does not need one: a list's
  `IsModified` aggregates from its children, and children are attach-marked as they are added
  to the list, so dirt reaches the parent through the existing channel. Marking applies to
  entity children only.

  **Placement matters:** the mark must live inside `EntityProperty.OnPropertyChanged`'s Value
  branch. `LazyLoadEntityProperty` calls `base.OnPropertyChanged` and then *undoes*
  `IsSelfModified` — an undo written against `IsSelfModified` would not undo a mark placed on
  the child. Lazy assignment is additionally insulated because the generated lazy setter uses
  `LoadValue`, which deliberately raises no `Value` notification. Placing the mark anywhere
  else (`SetValue`, `HandleNonNullValue`) loses that insulation.

Attach-marking is **mandatory, not optional**: without it, a user-added new child no longer
enables Save on a fetched parent (silent data loss — the child's insert is skipped by
`if (child.IsModified)` cascades, and `parent.IsSavable` stays false).

### Canonical pattern rule: factory-built children live inside the container's own op

Factory-time population must happen while the container is paused — i.e., items are created
and added **inside the list's own `[Create]`/`[Fetch]`** (each factory op pauses its own
target; there is no cascade). The Person example is the reference implementation:
`PersonPhoneList.Fetch` adds `personPhoneModelFactory.Fetch(entity)` items while the list is
paused; `PersonPhoneList.Update` calls `personPhoneModelFactory.Save(...)` per item so each
child gets its own `FactoryComplete(Insert/Update)` → `MarkUnmodified()` + `MarkOld()`.
Items appended to an already-resumed list (e.g., by a parent's `[Create]` after
`itemsFactory.Create()` returns) are attach-marked — correctly, because from the container's
perspective that is a post-baseline graph change.

### DID NOT DO (rejected 2026-08-21, with reasons)

- **`MarkClean()` opt-out + `IsCreateClean` state** — ordering hack (suppression flag racing
  `MarkNew`), confusing new state, new serialization surface. Opt-in via `MarkModified()`
  needs none of it.
- **Per-type `NewIsModified` knob (gated weld)** — makes `IsModified`'s meaning
  configuration-dependent (consumers can't interpret it without knowing the type's setting),
  and clean aggregates would require opting out every type in the family because child welds
  leak dirt up through lists.
- **Structural counting of `child.IsNew` into list/property `IsModified`** (the original
  recommendation in this todo) — cannot distinguish factory-built children from user-attached
  ones, so rich Create with children still cries wolf; and it leaves an untouched new child
  `IsModified=false`, silently breaking `if (child.IsModified) factory.Save(child)` cascades.
- **CSLA-style stamp inside `MarkNew()`** — routes create-dirt through `IsMarkedModified`,
  flipping `IsSelfModified` to true after every default Create (observable change even for
  apps that never opt in; `docs/reference/api.md` pins `IsSelfModified=false` after Create).

---

## Pre-Existing Findings — Fix FIRST, Same Arc (decided 2026-08-21)

Discovered during the design analysis. ISNEW makes `IsNew` and post-save lifecycle correctness
load-bearing for savability, so these land **before** the semantic flip (Plan 1):

1. **Design.Domain OrderAggregate lifecycle is unfaithful to the framework.**
   - `Order.Fetch` builds items via `itemFactory.Create()` + `LoadValue` — those items end
     `IsNew=true` (the generated factory calls `FactoryComplete(Create)` → `MarkNew()`;
     nothing ever marks them old). The comment at `Order.cs:143` ("After Fetch: each item
     IsNew=false") is wrong, and `Order.Update`'s `if (item.IsNew)` dispatch would re-insert
     fetched items.
   - `Order.Update` writes the repository directly and never cleans child state — after save,
     child dirt persists and the root stays `IsModified=true`. `OrderItem.cs:154-158` claims
     a `FactoryComplete(Update)` cascade to items that **does not exist** (verified: generated
     factories call `FactoryStart`/`FactoryComplete` on the single target only; Neatoo has no
     graph cascade).
   - Fix to the Person-example canonical: item `[Fetch]` for loads; per-item
     `factory.Save(...)` in the list `[Update]`; correct all lifecycle comments.
2. **No end-to-end aggregate `Save()` coverage.** Exactly one test file in Neatoo.UnitTest
   calls `.Save(`; no integration test saves an aggregate and asserts post-save graph state
   (unit tests invoke `list.FactoryComplete(Update)` manually to simulate it). Add real
   aggregate save lifecycle integration tests before changing savability semantics.
3. **`ValidateListBase.FactoryComplete` skips cache recalculation** — it sets
   `IsPaused = false` directly (`ValidateListBase.cs:573-576`) instead of the recalculation
   `ResumeAllActions` does (`ValidateListBase.cs:544-556`), so list validity/busy caches (and
   `EntityListBase`'s modified cache) can go stale after factory ops that add items while
   paused. Audit and fix.
4. **Paused adds skip `MarkAsChild`/`SetContainingList`** (`EntityListBase.cs:247-250` run
   only un-paused) — children fetched via the canonical paused path have `IsChild=false` and
   no `ContainingList` (so `item.Delete()` bypasses list routing). These calls are
   baseline-neutral; plan should decide whether to run them for paused adds too.

---

## Sequencing

Tracked in [todo.md](./todo.md)'s Plan Index (plans ISNEW-001…ISNEW-005):

- **Verified baseline (ISNEW-001, ISNEW-002, ISNEW-003):** Design.Domain OrderAggregate lifecycle
  fixes (Person-canonical), E2E aggregate save integration tests, list cache recalc audit
  (findings 1-3; decide 4).
- **The flip (ISNEW-004):** EntityBase changes (IsModified / IsSavable / Save guard),
  attach-marking (InsertItem / SetItem / EntityProperty), update pinned tests, add new
  state-transition and serialization round-trip tests.
- **Docs & release (ISNEW-005):** skill, guides, api reference + samples, Design.Domain comments,
  README brief mention, release notes, version bump to 0.31.0.

---

## Breaking Change & Migration — 0.31.0

Version: `0.30.2 → 0.31.0`. **Release notes must be explicit and clear** (Keith, 2026-08-21);
same for the Neatoo skill.

Consumer-facing changes to document:

- **`IsNew` no longer implies `IsModified`.** After Create: `IsNew=true, IsModified=false,
  IsSelfModified=false`, and the entity **is savable** (`IsSavable` now admits `IsNew`).
- **`if (entity.IsModified) Save()` call sites silently skip fresh creates** — migrate to
  `IsSavable` (or `IsModified || IsNew`).
- **Unsaved-changes guards bound to `IsModified` stop crying wolf** on untouched new
  entities — the ISNEW motivation. Guards that *want* to warn on any new object should use
  `IsNew || IsModified`.
- **A `[Create]` representing user work opts into dirty** with `MarkModified()` in the body.
- **User-attached items are now explicitly marked modified** (new items included; previously
  only non-new items were marked, new ones were dirty via the weld). Observable shift:
  user-attached items report `IsMarkedModified`/`IsSelfModified = true`.
- **Assigning an entity child to a parent property keeps dirtying the parent, by a new
  mechanism — no observable change.** (Corrected twice on 2026-08-21. An earlier draft
  claimed it "previously never did"; it did, for new children, through the weld. A second
  draft then claimed assigning an *unmodified existing* child would newly dirty the parent —
  that was implemented and rejected during ISNEW-004: it breaks the separate, deliberate
  invariant that a property HOLDING an unmodified child is not modified, which has its own
  unit coverage. The mark is scoped to **new** children, giving exact parity with the weld.
  Widening it is a distinct decision, not a side effect of this one.)
- zTreatment's hand-splits (`a.IsNew ? a.HasData : a.IsModified`) remain correct and become
  deletable.
- Factory save routing is untouched: generated `Save` dispatches on `IsDeleted`/`IsNew` only
  and never consults `IsModified` (verified in generated code); `IsNew` and
  `IsMarkedModified` already round-trip the wire.

## Tests

Pinned tests that change (in-scope by the 2026-08-19 decision):

- `EntityBaseStateTests.IsModified_WhenIsNew_ReturnsTrue` → returns false
- `TwoContainerMetaStateTests.Create_TwoContainer_IsModified_ReturnsTrue` → false
  (`Create_TwoContainer_IsSavable_ReturnsTrue` must still pass — via the `IsNew` term)
- `TwoContainerMetaStateTests.Create_ServerSideOnly_IsModified_ReturnsTrue` → false
- `EntityListBaseStateTransitionTests.Add_NewItem_ToCleanList_ListBecomesModified` — same
  outcome, new mechanism (attach-mark instead of weld); update the comment at line 126
- `docs/reference/api.md` snippet source (`src/samples/ApiReferenceSamples.cs`) — asserts
  `IsModified=true` after Create

New coverage to add (Plan 2, on top of Plan 1's E2E save tests):

- Create → clean + savable (flat, and rich with factory-built children — the motivating case)
- Create with `MarkModified()` in body → dirty; round-trips remote Create
- Fetched root + user attach of new child → parent modified + savable; child insert not
  skipped by `IsModified`-guarded cascades; add-then-remove returns parent to clean
- Entity-child property assignment dirties parent; LazyLoad path stays clean
- Post-save: graph fully clean (root and children), second Save blocked NotModified
- Created-then-`Delete()`d root: `IsDeleted` routing unchanged

## Doc / Skill Touchpoints

Every touchpoint carries **The Why** (canonical block above) at depth appropriate to the
medium — Keith, 2026-08-21: "document the why in the code, plan, skill, docs and even mention
it briefly in the README with a link to the docs."

- **Code comments** — XML remarks on `EntityBase.IsModified`, `IsSavable`, `MarkNew`,
  `MarkModified` and the attach-mark site in `EntityListBase.InsertItem` state the why (the
  two questions, savable-vs-modified, dirt-not-IsNew aggregates)
- `skills/neatoo/SKILL.md` — **verified stale lines (2026-08-21): `:100`** (Key Properties
  table row `IsSavable | bool | IsValid && IsModified && !IsBusy && !IsChild`) **and `:241`**
  ("`IsSavable` requires both `IsValid` and `IsModified`"). An earlier version of this list
  cited a line reading "True after Create (because IsNew)" — that string does not exist in
  the file; do not hunt for it. Add the Why and the COMMON MISTAKE (MarkModified is not
  needed for savability), plus `references/collections.md`; copy to `~/.claude/skills/neatoo/`
  per repo rule
- **Repo-root `CLAUDE.md`** — its State Properties section defines `IsSavable` as
  `(IsModified && IsValid && !IsBusy && !IsChild)` (`:88`-ish). This file is loaded into every
  agent session in this repo, so leaving it stale actively mis-teaches future work. Added to
  the touchpoint list 2026-08-21 (ISNEW-004 plan review, Pass A callout 3)
- `docs/guides/change-tracking.md` — full why treatment; `docs/guides/entities.md`,
  `docs/guides/remote-factory.md`, `docs/reference/api.md` (+ `src/samples/`)
- **`README.md` — brief mention only, linking to the change-tracking guide** for the full why
- `src/Design/Design.Domain` — `Order.cs`, `OrderItem.cs`, `OrderItemList.cs` (lifecycle
  comments largely rewritten by Plan 1), `BaseClasses/AllBaseClasses.cs`, `CommonGotchas.cs`
- `docs/release-notes/v0.31.0.md` (template per CI standards; link back to this todo)
- Skill/docs should state plainly: **`IsNew` does not aggregate** — per-object routing state;
  only `IsModified`/`IsValid`/`IsBusy` flow upward.

---

## CSLA Findings (fetched 2026-08-19, `MarimerLLC/csla` @ main, `Source/Csla/Core/BusinessBase.cs`)

Neatoo's current outcome is CSLA-faithful — but the *mechanism* differs in a way that matters
for this decision.

**Yes, CSLA's Create yields IsNew = true AND IsDirty = true**, and says so in words:

```csharp
/// <summary>
/// Marks the object as being a new object. This also marks the object
/// as being dirty and ensures that it is not marked for deletion.
/// </summary>
protected virtual void MarkNew()
{
  IsNew = true;
  IsDeleted = false;
  MetaPropertyHasChanged("IsNew");
  MetaPropertyHasChanged("IsDeleted");
  MarkDirty();
}

protected virtual void MarkOld()
{
  IsNew = false;
  MetaPropertyHasChanged("IsNew");
  MarkClean();
}
```

**But CSLA stamps; Neatoo welds.** CSLA's getter carries no `IsNew` term:

```csharp
public virtual bool IsDirty => IsSelfDirty || (_fieldManager != null &&
  FieldManager.IsDirty());
```

The dirtiness of a new CSLA object is a one-time flag write inside `MarkNew`, not a
structural derivation. In Neatoo the conflation is welded into the `IsModified` expression,
so the `IsNew=true / IsModified=false` state is *unreachable* rather than merely *unreached*.

**Why Lhotka stamped it** — the same savability coupling Neatoo has:

```csharp
var result = IsDirty && IsValid && !IsBusy;   // IsSavable
```

A Create'd object must be savable, and savability rides on dirty — so `MarkNew` declares
dirtiness. CSLA even concedes the point internally: `DataPortal_Create` loads defaults via
`LoadProperty`, which deliberately does **not** dirty the fields — yet the object must claim
dirtiness anyway to keep Save enabled. The classic app-level counter-workaround in CSLA shops
is `IsDirty && !IsNew` on unsaved-changes prompts: the recurring hand-split, again.

**Child lists depend on the stamp in CSLA too:** list dirt flows through `child.IsDirty`,
which `MarkNew` set. Either lineage needs an explicit replacement for the child flow if the
conflation is removed — in the final design, attach-marking.

**CSLA's IsNew is strictly per-object — verified 2026-08-21** (Keith asked whether CSLA moves
IsNew up the graph; it does not):

```csharp
// Core/BusinessBase.cs — plain instance state, no child consultation
public bool IsNew { get; private set; } = true;    // note: CSLA defaults new-until-proven-old

// BusinessListBase.cs — lists have no persistence identity (same as EntityListBase.IsNew => false)
bool ITrackStatus.IsNew => false;

// BusinessListBase.IsDirty — the ONLY upward channel is dirt
foreach (C item in DeletedList)
  if (!item.IsNew) return true;
foreach (C child in this)
  if (child.IsDirty) return true;

// Child_Update — cascade guard is child.IsDirty; the data portal routes
// insert-vs-update on each child's OWN IsNew internally
foreach (var child in this)
  if (child.IsDirty)
    dp.UpdateChild(child, parameters);
```

CSLA's `ITrackStatus` splits exactly as Neatoo's flags do: per-object routing state
(`IsNew`, `IsDeleted`) vs aggregating state (`IsDirty`, `IsValid`, `IsBusy`). A new child
enables Save on a CSLA parent only because `MarkNew` stamped it dirty and dirt aggregates —
the same architecture ISNEW preserves with the stamp relocated to attachment. The
`if (child.IsDirty)` cascade guard above is also the pattern that attach-marking keeps
working for untouched new children (and that structural `child.IsNew` counting would have
silently broken).

**Where the final design lands relative to CSLA:** `IsSavable = (IsModified || IsNew) && …`
removes the reason `MarkNew` ever needed to claim dirtiness — a deliberate departure CSLA
couldn't make (a generation of WinForms Save buttons was bound to `IsDirty`; Neatoo has no
such legacy). The stamp philosophy survives, relocated to the semantically honest events:
dirt stamps at **attachment** (framework, automatic) and at **create-that-is-user-work**
(domain author, `MarkModified()` in the body) — never at creation per se.
