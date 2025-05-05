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
Method get_FirstName
Method set_FirstName
Method get_LastName
Method set_LastName
Method get_Email
Method set_Email
Method get_Notes
Method set_Notes
Method get_PersonPhoneList
Method set_PersonPhoneList
Method MapFrom
MethodDeclarationSyntax MapFrom
Method MapTo
MethodDeclarationSyntax MapTo
Method MapModifiedTo
MethodDeclarationSyntax MapModifiedTo
Method MapModifiedTo is a Match
Parameter personEntity Person.Ef.PersonEntity found for MapModifiedTo
Parameter Property FirstName string found
Parameter Property LastName string found
Parameter Property Email string? found
Parameter Property Phone string? found
Parameter Property Notes string? found
Parameter Property Phones System.Collections.Generic.IList<Person.Ef.PersonPhoneEntity> found
Parameter Property Id int? found
Method Create
MethodDeclarationSyntax Create
Method Fetch
MethodDeclarationSyntax Fetch
Method Insert
MethodDeclarationSyntax Insert
Method Update
MethodDeclarationSyntax Update
Method Delete
MethodDeclarationSyntax Delete

                    */
namespace PersonDomainModel
{
    public partial interface IPerson
    {
        string? FirstName { get; set; }

        string? LastName { get; set; }

        string? Email { get; set; }

        string? Notes { get; set; }

        IPersonPhoneList PersonPhoneList { get; set; }
    }

    internal partial class Person
    {
        public partial string? FirstName { get => Getter<string?>(); set => Setter(value); }
        public partial string? LastName { get => Getter<string?>(); set => Setter(value); }
        public partial string? Email { get => Getter<string?>(); set => Setter(value); }
        public partial string? Notes { get => Getter<string?>(); set => Setter(value); }
        public partial IPersonPhoneList PersonPhoneList { get => Getter<IPersonPhoneList>(); set => Setter(value); }

        public partial void MapModifiedTo(PersonEntity personEntity)
        {
            if (this[nameof(FirstName)].IsModified)
            {
                personEntity.FirstName = this.FirstName ?? throw new NullReferenceException("PersonDomainModel.Person.FirstName");
            }

            if (this[nameof(LastName)].IsModified)
            {
                personEntity.LastName = this.LastName ?? throw new NullReferenceException("PersonDomainModel.Person.LastName");
            }

            if (this[nameof(Email)].IsModified)
            {
                personEntity.Email = this.Email;
            }

            if (this[nameof(Notes)].IsModified)
            {
                personEntity.Notes = this.Notes;
            }

            if (this[nameof(Id)].IsModified)
            {
                personEntity.Id = this.Id;
            }
        }
    }
}