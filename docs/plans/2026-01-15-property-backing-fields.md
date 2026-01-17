# Property Backing Fields Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace string-based dictionary property access with generated strongly-typed `Property<T>` backing fields, enabling lazy loading and improved type safety.

**Architecture:** Generator creates `Property<T>` backing fields for each entity property. `IPropertyFactory<TOwner>` DI interface creates properties via factory pattern. Base class constructor calls `InitializePropertyBackingFields()` which registers properties with `PropertyManager`. Lazy loading configured via `OnLoad` func in constructors.

**Tech Stack:** C# source generators (Roslyn), .NET DI, MSTest

---

## Background

**Current pattern (string-based):**
<!-- pseudo:current-pattern -->
```csharp
public string Name
{
    get => Getter<string>();     // Dictionary lookup by CallerMemberName
    set => Setter(value);        // Dictionary lookup + cast
}
```
<!-- /snippet -->

**New pattern (typed backing fields):**
<!-- pseudo:new-pattern -->
```csharp
// Generator creates:
protected Property<string> NameProperty { get; private set; } = null!;

// User writes:
public string Name
{
    get => NameProperty.Value;
    set => NameProperty.Value = value;
}
```
<!-- /snippet -->

**Key insight:** Task tracking moves from `Setter()` to `_PropertyManager_NeatooPropertyChanged` event handler, where `eventArgs.Property.Task` is already available.

---

## Phase 1: DI Infrastructure

### Task 1.1: Create IPropertyFactory Interface

**Files:**
- Create: `src/Neatoo/IPropertyFactory.cs`
- Test: `src/Neatoo.UnitTest/Unit/Core/PropertyFactoryTests.cs`

**Step 1: Write the failing test**

<!-- pseudo:property-factory-test -->
```csharp
// src/Neatoo.UnitTest/Unit/Core/PropertyFactoryTests.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

[TestClass]
public class PropertyFactoryTests
{
    [TestMethod]
    public void Create_ReturnsPropertyWithCorrectName()
    {
        // Arrange
        var factory = new DefaultPropertyFactory<TestOwner>();
        var owner = CreateTestOwner();

        // Act
        var property = factory.Create<string>(owner, "TestProperty");

        // Assert
        Assert.IsNotNull(property);
        Assert.AreEqual("TestProperty", property.Name);
    }

    private TestOwner CreateTestOwner()
    {
        // Will implement after services are updated
        throw new NotImplementedException();
    }
}

// Minimal test owner stub for compilation
public class TestOwner : IBase
{
    // Stub implementation
}
```
<!-- /snippet -->

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~PropertyFactoryTests.Create_ReturnsPropertyWithCorrectName" --no-build`
Expected: FAIL - IPropertyFactory does not exist

**Step 3: Write IPropertyFactory interface**

<!-- pseudo:iproperty-factory -->
```csharp
// src/Neatoo/IPropertyFactory.cs
namespace Neatoo;

/// <summary>
/// Factory for creating Property instances. Generic TOwner allows per-type DI registration.
/// </summary>
/// <typeparam name="TOwner">The owning entity type</typeparam>
public interface IPropertyFactory<TOwner> where TOwner : IBase
{
    /// <summary>
    /// Creates a typed property instance.
    /// </summary>
    Property<TProperty> Create<TProperty>(TOwner owner, string propertyName);

    /// <summary>
    /// Creates a PropertyManager for the owner.
    /// </summary>
    IValidatePropertyManager<IValidateProperty> CreatePropertyManager(TOwner owner);
}
```
<!-- /snippet -->

**Step 4: Build to verify interface compiles**

Run: `dotnet build src/Neatoo`
Expected: SUCCESS

**Step 5: Commit**

