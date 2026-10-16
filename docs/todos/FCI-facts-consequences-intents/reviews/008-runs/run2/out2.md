Writing up the answer now: the types are a Neatoo input model for the criteria and a RemoteFactory read model for the results.

# zCRM contact search

I haven't compiled any of this, since you asked for no files. The generated names it relies on (`IContactSearchCriteriaFactory`, `IContactSearchResultsFactory`, `SetCanSearch`) follow the Neatoo and RemoteFactory naming patterns.

**Decision you should confirm:** I counted a checked "Active only" box as a criterion on its own, so "all active contacts" is a valid search. If it shouldn't count, remove `activeOnly` from `HasCriterion`.

---

## 1. Domain project (loaded on both client and server)

### Search criteria: an input model

**Why it's here:** the criteria are something a person edits and that has rules, but they are never saved. That makes them a `ValidateBase`, not an entity. The two requirements are written as rules: the length check is a validation message on `NameFragment`, and "Search is enabled" is a `CanSearch` value the rules compute. The page only binds to `CanSearch`; it doesn't work out the condition itself. The rules run in the browser on every change, so the server is never called.

```csharp
// ZCrm.Domain/Contacts/IContactSearchCriteria.cs
using Neatoo;

namespace ZCrm.Domain.Contacts;

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

```csharp
// ZCrm.Domain/Contacts/ContactSearchCriteria.cs
using System.Linq.Expressions;
using Neatoo;
using Neatoo.RemoteFactory;

namespace ZCrm.Domain.Contacts;

[Factory]
internal partial class ContactSearchCriteria : ValidateBase<ContactSearchCriteria>, IContactSearchCriteria
{
    internal const int MinNameLength = 2;

    public partial string? NameFragment { get; set; }
    public partial string? City { get; set; }
    public partial bool ActiveOnly { get; set; }
    public partial bool CanSearch { get; private set; }

    public ContactSearchCriteria(IValidateBaseServices<ContactSearchCriteria> services) : base(services)
    {
        RuleManager.AddValidation(
            t => IsNameTooShort(t.NameFragment)
                ? $"Enter at least {MinNameLength} characters of the name."
                : string.Empty,
            t => t.NameFragment);

        // Admission for Search. IsValid is a trigger, so when the name
        // message appears or clears, CanSearch follows.
        RuleManager.AddActionAsync(
            async t => await t.SetCanSearch(HasCriterion(t.NameFragment, t.City, t.ActiveOnly) && t.IsValid),
            new Expression<Func<ContactSearchCriteria, object?>>[]
            {
                t => t.NameFragment, t => t.City, t => t.ActiveOnly, t => t.IsValid
            });
    }

    // Local: an empty search form needs nothing from the server.
    [Create]
    public void Create()
    {
    }

    internal static bool HasCriterion(string? nameFragment, string? city, bool activeOnly) =>
        !string.IsNullOrWhiteSpace(nameFragment) || !string.IsNullOrWhiteSpace(city) || activeOnly;

    internal static bool IsNameTooShort(string? nameFragment)
    {
        var trimmed = nameFragment?.Trim();
        return !string.IsNullOrEmpty(trimmed) && trimmed.Length < MinNameLength;
    }
}
```

### Search results: a read model

**Why it's here:** the results are data from the server that the screen only displays, with no rules and no saving. That makes them a plain `[Factory]` class with only a `[Fetch]`; there's no DTO and no controller. The `[Fetch]` is `[Remote]` because the client calls it. Its body runs on the server and is trimmed out of the client build. The guard at the top is the server's safety net against a caller that skipped the rules. It throws an exception rather than returning a message, because a client that gets past `CanSearch` is a bug, not invalid user input.

```csharp
// ZCrm.Domain/Contacts/ContactSearchResults.cs
using Neatoo.RemoteFactory;

namespace ZCrm.Domain.Contacts;

/// <summary>
/// One row of the contact search results.
/// </summary>
public sealed record ContactSearchRow(Guid Id, string Name, string City, DateTime? LastContactedOn);

/// <summary>
/// Read model for the contact search results.
/// </summary>
public interface IContactSearchResults
{
    IReadOnlyList<ContactSearchRow> Contacts { get; }
}

[Factory]
internal partial class ContactSearchResults : IContactSearchResults
{
    public IReadOnlyList<ContactSearchRow> Contacts { get; internal set; } = [];

