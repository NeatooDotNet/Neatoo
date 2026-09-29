# Domain Logic Placement

The domain layer is the home for all business logic. In a Neatoo application that layer has four rungs — entity rules, entity verbs, orchestration seams, and read models — and the ViewModel and Razor above them bind, adapt gestures, and mirror. The ladder, the three ownership questions, the mirror rule, and the gesture test are in the main `/neatoo` skill → "Domain Logic First." This reference is the decision tree that walks the ladder, followed by the wiring patterns for **rung 1, the entity rule** — Patterns 1–9 below all assume the tree has already put you on that rung.

> **Design assumption: open mutation.** Neatoo aggregates are graphs, not façades — any consumer (ViewModel, Razor binding, service) can call business methods on children or bind directly to deep properties. Design rules so that mutating any descendant leaves the root's `IsValid` / `IsModified` correct after all rules settle. See the main `/neatoo` skill → "The Aggregate Is a Graph, Not a Façade" and "Designing Rules for Open Mutation."

## The Logic Placement Decision Tree

Start from who initiates the behavior — not from what the screen shows. Every branch ends at a rung, and only some of them end at a rule.

```
Who initiates this behavior?
├── The user, with a gesture (click, pick, keystroke)
│   ├── It changes one property                  → ViewModel calls the entity setter; rules react (rung 1)
│   ├── It is an operation on one aggregate      → entity verb (rung 2), CanX by rule; ViewModel calls it
│   └── It spans aggregates or needs services    → orchestration seam (rung 3); ViewModel invokes it
├── A property change on an entity
│   ├── Derived from own properties              → AddAction (rung 1)
│   ├── Reacts to a child's property             → AddAction with child trigger (rung 1)
│   ├── Parent pushes to another child           → AddAction with child trigger, action writes the sibling (rung 1)
│   ├── Cross-sibling consistency in a list      → override HandleNeatooPropertyChanged (rung 1)
│   ├── Validation                               → AddValidation / RuleBase<T> (rung 1)
│   └── Needs a service
│       ├── ...and the work stays in the browser → class-based AsyncRuleBase<T> with DI (rung 1)
│       └── ...and it would round-trip           → NOT a rule. A seam the ViewModel invokes (rung 3)
├── A load or fetch — nothing the user did
│   ├── State the screen shows or gates on       → read model [Fetch] computes it (rung 4)
│   └── A policy applied on the user's behalf    → the seam that loads the graph applies it to the
│       (normalize, stage forward, seed)           returned in-memory graph, staged, with an explanation
│                                                  the UI can show (rung 3). Rules are paused during
│                                                  [Fetch] — that is why this cannot be a rule, and it
│                                                  is never the ViewModel's InitializeAsync.
└── Another aggregate, or a command
    └──                                          → orchestration seam (rung 3)

No branch ends at a ViewModel writing an entity outside the gesture branch.
Purely presentational choices — CSS class, layout, which component — are the UI's own and never enter this tree.
```

## Pattern 1: Computed/Derived Properties via AddAction

When a property's value depends on other properties, compute it in the domain model.

### Anti-Pattern: Computing in the UI

```razor
<!-- WRONG: Business logic in Blazor -->
<MudText>Total: @(order.Quantity * order.UnitPrice)</MudText>
<MudText>Status: @(order.Total > 1000 ? "High Value" : "Standard")</MudText>
@if (order.Quantity > 0 && order.UnitPrice > 0 && order.Total > 500)
{
    <MudAlert>Qualifies for discount</MudAlert>
}
```

### Correct: Domain Model Computes, UI Binds

