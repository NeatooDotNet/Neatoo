Everything needed is in hand. Here is the complete code, grouped by project, with a sentence before each file saying why it lives there.

Project layout assumed: `zCRM.Dal` (row types and repository interface, no EF reference), `zCRM.Ef` (DbContext and repository implementation), `zCRM.Domain` (Neatoo entities), `zCRM.Client` (Blazor WASM), `zCRM.Server` (ASP.NET Core host). The domain references only Dal, so EF Core never reaches the browser bundle. Two assumptions: a phone entry must have a number to be valid, and the email is optional but must be well formed when present.

## Domain (zCRM.Domain)

The interfaces are public and the concretes internal, so the client, the ViewModel and the page only ever see the interface. The root extends `IEntityRoot` (has `Save` and `IsSavable`); the child extends `IEntityBase` (cannot be saved on its own).

```csharp
// zCRM.Domain/Contacts/IContact.cs
using Neatoo;

namespace zCRM.Domain.Contacts;

public enum PhoneType
{
    Home,
    Mobile,
    Work
}

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    IPhoneNumberList? PhoneNumbers { get; }
}

/// <summary>Phone number child entity. No Save(): persisted through the Contact.</summary>
public interface IPhoneNumber : IEntityBase
{
    Guid Id { get; }
    string? Number { get; set; }
    PhoneType Type { get; set; }
}

/// <summary>Child list, parameterized on the child interface.</summary>
public interface IPhoneNumberList : IEntityListBase<IPhoneNumber>
{
}
```

The root carries the required-name and email rules as attributes because the rules run where the property is set, on both tiers. Its persistence operations are `[Remote]` and `internal`: the client calls them, the call crosses to the server, and the repository resolves there. Insert and Update re-run the rules and throw on an invalid graph; that is the server gate.

```csharp
// zCRM.Domain/Contacts/Contact.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal.Contacts;

namespace zCRM.Domain.Contacts;

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
    [EmailAddress(ErrorMessage = "Email must be a valid address")]
    [StringLength(254)]
    public partial string? Email { get; set; }

    public partial IPhoneNumberList? PhoneNumbers { get; private set; }

    public Contact(IEntityBaseServices<Contact> services) : base(services)
    {
    }

    // Local: no server dependency. The list comes from its own factory, never new.
    [Create]
    public async Task Create([Service] IPhoneNumberListFactory phonesFactory)
    {
        PhoneNumbers = phonesFactory.Create();

        // This Create hands an empty form to a person: run the rules so the
        // required names show their messages at once and Save stays disabled.
        // A per-operation choice; the list is empty so All is safe.
        await RunRules(RunRulesFlag.All);
    }

    [Remote]
    [Fetch]
    internal async Task<bool> Fetch(Guid id,
        [Service] IContactRepository repository,
        [Service] IPhoneNumberListFactory phonesFactory)
    {
        var row = await repository.GetAsync(id);
        if (row is null)
        {
            return false;
        }

        // Paused inside the operation: assignment is baseline, nothing is modified.
        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        PhoneNumbers = phonesFactory.Fetch(row.Phones);
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IContactRepository repository,
        [Service] IPhoneNumberListFactory phonesFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new ContactRow();
        MapTo(row);
        repository.Add(row);

        phonesFactory.Save(PhoneNumbers!, row.Phones);

        await repository.SaveChangesAsync();
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IContactRepository repository,
        [Service] IPhoneNumberListFactory phonesFactory)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await repository.GetAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        if (IsSelfModified)
        {
            MapTo(row);
        }

        phonesFactory.Save(PhoneNumbers!, row.Phones);

        await repository.SaveChangesAsync();
    }

    [Remote]
    [Delete]
    internal async Task Delete([Service] IContactRepository repository)
    {
        var row = await repository.GetAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        repository.Remove(row);
        await repository.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.Id = Id;
        row.FirstName = FirstName!;
        row.LastName = LastName!;
        row.Email = Email;
    }
}
```

The child has a public parameterless `[Create]` so the client can make one and add it to the list. Its Fetch, Insert and Update are `internal` with no `[Remote]`: only the list's server-side operations call them, and each one hands the child its own row so the child maps itself.

```csharp
// zCRM.Domain/Contacts/PhoneNumber.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal.Contacts;

namespace zCRM.Domain.Contacts;

[Factory]
internal partial class PhoneNumber : EntityBase<PhoneNumber>, IPhoneNumber
{
    public partial Guid Id { get; private set; }

    [DisplayName("Number")]
    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(30)]
    public partial string? Number { get; set; }

    [DisplayName("Type")]
    public partial PhoneType Type { get; set; }

    public PhoneNumber(IEntityBaseServices<PhoneNumber> services) : base(services)
    {
    }

    [Create]
    public void Create()
    {
        Type = PhoneType.Mobile;
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

The list coordinates the save: it walks the live items plus `DeletedList`, removes rows for deleted children, and hands each new or modified child its row through the child factory. That is what marks each child clean afterwards. Its operations are `internal`, called only by the root on the server.

```csharp
// zCRM.Domain/Contacts/PhoneNumberList.cs
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal.Contacts;

