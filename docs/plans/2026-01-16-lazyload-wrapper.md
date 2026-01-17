# LazyLoad<T> Wrapper Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace implicit lazy loading in `ValidateProperty<T>` with explicit `LazyLoad<T>` wrapper type that separates UI binding concerns from async loading.

**Architecture:** `LazyLoad<T>` wraps an async loader delegate, exposes state properties for UI binding (Value, IsLoading, IsLoaded), implements `IValidateMetaProperties` and `IEntityMetaProperties` by delegating to the wrapped value, and supports `await entity.Property` via `GetAwaiter()`. Factory interface `ILazyLoadFactory` injected via `[Service]` parameter.

**Tech Stack:** C#, .NET DI, MSTest, INotifyPropertyChanged

---

## Background

**Current pattern (implicit loading in ValidateProperty):**
```csharp
// Entity declaration
public string Name { get => Getter<string>(); set => Setter(value); }

// Configure lazy load (typically in constructor)
NameProperty.OnLoad = () => repository.GetNameAsync(Id);

// Access triggers load silently
var name = entity.Name;  // Magic: triggers background load
```

**Problems:**
- Accessing `.Name` has hidden side effects (triggers network call)
- No clear completion signal for imperative code
- UI binding and async concerns mixed in property getter

**New pattern (explicit LazyLoad<T>):**
```csharp
// Entity declaration
public LazyLoad<IHistory> History { get; private set; }

// Configure in constructor via factory
History = lazyLoadFactory.Create(() => repository.GetHistoryAsync(Id));

// Explicit async - no surprises
var history = await entity.History;  // Clear: async operation

// UI binding - state properties for reactive updates
@if (entity.History.IsLoading) { <Spinner /> }
else if (entity.History.Value is { } h) { <div>@h.Name</div> }
```

---

## Phase 1: Core LazyLoad<T> Class

### Task 1.1: Create Test File and First Failing Test

**Files:**
- Create: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Create test file with first test**

```csharp
// src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using System.Threading.Tasks;

namespace Neatoo.UnitTest.Unit.Core;

[TestClass]
public class LazyLoadTests
{
    [TestMethod]
    public void Value_BeforeLoad_ReturnsNull()
    {
        // Arrange
        var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(new TestValue("loaded")));

        // Act
        var value = lazyLoad.Value;

        // Assert
        Assert.IsNull(value);
    }
}

public class TestValue
{
    public string Name { get; }
    public TestValue(string name) => Name = name;
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Value_BeforeLoad_ReturnsNull" --no-build`
Expected: FAIL - LazyLoad<T> does not exist

**Step 3: Create minimal LazyLoad<T> class**

```csharp
// src/Neatoo/LazyLoad.cs
namespace Neatoo;

public class LazyLoad<T> where T : class
{
    private readonly Func<Task<T?>> _loader;
    private T? _value;

    public LazyLoad(Func<Task<T?>> loader)
    {
        _loader = loader;
    }

    public T? Value => _value;
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Value_BeforeLoad_ReturnsNull"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add LazyLoad<T> class with Value property"
```

---

### Task 1.2: Add IsLoaded Property

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for IsLoaded**

```csharp
[TestMethod]
public void IsLoaded_BeforeLoad_ReturnsFalse()
{
    // Arrange
    var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(new TestValue("loaded")));

    // Act & Assert
    Assert.IsFalse(lazyLoad.IsLoaded);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.IsLoaded_BeforeLoad_ReturnsFalse" --no-build`
Expected: FAIL - IsLoaded property does not exist

**Step 3: Add IsLoaded property**

Add to `LazyLoad.cs`:
```csharp
private bool _isLoaded;

public bool IsLoaded => _isLoaded;
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.IsLoaded_BeforeLoad_ReturnsFalse"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add IsLoaded property"
```

---

### Task 1.3: Add LoadAsync Method

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for LoadAsync**

