using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.UnitTest.ValidateBaseTests.ValidateListBaseRule;

public partial class ParentObj : ValidateBase<ParentObj>
{
    public ParentObj() : base(new ValidateBaseServices<ParentObj>())
    {
        ChildObjList = new ChildObjList();
        this.RuleManager.AddActionAsync(p =>
        {
            return p.ChildObjList.RunUniqueRule();
        }, p => p.ChildObjList[0].UniqueValue);
    }

    public partial ChildObjList ChildObjList { get; set; }
}

public partial class ChildObj : ValidateBase<ChildObj>
{
    public ChildObj() : base(new ValidateBaseServices<ChildObj>())
    {
        RuleManager.AddRule(new ChildObjUniqueValue());
    }

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

    [TestInitialize]
    public void TestInitialize()
    {
        parentObj.ChildObjList.Add(new ChildObj() { UniqueValue = "A" });
        parentObj.ChildObjList.Add(new ChildObj() { UniqueValue = "B" });
        parentObj.ChildObjList.Add(new ChildObj() { UniqueValue = "C" });
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
}
