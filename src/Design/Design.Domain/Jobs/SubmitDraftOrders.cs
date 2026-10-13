// -----------------------------------------------------------------------------
// Design.Domain - A Server Job Over Neatoo Entities
// -----------------------------------------------------------------------------
// A nightly job submits every draft order it is given. No person is involved,
// and the entity is still the right tool: the job reuses the same aggregate,
// the same verb and the same rules the screens use, so the two paths cannot
// drift apart.
//
// THE QUESTION THE HABIT ASKS: "Where do I validate before Save?"
//
// Nowhere. order.Submit() sets Status outside a factory operation, so the
// rule triggered by Status has already run by the time Submit returns. The
// job awaits WaitForTasks() for any async rules and then reads the entity's
// own answer, IsSavable. There is no validate step to call.
//
// DESIGN DECISION: no RunRules anywhere in this file. RunRules forces a
// re-run; the verb already ran what matters, and persisted data is assumed
// valid. The root's own [Update] still runs every rule and throws if the
// aggregate is invalid - that is the server's safety net, and this job never
// reaches it for an invalid order, because IsSavable was already false.
//
// DESIGN DECISION: three outcomes, decided by the entity's state, not by
// the job:
//   - IsSavable          -> Save() and reassign the result (Save returns a
//                           new instance; the old reference is stale).
//   - !IsValid           -> report it with the rule messages. Listing the
//                           broken rules is the answer; throwing is not.
//   - otherwise          -> !IsModified: the verb changed nothing (the order
//                           was already Submitted). Skip silently. Calling
//                           Save() here would throw NotModified.
//
// DID NOT DO THIS: catch SaveOperationException around Save() and turn it
// into a report entry. WHY NOT: an exception is an application failure, not
// a validation message. The gate cannot fire here because IsSavable was
// checked first.
//
// [Remote] is on the command because the job is server work: one call from
// wherever it is triggered, and every factory call inside it is then local.
// A static [Execute] without [Remote] is a local-only delegate that
// RemoteFactory registers on the client as well, with its body untrimmed. A
// client calling that would pay a round trip for every Fetch and every Save,
// and a server-only [Service] on it would compile and then fail on the client
// at call time. On the server itself, [Remote] changes nothing.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;
using Neatoo.RemoteFactory;

namespace Design.Domain.Jobs;

#region skill-job-over-entities
/// <summary>
/// Job: submits the given draft orders. Reuses the Order aggregate, its
/// Submit verb and its rules; no person involved.
/// </summary>
[Factory]
public static partial class SubmitDraftOrders
{
    [Remote]
    [Execute]
    private static async Task<SubmitReport> _Run(
        IReadOnlyList<Guid> orderIds,
        [Service] IOrderFactory orderFactory)
    {
        var submitted = new List<Guid>();
        var rejected = new List<RejectedOrder>();
        var unchanged = new List<Guid>();

        foreach (var id in orderIds)
        {
            var order = await orderFactory.Fetch(id)
                ?? throw new InvalidOperationException($"Order {id} not found");

            // The verb sets Status; the rule triggered by Status runs now.
            order.Submit();

            // Async rules, if any, finish here. Nothing else is run.
            await order.WaitForTasks();

            if (order.IsSavable)
            {
                // Save returns a new instance; use the result, not the old reference.
                order = (IOrder)await order.Save();
                submitted.Add(order.Id);
            }
            else if (!order.IsValid)
            {
                // The rules have spoken; report them, do not throw.
                rejected.Add(new RejectedOrder(
                    id,
                    order.PropertyMessages.Select(m => $"{m.Property.Name}: {m.Message}").ToList()));
            }
            else
            {
                // Valid but not modified: the verb changed nothing. Skip.
                unchanged.Add(id);
            }
        }

        return new SubmitReport(submitted, rejected, unchanged);
    }
}

/// <summary>
/// What the job did: data, not a success flag.
/// </summary>
public sealed record SubmitReport(
    IReadOnlyList<Guid> Submitted,
    IReadOnlyList<RejectedOrder> Rejected,
    IReadOnlyList<Guid> Unchanged);

/// <summary>
/// An order the rules rejected, with the messages a screen would have shown.
/// </summary>
public sealed record RejectedOrder(Guid OrderId, IReadOnlyList<string> Messages);
#endregion
