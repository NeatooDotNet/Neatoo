using KnockOff;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Rules;
using System.Linq.Expressions;

namespace Neatoo.UnitTest.Unit.Rules;

/// <summary>
/// A simple ValidateBase implementation for testing fluent rules.
/// Uses real Neatoo infrastructure instead of mocks.
/// </summary>
public partial class TestValidateTarget : ValidateBase<TestValidateTarget>
{
    public TestValidateTarget(IValidateBaseServices<TestValidateTarget> services) : base(services)
    {
    }

    public partial string? Name { get; set; }
    public partial string? Description { get; set; }
    public partial int Value { get; set; }
    public partial int Count { get; set; }
}

/// <summary>
/// Helper class to create TestValidateTarget instances using the DI container.
/// </summary>
public static class TestTargetFactory
{
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    private static IServiceProvider ServiceProvider
    {
        get
        {
            lock (_lock)
            {
                if (_serviceProvider == null)
                {
                    var services = new ServiceCollection();
                    services.AddNeatooServices(RemoteFactory.NeatooFactory.Server, typeof(TestValidateTarget).Assembly);
                    services.AddTransient<TestValidateTarget>();
                    _serviceProvider = services.BuildServiceProvider();
                }
                return _serviceProvider;
            }
        }
    }

    public static TestValidateTarget CreateTarget()
    {
        using var scope = ServiceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TestValidateTarget>();
    }
}

#region ActionFluentRule Tests

/// <summary>
/// Unit tests for ActionFluentRule.
/// Tests action invocation, trigger properties, and rule execution behavior.
/// </summary>
[TestClass]
[KnockOff<IRuleManager>]
public partial class ActionFluentRuleTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithActionAndTriggerProperties_CreatesRuleWithTriggers()
    {
        // Arrange
        Action<TestValidateTarget> action = t => { };
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;

        // Act
        var rule = new ActionFluentRule<TestValidateTarget>(action, triggerExpr);

        // Assert
        Assert.AreEqual(1, ((IRule)rule).TriggerProperties.Count);
        Assert.AreEqual("Name", ((IRule)rule).TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public void Constructor_WithMultipleTriggerProperties_CreatesRuleWithAllTriggers()
    {
        // Arrange
        Action<TestValidateTarget> action = t => { };
        Expression<Func<TestValidateTarget, object?>> trigger1 = t => t.Name;
        Expression<Func<TestValidateTarget, object?>> trigger2 = t => t.Description;

        // Act
        var rule = new ActionFluentRule<TestValidateTarget>(action, trigger1, trigger2);

        // Assert
        Assert.AreEqual(2, ((IRule)rule).TriggerProperties.Count);
        Assert.IsTrue(((IRule)rule).TriggerProperties.Any(t => t.PropertyName == "Name"));
        Assert.IsTrue(((IRule)rule).TriggerProperties.Any(t => t.PropertyName == "Description"));
    }

    [TestMethod]
    public void Constructor_WithNoTriggerProperties_CreatesRuleWithEmptyTriggers()
    {
        // Arrange
        Action<TestValidateTarget> action = t => { };

        // Act
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Assert
        Assert.AreEqual(0, ((IRule)rule).TriggerProperties.Count);
    }

    #endregion

    #region RunRule Tests

    [TestMethod]
    public async Task RunRule_InvokesProvidedAction()
    {
        // Arrange
        var actionInvoked = false;
        var target = TestTargetFactory.CreateTarget();
        Action<TestValidateTarget> action = t => { actionInvoked = true; };
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(actionInvoked, "Action should have been invoked");
    }

    [TestMethod]
    public async Task RunRule_PassesTargetToAction()
    {
        // Arrange
        TestValidateTarget? capturedTarget = null;
        var target = TestTargetFactory.CreateTarget();
        target.Name = "TestName";
        Action<TestValidateTarget> action = t => { capturedTarget = t; };
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.AreSame(target, capturedTarget);
        Assert.AreEqual("TestName", capturedTarget?.Name);
    }

    [TestMethod]
    public async Task RunRule_ReturnsRuleMessagesNone()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Action<TestValidateTarget> action = t => { };
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_AllowsActionToModifyTarget()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.Value = 10;
        Action<TestValidateTarget> action = t => { t.Value = 42; };
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.AreEqual(42, target.Value);
    }

    [TestMethod]
    public async Task RunRule_SetsExecutedToTrue()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Action<TestValidateTarget> action = t => { };
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Assert initial state
        Assert.IsFalse(rule.Executed);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(rule.Executed);
    }

    [TestMethod]
    public async Task RunRule_WithIValidateBaseInterface_CastsAndExecutes()
    {
        // Arrange
        var actionInvoked = false;
        var target = TestTargetFactory.CreateTarget();
        Action<TestValidateTarget> action = t => { actionInvoked = true; };
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Act - calling through IValidateBase interface
        await ((IRule)rule).RunRule((IValidateBase)target);

        // Assert
        Assert.IsTrue(actionInvoked);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidTargetTypeException))]
    public async Task RunRule_WithInvalidTargetType_ThrowsException()
    {
        // Arrange
        Action<TestValidateTarget> action = t => { };
        var rule = new ActionFluentRule<TestValidateTarget>(action);
        var differentTargetStub = new RuleProxyTests.Stubs.IValidateBase();

        // Act - should throw because stub is not TestValidateTarget
        await ((IRule)rule).RunRule(differentTargetStub);
    }

    #endregion

    #region RuleId and OnRuleAdded Tests

    [TestMethod]
    public void OnRuleAdded_SetsRuleId()
    {
        // Arrange
        var rule = new ActionFluentRule<TestValidateTarget>(t => { });
        var ruleManagerStub = new Stubs.IRuleManager();

        // Act
        rule.OnRuleAdded(ruleManagerStub, 42u);

        // Assert
        Assert.AreEqual(42u, rule.RuleId);
    }

    [TestMethod]
    public void OnRuleAdded_CalledMultipleTimes_KeepsFirstIndex()
    {
        // Arrange
        var rule = new ActionFluentRule<TestValidateTarget>(t => { });
        var ruleManagerStub = new Stubs.IRuleManager();

        // Act
        rule.OnRuleAdded(ruleManagerStub, 10u);
        rule.OnRuleAdded(ruleManagerStub, 20u);

        // Assert - first index should be retained
        Assert.AreEqual(10u, rule.RuleId);
    }

    [TestMethod]
    public void RuleId_DefaultsToZero()
    {
        // Arrange & Act
        var rule = new ActionFluentRule<TestValidateTarget>(t => { });

        // Assert
        Assert.AreEqual(0u, rule.RuleId);
    }

    #endregion

    #region RuleOrder Tests

    [TestMethod]
    public void RuleOrder_DefaultsToOne()
    {
        // Arrange & Act
        var rule = new ActionFluentRule<TestValidateTarget>(t => { });

        // Assert
        Assert.AreEqual(1, rule.RuleOrder);
    }

    #endregion

    #region Messages Tests

    [TestMethod]
    public void Messages_BeforeExecution_ReturnsEmptyList()
    {
        // Arrange
        var rule = new ActionFluentRule<TestValidateTarget>(t => { });

        // Act
        var messages = rule.Messages;

        // Assert
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, messages.Count);
    }

    #endregion
}