```bash
git add src/Neatoo/IPropertyFactory.cs
git commit -m "$(cat <<'EOF'
feat: add IPropertyFactory<TOwner> interface

Introduces generic factory interface for creating strongly-typed Property<T>
instances. TOwner generic allows per-type DI registration for customization.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 1.2: Create DefaultPropertyFactory Implementation

**Files:**
- Create: `src/Neatoo/Internal/DefaultPropertyFactory.cs`
- Modify: `src/Neatoo/Internal/ValidateProperty.cs:19-32` (add factory-friendly constructor)
- Test: `src/Neatoo.UnitTest/Unit/Core/PropertyFactoryTests.cs`

**Step 1: Write additional failing tests**

<!-- pseudo:additional-factory-tests -->
```csharp
// Add to PropertyFactoryTests.cs
[TestMethod]
public void Create_PropertyHasCorrectType()
{
    var factory = new DefaultPropertyFactory<TestOwner>();
    var owner = CreateTestOwner();

    var property = factory.Create<int>(owner, "Age");

    Assert.AreEqual(typeof(int), property.Type);
}

[TestMethod]
public void CreatePropertyManager_ReturnsValidManager()
{
    var factory = new DefaultPropertyFactory<TestOwner>();
    var owner = CreateTestOwner();

    var manager = factory.CreatePropertyManager(owner);

    Assert.IsNotNull(manager);
}
```
<!-- /snippet -->

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~PropertyFactoryTests" --no-build`
Expected: FAIL - DefaultPropertyFactory does not exist

**Step 3: Write DefaultPropertyFactory implementation**

<!-- pseudo:default-property-factory -->
```csharp
// src/Neatoo/Internal/DefaultPropertyFactory.cs
using Neatoo.Internal;

namespace Neatoo.Internal;

/// <summary>
/// Default implementation of IPropertyFactory that creates standard Property instances.
/// </summary>
public class DefaultPropertyFactory<TOwner> : IPropertyFactory<TOwner> where TOwner : IBase
{
    private readonly IPropertyInfoList<TOwner> _propertyInfoList;

    public DefaultPropertyFactory(IPropertyInfoList<TOwner> propertyInfoList)
    {
        _propertyInfoList = propertyInfoList;
    }

    public Property<TProperty> Create<TProperty>(TOwner owner, string propertyName)
    {
        var propertyInfo = _propertyInfoList.GetPropertyInfo(propertyName);
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Property '{propertyName}' not found on type {typeof(TOwner).Name}", nameof(propertyName));
        }

        return new ValidateProperty<TProperty>(propertyInfo);
    }

    public IValidatePropertyManager<IValidateProperty> CreatePropertyManager(TOwner owner)
    {
        return new ValidatePropertyManager<IValidateProperty>(this, _propertyInfoList);
    }
}
```
<!-- /snippet -->

**Step 4: Build and run tests**

Run: `dotnet build src/Neatoo && dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~PropertyFactoryTests"`
Expected: Tests pass

**Step 5: Commit**

```bash
git add src/Neatoo/Internal/DefaultPropertyFactory.cs
git commit -m "$(cat <<'EOF'
feat: add DefaultPropertyFactory implementation

Creates Property<T> instances using PropertyInfoList metadata.
No reflection at property creation time - all info from generator.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 1.3: Add IPropertyFactory to Services Interfaces

**Files:**
- Modify: `src/Neatoo/IValidateBaseServices.cs:17-32`
- Modify: `src/Neatoo/Internal/ValidateBaseServices.cs` (implementation)

**Step 1: Update IValidateBaseServices interface**

<!-- pseudo:ivalidate-base-services-update -->
```csharp
// In IValidateBaseServices.cs, add property:
IPropertyFactory<T> PropertyFactory { get; }
```
<!-- /snippet -->

**Step 2: Update ValidateBaseServices implementation**

<!-- pseudo:validate-base-services-update -->
```csharp
// In ValidateBaseServices.cs, add:
public IPropertyFactory<T> PropertyFactory { get; }

