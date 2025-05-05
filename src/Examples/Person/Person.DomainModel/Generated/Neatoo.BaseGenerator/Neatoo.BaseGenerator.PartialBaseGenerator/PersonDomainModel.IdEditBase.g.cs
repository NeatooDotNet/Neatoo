#nullable enable
using Neatoo;
using Microsoft.Extensions.DependencyInjection;
using Person.Ef;
using System.ComponentModel;
using System.Diagnostics;

/*
                    Debugging Messages:
                    Method .ctor
Method get_Id
Method set_Id
Method HandleIdPropertyChanged
MethodDeclarationSyntax HandleIdPropertyChanged

                    */
namespace PersonDomainModel
{
    internal partial class IdEditBase<T>
    {
        public partial int? Id { get => Getter<int?>(); set => Setter(value); }
    }
}