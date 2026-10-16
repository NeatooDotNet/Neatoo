I haven't compiled any of this. The design follows the Neatoo, RemoteFactory and MudNeatoo skill references, but the `using` lines for Neatoo's extension methods may need adjusting.

## The design

| Piece | Kind | Where it lives |
|---|---|---|
| `IContactSearchCriteria` / `ContactSearchCriteria` | Neatoo input model (`ValidateBase`) | Domain assembly, used by both tiers |
| `IContactSearchResults` / `ContactSearchResults` + `ContactSearchRow` | RemoteFactory read model: a plain `[Factory]` class with `[Fetch]` only | Domain assembly; its `[Fetch]` runs on the server |
| `Contact` / `ZCrmDbContext` | EF Core | `zCRM.Data`, which only the server uses at runtime |
| `ContactSearchViewModel` | Plain class | Client |
| `ContactSearch.razor` | Page | Client |

- **Criteria is an input model.** It is something a person edits, with rules, and nothing ever saves it. So it is a `ValidateBase`, not an `EntityBase`, a DTO, or fields on the page.
- **The rules own both screen behaviours.** The "minimum 2 characters" message is a validation rule on `NameFragment`. Whether Search is enabled is a fact, `CanSearch`, computed by a rule. The page only binds to them and decides nothing.
- **Results are a read model.** The screen only shows server data, so there's no base class, change tracking or DTO controller. The EF query sits in its `[Remote, Fetch]`.

---

## 1. `zCRM.Data` — the EF Core persistence layer

The domain's `[Fetch]` needs the DbContext type, so it lives in its own project. The domain references it with `PrivateAssets="all"`, and trimming removes it from the WASM bundle (see section 6).

**`zCRM.Data/Contact.cs`**
```csharp
namespace zCRM.Data;

/// <summary>Row in the Contacts table.</summary>
public class Contact
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? LastContactedOn { get; set; }
}
```

**`zCRM.Data/ZCrmDbContext.cs`**
```csharp
using Microsoft.EntityFrameworkCore;

namespace zCRM.Data;

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(e =>
        {
            e.ToTable("Contacts");
            e.Property(c => c.FirstName).HasMaxLength(100);
            e.Property(c => c.LastName).HasMaxLength(100);
            e.Property(c => c.City).HasMaxLength(100);
            e.HasIndex(c => c.City);
            e.HasIndex(c => new { c.LastName, c.FirstName });
        });
    }
}
```

## 2. Domain — the search criteria (input model)

It goes in the domain assembly because the browser runs the rules as the user types, and the same class exists on the server. The interface is public and the concrete class is internal, so everything outside the domain references the interface.

**`zCRM.Domain/Contacts/IContactSearchCriteria.cs`**
```csharp
using Neatoo;

namespace zCRM.Domain.Contacts;

/// <summary>
/// Input model for the contact search screen.
/// </summary>
public interface IContactSearchCriteria : IValidateBase
{
    string? NameFragment { get; set; }

    string? City { get; set; }

    bool ActiveOnly { get; set; }

    /// <summary>At least one criterion is entered and every criterion is valid.</summary>
    bool CanSearch { get; }
}
```

