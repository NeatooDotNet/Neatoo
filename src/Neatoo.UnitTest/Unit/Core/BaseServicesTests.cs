using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Core;

#region Test Helper Classes

/// <summary>
/// A simple ValidateBase for testing ValidateBaseServices.
/// </summary>
[SuppressFactory]
public class ValidateServicesTestTarget : ValidateBase<ValidateServicesTestTarget>
{
    public ValidateServicesTestTarget() : base(new ValidateBaseServices<ValidateServicesTestTarget>())
    {
        PauseAllActions();
    }

    public ValidateServicesTestTarget(ValidateBaseServices<ValidateServicesTestTarget> services) : base(services)
    {
        PauseAllActions();
    }

    public string? Name { get => Getter<string>(); set => Setter(value); }

    [Required]
    public string? RequiredField { get => Getter<string>(); set => Setter(value); }

    public int Age { get => Getter<int>(); set => Setter(value); }
}

/// <summary>
/// A simple EntityBase for testing EntityBaseServices.
/// </summary>
[SuppressFactory]
public class EntityServicesTestTarget : EntityBase<EntityServicesTestTarget>
{
    public EntityServicesTestTarget() : base(new EntityBaseServices<EntityServicesTestTarget>(null))
    {
        PauseAllActions();
    }

    public EntityServicesTestTarget(EntityBaseServices<EntityServicesTestTarget> services) : base(services)
    {
        PauseAllActions();
    }

    public string? Name { get => Getter<string>(); set => Setter(value); }

    [Required]
    public string? RequiredField { get => Getter<string>(); set => Setter(value); }

    public int Age { get => Getter<int>(); set => Setter(value); }

    // Expose MarkUnmodified for testing
    public new void MarkUnmodified() => base.MarkUnmodified();
}

#endregion

#region ValidateBaseServices Constructor Tests

[TestClass]
public class ValidateBaseServicesConstructorTests
{
    [TestMethod]
    public void DefaultConstructor_CreatesPropertyInfoList()
    {
        // Arrange & Act
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Assert
        Assert.IsNotNull(services.PropertyInfoList);
    }

    [TestMethod]
    public void DefaultConstructor_CreatesValidatePropertyManager()
    {
        // Arrange & Act
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Assert
        Assert.IsNotNull(services.ValidatePropertyManager);
    }

    [TestMethod]
    public void DefaultConstructor_CreatesRuleManagerFactory()
    {
        // Arrange & Act
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Assert
        Assert.IsNotNull(services.ruleManagerFactory);
    }

    [TestMethod]
    public void DefaultConstructor_PropertyManagerReturnsValidatePropertyManager()
    {
        // Arrange & Act
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Assert
        Assert.AreSame(services.ValidatePropertyManager, services.PropertyManager);
    }
}

#endregion

#region ValidateBaseServices PropertyInfoList Tests

[TestClass]
public class ValidateBaseServicesPropertyInfoListTests
{
    [TestMethod]
    public void PropertyInfoList_ContainsPublicProperties()
    {
        // Arrange
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Act & Assert
        Assert.IsTrue(services.PropertyInfoList.HasProperty("Name"));
        Assert.IsTrue(services.PropertyInfoList.HasProperty("RequiredField"));
        Assert.IsTrue(services.PropertyInfoList.HasProperty("Age"));
    }

    [TestMethod]
    public void PropertyInfoList_CanGetPropertyInfo()
    {
        // Arrange
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Act
        var propertyInfo = services.PropertyInfoList.GetPropertyInfo("Name");

        // Assert
        Assert.IsNotNull(propertyInfo);
        Assert.AreEqual("Name", propertyInfo.Name);
    }
}

#endregion

#region ValidateBaseServices CreateRuleManager Tests

[TestClass]
public class ValidateBaseServicesCreateRuleManagerTests
{
    [TestMethod]
    public void CreateRuleManager_ReturnsRuleManager()
    {
        // Arrange
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();
        var target = new ValidateServicesTestTarget(services);

        // Act
        var ruleManager = services.CreateRuleManager(target);

        // Assert
        Assert.IsNotNull(ruleManager);
        Assert.IsInstanceOfType(ruleManager, typeof(IRuleManager<ValidateServicesTestTarget>));
    }

    [TestMethod]
    public void CreateRuleManager_RuleManagerHasRulesFromAttributes()
    {
        // Arrange
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();
        var target = new ValidateServicesTestTarget(services);

        // Act
        var ruleManager = services.CreateRuleManager(target);

        // Assert - Should have RequiredRule for RequiredField
        var requiredRules = ruleManager.Rules.OfType<IRequiredRule>().ToList();
        Assert.IsTrue(requiredRules.Count > 0, "Expected at least one RequiredRule from [Required] attribute");
    }
}

