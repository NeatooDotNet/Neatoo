using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;
using Neatoo.UnitTest.TestInfrastructure.ServiceProviders;
using System.ComponentModel;

namespace Neatoo.UnitTest.TestInfrastructure;

/// <summary>
/// Base class for integration tests that require DI services.
/// Provides automatic service scope management and common test utilities.
/// </summary>
/// <remarks>
/// This base class handles:
/// - Creating and disposing service scopes automatically
/// - Providing convenient access to the service provider
/// - Common serialization utilities via NeatooJsonSerializer
///
/// Usage:
/// <code>
/// [TestClass]
/// public class MyTests : IntegrationTestBase
/// {
///     private IMyService _service = null!;
///
///     [TestInitialize]
///     public void TestInitialize()
///     {
///         InitializeScope();
///         _service = GetRequiredService&lt;IMyService&gt;();
///     }
/// }
/// </code>
/// </remarks>
public abstract class IntegrationTestBase
{
    private IServiceScope? _scope;

    /// <summary>
    /// Gets the current service scope. Throws if scope has not been initialized.
    /// </summary>
    protected IServiceScope Scope
    {
        get
        {
            if (_scope is null)
            {
                throw new InvalidOperationException(
                    "Service scope has not been initialized. Call InitializeScope() in your [TestInitialize] method.");
            }
            return _scope;
        }
    }

    /// <summary>
    /// Gets the service provider from the current scope.
    /// </summary>
    protected IServiceProvider ServiceProvider => Scope.ServiceProvider;

    /// <summary>
    /// Indicates whether a local portal configuration should be used.
    /// Override and return true in derived classes to use local portal.
    /// </summary>
    protected virtual bool UseLocalPortal => false;

    /// <summary>
    /// Initializes the service scope. Call this in your [TestInitialize] method.
    /// </summary>
    protected void InitializeScope()
    {
        _scope = TestServiceProvider.CreateScope(UseLocalPortal);
    }

    /// <summary>
    /// Cleans up the service scope. Call this in your [TestCleanup] method or
    /// let the default cleanup handle it.
    /// </summary>
    [TestCleanup]
    public virtual void CleanupScope()
    {
        _scope?.Dispose();
        _scope = null;
    }

    /// <summary>
    /// Gets a required service from the current scope.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    protected T GetRequiredService<T>() where T : notnull
    {
        return Scope.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets an optional service from the current scope.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The resolved service instance, or null if not registered.</returns>
    protected T? GetService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// Gets the JSON serializer for Neatoo objects.
    /// </summary>
    protected NeatooJsonSerializer GetSerializer()
    {
        return GetRequiredService<NeatooJsonSerializer>();
    }

    /// <summary>
    /// Serializes an object to JSON using the Neatoo serializer.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The JSON string representation.</returns>
    protected string Serialize(object obj)
    {
        return GetSerializer().Serialize(obj);
    }

    /// <summary>
    /// Deserializes JSON to an object using the Neatoo serializer.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    protected T Deserialize<T>(string json) where T : notnull
    {
        return GetSerializer().Deserialize<T>(json);
    }
}

/// <summary>
/// Base class for integration tests that need to track property changes.
/// Extends IntegrationTestBase with PropertyChanged event tracking utilities.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [TestClass]
/// public class MyTests : PropertyChangedTestBase
/// {
///     private IMyValidateObject _target = null!;
///
///     [TestInitialize]
///     public void TestInitialize()
///     {
///         InitializeScope();
///         _target = GetRequiredService&lt;IMyValidateObject&gt;();
///         TrackPropertyChanges(_target);
///     }
/// }
/// </code>
/// </remarks>
public abstract class PropertyChangedTestBase : IntegrationTestBase
{
    private readonly List<string> _propertyChangedNames = new();
    private readonly List<INotifyPropertyChanged> _trackedObjects = new();

    /// <summary>
    /// Gets the list of property names that have been changed.
    /// </summary>
    protected IReadOnlyList<string> PropertyChangedNames => _propertyChangedNames;

