Here's the Today tab end to end. It follows the Neatoo and RemoteFactory patterns. **I haven't compiled any of it**, since I was asked not to create files. Treat the namespaces of a few Neatoo types (`SaveOperationException`, `RunRulesFlag`, `NeatooPropertyChangedEventArgs`) as things to check on first build.

## Two decisions behind the design

- **One server call finds or starts the visit.** `OpenForDay` runs on the server. It looks up the contact's visit for that date and creates one only if none exists. A `[Create]` that searches the database, or a `[Fetch]` that creates a row, is the shape the libraries rule out.
- **"Today" comes from the browser's clock, the salesperson's name from the server.** A server in UTC would start tomorrow's visit for anyone working in the evening. So `OpenToday` runs on the client and reads the browser's local date. The name, though, comes from the signed-in user on the server and is never taken from the client.

---

## Domain assembly (`zCRM.Domain`, referenced by both client and server)

This assembly runs in the browser and on the server. EF Core stays out of it: server code reaches the database only through `IVisitDb`, which the server implements.

### `Visits/VisitInterfaces.cs`
Public interfaces, internal classes. `IActionItem` extends `IEntityBase`, so a child has no `Save()` and can only be saved through its visit.

```csharp
using Neatoo;

namespace zCRM.Domain.Visits;

/// <summary>Visit aggregate root: one contact, one day.</summary>
public partial interface IVisit : IEntityRoot
{
    Guid Id { get; }
    Guid ContactId { get; }
    DateTime VisitDate { get; }
    string? SalespersonName { get; }
    string? Notes { get; set; }
    IActionItemList? ActionItems { get; }
}

/// <summary>Action item on a visit. Saved through the visit only.</summary>
public partial interface IActionItem : IEntityBase
{
    Guid Id { get; }
    string? Description { get; set; }
    bool IsDone { get; set; }
}

public interface IActionItemList : IEntityListBase<IActionItem>
{
}
```