**`zCRM.Domain/Contacts/ContactSearchCriteria.cs`**
```csharp
using System.Linq.Expressions;
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

/// <summary>
/// Input model for the contact search screen. Validity and CanSearch are rules,
/// so the page binds them and decides nothing.
/// </summary>
[Factory]
internal partial class ContactSearchCriteria : ValidateBase<ContactSearchCriteria>, IContactSearchCriteria
{
    public const int MinimumNameFragmentLength = 2;

    public partial string? NameFragment { get; set; }

    public partial string? City { get; set; }

    public partial bool ActiveOnly { get; set; }

    public partial bool CanSearch { get; private set; }

    public ContactSearchCriteria(IValidateBaseServices<ContactSearchCriteria> services) : base(services)
    {
        // A name fragment is optional, but one that is entered must be long enough.
        RuleManager.AddValidation(
            t => !string.IsNullOrWhiteSpace(t.NameFragment)
                 && t.NameFragment.Trim().Length < MinimumNameFragmentLength
                ? $"Enter at least {MinimumNameFragmentLength} characters of the name."
                : string.Empty,
            t => t.NameFragment);

        // Admission for Search: at least one criterion, and nothing invalid.
        // IsValid is a trigger, so CanSearch follows when the name message
        // appears or clears.
        RuleManager.AddActionAsync(
            async t => await t.SetCanSearch(
                (!string.IsNullOrWhiteSpace(t.NameFragment)
                 || !string.IsNullOrWhiteSpace(t.City)
                 || t.ActiveOnly)
                && t.IsValid),
            new Expression<Func<ContactSearchCriteria, object?>>[]
            {
                t => t.NameFragment, t => t.City, t => t.ActiveOnly, t => t.IsValid
            });
    }

    // Local: an empty form needs nothing from the server. CanSearch starts
    // false, which is the correct baseline, so no rules are run here.
    [Create]
    public void Create()
    {
    }
}
```

## 3. Domain — the results (read model) and the server-side query

The query sits in the read model's `[Fetch]`, because that operation is where the client crosses to the server.

- **`[Remote]`:** the client's call crosses to the server.
- **`internal`:** the body, and EF Core with it, gets the server-only guard and is trimmed from the client.
- **No DTO or controller.** The client receives this same class back.

**`zCRM.Domain/Contacts/IContactSearchResults.cs`**
```csharp
namespace zCRM.Domain.Contacts;

/// <summary>One row of the contact search results.</summary>
public sealed record ContactSearchRow(Guid Id, string Name, string? City, DateOnly? LastContactedOn);

/// <summary>
/// Read model for the contact search screen.
/// </summary>
public interface IContactSearchResults
{
    IReadOnlyList<ContactSearchRow> Contacts { get; }
}
```

**`zCRM.Domain/Contacts/ContactSearchResults.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using zCRM.Data;

namespace zCRM.Domain.Contacts;

/// <summary>
/// Read model for the contact search screen: fetched, never edited or saved.
/// </summary>
[Factory]
internal partial class ContactSearchResults : IContactSearchResults
{
    // Public setter: a plain [Factory] class serializes only what it can set
    // on the other side.
    public IReadOnlyList<ContactSearchRow> Contacts { get; set; } = [];

    [Remote, Fetch]
    internal async Task Fetch(
        string? nameFragment,
        string? city,
        bool activeOnly,
        [Service] ZCrmDbContext db,
        CancellationToken cancellationToken)
    {
        var name = nameFragment?.Trim();
        var cityName = city?.Trim();

        // The criteria's rules gate the Search button. Reaching here without a
        // criterion means client and server disagree: an application failure,
        // not a validation message. It also stops an unfiltered table scan.
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(cityName) && !activeOnly)
        {
            throw new InvalidOperationException("A contact search needs at least one criterion.");
        }

        if (name is { Length: < ContactSearchCriteria.MinimumNameFragmentLength })
        {
            throw new InvalidOperationException(
                $"A name fragment needs at least {ContactSearchCriteria.MinimumNameFragmentLength} characters.");
        }

        IQueryable<Contact> query = db.Contacts.AsNoTracking();

        if (!string.IsNullOrEmpty(name))
        {
            // Matches "smi", "john" and "john sm".
            query = query.Where(c => (c.FirstName + " " + c.LastName).Contains(name));
        }

        if (!string.IsNullOrEmpty(cityName))
        {
            query = query.Where(c => c.City == cityName);
        }

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        Contacts = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new ContactSearchRow(
                c.Id,
                c.FirstName + " " + c.LastName,
                c.City,
                c.LastContactedOn))
            .ToListAsync(cancellationToken);
    }
}
```

