// -----------------------------------------------------------------------------
// Design.Domain - Service Registration Patterns
// -----------------------------------------------------------------------------
// This file documents how to register Neatoo services with DI.
// -----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.RemoteFactory;

namespace Design.Domain.DI;

// =============================================================================
// SERVICE REGISTRATION OVERVIEW
// =============================================================================
// Neatoo services are registered using extension methods on IServiceCollection.
// The main extension is AddNeatooServices() which registers all core services.
//
// TYPICAL REGISTRATION PATTERN:
//
// services.AddNeatooServices(NeatooFactory.Server, typeof(MyDomainAssembly).Assembly);
//
// NeatooFactory enum values:
// - NeatooFactory.Server: Server-side (full factory implementation)
// - NeatooFactory.Remote: Client-side ([Remote] methods make HTTP calls)
// - NeatooFactory.Logical: In-process (no HTTP, everything local)
//
// This registers:
// - IValidateBaseServices<T> for each ValidateBase<T> type
// - IEntityBaseServices<T> for each EntityBase<T> type
// - Generated factories (IXxxFactory implementations)
// - Property factories and managers
// - Rule managers
// =============================================================================

/// <summary>
/// Demonstrates: Service registration patterns.
/// </summary>
public static class ServiceRegistrationDemo
{
    // =========================================================================
    // Basic Registration
    // =========================================================================
    // Add Neatoo services for an assembly containing domain objects.
    // =========================================================================
    public static void RegisterBasicServices(IServiceCollection services)
    {
        // Register all Neatoo types in the Design.Domain assembly
        // NeatooFactory.Server = full implementation with [Service] resolution
        services.AddNeatooServices(NeatooFactory.Server, typeof(ServiceRegistrationDemo).Assembly);

        // This scans the assembly and registers:
        // - All types inheriting from ValidateBase<T> or EntityBase<T>
        // - All generated factory interfaces and implementations
        // - Supporting services (property factories, rule managers)
    }

    // =========================================================================
    // Multi-Assembly Registration
    // =========================================================================
    // For solutions with multiple domain assemblies.
    // =========================================================================
    public static void RegisterMultipleAssemblies(IServiceCollection services)
    {
        // Register multiple assemblies in a single call
        services.AddNeatooServices(
            NeatooFactory.Server,
            typeof(ServiceRegistrationDemo).Assembly);
        // To add more assemblies:
        // services.AddNeatooServices(
        //     NeatooFactory.Server,
        //     typeof(ServiceRegistrationDemo).Assembly,
        //     typeof(OtherDomain.SomeEntity).Assembly);

        // Each assembly's types are registered
    }

    // =========================================================================
    // Server vs Client Registration
    // =========================================================================
    // Server and client load the SAME domain assembly and the same generated
    // factories. The mode passed to AddNeatooServices picks the path at runtime.
    //
    // Server (NeatooFactory.Server):
    //   - Factories resolve [Service] parameters from the server's container
    //   - Every operation executes locally
    //
    // Client (NeatooFactory.Remote):
    //   - [Remote] operations are sent to the server's single POST /api/neatoo
    //     endpoint
    //   - Local operations (a [Create] with no [Remote]) execute on the client,
    //     resolving their [Service] parameters from the client's container
    //   - internal operations are server-only: calling one throws
    //     "Server-only method called in non-server runtime."
    // =========================================================================
    public static void RegisterServerServices(IServiceCollection services)
    {
        // Server registration - NeatooFactory.Server for full implementation
        services.AddNeatooServices(NeatooFactory.Server, typeof(ServiceRegistrationDemo).Assembly);

        // Server also needs:
        // - Repository implementations
        // - DbContext
        // - External service clients
        // services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        // services.AddDbContext<MyDbContext>();
    }

    public static void RegisterClientServices(IServiceCollection services)
    {
        // Client registration - NeatooFactory.Remote for HTTP proxy factories
        services.AddNeatooServices(NeatooFactory.Remote, typeof(ServiceRegistrationDemo).Assembly);

        // Client also needs an HttpClient registered under RemoteFactory's key,
        // pointed at the server (see src/Examples/Person/Person.App/Program.cs):
        // services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
        //     (sp, key) => new HttpClient { BaseAddress = new Uri(serverUrl) });
    }
}

// =============================================================================
// WHAT GETS REGISTERED
// =============================================================================
// AddNeatooServices registers these service patterns:
//
// - Neatoo's core services for ValidateBase<T> and EntityBase<T> (the
//   services objects, the property factory, the rule manager). See
//   src/Neatoo/AddNeatooServices.cs for the exact registrations and lifetimes.
// - The generated factories, registered by a generated registrar
//   ([assembly: NeatooFactoryRegistrar]); factories are scoped.
// - Domain types themselves, so DI can construct them.
//
// DESIGN DECISION: Transient lifetime for domain objects.
// Each factory call creates a new instance. Domain objects don't share state.
//
// DID NOT DO THIS: Use Scoped or Singleton for domain objects.
//
// REJECTED PATTERN:
//   services.AddScoped<Employee>();  // Shared within request - WRONG
//
// WHY NOT: Domain objects are identity-based. Two fetches of same ID should
// return independent instances. Scoped would return same instance, causing
// state bleeding between operations.
// =============================================================================

