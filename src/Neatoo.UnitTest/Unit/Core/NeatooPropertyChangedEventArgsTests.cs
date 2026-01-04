using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for the NeatooPropertyChangedEventArgs class.
/// Tests construction, property chaining, and property path building.
/// </summary>
[TestClass]
public class NeatooPropertyChangedEventArgsTests
{
    #region Test POCO Classes

    /// <summary>
    /// Simple test POCO class with properties for creating real Property instances.
    /// </summary>
    private class TestPoco
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Test POCO representing a parent entity with nested child.
    /// </summary>
    private class ParentPoco
    {
        public string? ParentProperty { get; set; }
        public TestPoco? Child { get; set; }
    }

    /// <summary>
    /// Test POCO representing a grandparent entity with nested parent.
    /// </summary>
    private class GrandparentPoco
    {
        public string? GrandchildProperty { get; set; }
        public ParentPoco? Parent { get; set; }
    }

    /// <summary>
    /// Test POCO with various level properties for deep nesting tests.
    /// </summary>
    private class LevelPoco
    {
        public string? Level1 { get; set; }
        public string? Level2 { get; set; }
        public string? Level3 { get; set; }
        public string? Level4 { get; set; }
        public string? Root { get; set; }
        public string? SingleProperty { get; set; }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a real ValidateProperty instance for the specified property name on a POCO type.
    /// </summary>
    private static ValidateProperty<T> CreateProperty<T>(string propertyName, Type pocoType)
    {
        var propertyInfo = pocoType.GetProperty(propertyName)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{pocoType.Name}'");
        var wrapper = new PropertyInfoWrapper(propertyInfo);
        return new ValidateProperty<T>(wrapper);
    }

    /// <summary>
    /// Creates a real ValidateProperty instance for TestPoco with the given property name.
    /// </summary>
    private static ValidateProperty<string> CreateStringProperty(string propertyName)
    {
        return CreateProperty<string>(propertyName, typeof(TestPoco));
    }

    #endregion

    #region Constructor with propertyName and source Tests

    [TestMethod]
    public void Constructor_WithPropertyNameAndSource_SetsPropertyName()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("TestProperty", source);

        // Assert
        Assert.AreEqual("TestProperty", args.PropertyName);
    }

    [TestMethod]
    public void Constructor_WithPropertyNameAndSource_SetsSource()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("TestProperty", source);