#endregion

#region ValidationFluentRule Tests

/// <summary>
/// Unit tests for ValidationFluentRule.
/// Tests validation logic, error message creation, and trigger property handling.
/// </summary>
[TestClass]
public class ValidationFluentRuleTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithFuncAndTriggerProperty_CreatesRuleWithSingleTrigger()
    {
        // Arrange
        Func<TestValidateTarget, string> validationFunc = t => string.Empty;
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;

        // Act
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Assert
        Assert.AreEqual(1, ((IRule)rule).TriggerProperties.Count);
        Assert.AreEqual("Name", ((IRule)rule).TriggerProperties[0].PropertyName);
    }

    #endregion

    #region RunRule - No Error Tests

    [TestMethod]
    public async Task RunRule_ReturnsEmptyString_ReturnsNoMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, string> validationFunc = t => string.Empty;
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ReturnsNull_ReturnsNoMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, string> validationFunc = t => null!;
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ReturnsWhitespace_ReturnsNoMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, string> validationFunc = t => "   ";
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region RunRule - Error Tests

    [TestMethod]
    public async Task RunRule_ReturnsErrorString_CreatesRuleMessage()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, string> validationFunc = t => "Name is required";
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Name", result[0].PropertyName);
        Assert.AreEqual("Name is required", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_ValidationLogicBasedOnTarget_ReturnsAppropriateResult()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.Name = "";
        Func<TestValidateTarget, string> validationFunc = t =>
            string.IsNullOrEmpty(t.Name) ? "Name cannot be empty" : string.Empty;
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Name cannot be empty", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_ValidationPasses_ReturnsEmptyMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.Name = "ValidName";
        Func<TestValidateTarget, string> validationFunc = t =>
            string.IsNullOrEmpty(t.Name) ? "Name cannot be empty" : string.Empty;
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ErrorMessageUsedDirectly_PreservesMessage()
    {
        // Arrange
        const string errorMessage = "Field must be at least 5 characters long";
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, string> validationFunc = t => errorMessage;
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Description;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, triggerExpr);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(errorMessage, result[0].Message);
        Assert.AreEqual("Description", result[0].PropertyName);
    }

    #endregion

    #region Executed Flag Tests

    [TestMethod]
    public async Task RunRule_SetsExecutedToTrue()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        var rule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);

        // Assert initial state
        Assert.IsFalse(rule.Executed);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(rule.Executed);
    }

    #endregion

    #region RuleId Tests

    [TestMethod]
    public void OnRuleAdded_SetsRuleId()
    {
        // Arrange
        var rule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var ruleManagerStub = new ActionFluentRuleTests.Stubs.IRuleManager();

        // Act
        rule.OnRuleAdded(ruleManagerStub, 100u);

        // Assert
        Assert.AreEqual(100u, rule.RuleId);
    }

    #endregion

    #region TriggerProperties Tests

    [TestMethod]
    public void TriggerProperties_ReturnsCorrectPropertyName()
    {
        // Arrange
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Value;
        var rule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, triggerExpr);

        // Act
        var triggerProperties = ((IRule)rule).TriggerProperties;

        // Assert
        Assert.AreEqual(1, triggerProperties.Count);
        Assert.AreEqual("Value", triggerProperties[0].PropertyName);
    }

    #endregion
}

