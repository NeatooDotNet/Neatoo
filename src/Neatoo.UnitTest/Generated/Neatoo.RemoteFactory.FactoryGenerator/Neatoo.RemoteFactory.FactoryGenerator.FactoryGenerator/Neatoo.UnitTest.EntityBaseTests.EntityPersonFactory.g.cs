﻿#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.Internal;
using Neatoo.UnitTest.PersonObjects;

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
namespace Neatoo.UnitTest.EntityBaseTests
{
    public interface IEntityPersonFactory
    {
        IEntityPerson FillFromDto(PersonDto dto);
        IEntityPerson Save(IEntityPerson target);
    }

    internal class EntityPersonFactory : FactorySaveBase<IEntityPerson>, IFactorySave<EntityPerson>, IEntityPersonFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public EntityPersonFactory(IServiceProvider serviceProvider, IFactoryCore<IEntityPerson> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public EntityPersonFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IEntityPerson> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public IEntityPerson LocalInsert(IEntityPerson target)
        {
            var cTarget = (EntityPerson)target ?? throw new Exception("IEntityPerson must implement EntityPerson");
            return DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Insert());
        }

        public virtual IEntityPerson FillFromDto(PersonDto dto)
        {
            return LocalFillFromDto(dto);
        }

        public IEntityPerson LocalFillFromDto(PersonDto dto)
        {
            var target = ServiceProvider.GetRequiredService<EntityPerson>();
            return DoFactoryMethodCall(target, FactoryOperation.Fetch, () => target.FillFromDto(dto));
        }

        public virtual IEntityPerson Save(IEntityPerson target)
        {
            return LocalSave(target);
        }

        async Task<IFactorySaveMeta?> IFactorySave<EntityPerson>.Save(EntityPerson target)
        {
            return await Task.FromResult((IFactorySaveMeta? )Save(target));
        }

        public virtual IEntityPerson LocalSave(IEntityPerson target)
        {
            if (target.IsDeleted)
            {
                throw new NotImplementedException();
            }
            else if (target.IsNew)
            {
                return LocalInsert(target);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            services.AddScoped<EntityPersonFactory>();
            services.AddScoped<IEntityPersonFactory, EntityPersonFactory>();
            services.AddTransient<EntityPerson>();
            services.AddTransient<IEntityPerson, EntityPerson>();
            services.AddScoped<IFactorySave<EntityPerson>, EntityPersonFactory>();
        }
    }
}