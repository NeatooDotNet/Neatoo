using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Core;
using Neatoo.UnitTest.BaseTests.Objects;

namespace Neatoo.UnitTest.BaseTests;

[TestClass]
public class BaseTests
{
    private IBaseObject single = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        single = new BaseObject();
    }

    [TestMethod]
    public void Base_Construct()
    {

    }

    [TestMethod]
    public void Base_Set()
    {
        single.Id = Guid.NewGuid();
        single.StringProperty = Guid.NewGuid().ToString();
    }

    [TestMethod]
    public void Base_SetGet()
    {
        var id = single.Id = Guid.NewGuid();
        var stringProperty = single.StringProperty = Guid.NewGuid().ToString();

        Assert.AreEqual(id, single.Id);
        Assert.AreEqual(stringProperty, single.StringProperty);
    }

    [TestMethod]
    public void Base_Set_Inherited_Type_Setter()
    {
        var B = new B();
        single.TestPropertyType = B;
    }

    [TestMethod]
    public void Base_Set_Inherited_Type_LoadProperty()
    {
        var B = new B();
        single.LoadPropertyTest(B);
    }

    [TestMethod]
    public void Base_Set_Parent()
    {
        var child = new BaseObject();
        single.Child = child;
        Assert.AreSame(single, child.Parent);
    }

    [TestMethod]
    public void Base_Set_Child()
    {
        var child = new BaseObject();
        single.Child = child;
        Assert.AreSame(single, child.Parent);
    }

    [TestMethod]
    public void Base_Private_CannotBypass()
    {
        Assert.ThrowsException<PropertyReadOnlyException>(() => single[nameof(IBaseObject.PrivateProperty)].SetValue(Guid.NewGuid().ToString()));
    }
}