namespace zCRM.Domain.Contacts;

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

## Server-side persistence (zCRM.Dal, zCRM.Ef, zCRM.Server)

The rows and the repository interface sit in a project with no EF Core reference, so the domain can take them as `[Service]` parameters without pulling EF into the client.

```csharp
// zCRM.Dal/Contacts/Rows.cs
namespace zCRM.Dal.Contacts;

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

public interface IContactRepository
{
    Task<ContactRow?> GetAsync(Guid id);
    void Add(ContactRow row);
    void Remove(ContactRow row);
    Task SaveChangesAsync();
}
```

The DbContext and the repository implementation are server-only. The phone relationship is required, so a `PhoneRow` removed from the contact's collection is deleted on `SaveChanges`, which is what the list's Update relies on.

```csharp
// zCRM.Ef/CrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Dal.Contacts;

namespace zCRM.Ef;

public class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }

    public DbSet<ContactRow> Contacts => Set<ContactRow>();
    public DbSet<PhoneRow> Phones => Set<PhoneRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactRow>(b =>
        {
            b.ToTable("ContactRow");
            b.HasKey(c => c.Id);
            b.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            b.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            b.Property(c => c.Email).HasMaxLength(254);

            // Required FK: removing a PhoneRow from Phones deletes it on SaveChanges.
            b.HasMany(c => c.Phones)
             .WithOne()
             .HasForeignKey(p => p.ContactId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhoneRow>(b =>
        {
            b.ToTable("PhoneRow");
            b.HasKey(p => p.Id);
            b.Property(p => p.Number).HasMaxLength(30).IsRequired();
        });
    }
}
```

```csharp
// zCRM.Ef/ContactRepository.cs
using Microsoft.EntityFrameworkCore;
using zCRM.Dal.Contacts;

namespace zCRM.Ef;

public class ContactRepository : IContactRepository
{
    private readonly CrmDbContext _db;

    public ContactRepository(CrmDbContext db)
    {
        _db = db;
    }

    // Tracked, with phones loaded, so the list's Update edits the collection in place.
    public Task<ContactRow?> GetAsync(Guid id) =>
        _db.Contacts.Include(c => c.Phones).SingleOrDefaultAsync(c => c.Id == id);

    public void Add(ContactRow row) => _db.Contacts.Add(row);

    public void Remove(ContactRow row) => _db.Contacts.Remove(row);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
```

The server host registers Neatoo in Server mode against the domain assembly, the DbContext, and the repository, then maps the Neatoo endpoint. The repository is scoped, so one save gets one DbContext and one flush.

```csharp
// zCRM.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Dal.Contacts;
using zCRM.Domain.Contacts;
using zCRM.Ef;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Crm")));
builder.Services.AddScoped<IContactRepository, ContactRepository>();

// Server mode: the generated factories run the operations here and this
// registers what UseNeatoo() needs. Do not also call AddNeatooAspNetCore.
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

app.Run();
```

## Client (zCRM.Client)

The client registers Neatoo in Remote mode, the keyed HttpClient the generated proxies call through, MudBlazor, and the ViewModel.

```csharp
// zCRM.Client/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client.ViewModels;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Remote mode: [Remote] operations cross to the server; everything else runs here.
builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);

builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

```razor
@* zCRM.Client/_Imports.razor *@
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using MudBlazor
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using Neatoo.Blazor.MudNeatoo.Extensions
@using zCRM.Client
@using zCRM.Client.ViewModels
@using zCRM.Domain.Contacts
```

The ViewModel holds the live aggregate, turns gestures into calls on it, and saves. It decides nothing: `CanSave` is the entity's `IsSavable`, and the only state it adds is an in-flight flag, because `Save` does not mark the entity busy during the remote call and a second click would send a second insert.

```csharp
// zCRM.Client/ViewModels/ContactEditViewModel.cs
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

