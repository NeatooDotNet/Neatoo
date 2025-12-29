# Quick Start Guide

This guide gets you from zero to a working Neatoo aggregate in 10 minutes.

## Prerequisites

- .NET 8 or later
- Visual Studio 2022 or VS Code with C# extension

## Installation

```bash
dotnet add package Neatoo
dotnet add package Neatoo.RemoteFactory
```

For Blazor with MudBlazor:
```bash
dotnet add package Neatoo.Blazor.MudNeatoo
```

## Step 1: Define Your Aggregate Interface

Every Neatoo aggregate requires a public interface:

```csharp
using Neatoo;

public partial interface IOrder : IEntityBase
{
    // Interface members are auto-generated from partial properties
}
```

## Step 2: Create the Aggregate Root

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    // Partial properties - Neatoo source-generates the backing implementation
    public partial int? Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial decimal Total { get; set; }
    public partial DateTime OrderDate { get; set; }

    // Create operation - called when factory creates a new instance
    [Create]
    public void Create()
    {
        OrderDate = DateTime.UtcNow;
    }

    // Fetch operation - loads from database
    [Remote]
    [Fetch]
    public async Task Fetch(int id, [Service] IOrderDbContext db)
    {
        var entity = await db.Orders.FindAsync(id);
        if (entity != null)
        {
            MapFrom(entity);
        }
    }

    // Insert operation - persists new entity
    [Remote]
    [Insert]
    public async Task Insert([Service] IOrderDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = new OrderEntity();
        MapTo(entity);
        db.Orders.Add(entity);
        await db.SaveChangesAsync();
    }

    // Update operation - persists changes
    [Remote]
    [Update]
    public async Task Update([Service] IOrderDbContext db)
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = await db.Orders.FindAsync(Id);
        if (entity == null)
            throw new KeyNotFoundException("Order not found");

        MapModifiedTo(entity);
        await db.SaveChangesAsync();
    }

    // Mapper methods - source-generated implementations
    public partial void MapFrom(OrderEntity entity);
    public partial void MapTo(OrderEntity entity);
    public partial void MapModifiedTo(OrderEntity entity);
}
```

## Step 3: Register Services

### Server (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IOrder).Assembly);

// Add the RemoteFactory endpoint
app.MapPost("/api/neatoo", (HttpContext ctx, RemoteRequestDto request) =>
    NeatooEndpoint.HandleRequest(ctx, request));
```

### Client (Blazor WebAssembly)

```csharp
// Program.cs
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IOrder).Assembly);
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001") });
```

## Step 4: Use the Factory

```csharp
// Inject the generated factory
@inject IOrderFactory OrderFactory

// Create a new order
var order = OrderFactory.Create();
order.CustomerName = "Acme Corp";
order.Total = 150.00m;

// Save returns the updated entity (with generated ID, etc.)
order = await OrderFactory.Save(order);

// Fetch an existing order
var existingOrder = await OrderFactory.Fetch(orderId);

// Modify and save
existingOrder.Total = 175.00m;
existingOrder = await OrderFactory.Save(existingOrder);
```

## Step 5: Bind to Blazor UI

```razor
@inject IOrderFactory OrderFactory

<MudNeatooTextField T="string" EntityProperty="@order[nameof(IOrder.CustomerName)]" />
<MudNeatooNumericField T="decimal" EntityProperty="@order[nameof(IOrder.Total)]" />

<MudButton Disabled="@(!order.IsSavable)" OnClick="@SaveOrder">Save</MudButton>

@if (order.IsBusy)
{
    <MudProgressCircular Indeterminate="true" />
}

<NeatooValidationSummary Entity="@order" />

@code {
    private IOrder order = default!;

    protected override void OnInitialized()
    {
        order = OrderFactory.Create();
    }

    private async Task SaveOrder()
    {
        order = await OrderFactory.Save(order);
    }
}
```

## Key Concepts

### Partial Properties

Properties must be declared as `partial` for Neatoo to generate the backing code that enables:
- Change tracking (IsModified)
- Validation rule triggering
- State serialization for client-server transfer

```csharp
// Correct - Neatoo generates implementation
public partial string? Name { get; set; }

// Incorrect - no state tracking
public string? Name { get; set; }
```

### Factory Attributes

| Attribute | Purpose |
|-----------|---------|
| `[Factory]` | Marks class for factory generation |
| `[Create]` | Called when factory creates new instance |
| `[Fetch]` | Called when factory loads from data source |
| `[Insert]` | Called when saving a new (IsNew) entity |
| `[Update]` | Called when saving an existing entity |
| `[Delete]` | Called when saving a deleted entity |
| `[Remote]` | Method is callable from client via RemoteFactory |
| `[Service]` | Parameter is injected from DI container |

### Meta-Properties

Neatoo entities expose bindable meta-properties:

| Property | Description |
|----------|-------------|
| `IsNew` | Entity has not been persisted |
| `IsModified` | Entity or children have changes |
| `IsValid` | All validation rules pass |
| `IsSavable` | Modified, valid, not busy, not child |
| `IsBusy` | Async operations in progress |
| `IsDeleted` | Entity is marked for deletion |

## Next Steps

- [Aggregates and Entities](aggregates-and-entities.md) - Deep dive into entity design
- [Validation and Rules](validation-and-rules.md) - Business rule implementation
- [Factory Operations](factory-operations.md) - Complete factory lifecycle
- [Blazor Binding](blazor-binding.md) - UI integration patterns