```csharp
[TestMethod]
public async Task LoadAsync_LoadsValue()
{
    // Arrange
    var expected = new TestValue("loaded");
    var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(expected));

    // Act
    var result = await lazyLoad.LoadAsync();

    // Assert
    Assert.AreSame(expected, result);
    Assert.AreSame(expected, lazyLoad.Value);
    Assert.IsTrue(lazyLoad.IsLoaded);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_LoadsValue" --no-build`
Expected: FAIL - LoadAsync method does not exist

**Step 3: Implement LoadAsync**

```csharp
public async Task<T?> LoadAsync()
{
    _value = await _loader();
    _isLoaded = true;
    return _value;
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_LoadsValue"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add LoadAsync method"
```

---

### Task 1.4: Add IsLoading Property

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for IsLoading**

```csharp
[TestMethod]
public async Task IsLoading_DuringLoad_ReturnsTrue()
{
    // Arrange
    var loadStarted = new TaskCompletionSource<bool>();
    var continueLoad = new TaskCompletionSource<TestValue?>();

    var lazyLoad = new LazyLoad<TestValue>(async () =>
    {
        loadStarted.SetResult(true);
        return await continueLoad.Task;
    });

    // Act
    var loadTask = lazyLoad.LoadAsync();
    await loadStarted.Task;  // Wait for load to start

    // Assert
    Assert.IsTrue(lazyLoad.IsLoading);
    Assert.IsFalse(lazyLoad.IsLoaded);

    // Cleanup
    continueLoad.SetResult(new TestValue("loaded"));
    await loadTask;

    Assert.IsFalse(lazyLoad.IsLoading);
    Assert.IsTrue(lazyLoad.IsLoaded);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.IsLoading_DuringLoad_ReturnsTrue" --no-build`
Expected: FAIL - IsLoading property does not exist

**Step 3: Add IsLoading property**

```csharp
private bool _isLoading;

public bool IsLoading => _isLoading;

public async Task<T?> LoadAsync()
{
    _isLoading = true;
    try
    {
        _value = await _loader();
        _isLoaded = true;
        return _value;
    }
    finally
    {
        _isLoading = false;
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.IsLoading_DuringLoad_ReturnsTrue"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add IsLoading property"
```

---

### Task 1.5: Add GetAwaiter for await Support

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for await syntax**

```csharp
[TestMethod]
public async Task Await_LoadsValue()
{
    // Arrange
    var expected = new TestValue("loaded");
    var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(expected));

    // Act
    var result = await lazyLoad;  // Uses GetAwaiter

    // Assert
    Assert.AreSame(expected, result);
    Assert.IsTrue(lazyLoad.IsLoaded);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Await_LoadsValue" --no-build`
Expected: FAIL - Cannot await LazyLoad<T>

**Step 3: Add GetAwaiter method**

Add using:
```csharp
using System.Runtime.CompilerServices;
```

Add method:
```csharp
public TaskAwaiter<T?> GetAwaiter() => LoadAsync().GetAwaiter();
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Await_LoadsValue"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add GetAwaiter for await syntax support"
```

---

### Task 1.6: Handle Concurrent Load Requests

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for concurrent loads**

```csharp
[TestMethod]
public async Task LoadAsync_CalledConcurrently_OnlyLoadsOnce()
{
    // Arrange
    var loadCount = 0;
    var loadStarted = new TaskCompletionSource<bool>();
    var continueLoad = new TaskCompletionSource<TestValue?>();

    var lazyLoad = new LazyLoad<TestValue>(async () =>
    {
        Interlocked.Increment(ref loadCount);
        loadStarted.TrySetResult(true);
        return await continueLoad.Task;
    });

    // Act - start multiple concurrent loads
    var task1 = lazyLoad.LoadAsync();
    await loadStarted.Task;  // Ensure first load started
    var task2 = lazyLoad.LoadAsync();
    var task3 = lazyLoad.LoadAsync();

    var expected = new TestValue("loaded");
    continueLoad.SetResult(expected);

    var results = await Task.WhenAll(task1, task2, task3);

    // Assert - only one actual load
    Assert.AreEqual(1, loadCount);
    Assert.IsTrue(results.All(r => ReferenceEquals(r, expected)));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_CalledConcurrently_OnlyLoadsOnce"`
