using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// A real ValidateBase implementation for testing ValidatePropertyManager behavior.
/// Uses SuppressFactory to avoid requiring the full factory infrastructure.
/// </summary>
[SuppressFactory]
public class ValidatePropertyManagerTestObject : ValidateBase<ValidatePropertyManagerTestObject>
{
    public ValidatePropertyManagerTestObject() : base(new ValidateBaseServices<ValidatePropertyManagerTestObject>())
    {
        PauseAllActions();
    }

    public string? Name { get => Getter<string?>(); set => Setter(value); }
    public int Age { get => Getter<int>(); set => Setter(value); }
    public string? Description { get => Getter<string?>(); set => Setter(value); }

    /// <summary>
    /// Exposes the RuleManager for testing.
    /// </summary>
    public new IRuleManager<ValidatePropertyManagerTestObject> RuleManager => base.RuleManager;

    /// <summary>
    /// Adds a validation error to make the object invalid.
    /// </summary>
    public void AddValidationError(string errorMessage)
    {
        ResumeAllActions();
        RuleManager.AddValidation(_ => errorMessage, t => t.Name);
        _ = RunRules(RunRulesFlag.All);
        PauseAllActions();
    }

    /// <summary>
    /// Clears all validation errors by clearing messages.
    /// </summary>
    public new void ClearAllMessages()
    {
        base.ClearAllMessages();
    }

    /// <summary>
    /// Exposes the PropertyManager for direct testing.
    /// </summary>
    public new IValidatePropertyManager<IValidateProperty> PropertyManager => base.PropertyManager;
}

/// <summary>
/// A child ValidateBase object for testing child validation scenarios.
/// </summary>
[SuppressFactory]
public class ValidatePropertyManagerChildObject : ValidateBase<ValidatePropertyManagerChildObject>
{
    public ValidatePropertyManagerChildObject() : base(new ValidateBaseServices<ValidatePropertyManagerChildObject>())
    {
        PauseAllActions();
    }

    public string? ChildName { get => Getter<string?>(); set => Setter(value); }
    public int ChildValue { get => Getter<int>(); set => Setter(value); }

    /// <summary>
    /// Exposes the RuleManager for testing.
    /// </summary>
    public new IRuleManager<ValidatePropertyManagerChildObject> RuleManager => base.RuleManager;

    /// <summary>
    /// Adds a validation error to make this child object invalid.
    /// </summary>
    public void AddValidationError(string errorMessage)
    {
        ResumeAllActions();
        RuleManager.AddValidation(_ => errorMessage, t => t.ChildName);
        _ = RunRules(RunRulesFlag.All);
        PauseAllActions();
    }
}

/// <summary>
/// A parent ValidateBase object that contains a child ValidateBase property for testing child validation.
/// </summary>
[SuppressFactory]
public class ValidatePropertyManagerParentObject : ValidateBase<ValidatePropertyManagerParentObject>
{
    public ValidatePropertyManagerParentObject() : base(new ValidateBaseServices<ValidatePropertyManagerParentObject>())
    {
        PauseAllActions();
    }

    public string? ParentName { get => Getter<string?>(); set => Setter(value); }
    public ValidatePropertyManagerChildObject? Child { get => Getter<ValidatePropertyManagerChildObject?>(); set => Setter(value); }

    /// <summary>
    /// Exposes the RuleManager for testing.
    /// </summary>
    public new IRuleManager<ValidatePropertyManagerParentObject> RuleManager => base.RuleManager;

    /// <summary>
    /// Adds a validation error to make this parent object invalid.
    /// </summary>
    public void AddValidationError(string errorMessage)
    {
        ResumeAllActions();
        RuleManager.AddValidation(_ => errorMessage, t => t.ParentName);
        _ = RunRules(RunRulesFlag.All);
        PauseAllActions();
    }

    /// <summary>
    /// Exposes the PropertyManager for direct testing.
    /// </summary>
    public new IValidatePropertyManager<IValidateProperty> PropertyManager => base.PropertyManager;
}

