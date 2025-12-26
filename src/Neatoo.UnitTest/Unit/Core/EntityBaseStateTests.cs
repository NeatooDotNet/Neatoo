using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Unit.Core;

/// <summary>
/// Unit tests for EntityBase{T} entity state flags and transitions.
/// Tests IsNew, IsChild, IsDeleted, IsModified, IsSelfModified, IsSavable, IsMarkedModified,
/// and the state transition methods (MarkNew, MarkOld, MarkAsChild, MarkDeleted, etc.).
/// Uses real Neatoo classes instead of mocks.
/// </summary>
[TestClass]
public class EntityBaseStateTests
{
    #region Test Helper Classes

    /// <summary>
    /// Concrete implementation of EntityBase for testing state flags.
    /// </summary>
    [SuppressFactory]
    private class TestEntity : EntityBase<TestEntity>
    {
        public TestEntity() : base(new EntityBaseServices<TestEntity>(null))
        {
            // Don't pause - we want to track changes
        }

        public string? Name { get => Getter<string>(); set => Setter(value); }
        public int Value { get => Getter<int>(); set => Setter(value); }

        // Expose protected members for testing
        public new void MarkNew() => base.MarkNew();
        public new void MarkOld() => base.MarkOld();
        public new void MarkAsChild() => base.MarkAsChild();
        public new void MarkDeleted() => base.MarkDeleted();
        public new void MarkModified() => base.MarkModified();
        public new void MarkUnmodified() => base.MarkUnmodified();

        public void Pause() => PauseAllActions();
        public void Resume() => ResumeAllActions();
    }

    /// <summary>
    /// Entity with child entity for testing child relationships.
    /// </summary>
    [SuppressFactory]
    private class TestParentEntity : EntityBase<TestParentEntity>
    {
        public TestParentEntity() : base(new EntityBaseServices<TestParentEntity>(null))
        {
        }

        public TestEntity? Child { get => Getter<TestEntity>(); set => Setter(value); }
    }

    private static TestEntity CreateEntity()
    {
        return new TestEntity();
    }

    private static TestEntity CreatePausedEntity()
    {
        var entity = new TestEntity();
        entity.Pause();
        return entity;
    }

    #endregion

    #region IsNew Tests

    [TestMethod]
    public void IsNew_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();

