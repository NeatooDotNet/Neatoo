using Neatoo.Core;
using Neatoo.Rules.Rules;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Neatoo.Rules;

public interface IRuleManager
{
    IEnumerable<IRule> Rules { get; }
    Task CheckRulesForProperty(string propertyName);
    Task CheckAllRules(CancellationToken? token = null);
    void AddRule<T>(IRule<T> rule) where T : IValidateBase;
    void AddRules<T>(params IRule<T>[] rules) where T : IValidateBase;
    Task RunRule(IRule r, CancellationToken? token = null);
    Task RunRule<T>(CancellationToken? token = null) where T : IRule;
}


public interface IRuleManager<T> : IRuleManager
    where T : class, IValidateBase
{
    ActionFluentRule<T> AddAction(Action<T> func, params Expression<Func<T, object?>>[] triggerProperties);
    ValidationFluentRule<T> AddValidation(Func<T, string> func, Expression<Func<T, object?>> triggerProperty);
    ActionAsyncFluentRule<T> AddActionAsync(Func<T, Task> func, params Expression<Func<T, object?>>[] triggerProperties);
    AsyncFluentRule<T> AddValidationAsync(Func<T, Task<string>> func, Expression<Func<T, object?>> triggerProperty);
}

public class RuleManagerFactory<T>
    where T : class, IValidateBase
{
    public RuleManagerFactory(IAttributeToRule attributeToRule)
    {
        AttributeToRule = attributeToRule;
    }

    public IAttributeToRule AttributeToRule { get; }

    public IRuleManager<T> CreateRuleManager(T target, IPropertyInfoList propertyInfoList)
    {
        return new RuleManager<T>(target, propertyInfoList, AttributeToRule);
    }
}

