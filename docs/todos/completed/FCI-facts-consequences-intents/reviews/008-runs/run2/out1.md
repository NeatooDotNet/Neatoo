Below are the domain model, the server-side persistence and the Blazor page with its ViewModel. It follows the compiled Neatoo, RemoteFactory and MudNeatoo patterns. Nothing has been built, so treat it as uncompiled.

## Where things live

| Project | Contents | Why |
|---|---|---|
| `zCRM.Domain` (referenced by client and server) | `Contact`, `Phone`, `PhoneList`, their interfaces, `PhoneType`, `ContactRow`/`PhoneRow`, `IContactDbContext` | The same compiled classes run on both sides, so there are no DTOs or controllers. Each save operation copies its own fields into its database row, so the row types and a narrow database interface have to be visible here. |
| `zCRM.Server` | `ZcrmDbContext`, `Program.cs` | EF Core and the real database exist only on the server. |
| `zCRM.Client` | `ContactEditViewModel`, `ContactEdit.razor`, `Program.cs` | The screen binds to the domain object's state and forwards clicks to it. It makes no decisions of its own. |

---

## Domain (`zCRM.Domain`)

**Interfaces.** These are public, and the classes behind them are `internal`. The contact's interface extends `IEntityRoot`, so it gets `Save()` and `IsSavable`. The phone's interface extends `IEntityBase`, so the compiler stops anyone from saving a phone on its own.

```csharp
using Neatoo;

namespace zCRM.Domain;

public enum PhoneType { Home, Mobile, Work }

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    IPhoneList? Phones { get; }
}

/// <summary>Phone number child of Contact; saved through the Contact.</summary>
public interface IPhone : IEntityBase
{
    Guid Id { get; }
    string? Number { get; set; }
    PhoneType Type { get; set; }
}

public interface IPhoneList : IEntityListBase<IPhone> { }
```

**Contact.** The validation rules are attributes on the properties, so they run whenever a property is set, on either the client or the server. `Create` runs locally. `Fetch`, `Insert`, `Update` and `Delete` are `[Remote]`, so the client's call goes to the server, where the database is.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial Guid Id { get; private set; }

    [DisplayName("First Name")]
    [Required(ErrorMessage = "First name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last Name")]
    [Required(ErrorMessage = "Last name is required")]
    public partial string? LastName { get; set; }

    [DisplayName("Email")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email is not a valid address")]
    public partial string? Email { get; set; }

    public partial IPhoneList? Phones { get; private set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services) { }

    [Create]
    public async Task Create([Service] IPhoneListFactory phonesFactory)
    {
        Phones = phonesFactory.Create();

        // Rules do not fire inside an operation. Without this, a blank contact
        // reports valid and Save is enabled on an empty form. All is safe: the
        // phone list is empty.
        await RunRules(RunRulesFlag.All);
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id,
        [Service] IContactDbContext db,
        [Service] IPhoneListFactory phonesFactory)
    {
        var row = await db.FindContactAsync(id);
        if (row is null)
        {
            return false;
        }

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        Phones = phonesFactory.Fetch(row.Phones);
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] IContactDbContext db,
        [Service] IPhoneListFactory phonesFactory)
    {
        // Server gate: a direct factory.Save skips IsSavable. Throw, never return.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();
        var row = new ContactRow { Id = Id };
        MapTo(row);
        db.Add(row);

        phonesFactory.Save(Phones!, row.Phones);

        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] IContactDbContext db,
        [Service] IPhoneListFactory phonesFactory)
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

        phonesFactory.Save(Phones!, row.Phones);

        await db.SaveChangesAsync();
    }

    // IEntityRoot exposes Delete(), so Save can route here; the phone rows cascade.
    [Remote, Delete]
    internal async Task Delete([Service] IContactDbContext db)
    {
        var row = await db.FindContactAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        db.Remove(row);
        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = Email;
    }
}
```

**Phone.** `Create` is public and runs locally, so the client can add a row. The other operations are `internal` and not `[Remote]`: they receive their own row from the list and always run on the server.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

[Factory]
internal partial class Phone : EntityBase<Phone>, IPhone
{
    public partial Guid Id { get; private set; }

    [DisplayName("Number")]
    [Required(ErrorMessage = "Number is required")]
    public partial string? Number { get; set; }

    [DisplayName("Type")]
    public partial PhoneType Type { get; set; }

    public Phone(IEntityBaseServices<Phone> services) : base(services) { }

    [Create]
    public async Task Create()
    {
        Type = PhoneType.Mobile;

        // A blank added row must report invalid, or the contact turns savable
        // and the server gate refuses the save.
        await RunRules(RunRulesFlag.All);
    }

    [Fetch]
    internal void Fetch(PhoneRow row)
    {
        Id = row.Id;
        Number = row.Number;
        Type = row.Type;
    }

    [Insert]
    internal void Insert(PhoneRow row)
    {
        Id = Guid.NewGuid();
        row.Id = Id;
        MapTo(row);
    }

    [Update]
    internal void Update(PhoneRow row)
    {
        MapTo(row);
    }

    private void MapTo(PhoneRow row)
    {
        row.Number = Number!;
        row.Type = Type;
    }
}
```

