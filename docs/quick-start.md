# Quick Start Guide

This guide gets you from zero to a working Neatoo aggregate in 10 minutes.

## Prerequisites

- .NET 8 or later
- Visual Studio 2022 or VS Code with C# extension

## Installation

```bash
dotnet add package Neatoo
```

> **Note:** Neatoo includes RemoteFactory as a dependency—no separate install needed.

For Blazor with MudBlazor components:
```bash
dotnet add package Neatoo.Blazor.MudNeatoo
```

## Step 1: Define Your Aggregate Interface

Every Neatoo aggregate requires a public interface:

<!-- snippet: qs-interface-pattern -->
```cs
public partial interface IOrder : IEntityBase
{
    // Interface members are auto-generated from partial properties
}
```
<!-- endSnippet -->

> **Learn more:** [Aggregates and Entities](aggregates-and-entities.md) - Interface pattern, base class selection

## Step 2: Create the Aggregate Root

<!-- snippet: qs-aggregate-root -->
```cs
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }

    // Partial properties - Neatoo source-generates the backing implementation
    public partial int? Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial decimal Total { get; set; }
    public partial DateTime OrderDate { get; set; }

    // Mapper methods - manually implemented
    public void MapFrom(OrderEntity entity)
    {
        Id = entity.Id;
        CustomerName = entity.CustomerName;
        Total = entity.Total;
        OrderDate = entity.OrderDate;
    }

    public void MapTo(OrderEntity entity)
    {
        entity.Id = Id ?? 0;
        entity.CustomerName = CustomerName;
        entity.Total = Total;
        entity.OrderDate = OrderDate;
    }

    // MapModifiedTo - source-generated, only copies modified properties
    public partial void MapModifiedTo(OrderEntity entity);

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
}
```
<!-- endSnippet -->

> **Learn more:** [Factory Operations](factory-operations.md) - Create, Fetch, Insert, Update, Delete lifecycle | [Mapper Methods](mapper-methods.md) - MapFrom, MapTo, MapModifiedTo

## Step 3: Register Services

### Server (ASP.NET Core)

<!-- snippet: qs-server-setup -->
```cs
// Program.cs (ASP.NET Core)
public static void ConfigureServer(WebApplicationBuilder builder, WebApplication app)
{
    builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IOrder).Assembly);

    // Add the RemoteFactory endpoint
    app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request, CancellationToken token) =>
    {
        var handler = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
        return handler(request, token);
    });
}
```
<!-- endSnippet -->

### Client (Blazor WebAssembly)

<!-- snippet: qs-client-setup -->
```cs
// Program.cs (Blazor WebAssembly)
public static void ConfigureClient(IServiceCollection services)
{
    services.AddNeatooServices(NeatooFactory.Remote, typeof(IOrder).Assembly);
    services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        new HttpClient { BaseAddress = new Uri("https://localhost:5001") });
}
```
<!-- endSnippet -->

> **Learn more:** [Remote Factory Pattern](remote-factory.md) - Client-server setup, NeatooFactory enum

## Step 4: Use the Factory

<!-- snippet: qs-factory-usage -->
```cs
public async Task FactoryUsageExample(int orderId)
{
    // Create a new order
    var order = _orderFactory.Create();
    order.CustomerName = "Acme Corp";
    order.Total = 150.00m;

    // Save returns the updated entity (with generated ID, etc.)
    order = await _orderFactory.Save(order);

    // Fetch an existing order
    var existingOrder = await _orderFactory.Fetch(orderId);

    // Modify and save
    existingOrder.Total = 175.00m;
    existingOrder = await _orderFactory.Save(existingOrder);
}
```
<!-- endSnippet -->

> **Learn more:** [Factory Operations](factory-operations.md) - Factory methods and save patterns

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

> **Learn more:** [Blazor Binding](blazor-binding.md) - MudNeatoo components, meta-properties (IsBusy, IsSavable, IsValid)

## Next Steps

- [Aggregates and Entities](aggregates-and-entities.md) - Deep dive into entity design
- [Validation and Rules](validation-and-rules.md) - Business rule implementation
- [Factory Operations](factory-operations.md) - Complete factory lifecycle
- [Blazor Binding](blazor-binding.md) - UI integration patterns
