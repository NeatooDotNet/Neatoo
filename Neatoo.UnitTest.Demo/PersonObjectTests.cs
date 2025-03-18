using Microsoft.Extensions.DependencyInjection;
using Neatoo.AuthorizationRules;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.Demo;

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