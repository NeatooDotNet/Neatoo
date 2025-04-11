using Neatoo.Rules;

namespace Person.DomainModel;
internal interface IUniquePhoneNumberRule : IRule<IPersonPhoneModel> { }

internal class UniquePhoneNumberRule : RuleBase<IPersonPhoneModel>, IUniquePhoneNumberRule
{
    public UniquePhoneNumberRule()
    {
        AddTriggerProperties(p => p.PhoneType, p => p.PhoneNumber);
    }

    protected override IRuleMessages Execute(IPersonPhoneModel target)
    {
        return RuleMessages.If(target.ParentPersonModel == null, nameof(IPersonPhoneModel.PhoneType), "Parent is null")
            .If(target.ParentPersonModel == null, nameof(IPersonPhoneModel.PhoneNumber), "Parent is null")
            .ElseIf(() => target.ParentPersonModel!.PersonPhoneModelList
                        .Where(c => c != target)
                        .Any(c => c.PhoneNumber == target.PhoneNumber), nameof(IPersonPhoneModel.PhoneNumber), "Phone number must be unique");
    }
}
