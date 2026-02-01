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
