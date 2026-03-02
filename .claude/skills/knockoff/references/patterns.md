# KnockOff Stub Patterns Reference

[Home](../../../../README.md) > [KnockOff Usage](../SKILL.md) > Stub Patterns

KnockOff supports nine distinct patterns for creating test stubs, organized into two categories:

**Standalone Patterns** (file-based, reusable across tests):
1. **Standalone** - `[KnockOff] partial class Stub : IService` - Dedicated stub class implementing interface
2. **Generic Standalone** - `[KnockOff] partial class Stub<T> : IService<T>` - Generic stub class with type parameters
3. **Standalone Class** - `[KnockOffBase<ConcreteClass>] partial class Stub` - Dedicated stub class for classes
4. **Generic Standalone Class** - `[KnockOffBase(typeof(ClassBase<>))] partial class Stub<T>` - Generic stub class for generic base classes

**Inline Patterns** (nested within test class):
5. **Inline Interface** - `[KnockOff<IService>]` - Nested stub for closed generic interface
6. **Inline Class** - `[KnockOff<ConcreteClass>]` - Nested stub for class with virtual members
7. **Inline Delegate** - `[KnockOff<DelegateType>]` - Nested stub for delegate types
8. **Open Generic Interface** - `[KnockOff(typeof(IService<>))]` - Nested generic stub from open generic interface
9. **Open Generic Class** - `[KnockOff(typeof(ServiceBase<>))]` - Nested generic stub from open generic class

## Pattern Relationships

```
Standalone Patterns (file-based, reusable)
|-- 1. Standalone               - [KnockOff] class Stub : IFoo
|-- 2. Generic Standalone       - [KnockOff] class Stub<T> : IFoo<T>
|-- 3. Standalone Class         - [KnockOffBase<SomeClass>] class Stub
|-- 4. Generic Standalone Class - [KnockOffBase(typeof(ClassBase<>))] class Stub<T>

Inline Patterns (nested within test class)
|-- 5. Inline Interface        - [KnockOff<IFoo>]
|-- 6. Inline Class            - [KnockOff<SomeClass>]
|-- 7. Inline Delegate         - [KnockOff<SomeDelegate>]
|-- 8. Open Generic Interface  - [KnockOff(typeof(IFoo<>))]
|-- 9. Open Generic Class      - [KnockOff(typeof(SomeClass<>))]
```

---

## Quick Decision Guide

| If you need... | Use this pattern |
|----------------|------------------|
| Reusable stub across multiple test files | Standalone |
| Custom methods on your stub | Standalone |
| Reusable generic stub with type parameters | Generic Standalone |
| Quick, test-local stub | Inline Interface |
| No extra stub files | Inline Interface |
| Stub a class (not interface) | Inline Class |
| Stub a delegate type | Inline Delegate |
| Test-local stub for generic interface | Open Generic Interface |
| Test-local stub for generic class | Open Generic Class |

---

## Standalone Pattern

The Standalone pattern creates a dedicated stub class in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- You prefer explicit, discoverable stub classes in IntelliSense
- You need the cleanest instantiation syntax (`new MyStub()`)

### Basic Setup

