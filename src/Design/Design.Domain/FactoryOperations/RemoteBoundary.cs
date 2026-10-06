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
// [Remote] marks a client entry point: when the client calls the operation,
// the call crosses to the server. It does not mean "this code runs on the
// server" - internal, non-[Remote] operations (every child operation) run on
// the server too, because they are only ever called from code already there.
// Once execution crosses to the server, subsequent calls stay there.
//
// DESIGN DECISION: [Remote] is on methods, not classes.
// - Entry point marking: the root operations the client calls carry it
// - Granular control: a [Create] that needs nothing from the server stays local
// - Clear boundary: once crossed to the server, execution stays there
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
//   internal partial class Employee : EntityBase<Employee>, IEmployee {
//       [Create]           // Local - no [Remote]
//       public void Create() { }
//
//       [Remote][Fetch]    // Client entry point - the call crosses to the server
//       internal void Fetch(int id, [Service] IRepo repo) { }
//   }
//
// WHY NOT: Class-level [Remote] would:
// 1. Force [Create] to go to server even when not needed
// 2. Require separate client/server class definitions
// 3. Break the natural pattern where methods declare their own requirements
//
// GENERATOR BEHAVIOR: For a [Remote] operation, the generated factory has two
// paths and picks one at runtime by how the tier registered Neatoo
// (NeatooFactory.Remote on the client, NeatooFactory.Server on the server):
//
// - Client: sends the delegate type and the arguments to the single
//   POST /api/neatoo endpoint and deserializes the returned object.
// - Server: resolves the object and the [Service] parameters from DI, calls
//   FactoryStart, your method, then FactoryComplete, and returns the object.
//
// The method body is internal, so the IL trimmer removes it from the
// published client. See Generated/Neatoo.Generator/Neatoo.Factory/ for the
// real output.
// =============================================================================

/// <summary>
/// Demonstrates: [Remote] boundary patterns.
/// </summary>
[Factory]
internal partial class RemoteBoundaryDemo : EntityBase<RemoteBoundaryDemo>, IRemoteBoundaryDemo
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
    #region skill-remote-entry-point
    [Create]
    public void Create()
    {
        // No persistence, no server-only services needed
        // Can run on client or server
    }

    // The client fetches this root, so it is a client entry point: [Remote]
    // makes the client call cross to the server, where the repository lives.
    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IRemoteDemoRepository repository)
    {
        // This method body runs on SERVER only.
        // repository is resolved from server's DI container.
        var data = repository.GetById(id);
        Id = data.Id;
        Name = data.Name;
    }
    #endregion

    [Remote]
    [Insert]
    internal void Insert([Service] IRemoteDemoRepository repository)
    {
        var generatedId = repository.Insert(Name!);
        Id = generatedId;
    }

    [Remote]
    [Update]
    internal void Update([Service] IRemoteDemoRepository repository)
    {
        repository.Update(Id, Name!);
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IRemoteDemoRepository repository)
    {
        repository.Delete(Id);
    }
}

// =============================================================================
// Constructor [Service] vs Method [Service]
// =============================================================================
// DESIGN DECISION: Service injection location determines availability.
//
// Constructor [Service]: resolved on every tier that builds the object.
//     - Must be registered on BOTH client and server
//     - Use for: IValidateBaseServices, IEntityBaseServices, shared config,
//       rules and [Execute] command delegates
//     - Never a server-only service (DbContext, repository): the client
//       builds the object too, and resolution fails there
//
// Method [Service]: resolved on the tier where the operation runs.
//     - On the server for a [Remote] root operation and for an internal
//       child operation (both run only on the server)
//     - On whichever tier calls a local [Create] - so a local [Create]
//       takes only services registered on both tiers, such as a child list
//       factory
//     - Use server-only services (IDbContext, external APIs) only on
//       operations that run on the server
//
// Enforcement:
// - Internal operations are guarded in the generated factory: calling one on
//   the client throws "Server-only method called in non-server runtime."
// - A local operation that asks for a service the client does not register
//   fails with a DI exception when the client calls it.
// =============================================================================

