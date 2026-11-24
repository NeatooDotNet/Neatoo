I couldn't compile any of this here: the folder is empty and you asked for code in the reply, not files. Every pattern below comes from the Neatoo, RemoteFactory and MudNeatoo skill references, whose examples are compiled and tested. A few `using` namespaces and the EF tracking behaviour come from memory; I list them at the end.

## Project layout

| Project | Holds | References |
|---|---|---|
| `Zcrm.Dal` | Row classes and the `IContactDb` interface. No EF Core | — |
| `Zcrm.Ef` | `ZcrmDbContext` | Dal, EF Core |
| `Zcrm.Domain` | Contact aggregate | Dal, Neatoo |
| `Zcrm.Client` | ViewModel, page, client `Program` | Domain, MudNeatoo |
| `Zcrm.Server` | Host, server `Program` | Domain, Ef, Client |

**Why this split:** the domain assembly runs in the browser too, so it can only depend on Dal. EF Core stays in a project only the server references.

---

## Zcrm.Dal

**Why here:** the domain's server operations take `IContactDb` as a `[Service]` parameter, so the interface must be something the domain can see. The rows are plain classes, so Dal needs no EF package.

```csharp
// Zcrm.Dal/ContactRow.cs
namespace Zcrm.Dal;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public ICollection<PhoneRow> Phones { get; set; } = new List<PhoneRow>();
}

public class PhoneRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Number { get; set; } = string.Empty;
    public int Type { get; set; }
}
```

```csharp
// Zcrm.Dal/IContactDb.cs
namespace Zcrm.Dal;

/// <summary>
/// The store the Contact aggregate's server operations write. Implemented by ZcrmDbContext.
/// </summary>
public interface IContactDb
{
    /// <summary>The contact row with its phone rows loaded and tracked; null when there is none.</summary>
    Task<ContactRow?> FindContactAsync(Guid id, CancellationToken cancellationToken = default);
    void AddContact(ContactRow row);
    void RemoveContact(ContactRow row);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
```

## Zcrm.Ef

**Why here:** this is the only project that references EF Core. The context implements `IContactDb` with explicit interface members, so its methods don't clash with `DbContext.Add` or `SaveChangesAsync`.

```csharp
// Zcrm.Ef/ZcrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using Zcrm.Dal;

namespace Zcrm.Ef;

public sealed class ZcrmDbContext(DbContextOptions<ZcrmDbContext> options) : DbContext(options), IContactDb
{
    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<PhoneRow> Phones => Set<PhoneRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(e =>
        {
            e.ToTable("Contact");
            e.HasKey(c => c.Id);
            // The entity sets its own key in [Insert]
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            e.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(254);
            // A row removed from Phones is an orphan of a required relationship, so EF deletes it
            e.HasMany(c => c.Phones)
             .WithOne()
             .HasForeignKey(p => p.ContactId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhoneRow>(e =>
        {
            e.ToTable("Phone");
            e.HasKey(p => p.Id);
            // Non-generated key: a new row found through Phones is tracked as Added, not Modified
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Number).HasMaxLength(30).IsRequired();
        });
    }

    Task<ContactRow?> IContactDb.FindContactAsync(Guid id, CancellationToken cancellationToken)
        => Contacts.Include(c => c.Phones).SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    void IContactDb.AddContact(ContactRow row) => Contacts.Add(row);

    void IContactDb.RemoveContact(ContactRow row) => Contacts.Remove(row);

    Task IContactDb.SaveAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}
```

---

## Zcrm.Domain: the Contact aggregate

**Why here:** this assembly is loaded on both the client and the server. The validation rules run in the browser while the salesperson types. The persistence operations are `internal` and run only on the server, where `IContactDb` is registered.

The design in brief:
- `Contact` is the root (`IEntityRoot`, so it has `Save()`).
- `PhoneNumber` is a child (`IEntityBase`, so it has no `Save()`).
- The list moves each phone's row in or out, and each object maps its own row.
- The concrete classes are `internal`; everything outside uses the interfaces.

```csharp
// Zcrm.Domain/Contacts/PhoneType.cs
namespace Zcrm.Domain.Contacts;

public enum PhoneType
{
    Home = 0,
    Mobile = 1,
    Work = 2,
}
```

```csharp
// Zcrm.Domain/Contacts/IContactInterfaces.cs
using Neatoo;

namespace Zcrm.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public partial interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    IPhoneNumberList? PhoneNumbers { get; }
}

/// <summary>A phone number of a contact. Saved through the Contact.</summary>
public partial interface IPhoneNumber : IEntityBase
{
    Guid Id { get; }
    string? Number { get; set; }
    PhoneType Type { get; set; }
}

public interface IPhoneNumberList : IEntityListBase<IPhoneNumber>
{
}
```

