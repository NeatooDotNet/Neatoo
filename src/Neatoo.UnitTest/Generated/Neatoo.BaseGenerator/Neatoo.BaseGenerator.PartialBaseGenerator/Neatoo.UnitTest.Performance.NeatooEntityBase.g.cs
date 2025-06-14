﻿#nullable enable
using Neatoo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

/*
                    DO NOT MODIFY
                    Generated by Neatoo.BaseGenerator
                    */
namespace Neatoo.UnitTest.Performance
{
    public partial class NeatooEntityBase
    {
        public partial int Id { get => Getter<int>(); set => Setter(value); }
        public partial string? Description { get => Getter<string?>(); set => Setter(value); }
        public partial NeatooEntityBase? ChildA { get => Getter<NeatooEntityBase?>(); set => Setter(value); }
        public partial NeatooEntityBase? ChildB { get => Getter<NeatooEntityBase?>(); set => Setter(value); }
    }
}