#endregion

#region ValidateBaseServices Integration Tests

[TestClass]
public class ValidateBaseServicesIntegrationTests
{
    [TestMethod]
    public void Integration_TargetCanUseServices()
    {
        // Arrange
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Act
        var target = new ValidateServicesTestTarget(services);

        // Assert
        Assert.IsNotNull(target);
    }

    [TestMethod]
    public void Integration_TargetHasPropertyManager()
    {
        // Arrange & Act
        var target = new ValidateServicesTestTarget();

        // Assert - Target should be able to get properties
        var property = target["Name"];
        Assert.IsNotNull(property);
    }

    [TestMethod]
    public async Task Integration_TargetValidationWorks()
    {
        // Arrange
        var target = new ValidateServicesTestTarget();
        target.ResumeAllActions();

        // Act - Set required field
        target.RequiredField = "Test Value";
        await target.RunRules();

        // Assert
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public async Task Integration_TargetValidationFailsWithoutRequiredField()
    {
        // Arrange
        var target = new ValidateServicesTestTarget();
        target.ResumeAllActions();

        // Act
        await target.RunRules();

        // Assert
        Assert.IsFalse(target.IsValid);
    }
}

#endregion

#region EntityBaseServices Constructor Tests

[TestClass]
public class EntityBaseServicesConstructorTests
{
    [TestMethod]
    public void Constructor_WithNullFactory_CreatesServices()
    {
        // Arrange & Act
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert
        Assert.IsNotNull(services);
    }

    [TestMethod]
    public void Constructor_CreatesEntityPropertyManager()
    {
        // Arrange & Act
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert
        Assert.IsNotNull(services.EntityPropertyManager);
    }

    [TestMethod]
    public void Constructor_ValidatePropertyManagerReturnsEntityPropertyManager()
    {
        // Arrange & Act
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert
        Assert.AreSame(services.EntityPropertyManager, services.ValidatePropertyManager);
    }

    [TestMethod]
    public void Constructor_PropertyManagerReturnsEntityPropertyManager()
    {
        // Arrange & Act
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert
        Assert.AreSame(services.EntityPropertyManager, services.PropertyManager);
    }

    [TestMethod]
    public void Constructor_FactoryIsNull_WhenNullPassed()
    {
        // Arrange & Act
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert
        Assert.IsNull(services.Factory);
    }
}

#endregion

#region EntityBaseServices PropertyInfoList Tests

[TestClass]
public class EntityBaseServicesPropertyInfoListTests
{
    [TestMethod]
    public void PropertyInfoList_ContainsPublicProperties()
    {
        // Arrange
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Act & Assert
        Assert.IsTrue(services.PropertyInfoList.HasProperty("Name"));
        Assert.IsTrue(services.PropertyInfoList.HasProperty("RequiredField"));
        Assert.IsTrue(services.PropertyInfoList.HasProperty("Age"));
    }
}

#endregion

#region EntityBaseServices Integration Tests

[TestClass]
public class EntityBaseServicesIntegrationTests
{
    [TestMethod]
    public void Integration_TargetCanUseServices()
    {
        // Arrange
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Act
        var target = new EntityServicesTestTarget(services);

        // Assert
        Assert.IsNotNull(target);
    }

    [TestMethod]
    public void Integration_TargetHasPropertyManager()
    {
        // Arrange & Act
        var target = new EntityServicesTestTarget();

        // Assert
        var property = target["Name"];
        Assert.IsNotNull(property);
    }

    [TestMethod]
    public async Task Integration_TargetValidationWorks()
    {
        // Arrange
        var target = new EntityServicesTestTarget();
        target.ResumeAllActions();

        // Act
        target.RequiredField = "Test Value";
        await target.RunRules();

        // Assert
        Assert.IsTrue(target.IsValid);
    }

    [TestMethod]
    public void Integration_TargetTracksModifiedState()
    {
        // Arrange
        var target = new EntityServicesTestTarget();
        target.ResumeAllActions();

        // Act
        target.Name = "Test";

        // Assert - Entity should track modifications
        Assert.IsTrue(target.IsModified);
    }

    [TestMethod]
    public void Integration_TargetCanMarkUnmodified()
    {
        // Arrange
        var target = new EntityServicesTestTarget();
        target.ResumeAllActions();
        target.Name = "Test";
        Assert.IsTrue(target.IsModified);

        // Act
        target.MarkUnmodified();

        // Assert
        Assert.IsFalse(target.IsModified);
    }
}

#endregion

#region EntityBaseServices EntityPropertyManager Tests

[TestClass]
public class EntityBaseServicesEntityPropertyManagerTests
{
    [TestMethod]
    public void EntityPropertyManager_IsEntityPropertyManager()
    {
        // Arrange
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert
        Assert.IsInstanceOfType(services.EntityPropertyManager, typeof(IEntityPropertyManager));
    }