        // Assert
        Assert.AreSame(source, args.Source);
    }

    [TestMethod]
    public void Constructor_WithPropertyNameAndSource_SetsOriginalEventArgsToSelf()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("TestProperty", source);

        // Assert
        Assert.AreSame(args, args.OriginalEventArgs);
    }

    [TestMethod]
    public void Constructor_WithPropertyNameAndSource_PropertyIsNull()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("TestProperty", source);

        // Assert
        Assert.IsNull(args.Property);
    }

    [TestMethod]
    public void Constructor_WithPropertyNameAndSource_InnerEventArgsIsNull()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("TestProperty", source);

        // Assert
        Assert.IsNull(args.InnerEventArgs);
    }

    [TestMethod]
    public void Constructor_WithPropertyNameAndSource_FullPropertyNameEqualsPropertyName()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("TestProperty", source);

        // Assert
        Assert.AreEqual("TestProperty", args.FullPropertyName);
    }

    [TestMethod]
    public void Constructor_WithEmptyPropertyName_SetsEmptyPropertyName()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("", source);

        // Assert
        Assert.AreEqual("", args.PropertyName);
    }

    [TestMethod]
    public void Constructor_WithNullSource_SetsNullSource()
    {
        // Arrange & Act
        var args = new NeatooPropertyChangedEventArgs("TestProperty", null!);

        // Assert
        Assert.IsNull(args.Source);
    }

    #endregion

    #region Constructor with IProperty Tests

    [TestMethod]
    public void Constructor_WithIProperty_SetsPropertyNameFromProperty()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.AreEqual("Name", args.PropertyName);
    }

    [TestMethod]
    public void Constructor_WithIProperty_SetsProperty()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.AreSame(property, args.Property);
    }

    [TestMethod]
    public void Constructor_WithIProperty_SetsSourceToProperty()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.AreSame(property, args.Source);
    }

    [TestMethod]
    public void Constructor_WithIProperty_SetsOriginalEventArgsToSelf()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.AreSame(args, args.OriginalEventArgs);
    }

    [TestMethod]
    public void Constructor_WithIProperty_InnerEventArgsIsNull()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.IsNull(args.InnerEventArgs);
    }

    [TestMethod]
    public void Constructor_WithNullIProperty_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(
            () => new NeatooPropertyChangedEventArgs((IValidateProperty)null!));

        Assert.AreEqual("property", exception.ParamName);
    }

    #endregion

    #region Constructor with IProperty, source, and previous Tests

    [TestMethod]
    public void Constructor_WithPropertySourceAndPrevious_SetsPropertyName()
    {
        // Arrange
        var property = CreateProperty<string>("ParentProperty", typeof(ParentPoco));
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs(property, source, null);

        // Assert
        Assert.AreEqual("ParentProperty", args.PropertyName);
    }

    [TestMethod]
    public void Constructor_WithPropertySourceAndPrevious_SetsProperty()
    {
        // Arrange
        var property = CreateStringProperty("Name");
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs(property, source, null);

        // Assert
        Assert.AreSame(property, args.Property);
    }

    [TestMethod]
    public void Constructor_WithPropertySourceAndPrevious_SetsSource()
    {
        // Arrange
        var property = CreateStringProperty("Name");
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs(property, source, null);

        // Assert
        Assert.AreSame(source, args.Source);
    }

    [TestMethod]
    public void Constructor_WithPropertySourceAndNullPrevious_SetsOriginalEventArgsToSelf()
    {
        // Arrange
        var property = CreateStringProperty("Name");
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs(property, source, null);

        // Assert
        Assert.AreSame(args, args.OriginalEventArgs);
    }

    [TestMethod]
    public void Constructor_WithPropertySourceAndNullPrevious_InnerEventArgsIsNull()
    {
        // Arrange
        var property = CreateStringProperty("Name");
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs(property, source, null);

        // Assert
        Assert.IsNull(args.InnerEventArgs);
    }

    [TestMethod]
    public void Constructor_WithNullPropertyInChainedConstructor_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new object();

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(
            () => new NeatooPropertyChangedEventArgs(null!, source, null));

        Assert.AreEqual("property", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithNullSourceInChainedConstructor_ThrowsArgumentNullException()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(
            () => new NeatooPropertyChangedEventArgs(property, null!, null));

        Assert.AreEqual("source", exception.ParamName);
    }

    #endregion

    #region Chaining and OriginalEventArgs Tests

    [TestMethod]
    public void Constructor_WithPreviousEventArgs_SetsInnerEventArgs()
    {
        // Arrange
        var childProperty = CreateStringProperty("Description");
        var childArgs = new NeatooPropertyChangedEventArgs(childProperty);

        var parentProperty = CreateProperty<string>("ParentProperty", typeof(ParentPoco));
        var parentSource = new object();

        // Act
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, parentSource, childArgs);

        // Assert
        Assert.AreSame(childArgs, parentArgs.InnerEventArgs);
    }

    [TestMethod]
    public void Constructor_WithPreviousEventArgs_SetsOriginalEventArgsToPreviousOriginal()
    {
        // Arrange
        var childProperty = CreateStringProperty("Description");
        var childArgs = new NeatooPropertyChangedEventArgs(childProperty);

        var parentProperty = CreateProperty<string>("ParentProperty", typeof(ParentPoco));
        var parentSource = new object();

        // Act
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, parentSource, childArgs);

        // Assert
        Assert.AreSame(childArgs, parentArgs.OriginalEventArgs);
    }

    [TestMethod]
    public void Constructor_WithMultipleLevelChaining_OriginalEventArgsPointsToRoot()
    {
        // Arrange
        var grandchildProperty = CreateProperty<string>("GrandchildProperty", typeof(GrandparentPoco));
        var grandchildArgs = new NeatooPropertyChangedEventArgs(grandchildProperty);

        var childProperty = CreateStringProperty("Description");
        var childSource = new object();
        var childArgs = new NeatooPropertyChangedEventArgs(childProperty, childSource, grandchildArgs);

        var parentProperty = CreateProperty<string>("ParentProperty", typeof(ParentPoco));
        var parentSource = new object();

        // Act
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, parentSource, childArgs);

        // Assert
        Assert.AreSame(grandchildArgs, parentArgs.OriginalEventArgs);
    }

    [TestMethod]
    public void Constructor_ThreeLevelChaining_InnerEventArgsChainCorrectly()
    {
        // Arrange - Level 1 (deepest/original)
        var level1Property = CreateProperty<string>("Level1", typeof(LevelPoco));
        var level1Args = new NeatooPropertyChangedEventArgs(level1Property);

        // Arrange - Level 2
        var level2Property = CreateProperty<string>("Level2", typeof(LevelPoco));
        var level2Source = new object();
        var level2Args = new NeatooPropertyChangedEventArgs(level2Property, level2Source, level1Args);

        // Arrange - Level 3 (outermost)
        var level3Property = CreateProperty<string>("Level3", typeof(LevelPoco));
        var level3Source = new object();

        // Act
        var level3Args = new NeatooPropertyChangedEventArgs(level3Property, level3Source, level2Args);

        // Assert
        Assert.AreSame(level2Args, level3Args.InnerEventArgs);
        Assert.AreSame(level1Args, level3Args.InnerEventArgs!.InnerEventArgs);
        Assert.IsNull(level3Args.InnerEventArgs!.InnerEventArgs!.InnerEventArgs);
    }

    #endregion

    #region FullPropertyName Tests

    [TestMethod]
    public void FullPropertyName_SingleLevel_ReturnsPropertyName()
    {
        // Arrange
        var property = CreateProperty<string>("SingleProperty", typeof(LevelPoco));

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.AreEqual("SingleProperty", args.FullPropertyName);
    }

    [TestMethod]
    public void FullPropertyName_TwoLevels_ReturnsDotNotationPath()
    {
        // Arrange
        var childProperty = CreateProperty<TestPoco>("Child", typeof(ParentPoco));
        var childArgs = new NeatooPropertyChangedEventArgs(childProperty);

        var parentProperty = CreateProperty<ParentPoco>("Parent", typeof(GrandparentPoco));
        var parentSource = new object();

        // Act
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, parentSource, childArgs);

        // Assert
        Assert.AreEqual("Parent.Child", parentArgs.FullPropertyName);
    }

    [TestMethod]
    public void FullPropertyName_ThreeLevels_ReturnsDotNotationPath()
    {
        // Arrange - Deepest level
        var grandchildProperty = CreateProperty<string>("GrandchildProperty", typeof(GrandparentPoco));
        var grandchildArgs = new NeatooPropertyChangedEventArgs(grandchildProperty);

        // Middle level
        var childProperty = CreateProperty<TestPoco>("Child", typeof(ParentPoco));
        var childSource = new object();
        var childArgs = new NeatooPropertyChangedEventArgs(childProperty, childSource, grandchildArgs);

        // Outermost level
        var parentProperty = CreateProperty<ParentPoco>("Parent", typeof(GrandparentPoco));
        var parentSource = new object();

        // Act
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, parentSource, childArgs);

        // Assert
        Assert.AreEqual("Parent.Child.GrandchildProperty", parentArgs.FullPropertyName);
    }

    [TestMethod]
    public void FullPropertyName_FourLevels_ReturnsDotNotationPath()
    {
        // Arrange - Level 1 (deepest)
        var level1Property = CreateProperty<string>("Level1", typeof(LevelPoco));
        var level1Args = new NeatooPropertyChangedEventArgs(level1Property);

        // Level 2
        var level2Property = CreateProperty<string>("Level2", typeof(LevelPoco));
        var level2Args = new NeatooPropertyChangedEventArgs(level2Property, new object(), level1Args);

        // Level 3
        var level3Property = CreateProperty<string>("Level3", typeof(LevelPoco));
        var level3Args = new NeatooPropertyChangedEventArgs(level3Property, new object(), level2Args);

        // Level 4 (outermost)
        var level4Property = CreateProperty<string>("Level4", typeof(LevelPoco));

        // Act
        var level4Args = new NeatooPropertyChangedEventArgs(level4Property, new object(), level3Args);

        // Assert
        Assert.AreEqual("Level4.Level3.Level2.Level1", level4Args.FullPropertyName);
    }

    [TestMethod]
    public void FullPropertyName_WithPropertyNameAndSourceConstructor_ReturnsPropertyName()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("MyProperty", source);

        // Assert
        Assert.AreEqual("MyProperty", args.FullPropertyName);
    }

    [TestMethod]
    public void FullPropertyName_InnerArgsAlsoHaveCorrectFullPropertyName()
    {
        // Arrange
        var childProperty = CreateProperty<TestPoco>("Child", typeof(ParentPoco));
        var childArgs = new NeatooPropertyChangedEventArgs(childProperty);

        var parentProperty = CreateProperty<ParentPoco>("Parent", typeof(GrandparentPoco));
        var parentSource = new object();
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, parentSource, childArgs);

        // Act & Assert
        Assert.AreEqual("Child", parentArgs.InnerEventArgs!.FullPropertyName);
        Assert.AreEqual("Parent.Child", parentArgs.FullPropertyName);
    }

    #endregion

    #region PropertyName Property Tests

    [TestMethod]
    public void PropertyName_IsReadOnly()
    {
        // Arrange
        var args = new NeatooPropertyChangedEventArgs("TestProperty", new object());

        // Act - PropertyName has no setter, so we just verify it returns the expected value
        var propertyName = args.PropertyName;

        // Assert
        Assert.AreEqual("TestProperty", propertyName);
    }

    [TestMethod]
    public void PropertyName_WithSpecialCharacters_PreservesValue()
    {
        // Arrange
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs("Property_With_Special-Chars123", source);

        // Assert
        Assert.AreEqual("Property_With_Special-Chars123", args.PropertyName);
    }

    #endregion

    #region Source Property Tests

    [TestMethod]
    public void Source_DifferentObjectTypes_ReturnsSameReference()
    {
        // Arrange
        var stringSource = "StringSource";
        var objectSource = new object();
        var listSource = new List<int> { 1, 2, 3 };

        // Act
        var args1 = new NeatooPropertyChangedEventArgs("Prop1", stringSource);
        var args2 = new NeatooPropertyChangedEventArgs("Prop2", objectSource);
        var args3 = new NeatooPropertyChangedEventArgs("Prop3", listSource);

        // Assert
        Assert.AreSame(stringSource, args1.Source);
        Assert.AreSame(objectSource, args2.Source);
        Assert.AreSame(listSource, args3.Source);
    }

    [TestMethod]
    public void Source_WithIPropertyConstructor_SourceIsTheProperty()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.AreSame(property, args.Source);
    }

    [TestMethod]
    public void Source_InChainedArgs_EachLevelHasOwnSource()
    {
        // Arrange
        var childProperty = CreateProperty<TestPoco>("Child", typeof(ParentPoco));
        var childArgs = new NeatooPropertyChangedEventArgs(childProperty);

        var parentProperty = CreateProperty<ParentPoco>("Parent", typeof(GrandparentPoco));
        var parentSource = new object();
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, parentSource, childArgs);

        // Assert
        Assert.AreSame(childProperty, childArgs.Source);
        Assert.AreSame(parentSource, parentArgs.Source);
        Assert.AreNotSame(childArgs.Source, parentArgs.Source);
    }

    #endregion

    #region Multiple Levels of Chaining Tests

    [TestMethod]
    public void MultipleLevelChaining_OriginalEventArgsAlwaysPointsToRoot()
    {
        // Arrange - Create a 5-level deep chain
        var rootProperty = CreateProperty<string>("Root", typeof(LevelPoco));
        var rootArgs = new NeatooPropertyChangedEventArgs(rootProperty);

        NeatooPropertyChangedEventArgs currentArgs = rootArgs;
        var levelProperties = new[] { "Level1", "Level2", "Level3", "Level4" };
        foreach (var levelName in levelProperties)
        {
            var property = CreateProperty<string>(levelName, typeof(LevelPoco));
            currentArgs = new NeatooPropertyChangedEventArgs(property, new object(), currentArgs);
        }

        // Act & Assert
        Assert.AreSame(rootArgs, currentArgs.OriginalEventArgs);
    }

    [TestMethod]
    public void MultipleLevelChaining_CanTraverseEntireChain()
    {
        // Arrange
        var level1Property = CreateProperty<string>("Level1", typeof(LevelPoco));
        var level1Args = new NeatooPropertyChangedEventArgs(level1Property);

        var level2Property = CreateProperty<string>("Level2", typeof(LevelPoco));
        var level2Args = new NeatooPropertyChangedEventArgs(level2Property, new object(), level1Args);

        var level3Property = CreateProperty<string>("Level3", typeof(LevelPoco));
        var level3Args = new NeatooPropertyChangedEventArgs(level3Property, new object(), level2Args);

        // Act - Traverse the chain
        var propertyNames = new List<string>();
        NeatooPropertyChangedEventArgs? current = level3Args;
        while (current != null)
        {
            propertyNames.Add(current.PropertyName);
            current = current.InnerEventArgs;
        }

        // Assert
        Assert.AreEqual(3, propertyNames.Count);
        Assert.AreEqual("Level3", propertyNames[0]);
        Assert.AreEqual("Level2", propertyNames[1]);
        Assert.AreEqual("Level1", propertyNames[2]);
    }

    [TestMethod]
    public void MultipleLevelChaining_EachLevelHasCorrectProperty()
    {
        // Arrange
        var childProperty = CreateStringProperty("Name");
        var parentProperty = CreateProperty<string>("ParentProperty", typeof(ParentPoco));

        var childArgs = new NeatooPropertyChangedEventArgs(childProperty);
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, new object(), childArgs);

        // Assert
        Assert.AreSame(parentProperty, parentArgs.Property);
        Assert.AreSame(childProperty, parentArgs.InnerEventArgs!.Property);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void FullPropertyName_WithEmptyPropertyNames_HandlesCorrectly()
    {
        // Arrange - Use a custom property name via the string constructor for the child
        // since we cannot create a Property with an empty name from reflection
        var childArgs = new NeatooPropertyChangedEventArgs("", new object());

        var parentProperty = CreateProperty<ParentPoco>("Parent", typeof(GrandparentPoco));

        // Act
        var parentArgs = new NeatooPropertyChangedEventArgs(parentProperty, new object(), childArgs);

        // Assert
        Assert.AreEqual("Parent.", parentArgs.FullPropertyName);
    }

    [TestMethod]
    public void Constructor_PropertyWithLongName_HandlesCorrectly()
    {
        // Arrange - Since we cannot create a property with a 1000-char name via reflection,
        // we use the string constructor which accepts any property name
        var longPropertyName = new string('A', 1000);

        // Act
        var args = new NeatooPropertyChangedEventArgs(longPropertyName, new object());

        // Assert
        Assert.AreEqual(longPropertyName, args.PropertyName);
        Assert.AreEqual(1000, args.PropertyName.Length);
    }

    [TestMethod]
    public void FullPropertyName_DeepNesting_BuildsCorrectPath()
    {
        // Arrange - Create a 10-level deep chain using property name constructor
        // This allows us to test deep nesting without needing 10 different property definitions
        NeatooPropertyChangedEventArgs? current = null;
        for (int i = 1; i <= 10; i++)
        {
            if (current == null)
            {
                current = new NeatooPropertyChangedEventArgs($"Prop{i}", new object());
            }
            else
            {
                // For chained constructors, we need an IProperty. Create properties dynamically
                // by reusing the LevelPoco properties with custom naming via wrapper pattern
                var dummyProperty = CreateProperty<string>("Level1", typeof(LevelPoco));

                // Create event args with the dummy property but we will verify using the chain structure
                current = new NeatooPropertyChangedEventArgs(dummyProperty, new object(), current);
            }
        }

        // Act
        var fullName = current!.FullPropertyName;

        // Assert - The pattern will be "Level1.Level1.Level1..." due to reusing the same property
        // but this still validates the chaining mechanism works correctly at 10 levels
        Assert.IsTrue(fullName.Split('.').Length == 10);
    }

    [TestMethod]
    public void FullPropertyName_DeepNesting_BuildsCorrectPath_WithDistinctNames()
    {
        // Arrange - Create a deep chain with distinct property names by using string constructor for base
        // and chaining with real properties
        var prop1Args = new NeatooPropertyChangedEventArgs("Prop1", new object());

        var level1Property = CreateProperty<string>("Level1", typeof(LevelPoco));
        var prop2Args = new NeatooPropertyChangedEventArgs(level1Property, new object(), prop1Args);

        var level2Property = CreateProperty<string>("Level2", typeof(LevelPoco));
        var prop3Args = new NeatooPropertyChangedEventArgs(level2Property, new object(), prop2Args);

        var level3Property = CreateProperty<string>("Level3", typeof(LevelPoco));
        var prop4Args = new NeatooPropertyChangedEventArgs(level3Property, new object(), prop3Args);

        var level4Property = CreateProperty<string>("Level4", typeof(LevelPoco));
        var prop5Args = new NeatooPropertyChangedEventArgs(level4Property, new object(), prop4Args);

        // Act
        var fullName = prop5Args.FullPropertyName;

        // Assert
        Assert.AreEqual("Level4.Level3.Level2.Level1.Prop1", fullName);
    }

    [TestMethod]
    public void InnerEventArgs_WhenNull_FullPropertyNameDoesNotHaveDot()
    {
        // Arrange
        var property = CreateProperty<string>("SingleProperty", typeof(LevelPoco));

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert
        Assert.IsNull(args.InnerEventArgs);
        Assert.IsFalse(args.FullPropertyName.Contains('.'));
    }

    #endregion

    #region Immutability Tests

    [TestMethod]
    public void EventArgs_PropertiesAreImmutableAfterConstruction()
    {
        // Arrange
        var property = CreateStringProperty("Name");
        var source = new object();

        // Act
        var args = new NeatooPropertyChangedEventArgs(property, source, null);

        // Assert - PropertyName should be the value captured at construction
        // With real Property<T>, the Name property is also immutable (set in constructor)
        Assert.AreEqual("Name", args.PropertyName);
    }

    [TestMethod]
    public void EventArgs_PropertyReferenceIsPreserved()
    {
        // Arrange
        var property = CreateStringProperty("Name");

        // Act
        var args = new NeatooPropertyChangedEventArgs(property);

        // Assert - The same Property instance is preserved
        Assert.AreSame(property, args.Property);
        Assert.AreEqual("Name", args.Property!.Name);
    }

    #endregion
}