<!-- snippet: skill-patterns-standalone-basic -->
```cs
public interface IUserRepository
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class PtUserRepositoryStub : IUserRepository { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-standalone-usage -->
```cs
// Standalone: instantiate like any class, configure via Verify()
var stub = new PtUserRepositoryStub();
stub.GetById.Call((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
stub.Save.Call((user) => { }).Verifiable();

IUserRepository repo = stub;
var user = repo.GetById(42);
repo.Save(user!);

stub.Verify();
```
<!-- endSnippet -->

### Benefits

- **Reusable**: Reference the stub from any test file
- **Stub overrides**: Add custom methods directly on the stub class
- **Discoverable**: Appears in IntelliSense when browsing your test project
- **Explicit**: Clear separation between test code and stub implementation
- **Clean syntax**: Simple `new MyStub()` instantiation

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for each stub
- **Partial class**: Must remember to mark the class as `partial`
- **Manual interface**: Must manually implement the interface signature

### Stub Overrides

Override protected virtual methods with the underscore suffix convention to provide default implementations:

<!-- snippet: skill-patterns-stub-overrides -->
```cs
[KnockOff]
public partial class PtUserRepositoryStubWithDefaults : IUserRepository
{
    // Override base class method with underscore suffix
    protected override User? GetById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }
}
```
<!-- endSnippet -->

---

## Generic Standalone Pattern

The Generic Standalone pattern creates a reusable generic stub class that can be instantiated with different type arguments across your test suite.

### When to Use

- You need a reusable stub for a generic interface (e.g., `IRepository<T>`)
- You want to use the same stub definition with different type arguments
- You need the same stub in multiple test files with various types
- You prefer clean instantiation syntax with type parameters

### Basic Setup

<!-- snippet: skill-patterns-generic-standalone-basic -->
```cs
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}

