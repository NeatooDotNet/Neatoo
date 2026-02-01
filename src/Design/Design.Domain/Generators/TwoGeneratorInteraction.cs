// -----------------------------------------------------------------------------
// Design.Domain - Two Generator Interaction Documentation
// -----------------------------------------------------------------------------
// This file documents how Neatoo.BaseGenerator and RemoteFactory work together.
// Understanding this interaction is essential for troubleshooting and extending.
// -----------------------------------------------------------------------------

using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.Generators;

// =============================================================================
// GENERATOR INTERACTION OVERVIEW
// =============================================================================
// Neatoo uses TWO Roslyn source generators that work together:
//
// 1. Neatoo.BaseGenerator (from Neatoo package)
//    - Generates property backing fields
//    - Generates InitializePropertyBackingFields()
//    - Handles partial property implementation
//
// 2. RemoteFactory (from Neatoo.RemoteFactory package)
//    - Generates factory interfaces (IXxxFactory)
//    - Generates factory implementations
//    - Handles [Remote] method proxying
//    - Generates HTTP endpoints for server
//
// EXECUTION ORDER: Both generators run during compilation (independently).
// There's no strict ordering between them - they operate on different code.
//
// DESIGN DECISION: Separate generators for separate concerns.
// - BaseGenerator: Object infrastructure (properties, backing fields)
// - RemoteFactory: Factory pattern and remoting
//
// This separation allows:
// - Using Neatoo without RemoteFactory (rare but possible)
// - Independent versioning of each generator
// - Clearer responsibility boundaries
// =============================================================================

/// <summary>
/// Demonstrates: What both generators produce for a typical entity.
/// </summary>
[Factory]
public partial class GeneratorDemo : EntityBase<GeneratorDemo>
{
    public partial string? Name { get; set; }
    public partial int Value { get; set; }

    public GeneratorDemo(IEntityBaseServices<GeneratorDemo> services) : base(services) { }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    public void Fetch(int id, [Service] IGeneratorDemoRepository repository)
    {
        var data = repository.GetById(id);
        this["Name"].LoadValue(data.Name);
        this["Value"].LoadValue(data.Value);
    }

    [Remote]
    [Insert]
    public void Insert([Service] IGeneratorDemoRepository repository) { }

    [Remote]
    [Update]
    public void Update([Service] IGeneratorDemoRepository repository) { }

    [Remote]
    [Delete]
    public void Delete([Service] IGeneratorDemoRepository repository) { }
}

// =============================================================================
// NEATOO.BASEGENERATOR OUTPUT
// =============================================================================
// For the class above, Neatoo.BaseGenerator produces (GeneratorDemo.g.cs):
//
// GENERATOR BEHAVIOR: Property backing field generation
//
// public partial class GeneratorDemo
// {
//     private IEntityProperty<string?> _nameProperty = null!;
//     private IEntityProperty<int> _valueProperty = null!;
//
//     public partial string? Name
//     {
//         get => _nameProperty.Value;
//         set => _nameProperty.SetValue(value);
//     }
//
//     public partial int Value
//     {
//         get => _valueProperty.Value;
//         set => _valueProperty.SetValue(value);
//     }
//
//     protected override void InitializePropertyBackingFields(IPropertyFactory<GeneratorDemo> factory)
//     {
//         base.InitializePropertyBackingFields(factory);
//
//         _nameProperty = factory.CreateProperty<string?>("Name", this);
//         PropertyManager.Add(_nameProperty);
//
//         _valueProperty = factory.CreateProperty<int>("Value", this);
//         PropertyManager.Add(_valueProperty);
//     }
// }
//
// KEY POINTS:
// - Each partial property gets a backing field of IEntityProperty<T>
// - Getter returns property.Value
// - Setter calls property.SetValue(value) which triggers modification tracking
// - InitializePropertyBackingFields creates properties via factory
// =============================================================================

