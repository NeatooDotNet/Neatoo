Everything I need is confirmed. Here is the complete code.

**One design point first.** Neatoo's rule is that an operation which may either load or create is a `[Remote, Execute]` on the root's class factory, not a `[Fetch]`. So the Today tab calls one factory method, `StartToday`, which runs on the server, tries the `[Fetch]` for that contact and date, and falls back to `[Create]` with the signed-in salesperson's name. The client passes its own calendar date, because "today" is the salesperson's local day, and the server supplies the name, because identity is never client input.

## Domain project (zCRM.Domain, compiled into both tiers)

**Rows and server contracts.** The rows are plain POCOs here, with no EF reference, because the domain's `[Fetch]`, `[Insert]` and `[Update]` take rows as parameters and the domain assembly also ships to the browser. The DbContext that maps them stays on the server.

```csharp
// zCRM.Domain/Persistence/Rows.cs
namespace zCRM.Domain.Persistence;

public class VisitRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public DateOnly VisitDate { get; set; }
    public string SalespersonName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<ActionItemRow> ActionItems { get; set; } = new();
}

public class ActionItemRow
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }          // non-nullable: removing a row from VisitRow.ActionItems deletes it
    public string Description { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}
```

```csharp
// zCRM.Domain/Persistence/IVisitRepository.cs
namespace zCRM.Domain.Persistence;

/// <summary>Repository for Visit aggregates. Implemented on the server; resolved only as a method [Service].</summary>
public interface IVisitRepository
{
    Task<VisitRow?> GetForContactOnDateAsync(Guid contactId, DateOnly visitDate);
    Task<VisitRow?> GetAsync(Guid id);
    void Add(VisitRow row);
    Task SaveChangesAsync();
}

/// <summary>The signed-in salesperson. Implemented on the server from the request's identity.</summary>
public interface ICurrentUser
{
    string DisplayName { get; }
}
```

**Aggregate interfaces.** Every reference uses an interface so a child can never be saved on its own: `IVisit` extends `IEntityRoot` and gets `Save()` and `IsSavable`; `IActionItem` extends `IEntityBase` and has neither.

```csharp
// zCRM.Domain/Visits/IVisitInterfaces.cs
using Neatoo;

namespace zCRM.Domain.Visits;

/// <summary>Visit aggregate root: one salesperson's visit to one contact on one day.</summary>
public interface IVisit : IEntityRoot
{
    Guid Id { get; }
    Guid ContactId { get; }
    DateOnly VisitDate { get; }
    string? SalespersonName { get; }
    string? Notes { get; set; }
    IActionItemList? ActionItems { get; }
}

/// <summary>Child entity: one thing to do that came out of the visit.</summary>
public interface IActionItem : IEntityBase
{
    Guid Id { get; }
    string? Description { get; set; }
    bool IsDone { get; set; }
}

public interface IActionItemList : IEntityListBase<IActionItem>
{
}
```

**The Visit root.** `StartToday` is the single entry point the tab uses. `Fetch` is `[Remote]` because the client may call it through the factory; `Create` is plain because creating needs nothing from the server when its inputs are supplied. `Insert` and `Update` re-run every rule and throw on an invalid aggregate, which is the server's guard against a caller that skipped `IsSavable`.

