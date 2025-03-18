using System.Diagnostics;
using System.Linq.Expressions;

namespace Neatoo.Rules;

public interface IRule
{
    /// <summary>
    /// Must be unique for every rule across all types
    /// </summary>
    uint UniqueIndex { get; }

    /// <summary>
    /// Rule has been executed at least once
    /// </summary>
    bool Executed { get; }
    IReadOnlyList<ITriggerProperty> TriggerProperties { get; }
    internal Task<PropertyErrors> RunRule(IValidateBase target, CancellationToken? token = null);
}

/// <summary>
/// Contravariant - Allows RuleManager to call even when generic types are different
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRule<T> : IRule
    where T : IValidateBase
{
    Task<PropertyErrors> RunRule(T target, CancellationToken? token = null);
}

// TODO - Doesn't work for serialization
internal static class RuleIndexer
{
    private static uint staticIndex = 0;
    internal static uint StaticIndex
    {
        get
        {
            staticIndex++;
            return staticIndex;
        }
    }
}

public abstract class AsyncRuleBase<T> : IRule<T>
    where T : class, IValidateBase
{
    protected AsyncRuleBase()
    {
        /// Must be unique for every rule across all types so Static counter is important
        UniqueIndex = RuleIndexer.StaticIndex;
    }

    public AsyncRuleBase(params Expression<Func<T, object?>>[] triggerOnPropertyNames) : this(triggerOnPropertyNames.AsEnumerable()) { }

    public AsyncRuleBase(IEnumerable<Expression<Func<T, object?>>> triggerOnPropertyNames) : this()
    {
        TriggerProperties.AddRange(triggerOnPropertyNames.Select(propertyName => new TriggerProperty<T>(propertyName)));
    }

    /// <summary>
    /// 
    /// </summary>
    public uint UniqueIndex { get; }

    protected PropertyErrors None = PropertyErrors.None;

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

    public abstract Task<PropertyErrors> Execute(T t, CancellationToken? token = null);

    protected PropertyErrors? PreviousErrors { get; set; }

    public virtual Task<PropertyErrors> RunRule(IValidateBase target, CancellationToken? token = null)
    {
        var typedTarget = target as T;
        
        if (typedTarget == null)
        {
            throw new Exception($"{target.GetType().Name} is not of type {typeof(T).Name}");
        }

        return RunRule(typedTarget, token);
    }

    public virtual async Task<PropertyErrors> RunRule(T target, CancellationToken? token = null)
    {
        try
        {
            Executed = true;

            var propertyErrors = await Execute(target, token);

            var setAtLeastOneProperty = true;

            foreach (var propertyError in propertyErrors)
            {
                if (target.PropertyManager.HasProperty(propertyError.Key))
                {
                    setAtLeastOneProperty = true;
                    target[propertyError.Key].SetErrorsForRule(UniqueIndex, propertyError.Value);
                }
            }

            if (PreviousErrors != null)
            {
                PreviousErrors.Select(t => t.Key).Except(propertyErrors.Select(p => p.Key)).ToList().ForEach(p =>
                {
                    if (target.PropertyManager.HasProperty(p))
                    {
                        var propertyValue = target[p];
                        propertyValue.ClearErrorsForRule(UniqueIndex);
                    }
                });
            }

            Debug.Assert(setAtLeastOneProperty, "You must have at least one trigger property that is a valid property on the target");

            PreviousErrors = propertyErrors;

            return propertyErrors;
        }
        catch (Exception ex)
        {
            TriggerProperties.ForEach(p =>
                {
                    // Allow children
                    if(target.PropertyManager.HasProperty(p.PropertyName))
                    {
                        var propertyValue = target[p.PropertyName];
                        propertyValue.SetErrorsForRule(UniqueIndex, [ex.Message]);
                    }
                });

            throw;
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

    public abstract PropertyErrors Execute(T target);

    public sealed override Task<PropertyErrors> Execute(T target, CancellationToken? token = null)
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

    public override PropertyErrors Execute(T target)
    {
        ExecuteFunc(target);
        return PropertyErrors.None;
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

    override public async Task<PropertyErrors> Execute(T target, CancellationToken? token = null)
    {
        await ExecuteFunc(target);
        return PropertyErrors.None;
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

    public override PropertyErrors Execute(T target)
    {
        var result = ExecuteFunc(target);

        if (string.IsNullOrWhiteSpace(result))
        {
            return PropertyErrors.None;
        }
        else
        {
            return new PropertyError(TriggerProperties.Single().PropertyName, result);
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

    public override async Task<PropertyErrors> Execute(T target, CancellationToken? token = null)
    {
        var result = await ExecuteFunc(target);

        if (string.IsNullOrWhiteSpace(result))
        {
            return PropertyErrors.None;
        }
        else
        {
            return new PropertyError(TriggerProperties.Single().PropertyName, result);
        }
    }
}

public class PropertyErrors : Dictionary<string, List<string>>
{

    public static PropertyErrors None = new PropertyErrors();

    public void Add(string propertyName, string message)
    {
        if (TryGetValue(propertyName, out var messages))
        {
            messages.Add(message);
        }
        else
        {
            Add(propertyName, new List<string> { message });
        }
    }

    public static implicit operator PropertyErrors(PropertyError error)
    {
        return new PropertyErrors { [error.PropertyName] = new List<string> { error.Message } };
    }
    public static implicit operator PropertyErrors((string name, string errorMessage) propertyError)
    {
        return new PropertyError(propertyError.name, propertyError.errorMessage);
    }
}

public class PropertyError
{
    public string PropertyName { get; }
    public string Message { get; }
    public PropertyError(string propertyName, string message)
    {
        PropertyName = propertyName;
        Message = message;
    }

    public static implicit operator PropertyError((string name, string errorMessage) propertyError)
    {
        return new PropertyError(propertyError.name, propertyError.errorMessage);
    }

}

public static class PropertyErrorExtension
{
    public static PropertyError PropertyError(this string propertyName, string message)
    {
        return new PropertyError(propertyName, message);
    }
}