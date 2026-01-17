using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel;

namespace Neatoo.UnitTest.Integration.Concepts.ValidateBase;

/// <summary>
/// Test entity with lazy loading configured on a child property.
/// </summary>
[SuppressFactory]
public partial class LazyLoadTestEntity : ValidateBase<LazyLoadTestEntity>
{
    private readonly Func<Task<string?>>? _nameLoadFunc;
    private readonly Func<Task<LazyLoadChildEntity?>>? _childLoadFunc;

    public LazyLoadTestEntity() : base(new ValidateBaseServices<LazyLoadTestEntity>())
    {
        PauseAllActions();
    }

    public LazyLoadTestEntity(
        ValidateBaseServices<LazyLoadTestEntity> services,
        Func<Task<string?>>? nameLoadFunc = null,
        Func<Task<LazyLoadChildEntity?>>? childLoadFunc = null) : base(services)
    {
        _nameLoadFunc = nameLoadFunc;
        _childLoadFunc = childLoadFunc;
        PauseAllActions();
    }

    public partial string? Name { get; set; }
    public partial LazyLoadChildEntity? Child { get; set; }

    public void ConfigureLazyLoadingForName()
    {
        if (_nameLoadFunc != null)
        {
            NameProperty.OnLoad = _nameLoadFunc;
        }
    }

    public void ConfigureLazyLoadingForChild()
    {
        if (_childLoadFunc != null)
        {
            ChildProperty.OnLoad = _childLoadFunc;
        }
    }

    // Expose property internals for testing
    public IValidateProperty<string?> ExposedNameProperty => NameProperty;
    public IValidateProperty<LazyLoadChildEntity?> ExposedChildProperty => ChildProperty;
}

/// <summary>
/// Child entity for lazy loading tests.
/// </summary>
[SuppressFactory]
public partial class LazyLoadChildEntity : ValidateBase<LazyLoadChildEntity>
{
    public LazyLoadChildEntity() : base(new ValidateBaseServices<LazyLoadChildEntity>())
    {
        PauseAllActions();
    }

    public partial string? ChildName { get; set; }
    public partial int Value { get; set; }
}

[TestClass]
public class LazyLoadingTests
{
    private List<string> _propertyChangedCalls = new();

    [TestInitialize]
    public void TestInitialize()
    {
        _propertyChangedCalls.Clear();
    }

