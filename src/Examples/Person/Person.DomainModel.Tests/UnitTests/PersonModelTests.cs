using System.Threading.Tasks;
using Moq;
using Neatoo;
using Neatoo.Internal;
using Neatoo.Rules;
using Person.DomainModel;
using Person.Ef;
using Xunit;

namespace Person.DomainModel.Tests.UnitTests
{
    public class PersonModelTests
    {
        private Mock<IPersonDbContext> mockPersonDbContext;
        private Mock<IPersonPhoneModelListFactory> mockPhoneModelListFactory;
        private Mock<IUniqueNameRule> mockUniqueNameRule;
        private Mock<PersonModel> mockPersonModel;
        private PersonModel personModel => mockPersonModel.Object;

        public PersonModelTests()
        {
            mockPersonDbContext = new Mock<IPersonDbContext>();
            mockPhoneModelListFactory = new Mock<IPersonPhoneModelListFactory>();
            mockUniqueNameRule = new Mock<AsyncRuleBase<IPersonModel>>().As<IUniqueNameRule>();
            mockUniqueNameRule.CallBase = true;

            mockPersonModel = new Mock<PersonModel>(new EditBaseServices<PersonModel>(null), mockUniqueNameRule.Object);
            mockPersonModel.Setup(personModel => personModel.IsSavable).Returns(true);
            mockPersonModel.Setup(personModel => personModel.RunRules(RunRulesFlag.All, null)).Returns(Task.CompletedTask);
            mockPersonModel.CallBase = true;
        }

        [Fact]
        public void Adds_UniqueNameRule()
        {
            var personModel = new PersonModel(new EditBaseServices<PersonModel>(null), mockUniqueNameRule.Object);

            // Assert
            mockUniqueNameRule.Verify(x => x.OnRuleAdded(It.IsAny<IRuleManager>(), It.IsAny<uint>()), Times.Once);
        }

        [Fact]
        public async Task Fetch_ShouldReturnTrue_WhenPersonExists()
        {
            // Arrange
            var personEntity = new PersonEntity { FirstName = "John", LastName = "Doe" };
            mockPersonDbContext.Setup(x => x.FindPerson(null)).ReturnsAsync(personEntity);

            var mockPhoneModelList = new Mock<IPersonPhoneModelList>();
            mockPhoneModelListFactory.Setup(x => x.Fetch(personEntity.Phones)).Returns(mockPhoneModelList.Object);

            // Act
            var result = await personModel.Fetch(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.True(result);
            Assert.Equal("John", personModel.FirstName);
            Assert.Equal("Doe", personModel.LastName);
            Assert.Equal(mockPhoneModelList.Object, personModel.PersonPhoneModelList);
        }

        [Fact]
        public async Task Fetch_ShouldReturnFalse_WhenPersonDoesNotExist()
        {
            // Arrange
            mockPersonDbContext.Setup(x => x.FindPerson(null)).ReturnsAsync((PersonEntity?)null);

            var personModel = new PersonModel(new EditBaseServices<PersonModel>(null), mockUniqueNameRule.Object);

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

            var mockPhoneModelList = new Mock<IPersonPhoneModelList>();
            personModel.PersonPhoneModelList = mockPhoneModelList.Object;

            personModel.FirstName = "John";
            personModel.LastName = "Doe";

            // Act
            var result = await personModel.Insert(mockPersonDbContext.Object, mockPhoneModelListFactory.Object);

            // Assert
            Assert.NotNull(result);
            mockPersonModel.Verify(x=> x.RunRules(RunRulesFlag.All, null), Times.Once);
            mockPersonDbContext.Verify(x => x.AddPerson(It.IsAny<PersonEntity>()), Times.Once);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Insert_ShouldReturnNull_WhenModelIsNotSavable()
        {
            // Arrange
            mockPersonModel.Setup(x => x.IsSavable).Returns(false);

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
            var personModel = new PersonModel(new EditBaseServices<PersonModel>(null), mockUniqueNameRule.Object);

            // Act
            await personModel.Delete(mockPersonDbContext.Object);

            // Assert
            mockPersonDbContext.Verify(x => x.DeleteAllPersons(), Times.Once);
            mockPersonDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
    }
}