        // Assert
        Assert.IsFalse(entity.IsNew);
    }

    [TestMethod]
    public void IsNew_AfterMarkNew_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.MarkNew();

        // Assert
        Assert.IsTrue(entity.IsNew);
    }

    [TestMethod]
    public void IsNew_AfterMarkOld_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();
        entity.MarkNew();
        Assert.IsTrue(entity.IsNew);

        // Act
        entity.MarkOld();

        // Assert
        Assert.IsFalse(entity.IsNew);
    }

    [TestMethod]
    public void IsNew_AfterFactoryCompleteCreate_ReturnsTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Act
        entity.FactoryComplete(FactoryOperation.Create);

        // Assert
        Assert.IsTrue(entity.IsNew);
    }

    [TestMethod]
    public void IsNew_AfterFactoryCompleteInsert_ReturnsFalse()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.MarkNew();

        // Act
        entity.FactoryComplete(FactoryOperation.Insert);

        // Assert
        Assert.IsFalse(entity.IsNew);
    }

    [TestMethod]
    public void IsNew_AfterFactoryCompleteUpdate_ReturnsFalse()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Act
        entity.FactoryComplete(FactoryOperation.Update);

        // Assert
        Assert.IsFalse(entity.IsNew);
    }

    #endregion

    #region IsChild Tests

    [TestMethod]
    public void IsChild_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();

        // Assert
        Assert.IsFalse(entity.IsChild);
    }

    [TestMethod]
    public void IsChild_AfterMarkAsChild_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.MarkAsChild();

        // Assert
        Assert.IsTrue(entity.IsChild);
    }

    [TestMethod]
    public void IsChild_CannotBeReversed()
    {
        // Once marked as child, entity stays as child
        // Arrange
        var entity = CreateEntity();
        entity.MarkAsChild();

        // Assert - No method to unmark as child
        Assert.IsTrue(entity.IsChild);
    }

    #endregion

    #region IsDeleted Tests

    [TestMethod]
    public void IsDeleted_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();

        // Assert
        Assert.IsFalse(entity.IsDeleted);
    }

    [TestMethod]
    public void IsDeleted_AfterDelete_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.Delete();

        // Assert
        Assert.IsTrue(entity.IsDeleted);
    }

    [TestMethod]
    public void IsDeleted_AfterMarkDeleted_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.MarkDeleted();

        // Assert
        Assert.IsTrue(entity.IsDeleted);
    }

    [TestMethod]
    public void IsDeleted_AfterUnDelete_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Delete();
        Assert.IsTrue(entity.IsDeleted);

        // Act
        entity.UnDelete();

        // Assert
        Assert.IsFalse(entity.IsDeleted);
    }

    [TestMethod]
    public void UnDelete_WhenNotDeleted_NoEffect()
    {
        // Arrange
        var entity = CreateEntity();
        Assert.IsFalse(entity.IsDeleted);

        // Act
        entity.UnDelete();

        // Assert
        Assert.IsFalse(entity.IsDeleted);
    }

    [TestMethod]
    public void Delete_RaisesPropertyChanged()
    {
        // Arrange
        var entity = CreateEntity();
        var propertyNames = new List<string>();
        entity.PropertyChanged += (s, e) => propertyNames.Add(e.PropertyName!);

        // Act
        entity.Delete();

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsDeleted"));
    }

    [TestMethod]
    public void UnDelete_RaisesPropertyChanged()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Delete();
        var propertyNames = new List<string>();
        entity.PropertyChanged += (s, e) => propertyNames.Add(e.PropertyName!);

        // Act
        entity.UnDelete();

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsDeleted"));
    }

    #endregion

    #region IsModified Tests

    [TestMethod]
    public void IsModified_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Assert
        Assert.IsFalse(entity.IsModified);
    }

    [TestMethod]
    public void IsModified_AfterPropertyChange_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(entity.IsModified);
    }

    [TestMethod]
    public void IsModified_WhenIsNew_ReturnsTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.MarkNew();

        // Assert
        Assert.IsTrue(entity.IsModified);
    }

    [TestMethod]
    public void IsModified_WhenIsDeleted_ReturnsTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.Delete();

        // Assert
        Assert.IsTrue(entity.IsModified);
    }

    [TestMethod]
    public void IsModified_WhenIsSelfModified_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.MarkModified();

        // Assert
        Assert.IsTrue(entity.IsModified);
    }

    [TestMethod]
    public void IsModified_AfterMarkUnmodified_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Name = "Test";
        Assert.IsTrue(entity.IsModified);

        // Act
        entity.MarkUnmodified();

        // Assert
        Assert.IsFalse(entity.IsModified);
    }

    #endregion

    #region IsSelfModified Tests

    [TestMethod]
    public void IsSelfModified_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Assert
        Assert.IsFalse(entity.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterPropertyChange_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(entity.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_WhenIsDeleted_ReturnsTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.Delete();

        // Assert
        Assert.IsTrue(entity.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterMarkModified_ReturnsTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Act
        entity.MarkModified();

        // Assert
        Assert.IsTrue(entity.IsSelfModified);
    }

    [TestMethod]
    public void IsSelfModified_AfterMarkUnmodified_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Name = "Test";
        Assert.IsTrue(entity.IsSelfModified);

        // Act
        entity.MarkUnmodified();

        // Assert
        Assert.IsFalse(entity.IsSelfModified);
    }

    #endregion

    #region IsMarkedModified Tests

    [TestMethod]
    public void IsMarkedModified_InitialState_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();

        // Assert
        Assert.IsFalse(entity.IsMarkedModified);
    }

    [TestMethod]
    public void IsMarkedModified_AfterMarkModified_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.MarkModified();

        // Assert
        Assert.IsTrue(entity.IsMarkedModified);
    }

    [TestMethod]
    public void IsMarkedModified_AfterMarkUnmodified_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();
        entity.MarkModified();

        // Act
        entity.MarkUnmodified();

        // Assert
        Assert.IsFalse(entity.IsMarkedModified);
    }

    [TestMethod]
    public void IsMarkedModified_PropertyChangeDoesNotAffect()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.Name = "Test";

        // Assert - Property change doesn't set IsMarkedModified
        Assert.IsFalse(entity.IsMarkedModified);
        Assert.IsTrue(entity.IsSelfModified);
    }

    #endregion

    #region IsSavable Tests

    [TestMethod]
    public void IsSavable_InitialState_ReturnsFalse()
    {
        // Not modified, so not savable
        var entity = CreatePausedEntity();

        // Assert
        Assert.IsFalse(entity.IsSavable);
    }

    [TestMethod]
    public void IsSavable_WhenModifiedAndValid_ReturnsTrue()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsValid);
        Assert.IsFalse(entity.IsBusy);
        Assert.IsFalse(entity.IsChild);
        Assert.IsTrue(entity.IsSavable);
    }

    [TestMethod]
    public void IsSavable_WhenNotModified_ReturnsFalse()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.Resume();

        // Assert
        Assert.IsFalse(entity.IsModified);
        Assert.IsFalse(entity.IsSavable);
    }

    [TestMethod]
    public void IsSavable_WhenChild_ReturnsFalse()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Name = "Test";
        entity.MarkAsChild();

        // Assert
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsChild);
        Assert.IsFalse(entity.IsSavable);
    }

    [TestMethod]
    public void IsSavable_WhenNew_ReturnsTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.MarkNew();
        entity.Resume();

        // Assert
        Assert.IsTrue(entity.IsNew);
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsSavable);
    }

    [TestMethod]
    public void IsSavable_WhenDeleted_ReturnsTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.Delete();
        entity.Resume();

        // Assert
        Assert.IsTrue(entity.IsDeleted);
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsSavable);
    }

    #endregion

    #region ModifiedProperties Tests

    [TestMethod]
    public void ModifiedProperties_InitialState_ReturnsEmpty()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Assert
        Assert.AreEqual(0, entity.ModifiedProperties.Count());
    }

    [TestMethod]
    public void ModifiedProperties_AfterPropertyChange_ContainsPropertyName()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(entity.ModifiedProperties.Contains("Name"));
    }

    [TestMethod]
    public void ModifiedProperties_MultipleChanges_ContainsAllPropertyNames()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        entity.Name = "Test";
        entity.Value = 42;

        // Assert
        var modifiedProps = entity.ModifiedProperties.ToList();
        Assert.IsTrue(modifiedProps.Contains("Name"));
        Assert.IsTrue(modifiedProps.Contains("Value"));
    }

    [TestMethod]
    public void ModifiedProperties_AfterMarkUnmodified_ReturnsEmpty()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Name = "Test";
        Assert.IsTrue(entity.ModifiedProperties.Any());

        // Act
        entity.MarkUnmodified();

        // Assert
        Assert.AreEqual(0, entity.ModifiedProperties.Count());
    }

    #endregion

    #region State Transition Tests

    [TestMethod]
    public void FactoryComplete_Create_SetsIsNewTrue()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Act
        entity.FactoryComplete(FactoryOperation.Create);

        // Assert
        Assert.IsTrue(entity.IsNew);
    }

    [TestMethod]
    public void FactoryComplete_Insert_SetsIsNewFalseAndUnmodified()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.MarkNew();
        entity.Name = "Test";

        // Act
        entity.FactoryComplete(FactoryOperation.Insert);

        // Assert
        Assert.IsFalse(entity.IsNew);
        Assert.IsFalse(entity.IsModified);
    }

    [TestMethod]
    public void FactoryComplete_Update_SetsUnmodified()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.Name = "Test";

        // Act
        entity.FactoryComplete(FactoryOperation.Update);

        // Assert
        Assert.IsFalse(entity.IsModified);
    }

    [TestMethod]
    public void FactoryComplete_Fetch_NoStateChange()
    {
        // Arrange
        var entity = CreatePausedEntity();

        // Act
        entity.FactoryComplete(FactoryOperation.Fetch);

        // Assert
        Assert.IsFalse(entity.IsNew);
        Assert.IsFalse(entity.IsModified);
    }

    #endregion

    #region PropertyChanged Event Tests

    [TestMethod]
    public void MarkNew_SetsIsNewToTrue()
    {
        // Arrange
        var entity = CreateEntity();
        Assert.IsFalse(entity.IsNew);

        // Act
        entity.MarkNew();

        // Assert
        Assert.IsTrue(entity.IsNew);
        // Note: MarkNew doesn't call CheckIfMetaPropertiesChanged by design
        // It's meant to be called during factory operations when paused
    }

    [TestMethod]
    public void MarkModified_RaisesPropertyChanged()
    {
        // Arrange
        var entity = CreateEntity();
        var propertyNames = new List<string>();
        entity.PropertyChanged += (s, e) => propertyNames.Add(e.PropertyName!);

        // Act
        entity.MarkModified();

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsModified") || propertyNames.Contains("IsSelfModified"));
    }

    [TestMethod]
    public void IsSavable_Change_RaisesPropertyChanged()
    {
        // Arrange
        var entity = CreateEntity();
        var propertyNames = new List<string>();
        entity.PropertyChanged += (s, e) => propertyNames.Add(e.PropertyName!);

        // Act - Make entity savable by modifying it
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsSavable"));
    }

    #endregion

    #region NeatooPropertyChanged Event Tests

    [TestMethod]
    public void IsModified_Change_RaisesNeatooPropertyChanged()
    {
        // Arrange
        var entity = CreateEntity();
        var propertyNames = new List<string>();
        entity.NeatooPropertyChanged += args =>
        {
            propertyNames.Add(args.PropertyName);
            return Task.CompletedTask;
        };

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsModified"));
    }

    [TestMethod]
    public void IsSavable_Change_RaisesNeatooPropertyChanged()
    {
        // Arrange
        var entity = CreateEntity();
        var propertyNames = new List<string>();
        entity.NeatooPropertyChanged += args =>
        {
            propertyNames.Add(args.PropertyName);
            return Task.CompletedTask;
        };

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(propertyNames.Contains("IsSavable"));
    }

    #endregion

    #region PauseAllActions Tests

    [TestMethod]
    public void PauseAllActions_PropertyChanges_DoNotSetModified()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Pause();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsFalse(entity.IsSelfModified);
    }

    [TestMethod]
    public void ResumeAllActions_PropertyChanges_SetModified()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Pause();
        entity.Resume();

        // Act
        entity.Name = "Test";

        // Assert
        Assert.IsTrue(entity.IsSelfModified);
    }

    #endregion

    #region Save Exception Tests

    [TestMethod]
    public async Task Save_WhenChild_ThrowsSaveOperationException()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Name = "Test";
        entity.MarkAsChild();

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<SaveOperationException>(() => entity.Save());
        Assert.AreEqual(SaveFailureReason.IsChildObject, ex.Reason);
    }

    [TestMethod]
    public async Task Save_WhenNotModified_ThrowsSaveOperationException()
    {
        // Arrange
        var entity = CreatePausedEntity();
        entity.Resume();

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<SaveOperationException>(() => entity.Save());
        Assert.AreEqual(SaveFailureReason.NotModified, ex.Reason);
    }

    [TestMethod]
    public async Task Save_WhenNoFactory_ThrowsSaveOperationException()
    {
        // Arrange
        var entity = CreateEntity();
        entity.Name = "Test"; // Make it savable

        // Act & Assert
        var ex = await Assert.ThrowsExceptionAsync<SaveOperationException>(() => entity.Save());
        Assert.AreEqual(SaveFailureReason.NoFactoryMethod, ex.Reason);
    }

    #endregion

    #region Interface Implementation Tests

    [TestMethod]
    public void ImplementsIEntityBaseInterface()
    {
        // Arrange
        var entity = CreateEntity();

        // Assert
        Assert.IsInstanceOfType(entity, typeof(IEntityBase));
    }

    [TestMethod]
    public void ImplementsIEntityMetaPropertiesInterface()
    {
        // Arrange
        var entity = CreateEntity();

        // Assert
        Assert.IsInstanceOfType(entity, typeof(IEntityMetaProperties));
    }

    [TestMethod]
    public void ImplementsIValidateBaseInterface()
    {
        // Arrange
        var entity = CreateEntity();

        // Assert
        Assert.IsInstanceOfType(entity, typeof(IValidateBase));
    }

    #endregion

    #region Complex Scenario Tests

    [TestMethod]
    public void Scenario_NewEntityLifecycle()
    {
        // Simulate typical new entity lifecycle
        var entity = CreatePausedEntity();

        // Factory creates entity
        entity.FactoryComplete(FactoryOperation.Create);
        Assert.IsTrue(entity.IsNew);
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsSavable);

        // User modifies entity
        entity.Name = "New Name";
        Assert.IsTrue(entity.IsModified);

        // Factory saves entity (insert)
        entity.Pause();
        entity.FactoryComplete(FactoryOperation.Insert);
        Assert.IsFalse(entity.IsNew);
        Assert.IsFalse(entity.IsModified);
        Assert.IsFalse(entity.IsSavable);
    }

    [TestMethod]
    public void Scenario_ExistingEntityLifecycle()
    {
        // Simulate typical existing entity lifecycle
        var entity = CreatePausedEntity();

        // Factory fetches entity
        entity.FactoryComplete(FactoryOperation.Fetch);
        Assert.IsFalse(entity.IsNew);
        Assert.IsFalse(entity.IsModified);
        Assert.IsFalse(entity.IsSavable);

        // User modifies entity
        entity.Name = "Updated Name";
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsSavable);

        // Factory saves entity (update)
        entity.Pause();
        entity.FactoryComplete(FactoryOperation.Update);
        Assert.IsFalse(entity.IsModified);
        Assert.IsFalse(entity.IsSavable);
    }

    [TestMethod]
    public void Scenario_DeleteEntityLifecycle()
    {
        // Simulate delete lifecycle
        var entity = CreatePausedEntity();
        entity.FactoryComplete(FactoryOperation.Fetch);

        // User deletes entity
        entity.Delete();
        Assert.IsTrue(entity.IsDeleted);
        Assert.IsTrue(entity.IsModified);
        Assert.IsTrue(entity.IsSavable);

        // User changes mind
        entity.UnDelete();
        Assert.IsFalse(entity.IsDeleted);
        Assert.IsFalse(entity.IsModified);
        Assert.IsFalse(entity.IsSavable);
    }

    #endregion
}