#endregion

#region AsyncFluentRule Tests

/// <summary>
/// Unit tests for AsyncFluentRule.
/// Tests async validation execution, awaiting behavior, and error handling.
/// </summary>
[TestClass]
public class AsyncFluentRuleTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithAsyncFuncAndTriggerProperty_CreatesRule()
    {
        // Arrange
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.Delay(1);
            return string.Empty;
        };
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;

        // Act
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, triggerExpr);

        // Assert
        Assert.AreEqual(1, ((IRule)rule).TriggerProperties.Count);
        Assert.AreEqual("Name", ((IRule)rule).TriggerProperties[0].PropertyName);
    }

    #endregion

    #region RunRule - Async Behavior Tests

    [TestMethod]
    public async Task RunRule_AwaitsAsyncFunction()
    {
        // Arrange
        var asyncExecutionCompleted = false;
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.Delay(10);
            asyncExecutionCompleted = true;
            return string.Empty;
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(asyncExecutionCompleted, "Async function should have been awaited");
    }

    [TestMethod]
    public async Task RunRule_ReturnsAfterAsyncCompletion()
    {
        // Arrange
        var callOrder = new List<string>();
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            callOrder.Add("async-start");
            await Task.Delay(10);
            callOrder.Add("async-end");
            return string.Empty;
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        callOrder.Add("before");
        await rule.RunRule(target);
        callOrder.Add("after");

        // Assert
        Assert.AreEqual(4, callOrder.Count);
        Assert.AreEqual("before", callOrder[0]);
        Assert.AreEqual("async-start", callOrder[1]);
        Assert.AreEqual("async-end", callOrder[2]);
        Assert.AreEqual("after", callOrder[3]);
    }

    #endregion

    #region RunRule - No Error Tests

    [TestMethod]
    public async Task RunRule_AsyncReturnsEmptyString_ReturnsNoMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.CompletedTask;
            return string.Empty;
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_AsyncReturnsNull_ReturnsNoMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.CompletedTask;
            return null!;
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_AsyncReturnsWhitespace_ReturnsNoMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.CompletedTask;
            return "   \t\n   ";
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region RunRule - Error Tests

    [TestMethod]
    public async Task RunRule_AsyncReturnsErrorString_CreatesRuleMessage()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.Delay(1);
            return "Async validation failed";
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Name", result[0].PropertyName);
        Assert.AreEqual("Async validation failed", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_AsyncValidationWithExternalService_ReturnsCorrectResult()
    {
        // Arrange - simulating an external validation service call
        var target = TestTargetFactory.CreateTarget();
        target.Name = "duplicate";
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            // Simulate external service call
            await Task.Delay(5);
            return t.Name == "duplicate" ? "Name already exists in the system" : string.Empty;
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Name already exists in the system", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_AsyncValidationPasses_ReturnsEmptyMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.Name = "unique";
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.Delay(1);
            return t.Name == "duplicate" ? "Name already exists" : string.Empty;
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region Executed Flag Tests

    [TestMethod]
    public async Task RunRule_SetsExecutedToTrue()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        var rule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; },
            t => t.Name);

        // Assert initial state
        Assert.IsFalse(rule.Executed);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(rule.Executed);
    }

    #endregion

    #region RuleId Tests

    [TestMethod]
    public void OnRuleAdded_SetsRuleId()
    {
        // Arrange
        var rule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; },
            t => t.Name);
        var ruleManagerStub = new ActionFluentRuleTests.Stubs.IRuleManager();

        // Act
        rule.OnRuleAdded(ruleManagerStub, 200u);

        // Assert
        Assert.AreEqual(200u, rule.RuleId);
    }

    #endregion

    #region Cancellation Token Tests

    [TestMethod]
    public async Task RunRule_WithCancellationToken_PassesTokenToExecution()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        var cts = new CancellationTokenSource();
        var rule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.Delay(1); return string.Empty; },
            t => t.Name);

        // Act - token is passed but not used in this simple test
        var result = await rule.RunRule(target, cts.Token);

        // Assert
        Assert.IsNotNull(result);
    }

    #endregion
}

#endregion

