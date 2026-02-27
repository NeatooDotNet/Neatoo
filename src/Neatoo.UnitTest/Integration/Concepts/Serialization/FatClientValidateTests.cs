using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.Serialization.ValidateTests;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Integration tests for ValidateObject serialization and deserialization.
/// Tests rule manager preservation, validation state, and child relationships.
/// </summary>
[TestClass]
public class FatClientValidateTests : IntegrationTestBase
{
    private IValidateObject _target = null!;
    private Guid _id;
    private string _name = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
        _id = Guid.NewGuid();
        _name = Guid.NewGuid().ToString();
        _target = GetRequiredService<IValidateObject>();
        _target.ID = _id;
        _target.Name = _name;
    }

    private IValidateObject DeserializeValidate(string json)
    {
        return Deserialize<IValidateObject>(json);
    }

    [TestMethod]
    public void FatClientValidate_Serialize()
    {
        var result = Serialize(_target);

        Assert.IsTrue(result.Contains(_id.ToString()));
        Assert.IsTrue(result.Contains(_name));
    }

    [TestMethod]
    public void FatClientValidate_Deserialize()
    {
        var json = Serialize(_target);

        var newTarget = DeserializeValidate(json);

        Assert.AreEqual(_target.ID, newTarget.ID);
        Assert.AreEqual(_target.Name, newTarget.Name);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_Modify()
    {
        Assert.IsTrue(_target.IsValid);

        var json = Serialize(_target);

        var newTarget = DeserializeValidate(json);

        newTarget.Name = "Error";
        Assert.IsFalse(newTarget.IsValid);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_RuleManager()
    {
        _target.Name = "Error";
        Assert.IsFalse(_target.IsValid);

        var json = Serialize(_target);

        var newTarget = DeserializeValidate(json);

        Assert.AreEqual(2, newTarget.RuleRunCount); // Ensure that RuleManager was deserialized, not run
        Assert.AreEqual(2, newTarget.Rules.Count());
        Assert.IsFalse(newTarget.IsValid);

        Assert.IsFalse(newTarget[nameof(IValidateObject.Name)].IsValid);

    }


    [TestMethod]
    public void FatClientValidate_Deserialize_Child()
    {
        var child = _target.Child = GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(_target);

        var newTarget = DeserializeValidate(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreSame(newTarget.Child.Parent, newTarget);
        Assert.AreEqual(child.ID, newTarget.Child.ID);
        Assert.AreEqual(child.Name, newTarget.Child.Name);

    }

    [TestMethod]
    public void FatClientValidate_Deserialize_Child_RuleManager()
    {
        var child = _target.Child = GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = "Error";
        Assert.IsFalse(child.IsValid);
        var json = Serialize(_target);

        var newTarget = DeserializeValidate(json);

        Assert.IsFalse(newTarget.IsValid);
        Assert.IsTrue(newTarget.IsSelfValid);
        Assert.AreEqual(1, newTarget.RuleRunCount);

        Assert.IsFalse(newTarget.Child.IsValid);
        Assert.IsFalse(newTarget.Child.IsSelfValid);
        Assert.AreEqual(1, newTarget.Child.RuleRunCount);

    }

    [TestMethod]
    public async Task FatClientValidate_Deserialize_ValidateProperty_Child()
    {
        // Ensure ValidateProperty.Child is a reference to the Child

        var child = _target.Child = GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = "Error";

        Assert.IsFalse(child.IsValid);

        var json = Serialize(_target);
        var newTarget = DeserializeValidate(json);

        Assert.IsFalse(newTarget.IsValid);

        child = newTarget.Child;

        await child.RunRules();

        Assert.IsFalse(newTarget.IsValid);

        newTarget.Child.Name = "Fine";

        Assert.IsTrue(newTarget.IsValid);

    }

    [TestMethod]
    public void FatClientValidate_Deserialize_Child_ParentRef()
    {
        var child = _target.Child = GetRequiredService<IValidateObject>();

        child.ID = Guid.NewGuid();
        child.Name = Guid.NewGuid().ToString();

        var json = Serialize(_target);

        var newTarget = DeserializeValidate(json);

        Assert.IsNotNull(newTarget.Child);
        Assert.AreEqual(child.ID, newTarget.Child.ID);
        Assert.AreEqual(child.Name, newTarget.Child.Name);
        Assert.AreSame(newTarget.Child.Parent, newTarget);

    }

    [TestMethod]
    public void FatClientValidate_Serialize_DictionaryProperty()
    {
        _target.Data = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var json = Serialize(_target);

        Assert.IsTrue(json.Contains("key1"));
        Assert.IsTrue(json.Contains("value1"));
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_DictionaryProperty()
    {
        _target.Data = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        var json = Serialize(_target);
        var newTarget = DeserializeValidate(json);

        Assert.IsNotNull(newTarget.Data);
        Assert.AreEqual(2, newTarget.Data.Count);
        Assert.AreEqual("value1", newTarget.Data["key1"]);
        Assert.AreEqual("value2", newTarget.Data["key2"]);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_SharedDictionaryReference()
    {
        var shared = new Dictionary<string, string>
        {
            { "key1", "value1" }
        };

        _target.Data = shared;
        _target.Data2 = shared;

        var json = Serialize(_target);
        var newTarget = DeserializeValidate(json);

        Assert.IsNotNull(newTarget.Data);
        Assert.IsNotNull(newTarget.Data2);
        Assert.AreSame(newTarget.Data, newTarget.Data2);
        Assert.AreEqual("value1", newTarget.Data["key1"]);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_NullDictionaryProperty()
    {
        // Data is null by default
        var json = Serialize(_target);
        var newTarget = DeserializeValidate(json);

        Assert.IsNull(newTarget.Data);
    }

    [TestMethod]
    public void FatClientValidate_Deserialize_MarkInvalid()
    {
        // This caught a really critical issue that lead to the RuleManager.TransferredResults logic
        // After being transferred the RuleId values would not match up
        // So the object would be stuck in InValid

        _target.MarkInvalid(Guid.NewGuid().ToString());

        var json = Serialize(_target);
        var newTarget = DeserializeValidate(json);

        Assert.IsFalse(_target.IsValid);
        Assert.IsFalse(newTarget.IsValid);
        Assert.IsNotNull(newTarget.ObjectInvalid);
    }
}
