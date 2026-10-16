I'm writing this as five projects. The split keeps EF Core out of the browser: the client references only the domain, and the domain references only a data-access project that has no EF package.

| Project | References | Holds |
|---|---|---|
| `Zcrm.Dal` | none | your EF `Contact` class, `IContactRepository` |
| `Zcrm.Ef` | Dal, EF Core | `ZcrmDbContext`, `ContactRepository` |
| `Zcrm.Domain` | Dal, Neatoo | the `Contact` domain model, the email-check command, the rule |
| `Zcrm.Server` | Domain, Ef, Client | `Program.cs` |
| `Zcrm.Client` | Domain, MudNeatoo | `Program.cs`, ViewModel, edit page |

**Assumptions:** your EF `Contact` has a `Guid Id` key. Its class needs no EF attributes, so it can live in `Zcrm.Dal`. The `using` lines for Neatoo namespaces are from memory, so your IDE may need to correct one or two.

**How the check works:** the duplicate check is a validation rule on the `Contact` domain model. On every edit the rule calls the server through a generated command, and the answer appears as an error under the Email field. The server re-runs the same rule before it writes, and a unique index covers two people saving the same email at the same moment.

---

## Zcrm.Dal

**Why here:** the domain can name these types without pulling EF Core into the browser.

```csharp
// Zcrm.Dal/Contact.cs
namespace Zcrm.Dal;

/// <summary>EF Core row for the Contacts table.</summary>
public class Contact
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
}
```

```csharp
// Zcrm.Dal/IContactRepository.cs
namespace Zcrm.Dal;

/// <summary>Server-only access to the Contacts table.</summary>
public interface IContactRepository
{
    Task<Contact?> GetAsync(Guid id);
    void Add(Contact contact);

    /// <summary>True when a contact other than <paramref name="excludingContactId"/> has this email.</summary>
    Task<bool> EmailInUseAsync(string email, Guid excludingContactId);

    Task SaveChangesAsync();
}
```

## Zcrm.Ef

**Why here:** this is the only project that references EF Core, and only the server references it.

```csharp
// Zcrm.Ef/ZcrmDbContext.cs
using Microsoft.EntityFrameworkCore;
using Zcrm.Dal;

namespace Zcrm.Ef;

public class ZcrmDbContext(DbContextOptions<ZcrmDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var contact = modelBuilder.Entity<Contact>();
        contact.HasKey(c => c.Id);
        contact.Property(c => c.Email).HasMaxLength(256).IsRequired();

        // The rule gives instant feedback; this index is the guarantee when two
        // users save the same email at the same moment.
        contact.HasIndex(c => c.Email).IsUnique();
    }
}
```

```csharp
// Zcrm.Ef/ContactRepository.cs
using Microsoft.EntityFrameworkCore;
using Zcrm.Dal;

namespace Zcrm.Ef;

public sealed class ContactRepository(ZcrmDbContext db) : IContactRepository
{
    public Task<Contact?> GetAsync(Guid id) =>
        db.Contacts.SingleOrDefaultAsync(c => c.Id == id);

    public void Add(Contact contact) => db.Contacts.Add(contact);

    public Task<bool> EmailInUseAsync(string email, Guid excludingContactId)
    {
        // Case-insensitive whatever the column collation is.
        var normalized = email.Trim().ToUpperInvariant();
        return db.Contacts.AnyAsync(c => c.Id != excludingContactId && c.Email.ToUpper() == normalized);
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
```

## Zcrm.Domain

**The command:** a `[Remote, Execute]` command. When the client calls it, the call crosses to the server, where the repository lives. When the server calls it (from the save check), it runs directly. Code generation turns it into a delegate, `ContactEmailUniqueness.IsEmailInUse(Guid, string)`, registered on both sides.

