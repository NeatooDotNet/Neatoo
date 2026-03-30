# Rules Execution Lifecycle

Rules in Neatoo follow strict execution rules. Understanding when rules fire — and critically, when they do NOT — prevents silent data bugs where computed properties return stale defaults.

## When Rules Fire

| Trigger | Rules Execute? | Details |
|---------|---------------|---------|
| Property setter (not paused) | Yes | `ChildNeatooPropertyChanged` calls `RunRules(propertyName)` |
| Explicit `await RunRules()` | Yes | **Always works — even while paused** |
| Explicit `await RunRules(RunRulesFlag.All)` | Yes | Runs ALL rules regardless of trigger properties |

## When Rules Do NOT Fire

| Situation | Why | What To Do |
|-----------|-----|------------|
| During `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]` | `FactoryStart()` calls `PauseAllActions()` before your code runs | Call `await RunRules(RunRulesFlag.All)` at end of factory method |
| During `PauseAllActions()` block | `IsPaused = true` — `ChildNeatooPropertyChanged` skips rules | Call `await RunRules(RunRulesFlag.All)` inside or after the block |
| `LoadValue` / `this["Prop"].LoadValue(x)` | Uses `ChangeReason.Load` — framework skips rules for Load events | Call `await RunRules(RunRulesFlag.All)` after loading |
| During JSON deserialization | `OnDeserializing` calls `PauseAllActions()` | Automatic — `OnDeserialized` calls `ResumeAllActions()` |

## ResumeAllActions Does NOT Run Rules

This is the most common source of confusion. `ResumeAllActions()` (called by `FactoryComplete` and `PauseAllActions` dispose) does the following:

- Sets `IsPaused = false`
- Recalculates cached `IsValid` and `IsSelfValid`
- Fires `PropertyChanged` for `IsValid`/`IsSelfValid` **only if they changed**
- Recalculates `IsBusy`

It does **NOT**:

- Run any rules
- Fire `PropertyChanged` for properties changed while paused
- Queue or replay skipped rule triggers
- Run AddAction computed property rules

## RunRules Works While Paused

`RunRules()` has **no IsPaused guard** — neither in `ValidateBase` nor in `RuleManager`. Call it inside a factory method or a `PauseAllActions()` block and it executes all matching rules immediately.

```csharp
[Create]
public async Task Create()
{
    Quantity = 10;
    UnitPrice = 5.00m;
    // Rules are paused (FactoryStart called PauseAllActions)
    // but RunRules works anyway:
    await RunRules(RunRulesFlag.All);
    // Total is now calculated
}

[Fetch]
internal async Task Fetch(int id, [Service] IRepository repo)
{
    var data = repo.GetOrder(id);
    this["Quantity"].LoadValue(data.Quantity);
    this["UnitPrice"].LoadValue(data.UnitPrice);
    // LoadValue uses ChangeReason.Load — rules skipped
    // Force rules to compute derived values:
    await RunRules(RunRulesFlag.All);
}
```

**Chaining caveat:** When a rule sets a property while paused, the property value IS updated, but the property change does not cascade to trigger other rules. `RunRulesFlag.All` handles this because it runs ALL rules regardless of triggers — not just rules whose trigger property changed.

## The Computed Property Gap

This is the most common bug. AddAction rules compute derived values, but they never fire during factory methods:

```csharp
public Order(IEntityBaseServices<Order> services) : base(services)
{
    // This rule computes Total when Quantity or UnitPrice changes
    RuleManager.AddAction(
        t => t.Total = t.Quantity * t.UnitPrice,
        t => t.Quantity, t => t.UnitPrice);
}

// WRONG: Total is 0 after Create returns — rule never fired
[Create]
public void Create()
{
    Quantity = 10;
    UnitPrice = 5.00m;
    // Total is still 0!
}

// RIGHT: Call RunRules to force computed values
[Create]
public async Task Create()
{
    Quantity = 10;
    UnitPrice = 5.00m;
    await RunRules(RunRulesFlag.All);
    // Total is now 50.00
}
```

**This applies to all factory methods** — `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`. Any factory method that sets properties which have dependent AddAction rules must call `await RunRules(RunRulesFlag.All)` at the end.

## Factory Method Lifecycle

```
1. Generated factory code calls FactoryStart(FactoryOperation)
   → PauseAllActions() → IsPaused = true
   → All property change events, rules, modification tracking suspended

2. Your factory method runs ([Create], [Fetch], etc.)
   → Property setters work — values ARE set
   → But ChildNeatooPropertyChanged skips because IsPaused
   → No rules fire, no PropertyChanged fires, no modification tracking
   → Call await RunRules(RunRulesFlag.All) here if needed

3. Generated factory code calls FactoryComplete(FactoryOperation)
   → ResumeAllActions() → IsPaused = false
   → Validity recalculated, meta state reset
   → NO rules run, NO PropertyChanged for changed properties
   → For EntityBase: MarkNew/MarkOld/MarkUnmodified based on operation

4. Factory returns to caller
   → Object is now unpaused
   → Next property change triggers rules normally
```

## RunRulesFlag Enum Reference

```csharp
[Flags]
public enum RunRulesFlag
{
    None = 0,          // Run no rules
    NoMessages = 1,    // Run rules with no validation messages
    Messages = 2,      // Run rules with messages (known issue: never matches*)
    NotExecuted = 4,   // Run rules that haven't been executed yet
    Executed = 8,      // Run rules that have already been executed
    Self = 16,         // Run only this object's rules (skip child properties)
    All = NoMessages | Messages | NotExecuted | Executed | Self
}
```

**Common usage:**

| Call | When |
|------|------|
| `await RunRules(RunRulesFlag.All)` | Re-run all rules (default). Clears messages first. Use at end of factory methods. |
| `await RunRules(RunRulesFlag.NotExecuted)` | Run only rules that haven't executed yet. Does not clear existing messages. |
| `await RunRules(RunRulesFlag.Self)` | Run this object's rules only, skip child property rules. |
| `await RunRules("PropertyName")` | Run rules triggered by a specific property. |

*`Messages` flag checks `IRule.Messages` which is never populated. `NoMessages` always matches (empty list), `Messages` never matches. Use `All` or `NotExecuted` instead.

## PauseAllActions Usage

`PauseAllActions()` returns `IDisposable`. Use for batch updates where intermediate rule execution is unnecessary:

```csharp
// Batch update — no rules fire between assignments
using (entity.PauseAllActions())
{
    entity.FirstName = "John";
    entity.LastName = "Doe";
    entity.Email = "john@example.com";
    // Optional: force rules if computed values needed before resume
    await entity.RunRules(RunRulesFlag.All);
}
// IsPaused is now false
// Future property changes trigger rules normally
// BUT: no rules ran for the batch changes unless RunRules was called above
```

## Related

- [validation.md](validation.md) — Rule types (AddAction, AddValidation, AddActionAsync, class-based rules)
- [pitfalls.md](pitfalls.md) — Common mistakes including the computed property gap
- [domain-logic-placement.md](domain-logic-placement.md) — Where to use AddAction vs AddValidation
