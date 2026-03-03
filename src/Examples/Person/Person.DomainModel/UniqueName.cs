using Neatoo.RemoteFactory;
using Person.Dal;

namespace DomainModel;

[Factory]
public static partial class UniqueName
{
    // This is executed by resolving UniqueName.IsUniqueName - a Delegate created by the Source Generator
    // It is ALWAYS executed Remotely on the Server (for now)
    [Execute]
    internal static async Task<bool> _IsUniqueName(Guid? id, string firstName, string lastName, [Service] IPersonDbContext personContext)
    {
        if (await personContext.PersonNameExists(id, firstName, lastName))
        {
            return false;
        }

        return !(firstName == "Fail" || lastName == "Fail"); // Not realistic - for Demo purposes only
    }
}
