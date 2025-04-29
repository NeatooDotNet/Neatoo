using Neatoo;
using Neatoo.RemoteFactory;
using Person.Ef;

namespace Person.DomainModel;

public interface IPersonPhoneModelList : IEditListBase<IPersonPhoneModel>
{
    IPersonPhoneModel AddPhoneNumber();
    Task RemovePhoneNumber(IPersonPhoneModel personPhoneModel);
}

[Factory]
internal class PersonPhoneModelList : EditListBase<IPersonPhoneModel>, IPersonPhoneModelList
{
    private readonly IPersonPhoneModelFactory personPhoneModelFactory;

    public PersonPhoneModelList([Service] IPersonPhoneModelFactory personPhoneModelFactory)
    {
        this.personPhoneModelFactory = personPhoneModelFactory;
    }

    public IPersonPhoneModel AddPhoneNumber()
    {
        var personPhoneModel = personPhoneModelFactory.Create();
        Add(personPhoneModel);
        return personPhoneModel;
    }

    public async Task RemovePhoneNumber(IPersonPhoneModel personPhoneModel)
    {
        this.Remove(personPhoneModel);
        await RunRules();
    }

    protected override async Task HandleNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        await base.HandleNeatooPropertyChanged(eventArgs);

        if ((eventArgs.PropertyName == nameof(IPersonPhoneModel.PhoneType) ||
            eventArgs.PropertyName == nameof(IPersonPhoneModel.PhoneNumber)))
        {
            if (eventArgs.Source is IPersonPhoneModel personPhoneModel)
            {
                await Task.WhenAll(this.Except([personPhoneModel])
                    .Select(c => c.RunRules()));
            }
        }
    }

    [Fetch]
    public void Fetch(IEnumerable<PersonPhoneEntity> personPhoneEntities,
                        [Service] IPersonPhoneModelFactory personPhoneModelFactory)
    {
        foreach (var personPhoneEntity in personPhoneEntities)
        {
            var personPhoneModel = personPhoneModelFactory.Fetch(personPhoneEntity);
            this.Add(personPhoneModel);
        }
    }

    [Update]
    public void Update(ICollection<PersonPhoneEntity> personPhoneEntities,
                        [Service] IPersonPhoneModelFactory personPhoneModelFactory)
    {
        foreach (var personPhoneModel in this.Union(DeletedList))
        {
            PersonPhoneEntity? personPhoneEntity = null;

            if (personPhoneModel.Id.HasValue)
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
