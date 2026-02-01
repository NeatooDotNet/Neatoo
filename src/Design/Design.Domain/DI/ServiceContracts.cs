// -----------------------------------------------------------------------------
// Design.Domain - Service Contract Interfaces
// -----------------------------------------------------------------------------
// This file documents IValidateBaseServices, IEntityBaseServices, and
// IPropertyFactory - the core service contracts that Neatoo uses.
// -----------------------------------------------------------------------------

using Neatoo;

namespace Design.Domain.DI;

// =============================================================================
// SERVICE CONTRACT OVERVIEW
// =============================================================================
// Neatoo uses service interfaces to wrap dependencies:
//
// IValidateBaseServices<T>: Services for ValidateBase<T> objects
// IEntityBaseServices<T>: Services for EntityBase<T> objects (extends above)
// IPropertyFactory<T>: Creates property backing fields
//
// DESIGN DECISION: Wrap services in container interfaces.
// This allows:
// - Adding services without breaking constructor signatures
// - Single injection point in constructor
// - Clear separation of framework vs application services
//
// DID NOT DO THIS: Inject each service separately.
//
// REJECTED PATTERN:
//   public Employee(
//       IPropertyFactory<Employee> propertyFactory,
//       IPropertyInfoList<Employee> propertyInfoList,
//       IRuleManagerFactory ruleManagerFactory,
//       IFactorySave<Employee> factory)  // Many parameters!
//
// ACTUAL PATTERN:
//   public Employee(IEntityBaseServices<Employee> services)  // One parameter
//
// WHY NOT: Constructor parameter proliferation. Adding a new framework service
// would require updating every entity constructor. The services wrapper hides
// framework complexity and enables non-breaking additions.
// =============================================================================

// =============================================================================
// IValidateBaseServices<T> INTERFACE
// =============================================================================
// Provides services for ValidateBase<T> objects:
//
// public interface IValidateBaseServices<T> where T : ValidateBase<T>
// {
//     // Property metadata for type T
//     IPropertyInfoList<T> PropertyInfoList { get; }
//
//     // Property manager with validation support
//     IValidatePropertyManager<IValidateProperty> ValidatePropertyManager { get; }
//
//     // Factory for creating property backing fields
//     IPropertyFactory<T> PropertyFactory { get; }
//
//     // Creates a rule manager for the target object
//     IRuleManager<T> CreateRuleManager(T target);
// }
//
// USAGE IN CONSTRUCTOR:
//   public MyValueObject(IValidateBaseServices<MyValueObject> services) : base(services)
//   {
//       // services.PropertyFactory used by InitializePropertyBackingFields
//       // services.ValidatePropertyManager manages all properties
//       // RuleManager created via services.CreateRuleManager(this)
//   }
// =============================================================================

// =============================================================================
// IEntityBaseServices<T> INTERFACE
// =============================================================================
// Extends IValidateBaseServices<T> for EntityBase<T> objects:
//
// public interface IEntityBaseServices<T> : IValidateBaseServices<T>
//     where T : EntityBase<T>
// {
//     // Property manager with modification tracking
//     IEntityPropertyManager EntityPropertyManager { get; }
//
//     // Factory for save operations (Insert/Update/Delete)
//     IFactorySave<T>? Factory { get; }
// }
//
// ADDITIONAL CAPABILITIES:
// - EntityPropertyManager: Tracks IsModified, IsSelfModified, ModifiedProperties
// - Factory: Used by Save() to call Insert/Update/Delete
//
// USAGE IN CONSTRUCTOR:
//   public MyEntity(IEntityBaseServices<MyEntity> services) : base(services)
//   {
//       // All ValidateBase services available
//       // Plus modification tracking and save factory
//   }
// =============================================================================