**PhoneList.** The contact doesn't touch phone rows. The list matches the contact's row collection to its own items, and each phone writes its own row.

```csharp
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain;

[Factory]
internal partial class PhoneList : EntityListBase<IPhone>, IPhoneList
{
    [Create]
    public void Create() { }

    [Fetch]
    internal void Fetch(IEnumerable<PhoneRow> rows, [Service] IPhoneFactory phoneFactory)
    {
        foreach (var row in rows)
        {
            Add(phoneFactory.Fetch(row));
        }
    }

    [Update]
    internal void Update(ICollection<PhoneRow> rows, [Service] IPhoneFactory phoneFactory)
    {
        foreach (var phone in this.Union(DeletedList))
        {
            if (phone.IsDeleted)
            {
                // A removed new phone never reaches DeletedList; EF deletes the orphaned row.
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

**Rows and the database interface.** The save operations need the row types to copy into, and the domain project can't reference the server project. The interface exposes only the four things the operations use.

```csharp
namespace zCRM.Domain;

public class ContactRow
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public ICollection<PhoneRow> Phones { get; set; } = new List<PhoneRow>();
}

public class PhoneRow
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string Number { get; set; } = "";
    public PhoneType Type { get; set; }
}

/// <summary>What the Contact aggregate's operations need from the store. Server-only.</summary>
public interface IContactDbContext
{
    /// <summary>The contact row with its phone rows loaded; null when none.</summary>
    Task<ContactRow?> FindContactAsync(Guid id);
    void Add(ContactRow row);
    void Remove(ContactRow row);
    Task SaveChangesAsync();
}
```

---

## Server (`zCRM.Server`)

**ZcrmDbContext.** This is the only place EF Core is used. Its interface methods are implemented explicitly so they don't clash with `DbContext.Add`, `DbContext.Remove` and `DbContext.SaveChangesAsync`.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain;

namespace zCRM.Server;

internal sealed class ZcrmDbContext : DbContext, IContactDbContext
{
    public ZcrmDbContext(DbContextOptions<ZcrmDbContext> options) : base(options) { }

    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<PhoneRow> Phones => Set<PhoneRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(e =>
        {
            e.ToTable("Contact");
            e.HasKey(c => c.Id);
            // The entity assigns its own key. Without ValueGeneratedNever, EF treats a
            // keyed PhoneRow found in the navigation as existing and issues an UPDATE.
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.FirstName).IsRequired();
            e.Property(c => c.LastName).IsRequired();

            // Required + cascade: a PhoneRow removed from Phones is deleted as an orphan.
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
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Number).IsRequired();
            e.Property(p => p.Type).HasConversion<string>();
        });
    }

    public Task<ContactRow?> FindContactAsync(Guid id) =>
        Contacts.Include(c => c.Phones).SingleOrDefaultAsync(c => c.Id == id);

    void IContactDbContext.Add(ContactRow row) => Contacts.Add(row);

    void IContactDbContext.Remove(ContactRow row) => Contacts.Remove(row);

    Task IContactDbContext.SaveChangesAsync() => SaveChangesAsync();
}
```

**Server `Program.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain;
using zCRM.Server;

var builder = WebApplication.CreateBuilder(args);

// Neatoo in Server mode: the generated factories run the operations here, and this
// registers what UseNeatoo() resolves. Do not also add AddNeatooAspNetCore after it
// (RemoteFactory#117).
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

// What the operations' [Service] parameters resolve: scoped, one DbContext per request.
builder.Services.AddDbContext<ZcrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));
builder.Services.AddScoped<IContactDbContext>(sp => sp.GetRequiredService<ZcrmDbContext>());

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// One endpoint carries every factory call: no controllers.
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

---

## Client (`zCRM.Client`)

**`_Imports.razor`**

```razor
@using System.ComponentModel
@using MudBlazor
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
@using zCRM.Domain
@using zCRM.Client.ViewModels
```

**ContactEditViewModel.** It holds the contact, forwards clicks to it, and saves. The Save button is enabled by the contact's own `IsSavable`. The `IsSaving` flag works around Neatoo#103: `Save()` doesn't mark the entity busy, so a double-click would otherwise send two inserts.

```csharp
using zCRM.Domain;

namespace zCRM.Client.ViewModels;

