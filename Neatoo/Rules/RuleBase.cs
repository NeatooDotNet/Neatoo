using System.Diagnostics;
using System.Linq.Expressions;

namespace Neatoo.Rules;

public interface IRule
{
    /// <summary>
    /// Rule has been executed at least once
    /// </summary>
    bool Executed { get; }
    int RuleOrder { get; }
    IReadOnlyList<ITriggerProperty> TriggerProperties { get; }
    Task<IRuleMessages> RunRule(IValidateBase target, CancellationToken? token = null);
    void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex); // TODO: Replace with Factory Method and Constructor
}

/// <summary>
/// Contravariant - Allows RuleManager to call even when generic types are different
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRule<T> : IRule
    where T : IValidateBase
{
    Task<IRuleMessages> RunRule(T target, CancellationToken? token = null);
}

public abstract class AsyncRuleBase<T> : IRule<T>
    where T : class, IValidateBase
{
    protected AsyncRuleBase()
    {
    }

    public AsyncRuleBase(params Expression<Func<T, object?>>[] triggerOnPropertyNames) : this(triggerOnPropertyNames.AsEnumerable()) { }

    public AsyncRuleBase(IEnumerable<Expression<Func<T, object?>>> triggerOnPropertyNames) : this()
    {
        TriggerProperties.AddRange(triggerOnPropertyNames.Select(propertyName => new TriggerProperty<T>(propertyName)));
    }

    /// <summary>
    /// If the rule is static you'll want to set this to a unique value
    /// </summary>
    protected uint UniqueIndex { get; set; }

    protected RuleMessages None = RuleMessages.None;
    public int RuleOrder { get; protected set; } = 1;
    public bool Executed { get; protected set; }

    IReadOnlyList<ITriggerProperty> IRule.TriggerProperties => TriggerProperties.AsReadOnly();
    protected List<ITriggerProperty> TriggerProperties { get; } = new List<ITriggerProperty>();

    protected virtual void AddTriggerProperties(params Expression<Func<T, object?>>[] triggerOnExpression)
    {
        TriggerProperties.AddRange(triggerOnExpression.Select(expression => new TriggerProperty<T>(expression)));
    }

    protected virtual void AddTriggerProperties(params ITriggerProperty[] triggerProperties)
    {
        TriggerProperties.AddRange(triggerProperties);
    }

    public abstract Task<IRuleMessages> Execute(T t, CancellationToken? token = null);

    protected IRuleMessages? PreviousErrors { get; set; }

    public virtual Task<IRuleMessages> RunRule(IValidateBase target, CancellationToken? token = null)
    {
        var typedTarget = target as T;

        if (typedTarget == null)
        {
            throw new Exception($"{target.GetType().Name} is not of type {typeof(T).Name}");
        }

        return RunRule(typedTarget, token);
    }

    public virtual async Task<IRuleMessages> RunRule(T target, CancellationToken? token = null)
    {
        try
        {
            Executed = true;

            var ruleMessages = await Execute(target, token);

            var setAtLeastOneProperty = true;

            foreach (var ruleMessage in ruleMessages.GroupBy(rm => rm.PropertyName).ToDictionary(g => g.Key, g => g.ToList()))
            {
                ruleMessage.Value.ForEach(rm => rm.RuleIndex = UniqueIndex);

                if (target.PropertyManager.HasProperty(ruleMessage.Key))
                {
                    setAtLeastOneProperty = true;
                    target[ruleMessage.Key].SetMessagesForRule(ruleMessage.Value);
                }
            }

            if (PreviousErrors != null)
            {
                PreviousErrors.Select(t => t.PropertyName).Except(ruleMessages.Select(p => p.PropertyName)).ToList().ForEach(p =>
                {
                    if (target.PropertyManager.HasProperty(p))
                    {
                        var propertyValue = target[p];
                        propertyValue.ClearMessagesForRule(UniqueIndex);
                    }
                });
            }

            Debug.Assert(setAtLeastOneProperty, "You must have at least one trigger property that is a valid property on the target");

            PreviousErrors = ruleMessages;

            return ruleMessages;
        }
        catch (Exception ex)
        {
            TriggerProperties.ForEach(p =>
                {
                    // Allow children to be trigger properties
                    if (target.PropertyManager.HasProperty(p.PropertyName))
                    {
                        var propertyValue = target[p.PropertyName];
                        propertyValue.SetMessagesForRule(p.PropertyName.RuleMessages(ex.Message).AsReadOnly());
                    }
                });

            throw;
        }
    }

    public virtual void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex)
    {
        // Does this break static rules??
        if (this.UniqueIndex == default)
        {
            this.UniqueIndex = uniqueIndex;
        }
    }

    /// <summary>
    /// Write a property without re-running any rules
    /// </summary>
    /// <typeparam name="P"></typeparam>
    /// <param name="target"></param>
    /// <param name="triggerProperty"></param>
    /// <param name="value"></param>
    protected void LoadProperty(T target, ITriggerProperty triggerProperty, object? value)
    {
        target[triggerProperty.PropertyName].LoadValue(value);
    }

    protected void LoadProperty<P>(T target, ITriggerProperty triggerProperty, P value)
    {
        target[triggerProperty.PropertyName].LoadValue(value);
    }

    protected void LoadProperty<P>(T target, Expression<Func<T, object?>> expression, P value)
    {
        var triggerProperty = new TriggerProperty<T>(expression);

        target[triggerProperty.PropertyName].LoadValue(value);
    }
}


