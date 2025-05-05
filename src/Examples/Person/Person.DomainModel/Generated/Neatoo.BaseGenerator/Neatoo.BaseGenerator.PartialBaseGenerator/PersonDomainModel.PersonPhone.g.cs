#nullable enable
using Neatoo;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

/*
                    Debugging Messages:
                    Method .ctor
Method get_PhoneType
Method set_PhoneType
Method get_PhoneNumber
Method set_PhoneNumber
Method get_ParentPerson
Method MapFrom
MethodDeclarationSyntax MapFrom
Method MapTo
MethodDeclarationSyntax MapTo
Method Fetch
MethodDeclarationSyntax Fetch
Method Update
MethodDeclarationSyntax Update

                    */
namespace PersonDomainModel
{
    public partial interface IPersonPhone
    {
        PhoneType? PhoneType { get; set; }

        string? PhoneNumber { get; set; }
    }

    internal partial class PersonPhone
    {
        public partial PhoneType? PhoneType { get => Getter<PhoneType?>(); set => Setter(value); }
        public partial string? PhoneNumber { get => Getter<string?>(); set => Setter(value); }
    }
}