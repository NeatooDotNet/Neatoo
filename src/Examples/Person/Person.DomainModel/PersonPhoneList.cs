using Neatoo;
using Neatoo.RemoteFactory;
using Person.Dal;

namespace DomainModel;

public interface IPersonPhoneList : IEntityListBase<IPersonPhone>
{
    IPersonPhone AddPhoneNumber();
    Task RemovePhoneNumber(IPersonPhone personPhoneModel);
}

[Factory]
internal class PersonPhoneList : EntityListBase<IPersonPhone>, IPersonPhoneList
{
    private readonly IPersonPhoneFactory personPhoneModelFactory;

    public PersonPhoneList([Service] IPersonPhoneFactory personPhoneModelFactory)
    {
        this.personPhoneModelFactory = personPhoneModelFactory;
    }

    public IPersonPhone AddPhoneNumber()
    {
        var personPhoneModel = personPhoneModelFactory.Create();
        Add(personPhoneModel);
        return personPhoneModel;
    }

    public async Task RemovePhoneNumber(IPersonPhone personPhoneModel)
    {
        this.Remove(personPhoneModel);
        await RunRules();
    }

    protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        await base.HandleNeatooPropertyChanged(eventArgs);

        if ((eventArgs.PropertyName == nameof(IPersonPhone.PhoneType) ||
            eventArgs.PropertyName == nameof(IPersonPhone.PhoneNumber)))
        {
            if (eventArgs.Source is IPersonPhone personPhoneModel)
            {
                await Task.WhenAll(this.Except([personPhoneModel])
                    .Select(c => c.RunRules()));
            }
        }
    }

    [Fetch]
    internal void Fetch(IEnumerable<PersonPhoneEntity> personPhoneEntities,
                        [Service] IPersonPhoneFactory personPhoneModelFactory)
    {
        foreach (var personPhoneEntity in personPhoneEntities)
        {
            var personPhoneModel = personPhoneModelFactory.Fetch(personPhoneEntity);
            this.Add(personPhoneModel);
        }
    }

    [Remote]
    [Fetch]
    internal async Task Fetch(Guid personId,
                              [Service] IPersonDbContext personContext,
                              [Service] IPersonPhoneFactory personPhoneModelFactory,
                              CancellationToken cancellationToken = default)
    {
        var phoneEntities = await personContext.FindPersonPhones(personId, cancellationToken);
        foreach (var entity in phoneEntities)
        {
            Add(personPhoneModelFactory.Fetch(entity, cancellationToken));
        }
    }

    [Update]
    internal void Update(ICollection<PersonPhoneEntity> personPhoneEntities,
                        [Service] IPersonPhoneFactory personPhoneModelFactory)
    {
        foreach (var personPhoneModel in this.Union(DeletedList))
        {
            PersonPhoneEntity? personPhoneEntity = null;

            if (!personPhoneModel.IsNew)
            {
                personPhoneEntity = personPhoneEntities.Single(x => x.Id == personPhoneModel.Id);
            }
            else
            {
                personPhoneEntity = new PersonPhoneEntity();
                personPhoneEntities.Add(personPhoneEntity);
            }

            if (personPhoneModel.IsDeleted)
            {
                personPhoneEntities.Remove(personPhoneEntity);
            }
            else
            {
                personPhoneModelFactory.Save(personPhoneModel, personPhoneEntity);
            }
        }
    }
}