```csharp
// Zcrm.Domain/Contacts/ContactEmailUniqueness.cs
using Neatoo.RemoteFactory;
using Zcrm.Dal;

namespace Zcrm.Domain.Contacts;

/// <summary>Server lookup the email rule calls.</summary>
[Factory]
public static partial class ContactEmailUniqueness
{
    // Value parameters before [Service] parameters (RemoteFactory#135).
    [Remote, Execute]
    private static Task<bool> _IsEmailInUse(Guid contactId, string email, [Service] IContactRepository repository)
    {
        return repository.EmailInUseAsync(email, contactId);
    }
}
```

**The rule:** it takes the command delegate, never the repository. The `Contact` is built in the browser too, so a server-only dependency would break it there. The rule passes the contact's own `Id`, so a contact never counts as a duplicate of itself.

```csharp
// Zcrm.Domain/Contacts/UniqueEmailRule.cs
using Neatoo;
using Neatoo.Rules;

namespace Zcrm.Domain.Contacts;

internal interface IUniqueEmailRule : IRule<Contact> { }

/// <summary>Email must not belong to another contact. Checked against the database while editing.</summary>
internal class UniqueEmailRule : AsyncRuleBase<Contact>, IUniqueEmailRule
{
    private readonly ContactEmailUniqueness.IsEmailInUse _isEmailInUse;

    public UniqueEmailRule(ContactEmailUniqueness.IsEmailInUse isEmailInUse) : base(t => t.Email)
    {
        _isEmailInUse = isEmailInUse;
    }

    protected override async Task<IRuleMessages> Execute(Contact target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Email))
        {
            return None;  // [Required] reports the empty case
        }

        return await _isEmailInUse(target.Id, target.Email.Trim())
            ? (nameof(Contact.Email), $"Another contact already uses {target.Email.Trim()}.").AsRuleMessages()
            : None;
    }
}
```

**The domain model:** the duplicate result is an ordinary validation message on `Email`. It makes `IsValid` false, which disables Save, and the field shows the message. Some notes on the operations:

- `Create` sets the `Id` up front so the rule can always exclude it.
- `Create` runs the rules so an empty form is not savable. The trade-off is that the required-field messages show as soon as the form opens.
- `Insert` and `Update` re-run every rule on the server and refuse an invalid contact. That catches a duplicate created between the check and the save, and any caller that skipped the check.

```csharp
// Zcrm.Domain/Contacts/Contact.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Dal;
using ContactRow = Zcrm.Dal.Contact;

namespace Zcrm.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public partial interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}

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
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is not a valid address")]
    [StringLength(256)]
    public partial string? Email { get; set; }

    public Contact(IEntityBaseServices<Contact> services, IUniqueEmailRule uniqueEmailRule) : base(services)
    {
        RuleManager.AddRule(uniqueEmailRule);
    }

    [Create]
    public async Task Create()
    {
        Id = Guid.NewGuid();
        await RunRules(RunRulesFlag.All);  // no children; Email is empty, so no server call
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IContactRepository repository)
    {
        var row = await repository.GetAsync(id);
        if (row == null)
        {
            return false;
        }

        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] IContactRepository repository)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = new ContactRow { Id = Id };
        MapTo(row);
        repository.Add(row);
        await repository.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] IContactRepository repository)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await repository.GetAsync(Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");
        MapTo(row);
        await repository.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.FirstName = FirstName!.Trim();
        row.LastName = LastName!.Trim();
        row.Email = Email!.Trim();
    }
}
```

**Registration:** the rule classes are `internal`, so the domain registers them itself. Both sides must call this, because the `Contact` (and therefore its rule) is built in the browser as well as on the server.

```csharp
// Zcrm.Domain/DomainRegistration.cs
using Microsoft.Extensions.DependencyInjection;
using Zcrm.Domain.Contacts;

namespace Zcrm.Domain;

public static class DomainRegistration
{
    /// <summary>Call on both tiers, after AddNeatooServices.</summary>
    public static IServiceCollection AddZcrmDomainRules(this IServiceCollection services)
    {
        services.AddTransient<IUniqueEmailRule, UniqueEmailRule>();  // transient: a rule tracks execution state
        return services;
    }
}
```

## Zcrm.Server

