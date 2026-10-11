# Domain Logic Placement

The domain layer is the home for all business logic. In a Neatoo application that layer has four rungs — entity rules, entity verbs, orchestration seams, and read models — and the ViewModel and Razor above them bind, adapt gestures, and mirror. The ladder, the three ownership questions, the mirror rule, and the gesture test are in the main `/neatoo` skill → "Domain Logic First." This reference is the decision tree that walks the ladder, followed by the wiring patterns for **rung 1, the entity rule** — Patterns 1–9 below all assume the tree has already put you on that rung.

> **Design assumption: open mutation.** Neatoo aggregates are graphs, not façades — any consumer (ViewModel, Razor binding, service) can call business methods on children or bind directly to deep properties. Design rules so that mutating any descendant leaves the root's `IsValid` / `IsModified` correct after all rules settle. See the main `/neatoo` skill → "The Aggregate Is a Graph, Not a Façade" and "Designing Rules for Open Mutation."

The compiled examples below come from one aggregate: a `WorkOrder` root with a list of `WorkOrderTask` children. Each task has `Hours`, `Rate`, a `Discount` the root pushes down, and a derived `Cost`; the root has a `Budget`, a `Status` driven by an `Approve` verb, and derived `TotalCost`, `IsOverBudget`, `CanApprove`, `ShowHoldBanner` and `HasUnschedulableTasks`.

## The Logic Placement Decision Tree

Start from who initiates the behavior — not from what the screen shows. Every branch ends at a rung, and only some of them end at a rule.

```
Who initiates this behavior?
├── The user, with a gesture (click, pick, keystroke)
│   ├── It changes one property                  → ViewModel calls the entity setter; rules react (rung 1)
│   ├── It is an operation on one aggregate      → entity verb (rung 2), CanX by rule; ViewModel calls it
│   └── It spans aggregates or needs services    → orchestration seam: a static [Remote, Execute] (rung 3); ViewModel invokes it
├── A property change on an entity
│   ├── Derived from own properties              → AddAction (rung 1)
│   ├── Reacts to a child's property             → AddAction with child trigger (rung 1)
│   ├── Parent pushes to another child           → AddAction with child trigger, action writes the sibling (rung 1)
│   ├── Cross-sibling consistency in a list      → override HandleNeatooPropertyChanged on the list (rung 1)
│   ├── Validation                               → AddValidation / RuleBase<T> (rung 1)
│   └── Needs server truth (uniqueness, overlap, a lookup)
│       ├── One question, one call               → async rule with an injected [Remote, Execute] command (rung 1)
│       └── Work the user starts on purpose      → NOT a rule. A verb or seam the ViewModel invokes (rung 2 or 3)
├── A load — nothing the user did
│   ├── State the screen shows or gates on       → read model [Fetch] computes it (rung 4)
│   ├── The object always exists                 → the root's [Fetch] by key (rung 3)
│   └── It may have to be created, or a policy   → a static [Remote, Execute] seam that creates or loads and
│       is applied on the user's behalf            returns the graph (rung 3). Not a [Fetch]: Fetch takes a key
│       (normalize, stage forward, seed)            for existing data, and rules are paused inside it. Never the
│                                                  ViewModel's InitializeAsync.
└── Another aggregate, or a command
    └──                                          → orchestration seam: a static [Remote, Execute] (rung 3)

No branch ends at a ViewModel writing an entity outside the gesture branch.
Purely presentational choices — CSS class, layout, which component — are the UI's own and never enter this tree.
```

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

A derived value is a `private set` partial property written by an `AddAction` rule: it notifies, it serializes, and the UI sees it read-only.