    /// <summary>
    /// Starts tracking PropertyChanged events for the specified object.
    /// </summary>
    /// <param name="target">The object to track.</param>
    protected void TrackPropertyChanges(INotifyPropertyChanged target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.PropertyChanged += OnPropertyChanged;
        _trackedObjects.Add(target);
    }

    /// <summary>
    /// Stops tracking PropertyChanged events for the specified object.
    /// </summary>
    /// <param name="target">The object to stop tracking.</param>
    protected void StopTrackingPropertyChanges(INotifyPropertyChanged target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.PropertyChanged -= OnPropertyChanged;
        _trackedObjects.Remove(target);
    }

    /// <summary>
    /// Clears the list of recorded property changes.
    /// </summary>
    protected void ClearPropertyChanges()
    {
        _propertyChangedNames.Clear();
    }

    /// <summary>
    /// Checks if a specific property was changed.
    /// </summary>
    /// <param name="propertyName">The name of the property to check.</param>
    /// <returns>True if the property was changed; otherwise, false.</returns>
    protected bool WasPropertyChanged(string propertyName)
    {
        return _propertyChangedNames.Contains(propertyName);
    }

    /// <summary>
    /// Asserts that a specific property was changed.
    /// </summary>
    /// <param name="propertyName">The name of the property to verify.</param>
    /// <param name="message">Optional message to display on failure.</param>
    protected void AssertPropertyChanged(string propertyName, string? message = null)
    {
        Assert.IsTrue(
            WasPropertyChanged(propertyName),
            message ?? $"Expected property '{propertyName}' to have been changed.");
    }

    /// <summary>
    /// Asserts that a specific property was NOT changed.
    /// </summary>
    /// <param name="propertyName">The name of the property to verify.</param>
    /// <param name="message">Optional message to display on failure.</param>
    protected void AssertPropertyNotChanged(string propertyName, string? message = null)
    {
        Assert.IsFalse(
            WasPropertyChanged(propertyName),
            message ?? $"Expected property '{propertyName}' to NOT have been changed.");
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not null)
        {
            _propertyChangedNames.Add(e.PropertyName);
        }
    }

    /// <summary>
    /// Cleans up tracked objects and the service scope.
    /// </summary>
    [TestCleanup]
    public override void CleanupScope()
    {
        foreach (var obj in _trackedObjects.ToList())
        {
            obj.PropertyChanged -= OnPropertyChanged;
        }
        _trackedObjects.Clear();
        _propertyChangedNames.Clear();

        base.CleanupScope();
    }
}

/// <summary>
/// Base class for client-server portal integration tests.
/// Provides separate client and server scopes for testing remote scenarios.
/// </summary>
public abstract class ClientServerTestBase
{
    private IServiceScope? _serverScope;
    private IServiceScope? _clientScope;

    /// <summary>
    /// Gets the server service scope.
    /// </summary>
    protected IServiceScope ServerScope
    {
        get
        {
            if (_serverScope is null)
            {
                throw new InvalidOperationException(
                    "Scopes have not been initialized. Call InitializeScopes() in your [TestInitialize] method.");
            }
            return _serverScope;
        }
    }

    /// <summary>
    /// Gets the client service scope.
    /// </summary>
    protected IServiceScope ClientScope
    {
        get
        {
            if (_clientScope is null)
            {
                throw new InvalidOperationException(
                    "Scopes have not been initialized. Call InitializeScopes() in your [TestInitialize] method.");
            }
            return _clientScope;
        }
    }

    /// <summary>
    /// Initializes both client and server scopes. Call this in your [TestInitialize] method.
    /// </summary>
    protected void InitializeScopes()
    {
        var scopes = ClientServerContainers.Scopes();
        _serverScope = scopes.server;
        _clientScope = scopes.client;
    }

    /// <summary>
    /// Gets a required service from the client scope.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    protected T GetClientService<T>() where T : notnull
    {
        return ClientScope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a required service from the server scope.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    protected T GetServerService<T>() where T : notnull
    {
        return ServerScope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Cleans up both service scopes.
    /// </summary>
    [TestCleanup]
    public virtual void CleanupScopes()
    {
        _serverScope?.Dispose();
        _clientScope?.Dispose();
        _serverScope = null;
        _clientScope = null;
    }
}
