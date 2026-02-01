// -----------------------------------------------------------------------------
// Design.Domain - [Remote] Attribute and Client-Server Boundary
// -----------------------------------------------------------------------------
// This file explains the [Remote] attribute - the critical marker that
// determines which operations cross from client to server.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryOperations;

// =============================================================================
// [Remote] - Client-to-Server Boundary Marker
// =============================================================================
// [Remote] marks factory methods that MUST execute on the server.
// Once execution crosses to the server, subsequent calls stay there.
//
// DESIGN DECISION: [Remote] is on methods, not classes.
// - Entry point marking: Client code knows which operations require server
// - Granular control: [Create] can be local; [Fetch] needs [Remote]
// - Clear boundary: Once crossed to server, execution stays there
//
// DID NOT DO THIS: Mark entire class as [Remote].
//
// REJECTED PATTERN:
//   [Remote]
//   public class Employee : EntityBase<Employee> {
//       // ALL methods would need to cross to server
//   }
//
// ACTUAL PATTERN:
//   public class Employee : EntityBase<Employee> {
//       [Create]           // Local OK - no [Remote]
//       public void Create() { }
//
//       [Remote][Fetch]    // Server required - has [Remote]
//       public void Fetch(int id, [Service] IRepo repo) { }
//   }
//
// WHY NOT: Class-level [Remote] would:
// 1. Force [Create] to go to server even when not needed
// 2. Require separate client/server class definitions
// 3. Break the natural pattern where methods declare their own requirements
//
// GENERATOR BEHAVIOR: For [Remote] methods, RemoteFactory generates:
//
// Client-side (HTTP proxy):
//   public async Task<Employee> Fetch(int id) {
//       var request = new { id };
//       var response = await httpClient.PostAsJsonAsync("/api/Employee/Fetch", request);
//       response.EnsureSuccessStatusCode();
//       return await response.Content.ReadFromJsonAsync<Employee>();
//   }
//
// Server-side (actual execution):
//   public Employee Fetch(int id) {
//       var obj = serviceProvider.GetRequiredService<Employee>();
//       var repo = serviceProvider.GetRequiredService<IRepo>();
//       obj.FactoryStart(FactoryOperation.Fetch);
//       obj.Fetch(id, repo);  // [Service] parameter resolved from DI
//       obj.FactoryComplete(FactoryOperation.Fetch);
//       return obj;
//   }
// =============================================================================

/// <summary>
/// Demonstrates: [Remote] boundary patterns.
/// </summary>
[Factory]
public partial class RemoteBoundaryDemo : EntityBase<RemoteBoundaryDemo>
{
    public partial int Id { get; set; }
    public partial string? Name { get; set; }

    public RemoteBoundaryDemo(IEntityBaseServices<RemoteBoundaryDemo> services) : base(services) { }

    // =========================================================================
    // Local Operation: No [Remote]
    // =========================================================================
    // [Create] doesn't need server access - runs wherever called.
    // - In Blazor WASM: runs on client
    // - In ASP.NET: runs on server
    // - In WPF calling server API: runs on client
    // =========================================================================
    [Create]
    public void Create()
    {
        // No persistence, no server-only services needed
        // Can run on client or server
    }

    // =========================================================================
    // Remote Operation: Needs [Remote]
    // =========================================================================
    // [Fetch] requires database access - must run on server.
    // The [Remote] attribute tells RemoteFactory to generate HTTP proxy.
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IRemoteDemoRepository repository)
    {
        // This method body runs on SERVER only.
        // repository is resolved from server's DI container.
        var data = repository.GetById(id);
        this["Id"].LoadValue(data.Id);
        this["Name"].LoadValue(data.Name);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IRemoteDemoRepository repository)
    {
        var generatedId = repository.Insert(Name!);
        this["Id"].LoadValue(generatedId);
    }

    [Remote]
    [Update]
    public void Update([Service] IRemoteDemoRepository repository)
    {
        repository.Update(Id, Name!);
    }

    [Remote]
    [Delete]
    public void Delete([Service] IRemoteDemoRepository repository)
    {
        repository.Delete(Id);
    }
}

// =============================================================================
// Constructor [Service] vs Method [Service]
// =============================================================================
// DESIGN DECISION: Service injection location determines availability.
//
// Constructor [Service]: Available on BOTH client AND server.
//     - Use for: IValidateBaseServices, IEntityBaseServices, shared config
//     - Resolved when object is created, regardless of location
//
// Method [Service]: Available ONLY on server (when method has [Remote]).
//     - Use for: IDbContext, external APIs, persistence services
//     - Client assemblies have stubs that throw "not registered"
//     - Forces correct [Remote] usage
//
// This is enforced at RUNTIME:
// - If you call a method with [Service] param without [Remote], and that
//   service isn't registered on client, you get a DI exception.
// - This naturally guides developers to add [Remote] where needed.
// =============================================================================

/// <summary>
/// Demonstrates: Constructor vs Method service injection.
/// </summary>
[Factory]
public partial class ServiceInjectionDemo : EntityBase<ServiceInjectionDemo>
{
    public partial string? Name { get; set; }

