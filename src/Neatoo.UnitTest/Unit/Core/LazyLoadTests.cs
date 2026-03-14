using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neatoo.UnitTest.Unit.Core;

[TestClass]
public class LazyLoadTests
{
    [TestMethod]
    public void Value_BeforeLoad_ReturnsNullSynchronously()
    {
        // Arrange -- Value getter triggers fire-and-forget load. With an async loader
        // (one that genuinely yields), Value returns null on first access because
        // the load hasn't completed yet.
        var continueLoad = new TaskCompletionSource<TestValue?>();
        var lazyLoad = new LazyLoad<TestValue>(async () => await continueLoad.Task);

        // Act
        var value = lazyLoad.Value;

        // Assert -- Value is null synchronously (the async load hasn't completed yet)
        Assert.IsNull(value);

        // Cleanup
        continueLoad.SetResult(new TestValue("loaded"));
    }

    [TestMethod]
    public void IsLoaded_BeforeLoad_ReturnsFalse()
    {
        // Arrange
        var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(new TestValue("loaded")));

        // Act & Assert
        Assert.IsFalse(lazyLoad.IsLoaded);
    }

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

    [TestMethod]
    public async Task LoadAsync_OnFailure_SetsErrorState()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Load failed");
        var lazyLoad = new LazyLoad<TestValue>(() => throw expectedException);

        // Act
        TestValue? result = null;
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

    [TestMethod]
    public async Task IsBusy_DelegatesToValue_WhenLoaded()
    {
        // Arrange
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

    #region Auto-Trigger Tests (Value getter triggers fire-and-forget load)

    [TestMethod]
    public async Task ValueAccess_TriggersFireAndForgetLoad()
    {
        // Arrange (Scenario 1, Rule 1 + 5) -- use a genuinely-async loader
        // so we can observe the null-before-load and then-loaded states
        var expected = new TestValue("loaded");
        var continueLoad = new TaskCompletionSource<TestValue?>();
        var lazyLoad = new LazyLoad<TestValue>(async () => await continueLoad.Task);

        // Act -- access Value to trigger fire-and-forget load
        var syncValue = lazyLoad.Value;

        // Assert -- Value is null synchronously (load hasn't completed)
        Assert.IsNull(syncValue);
        Assert.IsTrue(lazyLoad.IsLoading, "Load should be in progress after Value access");

        // Complete the load
        continueLoad.SetResult(expected);
        await lazyLoad.WaitForTasks();

        // After load, Value holds the loaded data
        Assert.AreSame(expected, lazyLoad.Value);
        Assert.IsTrue(lazyLoad.IsLoaded);
        Assert.IsFalse(lazyLoad.IsLoading);
    }

    [TestMethod]
    public async Task ValueAccess_AutoTrigger_CompletesSuccessfully()
    {
        // Arrange (Scenario 5, Rule 1 + 5) -- verify PropertyChanged fires
        var changedProperties = new List<string>();
        var expected = new TestValue("loaded");
        var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(expected));
        lazyLoad.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

        // Act -- trigger auto-load via Value getter
        _ = lazyLoad.Value;
        await lazyLoad.WaitForTasks();

        // Assert
        Assert.AreSame(expected, lazyLoad.Value);
        Assert.IsTrue(lazyLoad.IsLoaded);
        Assert.IsFalse(lazyLoad.IsLoading);
        CollectionAssert.Contains(changedProperties, nameof(LazyLoad<TestValue>.Value));
        CollectionAssert.Contains(changedProperties, nameof(LazyLoad<TestValue>.IsLoaded));
        CollectionAssert.Contains(changedProperties, nameof(LazyLoad<TestValue>.IsLoading));
    }

    [TestMethod]
    public void ValueAccess_AlreadyLoaded_ReturnsCachedValue()
    {
        // Arrange (Scenario 2, Rule 2) -- pre-loaded instance
        var loadCount = 0;
        var expected = new TestValue("hello");
        var lazyLoad = new LazyLoad<TestValue>(() =>
        {
            Interlocked.Increment(ref loadCount);
            return Task.FromResult<TestValue?>(new TestValue("should not load"));
        });
        // Pre-load using the pre-loaded constructor
        lazyLoad = new LazyLoad<TestValue>(expected);

        // Act
        var value = lazyLoad.Value;

        // Assert -- returns cached value immediately, no load triggered
        Assert.AreSame(expected, value);
        Assert.AreEqual(0, loadCount);
    }

    [TestMethod]
    public async Task ValueAccess_DuringLoad_DoesNotStartSecondLoad()
    {
        // Arrange (Scenario 3, Rule 3) -- explicit load in progress, then access Value
        var loadCount = 0;
        var continueLoad = new TaskCompletionSource<TestValue?>();

        var lazyLoad = new LazyLoad<TestValue>(async () =>
        {
            Interlocked.Increment(ref loadCount);
            return await continueLoad.Task;
        });

        // Start explicit load
        var loadTask = lazyLoad.LoadAsync();

        // Act -- access Value while load in progress
        var value = lazyLoad.Value;

        // Assert -- no second load triggered
        Assert.IsNull(value);
        Assert.AreEqual(1, loadCount);

        // Cleanup
        continueLoad.SetResult(new TestValue("loaded"));
        await loadTask;
        Assert.AreEqual(1, loadCount);
    }

    [TestMethod]
    public void ValueAccess_NoLoader_ReturnsNullWithoutException()
    {
        // Arrange (Scenario 4, Rule 4) -- deserialized instance with no loader
        var lazyLoad = new LazyLoad<TestValue>(); // Parameterless constructor, _loader = null

        // Act -- access Value on instance with no loader
        var value = lazyLoad.Value;

        // Assert -- returns null, no exception, no load triggered
        Assert.IsNull(value);
        Assert.IsFalse(lazyLoad.IsLoading);
        Assert.IsFalse(lazyLoad.IsLoaded);
        Assert.IsFalse(lazyLoad.HasLoadError);
    }

    [TestMethod]
    public async Task ValueAccess_AutoTrigger_LoadFailure_SetsErrorState()
    {
        // Arrange (Scenario 6, Rule 6)
        var lazyLoad = new LazyLoad<TestValue>(() => throw new InvalidOperationException("fail"));

        // Act -- trigger auto-load via Value getter (fire-and-forget)
        _ = lazyLoad.Value;

        // Wait for the auto-triggered load to complete (it will fail)
        // The TriggerLoadAsync wrapper catches the exception.
        // WaitForTasks returns _loadTask which is faulted, so await will throw.
        try
        {
            await lazyLoad.WaitForTasks();
        }
        catch (InvalidOperationException)
        {
            // Expected -- WaitForTasks surfaces the exception from the faulted _loadTask
        }

        // Assert -- error state is set, no unobserved exception
        Assert.IsTrue(lazyLoad.HasLoadError);
        Assert.AreEqual("fail", lazyLoad.LoadError);
        Assert.IsFalse(lazyLoad.IsLoading);
        Assert.IsFalse(lazyLoad.IsLoaded);
        Assert.IsFalse(((IValidateMetaProperties)lazyLoad).IsValid);
    }

    [TestMethod]
    public async Task ValueAccess_ConcurrentAccess_SharesOneLoad()
    {
        // Arrange (Scenario 7, Rule 7) -- multiple Value accesses before load starts
        var loadCount = 0;
        var continueLoad = new TaskCompletionSource<TestValue?>();
        var lazyLoad = new LazyLoad<TestValue>(async () =>
        {
            Interlocked.Increment(ref loadCount);
            return await continueLoad.Task;
        });

        // Act -- three concurrent Value accesses
        _ = lazyLoad.Value;
        _ = lazyLoad.Value;
        _ = lazyLoad.Value;

        continueLoad.SetResult(new TestValue("loaded"));
        await lazyLoad.WaitForTasks();

        // Assert -- only one loader invocation
        Assert.AreEqual(1, loadCount);
    }

    [TestMethod]
    public void IsLoadingAccess_DoesNotTriggerLoad()
    {
        // Arrange (Scenario 8, Rule 8)
        var loadCount = 0;
        var lazyLoad = new LazyLoad<TestValue>(() =>
        {
            Interlocked.Increment(ref loadCount);
            return Task.FromResult<TestValue?>(new TestValue("loaded"));
        });

        // Act -- access IsLoading without accessing Value
        var isLoading = lazyLoad.IsLoading;

        // Assert -- no load triggered
        Assert.IsFalse(isLoading);
        Assert.AreEqual(0, loadCount);
    }

    [TestMethod]
    public void IsLoadedAccess_DoesNotTriggerLoad()
    {
        // Arrange (Scenario 9, Rule 8)
        var loadCount = 0;
        var lazyLoad = new LazyLoad<TestValue>(() =>
        {
            Interlocked.Increment(ref loadCount);
            return Task.FromResult<TestValue?>(new TestValue("loaded"));
        });

        // Act -- access IsLoaded without accessing Value
        var isLoaded = lazyLoad.IsLoaded;

        // Assert -- no load triggered
        Assert.IsFalse(isLoaded);
        Assert.AreEqual(0, loadCount);
    }

    [TestMethod]
    public async Task ExplicitLoadAsync_StillWorksIdentically()
    {
        // Arrange (Scenario 12, Rule 11)
        var expected = new TestValue("explicit");
        var lazyLoad = new LazyLoad<TestValue>(() => Task.FromResult<TestValue?>(expected));

        // Act -- use explicit LoadAsync, not Value getter
        var result = await lazyLoad.LoadAsync();

        // Assert
        Assert.AreSame(expected, result);
        Assert.AreSame(expected, lazyLoad.Value);
        Assert.IsTrue(lazyLoad.IsLoaded);
    }

    [TestMethod]
    public async Task ExplicitLoadAsync_OnFailure_StillPropagatesException()
    {
        // Arrange (Scenario 12, Rule 11) -- explicit path must propagate exceptions
        var lazyLoad = new LazyLoad<TestValue>(() => throw new InvalidOperationException("explicit fail"));

        // Act & Assert -- exception propagates to caller
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => lazyLoad.LoadAsync());

        Assert.IsTrue(lazyLoad.HasLoadError);
        Assert.AreEqual("explicit fail", lazyLoad.LoadError);
    }

    [TestMethod]
    public async Task WaitForTasks_AfterAutoTrigger_AwaitsLoad()
    {
        // Arrange (Scenario 13, Rule 12)
        var continueLoad = new TaskCompletionSource<TestValue?>();
        var expected = new TestValue("waited");
        var lazyLoad = new LazyLoad<TestValue>(async () => await continueLoad.Task);

        // Act -- trigger auto-load
        _ = lazyLoad.Value;

        // Load is in progress; WaitForTasks should return _loadTask
        Assert.IsTrue(lazyLoad.IsLoading);

        continueLoad.SetResult(expected);
        await lazyLoad.WaitForTasks();

        // Assert -- load completed
        Assert.AreSame(expected, lazyLoad.Value);
        Assert.IsTrue(lazyLoad.IsLoaded);
        Assert.IsFalse(lazyLoad.IsLoading);
    }

    #endregion

    #region IEntityMetaProperties Tests

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

    // IsSavable tests removed — IsSavable moved to IEntityRoot, no longer on IEntityMetaProperties or LazyLoad<T>

    [TestMethod]
    public void IsChild_BeforeLoad_ReturnsFalse()
    {
        // Arrange
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(new TestEntityValue()));

        // Assert
        Assert.IsFalse(((IEntityMetaProperties)lazyLoad).IsChild);
    }

    [TestMethod]
    public void IsSelfModified_AlwaysReturnsFalse()
    {
        // Arrange - LazyLoad wrapper itself is never modified
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(new TestEntityValue { IsModifiedValue = true }));

        // Assert
        Assert.IsFalse(((IEntityMetaProperties)lazyLoad).IsSelfModified);
    }

    [TestMethod]
    public async Task IsNew_DelegatesToValue_WhenLoaded()
    {
        // Arrange
        var newValue = new TestEntityValue { IsNewValue = true };
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(newValue));

        // Act
        await lazyLoad.LoadAsync();

        // Assert
        Assert.IsTrue(((IEntityMetaProperties)lazyLoad).IsNew);
    }

    [TestMethod]
    public void IsNew_BeforeLoad_ReturnsFalse()
    {
        // Arrange
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(new TestEntityValue()));

        // Assert
        Assert.IsFalse(((IEntityMetaProperties)lazyLoad).IsNew);
    }

    [TestMethod]
    public async Task IsDeleted_DelegatesToValue_WhenLoaded()
    {
        // Arrange
        var deletedValue = new TestEntityValue { IsDeletedValue = true };
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(deletedValue));

        // Act
        await lazyLoad.LoadAsync();

        // Assert
        Assert.IsTrue(((IEntityMetaProperties)lazyLoad).IsDeleted);
    }

    [TestMethod]
    public void IsDeleted_BeforeLoad_ReturnsFalse()
    {
        // Arrange
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(new TestEntityValue()));

        // Assert
        Assert.IsFalse(((IEntityMetaProperties)lazyLoad).IsDeleted);
    }

    #endregion

    #region Serialization Tests

    [TestMethod]
    public void Serialization_PreserveValueAndLoadedState()
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

    #endregion

    #region Factory Tests

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

    [TestMethod]
    public void Factory_RegisteredInDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNeatooServices(NeatooFactory.Logical, typeof(LazyLoadTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Act
        var factory = provider.GetService<ILazyLoadFactory>();

        // Assert
        Assert.IsNotNull(factory);
        Assert.IsInstanceOfType(factory, typeof(LazyLoadFactory));
    }

    #endregion

    #region Nullable Reference Type Tests

    [TestMethod]
    public async Task NullableType_WithLoader_LoadsValue()
    {
        // Arrange
        var expected = new TestValue("nullable-loaded");
        var lazyLoad = new LazyLoad<TestValue?>(() => Task.FromResult<TestValue?>(expected));

        // Act
        var result = await lazyLoad.LoadAsync();

        // Assert
        Assert.AreSame(expected, result);
        Assert.AreSame(expected, lazyLoad.Value);
        Assert.IsTrue(lazyLoad.IsLoaded);
    }

    [TestMethod]
    public void NullableType_WithPreLoadedValue_IsLoaded()
    {
        // Arrange
        var expected = new TestValue("nullable-preloaded");

        // Act
        var lazyLoad = new LazyLoad<TestValue?>(expected);

        // Assert
        Assert.IsTrue(lazyLoad.IsLoaded);
        Assert.AreSame(expected, lazyLoad.Value);
    }

    [TestMethod]
    public async Task NullableType_Factory_WithLoader_CreatesLazyLoad()
    {
        // Arrange
        var factory = new LazyLoadFactory();
        var expected = new TestValue("factory-nullable");

        // Act
        var lazyLoad = factory.Create<TestValue?>(() => Task.FromResult<TestValue?>(expected));
        var result = await lazyLoad.LoadAsync();

        // Assert
        Assert.AreSame(expected, result);
    }

    [TestMethod]
    public void NullableType_Factory_WithValue_CreatesPreLoadedLazyLoad()
    {
        // Arrange
        var factory = new LazyLoadFactory();
        var expected = new TestValue("factory-nullable-preloaded");

        // Act
        var lazyLoad = factory.Create<TestValue?>(expected);

        // Assert
        Assert.IsTrue(lazyLoad.IsLoaded);
        Assert.AreSame(expected, lazyLoad.Value);
    }

    #endregion
}

public class TestValue
{
    public string Name { get; set; }

    public TestValue() => Name = string.Empty;
    public TestValue(string name) => Name = name;
}

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

public class TestEntityValue : IEntityMetaProperties, IValidateMetaProperties
{
    public bool IsModifiedValue { get; set; }
    public bool IsNewValue { get; set; }
    public bool IsDeletedValue { get; set; }

    // IEntityMetaProperties
    public bool IsChild => false;
    public bool IsModified => IsModifiedValue;
    public bool IsSelfModified => IsModifiedValue;
    public bool IsMarkedModified => false;
    public bool IsNew => IsNewValue;
    public bool IsDeleted => IsDeletedValue;

    // IValidateMetaProperties (required for interface)
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
