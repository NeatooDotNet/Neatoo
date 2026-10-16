I've read the Neatoo, RemoteFactory and MudNeatoo skill references. The full answer is below.

# zCRM: the contact's "Today" tab

There are three projects: **ZCrm.Domain** (referenced by both tiers), **ZCrm.Server** (ASP.NET Core and EF Core) and **ZCrm.Client** (Blazor WASM and MudBlazor). The point of the layout is that the domain assembly runs in the browser and on the server, so the client and server need no DTOs and no controllers.

---

## 1. Domain (`ZCrm.Domain`)

### Persistence contract and rows

These are in the domain because the operations take them as `[Service]` parameters and the child operations take their own row. They are plain classes with no EF reference, so EF Core never ships to the browser.

```cs
// ZCrm.Domain/Visits/VisitRows.cs
namespace ZCrm.Domain.Visits;

public class VisitRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public DateOnly VisitDate { get; set; }
    public string SalespersonName { get; set; } = "";
    public string? Notes { get; set; }
    public ICollection<ActionItemRow> ActionItems { get; set; } = new List<ActionItemRow>();
}

public class ActionItemRow
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public string Description { get; set; } = "";
    public bool IsDone { get; set; }
}
```

```cs
// ZCrm.Domain/Visits/IVisitDbContext.cs
namespace ZCrm.Domain.Visits;

/// <summary>
/// What the Visit aggregate's server operations need from the store. Implemented by the server's DbContext.
/// </summary>
public interface IVisitDbContext
{
    /// <summary>The contact's visit on that day, with its action items; null when there is none.</summary>
    Task<VisitRow?> FindVisitForDayAsync(Guid contactId, DateOnly visitDate);

    /// <summary>The visit with its action items; null when there is none.</summary>
    Task<VisitRow?> FindVisitAsync(Guid id);

    void AddVisit(VisitRow row);
    void RemoveVisit(VisitRow row);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The signed-in salesperson. Server-only: resolved from the request's user.
/// </summary>
public interface ICurrentSalesperson
{
    string Name { get; }
}
```

### Interfaces

The concrete classes are `internal` and everything refers to these interfaces. `IVisit` extends `IEntityRoot`, so it has `Save()`. `IActionItem` extends `IEntityBase`, so the compiler won't let anyone save a child on its own.

```cs
// ZCrm.Domain/Visits/IVisitInterfaces.cs
using Neatoo;

namespace ZCrm.Domain.Visits;

/// <summary>Visit aggregate root: one contact, one day.</summary>
public interface IVisit : IEntityRoot
{
    Guid Id { get; }
    Guid ContactId { get; }
    DateOnly VisitDate { get; }
    string? SalespersonName { get; }
    string? Notes { get; set; }
    IActionItemList? ActionItems { get; }
}

public interface IActionItem : IEntityBase
{
    Guid Id { get; }
    string? Description { get; set; }
    bool IsDone { get; set; }
}

public interface IActionItemList : IEntityListBase<IActionItem>
{
    /// <summary>Adds a new, empty action item to the visit.</summary>
    Task<IActionItem> AddActionItem();
}
```

### Visit (root)

Load-or-create is the `[Remote, Execute]` `OpenToday` method. It is one server call: it looks for the existing visit and creates one only when there is none. `FetchForDay` and `Create` are `internal` without `[Remote]`, which makes them server-only. That means the client can't start a visit with a salesperson name it made up; the name comes from the request's user on the server.

