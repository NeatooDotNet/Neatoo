using Neatoo;
using Neatoo.RemoteFactory;
using Person.Ef;

namespace DomainModel;

public interface IPersonPhoneList : IEditListBase<IPersonPhone>
{
    IPersonPhone AddPhoneNumber();
    Task RemovePhoneNumber(IPersonPhone personPhoneModel);
}

[Factory]
internal class PersonPhoneList : EditListBase<IPersonPhone>, IPersonPhoneList
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
    public void Fetch(IEnumerable<PersonPhoneEntity> personPhoneEntities,
                        [Service] IPersonPhoneFactory personPhoneModelFactory)
    {
        foreach (var personPhoneEntity in personPhoneEntities)
        {
            var personPhoneModel = personPhoneModelFactory.Fetch(personPhoneEntity);
            this.Add(personPhoneModel);
        }
    }

    [Update]
    public void Update(ICollection<PersonPhoneEntity> personPhoneEntities,
                        [Service] IPersonPhoneFactory personPhoneModelFactory)
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