#region ActionAsyncFluentRule Tests

/// <summary>
/// Unit tests for ActionAsyncFluentRule.
/// Tests async action execution without return values.
/// </summary>
[TestClass]
public class ActionAsyncFluentRuleTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithAsyncActionAndTriggerProperties_CreatesRule()
    {
        // Arrange
        Func<TestValidateTarget, Task> asyncAction = async t => await Task.Delay(1);
        Expression<Func<TestValidateTarget, object?>> trigger1 = t => t.Name;
        Expression<Func<TestValidateTarget, object?>> trigger2 = t => t.Value;

        // Act
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction, trigger1, trigger2);

        // Assert
        Assert.AreEqual(2, ((IRule)rule).TriggerProperties.Count);
    }

    [TestMethod]
    public void Constructor_WithNoTriggerProperties_CreatesRuleWithEmptyTriggers()
    {
        // Arrange
        Func<TestValidateTarget, Task> asyncAction = async t => await Task.CompletedTask;

        // Act
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Assert
        Assert.AreEqual(0, ((IRule)rule).TriggerProperties.Count);
    }

    #endregion

    #region RunRule - Async Behavior Tests

    [TestMethod]
    public async Task RunRule_AwaitsAsyncAction()
    {
        // Arrange
        var asyncExecutionCompleted = false;
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task> asyncAction = async t =>
        {
            await Task.Delay(10);
            asyncExecutionCompleted = true;
        };
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(asyncExecutionCompleted, "Async action should have been awaited");
    }

    [TestMethod]
    public async Task RunRule_ExecutesInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task> asyncAction = async t =>
        {
            callOrder.Add("async-start");
            await Task.Delay(5);
            callOrder.Add("async-end");
        };
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Act
        callOrder.Add("before");
        await rule.RunRule(target);
        callOrder.Add("after");

        // Assert
        Assert.AreEqual(4, callOrder.Count);
        Assert.AreEqual("before", callOrder[0]);
        Assert.AreEqual("async-start", callOrder[1]);
        Assert.AreEqual("async-end", callOrder[2]);
        Assert.AreEqual("after", callOrder[3]);
    }

    [TestMethod]
    public async Task RunRule_AlwaysReturnsRuleMessagesNone()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task> asyncAction = async t =>
        {
            await Task.Delay(1);
            t.Value = 999;
        };
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_PassesTargetToAction()
    {
        // Arrange
        TestValidateTarget? capturedTarget = null;
        var target = TestTargetFactory.CreateTarget();
        target.Name = "AsyncTest";
        Func<TestValidateTarget, Task> asyncAction = async t =>
        {
            await Task.CompletedTask;
            capturedTarget = t;
        };
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.AreSame(target, capturedTarget);
    }

    [TestMethod]
    public async Task RunRule_AllowsAsyncModificationOfTarget()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.Value = 0;
        Func<TestValidateTarget, Task> asyncAction = async t =>
        {
            await Task.Delay(1);
            t.Value = 100;
        };
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.AreEqual(100, target.Value);
    }

    #endregion

    #region Executed Flag Tests

    [TestMethod]
    public async Task RunRule_SetsExecutedToTrue()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert initial state
        Assert.IsFalse(rule.Executed);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(rule.Executed);
    }

    #endregion

    #region RuleId Tests

    [TestMethod]
    public void OnRuleAdded_SetsRuleId()
    {
        // Arrange
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);
        var ruleManagerStub = new ActionFluentRuleTests.Stubs.IRuleManager();

        // Act
        rule.OnRuleAdded(ruleManagerStub, 300u);

        // Assert
        Assert.AreEqual(300u, rule.RuleId);
    }

    [TestMethod]
    public void OnRuleAdded_DoesNotOverwriteExistingIndex()
    {
        // Arrange
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);
        var ruleManagerStub = new ActionFluentRuleTests.Stubs.IRuleManager();

        // Act
        rule.OnRuleAdded(ruleManagerStub, 50u);
        rule.OnRuleAdded(ruleManagerStub, 100u);

        // Assert
        Assert.AreEqual(50u, rule.RuleId);
    }

    #endregion

    #region TriggerProperties Tests

    [TestMethod]
    public void TriggerProperties_ReturnsTriggerPropertyList()
    {
        // Arrange
        Func<TestValidateTarget, Task> asyncAction = async t => await Task.CompletedTask;
        Expression<Func<TestValidateTarget, object?>> trigger1 = t => t.Name;
        Expression<Func<TestValidateTarget, object?>> trigger2 = t => t.Description;
        Expression<Func<TestValidateTarget, object?>> trigger3 = t => t.Value;
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction, trigger1, trigger2, trigger3);

        // Act
        var triggers = ((IRule)rule).TriggerProperties;

        // Assert
        Assert.AreEqual(3, triggers.Count);
        Assert.IsTrue(triggers.Any(t => t.PropertyName == "Name"));
        Assert.IsTrue(triggers.Any(t => t.PropertyName == "Description"));
        Assert.IsTrue(triggers.Any(t => t.PropertyName == "Value"));
    }

    [TestMethod]
    public void TriggerProperties_IsMatchWorksCorrectly()
    {
        // Arrange
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask,
            t => t.Name);

        // Act & Assert
        Assert.IsTrue(((IRule)rule).TriggerProperties[0].IsMatch("Name"));
        Assert.IsFalse(((IRule)rule).TriggerProperties[0].IsMatch("Description"));
    }

    #endregion

    #region IRule Interface Tests

    [TestMethod]
    public async Task RunRule_ThroughIRuleInterface_Works()
    {
        // Arrange
        var actionInvoked = false;
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task> asyncAction = async t =>
        {
            await Task.CompletedTask;
            actionInvoked = true;
        };
        IRule rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Act
        await rule.RunRule(target);

        // Assert
        Assert.IsTrue(actionInvoked);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidTargetTypeException))]
    public async Task RunRule_WithInvalidTargetType_ThrowsException()
    {
        // Arrange
        Func<TestValidateTarget, Task> asyncAction = async t => await Task.CompletedTask;
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);
        var differentTargetStub = new RuleProxyTests.Stubs.IValidateBase();

        // Act
        await ((IRule)rule).RunRule(differentTargetStub);
    }

    #endregion

    #region Messages Tests

    [TestMethod]
    public void Messages_ReturnsEmptyCollection()
    {
        // Arrange
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Act
        var messages = rule.Messages;

        // Assert
        Assert.IsNotNull(messages);
        Assert.AreEqual(0, messages.Count);
    }

    #endregion

    #region RuleOrder Tests

    [TestMethod]
    public void RuleOrder_DefaultsToOne()
    {
        // Arrange & Act
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert
        Assert.AreEqual(1, rule.RuleOrder);
    }

    #endregion
}

