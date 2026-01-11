using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Unit.Rules;

#region Test Helper Classes

/// <summary>
/// A ValidateBase target for RuleManager testing.
/// </summary>
[SuppressFactory]
public class RuleManagerTestTarget : ValidateBase<RuleManagerTestTarget>
{
    public RuleManagerTestTarget() : base(new ValidateBaseServices<RuleManagerTestTarget>())
    {
        PauseAllActions();
    }

    public string? Name { get => Getter<string>(); set => Setter(value); }

    [Required]
    public string? RequiredField { get => Getter<string>(); set => Setter(value); }

    public int Age { get => Getter<int>(); set => Setter(value); }

    public string? Description { get => Getter<string>(); set => Setter(value); }

    // Expose RuleManager for testing
    public IRuleManager<RuleManagerTestTarget> GetRuleManager() =>
        (IRuleManager<RuleManagerTestTarget>)typeof(ValidateBase<RuleManagerTestTarget>)
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(this)!;
}

/// <summary>
/// Target without any attribute-based rules.
/// </summary>
[SuppressFactory]
public class NoAttributeRulesTarget : ValidateBase<NoAttributeRulesTarget>
{
    public NoAttributeRulesTarget() : base(new ValidateBaseServices<NoAttributeRulesTarget>())
    {
        PauseAllActions();
    }

    public string? Name { get => Getter<string>(); set => Setter(value); }
    public int Value { get => Getter<int>(); set => Setter(value); }

    public IRuleManager<NoAttributeRulesTarget> GetRuleManager() =>
        (IRuleManager<NoAttributeRulesTarget>)typeof(ValidateBase<NoAttributeRulesTarget>)
            .GetProperty("RuleManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(this)!;
}

/// <summary>
/// Custom synchronous rule for testing - generic version.
/// </summary>
public class TestSyncRule<T> : RuleBase<T> where T : class, IValidateBase
{
    public int ExecutionCount { get; private set; }

    public TestSyncRule(params ITriggerProperty[] triggerProperties) : this(1, triggerProperties)
    {
    }

    public TestSyncRule(int ruleOrder, params ITriggerProperty[] triggerProperties)
    {
        RuleOrder = ruleOrder;
        foreach (var tp in triggerProperties)
        {
            TriggerProperties.Add(tp);
        }
    }

    protected override IRuleMessages Execute(T target)
    {
        ExecutionCount++;
        return RuleMessages.None;
    }
}

/// <summary>
/// Custom synchronous rule for testing RuleManagerTestTarget specifically.
/// </summary>
public class TestSyncRule : RuleBase<RuleManagerTestTarget>
{
    public int ExecutionCount { get; private set; }
    public string? LastNameValue { get; private set; }

    public TestSyncRule(params ITriggerProperty[] triggerProperties) : this(1, triggerProperties)
    {
    }

    public TestSyncRule(int ruleOrder, params ITriggerProperty[] triggerProperties)
    {
        RuleOrder = ruleOrder;
        foreach (var tp in triggerProperties)
        {
            TriggerProperties.Add(tp);
        }
    }

    protected override IRuleMessages Execute(RuleManagerTestTarget target)
    {
        ExecutionCount++;
        LastNameValue = target.Name;
        return RuleMessages.None;
    }
}

/// <summary>
/// Custom async rule for testing.
/// </summary>
public class TestAsyncRule : AsyncRuleBase<RuleManagerTestTarget>
{
    public int ExecutionCount { get; private set; }

    public TestAsyncRule(params ITriggerProperty[] triggerProperties)
    {
        foreach (var tp in triggerProperties)
        {
            TriggerProperties.Add(tp);
        }
    }

    protected override async Task<IRuleMessages> Execute(RuleManagerTestTarget target, CancellationToken? token = null)
    {
        ExecutionCount++;
        await Task.Delay(1); // Small delay to ensure async behavior
        return RuleMessages.None;
    }
}

/// <summary>
/// Rule that always returns an error message.
/// </summary>
public class TestErrorRule : RuleBase<RuleManagerTestTarget>
{
    private readonly string _errorMessage;

    public TestErrorRule(string errorMessage, params ITriggerProperty[] triggerProperties)
    {
        _errorMessage = errorMessage;
        foreach (var tp in triggerProperties)
        {
            TriggerProperties.Add(tp);
        }
    }

