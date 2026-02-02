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
    // Server and client register the same types, but get different implementations
    // based on [FactoryMode] assembly attribute.
    //
    // Server (FactoryMode.Full):
    //   - Factories resolve all [Service] dependencies
    //   - Methods execute locally with full access
    //
    // Client (FactoryMode.RemoteOnly):
    //   - Factories for [Remote] methods make HTTP calls
    //   - Non-[Remote] methods execute locally
    //   - [Service] parameters throw "not registered" on client
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

        // Client also needs:
        // - HttpClient for remote factory calls
        // services.AddHttpClient<INeatooHttpClient, NeatooHttpClient>();
    }
}

// =============================================================================
// WHAT GETS REGISTERED
// =============================================================================
// AddNeatooServices registers these service patterns:
//
// FOR EACH ValidateBase<T>:
//   services.AddTransient<T>();
//   services.AddTransient<IValidateBaseServices<T>, ValidateBaseServices<T>>();
//   services.AddTransient<ITFactory, TFactory>();  // Generated
//
// FOR EACH EntityBase<T>:
//   services.AddTransient<T>();
//   services.AddTransient<IEntityBaseServices<T>, EntityBaseServices<T>>();
//   services.AddTransient<IFactorySave<T>, TFactory>();  // Generated
//   services.AddTransient<ITFactory, TFactory>();  // Generated
//
// SUPPORTING SERVICES:
//   services.AddTransient(typeof(IPropertyFactory<>), typeof(PropertyFactory<>));
//   services.AddTransient(typeof(IPropertyInfoList<>), typeof(PropertyInfoList<>));
//   services.AddTransient(typeof(IRuleManager<>), typeof(RuleManager<>));
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
// When you call var employee = await employeeFactory.Create():
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
// FIX: Add services.AddNeatooServices(typeof(Employee).Assembly);
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
//   services.AddNeatooServices(typeof(SomeController).Assembly);
//   // Controllers assembly doesn't contain domain types!
//
// FIX: Use a type from the domain assembly:
//   services.AddNeatooServices(typeof(Employee).Assembly);
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
// 5. GENERATES registration extension
//    - AddNeatooRemoteFactory extension method
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
// PATTERN 2: Rules with dependencies (less common)
//
//   public class UniqueNameRule : AsyncRuleBase<Employee>
//   {
//       private readonly IEmployeeRepository _repo;
//
//       public UniqueNameRule(IEmployeeRepository repo) : base(t => t.Name)
//       {
//           _repo = repo;
//       }
//
//       protected override async Task<IRuleMessages> Execute(...)
//       {
//           if (await _repo.NameExists(target.Name))
//               return (nameof(Employee.Name), "Name already exists").AsRuleMessages();
//           return None;
//       }
//   }
//
//   // Registration:
//   services.AddTransient<UniqueNameRule>();
//
//   // Usage in constructor:
//   public Employee(IEntityBaseServices<Employee> services,
//                   [Service] UniqueNameRule uniqueNameRule)
//       : base(services)
//   {
//       RuleManager.AddRule(uniqueNameRule);
//   }
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
//   FIX: services.AddNeatooServices(typeof(Employee).Assembly);
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