// Update constructor to accept and store IPropertyFactory<T>
```
<!-- /snippet -->

**Step 3: Build to verify changes compile**

Run: `dotnet build src/Neatoo`
Expected: SUCCESS (may have warnings about missing DI registration)

**Step 4: Commit**

```bash
git add src/Neatoo/IValidateBaseServices.cs src/Neatoo/Internal/ValidateBaseServices.cs
git commit -m "$(cat <<'EOF'
feat: add PropertyFactory to services interfaces

IValidateBaseServices<T> now provides IPropertyFactory<T> for creating
typed property instances during entity initialization.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 1.4: Register DefaultPropertyFactory in DI

**Files:**
- Modify: `src/Neatoo/ServiceCollectionExtensions.cs` (or equivalent DI setup)

**Step 1: Add open generic registration**

<!-- pseudo:di-registration -->
```csharp
// In DI setup:
services.AddSingleton(typeof(IPropertyFactory<>), typeof(DefaultPropertyFactory<>));
```
<!-- /snippet -->

**Step 2: Build and verify**

Run: `dotnet build src/Neatoo`
Expected: SUCCESS

**Step 3: Commit**

```bash
git add src/Neatoo/ServiceCollectionExtensions.cs
git commit -m "$(cat <<'EOF'
feat: register DefaultPropertyFactory in DI

Open generic registration allows IPropertyFactory<T> resolution for any
entity type. Custom factories can override per-type.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2: Property<T> Lazy Loading

### Task 2.1: Add Lazy Loading Members to IValidateProperty

**Files:**
- Modify: `src/Neatoo/IValidateProperty.cs:26-121`
- Test: `src/Neatoo.UnitTest/Unit/Core/PropertyLazyLoadTests.cs`

**Step 1: Write failing tests for lazy loading**

<!-- pseudo:lazy-load-tests -->
```csharp
// src/Neatoo.UnitTest/Unit/Core/PropertyLazyLoadTests.cs
[TestClass]
public class PropertyLazyLoadTests
{
    [TestMethod]
    public async Task LazyLoad_WhenOnLoadConfigured_LoadsOnFirstAccess()
    {
        // Arrange
        var property = CreateProperty<string>("Name");
        property.OnLoad = () => Task.FromResult<string?>("Loaded Value");

        // Act
        var value = property.Value;
        await property.LoadTask!;

        // Assert
        Assert.AreEqual("Loaded Value", property.Value);
        Assert.IsTrue(property.IsLoaded);
    }

    [TestMethod]
    public async Task LazyLoad_WhenLoadFails_CreatesBrokenRule()
    {
        // Arrange
        var property = CreateProperty<string>("Name");
        property.OnLoad = () => throw new Exception("Load failed");

        // Act
        _ = property.Value; // Trigger load
        await Task.Delay(100); // Allow async to complete

        // Assert
        Assert.IsTrue(property.PropertyMessages.Any());
    }
}
```
<!-- /snippet -->

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~PropertyLazyLoadTests"`
Expected: FAIL - OnLoad, IsLoaded, LoadTask do not exist

**Step 3: Add lazy loading members to IValidateProperty**

<!-- pseudo:ivalidate-property-lazy -->
```csharp
// Add to IValidateProperty.cs:

/// <summary>
/// Async factory for lazy loading the property value.
/// When set and Value is null, accessing Value triggers load.
/// </summary>
Func<Task<object?>>? OnLoad { get; set; }

/// <summary>
/// True if OnLoad has completed (success or failure).
/// </summary>
bool IsLoaded { get; }

/// <summary>
/// The currently running or completed load task. Null if load not triggered.
/// </summary>
Task? LoadTask { get; }

/// <summary>
/// Explicitly triggers async load. Returns cached task if already loading.
/// </summary>
Task LoadAsync();
```
<!-- /snippet -->

**Step 4: Build to verify interface compiles**

Run: `dotnet build src/Neatoo`
Expected: FAIL - ValidateProperty doesn't implement new members

**Step 5: Commit interface change**

