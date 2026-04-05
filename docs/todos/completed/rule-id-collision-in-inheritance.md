# Bug: GetRuleId Ordinal Collision in Inheritance Hierarchies

Derived classes that register AddAction/AddValidation rules in their constructors get rule ID collisions with base class rules because the generated `GetRuleId` override starts ordinals at `1u` in every class.

**Created:** 2026-04-04
**Completed:** 2026-04-04
**Status:** Complete
**Origin:** zTreatment thin orchestrator — derived flow orchestrators cannot register AddAction rules without colliding with base class rules

---

## Problem

**Files:**
- `src/Neatoo.BaseGenerator/Generators/RuleIdGenerator.cs:29-35` — ordinals start at `1u` per class
- `src/Neatoo/Rules/RuleManager.cs:384-390` — `RegisterRule` does `Rules.Add(ruleId, rule)` which throws on duplicate keys

**How it works today:**

Each class with `[Factory]` gets a generated `GetRuleId` override that maps `CallerArgumentExpression` strings to sequential `uint` ordinals starting at `1u`:

```csharp
// Generated for BaseClass (has 3 AddAction calls)
protected override uint GetRuleId(string sourceExpression)
{
    return sourceExpression switch
    {
        @"t => t.Foo = t.A + t.B" => 1u,
        @"t => t.Bar = t.C" => 2u,
        @"t => t.Baz = t.D && t.E" => 3u,
        _ => base.GetRuleId(sourceExpression)
    };
}

// Generated for DerivedClass (has 1 AddAction call)
protected override uint GetRuleId(string sourceExpression)
{
    return sourceExpression switch
    {
        @"t => t.ResolveStep()" => 1u,  // COLLISION: 1u already used by base
        _ => base.GetRuleId(sourceExpression)
    };
}
```

When `DerivedClass` constructor calls `RuleManager.AddAction(...)`, the source expression is unique to the derived class, so it hits the derived `GetRuleId` and gets `1u`. But the base constructor already registered a rule with key `1u`. `Dictionary.Add` throws:

```
System.ArgumentException: An item with the same key has already been added. Key: 1
  at RuleManager`1.RegisterRule[TRule](TRule rule, String sourceExpression)
```

**Impact:** Any class that inherits from a Neatoo base class with rules cannot register its own rules in the constructor. The workaround is to move all AddAction rules to the base class constructor, which couples unrelated concerns.

## Fix

The derived class's ordinals must not overlap with the base class's ordinals. Two approaches:

### Option A: Offset derived ordinals by base class count

The source generator knows the base class type. It can count the base class's rule expressions (or read the base's generated `GetRuleId` to find the max ordinal) and start the derived class's ordinals at `baseMax + 1`:

```csharp
// Generated for DerivedClass — starts at 4u (base used 1u-3u)
protected override uint GetRuleId(string sourceExpression)
{
    return sourceExpression switch
    {
        @"t => t.ResolveStep()" => 4u,
        _ => base.GetRuleId(sourceExpression)
    };
}
```

### Option B: Use hash-based IDs instead of ordinals

Replace sequential ordinals with a deterministic hash of the source expression string. Collisions are theoretically possible but practically negligible for `uint` range with short expression strings.

```csharp
protected override uint GetRuleId(string sourceExpression)
{
    return sourceExpression switch
    {
        @"t => t.ResolveStep()" => 0x7A3F_1D2Eu, // hash of expression
        _ => base.GetRuleId(sourceExpression)
    };
}
```

### Recommendation

Option A is simpler and preserves the current deterministic ordinal scheme. The generator already analyzes the class hierarchy for property generation — extending it to count base-class rule expressions is straightforward.

## Resolution

**Option B was implemented** (hash-based IDs). The sequential ordinal scheme was replaced entirely with FNV-1a hash-based IDs derived from source expression string content. This is cleaner than Option A because it requires no cross-class analysis and is inherently collision-resistant across any inheritance depth.

### Changes Made

1. **`src/Neatoo.BaseGenerator/Generators/RuleIdGenerator.cs`** — Replaced `(uint)(i + 1)` with `ComputeFnv1aHash(expr)`. IDs are now output as hex literals (`0xHHHHHHHHu`). The generator's hash function matches `ValidateBase.ComputeRuleIdHash` exactly.
2. **`src/Neatoo.BaseGenerator.Tests/GetRuleIdGenerationTests.cs`** — Updated existing tests and added new tests for hash-based IDs and inheritance scenarios (`GetRuleId_UsesHashBasedIds`, `GetRuleId_DerivedClassWithRules_HashIdsDoNotCollide`, `GetRuleId_ThreeLevelHierarchy_AllHashIdsUnique`).
3. **`src/Neatoo.UnitTest/Integration/Concepts/Serialization/StableRuleIdEdgeCaseTests.cs`** — Updated two tests to work with hash-based IDs instead of small ordinals.

### Breaking Change Note

Rule IDs changed from small ordinals (1, 2, 3...) to large hashes (0xHHHHHHHH). This is a breaking change for any serialized state that includes rule IDs (e.g., session state, cached validation messages). In practice, rule ID serialized state is transient and not persisted long-term.

## Tasks

- [x] Fix `RuleIdGenerator.GenerateGetRuleIdMethod` to use hash-based IDs (Option B chosen over Option A)
- [x] Add a test: base class with N AddAction rules, derived class with M AddAction rules — all register without collision
- [x] Add a test: three-level hierarchy (Base → Middle → Leaf) with rules at each level