#endregion

#region Cross-Cutting Fluent Rule Tests

/// <summary>
/// Tests that verify behaviors common across all fluent rule types.
/// </summary>
[TestClass]
public class FluentRuleCrossCuttingTests
{
    #region IRule Interface Implementation Tests

    [TestMethod]
    public void AllFluentRules_ImplementIRule()
    {
        // Arrange & Act
        IRule actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        IRule validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        IRule asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        IRule actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert - all implement IRule
        Assert.IsInstanceOfType(actionRule, typeof(IRule));
        Assert.IsInstanceOfType(validationRule, typeof(IRule));
        Assert.IsInstanceOfType(asyncRule, typeof(IRule));
        Assert.IsInstanceOfType(actionAsyncRule, typeof(IRule));
    }

    [TestMethod]
    public void AllFluentRules_ImplementIRuleOfT()
    {
        // Arrange & Act
        IRule<TestValidateTarget> actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        IRule<TestValidateTarget> validationRule = new ValidationFluentRule<TestValidateTarget>(
            t => string.Empty, t => t.Name);
        IRule<TestValidateTarget> asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        IRule<TestValidateTarget> actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert
        Assert.IsInstanceOfType(actionRule, typeof(IRule<TestValidateTarget>));
        Assert.IsInstanceOfType(validationRule, typeof(IRule<TestValidateTarget>));
        Assert.IsInstanceOfType(asyncRule, typeof(IRule<TestValidateTarget>));
        Assert.IsInstanceOfType(actionAsyncRule, typeof(IRule<TestValidateTarget>));
    }

    #endregion

    #region Default State Tests

    [TestMethod]
    public void AllFluentRules_ExecutedDefaultsToFalse()
    {
        // Arrange & Act
        var actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        var validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        var actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert
        Assert.IsFalse(actionRule.Executed);
        Assert.IsFalse(validationRule.Executed);
        Assert.IsFalse(asyncRule.Executed);
        Assert.IsFalse(actionAsyncRule.Executed);
    }

    [TestMethod]
    public void AllFluentRules_RuleIdDefaultsToZero()
    {
        // Arrange & Act
        var actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        var validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        var actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert
        Assert.AreEqual(0u, actionRule.RuleId);
        Assert.AreEqual(0u, validationRule.RuleId);
        Assert.AreEqual(0u, asyncRule.RuleId);
        Assert.AreEqual(0u, actionAsyncRule.RuleId);
    }

    [TestMethod]
    public void AllFluentRules_RuleOrderDefaultsToOne()
    {
        // Arrange & Act
        var actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        var validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        var actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert
        Assert.AreEqual(1, actionRule.RuleOrder);
        Assert.AreEqual(1, validationRule.RuleOrder);
        Assert.AreEqual(1, asyncRule.RuleOrder);
        Assert.AreEqual(1, actionAsyncRule.RuleOrder);
    }