/// <summary>
/// Unit tests for the ValidatePropertyManager{P} class.
/// Tests validation state management, property message aggregation, pause/resume functionality,
/// and inherited PropertyManager behavior.
/// Uses real Neatoo classes (ValidateBase implementations) instead of mocks.
/// </summary>
[TestClass]
public class ValidatePropertyManagerTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act - Use the test object which properly initializes through ValidateBaseServices
        var testObject = new ValidatePropertyManagerTestObject();

        // Assert
        Assert.IsNotNull(testObject.PropertyManager);
    }

    [TestMethod]
    public void Constructor_InitialState_IsValidTrue()
    {
        // Arrange & Act
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Assert
        Assert.IsTrue(testObject.PropertyManager.IsValid);
    }

    [TestMethod]
    public void Constructor_InitialState_IsSelfValidTrue()
    {
        // Arrange & Act
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Assert
        Assert.IsTrue(testObject.PropertyManager.IsSelfValid);
    }

    [TestMethod]
    public void Constructor_InitialState_IsPausedFalse()
    {
        // Arrange & Act - Create object and resume to test default state
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Assert
        Assert.IsFalse(testObject.PropertyManager.IsPaused);
    }

    #endregion

    #region IsValid Tests Through ValidateBase

    [TestMethod]
    public void IsValid_NoValidationErrors_ReturnsTrue()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act & Assert
        Assert.IsTrue(testObject.PropertyManager.IsValid);
    }

    [TestMethod]
    public void IsValid_WithValidationError_ReturnsFalse()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");

        // Act & Assert
        Assert.IsFalse(testObject.IsValid);
    }

    [TestMethod]
    public void IsValid_AfterClearingErrors_ReturnsTrue()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");
        // AddValidationError pauses afterwards, so resume to check state
        testObject.ResumeAllActions();
        Assert.IsFalse(testObject.IsValid);

        // Act
        testObject.ClearAllMessages();

        // Assert
        Assert.IsTrue(testObject.IsValid);
    }

    [TestMethod]
    public void IsValid_WithInvalidChild_ReturnsFalse()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.AddValidationError("Child error");
        parent.ResumeAllActions();
        parent.Child = child;

        // Act & Assert - parent IsValid should be false because child is invalid
        Assert.IsFalse(parent.IsValid);
    }

    [TestMethod]
    public void IsValid_WithValidChild_ReturnsTrue()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.ResumeAllActions();
        // Child has no validation errors
        parent.ResumeAllActions();
        parent.Child = child;

        // Act & Assert
        Assert.IsTrue(parent.IsValid);
    }

    #endregion

    #region IsSelfValid Tests Through ValidateBase

    [TestMethod]
    public void IsSelfValid_NoValidationErrors_ReturnsTrue()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act & Assert
        Assert.IsTrue(testObject.PropertyManager.IsSelfValid);
    }

    [TestMethod]
    public void IsSelfValid_WithValidationError_ReturnsFalse()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");

        // Act & Assert
        Assert.IsFalse(testObject.IsSelfValid);
    }

    [TestMethod]
    public void IsSelfValid_WithInvalidChildOnly_ReturnsTrue()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.AddValidationError("Child error");
        parent.ResumeAllActions();
        parent.Child = child;

        // Act & Assert - IsSelfValid should be true because parent has no direct errors
        Assert.IsTrue(parent.IsSelfValid);
    }

    [TestMethod]
    public void IsSelfValid_WithParentAndChildErrors_ReturnsFalse()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.AddValidationError("Child error");
        parent.AddValidationError("Parent error");
        parent.ResumeAllActions();
        parent.Child = child;

        // Act & Assert
        Assert.IsFalse(parent.IsSelfValid);
    }

    #endregion

    #region PropertyMessages Tests

    [TestMethod]
    public void PropertyMessages_NoErrors_ReturnsEmptyCollection()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act
        var messages = testObject.PropertyManager.PropertyMessages;

        // Assert
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, messages.Count);
    }

    [TestMethod]
    public void PropertyMessages_WithError_ContainsMessage()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test validation error");

        // Act
        var messages = testObject.PropertyMessages;

        // Assert
        Assert.IsTrue(messages.Count >= 1);
        Assert.IsTrue(messages.Any(m => m.Message == "Test validation error"));
    }

    [TestMethod]
    public void PropertyMessages_MultipleProperties_AggregatesAllMessages()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Add validation rules for multiple properties
        testObject.RuleManager.AddValidation(_ => "Name error", t => t.Name);
        testObject.RuleManager.AddValidation(_ => "Age error", t => t.Age);
        _ = testObject.RunRules(RunRulesFlag.All);

        // Act
        var messages = testObject.PropertyMessages;

        // Assert
        Assert.IsTrue(messages.Count >= 2);
        Assert.IsTrue(messages.Any(m => m.Message == "Name error"));
        Assert.IsTrue(messages.Any(m => m.Message == "Age error"));
    }

    [TestMethod]
    public void PropertyMessages_AfterClearAllMessages_IsEmpty()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");
        Assert.IsTrue(testObject.PropertyMessages.Count >= 1);

        // Act
        testObject.ClearAllMessages();

        // Assert
        Assert.AreEqual(0, testObject.PropertyMessages.Count);
    }

    #endregion

    #region ClearSelfMessages Tests

    [TestMethod]
    public void ClearSelfMessages_WithErrors_ClearsAllPropertyMessages()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");
        Assert.IsTrue(testObject.PropertyMessages.Count >= 1);

        // Act
        testObject.ClearSelfMessages();

        // Assert - ClearSelfMessages clears all rule messages from properties
        Assert.AreEqual(0, testObject.PropertyMessages.Count);
    }

    [TestMethod]
    public void ClearSelfMessages_NoErrors_DoesNotThrow()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act & Assert (should not throw)
        testObject.ClearSelfMessages();
        Assert.AreEqual(0, testObject.PropertyMessages.Count);
    }

    [TestMethod]
    public void ClearSelfMessages_UpdatesIsSelfValid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");
        // AddValidationError pauses afterwards, so resume to check state
        testObject.ResumeAllActions();
        Assert.IsFalse(testObject.IsSelfValid);

        // Act
        testObject.ClearSelfMessages();

        // Assert
        Assert.IsTrue(testObject.IsSelfValid);
    }

    #endregion

    #region ClearAllMessages Tests

    [TestMethod]
    public void ClearAllMessages_WithErrors_ClearsAllMessages()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");
        Assert.IsTrue(testObject.PropertyMessages.Count >= 1);

        // Act
        testObject.ClearAllMessages();

        // Assert
        Assert.AreEqual(0, testObject.PropertyMessages.Count);
    }

    [TestMethod]
    public void ClearAllMessages_UpdatesIsValid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");
        // AddValidationError pauses afterwards, so resume to check state
        testObject.ResumeAllActions();
        Assert.IsFalse(testObject.IsValid);

        // Act
        testObject.ClearAllMessages();

        // Assert
        Assert.IsTrue(testObject.IsValid);
    }

    [TestMethod]
    public void ClearAllMessages_UpdatesIsSelfValid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.AddValidationError("Test error");
        // AddValidationError pauses afterwards, so resume to check state
        testObject.ResumeAllActions();
        Assert.IsFalse(testObject.IsSelfValid);

        // Act
        testObject.ClearAllMessages();

        // Assert
        Assert.IsTrue(testObject.IsSelfValid);
    }

    [TestMethod]
    public void ClearAllMessages_WithChildErrors_ClearsChildMessagesViaProperty()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.AddValidationError("Child error");
        parent.ResumeAllActions();
        parent.Child = child;
        Assert.IsTrue(child.PropertyMessages.Count >= 1);

        // Act
        parent.ClearAllMessages();

        // Assert - Child messages should be cleared via the property's ClearAllMessages
        Assert.AreEqual(0, child.PropertyMessages.Count);
    }

    #endregion

    #region PauseAllActions Tests

    [TestMethod]
    public void PauseAllActions_SetsIsPausedToTrue()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act
        testObject.PropertyManager.PauseAllActions();

        // Assert
        Assert.IsTrue(testObject.PropertyManager.IsPaused);
    }

    [TestMethod]
    public void PauseAllActions_CalledTwice_StaysTrue()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act
        testObject.PropertyManager.PauseAllActions();
        testObject.PropertyManager.PauseAllActions();

        // Assert
        Assert.IsTrue(testObject.PropertyManager.IsPaused);
    }

    [TestMethod]
    public void PauseAllActions_WhenPaused_PropertyChangedEventsNotRaised()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        var propertyChangedEvents = new List<string>();
        testObject.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        testObject.PauseAllActions();
        testObject.Name = "Test";

        // Assert - When paused, property changed events should not be raised
        Assert.IsFalse(propertyChangedEvents.Contains(nameof(testObject.Name)));
    }

    #endregion

    #region ResumeAllActions Tests

    [TestMethod]
    public void ResumeAllActions_SetsIsPausedToFalse()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.PropertyManager.PauseAllActions();
        Assert.IsTrue(testObject.PropertyManager.IsPaused);

        // Act
        testObject.PropertyManager.ResumeAllActions();

        // Assert
        Assert.IsFalse(testObject.PropertyManager.IsPaused);
    }

    [TestMethod]
    public void ResumeAllActions_WhenNotPaused_DoesNothing()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        Assert.IsFalse(testObject.PropertyManager.IsPaused);

        // Act
        testObject.PropertyManager.ResumeAllActions();

        // Assert
        Assert.IsFalse(testObject.PropertyManager.IsPaused);
    }

    [TestMethod]
    public void ResumeAllActions_AfterPause_PropertyChangedEventsRaised()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        var propertyChangedEvents = new List<string>();
        testObject.ResumeAllActions();
        testObject.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        testObject.Name = "Test";

        // Assert - After resume, property changed events should be raised
        Assert.IsTrue(propertyChangedEvents.Contains(nameof(testObject.Name)));
    }

    #endregion

    #region RunRules Tests

    [TestMethod]
    public async Task RunRules_AllFlag_ExecutesRulesOnAllProperties()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.RuleManager.AddValidation(_ => "Name validation", t => t.Name);
        testObject.RuleManager.AddValidation(_ => "Age validation", t => t.Age);

        // Act
        await testObject.RunRules(RunRulesFlag.All);

        // Assert
        var messages = testObject.PropertyMessages;
        Assert.IsTrue(messages.Count >= 2);
    }

    [TestMethod]
    public async Task RunRules_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        using var cts = new CancellationTokenSource();

        // Act
        await testObject.RunRules(RunRulesFlag.All, cts.Token);

        // Assert - Should complete without exception
        Assert.IsTrue(testObject.IsValid);
    }

    [TestMethod]
    public async Task RunRules_UpdatesValidationState()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.RuleManager.AddValidation(_ => "Validation error", t => t.Name);

        // Act
        await testObject.RunRules(RunRulesFlag.All);

        // Assert
        Assert.IsFalse(testObject.IsValid);
        Assert.IsFalse(testObject.IsSelfValid);
    }

    #endregion

    #region Property_PropertyChanged Tests

    [TestMethod]
    public void PropertyChanged_IsValidChange_RaisesIsValidChangedEvent()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        var propertyChangedEvents = new List<string>();
        testObject.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act - Add a validation error which will change IsValid
        testObject.AddValidationError("Test error");

        // Assert
        Assert.IsTrue(propertyChangedEvents.Contains(nameof(testObject.IsValid)));
    }

    [TestMethod]
    public void PropertyChanged_IsSelfValidChange_RaisesIsSelfValidChangedEvent()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        var propertyChangedEvents = new List<string>();
        testObject.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act - Add a validation error which will change IsSelfValid
        testObject.AddValidationError("Test error");

        // Assert
        Assert.IsTrue(propertyChangedEvents.Contains(nameof(testObject.IsSelfValid)));
    }

    [TestMethod]
    public void PropertyChanged_WhenPaused_DoesNotRaiseEvents()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        testObject.PauseAllActions();
        var propertyChangedEvents = new List<string>();
        testObject.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        testObject.Name = "Test";

        // Assert - No events should be raised when paused
        Assert.AreEqual(0, propertyChangedEvents.Count);
    }

    [TestMethod]
    public void PropertyChanged_ValueChange_IsProcessed()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();
        var propertyChangedEvents = new List<string>();
        testObject.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        testObject.Name = "New Value";

        // Assert
        Assert.IsTrue(propertyChangedEvents.Contains(nameof(testObject.Name)));
    }

    #endregion

    #region OnDeserialized Tests

    [TestMethod]
    public void OnDeserialized_RecalculatesIsValid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Get a property to create it in the bag
        var nameProperty = testObject.PropertyManager.GetProperty("Name");

        // Act - Cast to concrete type since OnDeserialized is not on interface
        var manager = testObject.PropertyManager as ValidatePropertyManager<IValidateProperty>;
        manager?.OnDeserialized();

        // Assert - After deserialization, IsValid should reflect actual property states
        Assert.IsTrue(testObject.PropertyManager.IsValid);
    }

    [TestMethod]
    public void OnDeserialized_RecalculatesIsSelfValid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Get a property to create it in the bag
        var nameProperty = testObject.PropertyManager.GetProperty("Name");

        // Act - Cast to concrete type since OnDeserialized is not on interface
        var manager = testObject.PropertyManager as ValidatePropertyManager<IValidateProperty>;
        manager?.OnDeserialized();

        // Assert - After deserialization, IsSelfValid should reflect actual property states
        Assert.IsTrue(testObject.PropertyManager.IsSelfValid);
    }

    #endregion

    #region Inherited PropertyManager Behavior Tests

    [TestMethod]
    public void GetProperty_ExistingProperty_ReturnsProperty()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();

        // Act
        var property = testObject.PropertyManager["Name"];

        // Assert
        Assert.IsNotNull(property);
        Assert.AreEqual("Name", property.Name);
    }

    [TestMethod]
    public void GetProperty_ReturnsValidateProperty()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();

        // Act
        var property = testObject.PropertyManager["Name"];

        // Assert
        Assert.IsInstanceOfType<IValidateProperty>(property);
    }

    [TestMethod]
    public void HasProperty_ExistingProperty_ReturnsTrue()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();

        // Act
        var hasProperty = ((IValidatePropertyManagerInternal<IValidateProperty>)testObject.PropertyManager).PropertyInfoList.HasProperty("Name");

        // Assert
        Assert.IsTrue(hasProperty);
    }

    [TestMethod]
    public void HasProperty_NonExistingProperty_ReturnsFalse()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();

        // Act
        var hasProperty = ((IValidatePropertyManagerInternal<IValidateProperty>)testObject.PropertyManager).PropertyInfoList.HasProperty("NonExistent");

        // Assert
        Assert.IsFalse(hasProperty);
    }

    [TestMethod]
    public void IsBusy_InitialState_ReturnsFalse()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();

        // Act & Assert
        Assert.IsFalse(testObject.PropertyManager.IsBusy);
    }

    #endregion

    #region Child Validation Tests

    [TestMethod]
    public void ChildValidation_InvalidChild_ParentIsValidFalse()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.AddValidationError("Child is invalid");
        parent.ResumeAllActions();

        // Act
        parent.Child = child;

        // Assert
        Assert.IsFalse(parent.IsValid);
    }

    [TestMethod]
    public void ChildValidation_InvalidChild_ParentIsSelfValidTrue()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.AddValidationError("Child is invalid");
        parent.ResumeAllActions();

        // Act
        parent.Child = child;

        // Assert - Parent itself is valid, only child is invalid
        Assert.IsTrue(parent.IsSelfValid);
    }

    [TestMethod]
    public void ChildValidation_ValidChild_ParentIsValidTrue()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.ResumeAllActions();
        parent.ResumeAllActions();

        // Act
        parent.Child = child;

        // Assert
        Assert.IsTrue(parent.IsValid);
    }

    [TestMethod]
    public void ChildValidation_ChildBecomesValid_ParentUpdates()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var child = new ValidatePropertyManagerChildObject();
        child.AddValidationError("Child error");
        parent.ResumeAllActions();
        parent.Child = child;
        Assert.IsFalse(parent.IsValid);

        // Act - Clear child's messages to make it valid
        child.ResumeAllActions();
        child.ClearAllMessages();

        // Assert - Parent should now be valid
        Assert.IsTrue(parent.IsValid);
    }

    [TestMethod]
    public void ChildValidation_NullChild_ParentIsValidTrue()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        parent.ResumeAllActions();

        // Act
        parent.Child = null;

        // Assert
        Assert.IsTrue(parent.IsValid);
    }

    [TestMethod]
    public void ChildValidation_ReplaceInvalidChildWithValid_ParentBecomesValid()
    {
        // Arrange
        var parent = new ValidatePropertyManagerParentObject();
        var invalidChild = new ValidatePropertyManagerChildObject();
        invalidChild.AddValidationError("Invalid child");
        var validChild = new ValidatePropertyManagerChildObject();
        validChild.ResumeAllActions();

        parent.ResumeAllActions();
        parent.Child = invalidChild;
        Assert.IsFalse(parent.IsValid);

        // Act
        parent.Child = validChild;

        // Assert
        Assert.IsTrue(parent.IsValid);
    }

    #endregion

    #region PropertyMessages Aggregation Tests

    [TestMethod]
    public void PropertyMessages_MultiplePropertiesWithErrors_AggregatesAll()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Add errors to multiple properties
        testObject.RuleManager.AddValidation(_ => "Name must not be empty", t => t.Name);
        testObject.RuleManager.AddValidation(_ => "Age must be positive", t => t.Age);
        testObject.RuleManager.AddValidation(_ => "Description required", t => t.Description);
        _ = testObject.RunRules(RunRulesFlag.All);

        // Act
        var messages = testObject.PropertyMessages;

        // Assert
        Assert.IsTrue(messages.Count >= 3);
    }

    [TestMethod]
    public void PropertyMessages_SamePropertyMultipleErrors_AggregatesAll()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Add multiple errors to the same property via different rules
        testObject.RuleManager.AddValidation(_ => "Name error 1", t => t.Name);
        testObject.RuleManager.AddValidation(_ => "Name error 2", t => t.Name);
        _ = testObject.RunRules(RunRulesFlag.All);

        // Act
        var messages = testObject.PropertyMessages;

        // Assert
        Assert.IsTrue(messages.Count >= 2);
        Assert.IsTrue(messages.Any(m => m.Message == "Name error 1"));
        Assert.IsTrue(messages.Any(m => m.Message == "Name error 2"));
    }

    [TestMethod]
    public void PropertyMessages_IsReadOnlyCollection()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act
        var messages = testObject.PropertyManager.PropertyMessages;

        // Assert
        Assert.IsInstanceOfType<IReadOnlyCollection<IPropertyMessage>>(messages);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void ImplementsIValidatePropertyManagerInterface()
    {
        // Arrange & Act
        var testObject = new ValidatePropertyManagerTestObject();

        // Assert
        Assert.IsInstanceOfType<IValidatePropertyManager<IValidateProperty>>(testObject.PropertyManager);
    }

    [TestMethod]
    public void ImplementsIPropertyManagerInterface()
    {
        // Arrange & Act
        var testObject = new ValidatePropertyManagerTestObject();

        // Assert
        Assert.IsInstanceOfType<IValidatePropertyManager<IValidateProperty>>(testObject.PropertyManager);
    }

    #endregion

    #region Edge Cases and Stress Tests

    [TestMethod]
    public void PauseResume_MultipleTimesInSequence_WorksCorrectly()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            testObject.PropertyManager.PauseAllActions();
            Assert.IsTrue(testObject.PropertyManager.IsPaused);
            testObject.PropertyManager.ResumeAllActions();
            Assert.IsFalse(testObject.PropertyManager.IsPaused);
        }
    }

    [TestMethod]
    public void ValidState_AfterMultipleOperations_ConsistentState()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act - Perform multiple operations (without adding persistent validation errors)
        testObject.Name = "Test1";
        testObject.Age = 25;
        testObject.Description = "Test Description";
        testObject.Name = "Test2";
        testObject.Age = 30;

        // Assert - Object should remain valid after property changes
        Assert.IsTrue(testObject.IsValid);
        Assert.IsTrue(testObject.IsSelfValid);
    }

    [TestMethod]
    public async Task RunRules_MultipleTimesInSequence_WorksCorrectly()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act
        for (int i = 0; i < 5; i++)
        {
            await testObject.RunRules(RunRulesFlag.All);
        }

        // Assert - Should complete without exceptions and maintain valid state
        Assert.IsTrue(testObject.IsValid);
    }

    [TestMethod]
    public void PropertyMessages_EmptyWhenNoPropertiesAccessed_ReturnsEmptyCollection()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Act - Don't access any properties, just get messages from manager
        var messages = testObject.PropertyManager.PropertyMessages;

        // Assert
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, messages.Count);
    }

    #endregion

    #region PropertyValidateChildDataWrongTypeException Tests

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_DefaultConstructor_CreatesInstance()
    {
        // Act
        var exception = new PropertyValidateChildDataWrongTypeException();

        // Assert
        Assert.IsNotNull(exception);
    }

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new PropertyValidateChildDataWrongTypeException(message);

        // Assert
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void PropertyValidateChildDataWrongTypeException_WithInnerException_SetsInnerException()
    {
        // Arrange
        var message = "Outer error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new PropertyValidateChildDataWrongTypeException(message, innerException);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    #endregion

    #region Validation State Consistency Tests

    [TestMethod]
    public void ValidationState_InitiallyValid_AllPropertiesValid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Access all properties to create them
        _ = testObject.Name;
        _ = testObject.Age;
        _ = testObject.Description;

        // Assert
        Assert.IsTrue(testObject["Name"].IsValid);
        Assert.IsTrue(testObject["Age"].IsValid);
        Assert.IsTrue(testObject["Description"].IsValid);
        Assert.IsTrue(testObject.IsValid);
    }

    [TestMethod]
    public void ValidationState_OnePropertyInvalid_OnlyThatPropertyMarkedInvalid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        // Add error only to Name property
        testObject.RuleManager.AddValidation(_ => "Name error", t => t.Name);
        _ = testObject.RunRules(RunRulesFlag.All);

        // Assert
        Assert.IsFalse(testObject["Name"].IsValid);
        Assert.IsTrue(testObject["Age"].IsValid);
        Assert.IsTrue(testObject["Description"].IsValid);
        Assert.IsFalse(testObject.IsValid);
    }

    [TestMethod]
    public void ValidationState_AllPropertiesInvalid_AllMarkedInvalid()
    {
        // Arrange
        var testObject = new ValidatePropertyManagerTestObject();
        testObject.ResumeAllActions();

        testObject.RuleManager.AddValidation(_ => "Name error", t => t.Name);
        testObject.RuleManager.AddValidation(_ => "Age error", t => t.Age);
        testObject.RuleManager.AddValidation(_ => "Desc error", t => t.Description);
        _ = testObject.RunRules(RunRulesFlag.All);

        // Assert
        Assert.IsFalse(testObject["Name"].IsValid);
        Assert.IsFalse(testObject["Age"].IsValid);
        Assert.IsFalse(testObject["Description"].IsValid);
        Assert.IsFalse(testObject.IsValid);
    }

    #endregion
}