```csharp
// Zcrm.Domain/Contacts/Contact.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Dal;

namespace Zcrm.Domain.Contacts;

/// <summary>
/// Contact aggregate root. Validates on every edit; writes its own row and hands
/// the phone rows to the phone list.
/// </summary>
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial Guid Id { get; private set; }

    [DisplayName("First name")]
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public partial string? FirstName { get; set; }

    [DisplayName("Last name")]
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public partial string? LastName { get; set; }

    [DisplayName("Email")]
    [StringLength(254)]
    public partial string? Email { get; set; }

    public partial IPhoneNumberList? PhoneNumbers { get; private set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrWhiteSpace(t.Email) || EmailPattern().IsMatch(t.Email)
                ? string.Empty
                : "Email is not a valid address",
            t => t.Email);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    /// <summary>
    /// A new contact. Local: no server call. Runs the rules so the empty form shows
    /// its required fields and Save stays disabled until they are filled; the phone
    /// list is empty, so All is safe.
    /// </summary>
    [Create]
    public async Task Create([Service] IPhoneNumberListFactory phonesFactory)
    {
        PhoneNumbers = phonesFactory.Create();
        await RunRules(RunRulesFlag.All);
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id,
        [Service] IContactDb db,
        [Service] IPhoneNumberListFactory phonesFactory)
    {
        var row = await db.FindContactAsync(id);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        PhoneNumbers = phonesFactory.Fetch(row.Phones);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] IContactDb db, [Service] IPhoneNumberListFactory phonesFactory)
    {
        // Server gate against a caller that skipped IsSavable. Throw, never return.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new ContactRow();
        MapTo(row);
        db.AddContact(row);

        phonesFactory.Save(PhoneNumbers!, row.Phones);

        await db.SaveAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] IContactDb db, [Service] IPhoneNumberListFactory phonesFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        phonesFactory.Save(PhoneNumbers!, row.Phones);

        await db.SaveAsync();
    }

    /// <summary>Removes the contact's row; its phone rows cascade.</summary>
    [Remote, Delete]
    internal async Task Delete([Service] IContactDb db)
    {
        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        db.RemoveContact(row);
        await db.SaveAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.Id = Id;
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = string.IsNullOrWhiteSpace(Email) ? null : Email;
    }
}
```

```csharp
// Zcrm.Domain/Contacts/PhoneNumber.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Dal;

namespace Zcrm.Domain.Contacts;

/// <summary>
/// A contact's phone number. Child of Contact: maps itself into the row its list
/// hands it; never saved on its own.
/// </summary>
[Factory]
internal partial class PhoneNumber : EntityBase<PhoneNumber>, IPhoneNumber
{
    public partial Guid Id { get; private set; }

    [DisplayName("Number")]
    [Required(ErrorMessage = "Number is required")]
    [StringLength(30)]
    public partial string? Number { get; set; }

    [DisplayName("Type")]
    public partial PhoneType Type { get; set; }

    public PhoneNumber(IEntityBaseServices<PhoneNumber> services) : base(services)
    {
    }

    /// <summary>
    /// A new phone row. Public and local: the client builds it, then adds it to the
    /// contact. Runs its rules so the empty number shows as required at once.
    /// </summary>
    [Create]
    public async Task Create()
    {
        Type = PhoneType.Mobile;
        await RunRules(RunRulesFlag.All);
    }

    [Fetch]
    internal void Fetch(PhoneRow row)
    {
        Id = row.Id;
        Number = row.Number;
        Type = (PhoneType)row.Type;
    }

    [Insert]
    internal void Insert(PhoneRow row)
    {
        Id = Guid.NewGuid();
        MapTo(row);
    }

    [Update]
    internal void Update(PhoneRow row)
    {
        MapTo(row);
    }

    private void MapTo(PhoneRow row)
    {
        row.Id = Id;
        row.Number = Number!;
        row.Type = (int)Type;
    }
}
```

```csharp
// Zcrm.Domain/Contacts/PhoneNumberList.cs
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Dal;

namespace Zcrm.Domain.Contacts;

/// <summary>
/// The contact's phone numbers. Brings the contact row's phone rows in line with
/// the list; each phone maps its own row.
/// </summary>
[Factory]
internal partial class PhoneNumberList : EntityListBase<IPhoneNumber>, IPhoneNumberList
{
    [Create]
    public void Create()
    {
    }

    [Fetch]
    internal void Fetch(IEnumerable<PhoneRow> rows, [Service] IPhoneNumberFactory phoneFactory)
    {
        foreach (var row in rows)
        {
            Add(phoneFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<PhoneRow> rows, [Service] IPhoneNumberFactory phoneFactory)
    {
        foreach (var phone in this.Union(DeletedList))
        {
            if (phone.IsDeleted)
            {
                // A new phone removed from the list never reaches DeletedList
                if (!phone.IsNew)
                {
                    rows.Remove(rows.Single(r => r.Id == phone.Id));
                }
            }
            else if (phone.IsNew)
            {
                var row = new PhoneRow();
                rows.Add(row);
                phoneFactory.Save(phone, row);
            }
            else if (phone.IsModified)
            {
                phoneFactory.Save(phone, rows.Single(r => r.Id == phone.Id));
            }
        }
    }
}
```