```csharp
// zCRM.Domain/Visits/Visit.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class Visit : EntityBase<Visit>, IVisit
{
    public partial Guid Id { get; set; }
    public partial Guid ContactId { get; set; }

    [DisplayName("Visit date")]
    public partial DateOnly VisitDate { get; set; }

    [Required(ErrorMessage = "Salesperson is required")]
    [DisplayName("Salesperson")]
    public partial string? SalespersonName { get; set; }

    [DisplayName("Notes")]
    public partial string? Notes { get; set; }

    public partial IActionItemList? ActionItems { get; set; }

    public Visit(IEntityBaseServices<Visit> services) : base(services)
    {
    }

    /// <summary>
    /// Today's visit for a contact: loaded if it exists, otherwise started new with
    /// today's date and the signed-in salesperson's name. Runs on the server; the
    /// factory calls inside it are local there.
    /// </summary>
    [Remote, Execute]
    public static async Task<IVisit> StartToday(
        Guid contactId,
        DateOnly visitDate,
        [Service] IVisitFactory factory,
        [Service] ICurrentUser currentUser)
    {
        var existing = await factory.Fetch(contactId, visitDate);
        if (existing is not null)
        {
            return existing;
        }

        return await factory.Create(contactId, visitDate, currentUser.DisplayName);
    }

    // Paused during the operation: these assignments are the baseline, not user work.
    // After Create: IsNew = true, IsModified = false, savable because IsNew.
    [Create]
    public void Create(Guid contactId, DateOnly visitDate, string salespersonName,
        [Service] IActionItemListFactory listFactory)
    {
        ContactId = contactId;
        VisitDate = visitDate;
        SalespersonName = salespersonName;
        ActionItems = listFactory.Create();
    }

    // Returns false when there is no visit for that day; the factory then returns null.
    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid contactId, DateOnly visitDate,
        [Service] IVisitRepository repository,
        [Service] IActionItemListFactory listFactory)
    {
        var row = await repository.GetForContactOnDateAsync(contactId, visitDate);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        ContactId = row.ContactId;
        VisitDate = row.VisitDate;
        SalespersonName = row.SalespersonName;
        Notes = row.Notes;
        ActionItems = listFactory.Fetch(row.ActionItems);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] IVisitRepository repository,
        [Service] IActionItemListFactory listFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new VisitRow();
        MapTo(row);
        repository.Add(row);

        listFactory.Save(ActionItems!, row.ActionItems);

        await repository.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] IVisitRepository repository,
        [Service] IActionItemListFactory listFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await repository.GetAsync(Id)
            ?? throw new KeyNotFoundException($"Visit {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        listFactory.Save(ActionItems!, row.ActionItems);

        await repository.SaveChangesAsync();
    }

    private void MapTo(VisitRow row)
    {
        row.Id = Id;
        row.ContactId = ContactId;
        row.VisitDate = VisitDate;
        row.SalespersonName = SalespersonName!;
        row.Notes = Notes;
    }
}
```

**The action item list.** Its operations are `internal` and never `[Remote]`: the root calls them on the server inside its own operation. `Update` walks live items plus `DeletedList` and hands each changed child its own row, so each child's factory operation marks that child clean afterward.

```csharp
// zCRM.Domain/Visits/ActionItemList.cs
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

    // The list is paused by its own operation, so these Adds are baseline, not modifications.
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

**The action item.** `Create` runs its rules so a freshly added blank row shows "Description is required" at once and holds the Save button until it is filled. The persistence operations take a row only the list's `Update` can supply.

```csharp
// zCRM.Domain/Visits/ActionItem.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain.Persistence;

namespace zCRM.Domain.Visits;

[Factory]
internal partial class ActionItem : EntityBase<ActionItem>, IActionItem
{
    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [DisplayName("Action item")]
    public partial string? Description { get; set; }

    [DisplayName("Done")]
    public partial bool IsDone { get; set; }

    public ActionItem(IEntityBaseServices<ActionItem> services) : base(services)
    {
    }

