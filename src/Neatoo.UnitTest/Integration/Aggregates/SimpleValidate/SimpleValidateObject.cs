namespace Neatoo.UnitTest.Integration.Aggregates.SimpleValidate;


public partial class SimpleValidateObject : ValidateBase<SimpleValidateObject>, ISimpleValidateObject
{
    public SimpleValidateObject(IValidateBaseServices<SimpleValidateObject> services,
                                IShortNameRule shortNameRule) : base(services)
    {
        RuleManager.AddRule(shortNameRule);
    }

    public partial Guid Id { get; }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string ShortName { get; set; }

    public new IValidateProperty this[string propertyName] { get => GetProperty(propertyName); }
}

public interface ISimpleValidateObject : IValidateBase
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string ShortName { get; set; }
    new IValidateProperty this[string propertyName]
    {
        get => GetProperty(propertyName);
    }
}
