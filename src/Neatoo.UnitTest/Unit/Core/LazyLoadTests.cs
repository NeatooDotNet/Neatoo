using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}

public class TestValue
{
    public string Name { get; }
    public TestValue(string name) => Name = name;
}
