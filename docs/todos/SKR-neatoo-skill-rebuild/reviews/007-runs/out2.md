Here is the full implementation. The layout follows the skill guidance: criteria are an input model (`ValidateBase`) because a person edits them and rules decide the button and the field message; results are a plain `[Factory]` read model because nothing is edited; the EF Core query lives behind an interface so the shared domain assembly never references EF Core and the Blazor client never ships it.

## Shared domain project: `zCRM.Domain`

**`Contacts/Search/IContactSearchCriteria.cs`** — the public interface; every reference (ViewModel, page) uses the interface and the concrete stays `internal`.

```csharp
using Neatoo;

namespace zCRM.Domain.Contacts.Search;

/// <summary>Input model for the contact search screen.</summary>
public interface IContactSearchCriteria : IValidateBase
{
    string? NameFragment { get; set; }
    string? City { get; set; }
    bool ActiveOnly { get; set; }

    /// <summary>Rule-computed: at least one criterion entered and the name fragment, if any, is usable.</summary>
    bool CanSearch { get; }
}
```

**`Contacts/Search/ContactSearchCriteria.cs`** — the rules live here, not in the page. One `AddValidation` puts the "too short" message on `NameFragment`; one `AddAction` computes `CanSearch` from all three inputs so the Search button binds a fact instead of re-deriving one. `[Create]` is local, so building the form never hits the server.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel;

namespace zCRM.Domain.Contacts.Search;

[Factory]
internal partial class ContactSearchCriteria : ValidateBase<ContactSearchCriteria>, IContactSearchCriteria
{
    private const int MinNameFragmentLength = 2;

    [DisplayName("Name")]
    public partial string? NameFragment { get; set; }

    [DisplayName("City")]
    public partial string? City { get; set; }

    [DisplayName("Active only")]
    public partial bool ActiveOnly { get; set; }

    public partial bool CanSearch { get; private set; }

    public ContactSearchCriteria(IValidateBaseServices<ContactSearchCriteria> services) : base(services)
    {
        // Message under the Name field when a fragment is entered but too short.
        RuleManager.AddValidation(
            t => IsTooShort(t.NameFragment)
                ? $"Enter at least {MinNameFragmentLength} characters"
                : string.Empty,
            t => t.NameFragment);

        // Admission for the Search gesture: something is entered, and the
        // name fragment (if present) is long enough to be worth sending.
        RuleManager.AddAction(
            t => t.CanSearch = HasAnyCriterion(t) && !IsTooShort(t.NameFragment),
            t => t.NameFragment,
            t => t.City,
            t => t.ActiveOnly);
    }

    [Create]
    public void Create()
    {
    }

    private static bool HasAnyCriterion(ContactSearchCriteria t) =>
        !string.IsNullOrWhiteSpace(t.NameFragment)
        || !string.IsNullOrWhiteSpace(t.City)
        || t.ActiveOnly;

    private static bool IsTooShort(string? fragment)
    {
        var trimmed = fragment?.Trim();
        return !string.IsNullOrEmpty(trimmed) && trimmed.Length < MinNameFragmentLength;
    }
}
```

**`Contacts/Search/ContactSearchRequest.cs`** — the immutable request that crosses the wire. A record keeps the `[Fetch]` signature short and serializes without ceremony. On a trimmed client it needs a preserve entry, as the RemoteFactory trimming reference describes.

```csharp
namespace zCRM.Domain.Contacts.Search;

/// <summary>What the client sends to the server to run a contact search.</summary>
public sealed record ContactSearchRequest(string? NameFragment, string? City, bool ActiveOnly);
```

**`Contacts/Search/ContactSearchRow.cs`** — one result row, immutable, shaped for the grid.

```csharp
namespace zCRM.Domain.Contacts.Search;

/// <summary>One row of the contact search results.</summary>
public sealed record ContactSearchRow(Guid Id, string FullName, string? City, DateTime? LastContactedOn);
```

**`Contacts/Search/IContactSearchQuery.cs`** — the seam between the read model and EF Core. Only the server implements it, so the domain assembly stays free of `Microsoft.EntityFrameworkCore`.

```csharp
namespace zCRM.Domain.Contacts.Search;

/// <summary>Server-only query over the Contacts table.</summary>
public interface IContactSearchQuery
{
    Task<IReadOnlyList<ContactSearchRow>> SearchAsync(ContactSearchRequest request, CancellationToken ct);
}
```

**`Contacts/Search/ContactSearchResults.cs`** — the read model. Plain `[Factory]` class, `[Fetch]` only, no Neatoo base class, because nobody edits results. `[Remote]` on the `internal` Fetch makes the generated factory a proxy on the client and runs the query on the server with the `[Service]` resolved there.

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts.Search;

/// <summary>Read model for the contact search results list.</summary>
public interface IContactSearchResults
{
    IReadOnlyList<ContactSearchRow> Rows { get; }
}

[Factory]
internal partial class ContactSearchResults : IContactSearchResults
{
    public IReadOnlyList<ContactSearchRow> Rows { get; internal set; } = [];

    [Remote]
    [Fetch]
    internal async Task Fetch(
        ContactSearchRequest request,
        [Service] IContactSearchQuery query,
        CancellationToken ct)
    {
        Rows = await query.SearchAsync(request, ct);
    }
}
```

