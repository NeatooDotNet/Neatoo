using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Person.Ef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Person.DomainModel
{
    public delegate Task<bool> IsUniqueName(string firstName, string lastName);

    [Factory]
    public static class IsUniqueNameImplementation
    {
        [Execute<IsUniqueName>]
        public static async Task<bool> IsUnique(string firstName, string lastName, [Service] IPersonContext personContext)
        {
            if (await personContext.Persons.AnyAsync(x => x.FirstName == firstName && x.LastName == lastName))
            {
                return false;
            }

            return !(firstName == "John" && lastName == "Doe"); // Not realistic - for Demo purposes only
        }
    }

    internal interface IUniqueNameRule : IRule<IPersonModel> { }

    internal class UniqueNameRule : AsyncRuleBase<IPersonModel>, IUniqueNameRule
    {
        private IsUniqueName isUniqueName;

        public UniqueNameRule([Service] IsUniqueName isUniqueName) : base()
        {
            this.isUniqueName = isUniqueName;
            AddTriggerProperties(p => p.FirstName, p => p.LastName);
        }

        public override async Task<IRuleMessages> Execute(IPersonModel t, CancellationToken? token = null)
        {
            if (!(await isUniqueName(t.FirstName!, t.LastName!)))
            {
                return (new[]
                {
                     (nameof(t.FirstName), "First and Last name combination is not unique"),
                        (nameof(t.LastName), "First and Last name combination is not unique")
                }).AsRuleMessages();
            }
            return None;
        }
    }

}
