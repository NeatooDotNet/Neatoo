﻿#nullable enable
using Neatoo;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.PersonObjects;

/*
                    DO NOT MODIFY
                    Generated by Neatoo.BaseGenerator
                    */
namespace Neatoo.UnitTest.ValidateBaseTests
{
    internal partial class ValidateObject
    {
        public partial IValidateObject Child { get => Getter<IValidateObject>(); set => Setter(value); }
        public partial IValidateObjectList ChildList { get => Getter<IValidateObjectList>(); set => Setter(value); }
    }
}