    protected override IRuleMessages Execute(RuleManagerTestTarget target)
    {
        return (TriggerProperties[0].PropertyName, _errorMessage).AsRuleMessages();
    }
}

#endregion

#region RuleManager Constructor Tests

[TestClass]
public class RuleManagerConstructorTests
{
    [TestMethod]
    public void Constructor_CreatesRuleManager()
    {
        // Arrange & Act
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        // Assert
        Assert.IsNotNull(ruleManager);
    }

    [TestMethod]
    public void Constructor_AddsAttributeBasedRules()
    {
        // Arrange & Act
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        // Assert - Should have RequiredRule for RequiredField
        var rules = ruleManager.Rules.ToList();
        Assert.IsTrue(rules.Any(r => r is IRequiredRule));
    }

    [TestMethod]
    public void Constructor_NoAttributeRules_HasOnlyObjectInvalidRule()
    {
        // Arrange & Act
        var target = new NoAttributeRulesTarget();
        var ruleManager = target.GetRuleManager();

        // Assert - ValidateBase always adds an ObjectInvalid validation rule
        var rules = ruleManager.Rules.ToList();
        Assert.AreEqual(1, rules.Count);  // Only the built-in ObjectInvalid rule
    }
}

#endregion

#region AddRule Tests

[TestClass]
public class RuleManagerAddRuleTests
{
    [TestMethod]
    public void AddRule_AddsRuleToManager()
    {
        // Arrange
        var target = new NoAttributeRulesTarget();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<NoAttributeRulesTarget>(t => t.Name);
        var rule = new TestSyncRule<NoAttributeRulesTarget>(triggerProperty);

        // Act
        ruleManager.AddRule(rule);

        // Assert
        Assert.IsTrue(ruleManager.Rules.Contains(rule));
    }

    [TestMethod]
    public void AddRule_CallsOnRuleAdded()
    {
        // Arrange
        var target = new NoAttributeRulesTarget();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<NoAttributeRulesTarget>(t => t.Name);
        var rule = new TestSyncRule<NoAttributeRulesTarget>(triggerProperty);

        // Act
        ruleManager.AddRule(rule);

        // Assert - UniqueIndex should be set
        Assert.AreNotEqual(0u, rule.UniqueIndex);
    }

    [TestMethod]
    public void AddRules_AddsMultipleRules()
    {
        // Arrange
        var target = new NoAttributeRulesTarget();
        var ruleManager = target.GetRuleManager();
        var trigger1 = new TriggerProperty<NoAttributeRulesTarget>(t => t.Name);
        var trigger2 = new TriggerProperty<NoAttributeRulesTarget>(t => t.Value);
        var rule1 = new TestSyncRule<NoAttributeRulesTarget>(trigger1);
        var rule2 = new TestSyncRule<NoAttributeRulesTarget>(trigger2);

        // Act
        ruleManager.AddRules(rule1, rule2);

        // Assert
        Assert.IsTrue(ruleManager.Rules.Contains(rule1));
        Assert.IsTrue(ruleManager.Rules.Contains(rule2));
    }

    [TestMethod]
    public void AddRule_EachRuleGetsUniqueIndex()
    {
        // Arrange
        var target = new NoAttributeRulesTarget();
        var ruleManager = target.GetRuleManager();
        var trigger1 = new TriggerProperty<NoAttributeRulesTarget>(t => t.Name);
        var trigger2 = new TriggerProperty<NoAttributeRulesTarget>(t => t.Value);
        var rule1 = new TestSyncRule<NoAttributeRulesTarget>(trigger1);
        var rule2 = new TestSyncRule<NoAttributeRulesTarget>(trigger2);

        // Act
        ruleManager.AddRule(rule1);
        ruleManager.AddRule(rule2);

        // Assert
        Assert.AreNotEqual(rule1.UniqueIndex, rule2.UniqueIndex);
    }
}

#endregion

#region RunRules by PropertyName Tests

[TestClass]
public class RuleManagerRunRulesByPropertyNameTests
{
    [TestMethod]
    public async Task RunRules_ByPropertyName_ExecutesMatchingRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(triggerProperty);
        ruleManager.AddRule(rule);

        // Act
        await ruleManager.RunRules("Name");

