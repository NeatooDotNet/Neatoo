using DomainModel.Tests.TestDoubles;
using KnockOff;
using Neatoo;
using Neatoo.Internal;
using Person.Ef;

namespace DomainModel.Tests.UnitTests
{
    // Using KnockOff 10.12.0 for all interfaces
    [KnockOff<IPersonDbContext>]
    [KnockOff<IPersonPhoneListFactory>]
    [KnockOff<IPersonPhoneList>]
    public partial class PersonTests
    {
        private Stubs.IPersonDbContext personDbContextStub;
        private Stubs.IPersonPhoneListFactory phoneListFactoryStub;
        private TestUniqueNameRule testUniqueNameRule;
        private TestPerson testPerson;

        public PersonTests()
        {
            personDbContextStub = new Stubs.IPersonDbContext();
            phoneListFactoryStub = new Stubs.IPersonPhoneListFactory();
            testUniqueNameRule = new TestUniqueNameRule();

            testPerson = new TestPerson(new EntityBaseServices<Person>(null), testUniqueNameRule)
            {
                IsSavableOverride = true
            };
        }

        [Fact]
        public void Adds_UniqueNameRule()
        {
            // Use a fresh rule for this test since the shared one is already used in constructor
            var freshRule = new TestUniqueNameRule();

            var person = new Person(new EntityBaseServices<Person>(null), freshRule);

            // Assert
            Assert.Equal(1, freshRule.OnRuleAddedCallCount);
            Assert.NotNull(freshRule.LastRuleManager);
        }

        [Fact]
        public async Task Fetch_ShouldReturnTrue_WhenPersonExists()
        {
            // Arrange
            var personEntity = new PersonEntity { FirstName = "John", LastName = "Doe" };
            personDbContextStub.FindPerson.OnCall((token) => Task.FromResult<PersonEntity?>(personEntity));

            var phoneListStub = new Stubs.IPersonPhoneList();
            phoneListFactoryStub.Fetch.OnCall((entities, token) => phoneListStub);

            // Act
            var result = await testPerson.Fetch(personDbContextStub, phoneListFactoryStub, CancellationToken.None);

            // Assert
            Assert.True(result);
            Assert.Equal("John", testPerson.FirstName);
            Assert.Equal("Doe", testPerson.LastName);
            Assert.Equal(phoneListStub, testPerson.PersonPhoneList);
        }

        [Fact]
        public async Task Fetch_ShouldReturnFalse_WhenPersonDoesNotExist()
        {
            // Arrange
            personDbContextStub.FindPerson.OnCall((token) => Task.FromResult<PersonEntity?>(null));

            var person = new Person(new EntityBaseServices<Person>(null), testUniqueNameRule);

            // Act
            var result = await person.Fetch(personDbContextStub, phoneListFactoryStub, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Insert_ShouldReturnPersonEntity_WhenModelIsSavable()
        {
            // Arrange
            personDbContextStub.SaveChangesAsync.OnCall((token) => Task.FromResult(1));

            var phoneListStub = new Stubs.IPersonPhoneList();
            testPerson.PersonPhoneList = phoneListStub;
            phoneListFactoryStub.Save.OnCall((target, entities, token) => phoneListStub);

            testPerson.FirstName = "John";
            testPerson.LastName = "Doe";

            // Act
            var result = await testPerson.Insert(personDbContextStub, phoneListFactoryStub, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, testPerson.RunRulesCallCount);
            personDbContextStub.AddPerson.Verify(Times.Once);
            personDbContextStub.SaveChangesAsync.Verify(Times.Once);
        }

        [Fact]
        public async Task Insert_ShouldReturnNull_WhenModelIsNotSavable()
        {
            // Arrange
            testPerson.IsSavableOverride = false;

            // Act
            var result = await testPerson.Insert(personDbContextStub, phoneListFactoryStub, CancellationToken.None);

            // Assert
            Assert.Null(result);
            personDbContextStub.AddPerson.Verify(Times.Never);
            personDbContextStub.SaveChangesAsync.Verify(Times.Never);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenPersonNotFound()
        {
            // Arrange
            personDbContextStub.FindPerson.OnCall((token) => Task.FromResult<PersonEntity?>(null));

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => testPerson.Update(personDbContextStub, phoneListFactoryStub, CancellationToken.None));
        }

        [Fact]
        public async Task Delete_ShouldCallDeleteAllPersons()
        {
            // Arrange
            var person = new Person(new EntityBaseServices<Person>(null), testUniqueNameRule);

            // Act
            await person.Delete(personDbContextStub, CancellationToken.None);

            // Assert
            personDbContextStub.DeleteAllPersons.Verify(Times.Once);
            personDbContextStub.SaveChangesAsync.Verify(Times.Once);
        }
    }
}