**What the factory attributes mean here:**
- `Create` has no `[Remote]`: building an empty contact needs nothing from the server, so it costs no round trip.
- `Fetch`, `Insert`, `Update` and `Delete` on the root are `[Remote] internal`: they are where the client's call crosses to the server.
- The child and list operations are `internal` without `[Remote]`: they only run on the server, called from the root.
- There is no Validate step anywhere. The rules ran when each field was set. The `RunRules(All)` inside Insert/Update is only a server-side safety net.

---

## Zcrm.Client: the ViewModel

**Why here:** it is screen code. It holds the live aggregate, turns gestures (add a phone, remove a phone, Save) into calls on the domain, and reads `IsSavable`. It makes no decisions of its own.

```csharp
// Zcrm.Client/Contacts/ContactEditViewModel.cs
using Zcrm.Domain.Contacts;

namespace Zcrm.Client.Contacts;

/// <summary>
/// ViewModel for the Contact edit screen: holds the aggregate, forwards the phone
/// gestures, saves.
/// </summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;
    private readonly IPhoneNumberFactory _phoneFactory;

    public ContactEditViewModel(IContactFactory contactFactory, IPhoneNumberFactory phoneFactory)
    {
        _contactFactory = contactFactory;
        _phoneFactory = phoneFactory;
    }

    /// <summary>The aggregate the page binds to. Null until created or loaded, or when not found.</summary>
    public IContact? Contact { get; private set; }

    /// <summary>A Fetch for an id the store does not hold.</summary>
    public bool NotFound { get; private set; }

    /// <summary>What the phone type select offers.</summary>
    public IReadOnlyList<PhoneType> PhoneTypes { get; } = Enum.GetValues<PhoneType>();

    /// <summary>True while a Save is on the wire: Save does not mark the entity busy (Neatoo#103).</summary>
    public bool IsSaving { get; private set; }

    public async Task New()
    {
        NotFound = false;
        Contact = await _contactFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
        NotFound = Contact is null;
    }

    public async Task AddPhone()
    {
        if (Contact?.PhoneNumbers is null)
        {
            return;
        }

        Contact.PhoneNumbers.Add(await _phoneFactory.Create());
    }

    /// <summary>A fetched phone moves to DeletedList; its row is removed on Save.</summary>
    public void RemovePhone(IPhoneNumber phone)
    {
        Contact?.PhoneNumbers?.Remove(phone);
    }

    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public async Task Save()
    {
        if (Contact is null || IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            await Contact.WaitForTasks();

            if (!Contact.IsSavable)
            {
                return;  // the field messages and the summary show why
            }

            Contact = (IContact)await Contact.Save();  // Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

## Zcrm.Client: the page

**Why here:** the page only lays out and binds controls. Each MudNeatoo field binds the entity's property object, so it gets its label, value, messages, busy state and read-only state from the domain. The page has no `EditForm`, no copy of the data in a POCO, and no validation logic.

```razor
@* Zcrm.Client/Contacts/ContactEdit.razor *@
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@inject ContactEditViewModel Vm
@inject ISnackbar Snackbar

<PageTitle>Contact</PageTitle>

