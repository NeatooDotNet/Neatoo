# Domain Logic Placement

The main `/neatoo` skill places logic by asking first whether a gesture is a fact ("A fact, a consequence, an intent"): a fact is a property, what follows from it is a rule, intent the state cannot hold is a verb, a load or another aggregate is a seam, and a screen's need for server truth is a read model. This reference is the mechanism for rules and verbs: which rule shape for which trigger, the patterns that make an aggregate behave as one, the verb, and a worked rules-first aggregate. The seam's and the read model's attributes are the RemoteFactory skill's.

> **Design assumption: open mutation.** Neatoo aggregates are graphs, not façades — any consumer (ViewModel, Razor binding, service) can call business methods on children or bind directly to deep properties. Design rules so that mutating any descendant leaves the root's `IsValid` / `IsModified` correct after all rules settle. The ViewModel adapts gestures into those calls and decides nothing (the main `/neatoo` skill's fourth model statement); the rules below are what make that safe.

The compiled examples below come from one aggregate: a `WorkOrder` root with a list of `WorkOrderTask` children. Each task has `Hours`, `Rate`, a `Discount` the root pushes down, and a derived `Cost`; the root has a `Budget`, a `Status` driven by an `Approve` verb, and derived `TotalCost`, `IsOverBudget`, `CanApprove`, `IsHeld` and `HasUnschedulableTasks`.

## Which Rule Shape

A property change drives a rule; what the rule reacts to decides its shape.

| The rule reacts to | Shape | Pattern |
|---|---|---|
| The entity's own properties, producing a value | `AddActionAsync` writing a `private set` partial property | 1 |
| The entity's own properties, producing a fact the UI shows or gates on | `AddActionAsync` writing a `bool` named for the business fact (`IsHeld`, `CanApprove`) | 2, 7 |
| A value another rule wrote | A second `AddActionAsync` triggered by that value — rules chain | 3 |
| A question only the server can answer, once per change | An async rule that takes a `[Remote, Execute]` command delegate or an interface-factory domain service | 4 |
| Several of the entity's properties together | `AddValidation` or `RuleBase<T>`, returning every message at once | 5 |
| A child's property | `AddActionAsync` on the parent with a child-property trigger (`t => t.Items![0].LineTotal`) | 6, 8 |
| A parent's property every child must follow | `AddActionAsync` on the parent that calls a business method on each child | 9 |
| A sibling's property in the same list | A rule on the child that reads its siblings through `Parent`; the list re-runs the siblings' rules (`HandleNeatooPropertyChanged`) | 6 |

Not a rule: work a person starts on purpose — several server calls, or a result they review before it applies. That is a verb or a seam the ViewModel calls on a named gesture. Presentational choices (CSS class, layout, which component) are the UI's own.

## Pattern 1: Computed/Derived Properties via AddAction

When a property's value depends on other properties, compute it in the domain model.

### Anti-Pattern: Computing in the UI

```razor
<!-- WRONG: Business logic in Blazor -->
<MudText>Cost: @(task.Hours * task.Rate * (1 - task.Discount))</MudText>
@if (task.Hours > 0 && !order.IsOnHold)
{
    <MudChip>Schedulable</MudChip>
}
```

### Correct: Domain Model Computes, UI Binds

A derived value is a `private set` partial property written by an `AddActionAsync` rule that awaits its generated `Set{Name}`: it notifies, it serializes, and the UI sees it read-only.

<!-- snippet: skill-computed-properties -->
<a id='snippet-skill-computed-properties'></a>
```cs
// A derived value is a private-set partial property written by a rule.
// It notifies, it serializes, and the UI sees it read-only.
RuleManager.AddActionAsync(
    async t => await t.SetCost(t.Hours * t.Rate * (1 - t.Discount)),
    t => t.Hours,
    t => t.Rate,
    t => t.Discount);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L37-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-computed-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The page renders `@task.Cost` as it is, with no arithmetic of its own: [blazor.md — A ViewModel Over a Live Aggregate](blazor.md#a-viewmodel-over-a-live-aggregate).

**Why this matters:**
- `Cost` updates when either input changes, wherever the change came from
- Logic is testable without Blazor
- UI binds to properties, doesn't compute

## Pattern 2: Visibility Binds to Business Facts

When the UI shows or hides elements based on business state, the entity exposes the business fact (`IsHeld`, `HasUnschedulableTasks`) and the UI binds to it. The entity never names a screen element: `ShowHoldBanner` on an entity is the smell, `IsHeld` is the fact. If a screen needs a name of its own for the element, that mapping lives in a ViewModel, one-to-one, deciding nothing.

### Anti-Pattern: UI Decides Visibility

```razor
<!-- WRONG: 27 conditional expressions in a dashboard -->
@if (visit.Status == "Active" && visit.TreatmentPlan != null
    && visit.TreatmentPlan.IsApproved && !visit.IsComplete)
{
    <TreatmentPanel />
}
@if (visit.AssessmentAreas.Any(a => a.NeedsReview)
    && visit.Status != "Discharged")
{
    <ReviewAlert />
}
```

### Correct: Domain Exposes State, UI Binds

The fact is a `bool` the domain computes. When it depends on a child's property, the trigger is the child's property path (`t => t.Tasks![0].IsSchedulable`), not the child reference — a trigger on `t => t.Tasks` fires only when the list is reassigned.

<!-- snippet: skill-derived-facts -->
<a id='snippet-skill-derived-facts'></a>
```cs
// A business fact derived from two others. The UI binds to it (for
// example @if (workOrder.IsHeld) around a banner) and decides nothing;
// the entity never names a screen element (no "ShowHoldBanner").
RuleManager.AddActionAsync(
    async t => await t.SetIsHeld(t.IsOnHold && t.Status != "Closed"),
    t => t.IsOnHold,
    t => t.Status);