Expected: FAIL - loadCount is 3

**Step 3: Implement concurrent load protection**

```csharp
private readonly object _loadLock = new();
private Task<T?>? _loadTask;

public Task<T?> LoadAsync()
{
    if (_isLoaded)
        return Task.FromResult(_value);

    lock (_loadLock)
    {
        if (_loadTask != null)
            return _loadTask;

        _loadTask = LoadAsyncCore();
        return _loadTask;
    }
}

private async Task<T?> LoadAsyncCore()
{
    _isLoading = true;
    try
    {
        _value = await _loader();
        _isLoaded = true;
        return _value;
    }
    finally
    {
        _isLoading = false;
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_CalledConcurrently_OnlyLoadsOnce"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): handle concurrent load requests with single execution"
```

---

### Task 1.7: Already Loaded Returns Immediately

**Files:**
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for already loaded state**

```csharp
[TestMethod]
public async Task LoadAsync_WhenAlreadyLoaded_ReturnsImmediately()
{
    // Arrange
    var loadCount = 0;
    var lazyLoad = new LazyLoad<TestValue>(() =>
    {
        Interlocked.Increment(ref loadCount);
        return Task.FromResult<TestValue?>(new TestValue("loaded"));
    });

    // Act
    await lazyLoad.LoadAsync();  // First load
    await lazyLoad.LoadAsync();  // Should not trigger another load
    await lazyLoad.LoadAsync();  // Should not trigger another load

    // Assert
    Assert.AreEqual(1, loadCount);
}
```

**Step 2: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_WhenAlreadyLoaded_ReturnsImmediately"`
Expected: PASS (already implemented in Task 1.6)

**Step 3: Commit**

```bash
git add src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "test(LazyLoad): verify already loaded returns immediately"
```

---

## Phase 2: Error Handling

### Task 2.1: Add HasLoadError and LoadError Properties

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for load error**

```csharp
[TestMethod]
public async Task LoadAsync_OnFailure_SetsErrorState()
{
    // Arrange
    var expectedException = new InvalidOperationException("Load failed");
    var lazyLoad = new LazyLoad<TestValue>(() => throw expectedException);

    // Act
    T? result = null;
    try
    {
        result = await lazyLoad.LoadAsync();
    }
    catch (InvalidOperationException)
    {
        // Expected
    }

    // Assert
    Assert.IsTrue(lazyLoad.HasLoadError);
    Assert.AreEqual("Load failed", lazyLoad.LoadError);
    Assert.IsFalse(lazyLoad.IsLoading);
    Assert.IsFalse(lazyLoad.IsLoaded);
    Assert.IsNull(lazyLoad.Value);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_OnFailure_SetsErrorState" --no-build`
Expected: FAIL - HasLoadError/LoadError do not exist

**Step 3: Add error properties**

```csharp
private string? _loadError;

public bool HasLoadError => _loadError != null;
public string? LoadError => _loadError;

private async Task<T?> LoadAsyncCore()
{
    _isLoading = true;
    try
    {
        _value = await _loader();
        _isLoaded = true;
        return _value;
    }
    catch (Exception ex)
    {
        _loadError = ex.Message;
        throw;
    }
    finally
    {
        _isLoading = false;
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_OnFailure_SetsErrorState"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add error state tracking on load failure"
```

---

## Phase 3: INotifyPropertyChanged

### Task 3.1: Implement INotifyPropertyChanged

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for property changed**

```csharp
[TestMethod]
public async Task LoadAsync_RaisesPropertyChangedForAllStateProperties()
{
    // Arrange
    var changedProperties = new List<string>();
    var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(new TestValue("loaded")));
    lazyLoad.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

    // Act
    await lazyLoad.LoadAsync();

    // Assert
    CollectionAssert.Contains(changedProperties, nameof(LazyLoad<TestValue>.Value));
    CollectionAssert.Contains(changedProperties, nameof(LazyLoad<TestValue>.IsLoaded));
    CollectionAssert.Contains(changedProperties, nameof(LazyLoad<TestValue>.IsLoading));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_RaisesPropertyChangedForAllStateProperties" --no-build`