    [TestMethod]
    public void AllFluentRules_MessagesDefaultsToEmptyCollection()
    {
        // Arrange & Act
        var actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        var validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        var actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Assert
        Assert.AreEqual(0, actionRule.Messages.Count);
        Assert.AreEqual(0, validationRule.Messages.Count);
        Assert.AreEqual(0, asyncRule.Messages.Count);
        Assert.AreEqual(0, actionAsyncRule.Messages.Count);
    }

    #endregion

    #region Executed After RunRule Tests

    [TestMethod]
    public async Task AllFluentRules_ExecutedIsTrueAfterRunRule()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        var actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        var validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        var actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Act
        await actionRule.RunRule(target);
        await validationRule.RunRule(target);
        await asyncRule.RunRule(target);
        await actionAsyncRule.RunRule(target);

        // Assert
        Assert.IsTrue(actionRule.Executed);
        Assert.IsTrue(validationRule.Executed);
        Assert.IsTrue(asyncRule.Executed);
        Assert.IsTrue(actionAsyncRule.Executed);
    }

    #endregion

    #region Trigger Properties Match Tests

    [TestMethod]
    public void TriggerProperties_AllRuleTypes_SupportIsMatch()
    {
        // Arrange
        var actionRule = new ActionFluentRule<TestValidateTarget>(t => { }, t => t.Name);
        var validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        var actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask, t => t.Name);

        // Act & Assert
        Assert.IsTrue(((IRule)actionRule).TriggerProperties[0].IsMatch("Name"));
        Assert.IsTrue(((IRule)validationRule).TriggerProperties[0].IsMatch("Name"));
        Assert.IsTrue(((IRule)asyncRule).TriggerProperties[0].IsMatch("Name"));
        Assert.IsTrue(((IRule)actionAsyncRule).TriggerProperties[0].IsMatch("Name"));
    }

    #endregion

    #region OnRuleAdded Idempotency Tests

    [TestMethod]
    public void OnRuleAdded_AllRuleTypes_PreservesFirstRuleId()
    {
        // Arrange
        var ruleManagerStub = new ActionFluentRuleTests.Stubs.IRuleManager();
        var actionRule = new ActionFluentRule<TestValidateTarget>(t => { });
        var validationRule = new ValidationFluentRule<TestValidateTarget>(t => string.Empty, t => t.Name);
        var asyncRule = new AsyncFluentRule<TestValidateTarget>(
            async t => { await Task.CompletedTask; return string.Empty; }, t => t.Name);
        var actionAsyncRule = new ActionAsyncFluentRule<TestValidateTarget>(
            async t => await Task.CompletedTask);

        // Act - call OnRuleAdded twice with different values
        actionRule.OnRuleAdded(ruleManagerStub, 1u);
        actionRule.OnRuleAdded(ruleManagerStub, 999u);

        validationRule.OnRuleAdded(ruleManagerStub, 2u);
        validationRule.OnRuleAdded(ruleManagerStub, 999u);

        asyncRule.OnRuleAdded(ruleManagerStub, 3u);
        asyncRule.OnRuleAdded(ruleManagerStub, 999u);

        actionAsyncRule.OnRuleAdded(ruleManagerStub, 4u);
        actionAsyncRule.OnRuleAdded(ruleManagerStub, 999u);

        // Assert - first value is preserved
        Assert.AreEqual(1u, actionRule.RuleId);
        Assert.AreEqual(2u, validationRule.RuleId);
        Assert.AreEqual(3u, asyncRule.RuleId);
        Assert.AreEqual(4u, actionAsyncRule.RuleId);
    }

    #endregion
}

#endregion

#region Lambda Behavior Tests

/// <summary>
/// Tests that verify lambda execution behavior for various scenarios.
/// </summary>
[TestClass]
public class FluentRuleLambdaBehaviorTests
{
    #region Lambda with External State Tests

    [TestMethod]
    public async Task ActionFluentRule_LambdaCanCaptureExternalState()
    {
        // Arrange
        var externalCounter = 0;
        var target = TestTargetFactory.CreateTarget();
        Action<TestValidateTarget> action = t => { externalCounter++; };
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Act
        await rule.RunRule(target);
        await rule.RunRule(target);
        await rule.RunRule(target);

        // Assert
        Assert.AreEqual(3, externalCounter);
    }

    [TestMethod]
    public async Task ValidationFluentRule_LambdaCanUseExternalValidationLogic()
    {
        // Arrange
        var minLength = 5;
        var target = TestTargetFactory.CreateTarget();
        target.Name = "Hi";
        Func<TestValidateTarget, string> validationFunc = t =>
            t.Name?.Length < minLength ? $"Name must be at least {minLength} characters" : string.Empty;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Name must be at least 5 characters", result[0].Message);
    }

