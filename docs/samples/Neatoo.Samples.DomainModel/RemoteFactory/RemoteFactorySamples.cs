/// <summary>
/// Code samples for docs/remote-factory.md
///
/// Snippets in this file:
/// - remote-attribute-patterns: Entity with [Remote] operations
/// - aggregate-vs-child-patterns: Aggregate root vs child entity patterns
/// - email-validation-rule: Sync rule for email validation
/// - unique-email-rule-async: Async rule with service dependency
/// - client-email-service: Client-side HTTP service implementation
/// - server-email-service: Server-side database service implementation
/// - authorization-pattern: Authorization class implementation
/// </summary>

using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;

namespace Neatoo.Samples.DomainModel.RemoteFactory;

#region remote-attribute-patterns
/// <summary>
/// Aggregate root entity with [Remote] operations.
/// All CRUD operations are callable from the client.
/// </summary>
public partial interface IRemotePerson : IEntityBase
{
    int Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}

[Factory]
internal partial class RemotePerson : EntityBase<RemotePerson>, IRemotePerson
{
    public RemotePerson(IEntityBaseServices<RemotePerson> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? FirstName { get; set; }
    public partial string? LastName { get; set; }
    public partial string? Email { get; set; }

    [Remote]  // Callable from client
    [Fetch]
    public void Fetch(int id)
    {
        Id = id;
        FirstName = "John";
        LastName = "Doe";
        Email = "john@example.com";
    }

    [Remote]
    [Insert]
    public Task Insert() => Task.CompletedTask;

    [Remote]
    [Update]
    public Task Update() => Task.CompletedTask;

    [Remote]
    [Delete]
    public Task Delete() => Task.CompletedTask;

    [Create]
    public void Create() { }
}
#endregion

#region aggregate-vs-child-patterns
/// <summary>
/// Aggregate Root - needs [Remote] for client-server communication.
/// </summary>
public partial interface IRemoteOrder : IEntityBase
{
    int Id { get; }
    string? CustomerName { get; set; }
}

[Factory]
internal partial class RemoteOrder : EntityBase<RemoteOrder>, IRemoteOrder
{
    public RemoteOrder(IEntityBaseServices<RemoteOrder> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? CustomerName { get; set; }

    [Remote]
    [Fetch]
    public void Fetch(int id)
    {
        // Load order from database
        Id = id;
        CustomerName = "Acme Corp";
    }

    [Remote]
    [Update]
    public Task Update()
    {
        // Save order and all children
        return Task.CompletedTask;
    }

    [Create]
    public void Create() { }
}

/// <summary>
/// Child Entity - NO [Remote] needed.
/// Saved through parent's operations.
/// </summary>
public partial interface IRemoteOrderLineItem : IEntityBase
{
    int Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
}

[Factory]
internal partial class RemoteOrderLineItem : EntityBase<RemoteOrderLineItem>, IRemoteOrderLineItem
{
    public RemoteOrderLineItem(IEntityBaseServices<RemoteOrderLineItem> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string? ProductName { get; set; }
    public partial int Quantity { get; set; }

    [Fetch]  // Called by Order.Fetch internally - no [Remote]
    public void Fetch(int id, string productName, int quantity)
    {
        Id = id;
        ProductName = productName;
        Quantity = quantity;
    }

    [Insert]  // Called by Order.Update internally - no [Remote]
    public Task Insert()
    {
        return Task.CompletedTask;
    }

    [Create]
    public void Create() { }
}
#endregion

#region email-validation-rule
/// <summary>
/// Interface for entities with email validation.
/// </summary>
public interface IPersonWithEmail : IValidateBase
{
    string? Email { get; set; }
}

/// <summary>
/// Sync validation rule for email format.
/// Executes on both client and server.
/// </summary>
public class EmailFormatValidationRule : RuleBase<IPersonWithEmail>
{
    public EmailFormatValidationRule() : base(p => p.Email) { }

    protected override IRuleMessages Execute(IPersonWithEmail target)
    {
        if (string.IsNullOrEmpty(target.Email))
            return (nameof(target.Email), "Email is required").AsRuleMessages();

        if (!target.Email.Contains('@'))
            return (nameof(target.Email), "Invalid email format").AsRuleMessages();

        return None;
    }
}
#endregion

#region unique-email-rule-async
/// <summary>
/// Service interface for email uniqueness checking.
/// Different implementations for client vs server.
/// </summary>
public interface IEmailExistsService
{
    Task<bool> ExistsAsync(string email);
}

/// <summary>
/// Async rule that checks email uniqueness via service.
/// Service implementation differs on client vs server.
/// </summary>
public class UniqueEmailAsyncRule : AsyncRuleBase<IPersonWithEmail>
{
    private readonly IEmailExistsService _emailService;

    public UniqueEmailAsyncRule(IEmailExistsService emailService) : base(p => p.Email)
    {
        _emailService = emailService;
    }

    protected override async Task<IRuleMessages> Execute(IPersonWithEmail target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        // On client: calls server API
        // On server: calls database directly
        if (await _emailService.ExistsAsync(target.Email))
            return (nameof(target.Email), "Email already exists").AsRuleMessages();

        return None;
    }
}
#endregion

#region client-email-service
/// <summary>
/// Client-side email service that calls the server API.
/// Registered in Blazor WASM DI container.
/// </summary>
public class ClientEmailExistsService : IEmailExistsService
{
    private readonly HttpClient _http;

    public ClientEmailExistsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        // Call server API endpoint
        var uri = new Uri($"/api/email/exists?email={Uri.EscapeDataString(email)}", UriKind.Relative);
        var response = await _http.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}
#endregion

#region server-email-service
/// <summary>
/// Server-side email service that queries the database.
/// Registered in ASP.NET Core DI container.
/// </summary>
public class ServerEmailExistsService : IEmailExistsService
{
    private readonly IUserRepository _repository;

    public ServerEmailExistsService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        return await _repository.EmailExistsAsync(email);
    }
}

/// <summary>
/// Repository interface for user data access.
/// </summary>
public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
}
#endregion

#region authorization-pattern
/// <summary>
/// Authorization interface for Person entity operations.
/// </summary>
public interface IPersonAuth
{
    bool CanCreate();
    bool CanFetch();
    bool CanInsert();
    bool CanUpdate();
    bool CanDelete();
}

/// <summary>
/// Authorization implementation that checks user permissions.
/// Injected via DI on the server.
/// </summary>
public class PersonAuth : IPersonAuth
{
    private readonly IUserPermissions _user;

    public PersonAuth(IUserPermissions user) => _user = user;

    public bool CanCreate() => _user.HasPermission("Person.Create");
    public bool CanFetch() => _user.HasPermission("Person.Read");
    public bool CanInsert() => _user.HasPermission("Person.Create");
    public bool CanUpdate() => _user.HasPermission("Person.Update");
    public bool CanDelete() => _user.HasPermission("Person.Delete");
}

/// <summary>
/// Interface for checking user permissions.
/// </summary>
public interface IUserPermissions
{
    bool HasPermission(string permission);
}
#endregion
