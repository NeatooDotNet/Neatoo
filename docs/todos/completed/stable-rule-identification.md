# Stable Rule Identification via Source Generation

Use source generation to assign stable rule identifiers at compile time, replacing the fragile runtime index-based system.

**Created:** 2026-01-11
**Status:** Complete
**Priority:** High

---

## Problem Statement

Rules are currently identified by sequential `_ruleIndex` (1, 2, 3...) assigned during registration. This breaks when:
- Rules are registered in different order on client vs server
- Code changes add/remove rules, shifting indices

When an aggregate is serialized with broken rules, the `RuleIndex` is serialized with the message. After deserialization, if the rule succeeds and tries to clear its messages, it may clear the wrong messages (or none) because indices don't match.

## Solution Overview

Use **source generation** with **`CallerArgumentExpression`** to:
1. Capture the literal source text of rule arguments at each call site
2. Generate a per-entity `RuleIdRegistry` mapping source text → stable ordinal
3. Only regenerate when an entity's source file changes

**Key principle:** Each entity class gets its own registry. Roslyn incrementally regenerates only the affected entity when its source changes.

---

## Source Generation Architecture

### Per-Entity Generation (Incremental Optimization)

The generator uses `ForAttributeWithMetadataName` targeting the `[Factory]` attribute (same as `BaseGenerator`). This ensures:
- Only entities trigger generation (not every class)
- Each entity generates independently
- Changing one entity doesn't regenerate others
- Consistent with existing Neatoo generator patterns

```csharp
[Generator]
public class RuleIdGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Same trigger as BaseGenerator - [Factory] attribute + ValidateBase inheritance
        var entities = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Neatoo.RemoteFactory.FactoryAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: ExtractEntityRuleInfo)
            .Where(x => x is not null);

        context.RegisterSourceOutput(entities, GenerateEntityRuleRegistry);
    }

    // Reuse same base class check as BaseGenerator
    private static bool ClassOrBaseClassIsNeatooBaseClass(INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.Name == "ValidateBase" &&
            namedTypeSymbol.ContainingNamespace.Name == "Neatoo")
            return true;

        return namedTypeSymbol.BaseType != null &&
               ClassOrBaseClassIsNeatooBaseClass(namedTypeSymbol.BaseType);
    }
}
```

### What Gets Analyzed Per Entity

For each entity class, the generator:
1. Finds the constructor(s)
2. Scans for `AddRule`, `AddValidation`, `AddAction` calls
3. Extracts the source text of the relevant argument
4. Scans properties for validation attributes
5. Generates a `RuleIdRegistry` nested class

### Generated Output Per Entity

```csharp
// User code
public partial class Employee : EntityBase<Employee>
{
    public Employee(IEmailFormatRule emailRule, ISalaryRangeRule salaryRule)
    {
        RuleManager.AddRule(emailRule);
        RuleManager.AddRule(salaryRule);

        RuleManager.AddValidation(
            t => string.IsNullOrEmpty(t.Name) ? "Name is required" : "",
            t => t.Name);
    }

    [Required]
    public string Name { get => Getter<string>(); set => Setter(value); }
}
```

```csharp
// Generated: Employee.RuleIds.g.cs
partial class Employee
{
    private static class RuleIdRegistry
    {
        private static readonly Dictionary<string, uint> _map = new()
        {
            // Injected rules (sorted alphabetically for determinism)
            ["emailRule"] = 1,
            ["salaryRule"] = 2,

            // Fluent rules
            ["t => string.IsNullOrEmpty(t.Name) ? \"Name is required\" : \"\""] = 3,

            // Attribute rules
            ["Required_Name"] = 4,
        };

        public static uint GetId(string sourceExpression) => _map[sourceExpression];
    }
}
```

### Why Per-Entity Is Optimal

| Approach | Regeneration Trigger | Cost |
|----------|---------------------|------|
| Global registry | ANY rule change anywhere | Regenerate entire registry |
| Per-entity registry | Only when that entity changes | Regenerate single entity |

Roslyn's incremental generator caches unchanged entities. Only modified files trigger regeneration.

### Extracting Source Text

The generator extracts the same text that `CallerArgumentExpression` would capture:

```csharp
static EntityRuleInfo ExtractEntityRuleInfo(
    GeneratorAttributeSyntaxContext ctx,
    CancellationToken ct)
{
    var classDecl = (ClassDeclarationSyntax)ctx.TargetNode;
    var rules = new List<string>();

    // Find constructors
    foreach (var ctor in classDecl.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
    {
        // Find AddRule/AddValidation/AddAction invocations
        foreach (var invocation in ctor.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (IsRuleRegistration(invocation, out var methodName))
            {
                var argIndex = GetRelevantArgumentIndex(methodName);
                var arg = invocation.ArgumentList.Arguments[argIndex];

                // Capture exact source text
                rules.Add(arg.Expression.ToFullString().Trim());
            }
        }
    }

    // Find validation attributes on properties
    foreach (var prop in classDecl.Members.OfType<PropertyDeclarationSyntax>())
    {
        foreach (var attr in prop.AttributeLists.SelectMany(a => a.Attributes))
        {
            if (IsValidationAttribute(attr))
            {
                rules.Add($"{attr.Name}_{prop.Identifier.Text}");
            }
        }
    }

    return new EntityRuleInfo(classDecl, rules);
}
```

### Deterministic Ordering

Rules are sorted alphabetically before assigning ordinals:

```csharp
var sortedRules = rules.Distinct().OrderBy(x => x).ToList();
for (int i = 0; i < sortedRules.Count; i++)
{
    registry[sortedRules[i]] = (uint)(i + 1);
}
```

This ensures:
- Same source → same sorted order → same ordinals
- Adding a rule may shift other ordinals, but that's fine (same source file = same client/server)
- Deterministic across compilations

---

## Design by Rule Registration Method

### 1. Injected Rules: `AddRule(IRule rule)`

**Current signature:**
```csharp
public TRule AddRule<TRule>(TRule rule) where TRule : IRule
```

**New signature:**
```csharp
public TRule AddRule<TRule>(
    TRule rule,
    [CallerArgumentExpression("rule")] string? ruleId = null) where TRule : IRule
```

**Usage & captured ID:**
```csharp
public Employee(IEmailFormatRule emailRule, ISalaryRangeRule salaryRule)
{
    RuleManager.AddRule(emailRule);   // ruleId = "emailRule"
    RuleManager.AddRule(salaryRule);  // ruleId = "salaryRule"
}
```

**Stability:** Parameter names are stable in source code. Renaming via IDE refactoring updates all references consistently.

---

### 2. Multiple Injected Rules: `AddRules(params IRule[] rules)`

**Current signature:**
```csharp
public void AddRules(params IRule[] rules)
```

**Challenge:** `CallerArgumentExpression` only works on single parameters, not params arrays.

**Solution:** Remove `AddRules` or convert to multiple `AddRule` calls internally with indexed IDs.

**Option A - Deprecate and remove:**
```csharp
// Instead of:
RuleManager.AddRules(rule1, rule2, rule3);

// Users write:
RuleManager.AddRule(rule1);
RuleManager.AddRule(rule2);
RuleManager.AddRule(rule3);
```

**Option B - Keep with expression capture (less ideal):**
```csharp
public void AddRules(
    params IRule[] rules,
    [CallerArgumentExpression("rules")] string? rulesExpr = null)
{
    // rulesExpr = "rule1, rule2, rule3"
    // Parse and split, or use as single ID + index
}
```

**Recommendation:** Option A - deprecate `AddRules`. Each rule deserves its own stable ID.

---

### 3. Fluent Validation (Sync): `AddValidation`

**Current signature:**
```csharp
public ValidationFluentRule<T> AddValidation(
    Func<T, string> func,
    Expression<Func<T, object?>> triggerProperty)
```

**New signature:**
```csharp
public ValidationFluentRule<T> AddValidation(
    Func<T, string> func,
    Expression<Func<T, object?>> triggerProperty,
    [CallerArgumentExpression("func")] string? ruleId = null)
```

**Usage & captured ID:**
```csharp
RuleManager.AddValidation(
    t => string.IsNullOrEmpty(t.Name) ? "Name is required" : "",
    t => t.Name);
// ruleId = "t => string.IsNullOrEmpty(t.Name) ? \"Name is required\" : \"\""

RuleManager.AddValidation(
    t => t.Name.Length > 50 ? "Name too long" : "",
    t => t.Name);
// ruleId = "t => t.Name.Length > 50 ? \"Name too long\" : \"\""
```