## 4. Client — the ViewModel

The ViewModel holds the criteria and the latest results, and forwards the Search gesture. It asks the criteria for `CanSearch` rather than checking anything itself. `IsSearching` is screen state: it disables the button while a request is in flight.

**`zCRM.Client/ViewModels/ContactSearchViewModel.cs`**
```csharp
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

/// <summary>
/// ViewModel for the contact search screen: holds the criteria and the latest results.
/// </summary>
internal sealed class ContactSearchViewModel
{
    private readonly IContactSearchResultsFactory _resultsFactory;

    public ContactSearchViewModel(
        IContactSearchCriteriaFactory criteriaFactory,
        IContactSearchResultsFactory resultsFactory)
    {
        _resultsFactory = resultsFactory;
        Criteria = criteriaFactory.Create();
    }

    /// <summary>The input model the page binds to.</summary>
    public IContactSearchCriteria Criteria { get; }

    /// <summary>Null until the first search.</summary>
    public IContactSearchResults? Results { get; private set; }

    /// <summary>A search is in flight.</summary>
    public bool IsSearching { get; private set; }

    /// <summary>The Search button binds to this: the criteria's answer, and not while a search runs.</summary>
    public bool CanSearch => Criteria.CanSearch && !IsSearching;

    public async Task Search()
    {
        await Criteria.WaitForTasks();

        if (!CanSearch)
        {
            return;
        }

        IsSearching = true;
        try
        {
            Results = await _resultsFactory.Fetch(Criteria.NameFragment, Criteria.City, Criteria.ActiveOnly);
        }
        finally
        {
            IsSearching = false;
        }
    }
}
```

## 5. Client — the page

The criteria fields use plain MudBlazor controls bound to Neatoo's property objects, not `MudNeatooTextField`. MudNeatoo components only accept an `EntityBase` property, and the criteria is a `ValidateBase` input model (Neatoo#95).

Each property's messages feed MudBlazor's `ErrorText`, which shows under the field. `Immediate="true"` makes the button and the message react as the user types. That costs nothing here, because the rules run in the browser with no server call.

The page doesn't subscribe to change events. Every value change comes through an `EventCallback` on this page, so Blazor re-renders it once the awaited `SetValue` (and the rules) finish.

**`zCRM.Client/Pages/ContactSearch.razor`**
```razor
@page "/contacts/search"
@using Neatoo
@using zCRM.Client.ViewModels
@using zCRM.Domain.Contacts
@inject ContactSearchViewModel Vm

<PageTitle>Contact search</PageTitle>

<MudText Typo="Typo.h5" Class="mb-4">Contact search</MudText>

@{
    var name = Vm.Criteria[nameof(IContactSearchCriteria.NameFragment)];
    var city = Vm.Criteria[nameof(IContactSearchCriteria.City)];
    var activeOnly = Vm.Criteria[nameof(IContactSearchCriteria.ActiveOnly)];
}

<MudGrid Spacing="2" Class="mb-2">
    <MudItem xs="12" sm="5">
        <MudTextField T="string"
                      Label="Name contains"
                      Variant="Variant.Outlined"
                      Immediate="true"
                      Clearable="true"
                      Value="@Vm.Criteria.NameFragment"
                      ValueChanged="@(async (string v) => await name.SetValue(v))"
                      Error="@(!name.IsValid)"
                      ErrorText="@Messages(name)" />
    </MudItem>
    <MudItem xs="12" sm="4">
        <MudTextField T="string"
                      Label="City"
                      Variant="Variant.Outlined"
                      Immediate="true"
                      Clearable="true"
                      Value="@Vm.Criteria.City"
                      ValueChanged="@(async (string v) => await city.SetValue(v))"
                      Error="@(!city.IsValid)"
                      ErrorText="@Messages(city)" />
    </MudItem>
    <MudItem xs="12" sm="3" Class="d-flex align-center">
        <MudCheckBox T="bool"
                     Label="Active only"
                     Value="@Vm.Criteria.ActiveOnly"
                     ValueChanged="@(async (bool v) => await activeOnly.SetValue(v))" />
    </MudItem>
</MudGrid>

<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           StartIcon="@Icons.Material.Filled.Search"
           Disabled="@(!Vm.CanSearch)"
           OnClick="Vm.Search"
           Class="mb-4">
    Search
</MudButton>

@if (Vm.IsSearching)
{
    <MudProgressLinear Indeterminate="true" Class="mb-2" />
}

@if (Vm.Results is not null)
{
    <MudTable T="ContactSearchRow" Items="@Vm.Results.Contacts" Dense="true" Hover="true" Elevation="0">
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>City</MudTh>
            <MudTh>Last contacted</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">@context.Name</MudTd>
            <MudTd DataLabel="City">@context.City</MudTd>
            <MudTd DataLabel="Last contacted">@context.LastContactedOn?.ToShortDateString()</MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudText>No contacts match.</MudText>
        </NoRecordsContent>
    </MudTable>
}

@code {
    private static string Messages(IValidateProperty property) =>
        string.Join(" ", property.PropertyMessages.Select(m => m.Message));
}
```