Expected: FAIL - PropertyChanged event does not exist

**Step 3: Implement INotifyPropertyChanged**

Add using and interface:
```csharp
using System.ComponentModel;

public class LazyLoad<T> : INotifyPropertyChanged where T : class
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
```

Update LoadAsyncCore:
```csharp
private async Task<T?> LoadAsyncCore()
{
    _isLoading = true;
    OnPropertyChanged(nameof(IsLoading));
    try
    {
        _value = await _loader();
        _isLoaded = true;
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(IsLoaded));
        return _value;
    }
    catch (Exception ex)
    {
        _loadError = ex.Message;
        OnPropertyChanged(nameof(HasLoadError));
        OnPropertyChanged(nameof(LoadError));
        throw;
    }
    finally
    {
        _isLoading = false;
        OnPropertyChanged(nameof(IsLoading));
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.LoadAsync_RaisesPropertyChangedForAllStateProperties"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): implement INotifyPropertyChanged"
```

---

## Phase 4: IValidateMetaProperties

### Task 4.1: Implement IValidateMetaProperties Interface

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for IValidateMetaProperties delegation**

```csharp
[TestMethod]
public async Task IsBusy_DelegatesToValue_WhenLoaded()
{
    // Arrange - need a TestValue that implements IValidateMetaProperties
    var busyValue = new TestValidateValue { IsBusyValue = true };
    var lazyLoad = new LazyLoad<TestValidateValue>(() => Task.FromResult<TestValidateValue?>(busyValue));

    // Act
    await lazyLoad.LoadAsync();

    // Assert
    Assert.IsTrue(((IValidateMetaProperties)lazyLoad).IsBusy);
}

[TestMethod]
public void IsBusy_WhenLoading_ReturnsTrue()
{
    // Arrange
    var continueLoad = new TaskCompletionSource<TestValue?>();
    var lazyLoad = new LazyLoad<TestValue>(async () => await continueLoad.Task);

    // Act
    var _ = lazyLoad.LoadAsync();

    // Assert
    Assert.IsTrue(((IValidateMetaProperties)lazyLoad).IsBusy);

    // Cleanup
    continueLoad.SetResult(null);
}

[TestMethod]
public void IsValid_WhenHasLoadError_ReturnsFalse()
{
    // Arrange
    var lazyLoad = new LazyLoad<TestValue>(() => throw new Exception("fail"));

    // Act
    try { lazyLoad.LoadAsync().GetAwaiter().GetResult(); } catch { }

    // Assert
    Assert.IsFalse(((IValidateMetaProperties)lazyLoad).IsValid);
}

// Helper class
public class TestValidateValue : IValidateMetaProperties
{
    public bool IsBusyValue { get; set; }
    public bool IsValidValue { get; set; } = true;

    public bool IsBusy => IsBusyValue;
    public bool IsValid => IsValidValue;
    public bool IsSelfValid => IsValidValue;
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => Array.Empty<IPropertyMessage>();
    public Task WaitForTasks() => Task.CompletedTask;
    public Task WaitForTasks(CancellationToken token) => Task.CompletedTask;
    public Task RunRules(string propertyName, CancellationToken? token = null) => Task.CompletedTask;
    public Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null) => Task.CompletedTask;
    public void ClearAllMessages() { }
    public void ClearSelfMessages() { }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests" --no-build`
Expected: FAIL - LazyLoad<T> does not implement IValidateMetaProperties

**Step 3: Implement IValidateMetaProperties**