/// <summary>ViewModel for the Contact edit screen: holds the aggregate, saves it.</summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;
    private readonly IPhoneFactory _phoneFactory;

    public ContactEditViewModel(IContactFactory contactFactory, IPhoneFactory phoneFactory)
    {
        _contactFactory = contactFactory;
        _phoneFactory = phoneFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created, or when not found.</summary>
    public IContact? Contact { get; private set; }

    /// <summary>What the phone type select offers.</summary>
    public IReadOnlyList<PhoneType> PhoneTypes { get; } = Enum.GetValues<PhoneType>();

    /// <summary>A save is in flight; Save does not mark the entity busy (Neatoo#103).</summary>
    public bool IsSaving { get; private set; }

    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public async Task New()
    {
        Contact = await _contactFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
    }

    public async Task AddPhone()
    {
        Contact?.Phones?.Add(await _phoneFactory.Create());
    }

    public void RemovePhone(IPhone phone)
    {
        Contact?.Phones?.Remove(phone);
    }

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
                return;  // invalid or unchanged; the messages are already on the fields
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

**`Pages/ContactEdit.razor`.** The page re-renders when the contact's own state changes (including `IsSavable`), because it shows the Save button. Each input refreshes itself. A phone row shows only inputs, so it needs no component or subscription of its own. It is keyed on the phone so each input stays attached to the right phone's properties.

```razor
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@inject ContactEditViewModel Vm

<PageTitle>Contact</PageTitle>

@if (Vm.Contact is null)
{
    <MudText>@(_loaded ? "Contact not found." : "Loading...")</MudText>
}
else
{
    @* Save returns a new instance; keying on it rebuilds the inputs so each
       binds the new instance's properties. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" Class="mb-3" />

        <MudText Typo="Typo.h6" Class="mt-4 mb-2">Phone Numbers</MudText>

        @foreach (var phone in Vm.Contact.Phones!)
        {
            <MudGrid @key="phone" Spacing="2" Class="mb-2">
                <MudItem xs="12" sm="6">
                    <MudNeatooTextField T="string" EntityProperty="@phone[nameof(IPhone.Number)]" Variant="Variant.Outlined" />
                </MudItem>
                <MudItem xs="10" sm="5">
                    <MudNeatooSelect T="PhoneType" EntityProperty="@phone[nameof(IPhone.Type)]" Variant="Variant.Outlined">
                        @foreach (var type in Vm.PhoneTypes)
                        {
                            <MudSelectItem T="PhoneType" Value="@type">@type</MudSelectItem>
                        }
                    </MudNeatooSelect>
                </MudItem>
                <MudItem xs="2" sm="1" Class="d-flex align-center">
                    <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => Vm.RemovePhone(phone))" />
                </MudItem>
            </MudGrid>
        }

        <MudButton Variant="Variant.Outlined" StartIcon="@Icons.Material.Filled.Add" OnClick="Vm.AddPhone" Class="mb-4">
            Add Phone
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
    [Parameter] public Guid? Id { get; set; }

    private IContact? _subscribed;
    private bool _loaded;

    protected override async Task OnParametersSetAsync()
    {
        _loaded = false;

        if (Id is Guid id)
        {
            await Vm.Load(id);
        }
        else
        {
            await Vm.New();
        }

        _loaded = true;
        Resubscribe();
    }

    // The page shows the Save button, so it re-renders on the contact's own
    // PropertyChanged: its properties and meta flags (IsSavable aggregates the phones).
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnContactChanged;
        }

        _subscribed = Vm.Contact;

        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged += OnContactChanged;
        }
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    private void OnContactChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.PropertyChanged -= OnContactChanged;
        }
    }
}
```

**Client `Program.cs`**

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Neatoo in Remote mode: the generated factories are proxies; [Remote] calls cross.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);

// The HttpClient the proxies call through: the server hosting this client.
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

---

## Things to check

- **Assumptions about requirements:**
  - **Email can be blank.** `[RegularExpression]` treats null or empty as valid. If email is mandatory, add `[Required]`.
  - **Phone number is required.** You didn't ask for this, but a phone row with no number is junk data.
  - **Delete operation.** I added `[Delete]` even though the screen has no Delete button. `IEntityRoot` exposes `Delete()`, and without the operation `Save()` would throw if anyone called it.
- **Why both `Create` methods run the rules:** rules don't fire inside a factory operation. Without that line, a blank contact or blank phone row would report valid. Save would then be enabled, and the server check would reject the save.
- **Not verified:** I didn't check these `using` namespaces against compiled code: `AddNeatooServices`, `UseNeatoo`, `RemoteFactoryServices`, `RunRulesFlag` and `SaveOperationException`. Fix them on the first build if needed.
- **Not included:**
  - `App.razor` and `MainLayout`. Use the standard MudBlazor template; `MudSelect` needs `MudPopoverProvider` there.
  - Authorization on the factory endpoint, which a real CRM needs.