public abstract class RuleBase<T> : AsyncRuleBase<T>
    where T : class, IValidateBase
{
    protected RuleBase() { }

    protected RuleBase(params Expression<Func<T, object?>>[] triggerOnPropertyNames) : base(triggerOnPropertyNames)
    {
    }

    public abstract IRuleMessages Execute(T target);

    public sealed override Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        return Task.FromResult(Execute(target));
    }
}

public class ActionFluentRule<T> : RuleBase<T>
where T : class, IValidateBase
{
    private Action<T> ExecuteFunc { get; }
    public ActionFluentRule(Action<T> execute, Expression<Func<T, object?>> triggerProperty) : base([triggerProperty])
    {
        this.ExecuteFunc = execute;
    }

    public override IRuleMessages Execute(T target)
    {
        ExecuteFunc(target);
        return RuleMessages.None;
    }
}

public class ActionAsyncFluentRule<T> : AsyncRuleBase<T>
where T : class, IValidateBase
{
    private Func<T, Task> ExecuteFunc { get; }
    public ActionAsyncFluentRule(Func<T, Task> execute, Expression<Func<T, object?>> triggerProperty) : base([triggerProperty])
    {
        this.ExecuteFunc = execute;
    }

    override public async Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        await ExecuteFunc(target);
        return RuleMessages.None;
    }
}

public class ValidationFluentRule<T> : RuleBase<T>
    where T : class, IValidateBase
{
    private Func<T, string> ExecuteFunc { get; }
    public ValidationFluentRule(Func<T, string> execute, Expression<Func<T, object?>> triggerProperty) : base([triggerProperty])
    {
        this.ExecuteFunc = execute;
    }

    public override IRuleMessages Execute(T target)
    {
        var result = ExecuteFunc(target);

        if (string.IsNullOrWhiteSpace(result))
        {
            return RuleMessages.None;
        }
        else
        {
            return (TriggerProperties.Single().PropertyName, result).AsRuleMessages();
        }
    }
}

public class AsyncFluentRule<T> : AsyncRuleBase<T>
where T : class, IValidateBase
{
    private Func<T, Task<string>> ExecuteFunc { get; }

    public AsyncFluentRule(Func<T, Task<string>> execute, Expression<Func<T, object?>> triggerProperty) : base([triggerProperty])
    {
        this.ExecuteFunc = execute;
    }

    public override async Task<IRuleMessages> Execute(T target, CancellationToken? token = null)
    {
        var result = await ExecuteFunc(target);

        if (string.IsNullOrWhiteSpace(result))
        {
            return RuleMessages.None;
        }
        else
        {
            return (TriggerProperties.Single().PropertyName, result).AsRuleMessages();
        }
    }
}