@if (Vm.NotFound)
{
    <MudAlert Severity="Severity.Warning">Contact not found.</MudAlert>
}
else if (Vm.Contact is null)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    @* @key: Save returns a new instance; keying on it rebuilds the bound
       components so each field subscribes to the new instance's wrappers. *@
    <div @key="Vm.Contact">
        <MudPaper Class="pa-4">
            <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

            <MudGrid Spacing="2">
                <MudItem xs="12" sm="6">
                    <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="12" sm="6">
                    <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="12">
                    <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" />
                </MudItem>
            </MudGrid>

            <MudText Typo="Typo.h6" Class="mt-6 mb-2">Phone numbers</MudText>

            @foreach (var phone in Vm.Contact.PhoneNumbers!)
            {
                @* Keyed on the item: a field subscribes to its property once, when first rendered. *@
                <MudGrid @key="phone" Spacing="2" Class="mb-2">
                    <MudItem xs="7">
                        <MudNeatooTextField T="string" EntityProperty="@phone[nameof(IPhoneNumber.Number)]" />
                    </MudItem>
                    <MudItem xs="4">
                        <MudNeatooSelect T="PhoneType" EntityProperty="@phone[nameof(IPhoneNumber.Type)]">
                            @foreach (var type in Vm.PhoneTypes)
                            {
                                <MudSelectItem T="PhoneType" Value="@type">@type</MudSelectItem>
                            }
                        </MudNeatooSelect>
                    </MudItem>
                    <MudItem xs="1" Class="d-flex align-center">
                        <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => Vm.RemovePhone(phone))" />
                    </MudItem>
                </MudGrid>
            }

            <MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.Add" OnClick="Vm.AddPhone" Class="mb-4">
                Add phone
            </MudButton>

            <div>
                <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="@(!Vm.CanSave)" OnClick="SaveAsync">
                    Save
                </MudButton>
            </div>
        </MudPaper>
    </div>
}

@code {
    [Parameter] public Guid? Id { get; set; }

    private IContact? _subscribed;

    protected override async Task OnParametersSetAsync()
    {
        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }

        Resubscribe();
    }

    // The fields subscribe to their own properties; the Save button reads the
    // aggregate's IsSavable, so the page re-renders on the graph-wide event.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnContactChanged;
        }

        _subscribed = Vm.Contact;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnContactChanged;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            await Vm.Save();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Save failed: {ex.Message}", Severity.Error);
        }

        // Save leaves the ViewModel holding a new instance
        Resubscribe();
    }

    private Task OnContactChanged(NeatooPropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnContactChanged;
        }
    }
}
```

```razor
@* Zcrm.Client/_Imports.razor *@
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
@using Zcrm.Client
@using Zcrm.Client.Contacts
@using Zcrm.Domain.Contacts
```

```razor
@* Zcrm.Client/App.razor *@
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <MudText>Nothing here.</MudText>
        </LayoutView>
    </NotFound>
</Router>
```

```razor
@* Zcrm.Client/MainLayout.razor — MudSelect needs the popover provider; the Snackbar needs its provider *@
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudMainContent Class="pa-4">
        <MudContainer MaxWidth="MaxWidth.Medium">@Body</MudContainer>
    </MudMainContent>
</MudLayout>
```

---

## Wiring both sides

**Why here:** each side registers Neatoo once. The client uses Remote mode (the generated factories are proxies that call the server). The server uses Server mode. Only the server registers `IContactDb`, so the server operations' `[Service]` parameters can only be resolved there.

```csharp
// Zcrm.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Client;
using Zcrm.Client.Contacts;
using Zcrm.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Neatoo on the client: the generated factories are proxies; [Remote] operations cross to the server
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);

// The HttpClient the proxies send through: the server that hosts this client
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

```csharp
// Zcrm.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Zcrm.Dal;
using Zcrm.Domain.Contacts;
using Zcrm.Ef;

var builder = WebApplication.CreateBuilder(args);

// Neatoo on the server: the factories run the operations here, and this registers what
// UseNeatoo() resolves. Do not also call AddNeatooAspNetCore (RemoteFactory#117 breaks every remote Save).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

// The [Service] parameters of the Contact operations
builder.Services.AddDbContext<ZcrmDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Zcrm")));
builder.Services.AddScoped<IContactDb>(sp => sp.GetRequiredService<ZcrmDbContext>());

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// One endpoint carries every factory call; no controllers, no DTOs
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## Decisions you should check

- **Email is optional.** It is validated only when something is entered. If every contact must have one, add `[Required]` to `Email`.
- **Phone number is required.** You didn't ask for this; I added it because a phone row with no number would be saved empty. A new phone also defaults to Mobile.
- **New forms show their errors immediately.** `Contact.Create` and `PhoneNumber.Create` run their rules on creation. Required fields show their messages right away and Save stays disabled. Without this, an empty form would report itself valid and only fail at the server.
- **Double-click guard.** `Save()` doesn't mark the entity busy while the call is on the wire (Neatoo#103), so a second click would send a second Insert. `IsSaving` in the ViewModel disables the button until the save returns.
- **Not verified:**
  - The namespaces for `AddNeatooServices`, `UseNeatoo`, `RemoteFactoryServices` and the factory attributes are from memory.
  - That EF tracks the new phone rows as Added, given `ValueGeneratedNever()`, is from memory of EF Core 3+ behaviour.
  - The first build will confirm both. I wrote no tests.

The server project also needs `Microsoft.AspNetCore.Components.WebAssembly.Server` for `UseBlazorFrameworkFiles`. `index.html` needs MudBlazor's CSS and JS links.