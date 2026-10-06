using Neatoo.RemoteFactory;
using Person.Dal;

namespace DomainModel;

[Factory]
public static partial class UniqueName
{
    // Resolved as the delegate UniqueName.IsUniqueName, which the source generator creates.
    // [Remote] makes the client cross to the server; on the server the delegate calls this method directly.
    // Without [Remote], RemoteFactory 1.9+ runs an [Execute] on the calling tier, where IPersonDbContext does not exist.
    [Remote]
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