// =============================================================================
// DEPENDENCY INJECTION FLOW
// =============================================================================
// When you call var employee = employeeFactory.Create():
//
// 1. Factory resolved from DI:
//    IEmployeeFactory factory = serviceProvider.GetRequiredService<IEmployeeFactory>();
//
// 2. Factory creates employee:
//    var employee = serviceProvider.GetRequiredService<Employee>();
//    // This resolves Employee's constructor dependencies
//
// 3. Employee constructor receives services:
//    public Employee(IEntityBaseServices<Employee> services) : base(services)
//    // IEntityBaseServices<Employee> was registered and resolved
//
// 4. Factory calls lifecycle methods:
//    employee.FactoryStart(FactoryOperation.Create);
//    employee.Create();  // Your [Create] method
//    employee.FactoryComplete(FactoryOperation.Create);
//
// 5. For [Remote] methods, [Service] parameters resolved:
//    employee.Fetch(id, repository);
//    // repository resolved: serviceProvider.GetRequiredService<IRepository>()
// =============================================================================

// =============================================================================
// COMMON REGISTRATION MISTAKES
// =============================================================================
//
// COMMON MISTAKE: Forgetting to register domain assembly.
//
// WRONG:
//   // Only registering infrastructure
//   services.AddScoped<IEmployeeRepository, EmployeeRepository>();
//   // Missing: services.AddNeatooServices(...);
//
// ERROR: "Unable to resolve service for type 'IEmployeeFactory'"
//
// FIX: Add services.AddNeatooServices(NeatooFactory.Server, typeof(Employee).Assembly);
//
// COMMON MISTAKE: Registering domain objects manually.
//
// WRONG:
//   services.AddScoped<Employee>();  // Manual registration
//   services.AddNeatooServices(...); // Also auto-registers
//   // Now Employee is registered twice with different lifetimes
//
// FIX: Let AddNeatooServices handle all Neatoo types.
//
// COMMON MISTAKE: Wrong assembly reference.
//
// WRONG:
//   services.AddNeatooServices(NeatooFactory.Server, typeof(SomeController).Assembly);
//   // Controllers assembly doesn't contain domain types!
//
// FIX: Use a type from the domain assembly:
//   services.AddNeatooServices(NeatooFactory.Server, typeof(Employee).Assembly);
// =============================================================================

// =============================================================================
// HOW FACTORY DISCOVERS [Factory] CLASSES
// =============================================================================
// RemoteFactory source generator runs at compile time and:
//
// 1. SCANS for [Factory] attribute on classes
//    - Looks in the assembly being compiled
//    - Finds all classes decorated with [Factory]
//
// 2. ANALYZES factory methods
//    - Finds methods with [Create], [Fetch], [Insert], [Update], [Delete], [Execute]
//    - Extracts parameter signatures
//    - Notes which methods have [Remote] attribute
//
// 3. GENERATES factory interface
//    - IEmployeeFactory with methods matching factory operations
//    - Return types based on operation (Task<T> for async, T for sync)
//
// 4. GENERATES factory implementation
//    - EmployeeFactory implementing IEmployeeFactory
//    - Constructor takes IServiceProvider
//    - Methods resolve domain object and call factory methods
//    - [Service] parameters resolved from DI at execution time
//
// 5. GENERATES a registrar
//    - A static FactoryServiceRegistrar, found through
//      [assembly: NeatooFactoryRegistrar] when AddNeatooServices runs
//    - Registers all factory interfaces and implementations
//
// GENERATOR OUTPUT LOCATION:
//   Generated/Neatoo.Generator/Neatoo.Factory/
//     - {Namespace}.{TypeName}Factory.g.cs
//
// To see generated code, add to .csproj:
//   <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
// =============================================================================

// =============================================================================
// DI CONTAINER ASSUMPTIONS
// =============================================================================
// Neatoo assumes Microsoft.Extensions.DependencyInjection:
// - IServiceProvider for resolving services
// - IServiceCollection for registration
// - Transient/Scoped/Singleton lifetime support
//
// DESIGN DECISION: Use IServiceProvider, not DI container abstraction.
// Neatoo doesn't abstract over DI containers. It directly uses:
// - IServiceProvider.GetService<T>()
// - IServiceProvider.GetRequiredService<T>()
//
// DID NOT DO THIS: Create custom IContainer abstraction.
//
// REJECTED PATTERN:
//   public interface INeatooContainer {
//       T Resolve<T>();
//   }
//
// WHY NOT: Microsoft.Extensions.DependencyInjection is the standard.
// All modern .NET frameworks support it. Adding abstraction would:
// - Complicate usage for no benefit
// - Require adapter implementations for each DI container
// - Prevent using DI container features directly
//
// SUPPORTED CONTAINERS (via Microsoft.Extensions.DependencyInjection.Abstractions):
// - Microsoft.Extensions.DependencyInjection (default)
// - Autofac (with extension)
// - Castle Windsor (with extension)
// - Any container with IServiceProvider support
// =============================================================================

