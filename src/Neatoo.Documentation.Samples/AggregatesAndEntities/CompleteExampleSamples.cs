/// <summary>
/// Code samples for docs/aggregates-and-entities.md - Complete Example section
///
/// Snippets in this file:
/// - docs:aggregates-and-entities:complete-example
///
/// Corresponding tests: CompleteExampleSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Documentation.Samples.AggregatesAndEntities.CompleteExample;

#region docs:aggregates-and-entities:complete-example
/// <summary>
/// Complete aggregate root example showing all key patterns.
/// </summary>
public partial interface IPerson : IEntityBase
{
    Guid? Id { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    IPersonPhoneList PersonPhoneList { get; }
}

[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services) : base(services) { }

    public partial Guid? Id { get; set; }

    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last Name*")]
    [Required(ErrorMessage = "Last Name is required")]
    public partial string? LastName { get; set; }

    public partial IPersonPhoneList PersonPhoneList { get; set; }

    // Mapper declarations - MapModifiedTo is source-generated
    public void MapFrom(PersonEntity entity)
    {
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
    }

    public void MapTo(PersonEntity entity)
    {
        entity.Id = Id;
        entity.FirstName = FirstName;
        entity.LastName = LastName;
    }

    public partial void MapModifiedTo(PersonEntity entity);

    [Create]
    public void Create([Service] IPersonPhoneListFactory phoneListFactory)
    {
        PersonPhoneList = phoneListFactory.Create();
    }

    [Fetch]
    public void Fetch(PersonEntity entity, [Service] IPersonPhoneListFactory phoneListFactory)
    {
        MapFrom(entity);
        PersonPhoneList = phoneListFactory.Fetch(entity.Phones);
    }

    [Insert]
    public Task Insert()
    {
        // In real code: create entity, MapTo, save to database
        Id = Guid.NewGuid();
        var entity = new PersonEntity();
        MapTo(entity);
        // db.Persons.Add(entity);
        // phoneListFactory.Save(PersonPhoneList, entity.Phones);
        // await db.SaveChangesAsync();
        return Task.CompletedTask;
    }

    [Update]
    public Task Update()
    {
        // In real code: fetch entity, MapModifiedTo, save changes
        // var entity = await db.Persons.FindAsync(Id);
        // MapModifiedTo(entity);
        // phoneListFactory.Save(PersonPhoneList, entity.Phones);
        // await db.SaveChangesAsync();
        return Task.CompletedTask;
    }

    [Delete]
    public Task Delete()
    {
        // In real code: delete from database
        // await db.Persons.Where(p => p.Id == Id).ExecuteDeleteAsync();
        return Task.CompletedTask;
    }
}

// EF Entity for data access
public class PersonEntity
{
    public Guid? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public ICollection<PersonPhoneEntity> Phones { get; set; } = new List<PersonPhoneEntity>();
}

public class PersonPhoneEntity
{
    public Guid? Id { get; set; }
    public string? PhoneNumber { get; set; }
}

// Child collection
public partial interface IPersonPhoneList : IEntityListBase<IPersonPhone> { }

public partial interface IPersonPhone : IEntityBase
{
    Guid? Id { get; set; }
    string? PhoneNumber { get; set; }
}

[Factory]
internal partial class PersonPhone : EntityBase<PersonPhone>, IPersonPhone
{
    public PersonPhone(IEntityBaseServices<PersonPhone> services) : base(services) { }

    public partial Guid? Id { get; set; }
    public partial string? PhoneNumber { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(PersonPhoneEntity entity)
    {
        Id = entity.Id;
        PhoneNumber = entity.PhoneNumber;
    }
}

[Factory]
internal class PersonPhoneList : EntityListBase<IPersonPhone>, IPersonPhoneList
{
    private readonly IPersonPhoneFactory _phoneFactory;

    public PersonPhoneList([Service] IPersonPhoneFactory phoneFactory)
    {
        _phoneFactory = phoneFactory;
    }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(IEnumerable<PersonPhoneEntity> entities)
    {
        foreach (var entity in entities)
        {
            Add(_phoneFactory.Fetch(entity));
        }
    }
}
#endregion