```bash
git add src/Neatoo/IValidateProperty.cs
git commit -m "$(cat <<'EOF'
feat: add lazy loading members to IValidateProperty

OnLoad, IsLoaded, LoadTask, LoadAsync() enable fire-and-forget lazy loading
with PropertyChanged notification on completion.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2.2: Implement Lazy Loading in ValidateProperty

**Files:**
- Modify: `src/Neatoo/Internal/ValidateProperty.cs`

**Step 1: Add lazy loading fields and implementation**

<!-- pseudo:validate-property-lazy -->
```csharp
// Add fields to ValidateProperty<T>:
private Func<Task<T?>>? _onLoad;
private Task? _loadTask;
private bool _isLoaded;

// Add properties:
public Func<Task<T?>>? OnLoad
{
    get => _onLoad;
    set => _onLoad = value;
}

public bool IsLoaded => _isLoaded;
public Task? LoadTask => _loadTask;

// Non-generic interface implementation:
Func<Task<object?>>? IValidateProperty.OnLoad
{
    get => _onLoad == null ? null : async () => await _onLoad();
    set => _onLoad = value == null ? null : async () => (T?)(await value());
}

// Modify Value getter to trigger lazy load:
public T? Value
{
    get
    {
        if (_value == null && _onLoad != null && _loadTask == null)
        {
            _loadTask = LoadAsync();
        }
        return _value;
    }
    set => SetValue(value);
}

// Implement LoadAsync:
public async Task LoadAsync()
{
    if (_onLoad == null) return;
    if (_loadTask != null)
    {
        await _loadTask;
        return;
    }

    try
    {
        _loadTask = LoadAsyncCore();
        await _loadTask;
    }
    catch (Exception ex)
    {
        // Add broken rule for load failure
        SetMessagesForRule(new PropertyMessage(
            "LazyLoad",
            PropertyMessageType.Error,
            $"Failed to load: {ex.Message}"));
    }
    finally
    {
        _isLoaded = true;
    }
}

private async Task LoadAsyncCore()
{
    var result = await _onLoad!();
    LoadValue(result);
    // Fire PropertyChanged via existing mechanism
}
```
<!-- /snippet -->

**Step 2: Build and run lazy load tests**

Run: `dotnet build src/Neatoo && dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~PropertyLazyLoadTests"`
Expected: Tests pass

**Step 3: Commit**

```bash
git add src/Neatoo/Internal/ValidateProperty.cs
git commit -m "$(cat <<'EOF'
feat: implement lazy loading in ValidateProperty

Fire-and-forget loading on first access when OnLoad configured.
Load failures surface as broken rules. PropertyChanged fires on completion.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3: Generator Updates

### Task 3.1: Analyze Current Generator Structure

**Files:**
- Read: `src/Neatoo.BaseGenerator/BaseGenerator.cs`

**Step 1: Review current generator**

Read the generator to understand:
- How it detects properties to generate
- Current output pattern (`get => Getter<T>()`)
- Where to add backing field generation

**Step 2: Document findings**

Note in this plan:
- Line numbers for property detection
- Line numbers for output generation
- Required changes

**Step 3: No commit (research only)**

---

### Task 3.2: Generate Property Backing Fields

**Files:**
- Modify: `src/Neatoo.BaseGenerator/BaseGenerator.cs`
- Test: Manual inspection of generated output

**Step 1: Update generator to produce backing fields**

For each partial property detected, generate:
<!-- pseudo:generated-backing-field -->
```csharp
protected Property<{PropertyType}> {PropertyName}Property { get; private set; } = null!;
```
<!-- /snippet -->

**Step 2: Generate InitializePropertyBackingFields override**

<!-- pseudo:generated-initialize-method -->
```csharp
protected override void InitializePropertyBackingFields(IPropertyFactory<{ClassName}> factory)
{
    base.InitializePropertyBackingFields(factory);
    {PropertyName}Property = factory.Create<{PropertyType}>(this, nameof({PropertyName}));
    PropertyManager.Register({PropertyName}Property);
}
```
<!-- /snippet -->

**Step 3: Update property getter/setter generation**

Change from:
<!-- pseudo:old-getter-setter -->
```csharp
public partial {Type} {Name} { get => Getter<{Type}>(); set => Setter(value); }
```
<!-- /snippet -->

To:
<!-- pseudo:new-getter-setter -->
```csharp
public partial {Type} {Name} { get => {Name}Property.Value; set => {Name}Property.Value = value; }
```
<!-- /snippet -->

**Step 4: Build and verify generated output**

Run: `dotnet build`
Inspect: `src/Neatoo.UnitTest/Generated/Neatoo.BaseGenerator/...` files

**Step 5: Commit**

```bash
git add src/Neatoo.BaseGenerator/
git commit -m "$(cat <<'EOF'
feat: generate Property<T> backing fields