<!-- snippet: skill-computed-properties -->
<a id='snippet-skill-computed-properties'></a>
```cs
// A derived value is a private-set partial property written by a rule.
// It notifies, it serializes, and the UI sees it read-only.
RuleManager.AddAction(
    t => t.Cost = t.Hours * t.Rate * (1 - t.Discount),
    t => t.Hours,
    t => t.Rate,
    t => t.Discount);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrderTask.cs#L37-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-computed-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

```razor
<!-- UI is a thin binding layer -->
<MudText>Cost: @task.Cost</MudText>
@if (task.IsSchedulable)
{
    <MudChip>Schedulable</MudChip>
}
```

**Why this matters:**
- `Cost` updates when either input changes, wherever the change came from
- Logic is testable without Blazor
- UI binds to properties, doesn't compute

## Pattern 2: Conditional Visibility via Domain Properties

When the UI shows/hides elements based on business state, expose that decision as a domain property.

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

A show/hide decision is a `bool` the domain computes. When the decision depends on a child's property, the trigger is the child's property path (`t => t.Tasks![0].IsSchedulable`), not the child reference — a trigger on `t => t.Tasks` fires only when the list is reassigned.

<!-- snippet: skill-visibility-rules -->
<a id='snippet-skill-visibility-rules'></a>
```cs
// A show/hide decision is domain state. The UI binds @if (ShowHoldBanner).
RuleManager.AddAction(
    t => t.ShowHoldBanner = t.IsOnHold && t.Status != "Closed",
    t => t.IsOnHold,
    t => t.Status);

// A flag derived from a child property. The trigger is the child's
// property path, so any task's IsSchedulable change recomputes it.
RuleManager.AddAction(
    t => t.HasUnschedulableTasks = t.Tasks?.Any(task => !task.IsSchedulable) ?? false,
    t => t.Tasks![0].IsSchedulable);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L56-L68' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-visibility-rules' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

```razor
<!-- UI just binds -->
@if (order.ShowHoldBanner)
{
    <HoldBanner />
}
@if (order.HasUnschedulableTasks)
{
    <MudAlert Severity="Severity.Warning">Some tasks cannot be scheduled</MudAlert>
}
```

## Pattern 3: Cascading State via Chained Rules

When one property change should trigger a cascade of updates, use chained rules. Setting a property inside a rule triggers the rules that watch that property, so the cascade needs no handler code. Here a task's `Hours` changes its `Cost` (Pattern 1); that bubbles into the root's `TotalCost` (Pattern 8); and that triggers this rule:

<!-- snippet: skill-chained-rule -->
<a id='snippet-skill-chained-rule'></a>
```cs
// Chained: the rule above sets TotalCost, which triggers this one.
// Hours -> Cost (on the task) -> TotalCost -> IsOverBudget, no handler code.
RuleManager.AddAction(
    t => t.IsOverBudget = t.TotalCost > t.Budget,
    t => t.TotalCost,
    t => t.Budget);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L77-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-chained-rule' title='Start of snippet'>anchor</a></sup>
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
            target.IsUsernameAvailable = false;
            return None;  // Don't check empty usernames - None is inherited from AsyncRuleBase
        }

        var available = await _isAvailable(target.Username);

        target.IsUsernameAvailable = available;

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

**What does not belong in a rule** is work the user should start on purpose: a recalculation that takes several server calls, or a result the user must review before it applies. That is an entity verb or a seam the ViewModel invokes on a named gesture (rung 2 or 3).

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

`AddValidation` takes exactly one trigger property, because the message attaches to that property. A validation over two properties is either a `RuleBase<T>` with two triggers that attaches the message where it belongs, or a validation on a computed property an `AddAction` recomputes from both inputs:

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
//   RuleManager.AddAction(t => t.Sum = t.A + t.B, t => t.A, t => t.B);
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
<sup><a href='/src/Design/Design.Domain/Rules/FluentRules.cs#L192-L214' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-property-validation-options' title='Start of snippet'>anchor</a></sup>
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
        RuleManager.AddAction(
            t => t.Sum = t.A + t.B + t.C,
            t => t.A,
            t => t.B,
            t => t.C);

        // Cross-property constraint, validated on the computed Sum: the action
        // above recomputes Sum whenever A, B or C changes, which triggers this.
        RuleManager.AddValidation(
            t => t.Sum > 100 ? "Sum cannot exceed 100" : string.Empty,
            t => t.Sum);

        // Action triggered by computed property
        RuleManager.AddAction(
            t => t.IsOverLimit = t.Sum > 100,
            t => t.Sum);
    }

    [Create]
    public void Create() { }
}
```
<sup><a href='/src/Design/Design.Domain/Rules/FluentRules.cs#L217-L254' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-cross-property-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The UI just binds the inputs. Validation runs when either input commits and shows through `PropertyMessages`.

