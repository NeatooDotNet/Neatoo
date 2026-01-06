using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Samples.DomainModel.TestInfrastructure;

namespace Neatoo.Samples.DomainModel.Tests;

/// <summary>
/// Base class for documentation sample tests.
/// Provides DI scope management and common test utilities.
/// </summary>
[TestCategory("Documentation")]
public abstract class SamplesTestBase
{
    private IServiceScope? _scope;

    /// <summary>
    /// Gets the current service scope.
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
    /// Initializes the service scope. Call this in [TestInitialize].
    /// </summary>
    protected void InitializeScope()
    {
        _scope = SampleServiceProvider.CreateScope();
    }

    /// <summary>
    /// Gets a required service from the current scope.
    /// </summary>
    protected T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets an optional service from the current scope.
    /// </summary>
    protected T? GetService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// Cleans up the service scope.
    /// </summary>
    [TestCleanup]
    public virtual void CleanupScope()
    {
        _scope?.Dispose();
        _scope = null;
    }
}