**Why here:** this is the only place the repository and the `DbContext` are registered, so the command's repository parameter can only be resolved on the server. Don't also call `AddNeatooAspNetCore`: registering it after `AddNeatooServices(Server)` breaks every save that comes from the client (RemoteFactory#117).

```csharp
// Zcrm.Server/Program.cs
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Dal;
using Zcrm.Domain;
using Zcrm.Domain.Contacts;
using Zcrm.Ef;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ZcrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Zcrm")));
builder.Services.AddScoped<IContactRepository, ContactRepository>();

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);
builder.Services.AddZcrmDomainRules();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseNeatoo();  // the one endpoint every factory and command call goes through
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

## Zcrm.Client

**Program.cs:** the same rule registration as the server, but in Remote mode, so the email-check delegate becomes an HTTP call to the server. Nothing server-only is registered here.

```csharp
// Zcrm.Client/Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using Zcrm.Client;
using Zcrm.Client.ViewModels;
using Zcrm.Domain;
using Zcrm.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddZcrmDomainRules();
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

**ViewModel:** it holds the contact and saves it. It makes no decisions about whether the email is a duplicate; it reads the contact's `IsSavable`. `IsSaving` is there because `Save()` doesn't mark the contact busy, so a double-click would otherwise send two inserts (Neatoo#103).

```csharp
// Zcrm.Client/ViewModels/ContactEditViewModel.cs
using Zcrm.Domain.Contacts;

namespace Zcrm.Client.ViewModels;

/// <summary>ViewModel for the contact edit screen: holds the aggregate, saves it.</summary>
internal sealed class ContactEditViewModel(IContactFactory contactFactory)
{
    public IContact? Contact { get; private set; }

    public bool IsSaving { get; private set; }

    public async Task New() => Contact = await contactFactory.Create();

    public async Task Load(Guid id) => Contact = await contactFactory.Fetch(id);

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
            await Contact.WaitForTasks();  // an email check may still be in flight
            if (!Contact.IsSavable)
            {
                return;  // e.g. the email is taken; the message is already on the field
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

**The edit page:** the duplicate message appears under the Email field with no code on the page. `MudNeatooTextField` shows the property's messages, and the field is disabled while the check runs. The page only listens to the contact's own `PropertyChanged`, so the Save button updates.

The check fires when the field commits (Tab or clicking elsewhere), not on every keystroke. That is the component's default, and it means one server call per committed value. If the user clicks Save straight after typing, the field commits first, the save handler waits for the check, and the save is refused with the message showing.

```razor
@* Zcrm.Client/Pages/ContactEdit.razor *@
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@using System.ComponentModel
@using Zcrm.Client.ViewModels
@using Zcrm.Domain.Contacts
@using Neatoo.Blazor.MudNeatoo.Components
@implements IDisposable
@inject ContactEditViewModel Vm

<MudText Typo="Typo.h5" Class="mb-4">Contact</MudText>

@if (!_loaded)
{
    <MudProgressLinear Indeterminate="true" />
}
else if (Vm.Contact is null)
{
    <MudAlert Severity="Severity.Warning">Contact not found.</MudAlert>
}
else
{
    @* @key: Save returns a new instance; rebuild the fields so they bind to it. *@
    <div @key="Vm.Contact">
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" Class="mb-3" />

        @* "Another contact already uses ..." appears here as the field's error,
           from UniqueEmailRule. The field is disabled while the check runs. *@
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" Class="mb-3" />

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   Disabled="@(!Vm.CanSave)"
                   OnClick="SaveAsync">
            Save
        </MudButton>
    </div>
}

@code {
    [Parameter] public Guid? Id { get; set; }

    private bool _loaded;
    private IContact? _subscribed;

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

    // The fields refresh themselves; the page re-renders for the contact's own
    // property and meta changes so the Save button follows IsSavable.
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

This hasn't been compiled or tested, and I didn't write tests. I left out deleting contacts and handling the database error if the unique index is ever hit. Ask if you want any of those added.