```csharp
// Domain model owns all computation
public Order(IEntityBaseServices<Order> services) : base(services)
{
    RuleManager.AddAction(
        t => t.Total = t.Quantity * t.UnitPrice,
        t => t.Quantity,
        t => t.UnitPrice);

    RuleManager.AddAction(
        t => t.ValueCategory = t.Total > 1000 ? "High Value" : "Standard",
        t => t.Total);

    RuleManager.AddAction(
        t => t.QualifiesForDiscount = t.Quantity > 0
            && t.UnitPrice > 0
            && t.Total > 500,
        t => t.Total);
}

public partial decimal Total { get; set; }
public partial string ValueCategory { get; set; }
public partial bool QualifiesForDiscount { get; set; }
```

```razor
<!-- UI is a thin binding layer -->
<MudText>Total: @order.Total</MudText>
<MudText>Status: @order.ValueCategory</MudText>
@if (order.QualifiesForDiscount)
{
    <MudAlert>Qualifies for discount</MudAlert>
}
```

**Why this matters:**
- `Total`, `ValueCategory`, and `QualifiesForDiscount` update automatically when inputs change
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

```csharp
// Domain model exposes computed visibility state
public Visit(IEntityBaseServices<Visit> services) : base(services)
{
    RuleManager.AddAction(
        t => t.ShowTreatmentPanel = t.Status == "Active"
            && t.TreatmentPlan != null
            && t.TreatmentPlan.IsApproved
            && !t.IsComplete,
        t => t.Status,
        t => t.TreatmentPlan,
        t => t.IsComplete);

    RuleManager.AddAction(
        t => t.ShowReviewAlert = t.HasAreasNeedingReview
            && t.Status != "Discharged",
        t => t.HasAreasNeedingReview,
        t => t.Status);
}

public partial bool ShowTreatmentPanel { get; set; }
public partial bool ShowReviewAlert { get; set; }
```

```razor
<!-- UI just binds -->
@if (visit.ShowTreatmentPanel)
{
    <TreatmentPanel />
}
@if (visit.ShowReviewAlert)
{
    <ReviewAlert />
}
```

## Pattern 3: Cascading State via Chained Rules

When one property change should trigger a cascade of updates, use chained rules. Setting a property inside a rule triggers rules that watch that property.

```csharp
public TreatmentPlan(IEntityBaseServices<TreatmentPlan> services) : base(services)
{
    // Step 1: When diagnosis changes, update treatment protocol
    RuleManager.AddActionAsync(
        async t =>
        {
            var protocol = await protocolService.GetForDiagnosis(t.DiagnosisCode);
            t.ProtocolName = protocol.Name;
            t.MaxVisits = protocol.MaxVisits;
        },
        t => t.DiagnosisCode);

    // Step 2: When MaxVisits changes, update remaining visits
    // This fires automatically when step 1 sets MaxVisits
    RuleManager.AddAction(
        t => t.RemainingVisits = t.MaxVisits - t.CompletedVisits,
        t => t.MaxVisits,
        t => t.CompletedVisits);

    // Step 3: When remaining visits changes, update status
    RuleManager.AddAction(
        t => t.NeedsExtension = t.RemainingVisits <= 2
            && t.CompletedVisits > 0,
        t => t.RemainingVisits);
}
```

The cascade is: `DiagnosisCode` -> `MaxVisits` -> `RemainingVisits` -> `NeedsExtension`. The UI binds to `NeedsExtension` without knowing about the cascade.

## Pattern 4: Async Rules — the Client-Resident Case Only

`AddActionAsync` and class-based `AsyncRuleBase<T>` let a rule await. Use them only when the awaited work is **client-resident** — a computation that never leaves the browser: a local engine, a lookup over graph state that is already loaded, a calculation that is merely expensive.

```csharp
// Client-resident: the engine is local, nothing leaves the browser.
// The header cells recompute as the provider moves the sliders.
RuleManager.AddAction(
    t => t.PowerW = t.DSeconds > 0 && t.ATotalCm2 > 0
        ? t.Fluence * t.ATotalCm2 / t.DSeconds
        : 0,
    t => t.Fluence, t => t.ATotalCm2, t => t.DSeconds);
```