// =============================================================================
// IPropertyFactory<T> INTERFACE
// =============================================================================
// Creates property backing fields during object initialization:
//
// public interface IPropertyFactory<T> where T : class, IValidateBase
// {
//     // For ValidateBase - creates IValidateProperty<P>
//     IValidateProperty<P> CreateProperty<P>(string propertyName, T target);
//
//     // For EntityBase - creates IEntityProperty<P>
//     IEntityProperty<P> CreateEntityProperty<P>(string propertyName, T target);
// }
//
// GENERATOR BEHAVIOR: InitializePropertyBackingFields uses this factory:
//
// protected override void InitializePropertyBackingFields(IPropertyFactory<T> factory)
// {
//     base.InitializePropertyBackingFields(factory);
//
//     // For ValidateBase:
//     _nameProperty = factory.CreateProperty<string?>("Name", this);
//
//     // For EntityBase:
//     _nameProperty = factory.CreateEntityProperty<string?>("Name", this);
//
//     PropertyManager.Add(_nameProperty);
// }
//
// DESIGN DECISION: Factory creates properties, not direct instantiation.
// This allows:
// - Consistent property creation across all types
// - Proper initialization with property metadata
// - Framework control over property lifecycle
// =============================================================================

// =============================================================================
// IFactorySave<T> INTERFACE
// =============================================================================
// Generated interface for save operations:
//
// public interface IFactorySave<T> where T : IEntityBase
// {
//     Task<T> Save(T entity);
// }
//
// Save() implementation routes based on state:
//
// public async Task<T> Save(T entity)
// {
//     if (entity.IsNew && !entity.IsDeleted)
//         return await Insert(entity);
//     if (entity.IsDeleted && !entity.IsNew)
//         return await Delete(entity);
//     if (entity.IsModified)
//         return await Update(entity);
//     return entity;  // Nothing to save
// }
//
// DESIGN DECISION: Save is on a separate interface from type-specific factory.
// This allows:
// - Generic Save() handling in framework code
// - Type-specific factories with custom Create/Fetch signatures
// - Clear separation of CRUD vs query operations
// =============================================================================

// =============================================================================
// IRuleManager<T> INTERFACE
// =============================================================================
// Manages validation and action rules for an object:
//
// public interface IRuleManager<T> : IRuleManager where T : class, IValidateBase
// {
//     // Add validation rule (returns error string or empty)
//     ValidationFluentRule<T> AddValidation(
//         Func<T, string> func,
//         Expression<Func<T, object?>> triggerProperty);
//
//     // Add action rule (performs side effects)
//     ActionFluentRule<T> AddAction(
//         Action<T> func,
//         Expression<Func<T, object?>> triggerProperty);
//
//     // Async variants
//     ValidationAsyncFluentRule<T> AddValidationAsync(...);
//     ActionAsyncFluentRule<T> AddActionAsync(...);
//
//     // Add class-based rule
//     void AddRule<TRule>(TRule rule) where TRule : IRule<T>;
//
//     // Run rules
//     Task RunRules(string propertyName, CancellationToken? token = null);
//     Task RunRules(RunRulesFlag flag = RunRulesFlag.All, CancellationToken? token = null);
// }
//
// Created via: services.CreateRuleManager(target)
// This ensures the rule manager is bound to the correct target instance.
// =============================================================================

// =============================================================================
// SERVICE RESOLUTION CHAIN
// =============================================================================
// When resolving IEntityBaseServices<Employee>:
//
// 1. DI resolves EntityBaseServices<Employee>
// 2. EntityBaseServices constructor receives:
//    - IPropertyInfoList<Employee>
//    - IPropertyFactory<Employee>
//    - IFactorySave<Employee> (the generated factory)
// 3. EntityBaseServices creates:
//    - ValidatePropertyManager (for validation tracking)
//    - EntityPropertyManager (for modification tracking)
// 4. CreateRuleManager() creates new RuleManager<Employee> when called
//
// This chain ensures all services are properly initialized before
// the domain object constructor runs.
// =============================================================================

// =============================================================================
// TESTING CONSIDERATIONS
// =============================================================================
// For unit testing, you can:
// 1. Use real Neatoo services (recommended in CLAUDE.md)
// 2. Create minimal service implementations for specific tests
//
// RECOMMENDED (from CLAUDE.md):
//   // Use real Neatoo dependencies, don't mock
//   var services = new ServiceCollection();
//   services.AddNeatooServices(typeof(Employee).Assembly);
//   var provider = services.BuildServiceProvider();
//   var factory = provider.GetRequiredService<IEmployeeFactory>();
//   var employee = factory.Create();
//
// NOT RECOMMENDED:
//   var mockServices = new Mock<IEntityBaseServices<Employee>>();
//   // This bypasses real Neatoo behavior
// =============================================================================