    // A blank item handed to a person: validate it now so the required message shows immediately.
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

## Server project (zCRM.Server)

The DbContext and repository live here because they are the only EF code and only the server has a database. The repository loads action items with the visit so the list's `Update` works against a tracked collection, and the non-nullable `VisitId` makes EF delete a row removed from that collection.

```csharp
// zCRM.Server/Data/CrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Persistence;

namespace zCRM.Server.Data;

public class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

    public DbSet<VisitRow> Visits => Set<VisitRow>();
    public DbSet<ActionItemRow> ActionItems => Set<ActionItemRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitRow>(b =>
        {
            b.ToTable("Visits");
            b.HasKey(v => v.Id);
            b.HasIndex(v => new { v.ContactId, v.VisitDate }).IsUnique();
            b.HasMany(v => v.ActionItems)
             .WithOne()
             .HasForeignKey(a => a.VisitId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActionItemRow>(b =>
        {
            b.ToTable("ActionItems");
            b.HasKey(a => a.Id);
        });
    }
}
```

```csharp
// zCRM.Server/Data/VisitRepository.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Persistence;

namespace zCRM.Server.Data;

public class VisitRepository : IVisitRepository
{
    private readonly CrmDbContext _db;

    public VisitRepository(CrmDbContext db)
    {
        _db = db;
    }

    public Task<VisitRow?> GetForContactOnDateAsync(Guid contactId, DateOnly visitDate)
        => _db.Visits
              .Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.ContactId == contactId && v.VisitDate == visitDate);

    public Task<VisitRow?> GetAsync(Guid id)
        => _db.Visits
              .Include(v => v.ActionItems)
              .SingleOrDefaultAsync(v => v.Id == id);

    public void Add(VisitRow row) => _db.Visits.Add(row);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
```

```csharp
// zCRM.Server/Security/HttpCurrentUser.cs
using zCRM.Domain.Persistence;

namespace zCRM.Server.Security;

/// <summary>The salesperson's name from the authenticated request. Server-only.</summary>
public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string DisplayName
        => _accessor.HttpContext?.User?.Identity?.Name
           ?? throw new InvalidOperationException("No authenticated user on this request.");
}
```

Server registration. Neatoo in Server mode registers the generated factories and the endpoint `UseNeatoo` maps. The order matters: do not add `AddNeatooAspNetCore` after this line, because that breaks every remote Save (RemoteFactory issue 117).

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using zCRM.Domain.Persistence;
using zCRM.Domain.Visits;
using zCRM.Server.Data;
using zCRM.Server.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Neatoo on the server: Server mode, the domain assembly.
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IVisit).Assembly);

builder.Services.AddAuthentication(/* your scheme */);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.UseNeatoo();          // maps the /api/neatoo endpoint the client's factories call

app.MapFallbackToFile("index.html");
app.Run();
```

## Client project (zCRM.Client, Blazor WebAssembly)

**The ViewModel** holds the live aggregate, turns gestures into calls on it, and saves. It decides nothing: `CanSave` reads `IsSavable`, and after `Save` it reassigns because `Save` returns a new instance. It is a plain class, so it is unit-testable without a browser.

```csharp
// zCRM.Client/ViewModels/ContactTodayViewModel.cs
using zCRM.Domain.Visits;

namespace zCRM.Client.ViewModels;

/// <summary>ViewModel for a contact's Today tab: today's Visit for that contact.</summary>
public sealed class ContactTodayViewModel
{
    private readonly IVisitFactory _visitFactory;
    private readonly IActionItemFactory _itemFactory;

    public ContactTodayViewModel(IVisitFactory visitFactory, IActionItemFactory itemFactory)
    {
        _visitFactory = visitFactory;
        _itemFactory = itemFactory;
    }

    /// <summary>The aggregate the tab binds to. Null until loaded.</summary>
    public IVisit? Visit { get; private set; }

    public bool IsSaving { get; private set; }

