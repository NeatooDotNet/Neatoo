#nullable enable
using Neatoo.RemoteFactory.Internal;
using Neatoo;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Person.DomainModel;
/*
Class Property PhoneType Person.DomainModel.PhoneType? found
Class Property PhoneNumber string? found
Class Property ParentPersonModel Person.DomainModel.IPersonModel? found
Class Property Id int? found
Class Property PropertyManager Neatoo.IEditPropertyManager found
Class Property Factory Neatoo.RemoteFactory.IFactorySave<Person.DomainModel.PersonPhoneModel>? found
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
Class Property RuleManager Neatoo.Rules.IRuleManager<Person.DomainModel.PersonPhoneModel> found
Class Property IsValid bool found
Class Property IsSelfValid bool found
Class Property PropertyMessages System.Collections.Generic.IReadOnlyCollection<Neatoo.IPropertyMessage> found
Class Property MetaState (bool IsValid, bool IsSelfValid, bool IsBusy, bool IsSelfBusy) found
Class Property ObjectInvalid string? found
Class Property this[] Neatoo.IValidateProperty found
Class Property IsPaused bool found
Class Property AsyncTaskSequencer Neatoo.Internal.AsyncTasks found
Class Property PropertyManager Neatoo.IPropertyManager<Neatoo.IProperty> found
Class Property Neatoo.IBase.PropertyManager Neatoo.IPropertyManager<Neatoo.IProperty> found
Class Property Parent Neatoo.IBase? found
Class Property this[] Neatoo.IProperty found
Class Property IsSelfBusy bool found
Class Property IsBusy bool found
Method MapFrom is a Match
Parameter personPhoneEntity Person.Ef.PersonPhoneEntity found for MapFrom
Parameter Property PhoneNumber string found
Parameter Property PersonId int found
Parameter Property PhoneType int found
Parameter Property Id int? found
Warning: Property PhoneType's type of Person.DomainModel.PhoneType? does not match int
Method MapTo is a Match
Parameter personPhoneEntity Person.Ef.PersonPhoneEntity found for MapTo
Parameter Property PhoneNumber string found
Parameter Property PersonId int found
Parameter Property PhoneType int found
Parameter Property Id int? found
Warning: Property PhoneType's type of Person.DomainModel.PhoneType? does not match int

*/
internal partial class PersonPhoneModel
{
    public partial void MapFrom(PersonPhoneEntity personPhoneEntity)
    {
        this.PhoneNumber = personPhoneEntity.PhoneNumber;
        this.PhoneType = (Person.DomainModel.PhoneType? )personPhoneEntity.PhoneType;
        this.Id = personPhoneEntity.Id;
    }

    public partial void MapTo(PersonPhoneEntity personPhoneEntity)
    {
        personPhoneEntity.PhoneNumber = this.PhoneNumber ?? throw new NullReferenceException("Person.DomainModel.PersonPhoneModel.PhoneNumber");
        personPhoneEntity.PhoneType = (int? )this.PhoneType ?? throw new NullReferenceException("Person.DomainModel.PersonPhoneModel.PhoneType");
        personPhoneEntity.Id = this.Id;
    }
}