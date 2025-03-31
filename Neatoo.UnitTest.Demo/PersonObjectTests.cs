using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Portal;

public partial interface IPersonObject : IEditBase
{
    public string FirstName { get; }
}

[Factory]
internal partial class PersonObject : EditBase<PersonObject>, IPersonObject
{
    public PersonObject() : base(new EditBaseServices<PersonObject>(null))
    {
        FirstName = "John";
    }

    public partial string FirstName { get; set; }
    public partial string LastName { get; set; }

    public partial void MapTo(PersonObjectDto personDto);
    public partial void MapFrom(PersonObjectDto personDto);

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