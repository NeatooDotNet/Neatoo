// -----------------------------------------------------------------------------
// Design.App - Order Edit ViewModel and the Save Handler
// -----------------------------------------------------------------------------
// The screen's state is the aggregate itself. The ViewModel holds the live
// IOrder, hands it to the page to bind, and owns the one piece of UI logic
// an edit screen has: the Save handler.
//
// THE QUESTION THE HABIT ASKS: "Where do I validate before Save?"
//
// Nowhere. Every rule ran as the user typed; the entity already knows whether
// it is valid, and the validation summary on the page is already showing the
// messages. The handler waits for any async rule still in flight, asks the
// entity whether it is savable, and saves. Nothing to check, nothing to catch.
//
// DESIGN DECISION: the handler reassigns the Save result. Save returns a new
// instance; the one the handler sent is stale the moment Save returns.
//
// DID NOT DO THIS: RunRules(RunRulesFlag.All) before Save. WHY NOT: the rules
// are not a step the UI runs; they ran when the properties changed. A forced
// re-run is a user decision for rare cases, not a save step.
//
// DID NOT DO THIS: try { Save } catch { show a banner }. WHY NOT: an invalid
// aggregate never reaches Save (IsSavable is false); what Save can throw is an
// application failure, and the page's error boundary owns those.
// -----------------------------------------------------------------------------

using Design.Domain.Aggregates.OrderAggregate;

namespace Design.App.ViewModels;

#region skill-save-handler
/// <summary>
/// ViewModel for the Order edit screen: holds the aggregate, saves it.
/// </summary>
internal sealed class OrderEditViewModel
{
    private readonly IOrderFactory _orderFactory;
    private readonly IOrderItemFactory _itemFactory;

    public OrderEditViewModel(IOrderFactory orderFactory, IOrderItemFactory itemFactory)
    {
        _orderFactory = orderFactory;
        _itemFactory = itemFactory;
    }

    /// <summary>The aggregate the page binds to. Null until loaded or created.</summary>
    public IOrder? Order { get; private set; }

    public async Task New()
    {
        Order = await _orderFactory.Create();
    }

    public async Task Load(Guid id)
    {
        Order = await _orderFactory.Fetch(id);
    }

    public void AddItem()
    {
        Order?.Items?.Add(_itemFactory.Create());
    }

    /// <summary>
    /// The Save button binds to this: enabled only when the entity says it
    /// can be saved. No separate validity check lives in the UI.
    /// </summary>
    public bool CanSave => Order?.IsSavable ?? false;

    /// <summary>
    /// The save handler. Waits for any async rule still running, then lets the
    /// entity decide; the messages for a refused save are already on the entity.
    /// </summary>
    public async Task Save()
    {
        if (Order is null)
        {
            return;
        }

        await Order.WaitForTasks();

        if (!Order.IsSavable)
        {
            return;  // invalid, or nothing changed; the summary shows why
        }

        Order = (IOrder)await Order.Save();  // reassign: Save returns a new instance
    }
}
#endregion