/// <summary>
/// Demonstrates: Constructor vs Method service injection.
/// </summary>
[Factory]
internal partial class ServiceInjectionDemo : EntityBase<ServiceInjectionDemo>, IServiceInjectionDemo
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
    // IDbContext is registered only on the server. This operation is [Remote],
    // so a client call crosses to the server and IDbContext resolves there.
    // =========================================================================
    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IDbContext dbContext)
    {
        // dbContext only exists on server
        var data = dbContext.Find<EntityData>(id);
        Name = data?.Name;
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IDbContext dbContext)
    {
        dbContext.Add(new EntityData { Name = Name });
        dbContext.SaveChanges();
    }

    [Remote]
    [Update]
    internal void Update([Service] IDbContext dbContext)
    {
        // Server-only operation
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IDbContext dbContext)
    {
        // Server-only operation
    }
}

// =============================================================================
// Entity Duality - Same Class as Root or Child
// =============================================================================
// An entity class CAN serve as an aggregate root in one graph and a child in
// another - but the two roles need separate factory operations and separate
// interfaces, because the framework decides which role a consumer sees from
// the interface (IEntityRoot vs IEntityBase), and decides what the generated
// factory exposes from each operation's signature and visibility.
//
// COMMON MISTAKE: assuming a child's persistence methods are called "by the
// parent's persistence code, NOT through the factory." Child persistence runs
// through the CHILD FACTORY's Save, coordinated by the child list's [Update] -
// that is what marks each child unmodified and old as it saves. A parent that
// writes child rows to the repository directly leaves every child dirty and
// new. See Aggregates/OrderAggregate and Entities for the canonical shape.
//
// The role split, concretely:
//
// - ROOT role: parent-less operations, [Remote], reached through a public
//   factory Save(target). Its interface extends IEntityRoot.
// - CHILD role: operations that take the child's own row from its list,
//   internal and non-[Remote], reached only through the list's [Update]. Its
//   interface extends IEntityBase.
//
// DESIGN DECISION: do not bolt a root role onto a child-only type. Any
// parent-less [Remote] operation makes the generator emit a PUBLIC
// Save(target) that lets consumers persist a child outside its aggregate -
// see the NO STANDALONE-ROOT OPERATIONS block in Entities/Address.cs for the
// full reasoning and the rejected pattern.
//
// The class below has only the ROOT role. Its [Remote] operations are client
// entry points; [Remote] does not decide root versus child - the interface
// and the operation signatures do.
// =============================================================================

/// <summary>
/// Demonstrates: an entity in the ROOT role only.
/// </summary>
[Factory]
internal partial class DualUseEntity : EntityBase<DualUseEntity>, IDualUseEntity
{
    public partial int Id { get; set; }
    public partial string? Street { get; set; }
    public partial string? City { get; set; }

    public DualUseEntity(IEntityBaseServices<DualUseEntity> services) : base(services) { }

    [Create]
    public void Create() { }

    // =========================================================================
    // These are ROOT-role operations: no parent identity in the signature, so
    // the generated factory exposes a public Save(target) for them.
    //
    // [Remote] makes these client entry points. It does NOT decide
    // root-vs-child - the interface and the operation signatures do.
    //
    // To ALSO serve as a child, this class would need a second set of
    // internal, non-[Remote] operations that take its own row from a list,
    // and a child-shaped interface extending IEntityBase.
    // =========================================================================

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IDualUseRepository repository)
    {
        // Called via factory when this is an aggregate root
        var data = repository.GetAddressById(id);
        Id = data.Id;
        Street = data.Street;
        City = data.City;
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IDualUseRepository repository)
    {
        // Reached through the root factory's Save
        var newId = repository.InsertAddress(Street!, City!);
        Id = newId;
    }

    [Remote]
    [Update]
    internal void Update([Service] IDualUseRepository repository)
    {
        repository.UpdateAddress(Id, Street!, City!);
    }

    [Remote]
    [Delete]
    internal void Delete([Service] IDualUseRepository repository)
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
