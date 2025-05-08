using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace DomainModel;

internal interface IUniqueNameRule : IRule<IPerson> { }

internal class UniqueNameRule : AsyncRuleBase<IPerson>, IUniqueNameRule
{
    private UniqueName.IsUniqueName isUniqueName;

    public UniqueNameRule(UniqueName.IsUniqueName isUniqueName) : base()
    {
        this.isUniqueName = isUniqueName;
        AddTriggerProperties(p => p.FirstName, p => p.LastName);
    }

    protected override async Task<IRuleMessages> Execute(IPerson t, CancellationToken? token = null)
    {
        if (!t[nameof(t.FirstName)].IsModified && !t[nameof(t.LastName)].IsModified)
        {
            return None;
        }

        if (!string.IsNullOrEmpty(t.FirstName) && !string.IsNullOrEmpty(t.LastName))
        {
            if(t.FirstName == "Delay" || t.LastName == "Delay")
            {
                await Task.Delay(5000); // Just for show
            }

            if (!(await isUniqueName(t.Id, t.FirstName!, t.LastName!)))
            {
                return (new[]
                {
                    (nameof(t.FirstName), "First and Last name combination is not unique"),
                    (nameof(t.LastName), "First and Last name combination is not unique")
                }).AsRuleMessages();
            }
        }
        return None;
    }
}