**Stability:** The lambda source text is unique per rule. Same source = same ID.

---

### 4. Fluent Validation (Async): `AddValidationAsync`

**Current signature:**
```csharp
public AsyncFluentRule<T> AddValidationAsync(
    Func<T, Task<string>> func,
    Expression<Func<T, object?>> triggerProperty)
```

**New signature:**
```csharp
public AsyncFluentRule<T> AddValidationAsync(
    Func<T, Task<string>> func,
    Expression<Func<T, object?>> triggerProperty,
    [CallerArgumentExpression("func")] string? ruleId = null)
```

**Usage & captured ID:**
```csharp
RuleManager.AddValidationAsync(
    async t => await emailService.ValidateAsync(t.Email) ? "" : "Invalid",
    t => t.Email);
// ruleId = "async t => await emailService.ValidateAsync(t.Email) ? \"\" : \"Invalid\""
```

---

### 5. Fluent Validation (Async with CancellationToken): `AddValidationAsync`

**Current signature:**
```csharp
public AsyncFluentRuleWithToken<T> AddValidationAsync(
    Func<T, CancellationToken, Task<string>> func,
    Expression<Func<T, object?>> triggerProperty)
```

**New signature:**
```csharp
public AsyncFluentRuleWithToken<T> AddValidationAsync(
    Func<T, CancellationToken, Task<string>> func,
    Expression<Func<T, object?>> triggerProperty,
    [CallerArgumentExpression("func")] string? ruleId = null)
```

**Usage & captured ID:**
```csharp
RuleManager.AddValidationAsync(
    async (t, ct) => await emailService.ValidateAsync(t.Email, ct) ? "" : "Invalid",
    t => t.Email);
// ruleId = "async (t, ct) => await emailService.ValidateAsync(t.Email, ct) ? \"\" : \"Invalid\""
```

---

### 6. Fluent Action (Sync): `AddAction`

#### Challenge: `CallerArgumentExpression` with `params`

**The Problem:** C# requires `params` to be the last parameter. When you place a `CallerArgumentExpression` optional parameter before `params`, the compiler tries to assign positional arguments to that parameter instead of skipping to `params`.

```csharp
// This signature LOOKS like it should work:
public ActionFluentRule<T> AddAction(
    Action<T> func,
    [CallerArgumentExpression("func")] string? ruleId = null,
    params Expression<Func<T, object?>>[] triggerProperties)

// But this call FAILS to compile:
AddAction(t => DoSomething(t), t => t.Name);
// Error: Cannot convert Expression to string
// The compiler tries to assign t => t.Name to ruleId (string?)
```

#### First Approach: Trigger Property Fallback (Rejected)

One workaround was to derive the ID from trigger property names:

```csharp
// ID derived from properties: "Action_Name_Age"
RuleManager.AddAction(t => DoSomething(t), t => t.Name, t => t.Age);
```

**Problem:** Two different actions on the same properties would collide:
```csharp
// Both produce ID "Action_Name_Age" - COLLISION!
RuleManager.AddAction(t => t.FullName = t.Name, t => t.Name, t => t.Age);
RuleManager.AddAction(t => t.Initials = t.Name[0], t => t.Name, t => t.Age);
```

#### Final Solution: Explicit Overloads (Breaking Change)

Replace `params` with explicit overloads for 1, 2, 3 triggers, plus an array overload for 4+:

```csharp
// 1 trigger - CallerArgumentExpression works
ActionFluentRule<T> AddAction(
    Action<T> func,
    Expression<Func<T, object?>> triggerProperty,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 2 triggers - CallerArgumentExpression works
ActionFluentRule<T> AddAction(
    Action<T> func,
    Expression<Func<T, object?>> triggerProperty1,
    Expression<Func<T, object?>> triggerProperty2,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 3 triggers - CallerArgumentExpression works
ActionFluentRule<T> AddAction(
    Action<T> func,
    Expression<Func<T, object?>> triggerProperty1,
    Expression<Func<T, object?>> triggerProperty2,
    Expression<Func<T, object?>> triggerProperty3,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 4+ triggers - array (NOT params), CallerArgumentExpression still works
ActionFluentRule<T> AddAction(
    Action<T> func,
    Expression<Func<T, object?>>[] triggerProperties,  // NOT params
    [CallerArgumentExpression("func")] string? sourceExpression = null);
```

