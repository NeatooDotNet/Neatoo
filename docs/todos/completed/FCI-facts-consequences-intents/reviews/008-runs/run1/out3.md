I wrote it the way Neatoo is designed to handle this. The duplicate-email check is a **rule on the Contact entity**. The rule calls the server through a **`[Remote, Execute]` command**, and that command queries the DbContext. The page has no code for it: the Email field already shows the entity's messages.

## How it works, and the decisions behind it

- **The check runs when the Email field commits**, which means when the user tabs away or clicks elsewhere, not on every keystroke. `MudNeatooTextField` sets the value on blur, so each email the user commits costs one server call. While the check runs, the field is disabled and the Save button is too, because `IsSavable` includes `!IsBusy`. If you really want it to check as they type, that needs debouncing, and it's a separate decision to make.
- **The rule takes the command's delegate, not the DbContext.** Rules run in the browser. In the browser the delegate is an HTTP call; on the server it calls the method directly.
- **A contact never matches its own row.** The rule passes the contact's own `Id` to the query and excludes it. Changing an email back to the saved value is therefore fine.
- **Loading a contact doesn't run the check.** Rules don't fire during `[Fetch]`, so opening a contact makes no server call.
- **The server checks again before saving.** `[Insert]` and `[Update]` re-run every rule on the server and refuse to save if any fails. This catches a client that skips the check. It doesn't fully close the gap where two users save the same email at the same moment; only a unique index in the database does that (see the end).

## Projects

| Project | Referenced by | Holds |
|---|---|---|
| `zCRM.Data` (your existing code) | Domain, Server | EF `Contact` and `ZCrmDbContext` |
| `zCRM.Domain` | Client, Server | `IContact`/`Contact`, the `ContactEmail` command, `UniqueEmailRule`, rule registration |
| `zCRM.Server` | — | Program.cs: Neatoo in Server mode, the DbContext |
| `zCRM.Client` | — | Program.cs: Neatoo in Remote mode, the ViewModel, the page |

The domain project goes on both client and server because the same rule has to run in the browser (for the user) and on the server (for the save check). It references `zCRM.Data` so the server-side code can use the DbContext. The cost is that the client ships an EF reference. With IL trimming (`IsServerRuntime=false`), the server-only method bodies are removed from the client build.

### Assumed (your existing EF code)

I'm assuming your EF entity has a `Guid Id` key:

```csharp
// zCRM.Data — assumed shape, not new code
namespace zCRM.Data;

public class Contact
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
}

public class ZCrmDbContext(DbContextOptions<ZCrmDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
}
```

## Domain (`zCRM.Domain`)

**`Contacts/IContact.cs`**: the public interface. `Contact` itself is `internal`, and everything outside the domain uses this interface. It extends `IEntityRoot`, which provides `Save()` and `IsSavable`. It's `partial` so the source generator can add `SetEmail(...)` and the other setters.

```csharp
using Neatoo;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
public partial interface IContact : IEntityRoot
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}
```