**Do not use a rule to fetch.** A rule that calls a remote service fires a server round-trip from inside a property setter: on every keystroke, with no user intent behind it, no cancellation, and nowhere to show what happened. In a client-server application that work belongs on a seam the ViewModel invokes deliberately (rung 3), or on a read model the screen loads once (rung 4).

```csharp
// WRONG: a fetch disguised as a rule — round-trips on every keystroke
RuleManager.AddActionAsync(
    async t =>
    {
        var coverage = await insuranceService.GetCoverage(t.InsuranceId);
        t.Copay = coverage.CopayAmount;
    },
    t => t.InsuranceId);

// RIGHT: the ViewModel handles a named gesture ("Look up" clicked, or the field committed)
// by invoking a seam; the seam owns the lookup and hands back what the screen binds to.
public async Task LookUpCoverageAsync()
    => Coverage = await _coverageLookup.Execute(Patient.InsuranceId);
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

```csharp
public DateRangeEntity(IEntityBaseServices<DateRangeEntity> services) : base(services)
{
    RuleManager.AddValidation(
        t => t.EndDate < t.StartDate
            ? "End date must be after start date" : "",
        t => t.EndDate,
        t => t.StartDate);

    RuleManager.AddValidation(
        t => (t.EndDate - t.StartDate)?.Days > 365
            ? "Date range cannot exceed one year" : "",
        t => t.EndDate,
        t => t.StartDate);
}
```

The UI just binds date pickers to `StartDate` and `EndDate`. Validation fires automatically and shows through `PropertyMessages`.

## Pattern 6: Child Property Triggers — Parent-Child Reactivity via AddAction

When a parent needs to react to child property changes, use `AddAction` with a **child property trigger expression**. This is the same type-safe, expression-based mechanism used for same-object reactivity.

### The Key Syntax: `t => t.ChildCollection![0].ChildProperty`

The `[0]` indexer is a syntactic placeholder — it does NOT mean "only the first item." `TriggerProperty` walks the expression tree, skips the indexer, and produces the property path `"ChildCollection.ChildProperty"`. When any child's property changes, the event bubbles up with this same path and the rule fires.

```csharp
public Order(IEntityBaseServices<Order> services) : base(services)
{
    // Recalculate total when any child item's LineTotal changes
    RuleManager.AddAction(
        t => t.OrderTotal = t.Items?.Sum(i => i.LineTotal) ?? 0,
        t => t.Items![0].LineTotal);
}

public partial decimal OrderTotal { get; set; }
```

### Multiple Child Properties

To react to multiple child properties, add multiple trigger expressions:

```csharp
RuleManager.AddAction(
    t => t.RecalculateAggregates(),
    t => t.Items![0].LineTotal,
    t => t.Items![0].Quantity);

// Works with single child objects too (not just collections):
RuleManager.AddAction(
    t => t.AddressLabel = $"{t.Address!.City}, {t.Address.State}",
    t => t.Address!.City,
    t => t.Address!.State);
```

### Common Mistake: Using Collection Reference as Trigger

```csharp
// WRONG: Only fires when Items property is reassigned (Items = newList),
// NOT when child items within the list change their properties.
RuleManager.AddAction(
    t => t.OrderTotal = t.Items?.Sum(i => i.LineTotal) ?? 0,
    t => t.Items);  // "Items" != "Items.LineTotal" — exact match, never fires

// RIGHT: Trigger on the specific child property
RuleManager.AddAction(
    t => t.OrderTotal = t.Items?.Sum(i => i.LineTotal) ?? 0,
    t => t.Items![0].LineTotal);  // "Items.LineTotal" — matches child changes
