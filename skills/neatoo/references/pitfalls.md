# Best Practices and Gotchas

This document captures important patterns and behaviors when working with Neatoo.

## Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| Checking `IsValid` without `await RunRules()` | Validation rules are async—`IsValid` may be stale | Always `await entity.RunRules()` before checking `IsValid` |
| Calling `Save()` on child entities | Child entities don't save independently—they're saved with their aggregate root | Call `Save()` only on the aggregate root |
| Missing `partial` keyword on class | Source generator won't generate factory methods | Add `partial` to class declaration |
| Missing `partial` keyword on properties | Source generator won't implement change tracking | Add `partial` to property declarations |
| Non-partial properties on domain models | **Data silently lost during client-server JSON serialization round-trip.** Properties appear to work locally but values are zero/null after Neatoo's serialization (e.g., after `Save()` or factory `Fetch()`). This is especially insidious because there's no error — values just disappear. | Make ALL domain model properties that need to survive serialization `partial`. Also note: `partial` properties cannot have default initializers — set defaults in `Create()` or `MapFrom()` instead. |
| Constructor injection for server-only services | Service unavailable on client causes DI failure | Use method injection (`[Service]` on method parameter) for server-only services |
| Adding `[Remote]` to child entity factory methods | Unnecessary—child methods are called from server | Only use `[Remote]` on aggregate root entry points |
| Expecting new items in DeletedList after remove | New items (IsNew=true) are removed entirely—nothing to delete from DB | Only existing items go to DeletedList |
| Moving entities between aggregates directly | Throws `InvalidOperationException`—item.Root must match list.Root | Remove → Save → Re-fetch/create → Add to new aggregate |
| Creating `LazyLoad<T>` in `[Fetch]` instead of constructor | Loader delegate lost during serialization — client gets `LazyLoad` with no loader, `await` throws `InvalidOperationException` | Create `LazyLoad<T>` in the **constructor** with a lambda that captures DI factory and references `this.Id` (resolved at load-time). See [lazy-loading.md](lazy-loading.md) |
| Using `OnDeserialized`/`ReinitializeLazyLoaders` to recreate `LazyLoad` | Unnecessary complexity — the converter now merges deserialized state into constructor-created instances, preserving the loader | Remove `OnDeserialized`/`InitializeLazyLoaders` workarounds; move `LazyLoad` creation to the constructor |
| Parent maps child properties to EF entities inline | Child has no `[Insert]`/`[Update]`/`[Delete]`; persistence logic is untestable and tangled into parent | Parent calls `childFactory.SaveAsync(child)` — child handles its own EF mapping in its own factory methods |
| Forgetting to iterate DeletedList in parent's `[Update]` | Removed children are never deleted from the database | After saving active children, iterate `ChildList.DeletedList` and call `childFactory.SaveAsync(deleted)` for each |
| Forgetting items are modified when added to collections | Adding a fetched (non-new) item marks both item and list as `IsModified` | Expected behavior—adding to a new parent is a state change |
| Kitchen-sink rule with early return | Only one form input shows an error at a time — user plays whack-a-mole | Use `new RuleMessages().If(...).If(...)` to return all errors, or use separate per-property rules |
| Worrying about rules firing during Fetch/Create | **Not a real risk.** `LoadValue` uses `ChangeReason.Load` (rules skip it). Factory operations (`[Create]`, `[Fetch]`) are wrapped in `PauseAllActions()` by the framework. Rules do not fire during hydration — no defensive coding needed. | No action required — this is handled by the framework |

---

## Anti-Pattern: Kitchen-Sink Validation Rule

A single class-based rule that validates multiple properties but early-returns after the first failure. Only one form input shows an error at a time — the user fixes it, submits, and a *different* error appears. This is a poor UX.

```csharp
// ANTI-PATTERN: Early return means only one error shows at a time
public class EmployeeEditRule : RuleBase<Employee>
{
    public EmployeeEditRule()
        : base(e => e.FirstName, e => e.LastName, e => e.Email) { }

    protected override IRuleMessages Execute(Employee target)
    {
        if (string.IsNullOrEmpty(target.FirstName))
            return (nameof(target.FirstName), "First name is required").AsRuleMessages();

        if (string.IsNullOrEmpty(target.LastName))
            return (nameof(target.LastName), "Last name is required").AsRuleMessages();

        if (string.IsNullOrEmpty(target.Email))
            return (nameof(target.Email), "Email is required").AsRuleMessages();

        return None;
    }
}
```

**Fix:** Return all errors at once using the `RuleMessages.If()` fluent builder:

```csharp
// CORRECT: All errors returned — all invalid fields show errors simultaneously
public class EmployeeEditRule : RuleBase<Employee>
{
    public EmployeeEditRule()
        : base(e => e.FirstName, e => e.LastName, e => e.Email) { }

    protected override IRuleMessages Execute(Employee target)
    {
        return new RuleMessages()
            .If(string.IsNullOrEmpty(target.FirstName),
                nameof(target.FirstName), "First name is required")
            .If(string.IsNullOrEmpty(target.LastName),
                nameof(target.LastName), "Last name is required")
            .If(string.IsNullOrEmpty(target.Email),
                nameof(target.Email), "Email is required");
    }
}
```

**Better yet** — if the validations are independent per-property checks, use separate rules or validation attributes instead of one combined rule. A class-based rule that spans multiple properties is appropriate for *cross-property* validation (e.g., "end date must be after start date"). For independent per-property checks, prefer `AddValidation` or `[Required]` attributes.

---

## Quick Reference

| Pattern | How To |
|---------|--------|
| Commands | Static class with `[Factory]` and `[Execute]` — see [base-classes.md](base-classes.md) |
| Read-only models | `ValidateBase` with only `[Fetch]` methods — see [base-classes.md](base-classes.md) |
| Authorization | Single `[AuthorizeFactory<IInterface>]` attribute — see `/RemoteFactory` skill |
| Property change events | `(e) => { return Task.CompletedTask; }` — see [properties.md](properties.md) |
| Check validity | `await RunRules()` first — see [validation.md](validation.md) |
| Testing | Use real factories, mock external deps — see [testing.md](testing.md) |
| Remove new item | Gone entirely (not in DeletedList) |
| Remove existing item | Goes to DeletedList, `IsDeleted = true` |
| Re-add removed item | Removed from DeletedList, `UnDelete()` called |
| Cross-aggregate transfer | Remove → Save → Re-fetch → Add to new aggregate |
