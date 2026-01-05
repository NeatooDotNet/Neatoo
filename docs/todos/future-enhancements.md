# Future Enhancements

Improvement ideas extracted from TODO comments (January 2026). These can be converted to GitHub issues when ready.

---

## 1. Replace IRule.OnRuleAdded with Factory Method Pattern

**Priority:** Low | **Category:** Design Improvement

**Current Behavior:**
`IRule.OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex)` is called after a rule is added to initialize its unique index and rule manager reference.

**Proposed Change:**
Replace this callback pattern with a factory method that constructs rules with the required dependencies already injected.

**Benefits:**
- Cleaner initialization - rules fully initialized at construction
- Immutable rule instances possible
- Removes mutable state from rule lifecycle

**File:** `src/Neatoo/Rules/RuleBase.cs`

---

## 2. Parent Tracking for Unassigned Objects

**Priority:** Low | **Category:** Design Note

**Observation:**
If an object isn't assigned to another `IBase`, it will still consider the previous parent to be its Parent.

**Context:**
In `EntityBase.ChildNeatooPropertyChanged`, when a child property changes, the parent may still be referenced even if the child was removed from the object graph.

**Potential Solutions:**
- Clear parent reference when object is removed from property
- Add explicit "detach" mechanism
- Track assignment state separately from parent reference

**File:** `src/Neatoo/EntityBase.cs`

---

## 3. Refactor NeatooBaseJsonTypeConverter Type Resolution

**Priority:** Low | **Category:** Code Quality

**Observation:**
The type resolution code in `NeatooBaseJsonTypeConverter.Read` that finds `EntityBase<>` properties could be cleaner.

**Context:**
The code walks up the type hierarchy to find the generic `EntityBase<>` base type. This works but is not elegant.

**Potential Solutions:**
- Extract to helper method with clear name
- Consider caching type metadata
- Use reflection helpers

**File:** `src/Neatoo/RemoteFactory/Internal/NeatooBaseJsonTypeConverter.cs`

---

*Extracted from TODO comments - 2026-01-04*