    [Remote, Fetch]
    internal async Task Fetch(
        string? nameFragment,
        string? city,
        bool activeOnly,
        [Service] IContactSearchQuery query,
        CancellationToken cancellationToken)
    {
        // The criteria's rules gate the Search button; this refuses a caller that skipped them.
        if (!ContactSearchCriteria.HasCriterion(nameFragment, city, activeOnly)
            || ContactSearchCriteria.IsNameTooShort(nameFragment))
        {
            throw new ArgumentException(
                $"Contact search needs at least one criterion and a name fragment of at least {ContactSearchCriteria.MinNameLength} characters.");
        }

        Contacts = await query.Search(nameFragment, city, activeOnly, cancellationToken);
    }
}
```

### The query interface

**Why it's here:** the `[Fetch]` above has to compile on both tiers, so the type of its `[Service]` parameter is declared in the domain project. The implementation lives only on the server, which keeps EF Core out of the browser bundle. It deliberately has **no** `[Factory]`: the client never calls it, only the server-side `[Fetch]` does.

```csharp
// ZCrm.Domain/Contacts/IContactSearchQuery.cs
namespace ZCrm.Domain.Contacts;

/// <summary>
/// Server-only contact query used by ContactSearchResults.Fetch.
/// </summary>
public interface IContactSearchQuery
{
    Task<IReadOnlyList<ContactSearchRow>> Search(
        string? nameFragment,
        string? city,
        bool activeOnly,
        CancellationToken cancellationToken);
}
```

---

## 2. Server project

### The EF Core query

**Why it's here:** this is the only code that touches `CrmDbContext`. It projects straight into the row record, so it uses `AsNoTracking` and doesn't load whole `Contact` entities. Matching on `FirstName + " " + LastName` means both "smi" and "john sm" find John Smith. Case sensitivity follows the database collation (case-insensitive by default on SQL Server). City matches exactly after trimming.

```csharp
// ZCrm.Server/Contacts/ContactSearchQuery.cs
using Microsoft.EntityFrameworkCore;
using ZCrm.Domain.Contacts;
using ZCrm.Server.Data;

namespace ZCrm.Server.Contacts;

internal sealed class ContactSearchQuery : IContactSearchQuery
{
    private readonly CrmDbContext _db;

    public ContactSearchQuery(CrmDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ContactSearchRow>> Search(
        string? nameFragment,
        string? city,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        IQueryable<Contact> contacts = _db.Contacts.AsNoTracking();

        var name = nameFragment?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            contacts = contacts.Where(c => (c.FirstName + " " + c.LastName).Contains(name));
        }

        var cityName = city?.Trim();
        if (!string.IsNullOrEmpty(cityName))
        {
            contacts = contacts.Where(c => c.City == cityName);
        }

        if (activeOnly)
        {
            contacts = contacts.Where(c => c.IsActive);
        }

        return await contacts
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new ContactSearchRow(c.Id, c.FirstName + " " + c.LastName, c.City, c.LastContactedOn))
            .ToListAsync(cancellationToken);
    }
}
```

The `Contacts` table shape the query assumes:

```csharp
// ZCrm.Server/Data/Contact.cs
namespace ZCrm.Server.Data;

public class Contact
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastContactedOn { get; set; }
}

// In CrmDbContext:
// public DbSet<Contact> Contacts => Set<Contact>();
```

### Server registration

Add these next to the existing `AddNeatooServices(NeatooFactory.Server, …)` and `AddDbContext<CrmDbContext>` calls:

```csharp
builder.Services.AddScoped<IContactSearchQuery, ContactSearchQuery>();
```

---

## 3. Client project (Blazor WebAssembly)

### ViewModel

**Why it's here:** it holds the criteria and the latest results, and turns the Search click into a `[Fetch]`. It makes no decisions of its own; `CanSearch` comes from the criteria. The one thing it adds is `IsSearching`, which is screen state that disables the button while a request is running so a double-click doesn't send two requests.

```csharp
// ZCrm.Client/Contacts/ContactSearchViewModel.cs
using ZCrm.Domain.Contacts;

namespace ZCrm.Client.Contacts;

/// <summary>
/// ViewModel for the contact search screen: holds the criteria, fetches the results.
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

    public IContactSearchCriteria Criteria { get; }

    /// <summary>Null until the first search.</summary>
    public IContactSearchResults? Results { get; private set; }

    public bool IsSearching { get; private set; }

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

Client registration, next to `AddNeatooServices(NeatooFactory.Remote, …)` and `AddMudServices()`:

```csharp
builder.Services.AddTransient<ContactSearchViewModel>();
```

### Page

