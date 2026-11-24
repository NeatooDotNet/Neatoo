using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.DeserializedSubscription;
using Neatoo.UnitTest.Integration.Concepts.Serialization.LazyLoadTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// One write to an object that came through the serializer is handled once at every level,
/// exactly as on an object built in this container (#140).
/// </summary>
/// <remarks>
/// Deserialization used to subscribe a second time at each level (entity to its property manager,
/// list to each item, property manager to properties that are not in the JSON), so one write to a
/// child ran the child's rule 2 times, the list's handler 4 times and a root rule on the child
/// path 8 times.
/// </remarks>
[TestClass]
public class DeserializedSubscriptionTests : IntegrationTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    private ISubscriptionRoot Created()
    {
        return GetRequiredService<ISubscriptionRootFactory>().Create();
    }

    private ISubscriptionRoot RoundTrip(ISubscriptionRoot root)
    {
        return Deserialize<ISubscriptionRoot>(Serialize(root));
    }

    private static void AssertOneChildWriteIsHandledOnceAtEachLevel(ISubscriptionRoot root)
    {
        var item = root.Items![0];

        item.X = "changed";

        Assert.AreEqual(1, item.RuleRuns, "runs of the item's rule on X");
        Assert.AreEqual(1, root.Items.HandledX, "calls to the list's HandleNeatooPropertyChanged for X");
        Assert.AreEqual(1, root.ChildPathRuleRuns, "runs of the root's rule on Items[0].X");
    }

    private static void AssertOneRootWriteIsHandledOnce(ISubscriptionRoot root)
    {
        var nameChanged = 0;
        root.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISubscriptionRoot.Name))
            {
                nameChanged++;
            }
        };

        root.Name = "changed";

        Assert.AreEqual(1, root.NameRuleRuns, "runs of the root's rule on Name");
        Assert.AreEqual(1, nameChanged, "PropertyChanged(Name) on the root");
    }

    [TestMethod]
    public void Created_OneRootWrite_RunsItsRuleOnce_AndRaisesPropertyChangedOnce()
    {
        AssertOneRootWriteIsHandledOnce(Created());
    }

    [TestMethod]
    public void Created_OneChildWrite_IsHandledOnceAtEachLevel()
    {
        AssertOneChildWriteIsHandledOnceAtEachLevel(Created());
    }

    [TestMethod]
    public void Deserialized_OneRootWrite_RunsItsRuleOnce_AndRaisesPropertyChangedOnce()
    {
        AssertOneRootWriteIsHandledOnce(RoundTrip(Created()));
    }

    [TestMethod]
    public void Deserialized_OneChildWrite_IsHandledOnceAtEachLevel()
    {
        AssertOneChildWriteIsHandledOnceAtEachLevel(RoundTrip(Created()));
    }

    [TestMethod]
    public void DeserializedTwice_OneRootWrite_RunsItsRuleOnce_AndRaisesPropertyChangedOnce()
    {
        AssertOneRootWriteIsHandledOnce(RoundTrip(RoundTrip(Created())));
    }

    [TestMethod]
    public void DeserializedTwice_OneChildWrite_IsHandledOnceAtEachLevel()
    {
        AssertOneChildWriteIsHandledOnceAtEachLevel(RoundTrip(RoundTrip(Created())));
    }

    /// <summary>
    /// A lazy-load property is not part of the serialized property list, so the property manager
    /// keeps the object the constructor registered. It must not be subscribed a second time.
    /// </summary>
    [TestMethod]
    public void Deserialized_WriteToALoadedLazyChild_ReachesTheRootOnce()
    {
        var parent = GetRequiredService<ILazyLoadEntityObject>();
        parent.ID = Guid.NewGuid();
        parent.Name = "Parent";

        var child = GetRequiredService<ILazyLoadEntityObject>();
        child.ID = Guid.NewGuid();
        child.Name = "Child";

        parent.LazyChild = new EntityLazyLoad<ILazyLoadEntityObject>(child);

        var deserialized = Deserialize<ILazyLoadEntityObject>(Serialize(parent));

        var heard = new List<string>();
        deserialized.NeatooPropertyChanged += e =>
        {
            heard.Add(e.FullPropertyName);
            return Task.CompletedTask;
        };

        deserialized.LazyChild.Value!.Name = "changed";

        var childNameEvents = heard.Count(name => name.EndsWith("." + nameof(ILazyLoadEntityObject.Name), StringComparison.Ordinal));
        Assert.AreEqual(1, childNameEvents, "the child's Name change heard at the root: " + string.Join(", ", heard));
    }
}

/// <summary>
/// The same claim for a graph fetched through the client and server containers: the client's
/// copy is built by the serializer, and one write is still handled once at every level (#140).
/// </summary>
[TestClass]
public class DeserializedSubscriptionClientServerTests : ClientServerTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScopes();
    }

    private async Task<ISubscriptionRoot> Fetched()
    {
        var root = await GetClientService<ISubscriptionRootFactory>().Fetch();

        Assert.IsTrue(RemoteCallCount > 0, "the fetch crossed to the server");
        Assert.IsNotNull(root);

        return root;
    }

    [TestMethod]
    public async Task Fetched_OneRootWrite_RunsItsRuleOnce_AndRaisesPropertyChangedOnce()
    {
        var root = await Fetched();

        var nameChanged = 0;
        root.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISubscriptionRoot.Name))
            {
                nameChanged++;
            }
        };

        root.Name = "changed";

        Assert.AreEqual(1, root.NameRuleRuns, "runs of the root's rule on Name");
        Assert.AreEqual(1, nameChanged, "PropertyChanged(Name) on the root");
    }

    [TestMethod]
    public async Task Fetched_OneChildWrite_IsHandledOnceAtEachLevel()
    {
        var root = await Fetched();
        var item = root.Items![0];

        item.X = "changed";

        Assert.AreEqual(1, item.RuleRuns, "runs of the item's rule on X");
        Assert.AreEqual(1, root.Items.HandledX, "calls to the list's HandleNeatooPropertyChanged for X");
        Assert.AreEqual(1, root.ChildPathRuleRuns, "runs of the root's rule on Items[0].X");
    }
}