    [TestMethod]
    public async Task AsyncFluentRule_LambdaCanUseAsyncExternalServices()
    {
        // Arrange
        // Simulate an async external service
        Func<string?, Task<bool>> externalAsyncValidator = async name =>
        {
            await Task.Delay(5);
            return name?.Length >= 3;
        };

        var target = TestTargetFactory.CreateTarget();
        target.Name = "AB";
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            var isValid = await externalAsyncValidator(t.Name);
            return isValid ? string.Empty : "Name is too short";
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Name is too short", result[0].Message);
    }

    #endregion

    #region Lambda Exception Handling Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task ActionFluentRule_LambdaThrows_ExceptionPropagates()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Action<TestValidateTarget> action = t => throw new InvalidOperationException("Test exception");
        var rule = new ActionFluentRule<TestValidateTarget>(action);

        // Act
        await rule.RunRule(target);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task ValidationFluentRule_LambdaThrows_ExceptionPropagates()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, string> validationFunc = t =>
            throw new InvalidOperationException("Validation exception");
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, t => t.Name);

        // Act
        await rule.RunRule(target);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task AsyncFluentRule_LambdaThrows_ExceptionPropagates()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async exception");
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        await rule.RunRule(target);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task ActionAsyncFluentRule_LambdaThrows_ExceptionPropagates()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task> asyncAction = async t =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async action exception");
        };
        var rule = new ActionAsyncFluentRule<TestValidateTarget>(asyncAction);

        // Act
        await rule.RunRule(target);
    }

    #endregion

    #region Lambda Multiple Executions Tests

    [TestMethod]
    public async Task ValidationFluentRule_MultipleExecutions_ReturnsCorrectResultEachTime()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, string> validationFunc = t =>
            string.IsNullOrEmpty(t.Name) ? "Name required" : string.Empty;
        var rule = new ValidationFluentRule<TestValidateTarget>(validationFunc, t => t.Name);

        // Act - first execution with empty name
        target.Name = "";
        var result1 = await rule.RunRule(target);

        // Act - second execution with valid name
        target.Name = "Valid";
        var result2 = await rule.RunRule(target);

        // Act - third execution with empty again
        target.Name = null;
        var result3 = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result1.Count);
        Assert.AreEqual(0, result2.Count);
        Assert.AreEqual(1, result3.Count);
    }

    [TestMethod]
    public async Task AsyncFluentRule_MultipleExecutions_ReturnsCorrectResultEachTime()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        Func<TestValidateTarget, Task<string>> asyncFunc = async t =>
        {
            await Task.Delay(1);
            return t.Value < 0 ? "Value must be non-negative" : string.Empty;
        };
        var rule = new AsyncFluentRule<TestValidateTarget>(asyncFunc, t => t.Value);

        // Act
        target.Value = -5;
        var result1 = await rule.RunRule(target);

        target.Value = 10;
        var result2 = await rule.RunRule(target);

        target.Value = -1;
        var result3 = await rule.RunRule(target);

        // Assert
        Assert.AreEqual(1, result1.Count);
        Assert.AreEqual(0, result2.Count);
        Assert.AreEqual(1, result3.Count);
    }

    #endregion
}

#endregion

#region ActionAsyncFluentRuleWithToken Tests

/// <summary>
/// Unit tests for ActionAsyncFluentRuleWithToken - async actions that receive CancellationToken.
/// </summary>
[TestClass]
public class ActionAsyncFluentRuleWithTokenTests
{
    [TestMethod]
    public void Constructor_WithAsyncFuncAndTriggerProperties_CreatesRuleWithTriggers()
    {
        // Arrange
        Func<TestValidateTarget, CancellationToken, Task> asyncFunc = async (t, token) => await Task.CompletedTask;
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;

        // Act
        var rule = new ActionAsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, triggerExpr);

        // Assert
        Assert.AreEqual(1, ((IRule)rule).TriggerProperties.Count);
        Assert.AreEqual("Name", ((IRule)rule).TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_WithNullToken_ExecutesAction()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        var wasExecuted = false;
        Func<TestValidateTarget, CancellationToken, Task> asyncFunc = async (t, token) =>
        {
            await Task.Delay(1);
            wasExecuted = true;
        };
        var rule = new ActionAsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        await rule.RunRule(target, null);

        // Assert
        Assert.IsTrue(wasExecuted);
    }

    [TestMethod]
    public async Task RunRule_WithValidToken_PassesTokenToAction()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        CancellationToken? receivedToken = null;
        Func<TestValidateTarget, CancellationToken, Task> asyncFunc = async (t, token) =>
        {
            receivedToken = token;
            await Task.CompletedTask;
        };
        var rule = new ActionAsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        using var cts = new CancellationTokenSource();

        // Act
        await rule.RunRule(target, cts.Token);