```csharp
public class LazyLoad<T> : INotifyPropertyChanged, IValidateMetaProperties where T : class
{
    // IValidateMetaProperties implementation
    public bool IsBusy => IsLoading || (_value as IValidateMetaProperties)?.IsBusy ?? false;

    public bool IsValid => !HasLoadError && (_value as IValidateMetaProperties)?.IsValid ?? true;

    public bool IsSelfValid => !HasLoadError;

    public IReadOnlyCollection<IPropertyMessage> PropertyMessages
    {
        get
        {
            if (HasLoadError)
            {
                // Return load error as a property message
                // Note: We need a way to create a PropertyMessage without an IValidateProperty
                // For now, return empty - we'll address this in a later task
                return Array.Empty<IPropertyMessage>();
            }
            return (_value as IValidateMetaProperties)?.PropertyMessages ?? Array.Empty<IPropertyMessage>();
        }
    }

    public Task WaitForTasks()
    {
        if (_loadTask != null && !_loadTask.IsCompleted)
            return _loadTask;
        return (_value as IValidateMetaProperties)?.WaitForTasks() ?? Task.CompletedTask;
    }

    public Task WaitForTasks(CancellationToken token)
    {
        if (_loadTask != null && !_loadTask.IsCompleted)
            return _loadTask.WaitAsync(token);
        return (_value as IValidateMetaProperties)?.WaitForTasks(token) ?? Task.CompletedTask;
    }

    public Task RunRules(string propertyName, CancellationToken? token = null)
        => (_value as IValidateMetaProperties)?.RunRules(propertyName, token) ?? Task.CompletedTask;

    public Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null)
        => (_value as IValidateMetaProperties)?.RunRules(runRules, token) ?? Task.CompletedTask;

    public void ClearAllMessages()
    {
        _loadError = null;
        (_value as IValidateMetaProperties)?.ClearAllMessages();
    }

    public void ClearSelfMessages()
    {
        _loadError = null;
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): implement IValidateMetaProperties with delegation"
```

---

## Phase 5: IEntityMetaProperties

### Task 5.1: Implement IEntityMetaProperties Interface

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for IEntityMetaProperties delegation**

```csharp
[TestMethod]
public async Task IsModified_DelegatesToValue_WhenLoaded()
{
    // Arrange
    var modifiedValue = new TestEntityValue { IsModifiedValue = true };
    var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(modifiedValue));

    // Act
    await lazyLoad.LoadAsync();

    // Assert
    Assert.IsTrue(((IEntityMetaProperties)lazyLoad).IsModified);
}

[TestMethod]
public void IsModified_BeforeLoad_ReturnsFalse()
{
    // Arrange
    var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(new TestEntityValue()));

    // Assert
    Assert.IsFalse(((IEntityMetaProperties)lazyLoad).IsModified);
}

// Helper class
public class TestEntityValue : IEntityMetaProperties, IValidateMetaProperties
{
    public bool IsModifiedValue { get; set; }
    public bool IsSavableValue { get; set; }

    // IEntityMetaProperties
    public bool IsChild => false;
    public bool IsModified => IsModifiedValue;
    public bool IsSelfModified => IsModifiedValue;
    public bool IsMarkedModified => false;
    public bool IsSavable => IsSavableValue;

    // IValidateMetaProperties
    public bool IsBusy => false;
    public bool IsValid => true;
    public bool IsSelfValid => true;
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => Array.Empty<IPropertyMessage>();
    public Task WaitForTasks() => Task.CompletedTask;
    public Task WaitForTasks(CancellationToken token) => Task.CompletedTask;
    public Task RunRules(string propertyName, CancellationToken? token = null) => Task.CompletedTask;
    public Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null) => Task.CompletedTask;
    public void ClearAllMessages() { }
    public void ClearSelfMessages() { }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.IsModified" --no-build`
Expected: FAIL - LazyLoad<T> does not implement IEntityMetaProperties

**Step 3: Implement IEntityMetaProperties**

```csharp
public class LazyLoad<T> : INotifyPropertyChanged, IValidateMetaProperties, IEntityMetaProperties where T : class
{
    // IEntityMetaProperties implementation
    public bool IsChild => (_value as IEntityMetaProperties)?.IsChild ?? false;

    public bool IsModified => (_value as IEntityMetaProperties)?.IsModified ?? false;

    public bool IsSelfModified => false;  // LazyLoad itself is never modified

    public bool IsMarkedModified => (_value as IEntityMetaProperties)?.IsMarkedModified ?? false;

    public bool IsSavable => (_value as IEntityMetaProperties)?.IsSavable ?? false;
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.IsModified"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): implement IEntityMetaProperties with delegation"
```

