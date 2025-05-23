﻿#nullable enable
using Neatoo;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

/*
                    DO NOT MODIFY
                    Generated by Neatoo.BaseGenerator
                    */
namespace DomainModel
{
    public partial interface IPerson
    {
        Guid? Id { get; set; }

        string? FirstName { get; set; }

        string? LastName { get; set; }

        string? Email { get; set; }

        string? Notes { get; set; }

        IPersonPhoneList PersonPhoneList { get; set; }
    }

    internal partial class Person
    {
        public partial Guid? Id { get => Getter<Guid?>(); set => Setter(value); }
        public partial string? FirstName { get => Getter<string?>(); set => Setter(value); }
        public partial string? LastName { get => Getter<string?>(); set => Setter(value); }
        public partial string? Email { get => Getter<string?>(); set => Setter(value); }
        public partial string? Notes { get => Getter<string?>(); set => Setter(value); }
        public partial IPersonPhoneList PersonPhoneList { get => Getter<IPersonPhoneList>(); set => Setter(value); }

        public partial void MapModifiedTo(PersonEntity personEntity)
        {
            if (this[nameof(Id)].IsModified)
            {
                personEntity.Id = this.Id;
            }

            if (this[nameof(FirstName)].IsModified)
            {
                personEntity.FirstName = this.FirstName ?? throw new NullReferenceException("DomainModel.Person.FirstName");
            }

            if (this[nameof(LastName)].IsModified)
            {
                personEntity.LastName = this.LastName ?? throw new NullReferenceException("DomainModel.Person.LastName");
            }

            if (this[nameof(Email)].IsModified)
            {
                personEntity.Email = this.Email;
            }

            if (this[nameof(Notes)].IsModified)
            {
                personEntity.Notes = this.Notes;
            }
        }
    }
}