// =============================================================================
// REMOTEFACTORY OUTPUT
// =============================================================================
// For the class above, RemoteFactory produces multiple files:
//
// GENERATOR BEHAVIOR: Factory interface generation
//
// File: IGeneratorDemoFactory.g.cs
// --------------------------------
// public interface IGeneratorDemoFactory : IFactorySave<GeneratorDemo>
// {
//     GeneratorDemo Create();
//     Task<GeneratorDemo> Fetch(int id);
//     // Insert, Update, Delete are on IFactorySave<T>
// }
//
// GENERATOR BEHAVIOR: Factory implementation (Full mode - server)
//
// File: GeneratorDemoFactory.g.cs
// -------------------------------
// public class GeneratorDemoFactory : IGeneratorDemoFactory
// {
//     private readonly IServiceProvider _serviceProvider;
//
//     public GeneratorDemoFactory(IServiceProvider serviceProvider)
//     {
//         _serviceProvider = serviceProvider;
//     }
//
//     public GeneratorDemo Create()
//     {
//         var obj = _serviceProvider.GetRequiredService<GeneratorDemo>();
//         obj.FactoryStart(FactoryOperation.Create);
//         obj.Create();  // Calls your [Create] method
//         obj.FactoryComplete(FactoryOperation.Create);
//         return obj;
//     }
//
//     public async Task<GeneratorDemo> Fetch(int id)
//     {
//         var obj = _serviceProvider.GetRequiredService<GeneratorDemo>();
//         var repository = _serviceProvider.GetRequiredService<IGeneratorDemoRepository>();
//         obj.FactoryStart(FactoryOperation.Fetch);
//         obj.Fetch(id, repository);  // [Service] resolved from DI
//         obj.FactoryComplete(FactoryOperation.Fetch);
//         return obj;
//     }
//
//     public async Task<GeneratorDemo> Save(GeneratorDemo obj)
//     {
//         if (obj.IsNew && !obj.IsDeleted) return await Insert(obj);
//         if (obj.IsDeleted && !obj.IsNew) return await Delete(obj);
//         if (obj.IsModified) return await Update(obj);
//         return obj;
//     }
//
//     // Insert, Update, Delete implementations similar to Fetch
// }
//
// GENERATOR BEHAVIOR: Remote proxy (RemoteOnly mode - client)
//
// File: GeneratorDemoFactoryRemote.g.cs (for client assemblies)
// -------------------------------------------------------------
// public class GeneratorDemoFactory : IGeneratorDemoFactory
// {
//     private readonly HttpClient _httpClient;
//
//     public GeneratorDemo Create()
//     {
//         // [Create] without [Remote] - local execution
//         var obj = _serviceProvider.GetRequiredService<GeneratorDemo>();
//         obj.Create();
//         return obj;
//     }
//
//     public async Task<GeneratorDemo> Fetch(int id)
//     {
//         // [Fetch] with [Remote] - HTTP call to server
//         var request = new { id };
//         var response = await _httpClient.PostAsJsonAsync(
//             "/api/GeneratorDemo/Fetch", request);
//         return await response.Content.ReadFromJsonAsync<GeneratorDemo>();
//     }
// }
// =============================================================================

// =============================================================================
// [FactoryMode] ASSEMBLY ATTRIBUTE
// =============================================================================
// RemoteFactory behavior is controlled by assembly-level attribute:
//
// [assembly: FactoryMode(FactoryMode.Full)]      // Server - full implementation
// [assembly: FactoryMode(FactoryMode.RemoteOnly)] // Client - HTTP proxies
//
// DESIGN DECISION: FactoryMode determines which code is generated.
// - Full: Generates actual implementations that resolve services and call methods
// - RemoteOnly: Generates HTTP proxies for [Remote] methods
//
// DID NOT DO THIS: Generate both modes and select at runtime.
//
// REJECTED PATTERN:
//   if (isServer) { useFullImplementation(); }
//   else { useRemoteProxy(); }
//
// WHY NOT:
// 1. Code size - client doesn't need server implementation code
// 2. Dependencies - client shouldn't reference server-only assemblies (EF Core)
// 3. Security - server code shouldn't be in client assembly
//
// The PrivateAssets pattern keeps server dependencies out of client builds.
// =============================================================================

// =============================================================================
// INTERACTION POINTS
// =============================================================================
// The two generators interact through:
//
// 1. PROPERTY INFRASTRUCTURE
//    - BaseGenerator creates property backing fields
//    - Factory methods use property indexer: this["Name"].LoadValue(value)
//
// 2. FACTORY LIFECYCLE METHODS
//    - Generated factory calls FactoryStart/FactoryComplete
//    - These are defined in ValidateBase/EntityBase (not generated)
//
// 3. SERVICE RESOLUTION
//    - BaseGenerator: Creates objects that expect services via constructor
//    - RemoteFactory: Factory resolves services from DI container
//
// 4. TYPE INFORMATION
//    - BaseGenerator: Operates on partial classes with partial properties
//    - RemoteFactory: Operates on classes with [Factory] and factory methods
//    - Both use same type system, no direct coordination needed
// =============================================================================

// =============================================================================
// TROUBLESHOOTING GENERATOR ISSUES
// =============================================================================
//
// ISSUE: Properties not generating
// CHECK: Is the class partial? Is the property partial?
// FIX: Ensure both class and property have 'partial' keyword
//
// ISSUE: Factory not generating
// CHECK: Does class have [Factory] attribute? (EntityBase/ValidateBase have it)
// FIX: Ensure class inherits from EntityBase<T> or ValidateBase<T>
//
// ISSUE: [Remote] not working
// CHECK: Is FactoryMode set correctly for the assembly?
// FIX: Add [assembly: FactoryMode(FactoryMode.Full)] or RemoteOnly
//
// ISSUE: Service not resolving
// CHECK: Is the service registered in DI? Is [Service] attribute present?
// FIX: Register service in Startup/Program.cs, add [Service] to parameter
//
// ISSUE: Generated code not updating
// CHECK: Did build complete? Any generator errors in Error List?
// FIX: Rebuild solution, check for generator diagnostic errors
//
// DEBUG TIP: Generated files are in obj/Debug/{tfm}/Generated/ folders
// You can examine them to understand what generators produced.
// =============================================================================

public interface IGeneratorDemoRepository
{
    (string Name, int Value) GetById(int id);
}
