using Moq;
using Neatoo.Rules;

namespace DomainModel.Tests.UnitTests
{
    public class UniqueNameRuleTests
    {
        [Fact]
        public async Task Execute_ShouldReturnNone_WhenNameIsUnique()
        {
            // Arrange
            var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
            mockIsUniqueName
                .Setup(x => x(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var rule = new UniqueNameRule(mockIsUniqueName.Object);

            var mockPerson = new Mock<IPerson>();
            mockPerson.SetupGet(x => x.FirstName).Returns("Jane");
            mockPerson.SetupGet(x => x.LastName).Returns("Doe");
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.FirstName))].IsModified).Returns(true);
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.LastName))].IsModified).Returns(true);

            // Act
            var result = await rule.RunRule(mockPerson.Object);

            // Assert
            Assert.Equal(RuleMessages.None, result);
        }

        [Fact]
        public async Task Execute_ShouldReturnErrorMessages_WhenNameIsNotUnique()
        {
            // Arrange
            var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
            mockIsUniqueName
                .Setup(x => x(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var rule = new UniqueNameRule(mockIsUniqueName.Object);

            var mockPerson = new Mock<IPerson>();
            mockPerson.SetupGet(x => x.FirstName).Returns("John");
            mockPerson.SetupGet(x => x.LastName).Returns("Doe");
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.FirstName))].IsModified).Returns(true);
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.LastName))].IsModified).Returns(true);

            // Act
            var result = await rule.RunRule(mockPerson.Object);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(mockPerson.Object.FirstName) && r.Message == "First and Last name combination is not unique");
            Assert.Contains(result, r => r.PropertyName == nameof(mockPerson.Object.LastName) && r.Message == "First and Last name combination is not unique");
        }

        [Fact]
        public async Task Execute_ShouldReturnNone_WhenNameIsNotModified()
        {
            // Arrange
            var mockIsUniqueName = new Mock<UniqueName.IsUniqueName>();
            var rule = new UniqueNameRule(mockIsUniqueName.Object);

            var mockPerson = new Mock<IPerson>();
            mockPerson.SetupGet(x => x.FirstName).Returns("Jane");
            mockPerson.SetupGet(x => x.LastName).Returns("Doe");
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.FirstName))].IsModified).Returns(false);
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.LastName))].IsModified).Returns(false);

            // Act
            var result = await rule.RunRule(mockPerson.Object);

            // Assert
            Assert.Equal(RuleMessages.None, result);
            mockIsUniqueName.Verify(x => x(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