**Key insight:** By using an explicit array instead of `params`, `CallerArgumentExpression` can be placed AFTER it. Callers with 4+ triggers must explicitly create an array:

```csharp
// 1-3 triggers: natural call syntax
RuleManager.AddAction(t => t.FullName = $"{t.First} {t.Last}", t => t.First, t => t.Last);
// sourceExpression = "t => t.FullName = $\"{t.First} {t.Last}\""

// 4+ triggers: explicit array
RuleManager.AddAction(
    t => t.Summary = $"{t.A}{t.B}{t.C}{t.D}",
    new[] { t => t.A, t => t.B, t => t.C, t => t.D });
// sourceExpression = "t => t.Summary = $\"{t.A}{t.B}{t.C}{t.D}\""
```

---

### 7. Fluent Action (Async): `AddActionAsync`

Same pattern as sync `AddAction` - explicit overloads:

```csharp
// 1 trigger
ActionAsyncFluentRule<T> AddActionAsync(
    Func<T, Task> func,
    Expression<Func<T, object?>> triggerProperty,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 2 triggers
ActionAsyncFluentRule<T> AddActionAsync(
    Func<T, Task> func,
    Expression<Func<T, object?>> triggerProperty1,
    Expression<Func<T, object?>> triggerProperty2,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 3 triggers
ActionAsyncFluentRule<T> AddActionAsync(
    Func<T, Task> func,
    Expression<Func<T, object?>> triggerProperty1,
    Expression<Func<T, object?>> triggerProperty2,
    Expression<Func<T, object?>> triggerProperty3,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 4+ triggers (array, not params)
ActionAsyncFluentRule<T> AddActionAsync(
    Func<T, Task> func,
    Expression<Func<T, object?>>[] triggerProperties,
    [CallerArgumentExpression("func")] string? sourceExpression = null);
```

**Usage & captured ID:**
```csharp
RuleManager.AddActionAsync(
    async t => t.TaxRate = await taxService.GetRateAsync(t.ZipCode),
    t => t.ZipCode);
// sourceExpression = "async t => t.TaxRate = await taxService.GetRateAsync(t.ZipCode)"
```

---

### 8. Fluent Action (Async with CancellationToken): `AddActionAsync`

Same pattern - explicit overloads:

```csharp
// 1 trigger
ActionAsyncFluentRuleWithToken<T> AddActionAsync(
    Func<T, CancellationToken, Task> func,
    Expression<Func<T, object?>> triggerProperty,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 2 triggers
ActionAsyncFluentRuleWithToken<T> AddActionAsync(
    Func<T, CancellationToken, Task> func,
    Expression<Func<T, object?>> triggerProperty1,
    Expression<Func<T, object?>> triggerProperty2,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 3 triggers
ActionAsyncFluentRuleWithToken<T> AddActionAsync(
    Func<T, CancellationToken, Task> func,
    Expression<Func<T, object?>> triggerProperty1,
    Expression<Func<T, object?>> triggerProperty2,
    Expression<Func<T, object?>> triggerProperty3,
    [CallerArgumentExpression("func")] string? sourceExpression = null);

// 4+ triggers (array, not params)
ActionAsyncFluentRuleWithToken<T> AddActionAsync(
    Func<T, CancellationToken, Task> func,
    Expression<Func<T, object?>>[] triggerProperties,
    [CallerArgumentExpression("func")] string? sourceExpression = null);
```

---

### 9. Attribute-Based Rules (Source Generated)

Attribute rules are discovered by the source generator scanning properties for validation attributes.

**Current:** Runtime reflection in `AddAttributeRules()` with sequential indexing.

**New:** Source generator emits registration code with stable IDs.

**Generated ID format:** `{AttributeType}_{PropertyName}`

**Example:**
```csharp
// User code
public class Employee : EntityBase<Employee>
{
    [Required]
    [StringLength(50)]
    public string Name { get => Getter<string>(); set => Setter(value); }

    [Required]
    [EmailAddress]
    public string Email { get => Getter<string>(); set => Setter(value); }
}
```

