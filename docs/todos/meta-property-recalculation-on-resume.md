# Meta Property Recalculation After ResumeAllActions

**Status:** In Progress
**Priority:** High
**Created:** 2026-03-01
**Last Updated:** 2026-03-01 (Architecture investigation complete -- Option C confirmed, Option D rejected)

---

## Problem

When a parent entity is paused (during `[Create]`, `[Fetch]`, or `PauseAllActions()`), child entities that go through their own independent factory lifecycle can become modified. The parent's `EntityPropertyManager` receives `PropertyChanged` events from these children but **drops them** because it's paused (line 137-141 in `EntityPropertyManager.cs`). When `ResumeAllActions()` is called, the cached `IsModified` / `IsSelfModified` values are never recalculated from the current property states — so the parent stays `IsModified = false` even though a child is genuinely modified.

### Observed in zTreatment

- `VisitHub` (EntityBase, root) → `ConsultationEntity` (child, partial property) → `Visit` → `TreatmentEntity`
- During `VisitHub.[Fetch]`, VisitHub is paused. `ConsultationEntity` is fetched via its own factory (fully resumed). Then `HandlePendingTreatmentGeneration` sets `TreatmentEntity` on Visit (which is not paused).
- Visit becomes modified → Consultation becomes modified → Consultation raises PropertyChanged
- VisitHub's PropertyManager is paused → **event dropped**
- VisitHub.FactoryComplete → ResumeAllActions → **no recalculation** → `VisitHub.IsModified = false`
- Result: child graph is modified, parent says it isn't
- Current workaround: explicit `MarkModified()` calls in VisitHub after treatment generation

### Scope: Not Just IsModified

The same pattern likely affects all meta properties tracked through `CheckIfMetaPropertiesChanged`:
- `IsModified` / `IsSelfModified` (EntityPropertyManager cached values)
- `IsValid` / `IsSelfValid` (ValidatePropertyManager / RuleManager)
- `IsSavable` (derived from IsModified + IsValid)
- `IsBusy`

## Solution

Needs careful design. Key concerns:

### 1. Performance Impact
When `ResumeAllActions()` recalculates and raises `PropertyChanged("IsModified")`, what does that trigger? Potential cascade:
- Parent entities listening for child IsModified changes
- Rule execution (if rules depend on meta properties)
- UI bindings responding to PropertyChanged
- `CheckIfMetaPropertiesChanged()` on ancestor entities
- Need to understand the full event chain before changing behavior

### 2. Was This Intentional?
The current behavior may have been a deliberate choice to avoid expensive recalculation during factory operations. Need to investigate:
- Are there scenarios where recalculation on resume would cause problems?
- Is there a reason `OnDeserialized()` recalculates but `ResumeAllActions()` doesn't?
- Could recalculation during resume cause re-entrant issues?

### 3. Which Properties Need Recalculation?
- `EntityPropertyManager.IsModified` and `IsSelfModified` — clearly affected
- `ValidatePropertyManager` equivalents (IsValid, rule messages) — investigate
- `RuleManager` state — does it need to re-evaluate after resume?

### 4. Where Should the Fix Go?
Options to evaluate:
- `EntityPropertyManager.ResumeAllActions()` — recalculate cached IsModified/IsSelfModified
- `ValidateBase.ResumeAllActions()` — recalculate meta properties after property manager resumes
- `EntityBase.FactoryComplete()` — recalculate only after factory operations, not general pause/resume
- Some combination

---

## Plans

- [Meta Property Recalculation After ResumeAllActions](../plans/meta-property-recalculation-on-resume.md)

---

## Tasks

- [ ] Research: trace full event chain when IsModified changes on resume
- [ ] Research: identify all meta properties affected (IsValid, IsBusy, etc.)
- [ ] Research: determine if current behavior was intentional (git history)
- [ ] Research: identify performance implications of recalculation on resume
- [ ] Design: decide where recalculation belongs (PropertyManager vs Base class vs FactoryComplete)
- [ ] Write reproduction test in Neatoo test suite
- [ ] Implement fix
- [ ] Verify zTreatment MarkModified workaround is no longer needed

---

## Progress Log

### 2026-03-01
- Investigated zTreatment pending `MarkModified()` changes in VisitHub.cs
- Traced the exact chain: VisitHub paused → child factory completes independently → child modified → PropertyChanged dropped by paused PropertyManager → ResumeAllActions doesn't recalculate
- Identified `EntityPropertyManager.ResumeAllActions()` (lines 126-133) as the location where recalculation is missing
- Noted `OnDeserialized()` (lines 185-186) already does this recalculation, confirming it's needed
- User raised concern: recalculation may cascade events with performance/correctness implications
- User noted this likely affects IsValid and other meta properties too, not just IsModified