// A flag derived from a child property. The trigger is the child's
// property path, so any task's IsSchedulable change recomputes it.
RuleManager.AddActionAsync(
    async t => await t.SetHasUnschedulableTasks(t.Tasks?.Any(task => !task.IsSchedulable) ?? false),
    t => t.Tasks![0].IsSchedulable);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L56-L70' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-derived-facts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The page binds `HasUnschedulableTasks` directly and `IsHeld` through the ViewModel's one-to-one name for it; it has no condition of its own: [blazor.md — A ViewModel Over a Live Aggregate](blazor.md#a-viewmodel-over-a-live-aggregate).

## Pattern 3: Cascading State via Chained Rules

When one property change should trigger a cascade of updates, use chained rules. Setting a property inside a rule triggers the rules that watch that property, so the cascade needs no handler code. Here a task's `Hours` changes its `Cost` (Pattern 1); that bubbles into the root's `TotalCost` (Pattern 8); and that triggers this rule:

<!-- snippet: skill-chained-rule -->
<a id='snippet-skill-chained-rule'></a>
```cs
// Chained: the rule above sets TotalCost, which triggers this one.
// Hours -> Cost (on the task) -> TotalCost -> IsOverBudget, no handler code.
RuleManager.AddActionAsync(
    async t => await t.SetIsOverBudget(t.TotalCost > t.Budget),
    t => t.TotalCost,
    t => t.Budget);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L79-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-chained-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-aggregation-test -->
<a id='snippet-skill-aggregation-test'></a>
```cs
[TestMethod]
public async Task TaskCosts_RollUpToTheRoot_AndChainIntoIsOverBudget()
{
    var (order, design, build) = await CreateWithTwoTasks();
    order.Budget = 100m;

    design.Hours = 2m;
    design.Rate = 30m;   // Cost 60
    build.Hours = 1m;
    build.Rate = 50m;    // Cost 50
    await order.WaitForTasks();

    Assert.AreEqual(60m, design.Cost, "The task's own rule computed Cost");
    Assert.AreEqual(110m, order.TotalCost, "The root's child-trigger rule summed the tasks");
    Assert.IsTrue(order.IsOverBudget, "Setting TotalCost triggered the chained rule");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L66-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-aggregation-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A step that needs the server — a protocol looked up from a diagnosis code — is an `AddActionAsync` that calls an injected `[Remote, Execute]` command (Pattern 4) and sets the next property in the chain; the rest of the cascade is unchanged.

## Pattern 4: Async Rules That Need the Server

An async rule may call the server. This is what `AddValidationAsync`, `AddActionAsync` and `AsyncRuleBase<T>` are for: a uniqueness check, an overlap check, a duplicate lookup. Because the rule is on the entity, the user sees the answer on the field while editing instead of after Save.

**Hard rule: a rule takes a command, never a server-only service.** Rule code and entity constructors run in the browser. A rule or an entity constructor that takes an Entity Framework service, a repository, or any other server-only type breaks the client; this has broken release builds. Inject a `[Remote, Execute]` command instead. The command encapsulates the server call: in the browser the delegate crosses to the server, and on the server the same delegate calls the method directly.

The command. `[Remote]` makes the client cross to the server (RemoteFactory 1.9+: without it, an `[Execute]` runs on the calling tier). The repository stays server-side:

<!-- snippet: skill-rule-command -->
<a id='snippet-skill-rule-command'></a>
```cs
/// <summary>
/// Command the rule calls. [Remote]: a client call crosses to the server.
/// </summary>
[Factory]
public static partial class UsernameAvailability
{
    [Remote]
    [Execute]
    private static Task<bool> _IsAvailable(string username, [Service] IUsernameRepository repository)
    {
        return Task.FromResult(!repository.UsernameExists(username));
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L126-L140' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The rule takes the command delegate. It never sees the repository:

<!-- snippet: skill-rule-with-command -->
<a id='snippet-skill-rule-with-command'></a>
```cs
/// <summary>
/// Rule interface: the entity takes the rule from DI by this interface, and
/// tests can substitute it.
/// </summary>
internal interface ICheckUsernameAvailabilityRule : IRule<AsyncRulesDemo> { }

/// <summary>
/// Demonstrates: async uniqueness validation through a [Remote, Execute] command.
/// </summary>
internal class CheckUsernameAvailabilityRule : AsyncRuleBase<AsyncRulesDemo>, ICheckUsernameAvailabilityRule
{
    private readonly UsernameAvailability.IsAvailable _isAvailable;

    // Trigger properties are passed to the base constructor
    public CheckUsernameAvailabilityRule(UsernameAvailability.IsAvailable isAvailable) : base(t => t.Username)
    {
        _isAvailable = isAvailable;
    }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Username))
        {
            await target.SetIsUsernameAvailable(false);
            return None;  // Don't check empty usernames - None is inherited from AsyncRuleBase
        }

        var available = await _isAvailable(target.Username);

        await target.SetIsUsernameAvailable(available);

        if (!available)
        {
            // Create error message: (propertyName, message).AsRuleMessages()
            return (nameof(AsyncRulesDemo.Username), $"Username '{target.Username}' is already taken").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L142-L183' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-with-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The entity receives the rule by constructor injection; the rule's interface is registered in DI on both tiers (see `validation.md` → "Async Rules That Call the Server"):

<!-- snippet: skill-rule-injected -->
<a id='snippet-skill-rule-injected'></a>
```cs
// A rule with a dependency comes from DI through its interface. The
// dependency must exist on both tiers - here, a command delegate.
public AsyncRulesDemo(
    IEntityBaseServices<AsyncRulesDemo> services,
    ICheckUsernameAvailabilityRule usernameAvailabilityRule) : base(services)
{
    // Register async rules
    RuleManager.AddRule(new ValidateEmailFormatRule());
    RuleManager.AddRule(usernameAvailabilityRule);
    RuleManager.AddRule(new FetchExternalDataRule());
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L55-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-injected' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Trigger on field commit, not per keystroke.** A rule runs every time its trigger property is set. `MudNeatooTextField` and `MudNeatooNumericField` set the property when the field loses focus, so a rule behind them makes one call per committed value. A control that sets the property continuously (a slider with `Immediate`, a raw `@bind:event="oninput"`) makes one call per change.

**What does not belong in a rule** is work the user should start on purpose: a recalculation that takes several server calls, or a result the user must review before it applies. That is an entity verb or a seam the ViewModel invokes on a named gesture.

Two shapes to refuse:

```csharp
// WRONG: a server-only service inside the rule. It breaks on the client.
RuleManager.AddValidationAsync(
    async t => await contactRepository.EmailExistsAsync(t.Id, t.Email) ? "This email is already in use." : "",
    t => t.Email);

// WRONG: the same check as a guard in a factory method. The user learns of it only after Save, as an exception.
if (await repository.EmailExistsAsync(Id, Email))
    throw new InvalidOperationException("This email is already in use.");
```

## Pattern 5: Cross-Property Validation

Business rules that span multiple properties belong in the domain model, not in UI event handlers.

### Anti-Pattern: UI Validates Cross-Property Rules

```razor
<!-- WRONG: the UI validates; the rule has no home in the domain -->
@code {
    void OnEndDateChanged(DateTime? value)
    {
        endDate = value;
        if (endDate < startDate)
            errorMessage = "End date must be after start date";
        else if ((endDate - startDate)?.Days > 365)
            errorMessage = "Date range cannot exceed one year";
        else
            errorMessage = null;
    }
}
```

### Correct: Domain Validates, UI Displays

`AddValidation` takes exactly one trigger property, because the message attaches to that property. A validation over two properties is either a `RuleBase<T>` with two triggers that attaches the message where it belongs, or a validation on a computed property an `AddActionAsync` recomputes from both inputs:

<!-- snippet: skill-cross-property-validation-options -->
<a id='snippet-skill-cross-property-validation-options'></a>
```cs
// COMMON MISTAKE: Using the wrong property expression.
//
// WRONG:
//   RuleManager.AddValidation(
//       t => t.A + t.B > 100 ? "Too high" : "",
//       t => t.A);  // Only triggers on A, not B!
//
// AddValidation takes exactly one trigger. For a validation over several
// properties, either:
//
// RIGHT (validate a computed property - the shape used below):
//   RuleManager.AddActionAsync(async t => await t.SetSum(t.A + t.B), t => t.A, t => t.B);
//   RuleManager.AddValidation(t => t.Sum > 100 ? "Too high" : "", t => t.Sum);
//
// RIGHT (a rule class, which takes any number of triggers):
//   internal class SumLimitRule : RuleBase<T>
//   {
//       public SumLimitRule() : base(t => t.A, t => t.B) { }
//       protected override IRuleMessages Execute(T t)
//           => RuleMessages.If(t.A + t.B > 100, nameof(t.A), "Too high");
//   }
```
<sup><a href='/src/Design/Design.Domain/Rules/FluentRules.cs#L195-L217' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-property-validation-options' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-cross-property-validation -->
<a id='snippet-skill-cross-property-validation'></a>
```cs
/// <summary>
/// Demonstrates: Different trigger property patterns.
/// </summary>
[Factory]
internal partial class TriggerPatternsDemo : ValidateBase<TriggerPatternsDemo>, ITriggerPatternsDemo
{
    public partial int A { get; set; }
    public partial int B { get; set; }
    public partial int C { get; set; }
    public partial int Sum { get; set; }
    public partial bool IsOverLimit { get; set; }

    public TriggerPatternsDemo(IValidateBaseServices<TriggerPatternsDemo> services) : base(services)
    {
        // Rule that depends on multiple properties
        RuleManager.AddActionAsync(
            async t => await t.SetSum(t.A + t.B + t.C),
            t => t.A,
            t => t.B,
            t => t.C);

        // Cross-property constraint, validated on the computed Sum: the action
        // above recomputes Sum whenever A, B or C changes, which triggers this.
        RuleManager.AddValidation(
            t => t.Sum > 100 ? "Sum cannot exceed 100" : string.Empty,
            t => t.Sum);

        // Action triggered by computed property
        RuleManager.AddActionAsync(
            async t => await t.SetIsOverLimit(t.Sum > 100),
            t => t.Sum);
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/FluentRules.cs#L220-L257' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-property-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The UI just binds the inputs. Validation runs when either input commits and shows through `PropertyMessages`.

## Pattern 6: Child Property Triggers — Parent-Child Reactivity via AddAction

When a parent needs to react to child property changes, use `AddActionAsync` with a **child property trigger expression**. This is the same type-safe, expression-based mechanism used for same-object reactivity.

### The Key Syntax: `t => t.ChildCollection![0].ChildProperty`

The `[0]` indexer is a syntactic placeholder — it does NOT mean "only the first item." `TriggerProperty` walks the expression tree, skips the indexer, and produces the property path `"ChildCollection.ChildProperty"`. When any child's property changes, the event bubbles up with this same path and the rule fires.

<!-- snippet: skill-child-property-trigger -->
<a id='snippet-skill-child-property-trigger'></a>
```cs
RuleManager.AddActionAsync(
    async t => await t.SetTotalAmount(t.Items?.Sum(i => i.LineTotal) ?? 0),
    t => t.Items![0].LineTotal);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L98-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-property-trigger' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Multiple Child Properties

To react to several child properties, pass several trigger expressions: `t => t.Items![0].LineTotal, t => t.Items![0].Quantity`. The same path syntax works for a single child object, without the indexer: `t => t.Address!.City, t => t.Address!.State`.

### Common Mistake: Using the Collection Reference as the Trigger

`RuleManager.AddActionAsync(..., t => t.Items)` fires only when the `Items` property is reassigned to a new list — never when an item inside it changes a property. `TriggerProperty.IsMatch` uses exact string equality: `"Items" != "Items.LineTotal"`. Trigger on the specific child property, as above.

### When to Use NeatooPropertyChanged Instead

Child property triggers handle most parent-child reactivity. Reserve `NeatooPropertyChanged` — the async event that carries `Source` and a dotted `FullPropertyName` for every change anywhere in the graph — for the cases that need:
- **Event args inspection** — `Source`, `FullPropertyName`
- **React to ANY child change** regardless of which property
- **Cross-sibling rules** — a list override that triggers rules on sibling items when one changes

The cross-sibling case has two halves. The rule itself is on the child, so the message lands on the task that is wrong; it reaches its siblings through `Parent`:

<!-- snippet: skill-sibling-validation -->
<a id='snippet-skill-sibling-validation'></a>
```cs
// Sibling consistency: the message lands on this task. When a Sequence
// changes, the list re-runs the siblings' rules (see WorkOrderTaskList).
RuleManager.AddValidation(
    t => t.Parent is IWorkOrder root
         && root.Tasks!.Any(other => !ReferenceEquals(other, t) && other.Sequence == t.Sequence)
        ? "Sequence must be unique within the work order"
        : string.Empty,
    t => t.Sequence);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L55-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-sibling-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Re-evaluation lives on the **list**, not the entity: an entity cannot override `HandleNeatooPropertyChanged`, and only the list sees every sibling. When one task's `Sequence` changes, the other tasks' uniqueness rule has to be re-run:

<!-- snippet: skill-cross-sibling-rules -->
<a id='snippet-skill-cross-sibling-rules'></a>
```cs
// Cross-sibling consistency lives on the LIST: an entity cannot override
// HandleNeatooPropertyChanged, and only the list sees every sibling. When a
// task's Sequence changes, re-run the siblings' rules so their uniqueness
// messages update too.
protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    await base.HandleNeatooPropertyChanged(eventArgs);

    if (eventArgs.PropertyName == nameof(IWorkOrderTask.Sequence)
        && eventArgs.Source is IWorkOrderTask changed)
    {
        await Task.WhenAll(this.Except([changed])
            .Select(sibling => sibling.RunRules(nameof(IWorkOrderTask.Sequence))));
    }
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTaskList.cs#L26-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-sibling-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-cross-sibling-test -->
<a id='snippet-skill-cross-sibling-test'></a>
```cs
[TestMethod]
public async Task DuplicateSequence_InvalidatesBothSiblings_UntilOneChanges()
{
    var (order, design, build) = await CreateWithTwoTasks();   // sequences 1 and 2
    await order.WaitForTasks();
    Assert.IsTrue(order.IsValid);

    build.Sequence = 1;
    await order.WaitForTasks();

    Assert.IsFalse(build.IsValid, "The task that changed sees the duplicate");
    Assert.IsFalse(design.IsValid, "The list re-ran the sibling's rules, so it sees it too");
    Assert.IsFalse(order.IsValid);

    build.Sequence = 3;
    await order.WaitForTasks();

    Assert.IsTrue(design.IsValid);
    Assert.IsTrue(build.IsValid);
    Assert.IsTrue(order.IsValid);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L130-L152' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-sibling-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Pattern 7: Status/Workflow State Machines

Workflow transitions and status logic belong in the domain model.

### Anti-Pattern: UI Manages Workflow

```razor
<!-- WRONG: the UI owns the transition and its guard -->
@code {
    void OnApprove()
    {
        if (plan.Status == "Pending" && plan.IsValid && currentUser.CanApprove)
        {
            plan.Status = "Approved";
            plan.ApprovedBy = currentUser.Name;
            plan.ApprovedDate = DateTime.Now;
        }
    }
}
```

### Correct: Domain Owns Workflow

The verb sets state; it does not check and throw. Admission is the `CanApprove` rule, which the UI binds to disable the button. A transition the domain disallows is a validation message, never an exception: the user sees why, `IsValid` goes false, and the save is blocked while the user is still editing.

<!-- snippet: skill-entity-verb -->
<a id='snippet-skill-entity-verb'></a>
```cs
/// <summary>
/// The verb sets state. Admission is the CanApprove rule, which the UI binds;
/// the verb does not check and throw.
/// </summary>
public async Task Approve(string approver)
{
    await SetApprovedBy(approver);
    await SetStatus("Approved");
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L126-L136' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-verb' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-can-x-rule -->
<a id='snippet-skill-can-x-rule'></a>
```cs
// Admission for the Approve verb. The UI binds the button to it; the
// verb itself does not check and throw.
RuleManager.AddActionAsync(
    async t => await t.SetCanApprove(t.Status == "Pending"),
    t => t.Status);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L48-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-can-x-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `[Create]` needs `CanApprove` and `Summary` right before the user touches anything, and rules do not fire during an operation, so it runs them itself (`All` is safe: a new work order has no children yet):

<!-- snippet: skill-create-computes-derived -->
<a id='snippet-skill-create-computes-derived'></a>
```cs
[Create]
public async Task Create([Service] IWorkOrderTaskListFactory tasksFactory)
{
    Tasks = tasksFactory.Create();
    Status = "Pending";

    // The object is paused: the assignment above ran no rule, so CanApprove
    // and Summary are still default. This operation needs them, so it runs
    // the rules that compute them. All is safe here because the task list
    // is empty - no finished child exists for a root rule to write.
    await RunRules(RunRulesFlag.All);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L138-L151' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-computes-derived' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-can-x-test -->
<a id='snippet-skill-can-x-test'></a>
```cs
[TestMethod]
public async Task Approve_SetsState_AndTheCanApproveRuleRecomputesAdmission()
{
    var order = await _factory.Create();
    Assert.AreEqual("Pending", order.Status);
    Assert.IsTrue(order.CanApprove, "Create ran the rules, so the admission flag is already right");

    await order.Approve("ada");
    await order.WaitForTasks();

    Assert.AreEqual("Approved", order.Status);
    Assert.AreEqual("ada", order.ApprovedBy);
    Assert.IsFalse(order.CanApprove, "The rule on Status recomputed the admission; nothing threw");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L49-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-can-x-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The page enables the Approve button from `CanApprove` and calls the verb; it never re-checks the status: [blazor.md — A ViewModel Over a Live Aggregate](blazor.md#a-viewmodel-over-a-live-aggregate).

## Pattern 8: Child Collection Aggregation

When computing aggregate values from child collections (sums, counts, any/all), expose these as domain properties using child property triggers. This is one of the most common places where LINQ ends up in `.razor` files.

### Anti-Pattern: LINQ in Razor

```razor
<!-- WRONG: Aggregation logic in UI -->
<MudText>Total: @order.Tasks.Sum(t => t.Cost)</MudText>
@if (order.Tasks.Any(t => !t.IsSchedulable))
{
    <MudAlert>Some tasks cannot be scheduled</MudAlert>
}
```

### Correct: Domain Aggregates, UI Binds

<!-- snippet: skill-child-aggregation -->
<a id='snippet-skill-child-aggregation'></a>
```cs
// Aggregation over the children, recomputed when any task's Cost changes
RuleManager.AddActionAsync(
    async t => await t.SetTotalCost(t.Tasks?.Sum(task => task.Cost) ?? 0),
    t => t.Tasks![0].Cost);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L72-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-aggregation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The page renders `@Vm.WorkOrder.TotalCost` as it is: [blazor.md — A ViewModel Over a Live Aggregate](blazor.md#a-viewmodel-over-a-live-aggregate).

Each aggregation targets the specific child property it depends on (`HasUnschedulableTasks`, in Pattern 2, triggers on `Tasks.IsSchedulable`). The `[0]` indexer is a syntactic placeholder — any child in the collection that changes the named property triggers the rule.

## Pattern 9: Parent-as-Orchestrator — Cross-Child Coordination

When one child changes and a different child needs updating, the parent orchestrates via `AddActionAsync`. The action body receives the parent (`t`), which can reach any child. This is the same child property trigger mechanism — the only difference is the action pushes changes DOWN to the children instead of computing a parent property.

### Anti-Pattern: UI Bridges Between Entities

```razor
<!-- WRONG: the UI orchestrates between children -->
@code {
    void OnDiscountChanged(decimal discount)
    {
        // WRONG: UI orchestrates between domain children
        foreach (var task in order.Tasks)
            task.ApplyDiscount(discount);
    }
}
```

### Correct: Parent Orchestrates in the Domain

Parents write children through the children's business methods — here `ApplyParentDiscount`, which reads the root through `Parent`. The second rule is the other half of the contract: a child rule that *reads* root state has no trigger for it, so the root re-runs the children's rules when that state changes.

<!-- snippet: skill-parent-orchestrator -->
<a id='snippet-skill-parent-orchestrator'></a>
```cs
// Parent writes children: when the discount changes, every task
// re-applies it through its own business method.
RuleManager.AddActionAsync(
    async t =>
    {
        foreach (var task in t.Tasks ?? Enumerable.Empty<IWorkOrderTask>())
        {
            await task.ApplyParentDiscount();
        }
    },
    t => t.Discount);

// A child rule that READS root state (IsSchedulable reads IsOnHold) has
// no trigger for it. The root re-runs the children's rules when it changes.
RuleManager.AddActionAsync(
    async t =>
    {
        foreach (var task in t.Tasks ?? Enumerable.Empty<IWorkOrderTask>())
        {
            await task.RunRules();
        }
    },
    t => t.IsOnHold);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L88-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-orchestrator' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-parent-in-child-method -->
<a id='snippet-skill-parent-in-child-method'></a>
```cs
/// <summary>
/// Child business method reading root state through Parent. Called by the
/// root's orchestrator rule whenever the discount changes.
/// </summary>
public async Task ApplyParentDiscount()
{
    await SetDiscount(((IWorkOrder)this.Parent!).Discount);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L74-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-method' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-orchestrator-test -->
<a id='snippet-skill-orchestrator-test'></a>
```cs
[TestMethod]
public async Task Discount_IsPushedToEveryTask_ByTheRootsRule()
{
    var (order, design, build) = await CreateWithTwoTasks();
    order.Budget = 100m;
    design.Hours = 2m;
    design.Rate = 30m;
    build.Hours = 1m;
    build.Rate = 50m;
    await order.WaitForTasks();
    Assert.IsTrue(order.IsOverBudget);

    order.Discount = 0.5m;
    await order.WaitForTasks();

    Assert.AreEqual(0.5m, design.Discount, "The root's rule called ApplyParentDiscount on each task");
    Assert.AreEqual(30m, design.Cost, "...which re-ran the task's Cost rule");
    Assert.AreEqual(55m, order.TotalCost, "...which bubbled back up into the root's aggregation");
    Assert.IsFalse(order.IsOverBudget);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L85-L106' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-orchestrator-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The UI binds `MudNeatooNumericField` to `order[nameof(IWorkOrder.Discount)]` and each task's `Cost` — no bridging code.

### Orchestrator Variants

| Variant | Trigger | Action |
|---------|---------|--------|
| Root property → every child | `t => t.Discount` | Calls a business method on each child in `t.Tasks!` |
| Single child → single child | `t => t.ChildA!.Prop` | Sets `t.ChildB!.Prop` |
| Single child → collection | `t => t.ChildA!.Prop` | Iterates `t.Items!` and calls a method on each |
| Collection → single child | `t => t.Items![0].Prop` | Sets `t.ChildB!.Prop` |
| Collection → parent + child | `t => t.Items![0].Prop` | Sets parent prop AND `t.ChildB!.Prop` |

The parent entity does not need to be the aggregate root — any entity with children can orchestrate between them.

## The Refactoring Smell Test

Two layers, two tests. The Razor test catches leaks into markup. The ViewModel test catches the leak the Razor test cannot see — logic that never reached the markup because the ViewModel absorbed it first. A codebase with thin Razor and a fat ViewModel passes the first test and fails the second.

### In `.razor`

| Smell | Move to |
|---|---|
| `@(a.X * b.Y)` arithmetic | Entity rule |
| `@if (a.Status == "X" && b.Count > 0)` | Entity `CanX` by rule or a read-model flag; bind to it |
| `@(list.Where(...).Count())` LINQ | Entity rule with child trigger |
| `@(condition ? "Label A" : "Label B")` ternary | Domain `string` property |
| `OnClick` handler that sets several properties | Entity verb; the handler calls it |
| `OnChanged` handler that validates | `AddValidation` |
| `@code` block with more than 5 lines of logic | A verb or a seam; at most a ViewModel gesture handler that calls one |
| Handler touching two entities | Orchestration seam, or a parent-orchestrator rule if both are children of one root |

**Rule of thumb:** more than three conditional or computed expressions in a `.razor` file, and logic has leaked.

### In the ViewModel

| Smell | What it is | Move to |
|---|---|---|
| `bool X => A && B` over entity or read-model values | A re-deriving mirror — the rule now has two owners | Entity `CanX` rule or read-model flag; the ViewModel reads it |
| `X => Id != 0 && Id == OtherId` | A domain fact assembled from ids | Read model |
| A method that writes an entity and handles no named gesture | A policy in the wrong layer | The seam that loads the graph, or a rule |
| A method that reads a registry or service and writes entity defaults | A verb in the wrong layer | Entity verb, or the seam |
| A guard in the ViewModel that re-derives an entity's admission check | The rule now has two owners | Entity `CanX` rule; the ViewModel binds to it |
| `InitializeAsync` that does more than fetch, bind, and subscribe | Load-time policy | A static `[Remote, Execute]` seam that creates or loads; a `[Fetch]` only when the object always exists |

**Rule of thumb:** every ViewModel member that writes an entity must name the gesture it handles; every ViewModel bool must be a read, not a composition.

## Class-Based Rules for Complex Logic

When logic exceeds 5 lines or needs a dependency, use `AsyncRuleBase<T>` (or `RuleBase<T>` when synchronous) instead of an inline lambda. The dependency is a `[Remote, Execute]` command delegate, never a server-only service; the rule has a DI interface so the entity can take it from DI and tests can substitute it:

<!-- snippet: skill-rule-with-command -->
<a id='snippet-skill-rule-with-command'></a>
```cs
/// <summary>
/// Rule interface: the entity takes the rule from DI by this interface, and
/// tests can substitute it.
/// </summary>
internal interface ICheckUsernameAvailabilityRule : IRule<AsyncRulesDemo> { }

/// <summary>
/// Demonstrates: async uniqueness validation through a [Remote, Execute] command.
/// </summary>
internal class CheckUsernameAvailabilityRule : AsyncRuleBase<AsyncRulesDemo>, ICheckUsernameAvailabilityRule
{
    private readonly UsernameAvailability.IsAvailable _isAvailable;

    // Trigger properties are passed to the base constructor
    public CheckUsernameAvailabilityRule(UsernameAvailability.IsAvailable isAvailable) : base(t => t.Username)
    {
        _isAvailable = isAvailable;
    }

    protected override async Task<IRuleMessages> Execute(AsyncRulesDemo target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Username))
        {
            await target.SetIsUsernameAvailable(false);
            return None;  // Don't check empty usernames - None is inherited from AsyncRuleBase
        }

        var available = await _isAvailable(target.Username);

        await target.SetIsUsernameAvailable(available);

        if (!available)
        {
            // Create error message: (propertyName, message).AsRuleMessages()
            return (nameof(AsyncRulesDemo.Username), $"Username '{target.Username}' is already taken").AsRuleMessages();
        }

        return None;
    }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/AsyncRules.cs#L142-L183' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-rule-with-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The entity receives the rule by constructor injection and registers it with `RuleManager.AddRule(rule)` (Pattern 4 shows the constructor).

## Testing Advantage

The payoff: all business logic is testable without a UI. This test exercises a child rule reading the root, the root re-running the children's rules, and two derived root flags, with no Blazor anywhere:

<!-- snippet: skill-parent-read-test -->
<a id='snippet-skill-parent-read-test'></a>
```cs
[TestMethod]
public async Task ChildRule_ReadsRootStateThroughParent_AndTheRootRerunsItWhenThatStateChanges()
{
    var (order, design, build) = await CreateWithTwoTasks();
    design.Hours = 2m;
    build.Hours = 1m;
    await order.WaitForTasks();

    Assert.IsTrue(design.IsSchedulable, "Hours > 0 and the parent is not on hold");
    Assert.IsFalse(order.HasUnschedulableTasks);
    Assert.IsFalse(order.IsHeld);

    order.IsOnHold = true;
    await order.WaitForTasks();

    Assert.IsFalse(design.IsSchedulable, "The root re-ran the tasks' rules; the child rule read Parent.IsOnHold");
    Assert.IsTrue(order.HasUnschedulableTasks, "...and the child's change bubbled into the root's flag");
    Assert.IsTrue(order.IsHeld);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L108-L128' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-read-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the logic lived in a `.razor` file, this test would be impossible.

## A Verb

A verb is one meaning of writing a fact, kept only for intent the state cannot hold. It is `async Task` and awaits each write, so the rules its writes trigger have run when it returns. It never persists: no `RunRules`, no guard, no throw.

<!-- snippet: skill-verb-submit -->
<a id='snippet-skill-verb-submit'></a>
```cs
public async Task Submit()
{
    await SetStatus("Submitted");
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L309-L314' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-verb-submit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Facts First: The TrainingPlan

A rules-first aggregate at the size of a real editing screen. A `TrainingPlan` root holds the approved minutes and a list of `Interval` children; each interval has an effort slider, a minutes value and a lock. Nearly everything a person does is a fact they write; the fit, the planned total, the next marks and the admission are consequences; four verbs carry the intent the state cannot hold.

The interfaces name each member for what it is:

<!-- snippet: skill-training-plan-interface -->
<a id='snippet-skill-training-plan-interface'></a>
```cs
/// <summary>
/// Training plan aggregate root.
/// </summary>
public partial interface ITrainingPlan : IEntityRoot
{
    /// <summary>Fact: the minutes the session is approved for. The fit shares them out.</summary>
    int ApprovedMinutes { get; set; }

    /// <summary>Consequence: the sum of the intervals' minutes.</summary>
    int PlannedMinutes { get; }

    string Status { get; }
    string? ApprovedBy { get; }

    /// <summary>Consequence: admission for Approve, maintained by a rule that reads IsValid.</summary>
    bool CanApprove { get; }

    IIntervalList? Intervals { get; }

    /// <summary>Intent: a new interval and its effort land together.</summary>
    Task<IInterval> AddInterval(string name, int effort);

    /// <summary>Intent: a stamp and a status, in that order.</summary>
    Task Approve(string approver);
}

/// <summary>
/// One interval of the plan.
/// </summary>
public partial interface IInterval : IEntityBase
{
    string Name { get; set; }

    /// <summary>Fact: the slider. Writing it re-weights the fit.</summary>
    int Effort { get; set; }

    /// <summary>Fact: a locked interval keeps its minutes; the fit shares the rest.</summary>
    bool MinutesLocked { get; set; }

    /// <summary>Consequence of the fit, or the value a gesture below set.</summary>
    int Minutes { get; }

    /// <summary>Consequences the UI writes into Effort to nudge it: a nudge is a plain write.</summary>
    int NextEffortUp { get; }
    int NextEffortDown { get; }

    /// <summary>Intent: set the minutes and fit the others around this interval, for this one fit.</summary>
    Task SlideMinutes(int minutes);

    /// <summary>Intent: lock, then set the minutes. The order is the meaning.</summary>
    Task LockMinutes(int minutes);
}

public interface IIntervalList : IEntityListBase<IInterval> { }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/TrainingPlanAggregate/ITrainingPlanInterfaces.cs#L26-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-training-plan-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**The slider is a fact.** `Effort` has a public setter and no verb; the binding writes it, and every rule that follows runs whoever wrote it. A nudge button is the same write: the domain publishes the next mark as a fact, and the button writes it into `Effort`.

<!-- snippet: skill-next-mark-facts -->
<a id='snippet-skill-next-mark-facts'></a>
```cs
// Consequences the UI writes back: a nudge is a plain write of Effort.
RuleManager.AddActionAsync(
    async t =>
    {
        await t.SetNextEffortUp(Math.Min(t.Effort + 1, 10));
        await t.SetNextEffortDown(Math.Max(t.Effort - 1, 1));
    },
    t => t.Effort);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/TrainingPlanAggregate/Interval.cs#L41-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-next-mark-facts' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-test-slider-is-a-fact -->
<a id='snippet-skill-test-slider-is-a-fact'></a>
```cs
[TestMethod]
public async Task EffortSlider_IsAFact_TheFitFollowsWhoeverWroteIt()
{
    var (plan, warmUp, main, coolDown) = await ThreeIntervals();

    await main.SetEffort(8);   // what a slider binding does: a plain, awaited write

    CollectionAssert.AreEqual(new[] { 10, 40, 10 }, Minutes(warmUp, main, coolDown));
    Assert.AreEqual(60, plan.PlannedMinutes);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/TrainingPlanTests.cs#L56-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-slider-is-a-fact' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**The fit reads the anchor.** The fit cannot know which interval moved, and must not. `SlideMinutes` sets the anchor for one cascade; every nested fit sees it, and the outermost scope clears it. Scopes nest and end on a throw.

<!-- snippet: skill-fit-reads-the-anchor -->
<a id='snippet-skill-fit-reads-the-anchor'></a>
```cs
/// <summary>
/// The fit: locked intervals and the anchored one keep their minutes; the rest share what is
/// left of the approved minutes in proportion to effort.
/// </summary>
private async Task Fit()
{
    if (Intervals == null || Intervals.Count == 0)
    {
        return;
    }

    var held = Intervals.Where(i => i.MinutesLocked || ReferenceEquals(i, _anchor)).ToList();
    var free = Intervals.Except(held).ToList();
    if (free.Count == 0)
    {
        return;
    }

    var remaining = Math.Max(ApprovedMinutes - held.Sum(i => i.Minutes), 0);
    var shares = Share(remaining, free.Select(i => Math.Max(i.Effort, 1)).ToList());

    for (var index = 0; index < free.Count; index++)
    {
        // Parent writes child through the child's own method. Each write re-enters
        // this rule; the nested fit computes the same shares and writes nothing new.
        await ((IIntervalFit)free[index]).ApplyFit(shares[index]);
    }
}

/// <summary>
/// The anchor for one gesture. Scopes nest: the previous anchor comes back when this one ends.
/// </summary>
IDisposable ITrainingPlanFit.AnchorFit(IInterval interval)
{
    var previous = _anchor;
    _anchor = interval;
    return new AnchorScope(() => _anchor = previous);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/TrainingPlanAggregate/TrainingPlan.cs#L91-L130' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-fit-reads-the-anchor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-test-anchor-evaporates -->
<a id='snippet-skill-test-anchor-evaporates'></a>
```cs
[TestMethod]
public async Task SlideMinutes_FitsTheOthersAroundIt_AndTheAnchorEvaporates()
{
    var (plan, warmUp, main, coolDown) = await ThreeIntervals();

    await warmUp.SlideMinutes(20);
    CollectionAssert.AreEqual(new[] { 20, 30, 10 }, Minutes(warmUp, main, coolDown),
        "the slid interval held for its fit; the others shared the remaining 40 by effort");

    await coolDown.SetEffort(6);
    CollectionAssert.AreEqual(new[] { 9, 26, 25 }, Minutes(warmUp, main, coolDown),
        "the next fact refit everything: the anchor did not outlive its gesture");
    Assert.AreEqual(60, plan.PlannedMinutes);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/TrainingPlanTests.cs#L69-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-anchor-evaporates' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Two intents on one fact are two verbs on one property.** `Minutes` is written with two meanings: `SlideMinutes` means "this many, and fit the others around me, this once"; `LockMinutes` means "this many, and keep them". The lock is an ordered write, and unlocking is a plain write of `MinutesLocked`:

<!-- snippet: skill-test-ordered-write -->
<a id='snippet-skill-test-ordered-write'></a>
```cs
[TestMethod]
public async Task LockMinutes_LocksThenSets_AndTheFitSharesTheRest()
{
    var (plan, warmUp, main, coolDown) = await ThreeIntervals();

    await main.LockMinutes(30);
    CollectionAssert.AreEqual(new[] { 15, 30, 15 }, Minutes(warmUp, main, coolDown));

    await plan.SetApprovedMinutes(90);
    CollectionAssert.AreEqual(new[] { 30, 30, 30 }, Minutes(warmUp, main, coolDown),
        "the locked interval kept its minutes; the free ones shared the new total");

    await main.SetMinutesLocked(false);   // unlocking is a plain write of a fact
    CollectionAssert.AreEqual(new[] { 18, 54, 18 }, Minutes(warmUp, main, coolDown));
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/TrainingPlanTests.cs#L86-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-ordered-write' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Adding an interval is a `Count` change.** The interval is built with its name and effort, so the add is the one change the fit hears:

<!-- snippet: skill-intent-add-interval -->
<a id='snippet-skill-intent-add-interval'></a>
```cs
/// <summary>
/// Intent: a new interval, built with its effort, joins the plan. The interval's Create makes
/// name and effort its baseline, so the add is the one change: the list's Count, which the fit
/// triggers on. One fit runs, with the right effort.
/// </summary>
public async Task<IInterval> AddInterval(string name, int effort)
{
    var interval = await _intervalFactory.Create(name, effort);
    Intervals!.Add(interval);
    await WaitForTasks();
    return interval;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/TrainingPlanAggregate/TrainingPlan.cs#L148-L161' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-intent-add-interval' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Admission follows validity.** `CanApprove` triggers on `IsValid` (the rule is in the main skill), so it follows every fact that changes validity, and the verb never checks:

<!-- snippet: skill-test-admission-on-meta-state -->
<a id='snippet-skill-test-admission-on-meta-state'></a>
```cs
[TestMethod]
public async Task CanApprove_FollowsValidity()
{
    var (plan, _, main, _) = await ThreeIntervals();
    Assert.IsTrue(plan.CanApprove);

    await main.LockMinutes(80);   // more than the 60 approved

    Assert.IsFalse(plan.IsValid);
    Assert.IsFalse(plan.CanApprove, "validity went false, so the admission fact followed");

    await plan.SetApprovedMinutes(100);

    Assert.IsTrue(plan.IsValid);
    Assert.IsTrue(plan.CanApprove);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/TrainingPlanTests.cs#L230-L247' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-test-admission-on-meta-state' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-intent-approve -->
<a id='snippet-skill-intent-approve'></a>
```cs
/// <summary>
/// Intent: who approved, then the status. Each write is awaited, so the status's
/// consequences (CanApprove) have landed when the verb returns.
/// </summary>
public async Task Approve(string approver)
{
    await SetApprovedBy(approver);
    await SetStatus("Approved");
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/TrainingPlanAggregate/TrainingPlan.cs#L163-L173' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-intent-approve' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## The Aggregate Is a Graph

Every node of an aggregate is directly addressable. A ViewModel calls a child's verb (`workOrder.Tasks![0].ApplyParentDiscount()`), a page binds a deep property (`task[nameof(IWorkOrderTask.Hours)]`), a service reads any depth. The root coordinates — it owns `Save()`, and its `IsValid`, `IsModified` and `IsSavable` aggregate every descendant — but it does not mediate access to its children. Encapsulation lives at the property: `private set`, verbs and rules guard state wherever it sits.

Routing every child mutation through a root wrapper rebuilds a gate the framework does not have, and duplicates the logic between root and child.

## Rules for Open Mutation

Because any consumer can call a child's verb, the rule graph must converge after that call: the child sets its state, its own rules run, its validity changes and bubbles, the root's rules triggered on that child property run, and the root's `IsValid` / `IsSavable` raise `PropertyChanged` for the bindings. Every mutation a consumer can make must leave the root correct once the rules have settled. If a child edit can leave the root invalid but unreported, a rule is missing — usually on the root, triggered by the child property that changed. That is the rule author's responsibility, not a framework guarantee.

| The invariant is about | The rule lives on | Its trigger |
|---|---|---|
| The child's own state | The child | Its own property |
| Root state that depends on a child | The root | A child-property trigger (Pattern 6) |
| A summary of the children exposed on the root | The root (`AddActionAsync`) | A child-property trigger (Pattern 8) |
| Consistency between siblings | The child, reading siblings through `Parent` | Its own property; the list re-runs the siblings |

## Parent

Every child has a `Parent`, set when it is assigned to a parent's partial property or added to a child list. It is the child's read access to the aggregate around it.

**In a child rule**, the child may not be attached yet, so pattern-match rather than cast:

<!-- snippet: skill-parent-in-child-rule -->
<a id='snippet-skill-parent-in-child-rule'></a>
```cs
// A child rule reads ambient root state through Parent. Parent is null
// until the task is attached, so pattern-match instead of casting.
RuleManager.AddActionAsync(
    async t => await t.SetIsSchedulable(t.Hours > 0 && t.Parent is IWorkOrder root && !root.IsOnHold),
    t => t.Hours);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L47-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**In a child factory method**, the child is always attached. Here the task writes a denormalized column from the root's state:

<!-- snippet: skill-parent-in-child-update -->
<a id='snippet-skill-parent-in-child-update'></a>
```cs
[Update]
internal void Update(WorkOrderTaskRow row)
{
    MapTo(row);
}

private void MapTo(WorkOrderTaskRow row)
{
    row.Id = Id;
    row.Name = Name!;
    row.Sequence = Sequence;
    row.Hours = Hours;
    row.Rate = Rate;
    row.Discount = Discount;
    row.Cost = Cost;
    row.IsSchedulable = IsSchedulable;

    // A denormalized column decided from root state, read through Parent.
    // On a persistence path the task is always attached.
    row.WorkOrderStatus = ((IWorkOrder)this.Parent!).Status!;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L107-L129' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-child-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The saved row carries the root's status, read through `Parent` at save time:

<!-- snippet: skill-parent-in-update-test -->
<a id='snippet-skill-parent-in-update-test'></a>
```cs
[TestMethod]
public async Task Save_TaskMapsRootStateIntoItsRow_ThroughParent()
{
    var row = _repository.SeedWorkOrder(MockWorkOrderRepository.Task("Design", 1, 2m, 30m));

    var order = (await _factory.Fetch(row.Id))!;
    Assert.IsFalse(order.IsModified, "A fetched aggregate is a clean baseline");
    Assert.IsTrue(order.CanApprove, "Fetch re-ran the root's own rules for its derived values");

    order.Tasks![0].Hours = 3m;   // a modified child: the list hands it its row
    await order.Approve("ada");
    await order.WaitForTasks();
    Assert.IsTrue(order.IsSavable);

    order = (IWorkOrder)await order.Save();

    var saved = _repository.Store[row.Id];
    Assert.AreEqual("Approved", saved.Status);
    Assert.AreEqual("ada", saved.ApprovedBy);
    Assert.AreEqual(3m, saved.Tasks.Single().Hours);
    Assert.AreEqual("Approved", saved.Tasks.Single().WorkOrderStatus,
        "The task read the root's status through Parent when it mapped itself");
    Assert.IsFalse(order.IsModified);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L154-L179' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-in-update-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**In a child verb**, the cast is safe for the same reason; `ApplyParentDiscount` under [Pattern 9](#correct-parent-orchestrates-in-the-domain) is the example.

Rules of use:

- `Parent` is typed `IValidateBase?`. Cast to the root's interface (`(IWorkOrder)Parent!`) where the child is always attached; pattern-match (`Parent is IWorkOrder root`) where it may not be, or where the same child type can sit under different roots. Inside a factory operation the parent may still be paused, so a child's write to it there is baseline and runs none of its rules.
- A child may write its parent's properties or call a parent method; `SlideMinutes` above calls its root to anchor the fit. What a child does not do is write a sibling through the parent (`Parent.Items[1].Hours = ...`); reading siblings through `Parent` is fine. Agreement between siblings is the parent's job, so the child writes the parent or calls it, and the parent, or a rule on it, writes the siblings.
- `Parent` stays inside the aggregate. A child that needs another aggregate through `Parent` has its boundary drawn wrong.
- A child rule that reads `Parent` has no trigger for the parent's property. When that state changes, the parent re-runs the children's rules — the orchestrator rule in Pattern 9.
