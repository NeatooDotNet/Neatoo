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
//    - Handles [Remote] calls: the client path sends them to the single
//      POST /api/neatoo endpoint; the server path runs them
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
internal partial class GeneratorDemo : EntityBase<GeneratorDemo>, IGeneratorDemo
{
    public partial string? Name { get; set; }
    public partial int Value { get; set; }

    public GeneratorDemo(IEntityBaseServices<GeneratorDemo> services) : base(services) { }

    [Create]
    public void Create() { }

    [Remote]
    [Fetch]
    internal void Fetch(int id, [Service] IGeneratorDemoRepository repository)
    {
        var data = repository.GetById(id);
        Name = data.Name;
        Value = data.Value;
    }

    [Remote]
    [Insert]
    internal void Insert([Service] IGeneratorDemoRepository repository) { }

    [Remote]
    [Update]
    internal void Update([Service] IGeneratorDemoRepository repository) { }

    [Remote]
    [Delete]
    internal void Delete([Service] IGeneratorDemoRepository repository) { }
}

// =============================================================================
// WHAT THE GENERATORS PRODUCE
// =============================================================================
// Read the real output instead of a description of it. Design.Domain sets
// CompilerGeneratedFilesOutputPath=Generated, so it is on disk:
//   Generated/Neatoo.BaseGenerator/...      - property implementations
//   Generated/Neatoo.Generator/Neatoo.Factory/  - factories
//
// GENERATOR BEHAVIOR (Neatoo.BaseGenerator), in outline:
// - Each partial property gets a protected accessor (NameProperty) that reads
//   the property object from PropertyManager, and getter/setter
//   implementations over it.
// - InitializePropertyBackingFields registers each property object through
//   the property factory.
// - Whether the setter tracks a change depends on pause state: inside a
//   factory operation the object is paused and nothing is tracked.
//
// GENERATOR BEHAVIOR (RemoteFactory), in outline:
// - IGeneratorDemoFactory: Create, Fetch and Save(target). The methods return
//   the entity's interface and take a CancellationToken.
// - The factory class carries BOTH a local path and a remote path for each
//   [Remote] operation. Which one runs is decided when the tier registers
//   Neatoo: AddNeatooServices(NeatooFactory.Server | Remote | Logical, assembly).
// - Save(target) routes on state: IsDeleted -> [Delete] (nothing at all when
//   the object is also IsNew); IsNew -> [Insert]; otherwise [Update].
//   IsModified is not consulted.
// - internal operations are guarded by NeatooRuntime.IsServerRuntime, so the
//   IL trimmer removes their bodies from a published client.
// - Factories are registered by a generated registrar, not by hand.
//
// DESIGN DECISION: One domain assembly ships to both tiers. Both paths are
// generated and the tier is chosen at registration; trimming keeps server
// bodies, and the services they reach, out of the published client.
// =============================================================================

// =============================================================================
// INTERACTION POINTS
// =============================================================================
// The two generators interact through:
//
// 1. PROPERTY INFRASTRUCTURE
//    - BaseGenerator creates property backing fields
//    - Factory methods assign the generated properties directly; the object is
//      paused during the operation, so assignment is a clean load
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
// CHECK: Does the class have the [Factory] attribute, and is it partial?
// FIX: Add [Factory]. A plain class with no Neatoo base gets a factory too.
//
// ISSUE: [Remote] call fails on the client
// CHECK: Does each tier register Neatoo with the right mode -
//        NeatooFactory.Remote on the client, NeatooFactory.Server on the server?
// FIX: AddNeatooServices(NeatooFactory.Remote, ...) in the client's Program.cs
//
// ISSUE: Service not resolving
// CHECK: Is the service registered in DI? Is [Service] attribute present?
// FIX: Register service in Startup/Program.cs, add [Service] to parameter
//
// ISSUE: Generated code not updating
// CHECK: Did build complete? Any generator errors in Error List?
// FIX: Rebuild solution, check for generator diagnostic errors
//
// DEBUG TIP: With CompilerGeneratedFilesOutputPath=Generated (as in this
// project), generated files are in the project's Generated/ folder.
// =============================================================================

public interface IGeneratorDemoRepository
{
    (string Name, int Value) GetById(int id);
}

// =============================================================================
// [SuppressFactory] - A Neatoo Class With No Generated Factory
// =============================================================================
// RemoteFactory generates a factory for every [Factory] class. A class that
// derives from a Neatoo base but is never created through a factory (a
// test-only object) carries [SuppressFactory] instead and is constructed
// directly with its services object.
//
// Neatoo.BaseGenerator keys on the same [Factory] attribute (see
// BaseGenerator.cs: ForAttributeWithMetadataName("Neatoo.RemoteFactory.
// FactoryAttribute")), so a [SuppressFactory] class gets NO generated partial
// properties. It must declare ordinary properties. For that reason no
// compiled example lives here; every Design.Domain class is a [Factory] class.
// =============================================================================
