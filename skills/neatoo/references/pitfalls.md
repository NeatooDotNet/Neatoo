# Best Practices and Gotchas

This document captures important patterns and behaviors when working with Neatoo.

## Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| Reading `IsValid` while async rules are in flight | `IsValid` reflects the rules that have finished | `await entity.WaitForTasks()` before reading `IsValid` or saving when async rules may be running. `RunRules()` is for forcing a re-run (after a `[Create]` that set values while paused), not the routine step before a read |
| Calling `Save()` on child entities | Children are persisted by their aggregate root | The child interface extends `IEntityBase`, which has no `Save()` or `IsSavable`; only the root interface (`IEntityRoot`) has them |
| Public concrete entity class | Consumers bind to the concrete, and `IsSavable`/`Save()` leak onto children | Public interface (`IOrder : IEntityRoot`, `IOrderItem : IEntityBase`), `internal` concrete, every reference through the interface |
| Missing `partial` keyword on class | Source generators cannot add the factory or the property implementations | Add `partial` to the class declaration |
| Missing `partial` keyword on properties | No change tracking, no rules, no serialization | Add `partial` to property declarations |
| Non-partial properties on domain models | **Data silently lost during client-server JSON serialization round-trip.** Properties appear to work locally but values are zero/null after Neatoo's serialization (e.g., after `Save()` or factory `Fetch()`). There is no error — values just disappear. | Make ALL domain model properties that need to survive serialization `partial`. `partial` properties cannot have default initializers — set defaults in `[Create]` |
| Constructor injection of a server-only service | The entity is constructed on the client too; DI fails there | `[Service]` on the factory method parameter for server-only services. A rule that needs the server takes a `[Remote, Execute]` command delegate |
| `[Remote]` on a child entity's or list's factory methods | Child and list operations are `internal`, reached only from the parent's factory method | Never. `[Remote]` goes on the root's `internal` entry points only. (The one exception is the `[Fetch]` an `EntityLazyLoad` loader calls from the client) |
| `[Fetch]` whose parameters are the object's data | A `[Fetch]` loads an object that exists | `[Fetch]` takes a key and loads through a `[Service]` repository; `[Create]` is `new` |
| Setting a Guid key in `[Create]` | The key is persistence state | Set `Id = Guid.NewGuid()` in `[Insert]` |
| Root maps child properties to rows inline | Child persistence is untestable and tangled into the root | The root makes or gets its row, hands the row's child collection to the list factory's `Save`, and flushes once. The list makes/finds/removes rows and hands each child its row. The child maps itself |
| Forgetting removed children | Removed children are never deleted from the database | The list's `[Update]` walks its `DeletedList` and removes those rows; the root never touches child rows |
| `LoadValue`, `PauseAllActions` or `MarkUnmodified` inside a factory method | The operation is already paused; a `using (PauseAllActions())` resumes the object early | Plain assignment. Inside a factory operation it is a clean baseline load |
| Expecting new items in `DeletedList` after remove | New items (`IsNew == true`) are removed entirely — there is nothing to delete | Only fetched items go to `DeletedList` |
| Moving a child between aggregates | Throws `InvalidOperationException` — a child belongs to one aggregate | Remove it from the source and create a new child in the target |
| Creating `EntityLazyLoad<T>` in `[Fetch]` or `[Create]` | The loader delegate is not serialized; the other tier's copy has no loader and `LoadAsync()` throws | Create `EntityLazyLoad<T>` in the **constructor** with a lambda that captures the DI factory and reads `this.Id` at load time. See [lazy-loading.md](lazy-loading.md) |
| Using `OnDeserialized`/`ReinitializeLazyLoaders` to recreate `EntityLazyLoad` | Unnecessary — the converter merges deserialized state into the constructor-created instance | Move `EntityLazyLoad` creation to the constructor |
| Treating "added to a collection" as not modified | Adding a child to a live parent marks the child modified so the parent becomes modified and savable | Expected behavior |
| Kitchen-sink rule with early return | Only one form input shows an error at a time | `new RuleMessages().If(...).If(...)` returns every error, or use separate per-property rules |
| Expecting `AddAction` computed properties to populate during `[Create]`/`[Fetch]` | **Rules do NOT fire during factory methods.** The operation runs paused; `ResumeAllActions()` does NOT run rules and `PropertyChanged` does NOT fire for changes made while paused | `await RunRules(RunRulesFlag.All)` at the end of the factory method. See [rules-lifecycle.md](rules-lifecycle.md) |
| Validating in a factory method by throwing | The user learns of it after Save, as an exception | Validation is a rule on the entity. The root's `[Insert]`/`[Update]` re-run the rules and throw `SaveOperationException(SaveFailureReason.IsInvalid)` only as the server gate |
| Logic in the ViewModel or Razor | Untestable; the rule has two owners | Rules on the entity; the UI binds. See [domain-logic-placement.md](domain-logic-placement.md) |

