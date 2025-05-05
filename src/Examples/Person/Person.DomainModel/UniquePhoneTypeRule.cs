using Neatoo.Rules;

namespace DomainModel;

internal interface IUniquePhoneTypeRule : IRule<IPersonPhone> { }

internal class UniquePhoneTypeRule : RuleBase<IPersonPhone>, IUniquePhoneTypeRule
{
    public UniquePhoneTypeRule()
    {
        AddTriggerProperties(p => p.PhoneType, p => p.PhoneNumber);
    }

    protected override IRuleMessages Execute(IPersonPhone target)
    {
        return RuleMessages.If(target.ParentPerson == null, nameof(IPersonPhone.PhoneType), "Parent is null")
            .If(target.ParentPerson == null, nameof(IPersonPhone.PhoneNumber), "Parent is null")
            .ElseIf(() => target.ParentPerson!.PersonPhoneList
                        .Where(c => c != target)
                        .Any(c => c.PhoneType == target.PhoneType), nameof(IPersonPhone.PhoneType), "Phone type must be unique");
    }
}