    private readonly ISharedConfiguration _config;

    // =========================================================================
    // Constructor Injection: Available Everywhere
    // =========================================================================
    // IEntityBaseServices is in BOTH client and server DI containers.
    // ISharedConfiguration (hypothetical) could also be registered on both.
    // =========================================================================
    public ServiceInjectionDemo(
        IEntityBaseServices<ServiceInjectionDemo> services,
        [Service] ISharedConfiguration config)
        : base(services)
    {
        _config = config;
        // config is available on client AND server
    }

    [Create]
    public void Create()
    {
        // Can use _config here - it's constructor-injected
        Name = _config.DefaultName;
    }

    // =========================================================================
    // Method Injection: Server-Only
    // =========================================================================
    // IDbContext is registered ONLY on server. If this method didn't have
    // [Remote] and was called on client, DI would throw "service not registered".
    // =========================================================================
    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IDbContext dbContext)
    {
        // dbContext only exists on server
        var data = dbContext.Find<EntityData>(id);
        this["Name"].LoadValue(data?.Name);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IDbContext dbContext)
    {
        dbContext.Add(new EntityData { Name = Name });
        dbContext.SaveChanges();
    }

    [Remote]
    [Update]
    public void Update([Service] IDbContext dbContext)
    {
        // Server-only operation
    }

    [Remote]
    [Delete]
    public void Delete([Service] IDbContext dbContext)
    {
        // Server-only operation
    }
}

// =============================================================================
// Entity Duality - Same Class as Root or Child
// =============================================================================
// An entity can be an aggregate root in one object graph and a child in another.
// The same class may have [Remote] methods for root scenarios while those
// same methods are NOT called when it's a child.
//
// Example:
// - Address as root: Can be fetched/saved independently
// - Address as child of Employee: Saved through Employee.Save()
//
// When Address is a child, its [Insert]/[Update]/[Delete] methods are called
// by the parent's persistence code, NOT through the factory.
// =============================================================================

/// <summary>
/// Demonstrates: Entity that can be root or child.
/// </summary>
[Factory]
public partial class DualUseEntity : EntityBase<DualUseEntity>
{
    public partial int Id { get; set; }
    public partial string? Street { get; set; }
    public partial string? City { get; set; }

    public DualUseEntity(IEntityBaseServices<DualUseEntity> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // When used as AGGREGATE ROOT:
    // These [Remote] methods are called through the factory.
    //
    // When used as CHILD:
    // These methods are NOT called through factory - parent coordinates.
    // The [Remote] attribute doesn't matter when called from server code.
    // =========================================================================

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IDualUseRepository repository)
    {
        // Called via factory when this is an aggregate root
        var data = repository.GetAddressById(id);
        this["Id"].LoadValue(data.Id);
        this["Street"].LoadValue(data.Street);
        this["City"].LoadValue(data.City);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IDualUseRepository repository)
    {
        // Called via factory when root, or by parent when child
        var newId = repository.InsertAddress(Street!, City!);
        this["Id"].LoadValue(newId);
    }

    [Remote]
    [Update]
    public void Update([Service] IDualUseRepository repository)
    {
        repository.UpdateAddress(Id, Street!, City!);
    }

    [Remote]
    [Delete]
    public void Delete([Service] IDualUseRepository repository)
    {
        repository.DeleteAddress(Id);
    }
}

// =============================================================================
// Blazor WASM Best Practice: Isolate EF Core
// =============================================================================
// DESIGN DECISION: Keep EF Core in a separate Infrastructure project.
// Use PrivateAssets="all" to prevent it from flowing to client assemblies.
//
// Project Structure:
//
// Infrastructure.csproj - Contains EF Core, not referenced by client
//   <PackageReference Include="Microsoft.EntityFrameworkCore" />
//
// Domain.csproj - References Infrastructure privately
//   <ProjectReference Include="..\Infrastructure\Infrastructure.csproj"
//                     PrivateAssets="all" />
//
// Server.csproj - Explicitly references both
//   <ProjectReference Include="..\Domain\Domain.csproj" />
//   <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
//
// Client.csproj - Only references Domain, never sees Infrastructure
//   <ProjectReference Include="..\Domain\Domain.csproj" />
//
// This ensures:
// - Domain classes can have [Service] IDbContext parameters
// - Client assembly compiles (IDbContext is just an interface reference)
// - Client runtime has no EF Core dependency
// - Method [Service] parameters are only resolved on server
// =============================================================================

// =============================================================================
// Support Types
// =============================================================================

public interface IRemoteDemoRepository
{
    (int Id, string Name) GetById(int id);
    int Insert(string name);
    void Update(int id, string name);
    void Delete(int id);
}

public interface ISharedConfiguration
{
    string DefaultName { get; }
}

public interface IDbContext
{
    T? Find<T>(object key) where T : class;
    void Add<T>(T entity) where T : class;
    void SaveChanges();
}

public class EntityData
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public interface IDualUseRepository
{
    (int Id, string Street, string City) GetAddressById(int id);
    int InsertAddress(string street, string city);
    void UpdateAddress(int id, string street, string city);
    void DeleteAddress(int id);
}