**Why it's here, and why it doesn't use the `MudNeatoo*` fields:** those components only work with entity properties. A `ValidateBase` property can't be bound through them ([Neatoo#95](https://github.com/NeatooDotNet/Neatoo/issues/95)), so each field is bound by hand to its Neatoo property: the value is written with `SetValue`, and the error state, message and read-only state come from the property. `Immediate="true"` makes each keystroke update the criteria, so the Search button turns on as soon as you type. On blur-commit it would stay disabled until you left the field. Per-keystroke updates are fine here because these rules are local and cheap. The page subscribes to the criteria's own `PropertyChanged` so the button and message refresh whenever the rules change something.

```razor
@* ZCrm.Client/Contacts/ContactSearchPage.razor *@
@page "/contacts"
@using System.ComponentModel
@using ZCrm.Domain.Contacts
@implements IDisposable
@inject ContactSearchViewModel Vm

<PageTitle>Contacts</PageTitle>

<MudText Typo="Typo.h4" Class="mb-4">Contacts</MudText>

@{
    var name = Vm.Criteria[nameof(IContactSearchCriteria.NameFragment)];
    var city = Vm.Criteria[nameof(IContactSearchCriteria.City)];
    var activeOnly = Vm.Criteria[nameof(IContactSearchCriteria.ActiveOnly)];
}

<MudPaper Class="pa-4 mb-4">
    <MudGrid Spacing="2">
        <MudItem xs="12" sm="5">
            @* The criteria's own message appears under the field. *@
            <MudTextField T="string"
                          Label="Name"
                          Value="@Vm.Criteria.NameFragment"
                          ValueChanged="@(async (string v) => await name.SetValue(v))"
                          Immediate="true"
                          Clearable="true"
                          Error="@(!name.IsValid)"
                          ErrorText="@string.Join(" ", name.PropertyMessages.Select(m => m.Message))"
                          Disabled="@name.IsReadOnly" />
        </MudItem>
        <MudItem xs="12" sm="4">
            <MudTextField T="string"
                          Label="City"
                          Value="@Vm.Criteria.City"
                          ValueChanged="@(async (string v) => await city.SetValue(v))"
                          Immediate="true"
                          Clearable="true"
                          Error="@(!city.IsValid)"
                          ErrorText="@string.Join(" ", city.PropertyMessages.Select(m => m.Message))"
                          Disabled="@city.IsReadOnly" />
        </MudItem>
        <MudItem xs="12" sm="3" Class="d-flex align-center">
            <MudCheckBox T="bool"
                         Label="Active only"
                         Value="@Vm.Criteria.ActiveOnly"
                         ValueChanged="@(async (bool v) => await activeOnly.SetValue(v))"
                         Disabled="@activeOnly.IsReadOnly" />
        </MudItem>
    </MudGrid>

    @* Enabled by the criteria's admission fact; the page has no condition of its own. *@
    <MudButton Variant="Variant.Filled"
               Color="Color.Primary"
               Disabled="@(!Vm.CanSearch)"
               OnClick="SearchAsync"
               Class="mt-3">
        Search
    </MudButton>
</MudPaper>

@if (Vm.Results is { } results)
{
    <MudTable T="ContactSearchRow" Items="@results.Contacts" Dense="true" Hover="true" Loading="@Vm.IsSearching">
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>City</MudTh>
            <MudTh>Last contacted</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">@context.Name</MudTd>
            <MudTd DataLabel="City">@context.City</MudTd>
            <MudTd DataLabel="Last contacted">@(context.LastContactedOn?.ToString("d") ?? "Never")</MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudText>No contacts match.</MudText>
        </NoRecordsContent>
    </MudTable>
}

@code {
    protected override void OnInitialized()
    {
        // The criteria never change instance, so one subscription for the page's lifetime.
        Vm.Criteria.PropertyChanged += OnCriteriaChanged;
    }

    private Task SearchAsync() => Vm.Search();

    private void OnCriteriaChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Vm.Criteria.PropertyChanged -= OnCriteriaChanged;
    }
}
```

---

## Things to know

- **The fetch only takes plain values:** the results `[Fetch]` receives `nameFragment`, `city` and `activeOnly`, not the criteria object. That keeps the request small and avoids sending a Neatoo object across the wire. The server guard reuses `HasCriterion` and `IsNameTooShort`, so the rule that disables the button and the server's check are the same code.
- **No result limit:** "Active only" alone can return every active contact. If that list will be large, add `.Take(n)` in `ContactSearchQuery` and an `IsTruncated` flag on the read model; I left it out because you didn't ask for it.
- **Old results stay on screen:** editing the criteria after a search doesn't clear the previous results. Say if you want them cleared when the criteria change.