using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.Integration.Concepts.BaseClass.Objects;

namespace Neatoo.UnitTest.Integration.Concepts.BaseClass;

/// <summary>
/// Integration tests for the Base class behavior.
/// Tests property getters/setters, parent-child relationships, and property access control.
/// </summary>
[TestClass]
public class BaseObjectTests
{
    private IBaseObject _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _sut = new BaseObject();
    }

    [TestMethod]
    public void Constructor_WhenCalled_CreatesValidInstance()
    {
        // Assert
        Assert.IsNotNull(_sut);
    }

    [TestMethod]
    public void PropertySetter_WhenValueSet_StoresValue()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var expectedString = Guid.NewGuid().ToString();

        // Act
        _sut.Id = expectedId;
        _sut.StringProperty = expectedString;

        // Assert
        Assert.AreEqual(expectedId, _sut.Id);
        Assert.AreEqual(expectedString, _sut.StringProperty);
    }

    [TestMethod]
    public void PropertySetter_WithInheritedType_AcceptsValue()
    {
        // Arrange
        var derivedInstance = new B();

        // Act
        _sut.TestPropertyType = derivedInstance;

        // Assert
        Assert.AreSame(derivedInstance, _sut.TestPropertyType);
    }

    [TestMethod]
    public void LoadPropertyTest_WithInheritedType_AcceptsValue()
    {
        // Arrange
        var derivedInstance = new B();

        // Act
        _sut.LoadPropertyTest(derivedInstance);

        // Assert - no exception thrown
    }

    [TestMethod]
    public void Child_WhenSet_SetsParentReference()
    {
        // Arrange
        var child = new BaseObject();

        // Act
        _sut.Child = child;

        // Assert
        Assert.AreSame(_sut, child.Parent);
    }

    [TestMethod]
    public void PrivateProperty_WhenAccessedViaIndexer_ThrowsPropertyReadOnlyException()
    {
        // Arrange & Act & Assert
        Assert.ThrowsException<PropertyReadOnlyException>(
            () => _sut[nameof(IBaseObject.PrivateProperty)].SetValue(Guid.NewGuid().ToString()));
    }
}