---

## Phase 6: Factory

### Task 6.1: Create ILazyLoadFactory Interface

**Files:**
- Create: `src/Neatoo/ILazyLoadFactory.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add test for factory**

```csharp
[TestMethod]
public async Task Factory_Create_WithLoader_CreatesLazyLoad()
{
    // Arrange
    var factory = new LazyLoadFactory();
    var expected = new TestValue("loaded");

    // Act
    var lazyLoad = factory.Create(() => Task.FromResult<TestValue?>(expected));
    var result = await lazyLoad;

    // Assert
    Assert.AreSame(expected, result);
}

[TestMethod]
public void Factory_Create_WithValue_CreatesPreLoadedLazyLoad()
{
    // Arrange
    var factory = new LazyLoadFactory();
    var expected = new TestValue("preloaded");

    // Act
    var lazyLoad = factory.Create(expected);

    // Assert
    Assert.IsTrue(lazyLoad.IsLoaded);
    Assert.AreSame(expected, lazyLoad.Value);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Factory" --no-build`
Expected: FAIL - ILazyLoadFactory/LazyLoadFactory do not exist

**Step 3: Create interface and implementation**

```csharp
// src/Neatoo/ILazyLoadFactory.cs
namespace Neatoo;

public interface ILazyLoadFactory
{
    LazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader) where TChild : class;
    LazyLoad<TChild> Create<TChild>(TChild? value) where TChild : class;
}

public class LazyLoadFactory : ILazyLoadFactory
{
    public LazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader) where TChild : class
    {
        return new LazyLoad<TChild>(loader);
    }

    public LazyLoad<TChild> Create<TChild>(TChild? value) where TChild : class
    {
        return new LazyLoad<TChild>(value);
    }
}
```

**Step 4: Add constructor for pre-loaded value to LazyLoad<T>**

```csharp
// In LazyLoad.cs - add second constructor
public LazyLoad(T? value)
{
    _loader = () => Task.FromResult(value);
    _value = value;
    _isLoaded = true;
}
```

**Step 5: Run tests to verify they pass**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Factory"`
Expected: PASS

**Step 6: Commit**

```bash
git add src/Neatoo/ILazyLoadFactory.cs src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add ILazyLoadFactory interface and implementation"
```

---

### Task 6.2: Register Factory in DI

**Files:**
- Modify: `src/Neatoo/AddNeatooServices.cs`
- Modify: `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs`

**Step 1: Add DI integration test**

```csharp
[TestMethod]
public void Factory_RegisteredInDI()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddNeatooServices(typeof(LazyLoadTests).Assembly);
    var provider = services.BuildServiceProvider();

    // Act
    var factory = provider.GetService<ILazyLoadFactory>();

    // Assert
    Assert.IsNotNull(factory);
    Assert.IsInstanceOfType(factory, typeof(LazyLoadFactory));
}
```

Add using at top of test file:
```csharp
using Microsoft.Extensions.DependencyInjection;
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Factory_RegisteredInDI"`
Expected: FAIL - ILazyLoadFactory not registered

**Step 3: Register in AddNeatooServices**

In `src/Neatoo/AddNeatooServices.cs`, add:
```csharp
services.AddTransient<ILazyLoadFactory, LazyLoadFactory>();
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Factory_RegisteredInDI"`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Neatoo/AddNeatooServices.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): register ILazyLoadFactory in DI"
```

---

## Phase 7: Remove Old Lazy Loading

### Task 7.1: Remove Lazy Loading from IValidateProperty

**Files:**
- Modify: `src/Neatoo/IValidateProperty.cs`

**Step 1: Remove lazy loading members from interface**

Remove from `IValidateProperty` (non-generic):
- `bool IsLoaded { get; }`
- `Task? LoadTask { get; }`
- `Task LoadAsync();`

Remove from `IValidateProperty<T>` (generic):
- `Func<Task<T?>>? OnLoad { get; set; }`
- `new Task<T?> LoadAsync();`

**Step 2: Build to find compilation errors**

Run: `dotnet build src/Neatoo`
Expected: FAIL - Compilation errors in ValidateProperty.cs

**Step 3: Commit interface changes**

```bash
git add src/Neatoo/IValidateProperty.cs
git commit -m "refactor(LazyLoad): remove lazy loading from IValidateProperty interface"
```

---

### Task 7.2: Remove Lazy Loading from ValidateProperty<T>

**Files:**
- Modify: `src/Neatoo/Internal/ValidateProperty.cs`

**Step 1: Remove lazy loading fields**

Remove:
```csharp
private readonly object _lazyLoadLock = new object();
private Func<Task<T?>>? _onLoad;
private Task<T?>? _loadTask;
private bool _isLoaded = true;
private const uint LazyLoadRuleId = 0xFFFFFFFF;
```

**Step 2: Remove lazy loading properties**

Remove:
```csharp
public Func<Task<T?>>? OnLoad { get; set; }
public bool IsLoaded => _isLoaded;
public Task? LoadTask => _loadTask;
```

**Step 3: Simplify Value getter**

Replace the complex Value getter with lazy load logic with simple getter:
```csharp
public T? Value
{
    get => _value;
    // ... rest of setter unchanged
}
```

**Step 4: Remove lazy loading methods**

Remove:
- `TriggerLazyLoadAsync()` method
- `LoadAsync()` methods (both generic and non-generic)
- `SetLazyLoadError()` method
- `ClearLazyLoadError()` method

**Step 5: Update IsBusy if it references _loadTask**

If IsBusy references `_loadTask`, remove that reference.

**Step 6: Update WaitForTasks if it references _loadTask**

If WaitForTasks references `_loadTask`, remove that reference.

**Step 7: Build to verify**

Run: `dotnet build src/Neatoo`
Expected: PASS (or remaining errors to fix)

**Step 8: Commit**

```bash
git add src/Neatoo/Internal/ValidateProperty.cs
git commit -m "refactor(LazyLoad): remove lazy loading implementation from ValidateProperty<T>"
```

---

### Task 7.3: Update or Remove Old Lazy Loading Tests

**Files:**
- Modify: `src/Neatoo.UnitTest/Integration/Concepts/ValidateBase/LazyLoadingTests.cs`

**Step 1: Review existing tests**

The file `LazyLoadingTests.cs` has 18 tests for the old lazy loading behavior. These need to be:
1. Removed (if testing deprecated behavior)
2. Migrated to test `LazyLoad<T>` (if testing important scenarios)

**Step 2: Delete or comment out old tests**

Since the behavior is fundamentally different (explicit vs implicit loading), most tests should be deleted. The new `LazyLoadTests.cs` covers the new behavior.

**Step 3: Build and run all tests**

Run: `dotnet test src/Neatoo.UnitTest`
Expected: Should identify any remaining compilation errors

**Step 4: Commit**

```bash
git add src/Neatoo.UnitTest/Integration/Concepts/ValidateBase/LazyLoadingTests.cs
git commit -m "test(LazyLoad): remove old lazy loading tests (replaced by LazyLoadTests)"
```

---

## Phase 8: Serialization Strategy

### Task 8.1: Handle LazyLoad<T> Serialization

**Files:**
- Modify: `src/Neatoo/LazyLoad.cs`

**Discussion:** LazyLoad<T> needs a serialization strategy for client-server scenarios:

1. **Option A: Don't serialize the loader** - Serialize only Value and IsLoaded. After deserialization, LoadAsync() would throw if not loaded.
2. **Option B: Always serialize as loaded** - Require pre-loading before serialization.
3. **Option C: Serialize as "unloaded" marker** - Client receives empty LazyLoad, must call API to load.

**Recommended: Option A** - Serialize Value and IsLoaded state only. The loader delegate cannot be serialized. This matches the design where server pre-loads data in Fetch methods.

**Step 1: Add JsonIgnore to non-serializable members**

```csharp
using System.Text.Json.Serialization;