## 6. Composition on each tier

You need these for the code above to run. Each tier registers Neatoo in its own mode: Server mode on the server, Remote mode in the browser.

Don't also call `AddNeatooAspNetCore` on the server. Registering it after `AddNeatooServices(NeatooFactory.Server, ...)` breaks every remote Save (RemoteFactory#117).

**`zCRM.Server/Program.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Data;
using zCRM.Domain.Contacts;

var builder = WebApplication.CreateBuilder(args);

// Server mode: generated factories run the operations here; also registers what UseNeatoo() resolves.
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContactSearchCriteria).Assembly);

// What the [Service] parameters resolve on the server.
builder.Services.AddDbContext<ZCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// The single RemoteFactory endpoint; no controllers.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

**`zCRM.Client/Program.cs`**
```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Remote mode: [Remote] operations cross to the server.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContactSearchCriteria).Assembly);

// The HttpClient the generated proxies call through: the server hosting this client.
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactSearchViewModel>();

await builder.Build().RunAsync();
```

**Project-file lines that keep EF Core out of the browser bundle:**
```xml
<!-- zCRM.Domain.csproj -->
<PropertyGroup><IsTrimmable>true</IsTrimmable></PropertyGroup>
<ItemGroup>
  <PackageReference Include="Neatoo" />
  <PackageReference Include="Neatoo.RemoteFactory" />  <!-- direct, or the generator does not run -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" PrivateAssets="all" />
  <ProjectReference Include="..\zCRM.Data\zCRM.Data.csproj" PrivateAssets="all" />
</ItemGroup>

<!-- zCRM.Client.csproj -->
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />
</ItemGroup>
```

The server project references `zCRM.Domain` and `zCRM.Data` directly, plus `Neatoo.RemoteFactory.AspNetCore` and the EF SQL Server provider.

## Choices the brief left open

- **Checking "Active only" counts as a criterion**, so it alone enables Search and lists all active contacts. If you meant only name or city to count, remove `|| t.ActiveOnly` from the `CanSearch` rule.
- **City is an exact match** and the name is a "contains" match. Whether either ignores case depends on your database collation.
- **The name fragment matches across "First Last"**, so `john sm` finds John Smith. That assumes the table stores separate first and last names.
- **There's no row limit or paging.** A broad filter such as "Active only" returns every matching row. Add `.Take(n)` or paging if the table is large.
- **The server repeats the two input checks as guards** (`[Fetch]` in section 3). They throw rather than return a message: a caller that skipped `CanSearch` is a bug, and this also stops an unfiltered table scan. This duplicates the rule logic.