        // Assert
        Assert.IsNotNull(receivedToken);
        Assert.AreEqual(cts.Token, receivedToken.Value);
    }

    [TestMethod]
    public async Task RunRule_WithCancelledToken_ThrowsBeforeExecution()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        var wasExecuted = false;
        Func<TestValidateTarget, CancellationToken, Task> asyncFunc = async (t, token) =>
        {
            wasExecuted = true;
            await Task.CompletedTask;
        };
        var rule = new ActionAsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await rule.RunRule(target, cts.Token));

        Assert.IsFalse(wasExecuted);
    }

    [TestMethod]
    public async Task RunRule_ActionCanCheckCancellation_CanCancelMidExecution()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        var readyToCancel = new TaskCompletionSource();
        var proceedToCheck = new TaskCompletionSource();

        Func<TestValidateTarget, CancellationToken, Task> asyncFunc = async (t, token) =>
        {
            readyToCancel.SetResult(); // Signal we're ready for cancellation
            await proceedToCheck.Task; // Wait until cancellation is requested
            token.ThrowIfCancellationRequested();
            await Task.Delay(1000); // Would take too long if not cancelled
        };
        var rule = new ActionAsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        using var cts = new CancellationTokenSource();

        // Act - Start the task, wait for it to be ready, then cancel
        var task = rule.RunRule(target, cts.Token);
        await readyToCancel.Task; // Ensure task is at the checkpoint
        cts.Cancel(); // Cancel the token
        proceedToCheck.SetResult(); // Let the task proceed to check the token

        // Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await task);
    }
}

#endregion

#region AsyncFluentRuleWithToken Tests

/// <summary>
/// Unit tests for AsyncFluentRuleWithToken - validation rules that receive CancellationToken.
/// </summary>
[TestClass]
public class AsyncFluentRuleWithTokenTests
{
    [TestMethod]
    public void Constructor_WithAsyncFuncAndTriggerProperty_CreatesRuleWithTrigger()
    {
        // Arrange
        Func<TestValidateTarget, CancellationToken, Task<string>> asyncFunc = async (t, token) => await Task.FromResult("");
        Expression<Func<TestValidateTarget, object?>> triggerExpr = t => t.Name;

        // Act
        var rule = new AsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, triggerExpr);

        // Assert
        Assert.AreEqual(1, ((IRule)rule).TriggerProperties.Count);
        Assert.AreEqual("Name", ((IRule)rule).TriggerProperties[0].PropertyName);
    }

    [TestMethod]
    public async Task RunRule_ReturnsNoError_HasEmptyMessages()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        Func<TestValidateTarget, CancellationToken, Task<string>> asyncFunc = async (t, token) => await Task.FromResult("");
        var rule = new AsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target, null);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task RunRule_ReturnsError_HasErrorMessage()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        Func<TestValidateTarget, CancellationToken, Task<string>> asyncFunc = async (t, token) =>
            await Task.FromResult("Validation failed");
        var rule = new AsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        // Act
        var result = await rule.RunRule(target, null);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Validation failed", result[0].Message);
    }

    [TestMethod]
    public async Task RunRule_WithValidToken_PassesTokenToFunc()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        CancellationToken? receivedToken = null;
        Func<TestValidateTarget, CancellationToken, Task<string>> asyncFunc = async (t, token) =>
        {
            receivedToken = token;
            return await Task.FromResult("");
        };
        var rule = new AsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        using var cts = new CancellationTokenSource();

        // Act
        await rule.RunRule(target, cts.Token);

        // Assert
        Assert.IsNotNull(receivedToken);
        Assert.AreEqual(cts.Token, receivedToken.Value);
    }

    [TestMethod]
    public async Task RunRule_WithCancelledToken_ThrowsBeforeExecution()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        var wasExecuted = false;
        Func<TestValidateTarget, CancellationToken, Task<string>> asyncFunc = async (t, token) =>
        {
            wasExecuted = true;
            return await Task.FromResult("");
        };
        var rule = new AsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
            await rule.RunRule(target, cts.Token));

        Assert.IsFalse(wasExecuted);
    }

    [TestMethod]
    public async Task RunRule_FuncCanCheckCancellation_CanCancelMidExecution()
    {
        // Arrange
        var target = TestTargetFactory.CreateTarget();
        target.ResumeAllActions();

        var readyToCancel = new TaskCompletionSource<bool>();
        var cancellationChecked = new TaskCompletionSource<bool>();

        Func<TestValidateTarget, CancellationToken, Task<string>> asyncFunc = async (t, token) =>
        {
            // Signal that we're ready for cancellation
            readyToCancel.SetResult(true);
            // Wait for cancellation to be triggered
            await cancellationChecked.Task;
            token.ThrowIfCancellationRequested();
            await Task.Delay(1000); // Would take too long if not cancelled
            return "";
        };
        var rule = new AsyncFluentRuleWithToken<TestValidateTarget>(asyncFunc, t => t.Name);

        using var cts = new CancellationTokenSource();

        // Act - Start the task
        var task = rule.RunRule(target, cts.Token);
        // Wait until the function signals it's ready for cancellation
        await readyToCancel.Task;
        // Cancel the token
        cts.Cancel();
        // Signal the function to check the cancellation
        cancellationChecked.SetResult(true);

        // Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await task);
    }
}

#endregion
