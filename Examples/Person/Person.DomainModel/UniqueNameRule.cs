using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Person.Ef;

namespace Person.DomainModel;


[Factory]
public static partial class UniqueName
{
    [Execute]
    private static async Task<bool> _IsUniqueName(int? id, string firstName, string lastName, [Service] IPersonContext personContext)
    {
        if (await personContext.Persons.AnyAsync(x => (id == null || x.Id != id) && x.FirstName == firstName && x.LastName == lastName))
        {
            return false;
        }

        return !(firstName == "John" && lastName == "Doe"); // Not realistic - for Demo purposes only
    }
}

internal interface IUniqueNameRule : IRule<IPersonModel> { }

internal class UniqueNameRule : AsyncRuleBase<IPersonModel>, IUniqueNameRule
{
    private UniqueName.IsUniqueName isUniqueName;

    public UniqueNameRule([Service] UniqueName.IsUniqueName isUniqueName) : base()
    {
        this.isUniqueName = isUniqueName;
        AddTriggerProperties(p => p.FirstName, p => p.LastName);
    }

    protected override async Task<IRuleMessages> Execute(IPersonModel t, CancellationToken? token = null)
    {
        if (!t[nameof(t.FirstName)].IsModified && !t[nameof(t.LastName)].IsModified)
        {
            return None;
        }

        if (!string.IsNullOrEmpty(t.FirstName) && !string.IsNullOrEmpty(t.LastName))
        {
            await Task.Delay(2000); // Just for show

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