**`Contacts/ContactEmail.cs`**: the command that crosses to the server. `[Remote]` sends the browser's call over HTTP. The DbContext is a `[Service]` parameter, so it's only resolved on the server. The generator produces a delegate named `ContactEmail.IsInUse`. The value parameters come before the `[Service]` one to avoid a RemoteFactory bug ([#135](https://github.com/NeatooDotNet/RemoteFactory/issues/135)).

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using zCRM.Data;

namespace zCRM.Domain.Contacts;

/// <summary>Server-side email lookups for the Contact aggregate.</summary>
[Factory]
public static partial class ContactEmail
{
    /// <summary>True when a contact other than <paramref name="excludingContactId"/> has this email.</summary>
    [Remote, Execute]
    private static async Task<bool> _IsInUse(
        string email,
        Guid excludingContactId,
        [Service] ZCrmDbContext db)
    {
        // Case sensitivity follows the column collation (SQL Server's default is case-insensitive).
        return await db.Contacts.AnyAsync(c => c.Email == email && c.Id != excludingContactId);
    }
}
```

**`Contacts/UniqueEmailRule.cs`**: the rule. It runs whenever `Email` is set. A duplicate becomes a message on the Email property, which makes the contact invalid. It doesn't throw an exception. It has its own DI interface so `Contact` can get it from DI and tests can swap it out.

```csharp
using Neatoo.Rules;

namespace zCRM.Domain.Contacts;

internal interface IUniqueEmailRule : IRule<Contact> { }

/// <summary>Email must not belong to another contact. Checked against the database.</summary>
internal sealed class UniqueEmailRule : AsyncRuleBase<Contact>, IUniqueEmailRule
{
    private readonly ContactEmail.IsInUse _isInUse;

    public UniqueEmailRule(ContactEmail.IsInUse isInUse) : base(t => t.Email)
    {
        _isInUse = isInUse;
    }

    protected override async Task<IRuleMessages> Execute(Contact target, CancellationToken? token = null)
    {
        if (string.IsNullOrWhiteSpace(target.Email))
        {
            return None;  // [Required] reports the empty case
        }

        // A new contact's Id is Guid.Empty, which excludes nothing.
        if (await _isInUse(target.Email.Trim(), target.Id))
        {
            return (nameof(Contact.Email), $"Another contact already uses {target.Email.Trim()}.").AsRuleMessages();
        }

        return None;
    }
}
```

**`Contacts/Contact.cs`**: the entity and its factory methods:
- **Create** runs in the browser.
- **Fetch, Insert and Update** are `[Remote]` and use the DbContext on the server.
- **Fetch** only assigns values. The object is paused during it, so nothing is marked modified and no rule runs.
- **Insert and Update** re-run all rules on the server before writing.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Data;
using ContactRow = zCRM.Data.Contact;

namespace zCRM.Domain.Contacts;

/// <summary>Contact aggregate root.</summary>
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
    [StringLength(256)]
    public partial string? Email { get; set; }

    public Contact(IEntityBaseServices<Contact> services, IUniqueEmailRule uniqueEmailRule)
        : base(services)
    {
        RuleManager.AddRule(uniqueEmailRule);
    }

    [Create]
    public void Create()
    {
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] ZCrmDbContext db)
    {
        var row = await db.Contacts.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
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
    internal async Task Insert([Service] ZCrmDbContext db)
    {
        // Server gate: includes the uniqueness rule. Throw, never return.
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        Id = Guid.NewGuid();

        var row = new ContactRow();
        MapTo(row);
        db.Contacts.Add(row);
        await db.SaveChangesAsync();
    }

    [Remote, Update]
    internal async Task Update([Service] ZCrmDbContext db)
    {
        await RunRules(RunRulesFlag.All);
        if (!IsValid)
        {
            throw new SaveOperationException(SaveFailureReason.IsInvalid);
        }

        var row = await db.Contacts.SingleOrDefaultAsync(c => c.Id == Id)
            ?? throw new KeyNotFoundException($"Contact {Id} not found");

        MapTo(row);
        await db.SaveChangesAsync();
    }

    private void MapTo(ContactRow row)
    {
        row.Id = Id;
        row.FirstName = FirstName!.Trim();
        row.LastName = LastName!.Trim();
        row.Email = Email!.Trim();
    }
}
```

**`DomainRegistration.cs`**: registers the rule. Rule types are `internal`, so the domain registers them itself. **Call this on both tiers**: `Contact` is built in the browser too, and its constructor needs the rule.

```csharp
using Microsoft.Extensions.DependencyInjection;
using zCRM.Domain.Contacts;

namespace zCRM.Domain;

public static class DomainRegistration
{
    public static IServiceCollection AddZCrmDomainRules(this IServiceCollection services)
    {
        // Transient: a rule tracks execution state per entity instance.
        services.AddTransient<IUniqueEmailRule, UniqueEmailRule>();
        return services;
    }
}
```

## Server (`zCRM.Server/Program.cs`)

The server registers Neatoo in Server mode and maps the single endpoint that every factory call goes through; there are no controllers. Don't also call `AddNeatooAspNetCore`: registering it after `AddNeatooServices` breaks every remote save ([RemoteFactory#117](https://github.com/NeatooDotNet/RemoteFactory/issues/117)). I assumed SQL Server; change the provider if yours differs.

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using zCRM.Data;
using zCRM.Domain;
using zCRM.Domain.Contacts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ZCrmDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("zCRM")));

