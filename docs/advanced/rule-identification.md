# Rule Identification and Serialization

Rules are assigned stable identifiers at compile time, enabling validation messages to survive serialization round-trips between client and server.

## Why This Matters

When an entity with broken rules is serialized (e.g., sent from server to client), the validation messages must be preserved. After deserialization, when a rule re-executes and passes, it should clear only its own messages - not messages from other rules.

Without stable identification, this scenario fails:
1. Server validates entity, two rules fail (Rule A and Rule B)
2. Entity serialized to client with both error messages
3. User fixes Rule A's condition
4. Rule A re-executes, passes, but incorrectly clears Rule B's message too

## How It Works

The source generator analyzes entity constructors and properties at compile time, capturing rule registrations and assigning deterministic IDs.

### ID Assignment

| Registration Type | ID Source | Example ID |
|-------------------|-----------|------------|
| `AddRule(rule)` | Argument expression | `new AgeValidationRule()` |
| `AddValidation(func, ...)` | Lambda expression | `t => t.Age < 0 ? "Invalid" : ""` |
| `AddAction(func, ...)` | Lambda expression | `t => t.FullName = $"{t.First} {t.Last}"` |
| `[Required]` attribute | Attribute + property | `RequiredAttribute_Email` |

### Generated Code

For each entity, the generator produces a `GetRuleId` override:

```csharp
protected override uint GetRuleId(string sourceExpression)
{
    return sourceExpression switch
    {
        @"new AgeValidationRule()" => 1u,
        @"t => t.Age < 0 ? ""Invalid"" : """"" => 2u,
        @"RequiredAttribute_Email" => 3u,
        _ => base.GetRuleId(sourceExpression)
    };
}
```

IDs are sorted alphabetically and assigned ordinals (1, 2, 3...). This ensures:
- Same source code produces same IDs on client and server
- IDs are small integers, not hashes
- Deterministic ordering across compilations

### CallerArgumentExpression

At runtime, `[CallerArgumentExpression]` captures the exact source text passed to `AddRule`, `AddValidation`, etc.:

```csharp
// In RuleManager
void AddRule<T>(IRule<T> rule,
    [CallerArgumentExpression("rule")] string? sourceExpression = null);
```

When you write:
```csharp
RuleManager.AddRule(new AgeValidationRule());
```

The compiler captures `"new AgeValidationRule()"` as `sourceExpression`. This matches the generated switch case.

### Fallback for Unknown Rules

If a rule's source expression doesn't match any generated case (e.g., rules added dynamically), the base implementation uses a deterministic FNV-1a hash:

```csharp
// Base implementation in ValidateBase
protected virtual uint GetRuleId(string sourceExpression)
{
    return ComputeRuleIdHash(sourceExpression);
}
```

This ensures all rules get stable IDs, even if not captured by the generator.

## No Action Required

This is entirely automatic. You don't need to:
- Assign rule IDs manually
- Configure the generator
- Change how you register rules

The source generator handles everything based on your existing code.

## Technical Details

### Whitespace Normalization

Both the generator and runtime normalize whitespace in source expressions:
- Multiple spaces/newlines collapse to single space
- Leading/trailing whitespace trimmed

This ensures formatting differences don't cause mismatches.

### Attribute Name Normalization

For validation attributes, names are normalized to include the `Attribute` suffix:
- `[Required]` becomes `RequiredAttribute_PropertyName`
- `[RequiredAttribute]` also becomes `RequiredAttribute_PropertyName`

This matches `Type.Name` at runtime, which always includes the suffix.

### Supported Attributes

The generator recognizes these validation attributes:
- `Required`, `StringLength`, `Range`
- `RegularExpression`, `EmailAddress`, `Phone`
- `CreditCard`, `Url`, `MaxLength`, `MinLength`

Custom validation attributes fall back to hash-based IDs.

## See Also

- [Validation and Rules](../validation-and-rules.md) - Rule registration and execution
- [Remote Factory](../remote-factory.md) - Client-server serialization
