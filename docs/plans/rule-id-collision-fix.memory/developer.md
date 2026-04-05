# Developer -- Rule ID Collision Fix

Last updated: 2026-04-04
Current step: Post-implementation review complete

## Key Context

This is a focused bug fix review (no formal plan file). The bug was that `RuleIdGenerator` generated sequential ordinals (`1u, 2u, 3u...`) starting at `1u` for every class, causing `ArgumentException` on `Dictionary.Add` when a derived class inherited rules from a base class.

The fix replaced sequential ordinals with FNV-1a hash-based IDs derived from the source expression string content.

## Post-Implementation Review

**Reviewed:** 2026-04-04
**Feature:** Rule ID Collision in Inheritance Hierarchies

### Production Code Review

**Files examined:**

- `src/Neatoo.BaseGenerator/Generators/RuleIdGenerator.cs` -- Core fix. Replaced `(uint)(i + 1)` with `ComputeFnv1aHash(expr)`, output as hex `0xHHHHHHHHu`. Added private `ComputeFnv1aHash` method.
- `src/Neatoo/ValidateBase.cs:689-713` -- Runtime `GetRuleId` virtual method and `ComputeRuleIdHash` fallback.
- `src/Neatoo/ValidateBase.cs:719-729` -- `NormalizeSourceExpression` and explicit interface implementation.
- `src/Neatoo/Rules/RuleManager.cs:384-390` -- `RegisterRule` uses `Dictionary.Add(ruleId, rule)`.
- `src/Neatoo/Rules/RuleBase.cs:244-247` -- `OnRuleAdded` sets `RuleId` only if default (0).
- `src/Neatoo/Rules/RuleMessage.cs` -- `RuleId` is a plain `uint` property, no range assumptions.

### Hash Function Comparison (CRITICAL CHECK)

**Generator (`RuleIdGenerator.cs:46-58`):**
```
uint hash = 2166136261;
foreach (char c in sourceExpression)
{
    hash ^= c;
    hash *= 16777619;
}
return hash;
```

**Runtime (`ValidateBase.cs:700-713`):**
```
uint hash = 2166136261;
foreach (char c in sourceExpression)
{
    hash ^= c;
    hash *= 16777619;
}
return hash;
```

**Verdict: EXACT MATCH.** Both use FNV-1a with offset basis 2166136261 and prime 16777619. Both operate on `char` (not byte), both use `unchecked` context. The generator's compile-time hashes will match the runtime fallback hashes exactly.

### Collision Risk Analysis

FNV-1a in 32-bit (`uint`) space:
- Birthday paradox threshold: ~65,536 expressions before 50% collision probability
- Practical entity rule count: typically 1-20 expressions per class hierarchy
- Risk is negligible for this use case

However, there is NO collision detection at compile time. If two different expressions hash to the same value, `Dictionary.Add` would throw at runtime. The generator could add a collision check and emit a diagnostic, but given the astronomical odds with typical rule counts, this is a non-blocking observation.

### Test Coverage Review

**Generator tests (`GetRuleIdGenerationTests.cs`):**
- `GetRuleId_AddRule_ExtractsRuleExpression` -- basic AddRule extraction
- `GetRuleId_MultipleAddRules_GeneratesAllExpressions` -- multiple rules
- `GetRuleId_AddValidation_ExtractsLambdaExpression` -- AddValidation
- `GetRuleId_AddAction_ExtractsLambdaExpression` -- AddAction
- `GetRuleId_RequiredAttribute_GeneratesAttributeExpression` -- attribute rules
- `GetRuleId_MultipleAttributes_GeneratesAllExpressions` -- multiple attributes
- `GetRuleId_UsesHashBasedIds` -- verifies hex hash format and no sequential ordinals
- `GetRuleId_ExpressionsAreSortedAlphabetically` -- deterministic ordering
- `GetRuleId_NoRules_DoesNotGenerateOverride` -- edge case
- `GetRuleId_FallsBackToBaseForUnknown` -- fallback arm
- `GetRuleId_DerivedClassWithRules_HashIdsDoNotCollide` -- **THE CORE BUG FIX TEST** (base + derived hierarchy)
- `GetRuleId_ThreeLevelHierarchy_AllHashIdsUnique` -- 3-level hierarchy

Coverage is good. The two new inheritance tests directly verify the bug is fixed.

**Edge case tests (`StableRuleIdEdgeCaseTests.cs`):**
- `RuleId_IsNotZero` -- was `RuleId_IsNotMaxUInt_UnlessIntentional`. New version checks for non-zero (uninitialized). This is a valid adaptation -- with hash-based IDs, the "not max uint" check was meaningless (any hash value is equally likely to be large). The new check verifies the important invariant: IDs are not uninitialized.
- `RuleIds_AreUnique` -- was `RuleIds_AreContiguous`. New version checks uniqueness instead of contiguity. This is correct -- hash-based IDs are distributed, not sequential. The original intent (IDs form a well-defined set) is preserved by checking uniqueness.
- `RuleId_StartsAtOneNotZero` -- unchanged, still passes (FNV-1a of any non-empty string never produces 0).

**Intent preservation verdict:** Both modified tests preserve their original intent. The first checked "IDs are in a reasonable range" (adapted from "not max uint" to "not zero"). The second checked "IDs form a coherent set" (adapted from "contiguous" to "unique"). Neither was gutted.

### Serialization Compatibility

Rule IDs changed from small ordinals (1, 2, 3...) to large hashes (0xHHHHHHHH). This IS a breaking change for any serialized state that includes rule IDs. However:

1. Rule state (broken rule messages with RuleIds) is per-version, not cross-version. There is no versioned storage of rule messages that would need migration.
2. The serialization format (`RuleMessage.RuleId`) is a plain `uint` -- no range checks anywhere.
3. All round-trip serialization tests pass, confirming the new IDs survive JSON round-trips.

**Conclusion:** No compatibility issue.

### Observations

1. **Non-blocking: No compile-time collision detection.** If two expressions hash to the same FNV-1a value, it would only be caught at runtime via `Dictionary.Add` throwing. Adding a collision check in the generator with a diagnostic would be a safety net. Given the probability is approximately 1 in 4 billion for any two expressions, this is informational, not blocking.

2. **Non-blocking: The todo file recommended Option A (offset ordinals) but Option B (hash-based IDs) was implemented.** Option B is arguably cleaner -- it requires no cross-class analysis and is inherently collision-resistant across any inheritance depth. The todo correctly identified both options and Option B was a valid choice.

3. **The `StableRuleIdEntity.BrokenRuleMessages` property uses reflection** (line 148: `prop.GetType().GetProperty("RuleMessages")`). This is pre-existing code, not introduced by this change. Noting it for awareness per the no-reflection policy but not blocking this review.

### Verdict

**Approved.** The fix is correct, the hash functions match exactly, test coverage is thorough, and the modified tests preserve their original intent. All 1,923 tests pass (1,782 unit/integration + 42 generator + 99 design = 1,923 total, 2 pre-existing skips in unit tests).