public class LazyLoad<T> : INotifyPropertyChanged, IValidateMetaProperties, IEntityMetaProperties where T : class
{
    [JsonIgnore]
    private readonly Func<Task<T?>>? _loader;

    [JsonIgnore]
    private readonly object _loadLock = new();

    [JsonIgnore]
    private Task<T?>? _loadTask;

    [JsonIgnore]
    public bool IsLoading => _isLoading;

    // Value and IsLoaded can be serialized
    public T? Value => _value;
    public bool IsLoaded => _isLoaded;

    // For deserialization - parameterless constructor
    [JsonConstructor]
    public LazyLoad()
    {
        _loader = null;
        _isLoaded = false;
    }
}
```

**Step 2: Add test for serialization round-trip**

```csharp
[TestMethod]
public async Task Serialization_PreserveValueAndLoadedState()
{
    // Arrange
    var factory = new LazyLoadFactory();
    var original = factory.Create(new TestValue("serialized"));

    // Act
    var json = JsonSerializer.Serialize(original);
    var deserialized = JsonSerializer.Deserialize<LazyLoad<TestValue>>(json);

    // Assert
    Assert.IsNotNull(deserialized);
    Assert.IsTrue(deserialized.IsLoaded);
    Assert.AreEqual("serialized", deserialized.Value?.Name);
}
```

**Step 3: Run test to verify it passes**

Run: `dotnet test src/Neatoo.UnitTest --filter "FullyQualifiedName~LazyLoadTests.Serialization"`
Expected: PASS

**Step 4: Commit**

```bash
git add src/Neatoo/LazyLoad.cs src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs
git commit -m "feat(LazyLoad): add JSON serialization support"
```

---

## Phase 9: Final Integration

### Task 9.1: Run Full Test Suite

**Step 1: Build entire solution**

Run: `dotnet build`
Expected: PASS

**Step 2: Run all tests**

Run: `dotnet test`
Expected: PASS (or document failures)

**Step 3: Fix any remaining issues**

Address any test failures or compilation errors discovered.

**Step 4: Commit fixes if any**

```bash
git add -A
git commit -m "fix(LazyLoad): address integration issues"
```

---

### Task 9.2: Update Design Document Status

**Files:**
- Modify: `docs/todos/lazy-loading-v2-design.md`

**Step 1: Update status to Complete**

Change status from "Design Complete" to "Complete".

**Step 2: Update Implementation Tasks checkboxes**

Mark all implemented tasks as complete.

**Step 3: Add completion note to Progress Log**

```markdown
### 2026-01-16 (Implementation Complete)
- Implemented LazyLoad<T> class with all properties
- Implemented IValidateMetaProperties and IEntityMetaProperties delegation
- Implemented INotifyPropertyChanged
- Implemented concurrent load protection
- Created ILazyLoadFactory and registered in DI
- Removed old lazy loading from ValidateProperty<T>
- Added JSON serialization support
- All tests passing
```

**Step 4: Commit**

```bash
git add docs/todos/lazy-loading-v2-design.md
git commit -m "docs: mark LazyLoad<T> implementation complete"
```

---

### Task 9.3: Move Design Doc to Completed

**Step 1: Move file**

Run: `git mv docs/todos/lazy-loading-v2-design.md docs/todos/completed/lazy-loading-v2-design.md`

**Step 2: Commit**

```bash
git add -A
git commit -m "docs: move lazy-loading-v2-design to completed"
```

---

## Summary of Files

| File | Action |
|------|--------|
| `src/Neatoo/LazyLoad.cs` | Create |
| `src/Neatoo/ILazyLoadFactory.cs` | Create |
| `src/Neatoo/AddNeatooServices.cs` | Modify (add DI registration) |
| `src/Neatoo/IValidateProperty.cs` | Modify (remove lazy loading) |
| `src/Neatoo/Internal/ValidateProperty.cs` | Modify (remove lazy loading) |
| `src/Neatoo.UnitTest/Unit/Core/LazyLoadTests.cs` | Create |
| `src/Neatoo.UnitTest/Integration/Concepts/ValidateBase/LazyLoadingTests.cs` | Delete or gut |
| `docs/todos/lazy-loading-v2-design.md` | Move to completed |