## Pattern 6: Child Property Triggers — Parent-Child Reactivity via AddAction

When a parent needs to react to child property changes, use `AddAction` with a **child property trigger expression**. This is the same type-safe, expression-based mechanism used for same-object reactivity.

### The Key Syntax: `t => t.ChildCollection![0].ChildProperty`

The `[0]` indexer is a syntactic placeholder — it does NOT mean "only the first item." `TriggerProperty` walks the expression tree, skips the indexer, and produces the property path `"ChildCollection.ChildProperty"`. When any child's property changes, the event bubbles up with this same path and the rule fires.

<!-- snippet: skill-child-property-trigger -->
<a id='snippet-skill-child-property-trigger'></a>
```cs
RuleManager.AddAction(
    t => t.TotalAmount = t.Items?.Sum(i => i.LineTotal) ?? 0,
    t => t.Items![0].LineTotal);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/OrderAggregate/Order.cs#L98-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-property-trigger' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Multiple Child Properties

To react to several child properties, pass several trigger expressions: `t => t.Items![0].LineTotal, t => t.Items![0].Quantity`. The same path syntax works for a single child object, without the indexer: `t => t.Address!.City, t => t.Address!.State`.

### Common Mistake: Using the Collection Reference as the Trigger

`RuleManager.AddAction(..., t => t.Items)` fires only when the `Items` property is reassigned to a new list — never when an item inside it changes a property. `TriggerProperty.IsMatch` uses exact string equality: `"Items" != "Items.LineTotal"`. Trigger on the specific child property, as above.

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
public void Approve(string approver)
{
    ApprovedBy = approver;
    Status = "Approved";
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L124-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-entity-verb' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-can-x-rule -->
<a id='snippet-skill-can-x-rule'></a>
```cs
// Admission for the Approve verb. The UI binds the button to it; the
// verb itself does not check and throw.
RuleManager.AddAction(
    t => t.CanApprove = t.Status == "Pending",
    t => t.Status);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L48-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-can-x-rule' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `[Create]` ends with `RunRules`, so `CanApprove` is right before the user touches anything (rules do not fire during a factory operation):

<!-- snippet: skill-create-with-run-rules -->
<a id='snippet-skill-create-with-run-rules'></a>
```cs
[Create]
public async Task Create([Service] IWorkOrderTaskListFactory tasksFactory)
{
    Tasks = tasksFactory.Create();
    Status = "Pending";

    // The object is paused: the assignment above ran no rule, so CanApprove
    // is still false. Force the rules so the new object is consistent.
    await RunRules(RunRulesFlag.All);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L136-L147' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-create-with-run-rules' title='Start of snippet'>anchor</a></sup>
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

    order.Approve("ada");
    await order.WaitForTasks();

    Assert.AreEqual("Approved", order.Status);
    Assert.AreEqual("ada", order.ApprovedBy);
    Assert.IsFalse(order.CanApprove, "The rule on Status recomputed the admission; nothing threw");
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L49-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-can-x-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

```razor
<!-- UI calls domain method, binds to computed state -->
<MudButton Disabled="@(!order.CanApprove)" OnClick="@(() => order.Approve(currentUser.Name))">
    Approve
