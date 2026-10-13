Everything needed is in hand. Here is the full design and code.

**Shape in one paragraph.** The uniqueness check is an async rule on the `Contact` entity, triggered by the `Email` property. The rule cannot touch the database because entity code also runs in the browser, so it calls a `[Remote, Execute]` command delegate. In the browser that delegate crosses to the server; on the server it calls the method directly, which is how the same rule also runs inside the server-side save gate. The repository that queries EF Core lives only on the server. The page binds the `Email` field through `MudNeatooTextField`, which commits on blur, shows the rule's message as the field error, and disables itself while the check is in flight.

## 1. Domain assembly (shared by client and server)

### `zCRM.Domain/Contacts/IContact.cs`

The public interface is what every other layer references. It extends `IEntityRoot` so only this root exposes `Save()` and `IsSavable`.

```csharp
using Neatoo;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}
```

### `zCRM.Domain/Contacts/IContactRepository.cs`

The domain owns the persistence contract so the entity's operations and the lookup command compile on both tiers without referencing EF Core. The implementation is server-only. `ContactData` is a plain record so the EF entity never leaves the server.

```csharp
namespace zCRM.Domain.Contacts;

/// <summary>Row shape the Contact aggregate reads and writes. Keeps the EF entity on the server.</summary>
public sealed record ContactData(Guid Id, string FirstName, string LastName, string Email);

/// <summary>Repository for Contact aggregates. Implemented on the server over the DbContext.</summary>
public interface IContactRepository
{
    /// <summary>True when a contact other than <paramref name="exceptContactId"/> already has this email.</summary>
    Task<bool> EmailInUseAsync(string email, Guid exceptContactId, CancellationToken cancellationToken = default);

    Task<ContactData?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task InsertAsync(ContactData contact, CancellationToken cancellationToken = default);
    Task UpdateAsync(ContactData contact, CancellationToken cancellationToken = default);
}
```

### `zCRM.Domain/Contacts/ContactEmailLookup.cs`

The command is the seam the rule calls. `[Remote]` makes a client call cross to the server. The repository is a method `[Service]`, so it resolves only where the method body runs. The generator emits a public `ContactEmailLookup.IsEmailInUse` delegate from the private underscore method.

```csharp
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

/// <summary>Server lookup the email-uniqueness rule calls. [Remote]: a client call crosses to the server.</summary>
[Factory]
public static partial class ContactEmailLookup
{
    [Remote]
    [Execute]
    private static Task<bool> _IsEmailInUse(
        Guid contactId,
        string email,
        [Service] IContactRepository repository)
    {
        return repository.EmailInUseAsync(email, exceptContactId: contactId);
    }
}
```

### `zCRM.Domain/Contacts/EmailMustBeUniqueRule.cs`

The rule is a class because it has a dependency. It takes the command delegate, never the repository, so the entity can be constructed in the browser. The interface lets the entity receive it from DI and lets tests substitute it. The message is attached to `Email`, which is what the field displays.

```csharp
using Neatoo;
using Neatoo.Rules;

namespace zCRM.Domain.Contacts;

/// <summary>DI interface for the rule so the entity takes it from the container.</summary>
internal interface IEmailMustBeUniqueRule : IRule<Contact> { }

/// <summary>BR-CON-001: no two contacts share an email. Checked against the database on every committed Email value.</summary>
internal sealed class EmailMustBeUniqueRule : AsyncRuleBase<Contact>, IEmailMustBeUniqueRule
{
    private readonly ContactEmailLookup.IsEmailInUse _isEmailInUse;

    public EmailMustBeUniqueRule(ContactEmailLookup.IsEmailInUse isEmailInUse) : base(t => t.Email)
    {
        _isEmailInUse = isEmailInUse;
    }

    protected override async Task<IRuleMessages> Execute(Contact target, CancellationToken? token = null)
    {
        var email = target.Email?.Trim();

        if (string.IsNullOrEmpty(email))
        {
            return None;  // [Required] covers the blank case; no round-trip for it
        }

        // Excludes the contact being edited, so an existing contact's own email is not reported as taken.
        var inUse = await _isEmailInUse(target.Id, email);

        return inUse
            ? (nameof(Contact.Email), $"Another contact already has the email {email}.").AsRuleMessages()
            : None;
    }
}
```

### `zCRM.Domain/Contacts/Contact.cs`

The entity carries the rule, so the answer is on the `Email` property the moment it is committed, with no save attempt. `[Create]` is local. `[Fetch]`, `[Insert]` and `[Update]` are `[Remote]` and `internal`, and take the repository as a method service so it resolves on the server. The gate in `Insert` and `Update` re-runs every rule on the server, including the uniqueness rule through the same delegate, and refuses an invalid aggregate.