---

## Anti-Pattern: Kitchen-Sink Validation Rule

A single class-based rule that validates multiple properties but early-returns after the first failure. Only one form input shows an error at a time — the user fixes it, submits, and a *different* error appears.

```csharp
// WRONG: each early return hides the checks below it
protected override IRuleMessages Execute(Employee target)
{
    if (string.IsNullOrEmpty(target.FirstName))
        return (nameof(target.FirstName), "First name is required").AsRuleMessages();
    if (string.IsNullOrEmpty(target.LastName))
        return (nameof(target.LastName), "Last name is required").AsRuleMessages();
    return None;
}
```

**Fix:** Return all errors at once using the `RuleMessages.If()` fluent builder, so every invalid field shows its error at the same time:

<!-- snippet: skill-multi-message-rule -->
<a id='snippet-skill-multi-message-rule'></a>
```cs
/// <summary>
/// Demonstrates: Rule returning multiple messages.
/// </summary>
internal class MultiMessageRule : RuleBase<RuleBasicsDemo>
{
    public MultiMessageRule() : base(t => t.Quantity, t => t.Price) { }

    protected override IRuleMessages Execute(RuleBasicsDemo target)
    {
        // Use fluent API to build multiple conditional messages
        return new RuleMessages()
            .If(target.Quantity < 0, nameof(RuleBasicsDemo.Quantity), "Quantity cannot be negative")
            .If(target.Price < 0, nameof(RuleBasicsDemo.Price), "Price cannot be negative")
            .If(target.Quantity > 1000, nameof(RuleBasicsDemo.Quantity), "Quantity exceeds maximum order limit");
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/RuleBasics.cs#L193-L210' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-multi-message-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Better yet** — if the validations are independent per-property checks, use separate rules or validation attributes instead of one combined rule. A class-based rule that spans multiple properties is for *cross-property* validation ("end date must be after start date"). For independent per-property checks, prefer `AddValidation` or `[Required]`.

---

## Quick Reference

| Pattern | How To |
|---------|--------|
| Commands | Static `[Factory]` class; `[Remote, Execute] private static Task<T> _Name(...)` when it needs the server, bare `[Execute]` runs on the calling tier — see [base-classes.md](base-classes.md) |
| Read models | Plain `[Factory]` class with `[Fetch]` only, no Neatoo base — see [base-classes.md](base-classes.md) |
| Authorization | Single `[AuthorizeFactory<IInterface>]` attribute — see `/RemoteFactory` skill |
| Property change events | `PropertyChanged` for own properties and meta flags; `NeatooPropertyChanged` (async, returns `Task`) for descendant paths — see [properties.md](properties.md) |
| Check validity | `await WaitForTasks()` first when async rules may be in flight — see [validation.md](validation.md) |
| Testing | Use real factories, mock external deps — see [testing.md](testing.md) |
| Add a child | `itemFactory.Create(...)` then `list.Add(item)`; a list method when creation takes no parameters |
| Remove new item | Gone entirely (not in `DeletedList`) |
| Remove existing item | Goes to `DeletedList`, `IsDeleted = true` |
| Re-add removed item | Removed from `DeletedList`, `UnDelete()` called |
| Cross-aggregate transfer | Remove from the source; create a new child in the target |
| Save cascade | Root: row, then list factory `Save(list, row.Children)`, then one flush. List: rows for children, `DeletedList` removals. Child: maps itself to its row |
