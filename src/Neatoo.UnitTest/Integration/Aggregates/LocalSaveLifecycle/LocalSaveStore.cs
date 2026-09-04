namespace Neatoo.UnitTest.Integration.Aggregates.LocalSaveLifecycle;

/// <summary>
/// Persistence stand-in for the local-save aggregate.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>SaveLifecycleStore</c>: both are static, and sharing
/// one between the remote and local fixtures would couple two test suites through
/// mutable global state. Reset by the test class's [TestInitialize].
/// </remarks>
internal static class LocalSaveStore
{
    private static int _nextOrderId = 1;
    private static int _nextLineId = 1;

    internal static readonly List<int> DeletedLineIds = new();
    internal static readonly List<int> UpdatedLineIds = new();
    internal static readonly List<int> InsertedLineIds = new();

    internal static void Reset()
    {
        _nextOrderId = 1;
        _nextLineId = 1;
        DeletedLineIds.Clear();
        UpdatedLineIds.Clear();
        InsertedLineIds.Clear();
    }

    internal static int NextOrderId() => _nextOrderId++;

    internal static int InsertLine()
    {
        var id = _nextLineId++;
        InsertedLineIds.Add(id);
        return id;
    }

    internal static void UpdateLine(int id) => UpdatedLineIds.Add(id);

    internal static void DeleteLine(int id) => DeletedLineIds.Add(id);
}