```csharp
using System.ComponentModel.DataAnnotations;
using Neatoo;
using Neatoo.RemoteFactory;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
[Factory]
internal partial class Contact : EntityBase<Contact>, IContact
{
    public partial Guid Id { get; private set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public partial string? FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public partial string? LastName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [StringLength(254)]
    public partial string? Email { get; set; }

    // The rule comes from DI through its interface; its dependency (the command
    // delegate) exists on both tiers, so the entity builds in the browser too.
    public Contact(
        IEntityBaseServices<Contact> services,
        IEmailMustBeUniqueRule emailMustBeUnique) : base(services)
    {
        RuleManager.AddRule(emailMustBeUnique);
    }

    [Create]
    public async Task Create()
    {
        // A blank form should show its required fields; rules do not fire inside
        // an operation, so this operation runs them. The uniqueness rule returns
        // at once for a blank email, so there is no server call here.
        await RunRules(RunRulesFlag.All);
    }

    [Remote]
    [Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IContactRepository repository)
    {
        var row = await repository.GetAsync(id);
        if (row is null)
        {
            return false;  // factory returns null
        }

        // Paused inside the operation: plain assignment is the clean baseline.
        Id = row.Id;
        FirstName = row.FirstName;
        LastName = row.LastName;
        Email = row.Email;
        return true;
    }

    [Remote]
    [Insert]
    internal async Task Insert([Service] IContactRepository repository)
    {
        await Gate();
        Id = Guid.NewGuid();
        await repository.InsertAsync(ToData());
    }

    [Remote]
    [Update]
    internal async Task Update([Service] IContactRepository repository)
    {
        await Gate();
        await repository.UpdateAsync(ToData());
    }

    // Server gate: re-run every rule here, including the uniqueness check (the
    // delegate calls the server method directly on this tier), and refuse an
    // invalid aggregate. Throw, never return: the framework marks the entity
    // saved after the operation returns.
    private async Task Gate()
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }
    }

    private ContactData ToData() => new(Id, FirstName!.Trim(), LastName!.Trim(), Email!.Trim());
}
```

### `zCRM.Domain/DomainRegistration.cs`

Rule types are `internal`, so the domain assembly registers them. Called on both tiers, because the entity is constructed on both.

```csharp
using Microsoft.Extensions.DependencyInjection;
using zCRM.Domain.Contacts;

namespace zCRM.Domain;

public static class DomainRegistration
{
    /// <summary>Call on BOTH tiers, after AddNeatooServices.</summary>
    public static IServiceCollection AddZCrmDomainRules(this IServiceCollection services)
    {
        // Transient: a rule tracks execution state per entity instance.
        services.AddTransient<IEmailMustBeUniqueRule, EmailMustBeUniqueRule>();
        return services;
    }
}
```

## 2. Server

### `zCRM.Server/Data/EfContactRepository.cs`

The only place the Neatoo `Contact` and the EF `Contact` meet, so the EF entity is aliased. The email comparison is case-insensitive and excludes the contact being edited. I assume the EF entity's key is a `Guid Id`; adjust the alias and key if yours differs.

```csharp
using Microsoft.EntityFrameworkCore;
using zCRM.Domain.Contacts;
using ContactRow = zCRM.Server.Data.Contact;   // the existing EF entity

namespace zCRM.Server.Data;

/// <summary>EF Core implementation of the Contact repository.</summary>
internal sealed class EfContactRepository : IContactRepository
{
    private readonly CrmDbContext _db;

    public EfContactRepository(CrmDbContext db)
    {
        _db = db;
    }

    public Task<bool> EmailInUseAsync(string email, Guid exceptContactId, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        return _db.Contacts
            .AsNoTracking()
            .AnyAsync(c => c.Id != exceptContactId
                        && c.Email != null
                        && c.Email.ToLower() == normalized,
                      cancellationToken);
    }

    public async Task<ContactData?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        return row is null ? null : new ContactData(row.Id, row.FirstName, row.LastName, row.Email);
    }

    public async Task InsertAsync(ContactData contact, CancellationToken cancellationToken = default)
    {
        _db.Contacts.Add(new ContactRow
        {
            Id = contact.Id,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Email = contact.Email,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ContactData contact, CancellationToken cancellationToken = default)
    {
        var row = await _db.Contacts.SingleAsync(c => c.Id == contact.Id, cancellationToken);
        row.FirstName = contact.FirstName;
        row.LastName = contact.LastName;
        row.Email = contact.Email;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

Two saves racing past the rule can still both pass. The database is the final arbiter, so add a unique index on the column in your `OnModelCreating`:

```csharp
modelBuilder.Entity<Contact>().HasIndex(c => c.Email).IsUnique();
```

### `zCRM.Server/Program.cs`

Neatoo in Server mode registers the generated factories and what `UseNeatoo()` resolves. Do not also call `AddNeatooAspNetCore` after it; that combination breaks every remote save (RemoteFactory issue 117). The repository is scoped to match the DbContext.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Domain;
using zCRM.Domain.Contacts;
using zCRM.Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Crm")));

// Neatoo + generated factories, Server mode. Do NOT add AddNeatooAspNetCore after this.
builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

// Server-only [Service] dependencies.
builder.Services.AddScoped<IContactRepository, EfContactRepository>();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseNeatoo();   // maps the /api/neatoo endpoint the client proxies call

app.MapFallbackToFile("index.html");
app.Run();
```