```

### When to Use NeatooPropertyChanged Instead

Child property triggers handle most parent-child reactivity. Reserve `NeatooPropertyChanged` for cases that need:
- **Event args inspection** — `ChangeReason`, `Source`, `FullPropertyName`
- **React to ANY child change** regardless of which property
- **Cross-sibling rules** — a list override that triggers rules on sibling items when one changes (see `HandleNeatooPropertyChanged` override pattern)

```csharp
// NeatooPropertyChanged fallback — only when event args or broad matching is needed
NeatooPropertyChanged += (args) =>
{
    if (args.OriginalEventArgs.Reason == ChangeReason.UserEdit)
    {
        // React to any user-initiated child change
    }
    return Task.CompletedTask;
};

// Cross-sibling pattern — override in EntityListBase subclass
protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    await base.HandleNeatooPropertyChanged(eventArgs);
    if (eventArgs.PropertyName == nameof(IPersonPhone.PhoneType)
        && eventArgs.Source is IPersonPhone changed)
    {
        // Re-run rules on all siblings except the one that changed
        await Task.WhenAll(this.Except([changed]).Select(c => c.RunRules()));
    }
}
```

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

```csharp
// Domain model method encapsulates the transition
public void Approve(string approverName)
{
    if (Status != "Pending")
        throw new InvalidOperationException($"Cannot approve from status '{Status}'");

    Status = "Approved";
    ApprovedBy = approverName;
    ApprovedDate = DateTime.Now;
}

// Domain model computes whether approval is allowed
public partial bool CanApprove { get; set; }

// Constructor rule:
RuleManager.AddAction(
    t => t.CanApprove = t.Status == "Pending" && t.IsValid,
    t => t.Status);
```

```razor
<!-- UI calls domain method, binds to computed state -->
<MudButton Disabled="@(!plan.CanApprove)" OnClick="@(() => plan.Approve(currentUser.Name))">
    Approve
</MudButton>
```

## Pattern 8: Child Collection Aggregation

When computing aggregate values from child collections (sums, counts, any/all), expose these as domain properties using child property triggers. This is one of the most common places where LINQ ends up in `.razor` files.

### Anti-Pattern: LINQ in Razor

```razor
<!-- WRONG: Aggregation logic in UI -->
<MudText>Total: @order.Items.Sum(i => i.LineTotal)</MudText>
<MudText>Item Count: @order.Items.Count(i => !i.IsDeleted)</MudText>
@if (order.Items.Any(i => i.Quantity <= 0))
{
    <MudAlert>Some items have invalid quantities</MudAlert>
}
```

### Correct: Domain Aggregates, UI Binds

```csharp
public Order(IEntityBaseServices<Order> services) : base(services)
{
    // Recalculate when any child's LineTotal changes
    RuleManager.AddAction(
        t => t.OrderTotal = t.Items?.Sum(i => i.LineTotal) ?? 0,
        t => t.Items![0].LineTotal);

    // Recalculate when any child's Quantity changes
    RuleManager.AddAction(
        t => t.HasInvalidQuantities = t.Items?.Any(i => i.Quantity <= 0) ?? false,
        t => t.Items![0].Quantity);
}