### 2026-03-01 (Architect Review)
- Completed deep codebase analysis of all pause/resume code paths
- Confirmed: ValidatePropertyManager and EntityPropertyManager both have stale cache bug on resume
- Confirmed: ValidateListBase and EntityListBase already fixed in v10.7.1 (commit ab5eead)
- Traced full event chain: EPM.PropertyChanged -> _PropertyManager_PropertyChanged -> CheckIfMetaPropertiesChanged
- Identified interface dispatch nuance: VPM.ResumeAllActions called via interface (not EPM override) from ValidateBase
- Analyzed double-resume pattern in EntityBase.FactoryComplete -- ordering is correct for the fix
- Verified no behavioral regression for: Create, Fetch, Insert/Update, user PauseAllActions using pattern
- Confirmed fix is conservative: recalculate cached values from existing property state, no rule re-execution
- Git history shows no deliberate decision to skip recalculation -- it was simply never added to property managers
- Created implementation plan at docs/plans/meta-property-recalculation-on-resume.md

### 2026-03-01 (Developer Review)
- Developer identified CRITICAL finding: interface dispatch analysis in plan was WRONG
- C# interface re-implementation means EPM.ResumeAllActions is ALWAYS called (even through IValidatePropertyManager)
- VPM.ResumeAllActions is NEVER called for EntityBase objects
- The proposed VPM fix would never run for EntityBase objects
- Developer proposed three options: (A) EPM handles all 5 properties, (B) EPM calls base first, (C) refactor to override
- Plan sent back to architect

### 2026-03-01 (Architect Revision)
- Independently verified developer's interface dispatch finding -- confirmed correct
- Evaluated all three options, chose Option C (refactor EPM to use override instead of new)
- Discovered secondary bug: VPM.Property_PropertyChanged processes events during EPM pause because VPM.IsPaused is never set
- Option C fixes both the recalculation bug and the secondary event-processing-during-pause bug
- Traced full call chains for PauseAllActions, ResumeAllActions, FactoryComplete with Option C -- all correct
- Identified that EPM.IsPaused property must be removed (use VPM's IsPaused instead)
- Confirmed EntityBase.PauseAllActions/ResumeAllActions double-calls become harmless NO-OPs
- Updated plan with revised design, set status to Under Review (Developer)

### 2026-03-01 (Developer Re-Review)
- Re-reviewed revised plan (Option C design)
- Verified all three original concerns are correctly addressed
- Verified code changes against actual source: VPM.IsPaused `protected set` accessible from EPM, override signatures match, IsPaused guards correct
- Verified all four call chain traces (Pause, Resume, FactoryComplete, using pattern) against source code
- Confirmed secondary bug analysis (VPM event processing during pause) is correct
- Approved plan, created Implementation Contract with 3 phases and 4 verification gates
- Plan status set to Ready for Implementation

### 2026-03-01 (Architect Investigation: Pause Architecture + SetParent)
- Investigated user's Option D hypothesis: "Remove IsPaused from property managers entirely"
- **Option D rejected.** Root cause: MetaState tracking problem. When PM caches update during pause, `CheckIfMetaPropertiesChanged` fires via the `_PropertyManager_PropertyChanged` path, which calls `ResetMetaState()` unconditionally. This captures the fresh PM values into MetaState during pause, eliminating the delta that `ResumeAllActions` needs to detect changes. At resume, there would be no delta to detect and no entity-level PropertyChanged events would fire.
- Secondary rejection reasons: (1) O(n) recalculation per property change during batch loading, (2) changes ValidateBase behavior which currently works correctly
- **Option C confirmed as correct architecture.** Deferred PM cache update creates a detectable delta between stale MetaState and fresh-at-resume PM caches.
- **SetParent independently verified as safe.** All SetParent calls go through NeatooPropertyChanged path (no IsPaused guard) or are direct calls. None blocked by VPM.Property_PropertyChanged guard.
- Updated plan with: Option D analysis section, red-green testing strategy, revised implementation contract (tests-first phasing)
- Updated implementation contract to require tests written FIRST (Phase 1), then production code (Phases 2-3), with explicit verification that tests FAIL before the fix

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass
- [ ] Design project builds successfully
- [ ] Design project tests pass
- [ ] zTreatment MarkModified workaround can be removed

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
