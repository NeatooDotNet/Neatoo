using DomainModel.Tests.TestDoubles;
using Moq;
using Neatoo;
using Neatoo.Internal;
using Person.Ef;

namespace DomainModel.Tests.UnitTests
{
    public class PersonTests
    {
        private Mock<IPersonDbContext> mockPersonDbContext;
        private Mock<IPersonPhoneListFactory> mockPhoneModelListFactory;
        private TestUniqueNameRule testUniqueNameRule;
        private TestPerson testPerson;

        public PersonTests()
        {
            mockPersonDbContext = new Mock<IPersonDbContext>();
            mockPhoneModelListFactory = new Mock<IPersonPhoneListFactory>();
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
            mockPersonDbContext.Setup(x => x.FindPerson(null)).ReturnsAsync(personEntity);

            var mockPhoneModelList = new Mock<IPersonPhoneList>();
            mockPhoneModelListFactory.Setup(x => x.Fetch(personEntity.Phones)).Returns(mockPhoneModelList.Object);

            // Act
            var result = await testPerson.Fetch(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.True(result);
            Assert.Equal("John", testPerson.FirstName);
            Assert.Equal("Doe", testPerson.LastName);
            Assert.Equal(mockPhoneModelList.Object, testPerson.PersonPhoneList);
        }

        [Fact]
        public async Task Fetch_ShouldReturnFalse_WhenPersonDoesNotExist()
        {
            // Arrange
            mockPersonDbContext.Setup(x => x.FindPerson(null)).ReturnsAsync((PersonEntity?)null);

            var person = new Person(new EntityBaseServices<Person>(null), testUniqueNameRule);

            // Act
            var result = await person.Fetch(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Insert_ShouldReturnPersonEntity_WhenModelIsSavable()
        {
            // Arrange
            var personEntity = new PersonEntity();
            mockPersonDbContext.Setup(x => x.AddPerson(It.IsAny<PersonEntity>()));
            mockPersonDbContext.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

            var mockPhoneModelList = new Mock<IPersonPhoneList>();
            testPerson.PersonPhoneList = mockPhoneModelList.Object;

            testPerson.FirstName = "John";
            testPerson.LastName = "Doe";

            // Act
            var result = await testPerson.Insert(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, testPerson.RunRulesCallCount);
            mockPersonDbContext.Verify(x => x.AddPerson(It.IsAny<PersonEntity>()), Times.Once);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Insert_ShouldReturnNull_WhenModelIsNotSavable()
        {
            // Arrange
            testPerson.IsSavableOverride = false;

            // Act
            var result = await testPerson.Insert(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.Null(result);
            mockPersonDbContext.Verify(x => x.AddPerson(It.IsAny<PersonEntity>()), Times.Never);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenPersonNotFound()
        {
            // Arrange
            mockPersonDbContext.Setup(x => x.FindPerson(It.IsAny<Guid?>())).ReturnsAsync((PersonEntity?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => testPerson.Update(mockPersonDbContext.Object, mockPhoneModelListFactory.Object));
        }

        [Fact]
        public async Task Delete_ShouldCallDeleteAllPersons()
        {
            // Arrange
            var person = new Person(new EntityBaseServices<Person>(null), testUniqueNameRule);

            // Act
            await person.Delete(mockPersonDbContext.Object);

            // Assert
            mockPersonDbContext.Verify(x => x.DeleteAllPersons(), Times.Once);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
    }
}
