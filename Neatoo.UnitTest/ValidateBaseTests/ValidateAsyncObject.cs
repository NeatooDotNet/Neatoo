using Neatoo.Core;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.PersonObjects;

namespace Neatoo.UnitTest.ValidateBaseTests;

public interface IValidateAsyncObject : IPersonBase
{
    string aLabel { get; set; }
    IValidateAsyncObject Child { get; set; }
    int RuleRunCount { get; }
    string ThrowException { get; set; }

    List<IValidateProperty> Properties => PropertyManager.GetProperties.Cast<IValidateProperty>().ToList();
}

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
    public async Task Fetch(PersonDto person,[Service] ValidateAsyncObjectFactory portal,[Service] IReadOnlyList<PersonDto> personTable)
    {
        base.FillFromDto(person);

        var childDto = personTable.FirstOrDefault(p => p.FatherId == Id);

        if (childDto != null)
        {
            Child = await portal.Fetch(childDto);
        }
    }

    public int RuleRunCount => ShortNameRule.RunCount + FullNameRule.RunCount;

}


public interface IValidateAsyncObjectList : IValidateListBase<IValidateAsyncObject>
{
    void Add(IValidateAsyncObject o);
}

public class ValidateAsyncObjectList : ValidateListBase<IValidateAsyncObject>, IValidateAsyncObjectList
{

    public ValidateAsyncObjectList() : base()
    {

    }

}
