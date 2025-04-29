using Neatoo.Rules;

namespace Person.DomainModel;

internal interface IUniquePhoneTypeRule : IRule<IPersonPhoneModel> { }

internal class UniquePhoneTypeRule : RuleBase<IPersonPhoneModel>, IUniquePhoneTypeRule
{
    public UniquePhoneTypeRule()
    {
        AddTriggerProperties(p => p.PhoneType, p => p.PhoneNumber);
    }

    protected override IRuleMessages Execute(IPersonPhoneModel target)
    {
        return RuleMessages.If(target.ParentPersonModel == null, nameof(IPersonPhoneModel.PhoneType), "Parent is null")
            .If(target.ParentPersonModel == null, nameof(IPersonPhoneModel.PhoneNumber), "Parent is null")
            .ElseIf(() => target.ParentPersonModel!.PersonPhoneModelList
                        .Where(c => c != target)
                        .Any(c => c.PhoneType == target.PhoneType), nameof(IPersonPhoneModel.PhoneType), "Phone type must be unique");
    }
}
