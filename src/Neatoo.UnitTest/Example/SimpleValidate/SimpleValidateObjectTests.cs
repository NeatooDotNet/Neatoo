using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.Example.SimpleValidate;

[TestClass]
public class SimpleValidateObjectTests
{
    private IServiceScope scope;

    [TestInitialize]
    public void TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        scope.Dispose();
    }


    [TestMethod]
    public void SimpleValidateObject()
    {
        var validateObject = scope.GetRequiredService<SimpleValidateObject>();

        validateObject.FirstName = "John";
        validateObject.LastName = "Smith";
        Assert.AreEqual("John Smith", validateObject.ShortName);
        Assert.IsTrue(validateObject.IsValid);
    }

    [TestMethod]
    public void SimpleValidateObject_InValid()
    {
        var validateObject = scope.GetRequiredService<SimpleValidateObject>();

        validateObject.FirstName = string.Empty;
        validateObject.LastName = "Smith";

        Assert.IsFalse(validateObject.IsValid);
        Assert.AreEqual(" Smith", validateObject.ShortName);
        Assert.IsFalse(validateObject[nameof(validateObject.FirstName)].IsValid);
    }

    [TestMethod]
    public void SimpleValidateObject_InValid_Fixed()
    {
        var validateObject = scope.GetRequiredService<SimpleValidateObject>();

        validateObject.FirstName = string.Empty;
        Assert.IsFalse(validateObject.IsValid);

        validateObject.FirstName = "John";
        validateObject.LastName = "Smith";
        Assert.IsTrue(validateObject.IsValid);

    }

}