    [TestMethod]
    public void EntityPropertyManager_CanCreateEntityProperties()
    {
        // Arrange
        var target = new EntityServicesTestTarget();

        // Act
        var property = target["Name"];

        // Assert - Property should be entity property (have modified tracking)
        Assert.IsInstanceOfType(property, typeof(IEntityProperty));
    }
}

#endregion

#region ValidateBaseServices Interface Tests

[TestClass]
public class ValidateBaseServicesInterfaceTests
{
    [TestMethod]
    public void Implements_IValidateBaseServices()
    {
        // Arrange & Act
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Assert
        Assert.IsInstanceOfType(services, typeof(IValidateBaseServices<ValidateServicesTestTarget>));
    }
}

#endregion

#region EntityBaseServices Interface Tests

[TestClass]
public class EntityBaseServicesInterfaceTests
{
    [TestMethod]
    public void Implements_IEntityBaseServices()
    {
        // Arrange & Act
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert
        Assert.IsInstanceOfType(services, typeof(IEntityBaseServices<EntityServicesTestTarget>));
    }

    [TestMethod]
    public void ExtendsValidateBaseServices()
    {
        // Arrange & Act
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Assert - Should inherit from ValidateBaseServices
        Assert.IsInstanceOfType(services, typeof(ValidateBaseServices<EntityServicesTestTarget>));
    }
}

#endregion

#region Multiple Instances Tests

[TestClass]
public class BaseServicesMultipleInstancesTests
{
    [TestMethod]
    public void ValidateBaseServices_MultipleTargetsCanUseSameServices()
    {
        // Arrange
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();

        // Act
        var target1 = new ValidateServicesTestTarget(services);
        var target2 = new ValidateServicesTestTarget(services);

        // Assert
        Assert.IsNotNull(target1);
        Assert.IsNotNull(target2);
        Assert.AreNotSame(target1, target2);
    }

    [TestMethod]
    public void EntityBaseServices_MultipleTargetsCanUseSameServices()
    {
        // Arrange
        var services = new EntityBaseServices<EntityServicesTestTarget>(null);

        // Act
        var target1 = new EntityServicesTestTarget(services);
        var target2 = new EntityServicesTestTarget(services);

        // Assert
        Assert.IsNotNull(target1);
        Assert.IsNotNull(target2);
        Assert.AreNotSame(target1, target2);
    }

    [TestMethod]
    public void MultipleTargets_HaveIndependentProperties()
    {
        // Arrange
        var target1 = new EntityServicesTestTarget();
        var target2 = new EntityServicesTestTarget();
        target1.ResumeAllActions();
        target2.ResumeAllActions();

        // Act
        target1.Name = "Target 1";
        target2.Name = "Target 2";

        // Assert
        Assert.AreEqual("Target 1", target1.Name);
        Assert.AreEqual("Target 2", target2.Name);
    }

    [TestMethod]
    public async Task MultipleTargets_HaveIndependentValidation()
    {
        // Arrange
        var target1 = new EntityServicesTestTarget();
        var target2 = new EntityServicesTestTarget();
        target1.ResumeAllActions();
        target2.ResumeAllActions();

        // Act
        target1.RequiredField = "Valid";
        await target1.RunRules();
        await target2.RunRules();

        // Assert
        Assert.IsTrue(target1.IsValid);
        Assert.IsFalse(target2.IsValid);
    }
}

#endregion

#region RuleManagerFactory Tests

[TestClass]
public class RuleManagerFactoryTests
{
    [TestMethod]
    public void RuleManagerFactory_CreatesRuleManagerWithAttributeRules()
    {
        // Arrange
        var services = new ValidateBaseServices<ValidateServicesTestTarget>();
        var target = new ValidateServicesTestTarget(services);

        // Act
        var ruleManager = services.CreateRuleManager(target);

        // Assert
        Assert.IsNotNull(ruleManager);
        Assert.IsTrue(ruleManager.Rules.Count() > 0);
    }

    [TestMethod]
    public void RuleManagerFactory_CanBePassedToConstructor()
    {
        // Arrange
        var attributeToRule = new AttributeToRule();
        var ruleManagerFactory = new RuleManagerFactory<ValidateServicesTestTarget>(attributeToRule);
        var propertyInfoList = new PropertyInfoList<ValidateServicesTestTarget>(pi => new PropertyInfoWrapper(pi));

        // Act
        var services = new ValidateBaseServices<ValidateServicesTestTarget>(propertyInfoList, ruleManagerFactory);

        // Assert
        Assert.IsNotNull(services);
    }
}

#endregion
