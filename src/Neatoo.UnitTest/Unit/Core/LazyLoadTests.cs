using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    [TestMethod]
    public async Task IsSavable_DelegatesToValue_WhenLoaded()
    {
        // Arrange
        var savableValue = new TestEntityValue { IsSavableValue = true };
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(savableValue));

        // Act
        await lazyLoad.LoadAsync();

        // Assert
        Assert.IsTrue(((IEntityMetaProperties)lazyLoad).IsSavable);
    }

    [TestMethod]
    public void IsSavable_BeforeLoad_ReturnsFalse()
    {
        // Arrange
        var lazyLoad = new LazyLoad<TestEntityValue>(() => Task.FromResult<TestEntityValue?>(new TestEntityValue()));

        // Assert
        Assert.IsFalse(((IEntityMetaProperties)lazyLoad).IsSavable);
    }

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
}

public class TestValue
{
    public string Name { get; }
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
    public bool IsSavableValue { get; set; }
    public bool IsNewValue { get; set; }
    public bool IsDeletedValue { get; set; }

    // IEntityMetaProperties
    public bool IsChild => false;
    public bool IsModified => IsModifiedValue;
    public bool IsSelfModified => IsModifiedValue;
    public bool IsMarkedModified => false;
    public bool IsSavable => IsSavableValue;
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