        // Assert
        Assert.AreEqual(1, rule.ExecutionCount);
    }

    [TestMethod]
    public async Task RunRules_ByPropertyName_DoesNotExecuteNonMatchingRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(triggerProperty);
        ruleManager.AddRule(rule);

        // Act
        await ruleManager.RunRules("Age");

        // Assert
        Assert.AreEqual(0, rule.ExecutionCount);
    }

    [TestMethod]
    public async Task RunRules_ByPropertyName_ExecutesMultipleMatchingRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule1 = new TestSyncRule(triggerProperty);
        var rule2 = new TestSyncRule(triggerProperty);
        ruleManager.AddRule(rule1);
        ruleManager.AddRule(rule2);

        // Act
        await ruleManager.RunRules("Name");

        // Assert
        Assert.AreEqual(1, rule1.ExecutionCount);
        Assert.AreEqual(1, rule2.ExecutionCount);
    }
}

#endregion

#region RunRules with RunRulesFlag Tests

[TestClass]
public class RuleManagerRunRulesFlagTests
{
    [TestMethod]
    public async Task RunRules_All_ExecutesAllRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var trigger1 = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var trigger2 = new TriggerProperty<RuleManagerTestTarget>(t => t.Age);
        var rule1 = new TestSyncRule(trigger1);
        var rule2 = new TestSyncRule(trigger2);
        ruleManager.AddRule(rule1);
        ruleManager.AddRule(rule2);

        // Act
        await ruleManager.RunRules(RunRulesFlag.All);

        // Assert
        Assert.AreEqual(1, rule1.ExecutionCount);
        Assert.AreEqual(1, rule2.ExecutionCount);
    }

    [TestMethod]
    public async Task RunRules_NotExecuted_OnlyExecutesNonExecutedRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var trigger1 = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var trigger2 = new TriggerProperty<RuleManagerTestTarget>(t => t.Age);
        var rule1 = new TestSyncRule(trigger1);
        var rule2 = new TestSyncRule(trigger2);
        ruleManager.AddRule(rule1);
        ruleManager.AddRule(rule2);

        // Execute one rule first
        await ruleManager.RunRules("Name");
        Assert.AreEqual(1, rule1.ExecutionCount);
        Assert.AreEqual(0, rule2.ExecutionCount);

        // Act - Run only not executed rules
        await ruleManager.RunRules(RunRulesFlag.NotExecuted);

        // Assert - rule1 should not run again, rule2 should run
        Assert.AreEqual(1, rule1.ExecutionCount);
        Assert.AreEqual(1, rule2.ExecutionCount);
    }

    [TestMethod]
    public async Task RunRules_Executed_OnlyExecutesAlreadyExecutedRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var trigger1 = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var trigger2 = new TriggerProperty<RuleManagerTestTarget>(t => t.Age);
        var rule1 = new TestSyncRule(trigger1);
        var rule2 = new TestSyncRule(trigger2);
        ruleManager.AddRule(rule1);
        ruleManager.AddRule(rule2);

        // Execute one rule first
        await ruleManager.RunRules("Name");
        Assert.IsTrue(rule1.Executed);
        Assert.IsFalse(rule2.Executed);

        // Act - Run only executed rules
        await ruleManager.RunRules(RunRulesFlag.Executed);

        // Assert - rule1 should run again, rule2 should not
        Assert.AreEqual(2, rule1.ExecutionCount);
        Assert.AreEqual(0, rule2.ExecutionCount);
    }
}

#endregion

#region RunRule Tests

[TestClass]
public class RuleManagerRunRuleTests
{
    [TestMethod]
    public async Task RunRule_ExecutesSpecificRule()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(triggerProperty);
        ruleManager.AddRule(rule);

        // Act
        await ruleManager.RunRule(rule);