builder.Services.AddNeatooServices(NeatooFactory.Server, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```

## Client (`zCRM.Client`)

**`Program.cs`**: Neatoo in Remote mode, so `[Remote]` calls, including the email check, go to the server. It also registers the domain rules again and the `HttpClient` the factories use.

```csharp
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

builder.Services.AddNeatooServices(NeatooFactory.Remote, typeof(IContact).Assembly);
builder.Services.AddZCrmDomainRules();

builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddTransient<ContactEditViewModel>();

await builder.Build().RunAsync();
```

**`ViewModels/ContactEditViewModel.cs`**: holds the contact and saves it. It makes no validation decisions. `CanSave` just reads `IsSavable`. `Save()` returns a new instance, so the ViewModel replaces its reference. `IsSaving` covers a known gap: `Save()` doesn't mark the entity busy, so a double click would otherwise send two inserts ([Neatoo#103](https://github.com/NeatooDotNet/Neatoo/issues/103)).

```csharp
using zCRM.Domain.Contacts;

namespace zCRM.Client.ViewModels;

/// <summary>ViewModel for the Contact edit screen: holds the aggregate, saves it.</summary>
internal sealed class ContactEditViewModel
{
    private readonly IContactFactory _contactFactory;

    public ContactEditViewModel(IContactFactory contactFactory)
    {
        _contactFactory = contactFactory;
    }

    public IContact? Contact { get; private set; }

    public bool IsSaving { get; private set; }

    public bool CanSave => !IsSaving && (Contact?.IsSavable ?? false);

    public void New()
    {
        Contact = _contactFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Contact = await _contactFactory.Fetch(id);
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
            // An email committed by the Save click itself may still be checking.
            await Contact.WaitForTasks();

            if (!Contact.IsSavable)
            {
                return;  // e.g. duplicate email; the message is already on the field
            }

            Contact = (IContact)await Contact.Save();
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

**`Pages/ContactEdit.razor`**: how the message is displayed. Nothing on this page is specific to the duplicate check. `MudNeatooTextField` shows the Email property's messages under the field, and the summary repeats them. The page subscribes to the contact's change events only because it displays the Save button.

```razor
@page "/contacts/new"
@page "/contacts/{Id:guid}"
@implements IDisposable
@using zCRM.Domain.Contacts
@using zCRM.Client.ViewModels
@using Neatoo
@using Neatoo.Blazor.MudNeatoo.Components
@using Neatoo.Blazor.MudNeatoo.Validation
@inject ContactEditViewModel Vm

@if (Vm.Contact is null)
{
    <MudText>Contact not found.</MudText>
}
else
{
    @* @key: Save returns a new instance; rebuild the fields so they bind to it. *@
    <div @key="Vm.Contact">
        <NeatooValidationSummary Entity="@Vm.Contact" ShowHeader="false" Dense="true" Class="mb-4" />

        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.FirstName)]" Variant="Variant.Outlined" Class="mb-3" />
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.LastName)]" Variant="Variant.Outlined" Class="mb-3" />

        @* On blur the uniqueness rule calls the server; the field is disabled while it
           runs and shows "Another contact already uses ..." under itself if taken. *@
        <MudNeatooTextField T="string" EntityProperty="@Vm.Contact[nameof(IContact.Email)]" Variant="Variant.Outlined" Class="mb-3" />

        <MudButton Variant="Variant.Filled" Color="Color.Primary" Disabled="@(!Vm.CanSave)" OnClick="SaveAsync">
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
            Vm.New();
        }

        Resubscribe();
    }

    private async Task SaveAsync()
    {
        await Vm.Save();
        Resubscribe();
        StateHasChanged();
    }

    // The fields refresh themselves; the page re-renders for the Save button.
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

## Before you use it

- **Nothing here is compiled.** You asked for code in the reply, not files, so it hasn't been built. The patterns follow the Neatoo and RemoteFactory reference examples. The `using` namespaces (`Neatoo.Rules`, `Neatoo.RemoteFactory`) are my best match and may need adjusting.
- **Add a unique index on `Contacts.Email`.** It's the only thing that stops two saves of the same email at the same moment. It's a change to your existing DbContext: `modelBuilder.Entity<Contact>().HasIndex(c => c.Email).IsUnique();` plus a migration. With the index, the losing save fails with a `DbUpdateException`, not a message on the field.
- **Case-insensitive matching depends on your database.** The query compares emails exactly and relies on the column's collation, which is case-insensitive by default on SQL Server. On PostgreSQL you'd need `citext` or to store emails lowercased.