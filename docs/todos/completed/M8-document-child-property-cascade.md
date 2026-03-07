# M8: Document Child Property Change Cascade Behavior

**Priority:** Medium
**Category:** Documentation
**Effort:** Low
**Status:** Not Started

---

## Problem Statement

Neatoo automatically cascades child property changes up through the aggregate hierarchy, triggering parent rule re-validation. This behavior is not well-documented, leading to confusion about:

- How child property changes propagate to parents
- When parent rules re-run in response to child changes
- Why downward cascade is prevented (infinite loop protection)
- What developers need to do (nothing - it's automatic)

---

## Current Behavior

When a child entity's property changes:

1. Child raises property changed event
2. Parent receives notification via `NeatooPropertyChanged`
3. Parent rules that depend on child values are automatically re-validated
4. This cascades up through the entire aggregate hierarchy

**Downward cascade is prevented** to avoid infinite loops. The aggregate is responsible for handling any downward dependencies.

---

## Documentation to Create

### Key Points to Document

1. **Automatic Upward Cascade**
   - Child property changes automatically trigger parent rule re-validation
   - No manual intervention required
   - Works through entire aggregate hierarchy

2. **No Downward Cascade**
   - Changes do not cascade down to prevent infinite loops
   - If parent changes should affect children, the aggregate must handle this explicitly

3. **Testing Implications**
   - Tests should NOT set up cascade behavior manually
   - A cascade-related test failure indicates an aggregate bug, not missing test setup
   - Trust Neatoo to handle cascade; focus tests on aggregate behavior

4. **Cross-Entity Validation**
   - Parent rules that aggregate child values (sums, counts, etc.) automatically re-run
   - Example: "Total must be less than 1000" where Total sums child amounts

---

## Example Documentation

### How Cascade Works

```
Child.Amount = 500
       ↓
Child raises PropertyChanged
       ↓
Parent receives NeatooPropertyChanged
       ↓
Parent.TotalRule re-validates (uses sum of Child.Amount)
       ↓
Parent.IsValid updated
       ↓
Grandparent receives NeatooPropertyChanged
       ↓
... continues up to aggregate root
```

### What Developers Need to Do

**Nothing.** Cascade is automatic. If validation isn't working as expected, the bug is in the aggregate's rule logic, not in cascade setup.

---

## Implementation Tasks

- [ ] Add section to `docs/validation-and-rules.md` about cascade behavior
- [ ] Add section to `docs/aggregates-and-entities.md` about parent-child event flow
- [ ] Update Neatoo skill with cascade documentation
- [ ] Add FAQ: "Why didn't my parent rule re-run when I changed a child?"
- [ ] Add FAQ: "How do I make parent changes affect children?"

---

## Skill Updates Required

Update the Neatoo Claude skill (`~/.claude/skills/neatoo/`) to include:

- Cascade behavior explanation
- Testing guidance (don't mock cascade, test aggregate behavior)
- Common misconceptions

---

## Origin

This documentation gap was identified during zTreatment unit testing guidance development. Question arose about whether tests need to set up cascade behavior - answer is no, Neatoo handles it automatically.