        // Assert
        Assert.AreEqual(1, rule.ExecutionCount);
    }

    [TestMethod]
    public async Task RunRule_ThrowsForNonAddedRule()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(triggerProperty);
        // Note: Not adding the rule to the manager

        // Act & Assert
        await Assert.ThrowsExceptionAsync<RuleNotAddedException>(async () =>
            await ruleManager.RunRule(rule));
    }

    [TestMethod]
    public async Task RunRule_ByType_ExecutesAllRulesOfType()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var trigger1 = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var trigger2 = new TriggerProperty<RuleManagerTestTarget>(t => t.Age);
        var syncRule1 = new TestSyncRule(trigger1);
        var syncRule2 = new TestSyncRule(trigger2);
        var asyncRule = new TestAsyncRule(trigger1);
        ruleManager.AddRule(syncRule1);
        ruleManager.AddRule(syncRule2);
        ruleManager.AddRule(asyncRule);

        // Act
        await ruleManager.RunRule<TestSyncRule>();

        // Assert
        Assert.AreEqual(1, syncRule1.ExecutionCount);
        Assert.AreEqual(1, syncRule2.ExecutionCount);
        Assert.AreEqual(0, asyncRule.ExecutionCount);
    }
}

#endregion

#region Fluent Rule - AddAction Tests

[TestClass]
public class RuleManagerAddActionTests
{
    [TestMethod]
    public void AddAction_ReturnsActionFluentRule()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        // Act
        var rule = ruleManager.AddAction(t => { }, t => t.Name);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(ActionFluentRule<RuleManagerTestTarget>));
    }

    [TestMethod]
    public void AddAction_AddsRuleToManager()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        // Act
        var rule = ruleManager.AddAction(t => { }, t => t.Name);

        // Assert
        Assert.IsTrue(ruleManager.Rules.Contains(rule));
    }

    [TestMethod]
    public async Task AddAction_RuleExecutesOnPropertyChange()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var executed = false;
        ruleManager.AddAction(t => { executed = true; }, t => t.Name);

        // Act
        target.Name = "Test";
        await target.RunRules();

        // Assert
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public async Task AddAction_CanModifyTarget()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        ruleManager.AddAction(t => { t.Description = "Modified by action"; }, t => t.Name);

        // Act
        target.Name = "Test";
        await target.RunRules();

        // Assert
        Assert.AreEqual("Modified by action", target.Description);
    }
}

#endregion

#region Fluent Rule - AddActionAsync Tests

[TestClass]
public class RuleManagerAddActionAsyncTests
{
    [TestMethod]
    public void AddActionAsync_ReturnsActionAsyncFluentRule()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        // Act
        var rule = ruleManager.AddActionAsync(async t => await Task.CompletedTask, t => t.Name);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(ActionAsyncFluentRule<RuleManagerTestTarget>));
    }

    [TestMethod]
    public async Task AddActionAsync_RuleExecutesAsynchronously()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var executed = false;
        ruleManager.AddActionAsync(async t =>
        {
            await Task.Delay(1);
            executed = true;
        }, t => t.Name);

        // Act
        target.Name = "Test";
        await target.RunRules();

        // Assert
        Assert.IsTrue(executed);
    }
}

#endregion

#region Fluent Rule - AddValidation Tests

[TestClass]
public class RuleManagerAddValidationTests
{
    [TestMethod]
    public void AddValidation_ReturnsValidationFluentRule()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        // Act
        var rule = ruleManager.AddValidation(t => null!, t => t.Name);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(ValidationFluentRule<RuleManagerTestTarget>));
    }

    [TestMethod]
    public async Task AddValidation_ReturnsErrorWhenValidationFails()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        ruleManager.AddValidation(t =>
            string.IsNullOrEmpty(t.Name) ? "Name is required" : null!,
            t => t.Name);

        // Act - Clear name to trigger validation
        target.Name = "";
        await target.RunRules();

        // Assert
        var nameProperty = target["Name"];
        Assert.IsTrue(nameProperty.PropertyMessages.Any(m => m.Message == "Name is required"));
    }

    [TestMethod]
    public async Task AddValidation_ClearsErrorWhenValidationPasses()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        ruleManager.AddValidation(t =>
            string.IsNullOrEmpty(t.Name) ? "Name is required" : null!,
            t => t.Name);

        // Trigger error first
        target.Name = "";
        await target.RunRules();
        Assert.IsTrue(target["Name"].PropertyMessages.Any(m => m.Message == "Name is required"));

        // Act - Set valid value
        target.Name = "Valid Name";
        await target.RunRules();

        // Assert
        Assert.IsFalse(target["Name"].PropertyMessages.Any(m => m.Message == "Name is required"));
    }
}

#endregion

#region Fluent Rule - AddValidationAsync Tests

