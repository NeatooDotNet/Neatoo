using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveLifecycle;

/// <summary>
/// In-memory persistence store for the save-lifecycle aggregate.
/// </summary>
/// <remarks>
/// Static because the client and server test containers are separate providers:
/// a shared static store is what makes them see one "database", the way a real
/// two-tier deployment does. Tests call <see cref="Reset"/> in TestInitialize.
/// </remarks>
public static class SaveLifecycleStore
{
    private static int _nextInvoiceId;
    private static int _nextLineId;

    public static Dictionary<int, InvoiceRow> Invoices { get; } = new();
    public static Dictionary<int, LineRow> Lines { get; } = new();

    // Recorded operations - tests assert which persistence calls actually fired
    public static List<int> InsertedInvoiceIds { get; } = new();
    public static List<int> UpdatedInvoiceIds { get; } = new();
    public static List<int> InsertedLineIds { get; } = new();
    public static List<int> UpdatedLineIds { get; } = new();
    public static List<int> DeletedLineIds { get; } = new();

    public static void Reset()
    {
        _nextInvoiceId = 1;
        _nextLineId = 100;
        Invoices.Clear();
        Lines.Clear();
        InsertedInvoiceIds.Clear();
        UpdatedInvoiceIds.Clear();
        InsertedLineIds.Clear();
        UpdatedLineIds.Clear();
        DeletedLineIds.Clear();
    }

    /// <summary>
    /// Seeds an invoice with lines, simulating rows that already exist in persistence.
    /// Returns the seeded invoice id. Does not record insert operations.
    /// </summary>
    public static int SeedInvoice(string customer, params (string Description, decimal Amount)[] lines)
    {
        var invoiceId = _nextInvoiceId++;
        Invoices[invoiceId] = new InvoiceRow(invoiceId, customer);

        foreach (var (description, amount) in lines)
        {
            var lineId = _nextLineId++;
            Lines[lineId] = new LineRow(lineId, invoiceId, description, amount);
        }

        return invoiceId;
    }

    public static int InsertInvoice(string customer)
    {
        var id = _nextInvoiceId++;
        Invoices[id] = new InvoiceRow(id, customer);
        InsertedInvoiceIds.Add(id);
        return id;
    }

    public static void UpdateInvoice(int id, string customer)
    {
        Invoices[id] = new InvoiceRow(id, customer);
        UpdatedInvoiceIds.Add(id);
    }

    public static int InsertLine(int invoiceId, string description, decimal amount)
    {
        var id = _nextLineId++;
        Lines[id] = new LineRow(id, invoiceId, description, amount);
        InsertedLineIds.Add(id);
        return id;
    }

    public static void UpdateLine(int id, string description, decimal amount)
    {
        var existing = Lines[id];
        Lines[id] = new LineRow(id, existing.InvoiceId, description, amount);
        UpdatedLineIds.Add(id);
    }

    public static void DeleteLine(int id)
    {
        Lines.Remove(id);
        DeletedLineIds.Add(id);
    }

    public static InvoiceRow GetInvoice(int id) => Invoices[id];

    public static IEnumerable<LineRow> GetLines(int invoiceId)
        => Lines.Values.Where(l => l.InvoiceId == invoiceId).OrderBy(l => l.Id).ToList();
}

public record InvoiceRow(int Id, string Customer);

public record LineRow(int Id, int InvoiceId, string Description, decimal Amount);