    /// <summary>Loads today's visit or starts a new one. One server round-trip either way.</summary>
    public async Task Load(Guid contactId)
    {
        Visit = await _visitFactory.StartToday(contactId, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task AddActionItem()
    {
        if (Visit?.ActionItems is null)
        {
            return;
        }

        Visit.ActionItems.Add(await _itemFactory.Create());
    }

    // A persisted item moves to DeletedList; a new one is simply discarded.
    public void RemoveActionItem(IActionItem item)
    {
        Visit?.ActionItems?.Remove(item);
    }

    public bool CanSave => !IsSaving && (Visit?.IsSavable ?? false);

    public async Task Save()
    {
        if (Visit is null)
        {
            return;
        }

        await Visit.WaitForTasks();

        if (!Visit.IsSavable)
        {
            return;   // invalid or unchanged; the summary already shows why
        }

        IsSaving = true;   // Save() does not mark the entity busy, so guard the double-click here
        try
        {
            Visit = (IVisit)await Visit.Save();
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

**The Today tab component.** It is a component rather than a routed page because it sits inside the contact page's tab strip and receives the contact's id as a parameter. Every input binds through a MudNeatoo component to the entity's property wrapper, so labels, messages, busy and read-only state come from the domain. The date and salesperson are shown as text because the operation set them and the person does not edit them.

```razor
@* zCRM.Client/Components/Contacts/ContactTodayTab.razor *@
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using zCRM.Client.ViewModels
@using zCRM.Domain.Visits
@implements IDisposable
@inject ContactTodayViewModel Vm

@if (Vm.Visit is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    @* @key: Save returns a new instance; keying rebuilds the bound fields on the new one. *@
    <div @key="Vm.Visit">
        <MudText Typo="Typo.subtitle1" Class="mb-1">
            Visit on @Vm.Visit.VisitDate.ToString("D") by @Vm.Visit.SalespersonName
        </MudText>

        <NeatooValidationSummary Entity="@Vm.Visit" ShowHeader="false" Dense="true" Class="mb-3" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Visit[nameof(IVisit.Notes)]"
                            Variant="Variant.Outlined"
                            Lines="4"
                            Sizing="InputSizing.Auto"
                            MaxLines="20"
                            UserAttributes="@(new() { ["spellcheck"] = "true" })"
                            Class="mb-4" />

        <MudText Typo="Typo.h6" Class="mb-2">Action items</MudText>

        @if (Vm.Visit.ActionItems is not null)
        {
            @foreach (var item in Vm.Visit.ActionItems)
            {
                <MudGrid Class="mb-1" Spacing="2" @key="item">
                    <MudItem xs="2" sm="1" Class="d-flex align-center">
                        <MudNeatooCheckBox T="bool" EntityProperty="@item[nameof(IActionItem.IsDone)]" />
                    </MudItem>
                    <MudItem xs="8" sm="10">
                        <MudNeatooTextField T="string"
                                            EntityProperty="@item[nameof(IActionItem.Description)]"
                                            Variant="Variant.Outlined"
                                            Margin="Margin.Dense" />
                    </MudItem>
                    <MudItem xs="2" sm="1" Class="d-flex align-center">
                        <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                       OnClick="@(() => Vm.RemoveActionItem(item))" />
                    </MudItem>
                </MudGrid>
            }
        }

        <MudButton Variant="Variant.Outlined" OnClick="AddActionItemAsync" Class="mb-4">
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

    private Guid _loadedContactId;
    private IVisit? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (ContactId == _loadedContactId)
        {
            return;
        }

        _loadedContactId = ContactId;
        await Vm.Load(ContactId);
        Resubscribe();
    }

    // The fields subscribe to their own property wrappers; the Save button and the
    // list read the entity, so the component re-renders on the graph-wide event.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }

        _subscribed = Vm.Visit;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnEntityChanged;
        }
    }

    private async Task AddActionItemAsync()
    {
        await Vm.AddActionItem();
        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    private Task OnEntityChanged(NeatooPropertyChangedEventArgs e)
        => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }
    }
}
```

**Client registration.** Neatoo in Remote mode generates proxies for the same domain assembly, and the keyed HttpClient is the transport those proxies use. The ViewModel is transient so each tab instance gets its own.

```csharp
// zCRM.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain.Visits;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Neatoo on the client: Remote mode, the domain assembly. [Remote] operations cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IVisit).Assembly);

// The HttpClient RemoteFactory's proxies call through. Hosted WASM: same origin as the server.
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactTodayViewModel>();

await builder.Build().RunAsync();
```

**Assumptions worth knowing.** The client's calendar date defines "today". The server trusts that date but takes the salesperson's name from the authenticated identity, so authentication must be configured on the server before this tab works. A brand-new untouched visit is savable because `IsNew` admits it, which lets a salesperson record a visit with no notes. Deleting a visit is out of scope for this tab, so the root has no `[Delete]`. The unique index on contact and date is the database's guard against two visits for the same day if two tabs race.