### `Visits/Visit.cs`
The aggregate root, including the find-or-start entry point. Key points:
- `Create` and `FetchForDay` are `internal` with no `[Remote]`, so only the server can call them. The client's only way in is `OpenToday` / `OpenForDay`.
- `Insert` and `Update` re-run the rules and refuse an invalid visit. That is the server's check against a caller that skipped `IsSavable`.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class Visit : EntityBase<Visit>, IVisit
{
    public Visit(IEntityBaseServices<Visit> services) : base(services) { }

    public partial Guid Id { get; private set; }

    public partial Guid ContactId { get; private set; }

    [DisplayName("Visit date")]
    public partial DateTime VisitDate { get; private set; }

    [DisplayName("Salesperson")]
    public partial string? SalespersonName { get; private set; }

    [DisplayName("Notes")]
    [StringLength(4000)]
    public partial string? Notes { get; set; }

    public partial IActionItemList? ActionItems { get; private set; }

    /// <summary>
    /// Today's visit for the contact. Not [Remote]: runs on the client, so "today"
    /// is the salesperson's local date, then crosses once through OpenForDay.
    /// </summary>
    [Execute]
    public static Task<IVisit> OpenToday(
        Guid contactId,
        [Service] TimeProvider clock,
        [Service] IVisitFactory factory)
    {
        return factory.OpenForDay(contactId, clock.GetLocalNow().Date);
    }

    /// <summary>
    /// The stored visit for the contact and day, or a new one. One round trip.
    /// </summary>
    [Remote, Execute]
    public static async Task<IVisit> OpenForDay(
        Guid contactId,
        DateTime visitDate,
        [Service] IVisitFactory factory)
    {
        return await factory.FetchForDay(contactId, visitDate.Date)
            ?? factory.Create(contactId, visitDate.Date);
    }

    /// <summary>
    /// Starts a visit, stamped with the signed-in salesperson. Server-only:
    /// reached through OpenForDay.
    /// </summary>
    [Create]
    internal void Create(
        Guid contactId,
        DateTime visitDate,
        [Service] ICurrentSalesperson salesperson,
        [Service] IActionItemListFactory itemsFactory)
    {
        ContactId = contactId;
        VisitDate = visitDate;
        SalespersonName = salesperson.Name;
        ActionItems = itemsFactory.Create();
    }

    /// <summary>The contact's visit on the day; false when there is none.</summary>
    [Fetch]
    internal async Task<bool> FetchForDay(
        Guid contactId,
        DateTime visitDate,
        [Service] IVisitDb db,
        [Service] IActionItemListFactory itemsFactory)
    {
        var row = await db.FindForDayAsync(contactId, visitDate);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        ContactId = row.ContactId;
        VisitDate = row.VisitDate;
        SalespersonName = row.SalespersonName;
        Notes = row.Notes;
        ActionItems = itemsFactory.Fetch(row.ActionItems);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert(
        [Service] IVisitDb db,
        [Service] ICurrentSalesperson salesperson,
        [Service] IActionItemListFactory itemsFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        // Paused: assignment is baseline. The stamp is the server's, not what the client sent.
        Id = Guid.NewGuid();
        SalespersonName = salesperson.Name;

        var row = new VisitRow
        {
            Id = Id,
            ContactId = ContactId,
            VisitDate = VisitDate,
            SalespersonName = SalespersonName,
        };
        MapTo(row);
        db.Add(row);

        itemsFactory.Save(ActionItems!, row.ActionItems);

        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update(
        [Service] IVisitDb db,
        [Service] IActionItemListFactory itemsFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Visit {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        itemsFactory.Save(ActionItems!, row.ActionItems);

        await db.SaveChangesAsync();
    }

    [Remote, Delete]
    internal async Task Delete([Service] IVisitDb db)
    {
        var row = await db.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Visit {Id} not found");

        db.Remove(row);
        await db.SaveChangesAsync();
    }

    private void MapTo(VisitRow row)
    {
        row.Notes = Notes;
    }
}
```

### `Visits/ActionItem.cs`
The child entity. Its `Create` is public and runs locally, so the client builds a new item with no server call. `Create` runs the item's rules so a blank row shows "Describe the action item" right away and blocks saving. Without that, a blank item would look valid until the server rejected it.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class ActionItem : EntityBase<ActionItem>, IActionItem
{
    public ActionItem(IEntityBaseServices<ActionItem> services) : base(services) { }

    public partial Guid Id { get; private set; }

    [DisplayName("Action item")]
    [Required(ErrorMessage = "Describe the action item")]
    [StringLength(500)]
    public partial string? Description { get; set; }

    [DisplayName("Done")]
    public partial bool IsDone { get; set; }

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
        row.Description = Description!;
        row.IsDone = IsDone;
    }
}
```

### `Visits/ActionItemList.cs`
The list coordinates the save: it adds, finds or removes rows and hands each item its own row. Each item maps itself, which is what marks it clean afterwards. Removed items that were already saved sit in `DeletedList` until then.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class ActionItemList : EntityListBase<IActionItem>, IActionItemList
{
    [Create]
    public void Create()
    {
    }

    [Fetch]
    internal void Fetch(IEnumerable<ActionItemRow> rows, [Service] IActionItemFactory itemFactory)
    {
        foreach (var row in rows)
        {
            Add(itemFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<ActionItemRow> rows, [Service] IActionItemFactory itemFactory)
    {
        foreach (var item in this.Union(DeletedList))
        {
            if (item.IsDeleted)
            {
                if (!item.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == item.Id));
                }
            }
            else if (item.IsNew)
            {
                var row = new ActionItemRow();
                rows.Add(row);
                itemFactory.Save(item, row);
            }
            else if (item.IsModified)
            {
                itemFactory.Save(item, rows.Single(r => r.Id == item.Id));
            }
        }
    }
}
```

### `Persistence/VisitRows.cs`
The two table rows, plus two interfaces for things only the server can do (database access and who's signed in). The domain needs the row shapes to map them. The interfaces keep EF Core and `HttpContext` out of the browser bundle.

```csharp
namespace zCRM.Domain.Persistence;

public class VisitRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public DateTime VisitDate { get; set; }
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

/// <summary>Visit storage. Implemented on the server over EF Core.</summary>
public interface IVisitDb
{
    Task<VisitRow?> FindAsync(Guid id);
    Task<VisitRow?> FindForDayAsync(Guid contactId, DateTime visitDate);
    void Add(VisitRow row);
    void Remove(VisitRow row);
    Task SaveChangesAsync();
}

/// <summary>The signed-in salesperson. Server-only.</summary>
public interface ICurrentSalesperson
{
    string Name { get; }
}
```

---

## Server (`zCRM.Server`)

### `Data/ZcrmDbContext.cs`
If you already have this DbContext, these three settings are the ones the domain code depends on:
- **`ValueGeneratedNever()` on both keys.** The entities set their own Guid ids. Without this, EF treats a new action item it finds through the visit's collection as an existing row, and the save fails with a concurrency error.
- **Unique index on (ContactId, VisitDate).** This is what actually guarantees one visit per contact per day.
- **Cascade delete.** The list removes a deleted item from the collection, and EF then deletes that row.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Persistence;

namespace zCRM.Server.Data;

public sealed class ZcrmDbContext(DbContextOptions<ZcrmDbContext> options) : DbContext(options)
{
    public DbSet<VisitRow> Visits => Set<VisitRow>();
    public DbSet<ActionItemRow> ActionItems => Set<ActionItemRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitRow>(visit =>
        {
            visit.ToTable("Visit");
            visit.HasKey(v => v.Id);
            visit.Property(v => v.Id).ValueGeneratedNever();
            visit.Property(v => v.VisitDate).HasColumnType("date");
            visit.Property(v => v.SalespersonName).HasMaxLength(200);
            visit.Property(v => v.Notes).HasMaxLength(4000);
            visit.HasIndex(v => new { v.ContactId, v.VisitDate }).IsUnique();
            visit.HasMany(v => v.ActionItems)
                 .WithOne()
                 .HasForeignKey(a => a.VisitId)
                 .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionItemRow>(item =>
        {
            item.ToTable("ActionItem");
            item.HasKey(a => a.Id);
            item.Property(a => a.Id).ValueGeneratedNever();
            item.Property(a => a.Description).HasMaxLength(500);
        });
    }
}
```

### `Data/VisitDb.cs`
The EF Core side of `IVisitDb`. It always loads a visit together with its action items, because the list's save needs the existing rows.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Persistence;

namespace zCRM.Server.Data;

internal sealed class VisitDb(ZcrmDbContext db) : IVisitDb
{
    public Task<VisitRow?> FindAsync(Guid id) =>
        db.Visits.Include(v => v.ActionItems).SingleOrDefaultAsync(v => v.Id == id);

    public Task<VisitRow?> FindForDayAsync(Guid contactId, DateTime visitDate) =>
        db.Visits.Include(v => v.ActionItems)
                 .SingleOrDefaultAsync(v => v.ContactId == contactId && v.VisitDate == visitDate);

    public void Add(VisitRow row) => db.Visits.Add(row);

    public void Remove(VisitRow row) => db.Visits.Remove(row);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
```

### `Identity/HttpCurrentSalesperson.cs`

```csharp
using zCRM.Domain.Persistence;

namespace zCRM.Server.Identity;

internal sealed class HttpCurrentSalesperson(IHttpContextAccessor accessor) : ICurrentSalesperson
{
    public string Name =>
        accessor.HttpContext?.User.Identity?.Name
        ?? throw new InvalidOperationException("No signed-in salesperson.");
}
```

### `Program.cs`
This uses `AddNeatooServices(Server)` plus `UseNeatoo()`, and deliberately no `AddNeatooAspNetCore`. Registering that after `AddNeatooServices` breaks every remote save (RemoteFactory#117). The cookie authentication is a placeholder for zCRM's real sign-in.

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;
using zCRM.Domain.Visits;
using zCRM.Server.Data;
using zCRM.Server.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IVisit).Assembly);

builder.Services.AddDbContext<ZcrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<IVisitDb, VisitDb>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentSalesperson, HttpCurrentSalesperson>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
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

## Client (`zCRM.Client`, Blazor WASM)

### `Program.cs`
The client registers only what client-side code uses. `TimeProvider` is here because `OpenToday` runs in the browser.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.Visits;
using zCRM.Domain.Visits;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IVisit).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMudServices();
builder.Services.AddTransient<TodayVisitViewModel>();

await builder.Build().RunAsync();
```

### `Visits/TodayVisitViewModel.cs`
The view model holds the live visit and passes the user's clicks to the domain. It makes no decisions of its own: the Save button is enabled by the visit's own `IsSavable`. `IsSaving` exists because `Save()` doesn't mark the visit busy, so a double-click would otherwise send two inserts (Neatoo#103).

```csharp
using zCRM.Domain.Visits;

namespace zCRM.Client.Visits;

internal sealed class TodayVisitViewModel
{
    private readonly IVisitFactory _visitFactory;
    private readonly IActionItemFactory _itemFactory;

    public TodayVisitViewModel(IVisitFactory visitFactory, IActionItemFactory itemFactory)
    {
        _visitFactory = visitFactory;
        _itemFactory = itemFactory;
    }

    /// <summary>Today's visit for the contact. Null while opening.</summary>
    public IVisit? Visit { get; private set; }

    public bool IsSaving { get; private set; }

    public bool CanSave => !IsSaving && (Visit?.IsSavable ?? false);

    public async Task Open(Guid contactId)
    {
        Visit = null;
        Visit = await _visitFactory.OpenToday(contactId);
    }

    public async Task AddActionItem()
    {
        if (Visit?.ActionItems is null)
        {
            return;
        }

        Visit.ActionItems.Add(await _itemFactory.Create());
    }

    public void RemoveActionItem(IActionItem item)
    {
        Visit?.ActionItems?.Remove(item);
    }

    public async Task Save()
    {
        if (Visit is null)
        {
            return;
        }

        IsSaving = true;
        try
        {
            await Visit.WaitForTasks();

            if (!Visit.IsSavable)
            {
                return;
            }

            Visit = (IVisit)await Visit.Save();  // Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

### `Visits/ContactTodayTab.razor`
The tab's content. MudTabs only renders the active panel by default, so the visit loads when the salesperson opens the tab. `@key` on the visit rebuilds the inputs after a save, because `Save()` returns a new instance. The tab subscribes to the visit's change event because it displays the Save button's state.

```razor
@using System.ComponentModel
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using zCRM.Domain.Visits
@implements IDisposable
@inject TodayVisitViewModel Vm

@if (Vm.Visit is null)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    <div @key="Vm.Visit">
        <MudText Typo="Typo.h6">@Vm.Visit.VisitDate.ToString("D")</MudText>
        <MudText Typo="Typo.body2" Class="mb-3">Salesperson: @Vm.Visit.SalespersonName</MudText>

        <NeatooValidationSummary Entity="@Vm.Visit" ShowHeader="false" Dense="true" Class="mb-3" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Visit[nameof(IVisit.Notes)]"
                            Variant="Variant.Outlined"
                            Lines="5"
                            Sizing="InputSizing.Auto"
                            MaxLines="20"
                            UserAttributes="@(new() { ["spellcheck"] = "true" })"
                            Class="mb-4" />

        <MudText Typo="Typo.subtitle1" Class="mb-2">Action items</MudText>

        @foreach (var item in Vm.Visit.ActionItems!)
        {
            <MudGrid @key="item" Spacing="2" Class="mb-1">
                <MudItem xs="3" sm="2" Class="d-flex align-center">
                    <MudNeatooCheckBox T="bool" EntityProperty="@item[nameof(IActionItem.IsDone)]" Label="Done" />
                </MudItem>
                <MudItem xs="7" sm="9">
                    <MudNeatooTextField T="string" EntityProperty="@item[nameof(IActionItem.Description)]" />
                </MudItem>
                <MudItem xs="2" sm="1" Class="d-flex align-center">
                    <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => Vm.RemoveActionItem(item))" />
                </MudItem>
            </MudGrid>
        }

        <MudButton Variant="Variant.Outlined" OnClick="Vm.AddActionItem" Class="my-3 mr-2">
            Add action item
        </MudButton>

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   Disabled="@(!Vm.CanSave)"
                   OnClick="SaveAsync"
                   Class="my-3">
            Save
        </MudButton>
    </div>
}

@code {
    [Parameter, EditorRequired]
    public Guid ContactId { get; set; }

    private Guid _openedFor;
    private IVisit? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (ContactId == _openedFor)
        {
            return;
        }

        _openedFor = ContactId;
        await Vm.Open(ContactId);
        Resubscribe();
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnVisitChanged;
        }

        _subscribed = Vm.Visit;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnVisitChanged;
        }
    }

    private Task OnVisitChanged(NeatooPropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnVisitChanged;
        }
    }
}
```

The contact page hosts it like this:

```razor
<MudTabPanel Text="Today">
    <ContactTodayTab ContactId="@ContactId" />
</MudTabPanel>
```

---

## Assumptions and gaps

- **One visit per contact per day,** whoever the salesperson is, as you described it. To make it one per salesperson per contact per day, add the salesperson to `FindForDayAsync` and to the unique index.
- **Two devices opening the same contact at once** can both start a new visit. The unique index rejects the second save with a `DbUpdateException`, which the tab doesn't handle yet. A snackbar telling the user to reload would be the minimum.
- **Action items have no stored order,** so they can come back in a different order after a reload. If that matters, add a position column to `ActionItemRow`.
- **The salesperson name is `User.Identity.Name`,** which may be a login or email rather than a display name. Change `HttpCurrentSalesperson` to read whichever claim zCRM uses.
- **The local date relies on the browser's time zone,** which Blazor WASM supports unless the app is built with `InvariantTimezone`.