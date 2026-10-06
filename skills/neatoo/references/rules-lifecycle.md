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
| `this["Prop"].LoadValue(x)` outside a factory operation | Uses `ChangeReason.Load` — the framework skips rules for Load events | Framework use (deserialization, the generated `EntityLazyLoad` setter). Application code sets properties through the setter; inside a factory operation plain assignment is already a baseline load |
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

## The Computed Property Gap

This is the most common bug. An `AddAction` rule computes a derived value, but it never fires during a factory method. The rule:

<!-- snippet: skill-computed-gap-rule -->
<a id='snippet-skill-computed-gap-rule'></a>
```cs
// This rule calculates Total when Quantity or Price changes
RuleManager.AddAction(
    t =>
    {
        t.Total = t.Quantity * t.Price;
        t.RuleHasRun = true;
    },
    t => t.Quantity,
    t => t.Price);
```
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L70-L80' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-computed-gap-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A `[Create]` that sets the inputs and expects the rule to have run. After it returns, `Total` is still 0:

<!-- snippet: skill-create-without-run-rules -->
<a id='snippet-skill-create-without-run-rules'></a>
```cs
/// <summary>
/// WRONG WAY: Sets properties expecting rule to calculate Total.
/// After Create() returns, Total is still 0 because rules were paused.
/// </summary>
[Create]
public void Create()
{
    Quantity = 10;
    Price = 5.00m;
    // Total is NOT calculated here - rule is paused!
}
```
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L83-L95' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-without-run-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## RunRules Works While Paused

`RunRules()` has **no IsPaused guard** — neither in `ValidateBase` nor in `RuleManager`. Call it inside a factory method or a `PauseAllActions()` block and it executes all matching rules immediately. The fix for the gap above:

<!-- snippet: skill-create-run-rules -->
<a id='snippet-skill-create-run-rules'></a>
```cs
/// <summary>
/// RIGHT WAY: Call RunRules at end of factory method.
/// RunRules works even while paused — no IsPaused guard.
/// </summary>
[Create]
public async Task CreateWithRunRules()
{
    Quantity = 10;
    Price = 5.00m;
    await RunRules(RunRulesFlag.All);  // Forces all rules to execute
    // Total is now 50.00
}
```
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L97-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-run-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The same applies to a `[Fetch]`: assign the row's values (the object is paused, so the assignments are a clean baseline), then `await RunRules(RunRulesFlag.All)` to populate the derived values that are not persisted. In an aggregate, run only the rules that write the root itself: the children finished their own `[Fetch]` and are no longer paused, so a root rule that wrote them would mark them modified. Their derived values are loaded from their rows, as `OrderItem` loads `LineTotal`.

**This applies to all factory methods** — `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`. Any factory method that sets properties which have dependent AddAction rules must call `await RunRules(RunRulesFlag.All)` at the end.

**Chaining caveat:** When a rule sets a property while paused, the property value IS updated, but the property change does not cascade to trigger other rules. `RunRulesFlag.All` handles this because it runs ALL rules regardless of triggers — not just rules whose trigger property changed.

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

## RunRulesFlag Reference

`RunRulesFlag` is a `[Flags]` enum:

| Value | Bit | Meaning |
|-------|-----|---------|
| `None` | 0 | Run no rules |
| `NoMessages` | 1 | Run rules with no validation messages |
| `Messages` | 2 | Run rules with messages (known issue: never matches*) |
| `NotExecuted` | 4 | Run rules that have not been executed yet |
| `Executed` | 8 | Run rules that have already been executed |
| `Self` | 16 | Run only this object's rules (skip child properties) |
| `All` | 31 | `NoMessages \| Messages \| NotExecuted \| Executed \| Self` |

**Common usage:**

| Call | When |
|------|------|
| `await RunRules(RunRulesFlag.All)` | Re-run all rules (default). Clears messages first. Use at end of factory methods. |
| `await RunRules(RunRulesFlag.NotExecuted)` | Run only rules that haven't executed yet. Does not clear existing messages. |
| `await RunRules(RunRulesFlag.Self)` | Run this object's rules only, skip child property rules. |
| `await RunRules("PropertyName")` | Run rules triggered by a specific property. |

*`Messages` flag checks `IRule.Messages` which is never populated. `NoMessages` always matches (empty list), `Messages` never matches. Use `All` or `NotExecuted` instead.

## PauseAllActions Usage

`PauseAllActions()` returns `IDisposable`. Never use it inside a factory operation: the operation is already paused, and disposing the `using` resumes the object early.

**Warning:** on an entity, a property set while paused is not marked modified. A batch of edits made inside `PauseAllActions()` on a fetched entity leaves `IsModified` false, so `IsSavable` stays false and the edits are not saved.

While paused, no rules fire between assignments and `PropertyChanged` is not raised; disposing the block does not run the skipped rules, so call `RunRules(RunRulesFlag.All)` if the computed values are needed:

<!-- snippet: skill-pause-all-actions -->
<a id='snippet-skill-pause-all-actions'></a>
```cs
[TestMethod]
public void Gotcha4_PausedPropertyChanges_DoNotTriggerRules()
{
    // Arrange
    var factory = _scope.GetRequiredService<IGotcha4DemoFactory>();
    var entity = factory.Create();

    // Act - Modify properties while paused
    using (entity.PauseAllActions())
    {
        entity.Quantity = 10;
        entity.Price = 5.00m;
    }
    // ResumeAllActions() is called, but rules don't automatically run

    // Assert - Total is NOT calculated
    Assert.AreEqual(0m, entity.Total, "Total should be 0 - rules did not run while paused");
}
```
<sup><a href='/src/Design/Design.Tests/GotchaTests/CommonGotchaTests.cs#L177-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-pause-all-actions' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [validation.md](validation.md) — Rule types (AddAction, AddValidation, AddActionAsync, class-based rules)
- [pitfalls.md](pitfalls.md) — Common mistakes including the computed property gap
- [domain-logic-placement.md](domain-logic-placement.md) — Where to use AddAction vs AddValidation
