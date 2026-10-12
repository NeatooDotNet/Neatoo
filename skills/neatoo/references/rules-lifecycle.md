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
| During `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]` | `FactoryStart()` calls `PauseAllActions()` before your code runs | The operation computes what it needs: run the rules that compute a derived value (`RunRules(nameof(X))`, or `All` when no finished child can be written), or load it from the row. Not a step every operation ends with |
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
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L82-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-computed-gap-rule' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L95-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-without-run-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## RunRules Works While Paused

`RunRules()` has **no IsPaused guard** — neither in `ValidateBase` nor in `RuleManager`. Call it inside a factory method or a `PauseAllActions()` block and it executes all matching rules immediately. So an operation that needs a derived value right when it returns runs the rules that compute it:

<!-- snippet: skill-create-computes-what-it-needs -->
<a id='snippet-skill-create-computes-what-it-needs'></a>
```cs
/// <summary>
/// RIGHT WAY: this operation needs Total to be right when it returns, so
/// it runs the rules that compute it. RunRules has no IsPaused guard.
/// A per-operation choice, not a step every operation ends with.
/// </summary>
[Create]
public async Task CreateWithRunRules()
{
    Quantity = 10;
    Price = 5.00m;
    await RunRules(RunRulesFlag.All);  // no children here, so All is safe
    // Total is now 50.00
}
```
<sup><a href='/src/Design/Design.Domain/CommonGotchas.cs#L109-L123' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-computes-what-it-needs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A `[Fetch]` loads persisted data, which is assumed valid, so it never re-runs rules to validate. It computes only the derived values it is not loading. Two shapes, and `WorkOrder.Fetch` uses both: aggregates over the children (`TotalCost`, `HasUnschedulableTasks`) are persisted and loaded, as the tasks load `Cost` and `OrderItem` loads `LineTotal`; the root's own derived values (`CanApprove`, `IsHeld`, `IsOverBudget`, `Summary`) are recomputed with `RunRules(nameof(Status))` / `RunRules(nameof(Budget))` — the rules that write the root itself, named by trigger. Not `RunRulesFlag.All` from a root whose rules write children: the children finished their own `[Fetch]` and are no longer paused, so a root rule that changed one of their values would mark that child modified and the fetched graph would report `IsModified == true`.

**It is a per-operation choice, not a step.** Ask what derived values this operation must hand back right, and supply those. `RunRules(RunRulesFlag.All)` at the end of every factory method is the mainstream habit ("validate before returning"), and it is wrong twice over: persisted data is assumed valid, and `All` from a root whose rules write children marks any just-fetched child whose value a rule changes as modified. Running rules to *validate* a returned object is the rare case — an operation that must not hand back an unvalidated object (the empty form a `[Create]` returns, see the Blazor reference) — and it is the user's call, made per operation.

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
   → Compute what the operation needs here: RunRules for the rules that write this object, or load the value

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
| `await RunRules(RunRulesFlag.All)` | Re-run all rules (default). Clears messages first. Inside a `[Create]` when the object has no children yet; at the top of a root's `[Insert]`/`[Update]` as the server gate, where any child a rule changes is about to be saved and re-baselined anyway. Not in a root's `[Fetch]` when its rules write children. |
| `await RunRules(RunRulesFlag.NotExecuted)` | Run only rules that haven't executed yet. Does not clear existing messages. |
| `await RunRules(RunRulesFlag.Self)` | Run this object's rules only, skip child property rules. |
| `await RunRules("PropertyName")` | Run rules triggered by a specific property. The form a root's `[Fetch]` uses to recompute its own derived values without writing its children (`RunRules(nameof(Status))`). |

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

## A Fetch That Computes What It Needs

Both shapes in one operation: child aggregates loaded from the row; the root's own derived values recomputed by trigger-named `RunRules`, never `All` from a root whose rules write children.

<!-- snippet: skill-fetch-computes-derived -->
<a id='snippet-skill-fetch-computes-derived'></a>
```cs
[Remote]
[Fetch]
internal async Task<bool> Fetch(Guid id,
    [Service] IWorkOrderRepository repository,
    [Service] IWorkOrderTaskListFactory tasksFactory)
{
    var row = repository.Get(id);
    if (row == null)
    {
        return false;
    }

    Id = row.Id;
    Status = row.Status;
    ApprovedBy = row.ApprovedBy;
    IsOnHold = row.IsOnHold;
    Budget = row.Budget;
    Discount = row.Discount;
    TotalCost = row.TotalCost;
    HasUnschedulableTasks = row.HasUnschedulableTasks;

    Tasks = tasksFactory.Fetch(row.Tasks);

    // Rules do not fire during an operation, so this one computes what it
    // needs. Two shapes, both used here:
    //  - Aggregates over the children (TotalCost, HasUnschedulableTasks) are
    //    persisted and loaded, as the tasks load Cost and IsSchedulable and
    //    OrderItem loads LineTotal.
    //  - The root's own derived values (CanApprove, IsHeld, IsOverBudget,
    //    Summary) are recomputed by running the rules that write THIS
    //    object, named by their trigger. Not RunRulesFlag.All: the tasks
    //    finished their own [Fetch] and are no longer paused, so a root rule
    //    that changed one of their values would mark it modified and the graph
    //    would report IsModified == true.
    // Neither is a validate step; persisted data is assumed valid.
    await RunRules(nameof(Status));
    await RunRules(nameof(Budget));
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L153-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fetch-computes-derived' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Related

- [validation.md](validation.md) — Rule types (AddAction, AddValidation, AddActionAsync, class-based rules)
- [pitfalls.md](pitfalls.md) — Common mistakes including the computed property gap
- [domain-logic-placement.md](domain-logic-placement.md) — Where to use AddAction vs AddValidation