```cs
// ZCrm.Domain/Visits/Visit.cs
using Neatoo;
using Neatoo.RemoteFactory;

namespace ZCrm.Domain.Visits;

[Factory]
internal partial class Visit : EntityBase<Visit>, IVisit
{
    public partial Guid Id { get; private set; }
    public partial Guid ContactId { get; private set; }
    public partial DateOnly VisitDate { get; private set; }
    public partial string? SalespersonName { get; private set; }
    public partial string? Notes { get; set; }
    public partial IActionItemList? ActionItems { get; private set; }

    public Visit(IEntityBaseServices<Visit> services) : base(services) { }

    /// <summary>
    /// The contact's visit for the salesperson's today: the stored one, or a new one. One round trip.
    /// </summary>
    /// <remarks>
    /// The client passes its own calendar date: the browser's today is the salesperson's
    /// today, while the server's clock may be on another day.
    /// </remarks>
    [Remote, Execute]
    public static async Task<IVisit> OpenToday(
        Guid contactId,
        DateOnly today,
        [Service] IVisitFactory factory,
        [Service] ICurrentSalesperson salesperson)
    {
        return await factory.FetchForDay(contactId, today)
            ?? factory.Create(contactId, today, salesperson.Name);
    }

    /// <summary>
    /// Starts the day's visit. Server-only: only OpenToday starts a visit.
    /// </summary>
    [Create]
    internal void Create(
        Guid contactId,
        DateOnly visitDate,
        string salespersonName,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        ContactId = contactId;
        VisitDate = visitDate;
        SalespersonName = salespersonName;
        ActionItems = actionItemListFactory.Create();
    }

    /// <summary>
    /// Loads the contact's visit on that day; false when there is none. Server-only.
    /// </summary>
    [Fetch]
    internal async Task<bool> FetchForDay(
        Guid contactId,
        DateOnly visitDate,
        [Service] IVisitDbContext db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        var row = await db.FindVisitForDayAsync(contactId, visitDate);
        if (row == null)
            return false;

        Id = row.Id;
        ContactId = row.ContactId;
        VisitDate = row.VisitDate;
        SalespersonName = row.SalespersonName;
        Notes = row.Notes;
        ActionItems = actionItemListFactory.Fetch(row.ActionItems);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert(
        [Service] IVisitDbContext db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        // Server gate against a caller that skipped IsSavable.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
            throw new SaveOperationException(SaveFailureReason.IsInvalid);

        Id = Guid.NewGuid();
        var row = new VisitRow { Id = Id };
        MapTo(row);
        db.AddVisit(row);
        actionItemListFactory.Save(ActionItems!, row.ActionItems);
        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update(
        [Service] IVisitDbContext db,
        [Service] IActionItemListFactory actionItemListFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
            throw new SaveOperationException(SaveFailureReason.IsInvalid);

        var row = await db.FindVisitAsync(Id)
            ?? throw new KeyNotFoundException($"Visit {Id} not found");

        if (IsSelfModified)
            MapTo(row);

        actionItemListFactory.Save(ActionItems!, row.ActionItems);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes the visit; its action item rows go with it.
    /// </summary>
    /// <remarks>
    /// Delete() on IEntityBase can reach this state, so Save needs somewhere to route it.
    /// </remarks>
    [Remote, Delete]
    internal async Task Delete([Service] IVisitDbContext db)
    {
        var row = await db.FindVisitAsync(Id)
            ?? throw new KeyNotFoundException($"Visit {Id} not found");
        db.RemoveVisit(row);
        await db.SaveChangesAsync();
    }

    private void MapTo(VisitRow row)
    {
        row.ContactId = ContactId;
        row.VisitDate = VisitDate;
        row.SalespersonName = SalespersonName ?? "";
        row.Notes = Notes;
    }
}
```

### ActionItemList

The list coordinates the save: it creates, finds or removes each item's row, then hands each item its row. Its operations are `internal` and never `[Remote]` because the root's operation already runs on the server.

```cs
// ZCrm.Domain/Visits/ActionItemList.cs
using Neatoo;
using Neatoo.RemoteFactory;

namespace ZCrm.Domain.Visits;

[Factory]
internal partial class ActionItemList : EntityListBase<IActionItem>, IActionItemList
{
    private readonly IActionItemFactory _actionItemFactory;

    // The child factory is registered on both tiers, so the list can be built on either.
    public ActionItemList(IActionItemFactory actionItemFactory)
    {
        _actionItemFactory = actionItemFactory;
    }

    [Create]
    public void Create()
    {
    }

    [Fetch]
    internal void Fetch(IEnumerable<ActionItemRow> rows, [Service] IActionItemFactory actionItemFactory)
    {
        foreach (var row in rows)
        {
            Add(actionItemFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<ActionItemRow> rows, [Service] IActionItemFactory actionItemFactory)
    {
        foreach (var item in this.Union(DeletedList))
        {
            if (item.IsDeleted)
            {
                // A new item that was removed is discarded and never reaches DeletedList.
                if (!item.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == item.Id));
                }
            }
            else if (item.IsNew)
            {
                var row = new ActionItemRow();
                rows.Add(row);
                actionItemFactory.Save(item, row);
            }
            else if (item.IsModified)
            {
                actionItemFactory.Save(item, rows.Single(r => r.Id == item.Id));
            }
        }
    }

    public async Task<IActionItem> AddActionItem()
    {
        var item = await _actionItemFactory.Create();
        Add(item);
        return item;
    }
}
```

