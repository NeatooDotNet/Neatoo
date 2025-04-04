using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Neatoo;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using Person.DomainModel;
using Person.Ef;
using Rocks;

[assembly: Rock(typeof(IUniqueNameRule), BuildType.Create)]


namespace Person.DomainModel.Tests.UnitTests;


public class PersonModelUnitTests
{
    private PersonModel person;
    private IUniqueNameRuleCreateExpectations uniqueNameRule = new IUniqueNameRuleCreateExpectations();
    private static uint uniqueindex = 1000; // TODO - Rule.UniqueIndex needs to go away
    public PersonModelUnitTests()
    {
        uniqueNameRule.Methods.RunRule(Arg.Any<IPersonModel>()).ReturnValue(Task.FromResult(IRuleMessages.None));
        uniqueNameRule.Methods.OnRuleAdded(Arg.Any<IRuleManager>(), Arg.Any<uint>());
        uniqueNameRule.Properties.Getters.TriggerProperties().ReturnValue(Array.Empty<ITriggerProperty>());

        person = new PersonModel(new EditBaseServices<PersonModel>(null), uniqueNameRule.Instance());
    }

    [Fact]
    public void PersonModelUnitTests_RequiredFieldsSet()
    {
        person.FirstName = "John";
        person.LastName = "Doe";
        Assert.True(person.IsValid);
    }

    [Fact]
    public async Task PersonModelUnitTests_Fetch()
    {
        var personPhoneModelListFactory = new Mock<IPersonPhoneModelListFactory>();
        var personContext = new Mock<IPersonContext>();
        var personEntity = new PersonEntity { Id = 1, FirstName = "John", LastName = "Doe", Notes = Guid.NewGuid().ToString() };

        personEntity.Phones = new List<PersonPhoneEntity> { new PersonPhoneEntity { Id = 1, PersonId = 1, PhoneNumber = "123-456-7890" } };
        personPhoneModelListFactory.Setup(x => x.Fetch(personEntity.Phones)).Returns(new PersonPhoneModelList(Mock.Of<IPersonPhoneModelFactory>()));

        personContext.Setup(x => x.Persons).ReturnsDbSet(new[] { personEntity });

        var successful = await person.Fetch(personContext.Object, personPhoneModelListFactory.Object);

        Assert.True(successful);
        Assert.Equal(personEntity.FirstName, person.FirstName);
        Assert.Equal(personEntity.LastName, person.LastName);
        Assert.Equal(personEntity.Notes, person.Notes);

        personPhoneModelListFactory.Verify(x => x.Fetch(personEntity.Phones), Times.Once);
    }

    [Fact]
    public async Task PersonModelUnitTests_Fetch_NotFound()
    {
        var personContext = new Mock<IPersonContext>();
        // Id == 2 is not a match
        var personEntity = new PersonEntity { Id = 2, FirstName = "John", LastName = "Doe", Notes = Guid.NewGuid().ToString() };

        personContext.Setup(x => x.Persons).ReturnsDbSet(new[] { personEntity });

        var successful = await person.Fetch(personContext.Object, null!);

        Assert.False(successful);
    }

    //[Fact]
    //public async Task PersonModelUnitTests_Insert()
    //{
    //    person.FirstName = "John";
    //    person.LastName = "Doe";
    //    person.Email = "email@email.com";

    //    var personContext = new Mock<IPersonContext>();
    //    // Id == 2 is not a match
    //    var personDbSet = new Mock<DbSet<PersonEntity>>();

    //    personContext.Setup(x => x.DeleteAllPersons()).Returns(Task.CompletedTask);
    //    personContext.Setup(x => x.Persons).Returns(personDbSet.Object);

    //    var personEntity = await person.Insert(personContext.Object);

    //    personContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    //    personContext.Verify(x => x.DeleteAllPersons(), Times.Once);
    //    personDbSet.Verify(x => x.Add(It.Is<PersonEntity>(personEntity => personEntity.Id == person.Id)), Times.Once);

    //    Assert.Equal(person.FirstName, personEntity.FirstName);
    //    Assert.Equal(person.LastName, personEntity.LastName);
    //    Assert.Equal(person.Email, personEntity.Email);
    //}

    //[Fact]
    //public async Task PersonModelUnitTests_Update()
    //{
    //    person.Id = 1;
    //    person.FirstName = "John";
    //    person.LastName = "Doe";
    //    person.Email = "email@email.com";

    //    var personContext = new Mock<IPersonContext>();

    //    var personEntity = new PersonEntity { Id = 1, FirstName = "Not", LastName = "JohnDoe", Notes = Guid.NewGuid().ToString() };

    //    personContext.Setup(x => x.Persons).ReturnsDbSet(new[] { personEntity });

    //    personEntity = await person.Update(personContext.Object);

    //    personContext.Verify(x => x.SaveChangesAsync(default), Times.Once);

    //    Assert.Equal(person.FirstName, personEntity.FirstName);
    //    Assert.Equal(person.LastName, personEntity.LastName);
    //    Assert.Equal(person.Email, personEntity.Email);
    //}

    //[Fact]
    //public async Task PersonModelUnitTests_Delete()
    //{
    //    person.Id = 1;

    //    var personContext = new Mock<IPersonContext>();

    //    var personEntity = new PersonEntity { Id = 1, FirstName = "Not", LastName = "JohnDoe", Notes = Guid.NewGuid().ToString() };

    //    personContext.Setup(x => x.Persons).ReturnsDbSet(new[] { personEntity });

    //    await person.Delete(personContext.Object);

    //    personContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
    //    personContext.Verify(x => x.RemovePerson(personEntity), Times.Once);
    //}
}