public class RuleManager<T> : IRuleManager<T>
    where T : class, IValidateBase
{
    protected T Target { get; }

    public RuleManager(T target, IPropertyInfoList propertyInfoList, IAttributeToRule attributeToRule)
    {
        this.Target = target ?? throw new TargetIsNullException();
        AddAttributeRules(attributeToRule, propertyInfoList);
    }

    IEnumerable<IRule> IRuleManager.Rules => Rules.Values;

    // Index the rules for serialization
    // So broken rules transfer correctly
    // This does assume that the rules are added in the same order (hmmm)
    protected uint ruleIndex = 1;

    private IDictionary<uint, IRule> Rules { get; } = new ConcurrentDictionary<uint, IRule>();

    protected virtual void AddAttributeRules(IAttributeToRule attributeToRule, IPropertyInfoList propertyInfoList)
    {
        var requiredRegisteredProp = propertyInfoList.Properties();

        foreach (var r in requiredRegisteredProp)
        {
            foreach (var a in r.PropertyInfo.GetCustomAttributes(true))
            {
                var rule = attributeToRule.GetRule<T>(r, a.GetType());
                if (rule != null) {
                    Rules.Add(ruleIndex++, rule);
                    rule.OnRuleAdded(this, ruleIndex);
                }
            }
        }
    }

    public void AddRules<T1>(params IRule<T1>[] rules) where T1: IValidateBase
    {
            foreach (var r in rules) { AddRule(r); }
    }

    public void AddRule<T1>(IRule<T1> rule) where T1 : IValidateBase
    {
        if (typeof(T1).IsAssignableFrom(typeof(T)))
        {
            Rules.Add(ruleIndex++, rule);
            rule.OnRuleAdded(this, ruleIndex);
        }
        else
        {
            throw new InvalidTargetTypeException($"{typeof(T1).FullName} is not assignable from {typeof(T).FullName}");
        }
    }

    public async Task RunRule<T1>(CancellationToken? token = null) where T1 : IRule
    {
        foreach (var rule in Rules)
        {
            if(rule.Value is T1 r)
            {
                await rule.Value.RunRule(Target, token);
            }
        }
    }

    public ActionAsyncFluentRule<T> AddActionAsync(Func<T, Task> func, params Expression<Func<T, object?>>[] triggerProperties)
    {
        ActionAsyncFluentRule<T> rule = new ActionAsyncFluentRule<T>(func, triggerProperties);
        Rules.Add(ruleIndex++, rule);
        rule.OnRuleAdded(this, ruleIndex);
        return rule;
    }

    public ActionFluentRule<T> AddAction(Action<T> func, params Expression<Func<T, object?>>[] triggerProperties)
    {
        ActionFluentRule<T> rule = new ActionFluentRule<T>(func, triggerProperties);
        Rules.Add(ruleIndex++, rule);
        rule.OnRuleAdded(this, ruleIndex);
        return rule;
    }

    public ValidationFluentRule<T> AddValidation(Func<T, string> func, Expression<Func<T, object?>> triggerProperty)
    {
        ValidationFluentRule<T> rule = new ValidationFluentRule<T>(func, triggerProperty);
        Rules.Add(ruleIndex++, rule);
        rule.OnRuleAdded(this, ruleIndex);
        return rule;
    }

    public AsyncFluentRule<T> AddValidationAsync(Func<T, Task<string>> func, Expression<Func<T, object?>> triggerProperty)
    {
        AsyncFluentRule<T> rule = new AsyncFluentRule<T>(func, triggerProperty);
        Rules.Add(ruleIndex++, rule);
        rule.OnRuleAdded(this, ruleIndex);
        return rule;
    }

    public async Task CheckRulesForProperty(string propertyName)
    {
        foreach (var rule in Rules.Values.Where(r => r.TriggerProperties.Any(t => t.IsMatch(propertyName)))
                                    .OrderBy(r => r.RuleOrder).ToList())
        {
            await RunRule(rule, CancellationToken.None);
        }
    }

    public async Task CheckAllRules(CancellationToken? token = null)
    {
        foreach (var ruleIndex in Rules.ToList())
        {
            await RunRule(ruleIndex.Value, token);
        }
    }

    public async Task RunRule(IRule r, CancellationToken? token = null)
    {
        if(!Rules.Values.Contains(r))
        {
            throw new Exception("Rule needs to already have been added to the RuleManager");
        }

        if (r is IRule rule)
        {
            await rule.RunRule(Target, token);
        }
        else
        {
            throw new InvalidRuleTypeException($"{r.GetType().FullName} cannot be executed for {typeof(T).FullName}");
        }

        if (token?.IsCancellationRequested ?? false)
        {
            return;
        }
    }
}

[Serializable]
public class TargetRulePropertyChangeException : Exception
{
    public TargetRulePropertyChangeException() { }
    public TargetRulePropertyChangeException(string message) : base(message) { }
    public TargetRulePropertyChangeException(string message, Exception inner) : base(message, inner) { }
    protected TargetRulePropertyChangeException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class InvalidRuleTypeException : Exception
{
    public InvalidRuleTypeException() { }
    public InvalidRuleTypeException(string message) : base(message) { }
    public InvalidRuleTypeException(string message, Exception inner) : base(message, inner) { }
    protected InvalidRuleTypeException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}

[Serializable]
public class InvalidTargetTypeException : Exception
{
    public InvalidTargetTypeException() { }
    public InvalidTargetTypeException(string message) : base(message) { }
    public InvalidTargetTypeException(string message, Exception inner) : base(message, inner) { }
    protected InvalidTargetTypeException(
      SerializationInfo info,
      StreamingContext context) : base(info, context) { }
}

[Serializable]
public class TargetIsNullException : Exception
{
    public TargetIsNullException() { }
    public TargetIsNullException(string message) : base(message) { }
    public TargetIsNullException(string message, Exception inner) : base(message, inner) { }
    protected TargetIsNullException(
      SerializationInfo info,
      StreamingContext context) : base(info, context) { }
}