public partial decimal OrderTotal { get; set; }
public partial bool HasInvalidQuantities { get; set; }
```

```razor
<!-- UI binds to precomputed domain properties -->
<MudText>Total: @order.OrderTotal</MudText>
@if (order.HasInvalidQuantities)
{
    <MudAlert>Some items have invalid quantities</MudAlert>
}
```

Each aggregation targets the specific child property it depends on. The `[0]` indexer is a syntactic placeholder — any child in the collection that changes the named property triggers the rule.

## Pattern 9: Parent-as-Orchestrator — Cross-Child Coordination

When one child changes and a different child needs updating, the parent orchestrates via `AddAction`. The action body receives the parent (`t`), which can reach any child. This is the same child property trigger mechanism — the only difference is the action pushes changes DOWN to another child instead of computing a parent property.

### Anti-Pattern: UI Bridges Between Entities

```razor
@code {
    void OnShippingStateChanged(string state)
    {
        // WRONG: UI orchestrates between domain children
        var rate = TaxRates.Get(state);
        foreach (var item in order.Items)
            item.TaxRate = rate;

        order.BillingAddress.DefaultState = state;
    }
}
```

### Correct: Parent Orchestrates in the Domain

```csharp
public Order(IEntityBaseServices<Order> services) : base(services)
{
    // When shipping state changes, update tax rate on all items
    RuleManager.AddAction(
        t =>
        {
            var rate = TaxRates.Get(t.ShippingAddress!.State);
            foreach (var item in t.Items!)
                item.TaxRate = rate;
        },
        t => t.ShippingAddress!.State);

    // When shipping state changes, default billing state
    RuleManager.AddAction(
        t => t.BillingAddress!.DefaultState = t.ShippingAddress!.State,
        t => t.ShippingAddress!.State);

    // When any item's status changes, update parent summary
    // AND enable/disable the payment child
    RuleManager.AddAction(
        t =>
        {
            t.AllItemsConfirmed = t.Items?.All(i => i.Status == "Confirmed") ?? false;
            t.Payment!.Enabled = t.AllItemsConfirmed;
        },
        t => t.Items![0].Status);
}
```

```razor
<!-- UI just binds — no bridging logic -->
<ShippingAddressForm Address="@order.ShippingAddress" />
<BillingAddressForm Address="@order.BillingAddress" />
<PaymentPanel Payment="@order.Payment" />
```

### Orchestrator Variants

| Variant | Trigger | Action |
|---------|---------|--------|
| Single child → single child | `t => t.ChildA!.Prop` | Sets `t.ChildB!.Prop` |
| Single child → collection | `t => t.ChildA!.Prop` | Iterates `t.Items!` and sets properties |
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
| A guard in the ViewModel that duplicates one the seam already enforces | Dead code that will drift | Delete it; read the seam's answer |
| `InitializeAsync` that does more than fetch, bind, and subscribe | Load-time policy | The seam's `Open` / `[Fetch]` (3) |

**Rule of thumb:** every ViewModel member that writes an entity must name the gesture it handles; every ViewModel bool must be a read, not a composition.

## Class-Based Rules for Complex Logic

When logic exceeds 5 lines or needs dependency injection, use `AsyncRuleBase<T>` instead of inline lambdas:

```csharp
internal class CalculateInsuranceEligibility : AsyncRuleBase<Patient>
{
    private readonly IEligibilityService _service;

    public CalculateInsuranceEligibility(IEligibilityService service)
        : base(t => t.InsuranceId, t => t.DateOfBirth)
    {
        _service = service;
    }

    protected override async Task<IRuleMessages> Execute(
        Patient target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.InsuranceId))
        {
            target.IsEligible = false;
            target.EligibilityMessage = "";
            return None;
        }

        var result = await _service.CheckEligibility(
            target.InsuranceId, target.DateOfBirth);

        target.IsEligible = result.Eligible;
        target.EligibilityMessage = result.Message;
        target.CoveragePercent = result.CoveragePercent;

        return result.Eligible
            ? None
            : (nameof(Patient.InsuranceId), result.Message).AsRuleMessages();
    }
}
```

Register in the entity constructor:
```csharp
RuleManager.AddRule(new CalculateInsuranceEligibility(eligibilityService));
```

## Testing Advantage

The payoff: all business logic is testable without a UI:

```csharp
[TestMethod]
public async Task Order_QualifiesForDiscount_WhenTotalExceeds500()
{
    var order = orderFactory.Create();
    order.Quantity = 10;
    order.UnitPrice = 60m;
    await order.WaitForTasks();

    Assert.IsTrue(order.QualifiesForDiscount);
    Assert.AreEqual("High Value", order.ValueCategory);
    Assert.AreEqual(600m, order.Total);
}
```

This test validates three computed properties without touching any UI code. If the logic lived in a `.razor` file, this test would be impossible.
