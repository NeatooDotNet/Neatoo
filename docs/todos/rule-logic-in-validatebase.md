# Move Rule Logic from ValidateBase to RuleManager

**Status:** Blocked
**Priority:** Medium
**Created:** 2026-03-14
**Last Updated:** 2026-03-14

---

## Problem

There is rule infrastructure logic in ValidateBase that looks like it belongs in RuleManager. Specifically the hash logic — `GetRuleId`, `ComputeRuleIdHash`, and `NormalizeSourceExpression` methods (ValidateBase.cs lines ~683-723). These compute stable rule IDs from source expressions using FNV-1a hashing and normalize whitespace in caller argument expressions. This is rule identity/registration logic, not entity validation logic.

## Solution

Evaluate whether this rule logic should move to RuleManager. Get the architect's assessment of where these responsibilities belong and whether moving them is feasible.

---

## Clarifications

Architect confirmed understanding, no questions. Key findings from comprehension check:

- **Architecturally correct:** The logic belongs in RuleManager — rule ID computation is a rule infrastructure concern, not entity validation logic.
- **Non-trivial move:** The virtual `GetRuleId` on `ValidateBase<T>` is the override point for the source generator (`RuleIdGenerator`), which emits a compile-time `switch` expression on the entity's partial class. Moving to RuleManager requires coordinated changes across `ValidateBase`, `RuleManager`, `IValidateBaseInternal`, and `RuleIdGenerator.cs`.
- **Current call chain:** `RuleManager.RegisterRule()` calls back into the entity via `IValidateBaseInternal.GetRuleId()` → `ValidateBase.GetRuleId()` (virtual) → either generated override or FNV-1a hash fallback.

---

## Requirements Review

**Reviewer:** [pending]
**Reviewed:** [pending]
**Verdict:** Pending

### Relevant Requirements Found

[pending]

### Gaps

[pending]

### Contradictions

[pending]

### Recommendations for Architect

[pending]

---

## Plans

[pending]

---

## Tasks

- [x] Architect comprehension check (Step 2) — Ready, no questions
- [ ] Business requirements review (Step 3)
- [ ] Architect plan creation & design (Step 4)
- [ ] Developer review (Step 5)
- [ ] Implementation (Step 7)
- [ ] Verification (Step 8)

---

## Progress Log

### 2026-03-14
- Created todo from user observation that rule ID hash logic in ValidateBase.cs (GetRuleId, ComputeRuleIdHash, NormalizeSourceExpression) may belong in RuleManager
- Discovery: hash logic is in ValidateBase.cs lines ~683-723; no hash logic exists in RuleManager currently
- Next: Architect comprehension check
- Architect comprehension check complete — confirmed the move is architecturally correct but non-trivial due to source generator override coupling
- Paused — user not working on this now. Resume at Step 3 (business requirements review) when ready

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

[pending]