## Server project: `zCRM.Server`

**`Data/Contact.cs`** and **`Data/CrmDbContext.cs`** — the EF Core table, stated so the query below has something concrete to compile against.

```csharp
using Microsoft.EntityFrameworkCore;

namespace zCRM.Server.Data;

public sealed class Contact
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastContactedOn { get; set; }
}

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
}
```

**`Data/EfContactSearchQuery.cs`** — the server-side query. It lives in the server project because it depends on the DbContext. Filters are added only for criteria that were supplied, the name match is case-insensitive on the concatenated full name, and the projection happens in SQL so only the three displayed columns come back.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Contacts.Search;

namespace zCRM.Server.Data;

internal sealed class EfContactSearchQuery(CrmDbContext db) : IContactSearchQuery
{
    public async Task<IReadOnlyList<ContactSearchRow>> SearchAsync(ContactSearchRequest request, CancellationToken ct)
    {
        IQueryable<Contact> contacts = db.Contacts.AsNoTracking();

        var name = request.NameFragment?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            var pattern = $"%{name}%";
            contacts = contacts.Where(c =>
                EF.Functions.ILike(c.FirstName + " " + c.LastName, pattern));
        }

        var city = request.City?.Trim();
        if (!string.IsNullOrEmpty(city))
        {
            contacts = contacts.Where(c => c.City != null && EF.Functions.ILike(c.City, city));
        }

        if (request.ActiveOnly)
        {
            contacts = contacts.Where(c => c.IsActive);
        }

        return await contacts
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Select(c => new ContactSearchRow(
                c.Id,
                c.FirstName + " " + c.LastName,
                c.City,
                c.LastContactedOn))
            .ToListAsync(ct);
    }
}
```

`EF.Functions.ILike` is the Npgsql provider's case-insensitive LIKE. On SQL Server swap both calls for `EF.Functions.Like` and rely on the column collation.

**`Program.cs`** — Neatoo in Server mode registered once, before `UseNeatoo`. Do not also call `AddNeatooAspNetCore` after it, which breaks remote calls (RemoteFactory#117). The DbContext and the query are scoped so each remote request gets its own.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain.Contacts.Search;
using zCRM.Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Crm")));

// Neatoo + generated factories, Server mode, pointed at the shared domain assembly.
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContactSearchCriteria).Assembly);

// The [Service] the read model's Fetch resolves on this tier only.
builder.Services.AddScoped<IContactSearchQuery, EfContactSearchQuery>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseNeatoo();   // maps the /api/neatoo endpoint the client proxies call

app.MapFallbackToFile("index.html");
app.Run();
```

## Client project: `zCRM.Client`

**`ViewModels/ContactSearchViewModel.cs`** — holds the input model and the last results, forwards the Search gesture to the read-model factory. It decides nothing: `CanSearch` is the model's fact, with only the in-flight guard added so a double click cannot send two requests.

```csharp
using zCRM.Domain.Contacts.Search;

namespace zCRM.Client.ViewModels;

internal sealed class ContactSearchViewModel
{
    private readonly IContactSearchResultsFactory _resultsFactory;

    public ContactSearchViewModel(
        IContactSearchCriteriaFactory criteriaFactory,
        IContactSearchResultsFactory resultsFactory)
    {
        _resultsFactory = resultsFactory;
        Criteria = criteriaFactory.Create();   // local: no server round-trip
    }

    /// <summary>The input model the page binds to.</summary>
    public IContactSearchCriteria Criteria { get; }

    /// <summary>Null until the first search has returned.</summary>
    public IContactSearchResults? Results { get; private set; }

    public bool IsSearching { get; private set; }

    public bool CanSearch => Criteria.CanSearch && !Criteria.IsBusy && !IsSearching;

    public async Task Search(CancellationToken ct = default)
    {
        await Criteria.WaitForTasks();

        if (!CanSearch)
        {
            return;
        }

        IsSearching = true;
        try
        {
            var request = new ContactSearchRequest(
                Criteria.NameFragment?.Trim(),
                Criteria.City?.Trim(),
                Criteria.ActiveOnly);

            Results = await _resultsFactory.Fetch(request, ct);
        }
        finally
        {
            IsSearching = false;
        }
    }
}
```

