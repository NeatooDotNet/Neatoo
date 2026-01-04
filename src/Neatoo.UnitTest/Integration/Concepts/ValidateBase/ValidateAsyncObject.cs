using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.Integration.Aggregates.Person;

namespace Neatoo.UnitTest.Integration.Concepts.ValidateBase;

public interface IValidateAsyncObject : IPersonBase
{
    string aLabel { get; set; }
    IValidateAsyncObject Child { get; set; }
    int RuleRunCount { get; }
    string ThrowException { get; set; }

    List<IValidateProperty> Properties { get; }
}

[Factory]
internal class ValidateAsyncObject : PersonValidateBase<ValidateAsyncObject>, IValidateAsyncObject
{
    public IShortNameAsyncRule ShortNameRule { get; }
    public IFullNameAsyncRule FullNameRule { get; }

    public ValidateAsyncObject(IValidateBaseServices<ValidateAsyncObject> services,
        IShortNameAsyncRule shortNameRule,
        IFullNameAsyncRule fullNameRule,
        IAsyncRuleThrowsException asyncRuleThrowsException,
        IRecursiveAsyncRule recursiveAsyncRule
        ) : base(services)
    {
        RuleManager.AddRules(shortNameRule, fullNameRule, recursiveAsyncRule);
        ShortNameRule = shortNameRule;
        FullNameRule = fullNameRule;
        // TODO : Can add a rule that's not the correct type, Handle?
        RuleManager.AddRule(asyncRuleThrowsException);

        RuleManager.AddActionAsync((v) => Task.Delay(10), _ => _.Child);
    }

    public string aLabel { get => Getter<string>(); set => Setter(value); }
    public IValidateAsyncObject Child { get { return Getter<IValidateAsyncObject>(); } set { Setter(value); } }

    public string ThrowException { get => Getter<string>(); set => Setter(value); }

    [Fetch]
    public async Task Fetch(PersonDto person, [Service] IValidateAsyncObjectFactory portal, [Service] IReadOnlyList<PersonDto> personTable)
    {
        base.FromDto(person);

        var childDto = personTable.FirstOrDefault(p => p.FatherId == Id);

        if (childDto != null)
        {
            Child = await portal.Fetch(childDto);
        }
    }

    public int RuleRunCount => ShortNameRule.RunCount + FullNameRule.RunCount;

    public List<IValidateProperty> Properties => ((IValidatePropertyManagerInternal<IValidateProperty>)PropertyManager).GetProperties.ToList();

}

public interface IValidateAsyncObjectList : IValidateListBase<IValidateAsyncObject>
{
    void Add(IValidateAsyncObject o);
}

public class ValidateAsyncObjectList : ValidateListBase<IValidateAsyncObject>, IValidateAsyncObjectList
{
    public ValidateAsyncObjectList() : base() { }
}