</MudButton>
```

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
RuleManager.AddAction(
    t => t.TotalCost = t.Tasks?.Sum(task => task.Cost) ?? 0,
    t => t.Tasks![0].Cost);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L70-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-child-aggregation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

```razor
<!-- UI binds to precomputed domain properties -->
<MudText>Total: @order.TotalCost</MudText>
@if (order.HasUnschedulableTasks)
{
    <MudAlert>Some tasks cannot be scheduled</MudAlert>
}
```

Each aggregation targets the specific child property it depends on (`HasUnschedulableTasks`, in Pattern 2, triggers on `Tasks.IsSchedulable`). The `[0]` indexer is a syntactic placeholder — any child in the collection that changes the named property triggers the rule.

## Pattern 9: Parent-as-Orchestrator — Cross-Child Coordination

When one child changes and a different child needs updating, the parent orchestrates via `AddAction`. The action body receives the parent (`t`), which can reach any child. This is the same child property trigger mechanism — the only difference is the action pushes changes DOWN to the children instead of computing a parent property.

### Anti-Pattern: UI Bridges Between Entities

```razor
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
RuleManager.AddAction(
    t =>
    {
        foreach (var task in t.Tasks ?? Enumerable.Empty<IWorkOrderTask>())
        {
            task.ApplyParentDiscount();
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
<sup><a href='/src/Design/Design.Domain/Aggregates/WorkOrderAggregate/WorkOrder.cs#L86-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-orchestrator' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-parent-in-child-method -->
<a id='snippet-skill-parent-in-child-method'></a>
```cs
/// <summary>
/// Child business method reading root state through Parent. Called by the
/// root's orchestrator rule whenever the discount changes.
/// </summary>
public void ApplyParentDiscount()
{
    Discount = ((IWorkOrder)this.Parent!).Discount;
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
| `@(a.X * b.Y)` arithmetic | Entity rule (rung 1) |
| `@if (a.Status == "X" && b.Count > 0)` | Entity `CanX` by rule (1) or a read-model flag (4); bind to it |
| `@(list.Where(...).Count())` LINQ | Entity rule with child trigger (1) |
| `@(condition ? "Label A" : "Label B")` ternary | Domain `string` property (1) |
| `OnClick` handler that sets several properties | Entity verb (2); the handler calls it |
| `OnChanged` handler that validates | `AddValidation` (1) |
| `@code` block with more than 5 lines of logic | A verb (2) or a seam (3); at most a ViewModel gesture handler that calls one |
| Handler touching two entities | Orchestration seam (3), or a parent-orchestrator rule (1) if both are children of one root |

**Rule of thumb:** more than three conditional or computed expressions in a `.razor` file, and logic has leaked.

### In the ViewModel

| Smell | What it is | Move to |
|---|---|---|
| `bool X => A && B` over entity or read-model values | A re-deriving mirror — the rule now has two owners | Entity `CanX` rule (1) or read-model flag (4); the ViewModel reads it |
| `X => Id != 0 && Id == OtherId` | A domain fact assembled from ids | Read model (4) |
| A method that writes an entity and handles no named gesture | A policy in the wrong layer | The seam that loads the graph (3), or a rule (1) |
| A method that reads a registry or service and writes entity defaults | A verb in the wrong layer | Entity verb (2), or the seam (3) |
| A guard in the ViewModel that re-derives an entity's admission check | The rule now has two owners | Entity `CanX` rule (1); the ViewModel binds to it |
| `InitializeAsync` that does more than fetch, bind, and subscribe | Load-time policy | A static `[Remote, Execute]` seam that creates or loads (3); a `[Fetch]` only when the object always exists |

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
            target.IsUsernameAvailable = false;
            return None;  // Don't check empty usernames - None is inherited from AsyncRuleBase
        }

        var available = await _isAvailable(target.Username);

        target.IsUsernameAvailable = available;

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
    Assert.IsFalse(order.ShowHoldBanner);

    order.IsOnHold = true;
    await order.WaitForTasks();

    Assert.IsFalse(design.IsSchedulable, "The root re-ran the tasks' rules; the child rule read Parent.IsOnHold");
    Assert.IsTrue(order.HasUnschedulableTasks, "...and the child's change bubbled into the root's flag");
    Assert.IsTrue(order.ShowHoldBanner);
}
```
<sup><a href='/src/Design/Design.Tests/AggregateTests/WorkOrderAggregateTests.cs#L108-L128' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-parent-read-test' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the logic lived in a `.razor` file, this test would be impossible.