/// <summary>ViewModel for the Contact edit screen: holds the aggregate, saves it.</summary>
public sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;
    private readonly IPhoneNumberFactory _phoneFactory;

    public ContactEditViewModel(IContactFactory contactFactory, IPhoneNumberFactory phoneFactory)
    {
        _contactFactory = contactFactory;
        _phoneFactory = phoneFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IContact? Contact { get; private set; }

    /// <summary>True while a Save round-trip is in flight.</summary>
    public bool IsSaving { get; private set; }

    public async Task New()
    {
        Contact = await _contactFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
    }

    public void AddPhone()
    {
        Contact?.PhoneNumbers?.Add(_phoneFactory.Create());
    }

    public void RemovePhone(IPhoneNumber phone)
    {
        Contact?.PhoneNumbers?.Remove(phone);
    }

    /// <summary>The Save button binds to this. The entity decides.</summary>
    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public async Task Save()
    {
        if (Contact is null || IsSaving)
        {
            return;
        }

        await Contact.WaitForTasks();

        if (!Contact.IsSavable)
        {
            return;  // invalid or unchanged; the summary and fields show why
        }

        IsSaving = true;
        try
        {
            Contact = (IContact)await Contact.Save();  // Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

The page is a binding layer. Each field binds the entity's property wrapper, which gives it the label, value, messages, busy and read-only state. The page subscribes to the entity so the Save button and the summary re-render on any change in the graph, and re-subscribes after Save because Save returns a new instance. The `@key` on the wrapper rebuilds the bound components for that new instance.

```razor
@* zCRM.Client/Pages/ContactEdit.razor *@
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@inject ContactEditViewModel Vm
@inject NavigationManager Nav

<MudContainer MaxWidth="MaxWidth.Small" Class="mt-4">
    <MudText Typo="Typo.h5" Class="mb-4">@(Id is null ? "New Contact" : "Edit Contact")</MudText>

    @if (Vm.Contact is null)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <div @key="Vm.Contact">
            <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

            <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
            <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" Class="mb-3" />
            <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" Class="mb-3" />

            <MudText Typo="Typo.h6" Class="my-3">Phone numbers</MudText>

            @if (Vm.Contact.PhoneNumbers is not null)
            {
                @foreach (var phone in Vm.Contact.PhoneNumbers)
                {
                    <MudGrid Class="mb-2" Spacing="2" @key="phone">
                        <MudItem xs="12" sm="6">
                            <MudNeatooTextField T="string" EntityProperty="@phone[nameof(IPhoneNumber.Number)]" Variant="Variant.Outlined" />
                        </MudItem>
                        <MudItem xs="9" sm="4">
                            <MudNeatooSelect T="PhoneType" EntityProperty="@phone[nameof(IPhoneNumber.Type)]" Variant="Variant.Outlined">
                                <MudSelectItem T="PhoneType" Value="PhoneType.Home">Home</MudSelectItem>
                                <MudSelectItem T="PhoneType" Value="PhoneType.Mobile">Mobile</MudSelectItem>
                                <MudSelectItem T="PhoneType" Value="PhoneType.Work">Work</MudSelectItem>
                            </MudNeatooSelect>
                        </MudItem>
                        <MudItem xs="3" sm="2" Class="d-flex align-center">
                            @* A gesture: the list discards a new phone or queues a fetched one for deletion. *@
                            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                           Color="Color.Error"
                                           OnClick="@(() => Vm.RemovePhone(phone))" />
                        </MudItem>
                    </MudGrid>
                }
            }

            <MudButton Variant="Variant.Outlined"
                       StartIcon="@Icons.Material.Filled.Add"
                       OnClick="Vm.AddPhone"
                       Class="mb-4">
                Add phone
            </MudButton>

            <div class="d-flex justify-end">
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           Disabled="@(!Vm.CanSave)"
                           OnClick="SaveAsync">
                    Save
                </MudButton>
            </div>
        </div>
    }
</MudContainer>

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

    // The page owns the subscription: input components subscribe to their own
    // property wrappers, but the Save button and the summary read the entity,
    // so the page re-renders on the graph-wide event.
    private void Resubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }

        _subscribed = Vm.Contact;

        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged += OnEntityChanged;
        }
    }

    private async Task SaveAsync()
    {
        var wasNew = Vm.Contact?.IsNew ?? false;

        await Vm.Save();
        Resubscribe();

        // A first save produces an Id; move the URL to it so a reload edits the saved contact.
        if (wasNew && Vm.Contact is { IsNew: false } saved)
        {
            Nav.NavigateTo($"/contacts/{saved.Id}", replace: true);
            return;
        }

        StateHasChanged();
    }

    private Task OnEntityChanged(NeatooPropertyChangedEventArgs e)
    {
        return InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.NeatooPropertyChanged -= OnEntityChanged;
        }
    }
}
```

How the pieces fit at runtime: the page creates or fetches a Contact through the generated factory. Each field commit sets a property, the attribute rules run in the browser, and the entity's `IsSavable` enables the Save button only when both names are present, the email parses, every phone has a number, and something changed. Save crosses to the server, where Insert or Update re-runs every rule, maps the contact into its row, hands the phone rows to the list, and flushes once. The saved instance comes back and replaces the one the page holds.