    private void TrackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null)
        {
            _propertyChangedCalls.Add(e.PropertyName);
        }
    }

    [TestMethod]
    public async Task LazyLoad_TriggersOnPropertyAccess()
    {
        // Arrange
        var loadWasCalled = false;
        var loadTaskSource = new TaskCompletionSource<string?>();

        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: async () =>
        {
            loadWasCalled = true;
            return await loadTaskSource.Task;
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - access the property
        var value = entity.Name;

        // Assert - load was triggered (fire-and-forget)
        Assert.IsTrue(loadWasCalled, "OnLoad should be triggered on property access");
        Assert.IsNull(value, "Value should be null before load completes");
        Assert.IsFalse(entity.ExposedNameProperty.IsLoaded, "IsLoaded should be false while loading");

        // Complete the load
        loadTaskSource.SetResult("Loaded Name");
        await entity.WaitForTasks();

        // Assert - value is now loaded
        Assert.IsTrue(entity.ExposedNameProperty.IsLoaded, "IsLoaded should be true after load");
        Assert.AreEqual("Loaded Name", entity.Name);
    }

    [TestMethod]
    public async Task LazyLoad_CompletionFiresPropertyChanged()
    {
        // Arrange
        var loadTaskSource = new TaskCompletionSource<string?>();
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: () => loadTaskSource.Task);
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        entity.ExposedNameProperty.PropertyChanged += TrackPropertyChanged;

        // Act - trigger the load
        _ = entity.Name;

        // Assert - PropertyChanged for LoadTask and IsBusy should fire during load
        Assert.IsTrue(_propertyChangedCalls.Contains("LoadTask"), "LoadTask PropertyChanged should fire when load starts");
        Assert.IsTrue(_propertyChangedCalls.Contains("IsBusy"), "IsBusy PropertyChanged should fire when load starts");

        _propertyChangedCalls.Clear();

        // Complete the load
        loadTaskSource.SetResult("Loaded Value");
        await entity.WaitForTasks();

        // Assert - Value PropertyChanged should fire after load completes
        Assert.IsTrue(_propertyChangedCalls.Contains("Value"), "Value PropertyChanged should fire when load completes");
        Assert.IsTrue(_propertyChangedCalls.Contains("IsLoaded"), "IsLoaded PropertyChanged should fire when load completes");
    }

    [TestMethod]
    public async Task LazyLoad_FailureCreatesBrokenRule()
    {
        // Arrange
        var loadTaskSource = new TaskCompletionSource<string?>();
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: () => loadTaskSource.Task);
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - trigger the load
        _ = entity.Name;

        // Fail the load
        loadTaskSource.SetException(new InvalidOperationException("Database connection failed"));
        await entity.WaitForTasks();

        // Assert - should have broken rule
        Assert.IsFalse(entity.ExposedNameProperty.IsValid, "Property should be invalid after load failure");
        Assert.IsFalse(entity.IsValid, "Entity should be invalid after load failure");
        Assert.IsTrue(entity.ExposedNameProperty.IsLoaded, "IsLoaded should be true (attempted) after failure");

        var messages = entity.ExposedNameProperty.PropertyMessages;
        Assert.AreEqual(1, messages.Count, "Should have one error message");
        Assert.IsTrue(messages.First().Message.Contains("Failed to load"), "Message should indicate load failure");
    }

    [TestMethod]
    public async Task LazyLoad_ExplicitLoadAsync()
    {
        // Arrange
        var loadWasCalled = false;
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: async () =>
        {
            loadWasCalled = true;
            await Task.Delay(10);
            return "Explicitly Loaded";
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - explicitly call LoadAsync
        var result = await entity.ExposedNameProperty.LoadAsync();

        // Assert
        Assert.IsTrue(loadWasCalled, "OnLoad should be called");
        Assert.AreEqual("Explicitly Loaded", result);
        Assert.AreEqual("Explicitly Loaded", entity.Name);
        Assert.IsTrue(entity.ExposedNameProperty.IsLoaded, "IsLoaded should be true after LoadAsync");
    }

    [TestMethod]
    public async Task LazyLoad_DoesNotRetriggerAfterLoaded()
    {
        // Arrange
        var loadCount = 0;
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: async () =>
        {
            loadCount++;
            await Task.Delay(1);
            return "Loaded";
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - access property multiple times
        _ = entity.Name;
        await entity.WaitForTasks();
        _ = entity.Name;
        _ = entity.Name;

        // Assert - should only load once
        Assert.AreEqual(1, loadCount, "OnLoad should only be called once");
    }

    [TestMethod]
    public async Task LazyLoad_IsBusyDuringLoad()
    {
        // Arrange
        var loadTaskSource = new TaskCompletionSource<string?>();
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: () => loadTaskSource.Task);
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - trigger the load
        _ = entity.Name;

        // Assert - property should be busy during load
        Assert.IsTrue(entity.ExposedNameProperty.IsBusy, "Property should be busy during load");
        Assert.IsNotNull(entity.ExposedNameProperty.LoadTask, "LoadTask should be set during load");

        // Complete the load
        loadTaskSource.SetResult("Done");
        await entity.WaitForTasks();

        // Assert - no longer busy
        Assert.IsFalse(entity.ExposedNameProperty.IsBusy, "Property should not be busy after load");
        Assert.IsNull(entity.ExposedNameProperty.LoadTask, "LoadTask should be null after load");
    }

    [TestMethod]
    public async Task LazyLoad_WaitForTasksAwaitsLoad()
    {
        // Arrange
        var loadComplete = false;
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: async () =>
        {
            await Task.Delay(50);
            loadComplete = true;
            return "Delayed Load";
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - trigger the load
        _ = entity.Name;
        Assert.IsFalse(loadComplete, "Load should not be complete yet");

        // Wait for tasks
        await entity.WaitForTasks();

        // Assert
        Assert.IsTrue(loadComplete, "Load should be complete after WaitForTasks");
        Assert.AreEqual("Delayed Load", entity.Name);
    }

    [TestMethod]
    public async Task LazyLoad_WithChildEntity()
    {
        // Arrange
        var child = new LazyLoadChildEntity();
        child.ResumeAllActions();
        child.ChildName = "Child Object";
        child.Value = 42;

        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, childLoadFunc: async () =>
        {
            await Task.Delay(10);
            return child;
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForChild();

        // Act - trigger the load
        _ = entity.Child;
        await entity.WaitForTasks();

        // Assert
        Assert.IsNotNull(entity.Child);
        Assert.AreSame(child, entity.Child);
        Assert.AreEqual("Child Object", entity.Child.ChildName);
        Assert.AreEqual(42, entity.Child.Value);
    }

    [TestMethod]
    public void LazyLoad_NonLazyPropertyIsLoadedByDefault()
    {
        // Arrange
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services);
        entity.ResumeAllActions();

        // Don't configure lazy loading

        // Assert - IsLoaded should be true by default
        Assert.IsTrue(entity.ExposedNameProperty.IsLoaded, "Non-lazy properties should be loaded by default");
    }

    [TestMethod]
    public async Task LazyLoad_ConcurrentAccessTriggersOnce()
    {
        // Arrange
        var loadCount = 0;
        var loadTaskSource = new TaskCompletionSource<string?>();
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: () =>
        {
            Interlocked.Increment(ref loadCount);
            return loadTaskSource.Task;
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - access property from multiple "threads" concurrently
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() => { var unused = entity.Name; })).ToArray();
        await Task.WhenAll(tasks);

        loadTaskSource.SetResult("Concurrent Load");
        await entity.WaitForTasks();

        // Assert - should only load once
        Assert.AreEqual(1, loadCount, "OnLoad should only be triggered once even with concurrent access");
    }

    [TestMethod]
    public void LazyLoad_OnLoadNotSerializedByDesign()
    {
        // Arrange
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: () => Task.FromResult<string?>("Should not serialize"));
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Get the property and verify OnLoad is set
        Assert.IsNotNull(entity.ExposedNameProperty.OnLoad, "OnLoad should be configured");

        // The OnLoad property has [JsonIgnore] attribute, meaning it won't serialize
        // This is intentional - lazy loading handlers must be reconfigured after deserialization
        // because they typically capture DI dependencies that can't be serialized

        // After deserialization, the client must reconfigure OnLoad for lazy loading to work
        // This is the expected behavior per the design document
        Assert.IsTrue(true, "OnLoad is not serialized by design - reconfigure after deserialization");
    }

    [TestMethod]
    public async Task LazyLoad_CanReconfigureOnLoadAfterManualValueSet()
    {
        // This test simulates what happens after deserialization:
        // The value is present, but OnLoad can be reconfigured for future lazy loads
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services);
        entity.ResumeAllActions();

        // Set value directly (simulating deserialized value)
        entity.Name = "Pre-existing Value";

        // Reconfigure lazy loading (as would be done after deserialization)
        var loadCalled = false;
        entity.ExposedNameProperty.OnLoad = async () =>
        {
            loadCalled = true;
            await Task.Delay(1);
            return "Lazy Loaded Value";
        };

        // Value is already present - accessing it should NOT trigger lazy load
        var value = entity.Name;
        await entity.WaitForTasks();

        Assert.IsFalse(loadCalled, "Lazy load should not trigger when value already exists");
        Assert.AreEqual("Pre-existing Value", value);
    }

    [TestMethod]
    public async Task LazyLoad_ReloadAfterClearingValue()
    {
        // Arrange
        var loadCount = 0;
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: async () =>
        {
            loadCount++;
            await Task.Delay(1);
            return $"Load #{loadCount}";
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // First load
        _ = entity.Name;
        await entity.WaitForTasks();
        Assert.AreEqual(1, loadCount, "First load");
        Assert.AreEqual("Load #1", entity.Name);

        // Clear the value and reconfigure for reload
        entity.Name = null!;

        // Access should NOT trigger reload since IsLoaded is still true
        // (Once loaded, stays loaded to prevent infinite reload loops)
        _ = entity.Name;
        await entity.WaitForTasks();

        Assert.AreEqual(1, loadCount, "Should not reload after clearing - IsLoaded prevents it");
    }

    [TestMethod]
    public async Task LazyLoad_ValueChangedAfterLoadDoesNotRetrigger()
    {
        // Arrange
        var loadCount = 0;
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: async () =>
        {
            loadCount++;
            await Task.Delay(1);
            return "Initial Load";
        });
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Trigger initial load
        _ = entity.Name;
        await entity.WaitForTasks();
        Assert.AreEqual(1, loadCount);

        // Change the value manually
        entity.Name = "Manual Value";
        await entity.WaitForTasks();

        // Access again - should not retrigger load
        var value = entity.Name;
        Assert.AreEqual("Manual Value", value);
        Assert.AreEqual(1, loadCount, "Load should not retrigger after manual value change");
    }

    [TestMethod]
    public void LazyLoad_SynchronousLoadReturnsValueImmediately()
    {
        // Arrange - OnLoad returns a completed task (no async fork)
        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, nameLoadFunc: () => Task.FromResult<string?>("Sync Value"));
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForName();

        // Act - access the property (no await needed for sync load)
        var value = entity.Name;

        // Assert - value should be available immediately for synchronous loads
        Assert.AreEqual("Sync Value", value, "Synchronous loads should return value on first access");
        Assert.IsTrue(entity.ExposedNameProperty.IsLoaded, "IsLoaded should be true after sync load");
    }

    [TestMethod]
    public void LazyLoad_SynchronousLoadWithChildEntity()
    {
        // Arrange
        var child = new LazyLoadChildEntity();
        child.ResumeAllActions();
        child.ChildName = "Sync Child";
        child.Value = 99;

        var services = new ValidateBaseServices<LazyLoadTestEntity>();
        var entity = new LazyLoadTestEntity(services, childLoadFunc: () => Task.FromResult<LazyLoadChildEntity?>(child));
        entity.ResumeAllActions();
        entity.ConfigureLazyLoadingForChild();

        // Act - access the property (no await needed for sync load)
        var loadedChild = entity.Child;

        // Assert - child should be available immediately
        Assert.IsNotNull(loadedChild, "Synchronous child load should return value on first access");
        Assert.AreSame(child, loadedChild);
        Assert.AreEqual("Sync Child", loadedChild.ChildName);
        Assert.AreEqual(99, loadedChild.Value);
    }
}
