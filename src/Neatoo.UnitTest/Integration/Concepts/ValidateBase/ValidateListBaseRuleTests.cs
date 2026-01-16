using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel;

namespace Neatoo.UnitTest.Integration.Concepts.ValidateBase.ValidateListBaseRule;

[Factory]
public partial class ParentObj : ValidateBase<ParentObj>
{
    public ParentObj() : base(new ValidateBaseServices<ParentObj>())
    {
        ChildObjListProperty.LoadValue(new ChildObjList());
        this.RuleManager.AddActionAsync(p =>
        {
            return p.ChildObjList.RunUniqueRule();
        }, p => p.ChildObjList[0].UniqueValue);
    }

    public partial ChildObjList ChildObjList { get; set; }
}

[Factory]
public partial class ChildObj : ValidateBase<ChildObj>
{
    public ChildObj() : base(new ValidateBaseServices<ChildObj>())
    {
        RuleManager.AddRule(new ChildObjUniqueValue());
    }

    public partial string Identifier { get; set; }
    public partial string UniqueValue { get; set; }

    public ParentObj? ParentObj => this.Parent as ParentObj;

    public Task RunUniqueRule()
    {
        return RuleManager.RunRule<ChildObjUniqueValue>();
    }
}

public class ChildObjList : ValidateListBase<ChildObj>
{
    public Task RunUniqueRule()
    {
        return Task.WhenAll(this.Select(r => r.RunUniqueRule()));
    }

    public Task Remove(int index)
    {
        this.RemoveAt(index);
        return RunRules();
    }
}

public class ChildObjUniqueValue : RuleBase<ChildObj>
{
    public ChildObjUniqueValue() : base()
    {
        AddTriggerProperties(c => c.UniqueValue);
    }

    protected override IRuleMessages Execute(ChildObj target)
    {
        if(target.ParentObj == null)
        {
            return nameof(ChildObj.UniqueValue).RuleMessages("ParentObj is null");
        }

        if (target.ParentObj.ChildObjList
            .Where(c => c != target)
            .Any(c => c.UniqueValue == target.UniqueValue))
        {
            return nameof(ChildObj.UniqueValue).RuleMessages("UniqueValue must be unique in the list");
        }

        return None;
    }
}

[TestClass]
public class ValidateListBaseRuleTests
{

    ParentObj parentObj = new ParentObj();
    List<PropertyChangedEventArgs> propertyChangedEvents = new List<PropertyChangedEventArgs>();
    List<NeatooPropertyChangedEventArgs> neatooPropertyChangedEvents = new List<NeatooPropertyChangedEventArgs>();

    [TestInitialize]
    public void TestInitialize()
    {
        parentObj.ChildObjList.Add(new ChildObj() { Identifier = "0", UniqueValue = "A" });
        parentObj.ChildObjList.Add(new ChildObj() { Identifier = "1", UniqueValue = "B" });
        parentObj.ChildObjList.Add(new ChildObj() { Identifier = "2", UniqueValue = "C" });

        parentObj.PropertyChanged += (s, e) =>
        {
            propertyChangedEvents.Add(e);
        };

        parentObj.NeatooPropertyChanged += (e) =>
        {
            neatooPropertyChangedEvents.Add(e);
            return Task.CompletedTask;
        };
    }

    [TestMethod]
    public void ValidateListBaseRuleTests_Constructor()
    {
        Assert.IsNotNull(parentObj.ChildObjList);
    }

    [TestMethod]
    public void ValidateListBaseRuleTests_UniqueValue_Invalid()
    {
        parentObj.ChildObjList[0].UniqueValue = "A";
        parentObj.ChildObjList[1].UniqueValue = "A";
        parentObj.ChildObjList[2].UniqueValue = "A";

        Assert.IsFalse(parentObj.IsValid);
    }

    [TestMethod]
    public void ValidateListBaseRuleTests_UniqueValue_Valid()
    {
        parentObj.ChildObjList[0].UniqueValue = "A";
        parentObj.ChildObjList[1].UniqueValue = "A";
        parentObj.ChildObjList[2].UniqueValue = "A";

        parentObj.ChildObjList[0].UniqueValue = "C";
        parentObj.ChildObjList[1].UniqueValue = "D";
        parentObj.ChildObjList[2].UniqueValue = "E";

        Assert.IsTrue(parentObj.IsValid);
    }

    [TestMethod]
    public void ValidateListBaseRuleTests_UniqueValue_Fixed()
    {
        parentObj.ChildObjList[0].UniqueValue = "E";
        parentObj.ChildObjList[1].UniqueValue = "E";

        parentObj.ChildObjList[0].UniqueValue = "F";

        Assert.IsTrue(parentObj.IsValid);
    }

    [TestMethod]
    public async Task ValidateListBaseRuleTests_UniqueValue_Removed_Fixed()
    {
        parentObj.ChildObjList[0].UniqueValue = "E";
        parentObj.ChildObjList[1].UniqueValue = "E";

        propertyChangedEvents.Clear();
        neatooPropertyChangedEvents.Clear();
        await parentObj.ChildObjList.Remove(1);

        Assert.IsTrue(parentObj.IsValid);

        Assert.AreEqual("IsValid", propertyChangedEvents[0].PropertyName);
    }
}