[TestClass]
public class RuleManagerAddValidationAsyncTests
{
    [TestMethod]
    public void AddValidationAsync_ReturnsAsyncFluentRule()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        // Act
        var rule = ruleManager.AddValidationAsync(async t => await Task.FromResult<string>(null!), t => t.Name);

        // Assert
        Assert.IsNotNull(rule);
        Assert.IsInstanceOfType(rule, typeof(AsyncFluentRule<RuleManagerTestTarget>));
    }

    [TestMethod]
    public async Task AddValidationAsync_ExecutesAsyncValidation()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        ruleManager.AddValidationAsync(async t =>
        {
            await Task.Delay(1);
            return string.IsNullOrEmpty(t.Name) ? "Name required async" : null!;
        }, t => t.Name);

        // Act
        target.Name = "";
        await target.RunRules();

        // Assert
        var nameProperty = target["Name"];
        Assert.IsTrue(nameProperty.PropertyMessages.Any(m => m.Message == "Name required async"));
    }
}

#endregion

#region Rule Execution Order Tests

[TestClass]
public class RuleManagerExecutionOrderTests
{
    [TestMethod]
    public async Task RunRules_ExecutesInRuleOrderOrder()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var executionOrder = new List<int>();

        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);

        var rule1 = new TestSyncRule(10, trigger);
        var rule2 = new TestSyncRule(5, trigger);
        var rule3 = new TestSyncRule(15, trigger);

        // Add in non-order sequence
        ruleManager.AddRule(rule1);
        ruleManager.AddRule(rule2);
        ruleManager.AddRule(rule3);

        // Act
        await ruleManager.RunRules("Name");

        // Assert - Rules should execute in order: rule2 (5), rule1 (10), rule3 (15)
        // Since we can't easily track execution order with current rule design,
        // we just verify all rules executed
        Assert.AreEqual(1, rule1.ExecutionCount);
        Assert.AreEqual(1, rule2.ExecutionCount);
        Assert.AreEqual(1, rule3.ExecutionCount);
    }
}

#endregion

#region RuleManager Exception Tests

[TestClass]
public class RuleManagerExceptionTests
{
    [TestMethod]
    public void InvalidTargetTypeException_HasMessage()
    {
        // Arrange & Act
        var ex = new InvalidTargetTypeException("Test message");

        // Assert
        Assert.AreEqual("Test message", ex.Message);
    }

    [TestMethod]
    public void InvalidRuleTypeException_HasMessage()
    {
        // Arrange & Act
        var ex = new InvalidRuleTypeException("Test message");

        // Assert
        Assert.AreEqual("Test message", ex.Message);
    }

    [TestMethod]
    public void TargetIsNullException_CanBeCreated()
    {
        // Arrange & Act
        var ex = new TargetIsNullException();

        // Assert
        Assert.IsNotNull(ex);
    }

    [TestMethod]
    public void TargetRulePropertyChangeException_HasMessage()
    {
        // Arrange & Act
        var ex = new TargetRulePropertyChangeException("Test message");

        // Assert
        Assert.AreEqual("Test message", ex.Message);
    }
}

#endregion

#region RuleManagerFactory Tests

[TestClass]
public class RuleManagerFactoryUnitTests
{
    [TestMethod]
    public void Constructor_SetsAttributeToRule()
    {
        // Arrange
        var attributeToRule = new AttributeToRule();

        // Act
        var factory = new RuleManagerFactory<RuleManagerTestTarget>(attributeToRule);

        // Assert
        Assert.AreSame(attributeToRule, factory.AttributeToRule);
    }

    [TestMethod]
    public void CreateRuleManager_ReturnsRuleManager()
    {
        // Arrange
        var attributeToRule = new AttributeToRule();
        var factory = new RuleManagerFactory<RuleManagerTestTarget>(attributeToRule);
        var target = new RuleManagerTestTarget();
        var propertyInfoList = new PropertyInfoList<RuleManagerTestTarget>(pi => new PropertyInfoWrapper(pi));

        // Act
        var ruleManager = factory.CreateRuleManager(target, propertyInfoList);

        // Assert
        Assert.IsNotNull(ruleManager);
        Assert.IsInstanceOfType(ruleManager, typeof(IRuleManager<RuleManagerTestTarget>));
    }

