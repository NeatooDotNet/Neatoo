using KnockOff;
using Moq;
using Neatoo.Rules;

namespace DomainModel.Tests.UnitTests
{
    [KnockOff<UniqueName.IsUniqueName>]
    public partial class UniqueNameRuleTests
    {
        [Fact]
        public async Task Execute_ShouldReturnNone_WhenNameIsUnique()
        {
            // Arrange - KnockOff delegate stub
            var isUniqueStub = new Stubs.IsUniqueName();
            isUniqueStub.Interceptor.OnCall = (ko, id, firstName, lastName) => Task.FromResult(true);

            var rule = new UniqueNameRule(isUniqueStub);

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
            // Arrange - KnockOff delegate stub
            var isUniqueStub = new Stubs.IsUniqueName();
            isUniqueStub.Interceptor.OnCall = (ko, id, firstName, lastName) => Task.FromResult(false);

            var rule = new UniqueNameRule(isUniqueStub);

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
            // Arrange - KnockOff delegate stub (no OnCall configured = will not be called)
            var isUniqueStub = new Stubs.IsUniqueName();
            var rule = new UniqueNameRule(isUniqueStub);

            var mockPerson = new Mock<IPerson>();
            mockPerson.SetupGet(x => x.FirstName).Returns("Jane");
            mockPerson.SetupGet(x => x.LastName).Returns("Doe");
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.FirstName))].IsModified).Returns(false);
            mockPerson.Setup(x => x[It.Is<string>(s => s == nameof(mockPerson.Object.LastName))].IsModified).Returns(false);

            // Act
            var result = await rule.RunRule(mockPerson.Object);

            // Assert
            Assert.Equal(RuleMessages.None, result);
            Assert.False(isUniqueStub.Interceptor.WasCalled);
            Assert.Equal(0, isUniqueStub.Interceptor.CallCount);
        }
    }
}