**`Pages/ContactSearch.razor`** — the page. The three inputs bind the property objects by hand because MudNeatoo components take `IEntityProperty` and an input model exposes `IValidateProperty` (Neatoo#95). Each field commits through `SetValue`, which runs the rules; the error text under the Name field is the rule's message. The page owns the subscription so the Search button and field metadata re-render when any rule result changes.

```razor
@page "/contacts/search"
@using zCRM.Client.ViewModels
@using zCRM.Domain.Contacts.Search
@implements IDisposable
@inject ContactSearchViewModel Vm

<MudText Typo="Typo.h5" Class="mb-4">Find contacts</MudText>

@{
    var name = Vm.Criteria[nameof(IContactSearchCriteria.NameFragment)];
    var city = Vm.Criteria[nameof(IContactSearchCriteria.City)];
    var activeOnly = Vm.Criteria[nameof(IContactSearchCriteria.ActiveOnly)];
}

<MudGrid Spacing="2" Class="mb-4">
    <MudItem xs="12" sm="5">
        <MudTextField T="string"
                      Label="Name"
                      Value="@Vm.Criteria.NameFragment"
                      ValueChanged="@(async (string v) => await name.SetValue(v))"
                      Error="@(!name.IsValid)"
                      ErrorText="@string.Join(" ", name.PropertyMessages.Select(m => m.Message))"
                      Disabled="@name.IsReadOnly"
                      Variant="Variant.Outlined" />
    </MudItem>
    <MudItem xs="12" sm="4">
        <MudTextField T="string"
                      Label="City"
                      Value="@Vm.Criteria.City"
                      ValueChanged="@(async (string v) => await city.SetValue(v))"
                      Error="@(!city.IsValid)"
                      ErrorText="@string.Join(" ", city.PropertyMessages.Select(m => m.Message))"
                      Disabled="@city.IsReadOnly"
                      Variant="Variant.Outlined" />
    </MudItem>
    <MudItem xs="12" sm="3" Class="d-flex align-center">
        <MudCheckBox T="bool"
                     Label="Active only"
                     Value="@Vm.Criteria.ActiveOnly"
                     ValueChanged="@(async (bool v) => await activeOnly.SetValue(v))"
                     Disabled="@activeOnly.IsReadOnly" />
    </MudItem>
</MudGrid>

@* The button binds the model's admission fact; the page adds no condition of its own. *@
<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           Disabled="@(!Vm.CanSearch)"
           OnClick="SearchAsync"
           Class="mb-4">
    Search
</MudButton>

@if (Vm.IsSearching)
{
    <MudProgressLinear Indeterminate="true" Class="mb-4" />
}

@if (Vm.Results is { } results)
{
    @if (results.Rows.Count == 0)
    {
        <MudText>No contacts match.</MudText>
    }
    else
    {
        <MudTable Items="@results.Rows" Dense="true" Hover="true">
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>City</MudTh>
                <MudTh>Last contacted</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd DataLabel="Name">@context.FullName</MudTd>
                <MudTd DataLabel="City">@context.City</MudTd>
                <MudTd DataLabel="Last contacted">
                    @(context.LastContactedOn?.ToString("yyyy-MM-dd") ?? "Never")
                </MudTd>
            </RowTemplate>
        </MudTable>
    }
}

@code {
    protected override void OnInitialized()
    {
        // The field controls read property metadata and the button reads
        // CanSearch, so the page re-renders on every change the model raises.
        Vm.Criteria.NeatooPropertyChanged += OnCriteriaChanged;
    }

    private async Task SearchAsync()
    {
        StateHasChanged();          // show the progress bar immediately
        await Vm.Search();
        StateHasChanged();
    }

    private Task OnCriteriaChanged(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Vm.Criteria.NeatooPropertyChanged -= OnCriteriaChanged;
    }
}
```

**`_Imports.razor`** additions — needed for the event args type and MudBlazor.

```razor
@using MudBlazor
@using Neatoo
```

**`Program.cs`** — Neatoo in Remote mode so `[Remote]` operations cross to the server while `[Create]` runs locally, the keyed `HttpClient` that the generated proxies call, MudBlazor, and the ViewModel.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain.Contacts.Search;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContactSearchCriteria).Assembly);

// Hosted WASM: the server that serves the client also hosts /api/neatoo.
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactSearchViewModel>();

await builder.Build().RunAsync();
```

## Behaviour check against the requirements

- **Search disabled until a criterion is entered.** `CanSearch` starts false after `Create` and only the `AddAction` rule sets it, triggered by any of the three inputs.
- **Short name fragment shows a message under the field.** The `AddValidation` rule puts the message on `NameFragment`; `ErrorText` renders it. Because `CanSearch` uses the same `IsTooShort` helper, a one-character fragment also keeps the button disabled rather than sending a near-wildcard query.
- **Text fields commit on blur or Enter**, not per keystroke, which is the MudBlazor default and matches the skill's guidance for rule-backed fields. Add `Immediate="true"` to the Name field if the message should appear while typing.
- **Results show name, city, last-contacted date.** The projection happens in SQL and the row record carries exactly those three plus the id for a future detail link.

One assumption to flag: the query uses Npgsql's `ILike`. If zCRM targets SQL Server, replace it with `EF.Functions.Like` as noted above.