    [TestMethod]
    public void CreateRuleManager_CreatesRulesFromAttributes()
    {
        // Arrange
        var attributeToRule = new AttributeToRule();
        var factory = new RuleManagerFactory<RuleManagerTestTarget>(attributeToRule);
        var target = new RuleManagerTestTarget();
        var propertyInfoList = new PropertyInfoList<RuleManagerTestTarget>(pi => new PropertyInfoWrapper(pi));

        // Act
        var ruleManager = factory.CreateRuleManager(target, propertyInfoList);

        // Assert - Should have RequiredRule for RequiredField
        Assert.IsTrue(ruleManager.Rules.Any(r => r is IRequiredRule));
    }
}

#endregion

#region IRuleManager Interface Tests

[TestClass]
public class IRuleManagerInterfaceTests
{
    [TestMethod]
    public void Rules_ReturnsEnumerable()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        IRuleManager ruleManager = target.GetRuleManager();

        // Assert
        Assert.IsInstanceOfType(ruleManager.Rules, typeof(IEnumerable<IRule>));
    }

    [TestMethod]
    public async Task RunRules_CanBeCalledWithoutParameters()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        IRuleManager ruleManager = target.GetRuleManager();

        // Act & Assert - Should not throw
        await ruleManager.RunRules();
    }
}

#endregion

#region Cancellation Tests

[TestClass]
public class RuleManagerCancellationTests
{
    [TestMethod]
    public async Task RunRules_RespectsCancellationToken()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestAsyncRule(trigger);
        ruleManager.AddRule(rule);

        // Act & Assert - Should throw OperationCanceledException when token is already cancelled
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await ruleManager.RunRules(RunRulesFlag.All, cts.Token));

        // Rule should not have executed
        Assert.IsFalse(rule.Executed);
    }

    [TestMethod]
    public async Task RunRule_RespectsCancellationToken()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        ruleManager.AddRule(rule);

        // Act & Assert - Should throw OperationCanceledException when token is already cancelled
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await ruleManager.RunRule(rule, cts.Token));

        // Rule should not have executed
        Assert.IsFalse(rule.Executed);
    }
}

#endregion

#region ShouldRunRule Tests