// =============================================================================
// CUSTOM RULE REGISTRATION
// =============================================================================
// Rules are typically added in constructors, but you can also register
// rule classes in DI for dependency injection into rules.
//
// PATTERN 1: Rules added in constructor (most common)
//
//   public Employee(...) : base(services)
//   {
//       RuleManager.AddRule(new NameRequiredRule());
//   }
//
// PATTERN 2: Rules with dependencies
//
// A rule is built on every tier that builds the entity, the browser included,
// so it never takes a server-only service (a repository, a DbContext). A rule
// that needs the server takes an [Execute] command delegate instead; in the
// browser the delegate crosses to the server, on the server it calls the
// method directly.
//
//   [Factory]
//   public static partial class UniqueName
//   {
//       [Remote, Execute]
//       private static Task<bool> _IsUnique(Guid? id, string name,
//           [Service] IEmployeeRepository repository)
//           => repository.IsNameUnique(id, name);
//   }
//
//   internal class UniqueNameRule : AsyncRuleBase<IEmployee>, IUniqueNameRule
//   {
//       private readonly UniqueName.IsUnique _isUnique;
//
//       public UniqueNameRule(UniqueName.IsUnique isUnique) : base(t => t.Name)
//       {
//           _isUnique = isUnique;
//       }
//
//       protected override async Task<IRuleMessages> Execute(IEmployee target, CancellationToken? token = null)
//           => await _isUnique(target.Id, target.Name)
//               ? None
//               : (nameof(IEmployee.Name), "Name already exists").AsRuleMessages();
//   }
//
//   // Registration, on BOTH tiers. The rule types are internal, so the
//   // domain assembly registers them (see DI/DomainRegistration.cs):
//   services.AddTransient<IUniqueNameRule, UniqueNameRule>();
//
//   // Usage in constructor:
//   public Employee(IEntityBaseServices<Employee> services, IUniqueNameRule uniqueNameRule)
//       : base(services)
//   {
//       RuleManager.AddRule(uniqueNameRule);
//   }
//
// Compiled examples: Rules/AsyncRules.cs (UsernameAvailability and
// CheckUsernameAvailabilityRule) and the Person example's UniqueNameRule.cs.
//
// DESIGN DECISION: Rules are NOT auto-registered.
// Rules are typically stateless and created inline. Auto-registration
// would add complexity for a scenario that's rarely needed.
// =============================================================================

// =============================================================================
// SCOPED VS TRANSIENT LIFETIME CONSIDERATIONS
// =============================================================================
// DESIGN DECISION: Domain objects are always Transient.
//
// Transient (AddTransient):
//   - New instance per resolution
//   - Independent state between instances
//   - CORRECT for domain objects
//
// Scoped (AddScoped):
//   - Same instance within scope (e.g., HTTP request)
//   - Shared state within scope
//   - WRONG for domain objects
//
// Singleton (AddSingleton):
//   - One instance for application lifetime
//   - Shared across all requests
//   - NEVER for domain objects
//
// WHY TRANSIENT MATTERS:
//
// Scenario: Two places in code fetch same employee
//   var emp1 = await factory.Fetch(1);
//   emp1.Name = "Changed";
//
//   var emp2 = await factory.Fetch(1);  // Different request in same scope
//   // emp2.Name should be "Original" from DB
//
// With TRANSIENT: emp2 is a fresh instance, Name = "Original" (CORRECT)
// With SCOPED: emp2 is same as emp1, Name = "Changed" (WRONG - state bleeding)
//
// INFRASTRUCTURE SERVICES can be Scoped:
//   services.AddScoped<IEmployeeRepository, EmployeeRepository>();
//   services.AddScoped<MyDbContext>();
//
// This is fine because repositories don't hold entity state.
// =============================================================================

// =============================================================================
// SERVICE RESOLUTION EXCEPTIONS
// =============================================================================
// Common DI exceptions and their causes:
//
// "Unable to resolve service for type 'IEmployeeFactory'"
//   CAUSE: Assembly not registered with AddNeatooServices
//   FIX: services.AddNeatooServices(NeatooFactory.Server, typeof(Employee).Assembly);
//
// "Unable to resolve service for type 'IEmployeeRepository'"
//   CAUSE: Repository not registered
//   FIX: services.AddScoped<IEmployeeRepository, EmployeeRepository>();
//
// "A circular dependency was detected for the service of type 'Employee'"
//   CAUSE: Employee depends on something that depends on Employee
//   FIX: Break cycle using Lazy<T> or factory delegate
//
// "Cannot resolve scoped service 'X' from root provider"
//   CAUSE: Trying to resolve scoped service outside of scope
//   FIX: Create IServiceScope first, then resolve from scope
// =============================================================================
