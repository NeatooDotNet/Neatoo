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
    public delegate Task<bool> IsUniqueName(int? id, string firstName, string lastName);

    [Factory]
    public static class IsUniqueNameImplementation
    {
        [Execute<IsUniqueName>]
        public static async Task<bool> IsUnique(int? id, string firstName, string lastName, [Service] IPersonContext personContext)
        {
            await Task.Delay(1000); // Just for show

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
        private IsUniqueName isUniqueName;

        public UniqueNameRule([Service] IsUniqueName isUniqueName) : base()
        {
            this.isUniqueName = isUniqueName;
            AddTriggerProperties(p => p.FirstName, p => p.LastName);
        }

        protected override async Task<IRuleMessages> Execute(IPersonModel t, CancellationToken? token = null)
        {
            if (!(await isUniqueName(t.Id, t.FirstName!, t.LastName!)))
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