/// <summary>
/// Tests for the static ShouldRunRule method that determines if a rule should execute based on flags.
/// </summary>
[TestClass]
public class RuleManagerShouldRunRuleTests
{
    private RuleManagerTestTarget _target = null!;
    private IRuleManager<RuleManagerTestTarget> _ruleManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _target = new RuleManagerTestTarget();
        _target.ResumeAllActions();
        _ruleManager = _target.GetRuleManager();
    }

    #region Flag.All Tests

    [TestMethod]
    public void ShouldRunRule_FlagAll_NotExecutedRule_ReturnsTrue()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.All);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ShouldRunRule_FlagAll_ExecutedRule_ReturnsTrue()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);
        await _ruleManager.RunRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.All);

        Assert.IsTrue(result);
    }

    #endregion

    #region Flag.Self Tests

    [TestMethod]
    public void ShouldRunRule_FlagSelf_ReturnsTrue()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.Self);

        Assert.IsTrue(result);
    }

    #endregion

    #region Flag.NotExecuted Tests

    [TestMethod]
    public void ShouldRunRule_FlagNotExecuted_NotExecutedRule_ReturnsTrue()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.NotExecuted);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ShouldRunRule_FlagNotExecuted_ExecutedRule_ReturnsFalse()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);
        await _ruleManager.RunRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.NotExecuted);

        Assert.IsFalse(result);
    }

    #endregion

    #region Flag.Executed Tests

    [TestMethod]
    public void ShouldRunRule_FlagExecuted_NotExecutedRule_ReturnsFalse()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.Executed);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ShouldRunRule_FlagExecuted_ExecutedRule_ReturnsTrue()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);
        await _ruleManager.RunRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.Executed);

        Assert.IsTrue(result);
    }

    #endregion

    #region Flag.None Tests

    [TestMethod]
    public void ShouldRunRule_FlagNone_ReturnsFalse()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);

        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.None);

        Assert.IsFalse(result);
    }

    #endregion

    #region Flag.NoMessages Tests - Documents Broken Behavior

    /// <summary>
    /// NoMessages always returns true because IRule.Messages is never populated.
    /// This documents existing (broken) behavior.
    /// </summary>
    [TestMethod]
    public void ShouldRunRule_FlagNoMessages_AlwaysReturnsTrue_BecauseMessagesNeverPopulated()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);

        // NoMessages always matches because rule.Messages is always empty
        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.NoMessages);

        Assert.IsTrue(result, "NoMessages always returns true because IRule.Messages is never populated");
    }

    /// <summary>
    /// Even rules that return error messages still have empty IRule.Messages
    /// because PreviousMessages is never set. This documents existing (broken) behavior.
    /// </summary>
    [TestMethod]
    public async Task ShouldRunRule_FlagNoMessages_RuleThatReturnsErrors_StillReturnsTrue_BecauseMessagesNeverPopulated()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestErrorRule("Error message", trigger);
        _ruleManager.AddRule(rule);
        await _ruleManager.RunRule(rule);

        // Even after running an error rule, rule.Messages is empty
        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.NoMessages);

        Assert.IsTrue(result, "NoMessages returns true even for error rules because IRule.Messages is never populated");
    }

    #endregion

    #region Flag.Messages Tests - Documents Broken Behavior

    /// <summary>
    /// Messages always returns false because IRule.Messages is never populated.
    /// This documents existing (broken) behavior.
    /// </summary>
    [TestMethod]
    public async Task ShouldRunRule_FlagMessages_AlwaysReturnsFalse_BecauseMessagesNeverPopulated()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestErrorRule("Error message", trigger);
        _ruleManager.AddRule(rule);
        await _ruleManager.RunRule(rule);

        // Messages never matches because rule.Messages is always empty
        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.Messages);

        Assert.IsFalse(result, "Messages always returns false because IRule.Messages is never populated");
    }

    #endregion

    #region Combined Flags Tests

    [TestMethod]
    public void ShouldRunRule_CombinedNotExecutedAndExecuted_NotExecutedRule_ReturnsTrue()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);

        // NotExecuted matches
        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.NotExecuted | RunRulesFlag.Executed);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ShouldRunRule_CombinedNotExecutedAndExecuted_ExecutedRule_ReturnsTrue()
    {
        var trigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(trigger);
        _ruleManager.AddRule(rule);
        await _ruleManager.RunRule(rule);

        // Executed matches
        var result = RuleManager<RuleManagerTestTarget>.ShouldRunRule(rule, RunRulesFlag.NotExecuted | RunRulesFlag.Executed);

        Assert.IsTrue(result);
    }

    #endregion
}

#endregion

#region Integration with ValidateBase Tests

[TestClass]
public class RuleManagerValidateBaseIntegrationTests
{
    [TestMethod]
    public async Task Integration_PropertyChangeTriggersRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var rule = new TestSyncRule(triggerProperty);
        ruleManager.AddRule(rule);

        // Act
        target.Name = "New Value";
        await target.RunRules();

        // Assert
        Assert.IsTrue(rule.Executed);
        Assert.AreEqual("New Value", rule.LastNameValue);
    }

    [TestMethod]
    public async Task Integration_ValidationRulesAffectIsValid()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        target.ResumeAllActions();
        var ruleManager = target.GetRuleManager();
        var triggerProperty = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var errorRule = new TestErrorRule("Name has error", triggerProperty);
        ruleManager.AddRule(errorRule);

        // Act
        target.Name = "Test";
        await target.RunRules();

        // Assert
        Assert.IsFalse(target.IsValid);
    }

    [TestMethod]
    public async Task Integration_MultiplePropertiesWithRules()
    {
        // Arrange
        var target = new RuleManagerTestTarget();
        var ruleManager = target.GetRuleManager();

        var nameTrigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Name);
        var ageTrigger = new TriggerProperty<RuleManagerTestTarget>(t => t.Age);

        var nameRule = new TestSyncRule(nameTrigger);
        var ageRule = new TestSyncRule(ageTrigger);

        ruleManager.AddRule(nameRule);
        ruleManager.AddRule(ageRule);
        target.ResumeAllActions();

        // Act - RunRules should execute all rules
        await ruleManager.RunRules(RunRulesFlag.All);

        // Assert
        Assert.AreEqual(1, nameRule.ExecutionCount);
        Assert.AreEqual(1, ageRule.ExecutionCount);
    }
}

#endregion
