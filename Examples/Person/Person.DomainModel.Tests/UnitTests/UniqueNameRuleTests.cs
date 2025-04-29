using System.Threading;
using System.Threading.Tasks;
using Moq;
using Neatoo.Rules;
using Person.DomainModel;
using Xunit;

namespace Person.DomainModel.Tests.UnitTests
{
    public class UniqueNameRuleTests
    {
        [Fact]
        public async Task Execute_ShouldReturnNone_WhenNameIsUnique()
        {
            // Arrange
            var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
            mockIsUniqueName
                .Setup(x => x(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var rule = new UniqueNameRule(mockIsUniqueName.Object);

            var mockPersonModel = new Mock<IPersonModel>();
            mockPersonModel.SetupGet(x => x.FirstName).Returns("Jane");
            mockPersonModel.SetupGet(x => x.LastName).Returns("Doe");
            mockPersonModel.Setup(x => x[It.Is<string>(s => s == nameof(mockPersonModel.Object.FirstName))].IsModified).Returns(true);
            mockPersonModel.Setup(x => x[It.Is<string>(s => s == nameof(mockPersonModel.Object.LastName))].IsModified).Returns(true);

            // Act
            var result = await rule.RunRule(mockPersonModel.Object);

            // Assert
            Assert.Equal(RuleMessages.None, result);
        }

        [Fact]
        public async Task Execute_ShouldReturnErrorMessages_WhenNameIsNotUnique()
        {
            // Arrange
            var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
            mockIsUniqueName
                .Setup(x => x(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var rule = new UniqueNameRule(mockIsUniqueName.Object);

            var mockPersonModel = new Mock<IPersonModel>();
            mockPersonModel.SetupGet(x => x.FirstName).Returns("John");
            mockPersonModel.SetupGet(x => x.LastName).Returns("Doe");
            mockPersonModel.Setup(x => x[It.Is<string>(s => s == nameof(mockPersonModel.Object.FirstName))].IsModified).Returns(true);
            mockPersonModel.Setup(x => x[It.Is<string>(s => s == nameof(mockPersonModel.Object.LastName))].IsModified).Returns(true);

            // Act
            var result = await rule.RunRule(mockPersonModel.Object);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(mockPersonModel.Object.FirstName) && r.Message == "First and Last name combination is not unique");
            Assert.Contains(result, r => r.PropertyName == nameof(mockPersonModel.Object.LastName) && r.Message == "First and Last name combination is not unique");
        }

        [Fact]
        public async Task Execute_ShouldReturnNone_WhenNameIsNotModified()
        {
            // Arrange
            var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
            var rule = new UniqueNameRule(mockIsUniqueName.Object);

            var mockPersonModel = new Mock<IPersonModel>();
            mockPersonModel.SetupGet(x => x.FirstName).Returns("Jane");
            mockPersonModel.SetupGet(x => x.LastName).Returns("Doe");
            mockPersonModel.Setup(x => x[It.Is<string>(s => s == nameof(mockPersonModel.Object.FirstName))].IsModified).Returns(false);
            mockPersonModel.Setup(x => x[It.Is<string>(s => s == nameof(mockPersonModel.Object.LastName))].IsModified).Returns(false);

            // Act
            var result = await rule.RunRule(mockPersonModel.Object);

            // Assert
            Assert.Equal(RuleMessages.None, result);
            mockIsUniqueName.Verify(x => x(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