### ActionItem (child)

Each item writes its own row. Its `Create` is `public` and runs locally, so "Add action item" needs no server call. `Create` runs the item's rules, so a blank item shows "required" straight away and disables Save. Without that, the browser would allow the save and the server gate would then reject it.

```cs
// ZCrm.Domain/Visits/ActionItem.cs
using Neatoo;
using Neatoo.RemoteFactory;

namespace ZCrm.Domain.Visits;

[Factory]
internal partial class ActionItem : EntityBase<ActionItem>, IActionItem
{
    public partial Guid Id { get; private set; }
    public partial string? Description { get; set; }
    public partial bool IsDone { get; set; }

    public ActionItem(IEntityBaseServices<ActionItem> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Description) ? "Describe the action item" : string.Empty,
            t => t.Description);
    }

    [Create]
    public async Task Create()
    {
        await RunRules(RunRulesFlag.All);
    }

    [Fetch]
    internal void Fetch(ActionItemRow row)
    {
        Id = row.Id;
        Description = row.Description;
        IsDone = row.IsDone;
    }

    [Insert]
    internal void Insert(ActionItemRow row)
    {
        Id = Guid.NewGuid();
        MapTo(row);
    }

    [Update]
    internal void Update(ActionItemRow row)
    {
        MapTo(row);
    }

    private void MapTo(ActionItemRow row)
    {
        row.Id = Id;
        row.Description = Description ?? "";
        row.IsDone = IsDone;
    }
}
```

---

## 2. Server (`ZCrm.Server`)

**Packages:** `Neatoo`, `Neatoo.RemoteFactory.AspNetCore`, EF Core with your provider.

### DbContext

The DbContext implements the domain's `IVisitDbContext`, so EF Core stays on the server.

`ValueGeneratedNever()` on the keys matters. The domain assigns its own Guids. If EF thinks a key is generated, it sees a new `ActionItemRow` with its key already set in a tracked collection, treats it as an existing row, and sends an UPDATE where an INSERT was needed.

```cs
// ZCrm.Server/Data/ZCrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using ZCrm.Domain.Visits;

namespace ZCrm.Server.Data;

public class ZCrmDbContext : DbContext, IVisitDbContext
{
    public ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : base(options) { }

    public DbSet<VisitRow> Visits => Set<VisitRow>();
    public DbSet<ActionItemRow> ActionItems => Set<ActionItemRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitRow>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).ValueGeneratedNever();
            e.HasIndex(v => new { v.ContactId, v.VisitDate }).IsUnique(); // one visit per contact per day
            e.HasMany(v => v.ActionItems)
             .WithOne()
             .HasForeignKey(a => a.VisitId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionItemRow>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
        });
    }

    public Task<VisitRow?> FindVisitForDayAsync(Guid contactId, DateOnly visitDate) =>
        Visits.Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.ContactId == contactId && v.VisitDate == visitDate);

    public Task<VisitRow?> FindVisitAsync(Guid id) =>
        Visits.Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.Id == id);

    public void AddVisit(VisitRow row) => Visits.Add(row);

    public void RemoveVisit(VisitRow row) => Visits.Remove(row);
}
```

### The signed-in salesperson

This is where the salesperson's name comes from. It reads the request's user, so it exists only on the server.

```cs
// ZCrm.Server/Identity/HttpCurrentSalesperson.cs
using ZCrm.Domain.Visits;

namespace ZCrm.Server.Identity;

internal sealed class HttpCurrentSalesperson : ICurrentSalesperson
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentSalesperson(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string Name =>
        _accessor.HttpContext?.User.Identity?.Name
        ?? throw new InvalidOperationException("No signed-in salesperson.");
}
```