## 3. Client (Blazor WebAssembly)

### `zCRM.Client/Program.cs`

Remote mode makes the generated factories proxies. The same domain rule registration runs here because the entity is built in the browser. The keyed `HttpClient` is the transport the proxies use.

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Client;
using zCRM.Client.ViewModels;
using zCRM.Domain;
using zCRM.Domain.Contacts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

### `zCRM.Client/ViewModels/ContactEditViewModel.cs`

The ViewModel holds the live aggregate and forwards gestures. It decides nothing: `CanSave` is a read of `IsSavable`, which is already false while the email check is busy or after it fails. `IsSaving` guards the double-click gap during the remote save (Neatoo issue 103).

```csharp
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

/// <summary>ViewModel for the Contact edit screen: holds the aggregate, saves it.</summary>
public sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;

    public ContactEditViewModel(IContactFactory contactFactory)
    {
        _contactFactory = contactFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IContact? Contact { get; private set; }

    /// <summary>A save round-trip is in flight; Save() does not mark the entity busy itself.</summary>
    public bool IsSaving { get; private set; }

    public async Task New()
    {
        Contact = await _contactFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
    }

    /// <summary>IsSavable = IsValid && (IsModified || IsNew) && !IsBusy. The email check sets IsBusy and IsValid.</summary>
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
            await Contact.WaitForTasks();   // let an in-flight email check finish

            if (!Contact.IsSavable)
            {
                return;                     // the message is already on the Email property
            }

            Contact = (IContact)await Contact.Save();   // reassign: Save returns a new instance
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

### `zCRM.Client/Pages/ContactEdit.razor`

The `Email` field is a `MudNeatooTextField`. It commits on blur, so the rule makes one server call per committed value. It disables itself while the rule is busy and shows the rule's message as its error text, so the user sees "Another contact already has the email ..." as soon as the check returns and before any save. The progress bar and the summary are extra bindings of the same property state. The page owns the entity subscription so the Save button re-renders when the check completes.

```razor
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@using zCRM.Client.ViewModels
@using zCRM.Domain.Contacts
@inject ContactEditViewModel Vm

<MudText Typo="Typo.h5" Class="mb-4">@(Id is null ? "New Contact" : "Edit Contact")</MudText>

@if (Vm.Contact is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    @* @key: Save returns a new instance; keying rebuilds the fields so each
       subscribes to the new instance's property wrappers. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]"
                            Variant="Variant.Outlined"
                            Class="mb-3" />

        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Contact[nameof(IContact.LastName)]"
                            Variant="Variant.Outlined"
                            Class="mb-3" />

        @* Commits on blur. Disabled while the uniqueness rule runs (IsBusy);
           shows the rule's message as the field error (PropertyMessages). *@
        <MudNeatooTextField T="string"
                            EntityProperty="@Vm.Contact[nameof(IContact.Email)]"
                            Variant="Variant.Outlined"
                            HelperText="Must not be used by another contact"
                            Class="mb-1" />

        @if (Vm.Contact[nameof(IContact.Email)].IsBusy)
        {
            <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-1" />
            <MudText Typo="Typo.caption" Class="mb-3">Checking email…</MudText>
        }

        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   Disabled="@(!Vm.CanSave)"
                   OnClick="SaveAsync"
                   Class="mt-3">
            Save
        </MudButton>
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

    // The fields subscribe to their own property wrappers; the Save button,
    // the summary and the busy indicator read the entity, so the page
    // re-renders on the graph-wide event.
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
        await Vm.Save();
        Resubscribe();     // the ViewModel now holds the instance Save returned
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

## What happens at runtime

1. The user types an email and tabs out. The field calls `SetValue`, which sets `Email` and starts the rule.
2. `IsBusy` goes true on the property and the root. The field disables, the progress bar shows, and `IsSavable` turns false so Save is disabled.
3. The delegate posts to the server endpoint, the repository runs one `AnyAsync` against the Contacts table excluding this contact's id, and the answer comes back.
4. If taken, the message lands on `Email`. The field shows it in red, the summary lists it, and Save stays disabled until the email changes to one that passes.
5. On Save, the server `Insert` or `Update` re-runs the same rule through the same delegate as a gate, and the unique index covers the remaining race.

Assumptions worth checking against your repo: the EF entity is `zCRM.Server.Data.Contact` with a `Guid Id` key, the DbContext is `CrmDbContext`, and the client project's `_Imports.razor` can take the three MudNeatoo usings globally instead of per page.
