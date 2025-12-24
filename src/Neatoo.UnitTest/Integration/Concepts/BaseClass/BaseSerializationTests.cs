using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.BaseClass.Objects;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.UnitTest.Integration.Concepts.BaseClass;


[TestClass]
public class BaseSerializationTests
{
    IServiceScope scope;

    private IBaseObject single;
    private IBaseObject child;
    private IBaseObject third;
    private NeatooJsonSerializer serializer;

    [TestInitialize]
    public void TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();

        single = new BaseObject();

        single.Id = Guid.NewGuid();
        single.StringProperty = Guid.NewGuid().ToString();

        child = new BaseObject();

        child.Id = Guid.NewGuid();
        child.StringProperty = Guid.NewGuid().ToString();

        single.Child = child;

        third = new BaseObject();
        third.Id = Guid.NewGuid();
        third.StringProperty = Guid.NewGuid().ToString();

        third.Child = child;

        serializer = scope.GetRequiredService<NeatooJsonSerializer>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        scope.Dispose();
    }


    [TestMethod]
    public void Serialize_BaseObject_SerializesWithoutError()
    {
        List<IBase> list = new List<IBase>() { single, third, child };
        var json = serializer.Serialize(list);

        var deserialized = (List<IBase>)serializer.Deserialize(json, typeof(List<IBase>));

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(3, deserialized.Count);
    }

    [TestMethod]
    public void Deserialize_WithSharedReferences_PreservesObjectIdentity()
    {
        List<IBase> list = new List<IBase>() { single, third, child };
        var json = serializer.Serialize(list);

        var deserialized = (List<IBase>)serializer.Deserialize(json, typeof(List<IBase>));

        var result = deserialized.Cast<IBaseObject>().ToList();

        Assert.AreSame(result[0].Child, result[1].Child);
        Assert.AreSame(result[2], result[0].Child);
    }

    [TestMethod]
    public void Deserialize_PrivateProperty_RemainsReadOnly()
    {
        var json = serializer.Serialize(single);

        var deserialized = (IBaseObject)serializer.Deserialize(json, typeof(IBaseObject));

        Assert.ThrowsException<PropertyReadOnlyException>(() => deserialized[nameof(IBaseObject.PrivateProperty)].SetValue(Guid.NewGuid().ToString()));
    }

    public class WhatHappens
    {
        public WhatHappens()
        {
            Property = "Hello";
        }
        public string Property { get; set; }
    }


    [TestMethod]
    public void Deserialize_PocoObject_RestoresPropertyValue()
    {
        var obj = new WhatHappens();
        obj.Property = "NotHello";
        var json = serializer.Serialize(obj);
        var deserialized = (WhatHappens)serializer.Deserialize(json, typeof(WhatHappens));
        Assert.AreEqual(obj.Property, deserialized.Property);
    }
}