### Program.cs

There are no controllers: `UseNeatoo()` maps the single endpoint that every factory call goes through.

```cs
// ZCrm.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Domain.Visits;
using ZCrm.Server.Data;
using ZCrm.Server.Identity;

var builder = WebApplication.CreateBuilder(args);

// Neatoo and the generated factories in Server mode. Do not also call
// AddNeatooAspNetCore after this: that breaks remote saves (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IVisit).Assembly);

// The [Service] dependencies the Visit operations resolve.
builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("ZCrm")));
builder.Services.AddScoped<IVisitDbContext>(sp => sp.GetRequiredService<ZCrmDbContext>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentSalesperson, HttpCurrentSalesperson>();

// zCRM's authentication scheme is registered here (assumed; not part of this feature).
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## 3. Client (`ZCrm.Client`)

**Packages:** `Neatoo`, `MudBlazor`, `Neatoo.Blazor.MudNeatoo`.

### Program.cs

```cs
// ZCrm.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using ZCrm.Client;
using ZCrm.Client.Visits;
using ZCrm.Domain.Visits;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Remote mode: the generated factories are proxies; [Remote] calls cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IVisit).Assembly);

// The HttpClient the proxies send through: the server that hosts this client.
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

builder.Services.AddTransient<TodayVisitViewModel>();

await builder.Build().RunAsync();
```

### _Imports.razor

```razor
@* ZCrm.Client/_Imports.razor (additions) *@
@using MudBlazor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using ZCrm.Domain.Visits
@using ZCrm.Client.Visits
```

### TodayVisitViewModel

The ViewModel holds the aggregate, passes the page's actions on to the domain, and saves. It makes no decisions of its own. "Can I save?" is the visit's `IsSavable`. The `IsSaving` flag is there only because `Save()` doesn't mark the entity busy (Neatoo#103), so without it a double-click would send two inserts.

```cs
// ZCrm.Client/Visits/TodayVisitViewModel.cs
using ZCrm.Domain.Visits;

namespace ZCrm.Client.Visits;

/// <summary>
/// ViewModel for a contact's Today tab: opens today's visit, saves it.
/// </summary>
internal sealed class TodayVisitViewModel
{
    private readonly IVisitFactory _visitFactory;

    public TodayVisitViewModel(IVisitFactory visitFactory)
    {
        _visitFactory = visitFactory;
    }

    /// <summary>The aggregate the tab binds to. Null until opened.</summary>
    public IVisit? Visit { get; private set; }

    public bool IsSaving { get; private set; }

    public bool CanSave => !IsSaving && (Visit?.IsSavable ?? false);