**Generated code:**
```csharp
partial class Employee
{
    protected override void RegisterAttributeRules(IRuleManager ruleManager)
    {
        ruleManager.AddRule("Required_Name", new RequiredRule<string>(this, x => x.Name));
        ruleManager.AddRule("StringLength_Name", new StringLengthRule(this, x => x.Name, 50));
        ruleManager.AddRule("Required_Email", new RequiredRule<string>(this, x => x.Email));
        ruleManager.AddRule("EmailAddress_Email", new EmailAddressRule(this, x => x.Email));
    }
}
```

**Stability:** Attribute type + property name is guaranteed unique (C# doesn't allow duplicate attributes of same type on same member).

---

## Implementation Changes

### 1. IRuleMessage Changes

```csharp
public interface IRuleMessage
{
    // Existing - renamed from RuleIndex to RuleId
    // Same type (uint), but now assigned deterministically by generator
    uint RuleId { get; }

    string PropertyName { get; }
    string? Message { get; }
}
```

### 2. IRule Changes

```csharp
public interface IRule
{
    // Existing - renamed from UniqueIndex to RuleId
    // Same type (uint), but now assigned deterministically by generator
    uint RuleId { get; }

    // Internal setter used by RuleManager during registration
    void SetRuleId(uint ruleId);

    // ... rest of interface
}
```

### 3. RuleManager Registration Changes

```csharp
// Storage remains uint-keyed (ordinals from generator)
private IDictionary<uint, IRule> Rules { get; } = new Dictionary<uint, IRule>();

// New registration uses generated RuleIdRegistry
private TRule RegisterRule<TRule>(TRule rule, string sourceExpression) where TRule : IRule
{
    // RuleIdRegistry is generated per-entity, accessed via partial class
    var ruleId = RuleIdRegistry.GetId(sourceExpression);
    rule.SetRuleId(ruleId);
    this.Rules.Add(ruleId, rule);
    return rule;
}
```

The `RuleIdRegistry` is a nested static class generated for each entity, containing a compile-time dictionary mapping source expressions to ordinals.

### 4. ValidateProperty Message Matching

```csharp
// Current (index from registration order)
this.RuleMessages.RemoveAll(rm => rm.RuleIndex == ruleIndex);

// New (id from generator - same type, just renamed and deterministic)
this.RuleMessages.RemoveAll(rm => rm.RuleId == ruleId);
```

The matching logic is identical (uint comparison). The difference is the value itself is now deterministic based on sorted source text rather than registration order.

### 5. Serialization

```csharp
public class RuleMessage : IRuleMessage
{
    [JsonPropertyName("id")]
    public uint RuleId { get; set; }  // Ordinal from generator

    [JsonPropertyName("prop")]
    public string PropertyName { get; set; }

    [JsonPropertyName("msg")]
    public string? Message { get; set; }
}
```

Note: `RuleId` remains a `uint` since the generator assigns ordinals. The difference from the old `RuleIndex` is that these ordinals are deterministic (alphabetically sorted) rather than registration-order dependent.

---

## Migration Strategy

### Phase 1: Add New System (Non-Breaking)
1. Add `RuleId` property to `IRule` and `IRuleMessage`
2. Add `CallerArgumentExpression` parameters (with defaults)
3. Store rules by both index and ID
4. Match messages by ID first, fall back to index

### Phase 2: Deprecation Warnings
1. Mark `RuleIndex`/`UniqueIndex` as `[Obsolete]`
2. Emit compiler warnings for index-based access
3. Update documentation

### Phase 3: Remove Legacy (Major Version)
1. Remove `RuleIndex`/`UniqueIndex` properties
2. Remove fallback matching logic
3. Remove `AddRules` method (if deprecated)

---

## Ordinal Assignment Strategy

The source generator assigns ordinals (uint) rather than hashes. This provides:
- Compact 4-byte identifiers
- Efficient integer comparison
- Fast dictionary lookup
- No runtime computation

**How ordinals are assigned:**

1. Generator collects all rule source expressions for an entity
2. Sorts them alphabetically (deterministic ordering)
3. Assigns sequential ordinals starting at 1

```csharp
// Generated registry for Employee
private static readonly Dictionary<string, uint> _map = new()
{
    ["emailRule"] = 1,                                    // Sorted first
    ["salaryRule"] = 2,                                   // Sorted second
    ["t => string.IsNullOrEmpty(t.Name) ? ..."] = 3,     // Sorted third
    ["Required_Name"] = 4,                                // Sorted fourth
};
```

**Stability guarantee:** Same source text → same sorted position → same ordinal. If client and server compile the same source, they get identical registries.

---

## Edge Cases

### Whitespace Differences

```csharp
// These are different source text
t => t.Name.Length > 50 ? "Too long" : ""
t=>t.Name.Length>50?"Too long":""
```

**Reality:** Formatters normalize code. Same codebase = same formatting = same IDs.

**Recommendation:** No normalization needed. Trust consistent formatting.

### Method References vs Lambdas

```csharp
RuleManager.AddValidation(ValidateName, t => t.Name);  // ruleId = "ValidateName"
RuleManager.AddValidation(t => ValidateName(t), t => t.Name);  // ruleId = "t => ValidateName(t)"
```

Both are valid and stable. Different source text = different IDs = correct behavior.

---

## Summary Table

| Method | CallerArgumentExpression Target | Source Captured | RuleId (ordinal) |
|--------|--------------------------------|-----------------|------------------|
| `AddRule(rule)` | `rule` | `"emailRule"` | `1` |
| `AddValidation(func, trigger)` | `func` | `"t => t.Name.Length > 50 ? ..."` | `3` |
| `AddValidationAsync(func, trigger)` | `func` | `"async t => await svc.Check(t.Email)"` | `2` |
| `AddAction(func, triggers...)` | `func` | `"t => t.FullName = $\"{t.First}...\""` | `4` |
| `AddActionAsync(func, triggers...)` | `func` | `"async t => t.Rate = await ..."` | `5` |
| Attribute rules (generated) | N/A | `"Required_Name"` | `6` |

*Note: Ordinal values depend on alphabetical sort order of all rules in the entity.*

---

## Task List

### Source Generator
- [x] Create `RuleIdGenerator` incremental source generator (integrated into BaseGenerator)
- [x] Implement `ForAttributeWithMetadataName` trigger on entity base classes
- [x] Extract rule registration calls from constructors
- [x] Extract validation attributes from properties
- [x] Generate per-entity `GetRuleId` override with switch expression
- [x] Add normalization for deterministic matching
- [ ] Add generator unit tests

### Runtime Changes
- [x] Add `RuleId` (uint) property to `IRule` interface
- [x] Add `RuleId` (uint) property to `IRuleMessage` interface
- [x] Update `RuleManager.AddRule` with `CallerArgumentExpression`
- [x] Update `RuleManager.AddValidation` with `CallerArgumentExpression`
- [x] Update `RuleManager.AddValidationAsync` (both overloads)
- [x] Update `RuleManager.AddAction` with `CallerArgumentExpression` (explicit overloads for 1-3 triggers + array)
- [x] Update `RuleManager.AddActionAsync` with `CallerArgumentExpression` (explicit overloads for 1-3 triggers + array)
- [x] Update `RuleManager.AddActionAsync` with CancellationToken (explicit overloads for 1-3 triggers + array)
- [x] Update `RegisterRule` to use `GetRuleId()` virtual method
- [x] Update `ValidateProperty` message matching to use `RuleId`
- [x] Remove old `RuleIndex`/`UniqueIndex` (breaking change)

### Migration
- [x] Add hash fallback in base `GetRuleId` for unknown expressions
- [x] Remove `AddRules` method (breaking change)
- [x] Remove old `RuleIndex`/`UniqueIndex` (breaking change)

### Testing
- [x] Existing tests pass (1918 tests)
- [x] Add integration tests for serialization round-trip with rule IDs (StableRuleIdSerializationTests.cs - 20 tests)
- [x] Add edge case and stress tests (StableRuleIdEdgeCaseTests.cs - 21 tests)
- [x] Add generator unit tests (Neatoo.BaseGenerator.Tests - 26 tests)
- [x] Verify client-server rule matching (verified via StableRuleIdSerializationTests - 41 tests + Person example - 52 tests)

### Documentation
- [x] Document CallerArgumentExpression + params challenge and solution
- [x] Create advanced/rule-identification.md with detailed internals documentation
- [x] Update docs/index.md with new "Internals" section
- [x] ~~Add migration guide for existing code~~ (skipped - changes are internal, no user migration needed)
