#nullable enable
using Neatoo.RemoteFactory.Internal;
using Microsoft.EntityFrameworkCore;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules.Rules;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Person.DomainModel;
/*
Class Property FirstName string? found
Class Property LastName string? found
Class Property Email string? found
Class Property Notes string? found
Class Property PersonPhoneModelList Person.DomainModel.IPersonPhoneModelList found
Class Property Id int? found
Class Property PropertyManager Neatoo.IEditPropertyManager found
Class Property Factory Neatoo.RemoteFactory.IFactorySave<Person.DomainModel.PersonModel>? found
Class Property IsMarkedModified bool found
Class Property IsModified bool found
Class Property IsSelfModified bool found
Class Property IsSavable bool found
Class Property IsNew bool found
Class Property IsDeleted bool found
Class Property ModifiedProperties System.Collections.Generic.IEnumerable<string> found
Class Property IsChild bool found
Class Property EditMetaState (bool IsModified, bool IsSelfModified, bool IsSavable, bool IsDeleted) found
Class Property Neatoo.IEditMetaProperties.IsMarkedModified bool found
Class Property this[] Neatoo.IEditProperty found
Class Property PropertyManager Neatoo.IValidatePropertyManager<Neatoo.IValidateProperty> found
Class Property RuleManager Neatoo.Rules.IRuleManager<Person.DomainModel.PersonModel> found
Class Property IsValid bool found
Class Property IsSelfValid bool found
Class Property PropertyMessages System.Collections.Generic.IReadOnlyCollection<Neatoo.IPropertyMessage> found
Class Property MetaState (bool IsValid, bool IsSelfValid, bool IsBusy) found
Class Property ObjectInvalid string? found
Class Property this[] Neatoo.IValidateProperty found
Class Property IsPaused bool found
Class Property RunningTasks Neatoo.Internal.AsyncTasks found
Class Property PropertyManager Neatoo.IPropertyManager<Neatoo.IProperty> found
Class Property Neatoo.IBase.PropertyManager Neatoo.IPropertyManager<Neatoo.IProperty> found
Class Property Parent Neatoo.IBase? found
Class Property this[] Neatoo.IProperty found
Class Property IsBusy bool found
Method MapFrom is a Match
Parameter personEntity Person.Ef.PersonEntity found for MapFrom
Parameter Property FirstName string found
Parameter Property LastName string found
Parameter Property Email string? found
Parameter Property Phone string? found
Parameter Property Notes string? found
Parameter Property Phones System.Collections.Generic.ICollection<Person.Ef.PersonPhoneEntity> found
Parameter Property Id int? found
Method MapTo is a Match
Parameter personEntity Person.Ef.PersonEntity found for MapTo
Parameter Property FirstName string found
Parameter Property LastName string found
Parameter Property Email string? found
Parameter Property Phone string? found
Parameter Property Notes string? found
Parameter Property Phones System.Collections.Generic.ICollection<Person.Ef.PersonPhoneEntity> found
Parameter Property Id int? found

*/
internal partial class PersonModel
{
    public partial void MapFrom(PersonEntity personEntity)
    {
        this.FirstName = personEntity.FirstName;
        this.LastName = personEntity.LastName;
        this.Email = personEntity.Email;
        this.Notes = personEntity.Notes;
        this.Id = personEntity.Id;
    }

    public partial void MapTo(PersonEntity personEntity)
    {
        personEntity.FirstName = this.FirstName ?? throw new NullReferenceException("Person.DomainModel.PersonModel.FirstName");
        personEntity.LastName = this.LastName ?? throw new NullReferenceException("Person.DomainModel.PersonModel.LastName");
        personEntity.Email = this.Email;
        personEntity.Notes = this.Notes;
        personEntity.Id = this.Id;
    }
}