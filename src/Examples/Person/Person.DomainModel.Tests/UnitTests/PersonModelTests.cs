using Moq;
using Neatoo;
using Neatoo.Internal;
using Neatoo.Rules;
using Person.Ef;

namespace DomainModel.Tests.UnitTests
{
    public class PersonTests
    {
        private Mock<IPersonDbContext> mockPersonDbContext;
        private Mock<IPersonPhoneListFactory> mockPhoneModelListFactory;
        private Mock<IUniqueNameRule> mockUniqueNameRule;
        private Mock<Person> mockPerson;
        private Person personModel => mockPerson.Object;

        public PersonTests()
        {
            mockPersonDbContext = new Mock<IPersonDbContext>();
            mockPhoneModelListFactory = new Mock<IPersonPhoneListFactory>();
            mockUniqueNameRule = new Mock<AsyncRuleBase<IPerson>>().As<IUniqueNameRule>();
            mockUniqueNameRule.CallBase = true;

            mockPerson = new Mock<Person>(new EntityBaseServices<Person>(null), mockUniqueNameRule.Object);
            mockPerson.Setup(personModel => personModel.IsSavable).Returns(true);
            mockPerson.Setup(personModel => personModel.RunRules(RunRulesFlag.All, null)).Returns(Task.CompletedTask);
            mockPerson.CallBase = true;
        }

        [Fact]
        public void Adds_UniqueNameRule()
        {
            var personModel = new Person(new EntityBaseServices<Person>(null), mockUniqueNameRule.Object);

            // Assert
            mockUniqueNameRule.Verify(x => x.OnRuleAdded(It.IsAny<IRuleManager>(), It.IsAny<uint>()), Times.Once);
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
            var result = await personModel.Fetch(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.True(result);
            Assert.Equal("John", personModel.FirstName);
            Assert.Equal("Doe", personModel.LastName);
            Assert.Equal(mockPhoneModelList.Object, personModel.PersonPhoneList);
        }

        [Fact]
        public async Task Fetch_ShouldReturnFalse_WhenPersonDoesNotExist()
        {
            // Arrange
            mockPersonDbContext.Setup(x => x.FindPerson(null)).ReturnsAsync((PersonEntity?)null);

            var personModel = new Person(new EntityBaseServices<Person>(null), mockUniqueNameRule.Object);

            // Act
            var result = await personModel.Fetch(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

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
            personModel.PersonPhoneList = mockPhoneModelList.Object;

            personModel.FirstName = "John";
            personModel.LastName = "Doe";

            // Act
            var result = await personModel.Insert(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.NotNull(result);
            mockPerson.Verify(x=> x.RunRules(RunRulesFlag.All, null), Times.Once);
            mockPersonDbContext.Verify(x => x.AddPerson(It.IsAny<PersonEntity>()), Times.Once);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Insert_ShouldReturnNull_WhenModelIsNotSavable()
        {
            // Arrange
            mockPerson.Setup(x => x.IsSavable).Returns(false);

            // Act
            var result = await personModel.Insert(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.Null(result);
            mockPersonDbContext.Verify(x => x.AddPerson(It.IsAny<PersonEntity>()), Times.Never);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenPersonNotFound()
        {
            // Arrange
            mockPersonDbContext.Setup(x => x.FindPerson(1)).ReturnsAsync((PersonEntity?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => personModel.Update(mockPersonDbContext.Object, mockPhoneModelListFactory.Object));
        }

        [Fact]
        public async Task Delete_ShouldCallDeleteAllPersons()
        {
            // Arrange
            var personModel = new Person(new EntityBaseServices<Person>(null), mockUniqueNameRule.Object);

            // Act
            await personModel.Delete(mockPersonDbContext.Object);

            // Assert
            mockPersonDbContext.Verify(x => x.DeleteAllPersons(), Times.Once);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
    }
}
