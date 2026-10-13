# Known Pains

Framework defects and missing guards that are open today, each with what you see, where it is tracked, and what to do until it closes. A pain is the framework's fault and goes away when its issue closes; a mistake that stays a mistake is in [pitfalls.md](pitfalls.md).

## Open Defects

| You see | Tracked | Until it closes |
|---|---|---|
| A double-click on Save sends two `[Insert]`s: the sent instance stays `IsSavable` until the handler reassigns the result | [Neatoo#103](https://github.com/NeatooDotNet/Neatoo/issues/103) (critical) | Hold a saving flag in the save handler and disable the button while it is set; `IsSavable` alone cannot do it yet |
| After a cancelled `RunRules`, the object stays invalid and unsavable; `RunRules(RunRulesFlag.All)` does not clear the `MarkInvalid` the cancellation set | [Neatoo#96](https://github.com/NeatooDotNet/Neatoo/issues/96) | There is no in-place recovery: fetch the object again (or create it again) |
| `RunRules(RunRulesFlag.Self)` returns while a child's async rule is still running, so the root still reports busy | [Neatoo#89](https://github.com/NeatooDotNet/Neatoo/issues/89) (open question) | `await WaitForTasks()` after it when the caller reads `IsBusy` or `IsSavable` |
| Reordering a child list is invisible to change tracking: `Move` marks nothing modified, so `IsModified` and `IsSavable` stay false after a reorder | [Neatoo#109](https://github.com/NeatooDotNet/Neatoo/issues/109) | The aggregate's move verb calls the list's `Move`, then `MarkModified()` on itself. Not `Remove` then `Insert`: the list is one child short in between, and a rule on its count fires |
| A MudNeatoo field will not compile against an input model's property (`EntityProperty` is `IEntityProperty`; a `ValidateBase` indexer returns `IValidateProperty`) | [Neatoo#95](https://github.com/NeatooDotNet/Neatoo/issues/95) | Bind the property object by hand — [blazor.md, Mode 2](blazor.md#mode-2-property-metadata-binding) |
| `MudNeatooTextField` cannot show a value in a format other than its stored type (no `Converter`) | [Neatoo#93](https://github.com/NeatooDotNet/Neatoo/issues/93) | Bind that field by hand, the same way, with the parse and format in tested domain code |
| Every remote Save fails once `AddNeatooAspNetCore` is registered after `AddNeatooServices(NeatooFactory.Server, ...)` | [RemoteFactory#117](https://github.com/NeatooDotNet/RemoteFactory/issues/117) | Register `AddNeatooServices(NeatooFactory.Server, ...)` and map the endpoint with `UseNeatoo()`; do not call `AddNeatooAspNetCore` — [blazor.md, Neatoo on Each Tier](blazor.md#neatoo-on-each-tier) |

## Missing Analyzers

Each of these compiles and fails at run time. An analyzer for each is planned; until then the guard is review.

| The mistake | What happens | Instead |
|---|---|---|
| The result of `Save()` is discarded | The caller keeps the instance it sent; on the client that is a stale copy | `order = (IOrder)await order.Save();` — the save handler in [blazor.md](blazor.md#the-save-handler) |
| A rule triggers on a collection reference (`t => t.Items`) | It fires only when the list is reassigned, never when an item's property changes | Trigger on the child property path (`t => t.Items![0].LineTotal`) — [domain-logic-placement.md, Pattern 6](domain-logic-placement.md#pattern-6-child-property-triggers--parent-child-reactivity-via-addaction) |
| `LoadValue` inside a factory operation | Nothing it does is needed: the operation is already paused, and plain assignment is the baseline | Assign the property — [properties.md](properties.md#assignment-inside-and-outside-a-factory-operation) |

`LazyLoad<T>` / `EntityLazyLoad` are being removed from RemoteFactory and Neatoo rather than fixed; build nothing new on them ([lazy-loading.md](lazy-loading.md) is kept until the removal ships).