    /// <summary>Today's visit for the contact: the stored one, or a new one. One server call.</summary>
    public async Task Open(Guid contactId)
    {
        Visit = null;
        Visit = await _visitFactory.OpenToday(contactId, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task AddActionItem()
    {
        if (Visit?.ActionItems is not null)
        {
            await Visit.ActionItems.AddActionItem();
        }
    }

    public void RemoveActionItem(IActionItem item)
    {
        Visit?.ActionItems?.Remove(item);
    }

    public async Task Save()
    {
        if (Visit is null || IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            await Visit.WaitForTasks();

            if (!Visit.IsSavable)
            {
                return; // invalid or unchanged; the messages are already on the visit
            }

            Visit = (IVisit)await Visit.Save(); // reassign: Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

### TodayVisitTab.razor

The tab loads when its `ContactId` arrives. It subscribes only to the visit's own `PropertyChanged`, because the Save button is the only thing it shows that depends on the visit's state. The inputs refresh themselves. `@key` rebuilds the bound components after a save, because the save returns a new instance.

```razor
@* ZCrm.Client/Visits/TodayVisitTab.razor *@
@using System.ComponentModel
@implements IDisposable
@inject TodayVisitViewModel Vm
@inject ISnackbar Snackbar

@if (Vm.Visit is null)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    <div @key="Vm.Visit">
        <MudText Typo="Typo.subtitle1" Class="mb-3">
            @Vm.Visit.VisitDate.ToString("D") · @Vm.Visit.SalespersonName
        </MudText>

        <NeatooValidationSummary Entity="@Vm.Visit" ShowHeader="false" Dense="true" Class="mb-3" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Visit[nameof(IVisit.Notes)]"
                            Variant="Variant.Outlined"
                            Lines="6"
                            Sizing="InputSizing.Auto"
                            MaxLines="30"
                            UserAttributes="@(new() { ["spellcheck"] = "true" })"
                            Class="mb-4" />

        <MudText Typo="Typo.h6" Class="mb-2">Action items</MudText>

        @foreach (var item in Vm.Visit.ActionItems!)
        {
            <ActionItemEditor @key="item" Item="@item" OnRemove="@(() => Vm.RemoveActionItem(item))" />
        }

        <MudButton Variant="Variant.Outlined"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="Vm.AddActionItem"
                   Class="mb-4">
            Add action item
        </MudButton>

        <div>
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       Disabled="@(!Vm.CanSave)"
                       OnClick="SaveAsync">
                Save
            </MudButton>
        </div>
    </div>
}

@code {
    [Parameter, EditorRequired] public Guid ContactId { get; set; }

    private Guid? _openedFor;
    private IVisit? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (_openedFor == ContactId)
        {
            return;
        }

        _openedFor = ContactId;
        await Vm.Open(ContactId);
        Resubscribe();
    }

    private async Task SaveAsync()
    {
        try
        {
            await Vm.Save();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"The visit was not saved: {ex.Message}", Severity.Error);
        }

        Resubscribe();
    }

    // The Save button reads the visit's IsSavable, so the tab re-renders on the
    // visit's own PropertyChanged (its properties and meta flags) - nothing deeper.
    private void Resubscribe()
    {
        if (ReferenceEquals(_subscribed, Vm.Visit))
        {
            return;
        }

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnVisitChanged;
        }

        _subscribed = Vm.Visit;

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged += OnVisitChanged;
        }
    }

    private void OnVisitChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnVisitChanged;
        }
    }
}
```

### ActionItemEditor.razor

This is one row. The name avoids a clash with `ActionItemRow`. The row shows nothing outside its inputs, so it needs no subscription. The remove action goes back to the ViewModel through the tab.

```razor
@* ZCrm.Client/Visits/ActionItemEditor.razor *@
<MudGrid Spacing="2" Class="mb-1">
    <MudItem xs="2" sm="1" Class="d-flex align-center">
        <MudNeatooCheckBox T="bool" EntityProperty="@Item[nameof(IActionItem.IsDone)]" Label="Done" />
    </MudItem>
    <MudItem xs="9" sm="10">
        <MudNeatooTextField T="string" EntityProperty="@Item[nameof(IActionItem.Description)]" />
    </MudItem>
    <MudItem xs="1" Class="d-flex align-center">
        <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@OnRemove" />
    </MudItem>
</MudGrid>

@code {
    [Parameter, EditorRequired] public IActionItem Item { get; set; } = default!;
    [Parameter] public EventCallback OnRemove { get; set; }
}
```

### On the contact page

MudTabs renders only the active tab by default, so the visit loads when the salesperson opens "Today":

```razor
<MudTabs>
    @* ...other tabs... *@
    <MudTabPanel Text="Today">
        <TodayVisitTab ContactId="@ContactId" />
    </MudTabPanel>
</MudTabs>
```

---

## Decisions and assumptions to check

None of this has been compiled or run.

- **Whose "today":** the browser sends its own date, which is the salesperson's local day. If you'd rather the server decide, it needs each salesperson's time zone, which this spec doesn't give.
- **One visit per contact per day, not per salesperson:** this follows your wording "today's Visit for that contact". If two salespeople can visit the same contact on the same day, add `SalespersonName` to the lookup and the unique index.
- **Two tabs opening the same new visit at once:** both get a new visit, and the second insert fails on the unique index. The tab shows that error in the snackbar.
- **A new, untouched visit can still be saved:** `IsSavable` counts a new object as savable. Clicking Save on a blank new visit stores an empty visit; nothing is stored unless Save is clicked.
- **Authorization:** `OpenToday` and the saves rely on the signed-in user that `ICurrentSalesperson` reads. I didn't add a RemoteFactory authorization attribute because your auth scheme isn't specified. Add one before release.