using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}

public class TestValue
{
    public string Name { get; }
    public TestValue(string name) => Name = name;
}
