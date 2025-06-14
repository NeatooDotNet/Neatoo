﻿#nullable enable
using Neatoo;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

/*
                    DO NOT MODIFY
                    Generated by Neatoo.BaseGenerator
                    */
namespace Neatoo.UnitTest.SystemTextJson.EntityTests
{
    public partial class EntityObject
    {
        public partial Guid ID { get => Getter<Guid>(); set => Setter(value); }
        public partial string Name { get => Getter<string>(); set => Setter(value); }
        public partial IEntityObject Child { get => Getter<IEntityObject>(); set => Setter(value); }
        public partial IEntityObjectList ChildList { get => Getter<IEntityObjectList>(); set => Setter(value); }
        public partial int? Required { get => Getter<int?>(); set => Setter(value); }
    }
}