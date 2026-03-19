# Domain Logic Placement

This reference provides detailed guidance for placing business logic in Neatoo domain models instead of the UI layer. The core principle: **the domain model is the home for all business logic.** The UI is a thin binding layer.

## The Logic Placement Decision Tree

When encountering any business logic during implementation, apply this decision tree:

```
Is this logic about WHAT to display or HOW to display it?
├── HOW to display (CSS class, layout, component choice) → UI layer
└── WHAT to display (computed values, conditions, derived state) → Domain model
    ├── Derived from own property values? → AddAction / AddActionAsync
    ├── Reacts to child property changes? → AddAction with child trigger (t => t.Children![0].Prop)
    ├── Parent orchestrates between children? → AddAction with child trigger, action pushes to other child
    ├── Validation / error condition? → AddValidation / AddValidationAsync
    ├── Reacts to own property changes? → AddAction triggered by that property
    ├── Cross-property computation? → AddAction with multiple triggers
    ├── Cross-sibling rules in a list? → Override HandleNeatooPropertyChanged
    └── Needs external service? → AddActionAsync or class-based AsyncRuleBase<T>
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

## Pattern 4: Async Side-Effects via AddActionAsync

When a property change should fetch external data or perform I/O, use `AddActionAsync`.

```csharp
public Patient(IEntityBaseServices<Patient> services,
    IInsuranceService insuranceService) : base(services)
{
    // When insurance ID changes, look up coverage
    RuleManager.AddActionAsync(
        async t =>
        {
            if (string.IsNullOrEmpty(t.InsuranceId)) return;
            var coverage = await insuranceService.GetCoverage(t.InsuranceId);
            t.CoveragePlan = coverage.PlanName;
            t.CoverageActive = coverage.IsActive;
            t.Copay = coverage.CopayAmount;
        },
        t => t.InsuranceId);
}
```

The UI binds to `CoveragePlan`, `CoverageActive`, `Copay`. When the user types an insurance ID, the domain model reactively fetches and populates. The UI shows a spinner via `IsBusy`.

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

When reviewing .razor files, look for these smells that indicate misplaced logic:

| Smell in .razor | Move To |
|----------------|---------|
| `@(a.X * b.Y)` arithmetic | `AddAction` computed property |
| `@if (a.Status == "X" && b.Count > 0)` | Domain `bool` property via `AddAction` |
| `@(list.Where(...).Count())` LINQ | Domain computed property |
| `@(condition ? "Label A" : "Label B")` ternary | Domain `string` property |
| `OnClick` handler that sets multiple properties | Domain method |
| `OnChanged` handler that validates | `AddValidation` or `AddValidationAsync` |
| `@code` block with > 5 lines of logic | Domain rules or methods |
| Event handler setting properties on two entities | Domain `AddAction` orchestrator |

**Rule of thumb:** If a `.razor` file has more than 3 conditional/computed expressions, business logic is leaking into the UI.

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
