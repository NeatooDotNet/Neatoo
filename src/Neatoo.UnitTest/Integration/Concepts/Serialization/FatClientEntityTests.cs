using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.EntityTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Integration tests for EntityObject serialization and deserialization.
/// Tests state preservation, child relationships, and modified property tracking.
/// </summary>
[TestClass]
public class FatClientEntityTests : IntegrationTestBase
{
    private IEntityObject _target = null!;
    private Guid _id;
    private string _name = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        _id = Guid.NewGuid();
        _name = Guid.NewGuid().ToString();
        _target = GetRequiredService<IEntityObject>();
        _target.ID = _id;
        _target.Name = _name;
    }

    private IEntityObject DeserializeEntity(string json)
    {
        return Deserialize<IEntityObject>(json);
    }

    [TestMethod]
    public void FatClientEntity_Serialize()
    {
        var result = Serialize(_target);

        Assert.IsTrue(result.Contains(_id.ToString()));
        Assert.IsTrue(result.Contains(_name));
    }

    [TestMethod]
    public void FatClientEntity_Deserialize()
    {
        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.AreEqual(_target.ID, newTarget.ID);
        Assert.AreEqual(_target.Name, newTarget.Name);
    }


    [TestMethod]
    public void FatClientEntity_Deserialize_Modify()
    {
        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        var id = Guid.NewGuid();
        newTarget.ID = id;
        Assert.AreEqual(id, newTarget.ID);

    }

    [TestMethod]
    public void FatClientEntity_Deserialize_Child()
    {
        var child = _target.Child = GetRequiredService<IEntityObject>();

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreEqual(child.ID, newTarget.Child.ID);
        Assert.AreEqual(child.Name, newTarget.Child.Name);
    }

    [TestMethod]
    public void FatClientEntity_Deserialize_Child_ParentRef()
    {
        var child = _target.Child = GetRequiredService<IEntityObject>();

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreEqual(child.ID, newTarget.Child.ID);
        Assert.AreEqual(child.Name, newTarget.Child.Name);
        Assert.AreSame(newTarget.Child.Parent, newTarget);
    }

    [TestMethod]
    public void FatClientEntity_IsModified()
    {
        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsTrue(newTarget.IsModified);
        Assert.IsTrue(newTarget.IsSelfModified);
    }

    [TestMethod]
    public void FatClientEntity_IsModified_False()
    {

        _target.MarkUnmodified();
        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsFalse(newTarget.IsModified);
        Assert.IsFalse(newTarget.IsSelfModified);

    }

    [TestMethod]
    public void FatClientEntity_IsNew()
    {

        _target.MarkNew();
        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsTrue(newTarget.IsNew);

    }

    [TestMethod]
    public void FatClientEntity_IsNew_False()
    {

        _target.MarkOld();

        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsFalse(newTarget.IsNew);

    }

    [TestMethod]
    public void FatClientEntity_IsChild()
    {

        _target.MarkAsChild();

        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsTrue(newTarget.IsChild);

    }

    [TestMethod]
    public void FatClientEntity_IsChild_False()
    {

        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsFalse(newTarget.IsChild);

    }

    [TestMethod]
    public void FatClientEntity_ModifiedProperties()
    {

        var orig = _target.ModifiedProperties.ToList();

        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        var result = newTarget.ModifiedProperties.ToList();

        CollectionAssert.AreEquivalent(orig, result);

    }

    [TestMethod]
    public void FatClientEntity_IsDeleted()
    {
        _target.Delete();

        var json = Serialize(_target);

        var newTarget = DeserializeEntity(json);

        Assert.IsTrue(_target.IsDeleted);
        Assert.IsTrue(_target.IsModified);
        Assert.IsTrue(_target.IsSelfModified);
        Assert.IsTrue(newTarget.IsDeleted);
        Assert.IsTrue(newTarget.IsModified);
        Assert.IsTrue(newTarget.IsSelfModified);
    }
}
