using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Portal;

public partial interface IPersonObject : IEntityBase
{
    public string FirstName { get; }
}

[Factory]
internal partial class PersonObject : EntityBase<PersonObject>, IPersonObject
{
    public PersonObject() : base(new EntityBaseServices<PersonObject>(null))
    {
        FirstName = "John";
    }

    public partial string FirstName { get; set; }
    public partial string LastName { get; set; }

    public void MapTo(PersonObjectDto personDto)
    {
        personDto.FirstName = this.FirstName;
        personDto.LastName = this.LastName;
    }

    public void MapFrom(PersonObjectDto personDto)
    {
        this.FirstName = personDto.FirstName;
        this.LastName = personDto.LastName;
    }

    public partial void MapModifiedTo(PersonObjectDto personDto);

    public void Method(PersonObjectDto dto)
    {
        if (this[nameof(FirstName)].IsModified)
        {
            dto.FirstName = this.FirstName;
        }
    }
}

public class PersonObjectDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class PersonObjectTests
{

    [Fact]
    public void Test1()
    {
        var person = new PersonObject();

        //person.FirstName = "John";
        person.LastName = "Doe";

    }
}