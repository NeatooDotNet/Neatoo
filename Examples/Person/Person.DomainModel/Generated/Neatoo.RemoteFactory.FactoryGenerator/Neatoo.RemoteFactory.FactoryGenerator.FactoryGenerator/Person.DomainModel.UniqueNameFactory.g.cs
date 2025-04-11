#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Neatoo.Rules;
using Person.Ef;

/*
							Static Class: Person.DomainModel.UniqueName Name: UniqueName
No MethodDeclarationSyntax for Equals
No MethodDeclarationSyntax for Equals
No MethodDeclarationSyntax for Finalize
No MethodDeclarationSyntax for GetHashCode
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No MethodDeclarationSyntax for ReferenceEquals
No MethodDeclarationSyntax for ToString

*/
namespace Person.DomainModel
{
    public static partial class UniqueName
    {
        public delegate Task<bool> IsUniqueName(int? id, string firstName, string lastName);
        internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            if (remoteLocal == NeatooFactory.Remote)
            {
                services.AddTransient<UniqueName.IsUniqueName>(cc =>
                {
                    return (id, firstName, lastName) => cc.GetRequiredService<IMakeRemoteDelegateRequest>().ForDelegate<bool>(typeof(UniqueName.IsUniqueName), [id, firstName, lastName]);
                });
            }

            if (remoteLocal == NeatooFactory.Local)
            {
                services.AddTransient<UniqueName.IsUniqueName>(cc =>
                {
                    return (int? id, string firstName, string lastName) =>
                    {
                        var personContext = cc.GetRequiredService<IPersonContext>();
                        return UniqueName._IsUniqueName(id, firstName, lastName, personContext);
                    };
                });
            }
        }
    }
}