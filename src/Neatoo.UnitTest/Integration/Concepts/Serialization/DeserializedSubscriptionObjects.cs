using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization.DeserializedSubscription;

// Fixtures for DeserializedSubscriptionTests (#140): a root, a list and a child that each count
// how often they handle one write. The counters are plain members, not partial properties, so
// they are not serialized: every deserialized instance starts at zero.

public interface ISubscriptionItem : IEntityBase
{
    string? X { get; set; }

    /// <summary>Runs of this item's rule on <see cref="X"/>.</summary>
    int RuleRuns { get; }
}

public interface ISubscriptionItemList : IEntityListBase<ISubscriptionItem>
{
    /// <summary>Calls to HandleNeatooPropertyChanged for an item's X.</summary>
    int HandledX { get; }
}

public interface ISubscriptionRoot : IEntityRoot
{
    string? Name { get; set; }
    ISubscriptionItemList? Items { get; }

    /// <summary>Runs of the rule on <see cref="Name"/>.</summary>
    int NameRuleRuns { get; }

    /// <summary>Runs of the rule triggered on the child path <c>Items[0].X</c>.</summary>
    int ChildPathRuleRuns { get; }
}

[Factory]
internal partial class SubscriptionItem : EntityBase<SubscriptionItem>, ISubscriptionItem
{
    public SubscriptionItem(IEntityBaseServices<SubscriptionItem> services) : base(services)
    {
        RuleManager.AddAction(t => { t.RuleRuns++; }, t => t.X);
    }

    public partial string? X { get; set; }

    public int RuleRuns { get; private set; }

    [Fetch]
    internal void Fetch(string x)
    {
        this[nameof(X)].LoadValue(x);
    }
}

internal class SubscriptionItemList : EntityListBase<ISubscriptionItem>, ISubscriptionItemList
{
    public int HandledX { get; private set; }

    protected override Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(ISubscriptionItem.X))
        {
            this.HandledX++;
        }

        return base.HandleNeatooPropertyChanged(eventArgs);
    }
}

[Factory]
internal partial class SubscriptionRoot : EntityBase<SubscriptionRoot>, ISubscriptionRoot
{
    public SubscriptionRoot(IEntityBaseServices<SubscriptionRoot> services) : base(services)
    {
        RuleManager.AddAction(t => { t.NameRuleRuns++; }, t => t.Name);
        RuleManager.AddAction(t => { t.ChildPathRuleRuns++; }, t => t.Items![0].X);
    }

    public partial string? Name { get; set; }
    public partial ISubscriptionItemList? Items { get; set; }

    public int NameRuleRuns { get; private set; }
    public int ChildPathRuleRuns { get; private set; }

    /// <summary>A root with one item, built in this container.</summary>
    [Create]
    internal void Create([Service] ISubscriptionItemList items, [Service] ISubscriptionItemFactory itemFactory)
    {
        this.Build(items, itemFactory);
    }

    /// <summary>The same graph, built on the server and sent to the client.</summary>
    [Remote]
    [Fetch]
    internal void Fetch([Service] ISubscriptionItemList items, [Service] ISubscriptionItemFactory itemFactory)
    {
        this.Build(items, itemFactory);
    }

    private void Build(ISubscriptionItemList items, ISubscriptionItemFactory itemFactory)
    {
        this[nameof(Name)].LoadValue("root");
        items.Add(itemFactory.Fetch("item"));
        this[nameof(Items)].LoadValue(items);
    }
}