Generator now creates typed backing fields and InitializePropertyBackingFields
override for each partial property. Replaces Getter<T>/Setter pattern.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 4: Base Class Updates

### Task 4.1: Add InitializePropertyBackingFields to ValidateBase

**Files:**
- Modify: `src/Neatoo/ValidateBase.cs:177-204` (constructor)
- Modify: `src/Neatoo/ValidateBase.cs:380-438` (Getter/Setter)

**Step 1: Add abstract InitializePropertyBackingFields method**

<!-- pseudo:abstract-initialize -->
```csharp
// In ValidateBase<T>:
protected abstract void InitializePropertyBackingFields(IPropertyFactory<T> factory);
```
<!-- /snippet -->

**Step 2: Call from constructor**

<!-- pseudo:constructor-call -->
```csharp
// In ValidateBase<T> constructor:
public ValidateBase(IValidateBaseServices<T> services)
{
    Services = services;
    PropertyManager = services.PropertyFactory.CreatePropertyManager((T)this);
    InitializePropertyBackingFields(services.PropertyFactory);
    // ... rest of constructor
}
```
<!-- /snippet -->

**Step 3: Build to verify (will fail - entities don't have override yet)**

Run: `dotnet build src/Neatoo`
Expected: FAIL - derived classes don't implement abstract method (expected at this stage)

**Step 4: Commit**

```bash
git add src/Neatoo/ValidateBase.cs
git commit -m "$(cat <<'EOF'
feat: add InitializePropertyBackingFields to ValidateBase

Abstract method called during construction to initialize generated
Property<T> backing fields. Generator provides override.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4.2: Move Task Tracking to Property Changed Handler

**Files:**
- Modify: `src/Neatoo/ValidateBase.cs` (_PropertyManager_NeatooPropertyChanged)

**Step 1: Update event handler to track property tasks**

<!-- pseudo:property-changed-handler -->
```csharp
// In _PropertyManager_NeatooPropertyChanged:
private Task _PropertyManager_NeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
{
    // Track async tasks from property operations
    if (eventArgs.Property?.Task is Task task && !task.IsCompleted)
    {
        if (this.Parent is IValidateBaseInternal parentInternal)
        {
            parentInternal.AddChildTask(task);
        }
        this.RunningTasks.AddTask(task);
    }

    return this.ChildNeatooPropertyChanged(eventArgs);
}
```
<!-- /snippet -->

**Step 2: Build and run existing tests**

Run: `dotnet build && dotnet test src/Neatoo.UnitTest`
Expected: Some tests may fail (expected during transition)

**Step 3: Commit**

```bash
git add src/Neatoo/ValidateBase.cs
git commit -m "$(cat <<'EOF'
refactor: move task tracking to PropertyChanged handler

Task tracking now happens in response to property events rather than
in Setter(). Uses eventArgs.Property.Task for cleaner separation.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4.3: Remove Getter<T>/Setter Methods

**Files:**
- Modify: `src/Neatoo/ValidateBase.cs:380-438`
- Modify: `src/Neatoo/Base.cs` (if Getter<T> is defined there)

**Step 1: Remove Getter<T> method**

Delete or mark obsolete:
<!-- pseudo:obsolete-getter -->
```csharp
[Obsolete("Use {PropertyName}Property.Value instead")]
protected virtual P? Getter<P>([CallerMemberName] string propertyName = "")
```
<!-- /snippet -->

**Step 2: Remove Setter<T> method**

Delete or mark obsolete:
<!-- pseudo:obsolete-setter -->
```csharp
[Obsolete("Use {PropertyName}Property.Value = value instead")]
protected virtual void Setter<P>(P? value, [CallerMemberName] string propertyName = "")
```
<!-- /snippet -->

**Step 3: Build (will fail - generated code still uses Getter/Setter)**

Run: `dotnet build`
Expected: FAIL until generator is updated (this task coordinates with 3.2)

**Step 4: Commit (after generator update)**

```bash
git add src/Neatoo/ValidateBase.cs src/Neatoo/Base.cs
git commit -m "$(cat <<'EOF'
refactor: remove Getter<T>/Setter<P> methods

Property access now uses generated backing fields directly:
get => NameProperty.Value; set => NameProperty.Value = value;

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 5: Update PropertyManager

### Task 5.1: Add Register Method to PropertyManager

**Files:**
- Modify: `src/Neatoo/Internal/ValidatePropertyManager.cs`

**Step 1: Add Register method**

<!-- pseudo:register-method -->
```csharp
// In ValidatePropertyManager<P>:
public void Register(P property)
{
    if (property == null) throw new ArgumentNullException(nameof(property));

    var name = property.Name;
    if (_propertyBag.ContainsKey(name))
    {
        throw new InvalidOperationException($"Property '{name}' already registered");
    }

    _propertyBag[name] = property;

    // Subscribe to property events
    property.NeatooPropertyChanged += PropertyNeatooPropertyChanged;
    property.PropertyChanged += PropertyPropertyChanged;
}
```
<!-- /snippet -->

**Step 2: Build and test**

Run: `dotnet build && dotnet test src/Neatoo.UnitTest`

**Step 3: Commit**

```bash
git add src/Neatoo/Internal/ValidatePropertyManager.cs
git commit -m "$(cat <<'EOF'
feat: add Register method to PropertyManager

Properties are now explicitly registered during InitializePropertyBackingFields
instead of lazy-loaded on first access.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 6: Update Tests

### Task 6.1: Update Unit Tests for Property System

**Files:**
- Modify: `src/Neatoo.UnitTest/Unit/Core/EntityPropertyTests.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/EntityPropertyManagerTests.cs`

**Step 1: Update tests to use new pattern**

For each test that creates properties manually, update to use factory:
<!-- pseudo:test-update-pattern -->
```csharp
// Old:
var property = new ValidateProperty<string>(propertyInfo);

// New:
var factory = new DefaultPropertyFactory<TestEntity>(propertyInfoList);
var property = factory.Create<string>(owner, "Name");
```
<!-- /snippet -->

**Step 2: Run all unit tests**

Run: `dotnet test src/Neatoo.UnitTest/Unit`

**Step 3: Commit**

```bash
git add src/Neatoo.UnitTest/Unit/
git commit -m "$(cat <<'EOF'
test: update unit tests for property factory pattern

Tests now use IPropertyFactory<T> to create properties instead of
direct constructor calls.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6.2: Update Integration Tests

**Files:**
- Modify: `src/Neatoo.UnitTest/Integration/Concepts/ValidateBase/*.cs`
- Modify: `src/Neatoo.UnitTest/Integration/Concepts/EntityBase/*.cs`

**Step 1: Verify all integration tests still pass**

Run: `dotnet test src/Neatoo.UnitTest/Integration`

**Step 2: Fix any failures**

Most should pass after generator updates. Fix any that assume old Getter/Setter pattern.

**Step 3: Commit**

```bash
git add src/Neatoo.UnitTest/Integration/
git commit -m "$(cat <<'EOF'
test: update integration tests for backing fields

Integration tests now work with generated Property<T> backing fields.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6.3: Add Lazy Loading Integration Tests

**Files:**
- Create: `src/Neatoo.UnitTest/Integration/Concepts/LazyLoading/LazyLoadTests.cs`

**Step 1: Write comprehensive lazy loading tests**

<!-- pseudo:lazy-load-integration -->
```csharp
[TestClass]
public class LazyLoadIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task LazyLoad_ChildCollection_LoadsOnAccess()
    {
        // Test that child collection lazy loads when accessed
    }

    [TestMethod]
    public async Task LazyLoad_Failure_SurfacesAsBrokenRule()
    {
        // Test that load failures create validation errors
    }

    [TestMethod]
    public async Task LazyLoad_Serialization_PreservesLoadedState()
    {
        // Test that serialization works with lazy-loaded properties
    }
}
```
<!-- /snippet -->

**Step 2: Run tests**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadIntegrationTests"`

**Step 3: Commit**

```bash
git add src/Neatoo.UnitTest/Integration/Concepts/LazyLoading/
git commit -m "$(cat <<'EOF'
test: add lazy loading integration tests

Covers fire-and-forget loading, failure handling, and serialization.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

## Phase 7: Documentation

### Task 7.1: Update property-system.md

**Files:**
- Modify: `docs/property-system.md`

**Step 1: Update documentation to reflect new pattern**

Document:
- New property declaration pattern
- Generated backing fields
- How to configure lazy loading
- Migration from old pattern

**Step 2: Verify code samples compile**

Run: `dotnet mdsnippets` (if using markdown snippets)

**Step 3: Commit**

```bash
git add docs/property-system.md
git commit -m "$(cat <<'EOF'
docs: update property system documentation

Documents generated Property<T> backing fields and lazy loading pattern.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 7.2: Create lazy-loading.md

**Files:**
- Create: `docs/lazy-loading.md`

**Step 1: Write lazy loading documentation**

Cover:
- Configuring OnLoad in constructor
- Fire-and-forget behavior
- Error handling (broken rules)
- Explicit loading with LoadAsync()
- IsBusy integration

**Step 2: Commit**

```bash
git add docs/lazy-loading.md
git commit -m "$(cat <<'EOF'
docs: add lazy loading documentation

Comprehensive guide to configuring and using lazy-loaded properties.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
EOF
)"
```

---

## Execution Order

**Critical path (must be sequential):**
1. Task 1.1 → 1.2 → 1.3 → 1.4 (DI foundation)
2. Task 2.1 → 2.2 (Lazy loading)
3. Task 3.1 → 3.2 (Generator)
4. Task 4.1 → 4.2 → 4.3 (Base classes)
5. Task 5.1 (PropertyManager)
6. Tasks 6.1, 6.2, 6.3 (Tests - can run in parallel)
7. Tasks 7.1, 7.2 (Docs - can run in parallel)

**Note:** Tasks 3.2 and 4.3 are tightly coupled - generator must produce new pattern before removing old Getter/Setter.

---

## Verification Commands

```powershell
# Full build
dotnet build

# All tests
dotnet test src/Neatoo.UnitTest

# Unit tests only
dotnet test src/Neatoo.UnitTest/Unit

# Integration tests only
dotnet test src/Neatoo.UnitTest/Integration

# Specific test class
dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~PropertyFactoryTests"

# View generated files
Get-ChildItem -Recurse src/Neatoo.UnitTest/Generated/*.g.cs | Select-Object -First 5 | Get-Content
```

---

## Rollback Plan

If issues arise:

1. Generator changes can be reverted independently - old Getter/Setter still works
2. Keep Getter<T>/Setter marked `[Obsolete]` instead of removing until migration complete
3. IPropertyFactory can be optional (default to existing behavior if not registered)

---

## Open Questions (from brainstorming)

1. **Keep Getter<T>/Setter as fallback?** → Decision: Remove entirely (no public release yet)
2. **Rule triggers** → Keep string-based for now, typed references as future enhancement
3. **Collection properties** → No special handling needed, Property<EntityList<T>> works
4. **Inheritance** → Generator handles via `base.InitializePropertyBackingFields(factory)` call
