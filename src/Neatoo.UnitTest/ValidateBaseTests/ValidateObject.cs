﻿using Neatoo.RemoteFactory;
using Neatoo.UnitTest.PersonObjects;

namespace Neatoo.UnitTest.ValidateBaseTests;

public interface IValidateObject : IPersonBase
{
    IValidateObject Child { get; set; }
    int RuleRunCount { get; }
    void TestMarkInvalid(string message);
    IDisposable PauseAllActions();
    void ResumeAllActions();
}

[Factory]
internal partial class ValidateObject : PersonValidateBase<ValidateObject>, IValidateObject
{
    public IShortNameRule ShortNameRule { get; }
    public IFullNameRule FullNameRule { get; }

    public ValidateObject(IValidateBaseServices<ValidateObject> services,
        IShortNameRule shortNameRule,
        IFullNameRule fullNameRule,
        IRecursiveRule recursiveRule,
        IRuleThrowsException ruleThrowsException
        ) : base(services)
    {
        RuleManager.AddRules(shortNameRule, fullNameRule, recursiveRule, ruleThrowsException);
        ShortNameRule = shortNameRule;
        FullNameRule = fullNameRule;
    }

    public partial IValidateObject Child { get; set; }
    public partial IValidateObjectList ChildList { get; set; }

    [Fetch]
    public async Task Fetch(PersonDto person, [Service] IValidateObjectFactory portal, [Service] IReadOnlyList<PersonDto> personTable)
    {
        base.FromDto(person);

        var childDto = personTable.FirstOrDefault(p => p.FatherId == Id);

        if (childDto != null)
        {
            Child = await portal.Fetch(childDto);
        }
    }

    public int RuleRunCount => ShortNameRule.RunCount + FullNameRule.RunCount;

    public void TestMarkInvalid(string message)
    {
        MarkInvalid(message);
    }
}


public interface IValidateObjectList : IValidateListBase<IValidateObject>
{
    void Add(IValidateObject obj);

}

public class ValidateObjectList : ValidateListBase<IValidateObject>, IValidateObjectList
{

    public ValidateObjectList() : base()
    {
    }

}
