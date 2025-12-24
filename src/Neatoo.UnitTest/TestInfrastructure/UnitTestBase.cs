using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.TestInfrastructure;

/// <summary>
/// Base class for pure unit tests that do not require DI services.
/// Provides common assertion helpers and test utilities.
/// </summary>
/// <remarks>
/// Use this base class when:
/// - Testing isolated classes with mocked dependencies
/// - Testing pure functions or value objects
/// - You don't need the full DI container
///
/// For integration tests that need DI services, use <see cref="IntegrationTestBase"/> instead.
/// </remarks>
public abstract class UnitTestBase
{
    /// <summary>
    /// Asserts that an action throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="messageContains">Optional substring that must be in the exception message.</param>
    /// <returns>The thrown exception for further assertions.</returns>
    protected static TException AssertThrows<TException>(Action action, string? messageContains = null)
        where TException : Exception
    {
        var exception = Assert.ThrowsException<TException>(action);

        if (messageContains is not null)
        {
            Assert.IsTrue(
                exception.Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase),
                $"Expected exception message to contain '{messageContains}', but was: '{exception.Message}'");
        }

        return exception;
    }

    /// <summary>
    /// Asserts that an async action throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="action">The async action to execute.</param>
    /// <param name="messageContains">Optional substring that must be in the exception message.</param>
    /// <returns>A task that represents the assertion operation.</returns>
    protected static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action, string? messageContains = null)
        where TException : Exception
    {
        var exception = await Assert.ThrowsExceptionAsync<TException>(action);

        if (messageContains is not null)
        {
            Assert.IsTrue(
                exception.Message.Contains(messageContains, StringComparison.OrdinalIgnoreCase),
                $"Expected exception message to contain '{messageContains}', but was: '{exception.Message}'");
        }

        return exception;
    }

    /// <summary>
    /// Asserts that two collections contain the same elements, regardless of order.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="expected">The expected collection.</param>
    /// <param name="actual">The actual collection.</param>
    protected static void AssertCollectionsEquivalent<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        var expectedList = expected.ToList();
        var actualList = actual.ToList();

        CollectionAssert.AreEquivalent(expectedList, actualList);
    }

    /// <summary>
    /// Asserts that a collection contains a specific item.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="item">The item to find.</param>
    /// <param name="message">Optional message on failure.</param>
    protected static void AssertContains<T>(IEnumerable<T> collection, T item, string? message = null)
    {
        Assert.IsTrue(
            collection.Contains(item),
            message ?? $"Expected collection to contain '{item}'.");
    }

    /// <summary>
    /// Asserts that a collection does not contain a specific item.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="item">The item that should not be present.</param>
    /// <param name="message">Optional message on failure.</param>
    protected static void AssertDoesNotContain<T>(IEnumerable<T> collection, T item, string? message = null)
    {
        Assert.IsFalse(
            collection.Contains(item),
            message ?? $"Expected collection to NOT contain '{item}'.");
    }

    /// <summary>
    /// Creates a unique test string using a GUID.
    /// </summary>
    /// <returns>A unique string.</returns>
    protected static string CreateUniqueString()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Creates a unique test ID.
    /// </summary>
    /// <returns>A new GUID.</returns>
    protected static Guid CreateUniqueId()
    {
        return Guid.NewGuid();
    }
}