[KnockOff]
public partial class PtRepositoryStub<T> : IRepository<T> where T : class { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-generic-standalone-usage -->
```cs
// Generic Standalone: reusable across multiple type arguments
var userRepo = new PtRepositoryStub<User>();
userRepo.GetById.Call((id) => new User { Id = id, Name = "Test" }).Verifiable();
userRepo.Save.Call((entity) => { }).Verifiable();

var productRepo = new PtRepositoryStub<Product>();
productRepo.GetById.Call((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
```
<!-- endSnippet -->

### Benefits

- **Single definition**: Define once, use with any type argument
- **Reusable**: Share across multiple test files
- **Type-safe**: Compiler enforces type constraints
- **Clean syntax**: `new RepositoryStub<User>()` - clear and readable
- **Stub overrides**: Supports custom helper methods like Standalone

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for the stub
- **Partial class**: Must mark as `partial`
- **Constraints must match**: Type constraints must mirror the interface

### Generic Standalone vs Open Generic

| Aspect | Generic Standalone | Open Generic |
|--------|-------------------|--------------|
| **Syntax** | `[KnockOff] class Stub<T> : IFoo<T>` | `[KnockOff(typeof(IFoo<>))]` |
| **Instantiation** | `new Stub<User>()` | `new Stubs.IFoo<User>()` |
| **Reusability** | Across test files | Within one test class |
| **Stub overrides** | Yes | No |
| **Best for** | Shared generic stubs | One-time use |

---

## Standalone Class Pattern

The Standalone Class pattern creates a dedicated stub class for concrete or abstract classes in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same class stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- The class has virtual or abstract members you want to intercept
- You prefer explicit, discoverable stub classes in IntelliSense
- You cannot or don't want to extract an interface

### Basic Setup

<!-- snippet: skill-patterns-standalone-class-basic -->
```cs
// Target class with virtual members
public abstract class ServiceBase
{
    public abstract void Initialize();
    public virtual string Name { get; set; } = "";
}

[KnockOffBase<ServiceBase>]
public partial class PtServiceStub { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-standalone-class-usage -->
```cs
// Standalone Class: instantiate like any class, use .Object
var stub = new PtServiceStub();
stub.Initialize.Call(() => { }).Verifiable();
stub.Name.Get(() => "TestService");

ServiceBase service = stub.Object;  // Use .Object!
service.Initialize();

stub.Verify();
```
<!-- endSnippet -->

### Base Fallback Behavior

Standalone class stubs call the base class implementation by default for unconfigured virtual methods. Only configure what you need to override. Abstract methods return `default(T)` when unconfigured (there is no base to call).

- **Virtual method, unconfigured**: calls base class implementation
- **Virtual method, configured** (Return/Call/When): interceptor handles it, base is NOT called
- **Abstract method, unconfigured**: returns `default(T)` (or throws in strict mode)

This is equivalent to Moq's `CallBase = true`, but it is the default behavior in KnockOff -- no opt-in required.

### Benefits

- **Reusable**: Reference the stub from any test file
- **Stub overrides**: Add custom methods directly on the stub class
- **Discoverable**: Appears in IntelliSense when browsing your test project
- **Explicit**: Clear separation between test code and stub implementation
- **No interface needed**: Stub classes directly without creating interfaces
- **CallBase by default**: Virtual methods fall back to base implementation automatically

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for each stub
- **Partial class**: Must remember to mark the class as `partial`
- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`

---

## Generic Standalone Class Pattern

The Generic Standalone Class pattern creates a reusable generic stub class for generic base classes that can be instantiated with different type arguments across your test suite.

### When to Use

- You need a reusable stub for a generic class (e.g., `RepositoryBase<T>`, `ServiceBase<T>`)
- You want to use the same stub definition with different type arguments
- You need the same stub in multiple test files with various types
- The class has virtual or abstract members you want to intercept

### Basic Setup

<!-- snippet: skill-patterns-generic-standalone-class-basic -->
```cs
public abstract class RepositoryBase<T> where T : class
{
    public abstract T? GetById(int id);
    public abstract void Save(T entity);
}

[KnockOffBase(typeof(RepositoryBase<>))]
public partial class PtRepositoryBaseStub<T> where T : class { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-generic-standalone-class-usage -->
```cs
// Generic Standalone Class: reusable across multiple type arguments
var userRepo = new PtRepositoryBaseStub<User>();
userRepo.GetById.Call((id) => new User { Id = id, Name = "Test" }).Verifiable();
userRepo.Save.Call((entity) => { }).Verifiable();

RepositoryBase<User> repo = userRepo.Object;  // Use .Object!
var user = repo.GetById(1);
repo.Save(user!);

userRepo.Verify();
```
<!-- endSnippet -->

### Base Fallback Behavior

Like Standalone Class stubs, generic standalone class stubs call the base class implementation by default for unconfigured virtual methods. Abstract methods return `default(T)` when unconfigured. See the Standalone Class pattern's "Base Fallback Behavior" section for details.

### Benefits

- **Single definition**: Define once, use with any type argument
- **Reusable**: Share across multiple test files
- **Type-safe**: Compiler enforces type constraints
- **Clean syntax**: `new RepositoryStub<User>().Object` - clear and readable
- **Stub overrides**: Supports custom helper methods like Standalone patterns
- **No interface needed**: Stub generic classes directly
- **CallBase by default**: Virtual methods fall back to base implementation automatically

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for the stub
- **Partial class**: Must mark as `partial`
- **Constraints must match**: Type constraints must mirror the base class
- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`

### Generic Standalone Class vs Open Generic Class

| Aspect | Generic Standalone Class | Open Generic Class |
|--------|--------------------------|-------------------|
| **Syntax** | `[KnockOffBase(typeof(Foo<>))] class Stub<T>` | `[KnockOff(typeof(Foo<>))]` |
| **Instantiation** | `new Stub<User>().Object` | `new Stubs.Foo<User>().Object` |
| **Reusability** | Across test files | Within one test class |
| **Stub overrides** | Yes | No |
| **Best for** | Shared generic class stubs | One-time use |

---

## Inline Interface Pattern

The Inline Interface pattern generates a stub class scoped to your test class. The stub is accessed through a nested `Stubs` namespace.

### When to Use

- You need a stub only within one test class
- You don't need custom methods on the stub
- You want minimal ceremony and no extra files
- The interface is non-generic or you want a closed generic stub

### Basic Setup

<!-- snippet: skill-patterns-inline-interface-basic -->
```cs
[KnockOff<IEmailService>]
public partial class PtEmailServiceTests
{
    // The generator creates Stubs.IEmailService
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-inline-interface-usage -->
```cs
// Inline Interface: access via Stubs namespace
var stub = new Stubs.IEmailService();
stub.Send.Call((string to, string subject) => true).Verifiable();

IEmailService email = stub;
email.Send("test@example.com", "Hello");

stub.Verify();
```
<!-- endSnippet -->

### Benefits

- **Scoped**: Stub exists only for this test class, reducing namespace pollution
- **Less ceremony**: No separate file, no manual interface implementation
- **Automatic**: Stub class generated from interface definition
- **Co-located**: Stub definition and usage in same file

### Trade-offs

- **No stub overrides**: Cannot add custom methods to the generated stub
- **Stubs namespace**: Must use `Stubs.IFoo` syntax to instantiate
- **Test-local only**: Cannot reuse across multiple test classes

---

## Inline Class Pattern

The Inline Class pattern generates a stub for abstract or virtual class members. This allows stubbing classes without extracting interfaces.

### When to Use

- You need to stub a class (not an interface)
- The class has `virtual` or `abstract` members you want to intercept
- You cannot or don't want to extract an interface
- You're testing code that depends on a concrete class

### Basic Setup

<!-- snippet: skill-patterns-inline-class-basic -->
```cs
// Target class with virtual members
public class UserService
{
    public virtual User? GetUser(int id) => null;
    public virtual void SaveUser(User user) { }
    public virtual bool IsConnected { get; set; }
}

[KnockOff<UserService>]
public partial class PtUserServiceTests
{
    // The generator creates Stubs.UserService
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-inline-class-usage -->
```cs
// Inline Class: configure stub, use .Object for the class instance
var stub = new Stubs.UserService();
stub.GetUser.Call((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

UserService service = stub.Object;  // Use .Object!
var user = service.GetUser(42);

stub.Verify();
```
<!-- endSnippet -->

### Base Fallback Behavior

Inline class stubs call the base class implementation by default for unconfigured virtual methods. Only configure what you need to override. Abstract methods return `default(T)` when unconfigured (there is no base to call).

- **Virtual method, unconfigured**: calls base class implementation
- **Virtual method, configured** (Return/Call/When): interceptor handles it, base is NOT called
- **Abstract method, unconfigured**: returns `default(T)` (or throws in strict mode)

This is equivalent to Moq's `CallBase = true`, but it is the default behavior in KnockOff -- no opt-in required.

### Benefits

- **Stub classes**: Works with classes, not just interfaces
- **No interface extraction**: Avoids creating interfaces just for testing
- **Virtual members**: Intercepts any `virtual` or `abstract` members
- **Inheritance**: Properly inherits from the target class
- **CallBase by default**: Virtual methods fall back to base implementation automatically

### Trade-offs

- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`
- **No stub overrides**: Cannot add custom methods like Standalone pattern
- **Class limitations**: Subject to any sealed/non-virtual restrictions

---

## Inline Delegate Pattern

The Inline Delegate pattern is a specialized use of the Inline Interface pattern for delegate types. It generates a stub for delegates, allowing you to test code that accepts delegates as parameters, such as validation rules, factories, or callbacks.

### When to Use

- You need to stub a delegate type
- You want to track delegate invocations
- You need to configure delegate behavior in tests
- You are testing validation rules, factories, or event handlers

### Basic Setup

<!-- snippet: skill-patterns-inline-delegate-basic -->
```cs
// Define delegate types
public delegate bool ValidationRule(string value);
public delegate T Factory<T>();

[KnockOff<ValidationRule>]
[KnockOff<Factory<User>>]
public partial class PtDelegateTests
{
    // The generator creates Stubs.ValidationRule and Stubs.Factory
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-inline-delegate-usage -->
```cs
// Inline Delegate: configure via Interceptor, implicit conversion to delegate
var ruleStub = new Stubs.ValidationRule();
ruleStub.Interceptor.Call((value) => value != "invalid");

ValidationRule rule = ruleStub;  // Implicit conversion
bool isValid = rule("test");

ruleStub.Interceptor.Verify(Called.Once);
```
<!-- endSnippet -->

### Benefits

- **Implicit conversion**: Stub converts to delegate type automatically
- **Invocation tracking**: Use `Verify()`, `LastArg`, `LastArgs`
- **Behavior configuration**: Use `Return`, `Call`, When chains
- **Verification**: Use `Verify()`, `Called` constraints, and `.Verifiable()` chaining
- **Sequences**: `Return(first, params rest)`, `ThenReturn`, `ThenCall`
- **Async auto-wrapping**: `Return(42)` for `Task<int>` delegates (auto-wraps in Task.FromResult)

### Trade-offs

- **Interceptor property**: Access tracking via `stub.Interceptor` (not direct properties)
- **Test-local only**: Cannot reuse across multiple test classes
- **Named delegates only**: Cannot stub inline `Func<T>` or `Action<T>` directly

---

## Open Generic Interface Pattern

The Open Generic Interface pattern generates a generic stub class within your test class that can be instantiated with any type argument. Use this when you need a test-local generic interface stub without creating a separate file.

### When to Use

- You need a generic interface stub only within one test class
- You don't need custom methods on the stub
- You want to test with multiple type arguments in one test class
- You prefer inline definition over a separate file

### Basic Setup

<!-- snippet: skill-patterns-open-generic-interface-basic -->
```cs
[KnockOff(typeof(IService<>))]
public partial class PtOpenGenericTests
{
    // The generator creates Stubs.IService<T>
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-open-generic-interface-usage -->
```cs
// Open Generic Interface: instantiate with any type argument
var userStub = new Stubs.IService<User>();
userStub.GetItem.Call((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

var productStub = new Stubs.IService<Product>();
productStub.GetItem.Call((id) => new Product { Id = id, Name = "FromStub" }).Verifiable();

// The stub IS the interface implementation (no .Object needed)
IService<User> userService = userStub;
var user = userService.GetItem(1);

userStub.Verify();
```
<!-- endSnippet -->

### Benefits

- **Flexible**: Use any type argument without defining separate stubs
- **No extra files**: Stub defined inline with tests
- **Type constraints**: Preserves constraints from the original generic type
- **Multiple types**: Use different type arguments in the same test class
- **Direct assignment**: Stub IS the interface implementation (no `.Object` needed)

### Trade-offs

- **Test-local only**: Cannot reuse across multiple test classes
- **No stub overrides**: Cannot add custom methods to the generated stub
- **typeof syntax**: Requires `typeof(IFoo<>)` with empty angle brackets
- **Stubs namespace**: Must use `Stubs.IFoo<T>` syntax

> **NOTE:** For reusable generic stubs across multiple test files, use the Generic Standalone pattern instead.

---

## Open Generic Class Pattern

The Open Generic Class pattern generates a generic stub class within your test class for stubbing abstract or virtual generic classes. Like the Inline Class pattern, you access the actual instance via the `.Object` property.

### When to Use

- You need to stub a generic abstract or virtual class
- You don't need custom methods on the stub
- You want to test with multiple type arguments in one test class
- You prefer inline definition over a separate file

### Basic Setup

<!-- snippet: skill-patterns-open-generic-class-basic -->
```cs
public abstract class ServiceBaseGeneric<T>
{
    public abstract T? GetItem(int id);
    public abstract void Process(T item);
}

[KnockOff(typeof(ServiceBaseGeneric<>))]
public partial class PtOpenGenericClassTests
{
    // The generator creates Stubs.ServiceBaseGeneric<T>
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: skill-patterns-open-generic-class-usage -->
```cs
// Open Generic Class: instantiate with any type argument, use .Object
var userStub = new Stubs.ServiceBaseGeneric<User>();
userStub.GetItem.Call((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

// IMPORTANT: .Object gives you the actual class instance
ServiceBaseGeneric<User> service = userStub.Object;
var user = service.GetItem(1);

userStub.Verify();
```
<!-- endSnippet -->

### Base Fallback Behavior

Like all class stubs, open generic class stubs call the base class implementation by default for unconfigured virtual methods. Abstract methods return `default(T)` when unconfigured. See the Inline Class pattern's "Base Fallback Behavior" section for details.

### Benefits

- **Flexible**: Use any type argument without defining separate stubs
- **No extra files**: Stub defined inline with tests
- **Type constraints**: Preserves constraints from the original generic type
- **Multiple types**: Use different type arguments in the same test class
- **Class support**: Works with abstract classes, not just interfaces
- **CallBase by default**: Virtual methods fall back to base implementation automatically

### Trade-offs

- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Test-local only**: Cannot reuse across multiple test classes
- **No stub overrides**: Cannot add custom methods to the generated stub
- **typeof syntax**: Requires `typeof(Foo<>)` with empty angle brackets
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`

### Key Difference from Open Generic Interface

| Aspect | Open Generic Interface | Open Generic Class |
|--------|------------------------|-------------------|
| **Syntax** | `[KnockOff(typeof(IFoo<>))]` | `[KnockOff(typeof(Foo<>))]` |
| **Instantiation** | `new Stubs.IFoo<T>()` | `new Stubs.Foo<T>().Object` |
| **Assignment** | `IFoo<T> foo = stub;` | `Foo<T> foo = stub.Object;` |
| **Best for** | Generic interfaces | Generic abstract/virtual classes |

> **NOTE:** For reusable generic stubs across multiple test files, use the Generic Standalone pattern instead.

---

## Pattern Comparison

| Feature | Standalone | Generic Standalone | Standalone Class | Generic Standalone Class | Inline Interface | Inline Class | Inline Delegate | Open Generic Interface | Open Generic Class |
|---------|------------|-------------------|-----------------|--------------------------|------------------|--------------|-----------------|----------------------|-------------------|
| **Reusable across test files** | Yes | Yes | Yes | Yes | No | No | No | No | No |
| **Custom stub overrides** | Yes | Yes | Yes | Yes | No | No | No | No | No |
| **Extra file required** | Yes | Yes | Yes | Yes | No | No | No | No | No |
| **Supports interfaces** | Yes | Yes | No | No | Yes | No | No | Yes | No |
| **Supports classes** | No | No | Yes | Yes | No | Yes | No | No | Yes |
| **Supports delegates** | No | No | No | No | No | No | Yes | Yes* | No |
| **Supports generics** | No | Yes | No | Yes | Closed only | Closed only | Closed only | Yes | Yes |
| **Uses .Object property** | No | No | Yes | Yes | No | Yes | No | No | Yes |
| **Source() delegation** | Yes | Yes | No | No | Yes | No | No | Yes | No |
| **Instantiation syntax** | `new MyStub()` | `new MyStub<T>()` | `new MyStub().Object` | `new MyStub<T>().Object` | `new Stubs.IFoo()` | `new Stubs.Foo().Object` | `new Stubs.Del()` | `new Stubs.IFoo<T>()` | `new Stubs.Foo<T>().Object` |
| **Best for** | Shared interface stubs | Shared generic interface stubs | Shared class stubs | Shared generic class stubs | Local interface stubs | Local class stubs | Delegate stubs | Local generic interface stubs | Local generic class stubs |

*Note: Open Generic Delegate (`[KnockOff(typeof(Factory<>))]`) behaves like Open Generic Interface (no `.Object`), as delegates are reference types that can be directly assigned.

---

## Choosing a Pattern

Follow this decision tree to select the appropriate pattern:

```
Is it a DELEGATE type?
|-- YES --> Inline Delegate pattern
|           [KnockOff<ValidationRule>]
|
|-- NO --> Is it a GENERIC interface/class?
    |
    |-- YES --> Do you need the stub in MULTIPLE test files?
    |   |
    |   |-- YES --> Generic Standalone pattern
    |   |           [KnockOff] class Stub<T> : IRepo<T>
    |   |
    |   |-- NO --> Is it a CLASS (not interface)?
    |       |
    |       |-- YES --> Open Generic Class pattern
    |       |           [KnockOff(typeof(ServiceBase<>))]
    |       |           Use: new Stubs.ServiceBase<T>().Object
    |       |
    |       |-- NO --> Do you need CUSTOM METHODS on the stub?
    |           |
    |           |-- YES --> Generic Standalone pattern
    |           |           [KnockOff] class Stub<T> : IRepo<T>
    |           |
    |           |-- NO --> Open Generic Interface pattern
    |                      [KnockOff(typeof(IRepo<>))]
    |                      Use: new Stubs.IRepo<T>()
    |
    |-- NO --> Is it a CLASS (not interface)?
        |
        |-- YES --> Inline Class pattern
        |           [KnockOff<SomeClass>]
        |
        |-- NO --> Do you need the stub in MULTIPLE test files?
            |
            |-- YES --> Standalone pattern
            |           [KnockOff] class Stub : IFoo
            |
            |-- NO --> Do you need CUSTOM METHODS on the stub?
                |
                |-- YES --> Standalone pattern
                |           [KnockOff] class Stub : IFoo
                |
                |-- NO --> Inline Interface pattern
                           [KnockOff<IFoo>]
```

### Examples by Scenario

| Scenario | Recommended Pattern |
|----------|---------------------|
| Repository stub used in 5+ test classes | Standalone |
| Stub with `WithAdminUser()` helper method | Standalone |
| Generic repository shared across tests | Generic Standalone |
| Quick stub for single test class | Inline Interface |
| Stub a `DbContext` with virtual `DbSet` properties | Inline Class |
| Stub an abstract base class | Inline Class |
| Stub a validation rule delegate | Inline Delegate |
| Stub a factory function delegate | Inline Delegate |
| Generic interface stub for one test class | Open Generic Interface |
| `IRepository<T>` for multiple types in one test | Open Generic Interface |
| Generic abstract class stub for one test class | Open Generic Class |
| `ServiceBase<T>` with virtual methods | Open Generic Class |

---

## Complete Example

This example demonstrates all nine patterns working together:

<!-- snippet: skill-patterns-complete-example -->
```cs
// 1. Standalone: direct instantiation
var emailStub = new PtEmailSvcStub();
emailStub.Send.Call((string to, string subject, string body) => true).Verifiable();
IEmailSvc email = emailStub;

// 2. Generic Standalone: reusable with type args
var notifierStub = new PtNotifierStub<User>();
notifierStub.Notify.Call((item) => { }).Verifiable();
INotifier<User> notifier = notifierStub;

// 3. Standalone Class: reusable class stub, uses .Object
var cacheStub = new PtServiceStub();
cacheStub.Initialize.Call(() => { }).Verifiable();
cacheStub.Name.Get(() => "TestService");
ServiceBase cache = cacheStub.Object;

// 4. Generic Standalone Class: reusable generic class stub, uses .Object
var repoStub = new PtRepositoryBaseStub<User>();
repoStub.GetById.Call((id) => new User { Id = id }).Verifiable();
RepositoryBase<User> repo = repoStub.Object;

// 5. Inline Interface: via Stubs namespace
var loggerStub = new PtInlineHost.Stubs.ILogger();
loggerStub.Log.Call((msg) => { }).Verifiable();
ILogger logger = loggerStub;

// 6. Inline Class: use .Object for class instance
var auditStub = new PtInlineHost.Stubs.AuditService();
auditStub.Audit.Call((action) => { }).Verifiable();
AuditService audit = auditStub.Object;

// 7. Inline Delegate: implicit conversion
var ruleStub = new PtDelegateHost.Stubs.ValidationRule();
ruleStub.Interceptor.Call((value) => true);
ValidationRule rule = ruleStub;

// 8. Open Generic Interface: inline stub with type args
var processorStub = new PtOpenGenericInterfaceHost.Stubs.IProcessor<Order>();
processorStub.Process.Call((item) => { }).Verifiable();
IProcessor<Order> processor = processorStub;

// 9. Open Generic Class: inline stub with type args, uses .Object
var serviceStub = new PtOpenGenericClassHost.Stubs.ServiceBaseGeneric<Order>();
serviceStub.GetItem.Call((id) => new Order { Id = id }).Verifiable();
ServiceBaseGeneric<Order> service = serviceStub.Object;  // .Object required for class patterns
```
<!-- endSnippet -->

---

**UPDATED:** 2026-02-08 (Nine patterns including Standalone Class stubs; class stub CallBase behavior)
