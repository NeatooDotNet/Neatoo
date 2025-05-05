using Moq;
using Neatoo.Rules;

namespace DomainModel.Tests.UnitTests
{
    public class UniquePhoneNumberRuleTests
    {
        [Fact]
        public async Task Execute_ShouldReturnNone_WhenPhoneNumberIsUnique()
        {
            // Arrange
            var mockParentPerson = new Mock<IPerson>();
            var mockPhoneModelList = new Mock<IPersonPhoneList>();

            mockPhoneModelList
                .Setup(x => x.GetEnumerator())
                .Returns((Array.Empty<IPersonPhone>()).AsEnumerable().GetEnumerator());

            mockParentPerson
                .SetupGet(x => x.PersonPhoneList)
                .Returns(mockPhoneModelList.Object);

            var mockPhoneModel = new Mock<IPersonPhone>();
            mockPhoneModel.SetupGet(x => x.PhoneNumber).Returns("1234567890");
            mockPhoneModel.SetupGet(x => x.ParentPerson).Returns(mockParentPerson.Object);

            var rule = new UniquePhoneNumberRule();

            // Act
            var result = await rule.RunRule(mockPhoneModel.Object);

            // Assert
            Assert.Equal(RuleMessages.None, result);
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenPhoneNumberIsNotUnique()
        {
            // Arrange
            var mockParentPerson = new Mock<IPerson>();
            var mockPhoneModelList = new Mock<IPersonPhoneList>();

            var existingPhoneModel = new Mock<IPersonPhone>();
            existingPhoneModel.SetupGet(x => x.PhoneNumber).Returns("1234567890");

            mockPhoneModelList
                .Setup(x => x.GetEnumerator())
                .Returns((new[] { existingPhoneModel.Object }).AsEnumerable().GetEnumerator());

            mockParentPerson
                .SetupGet(x => x.PersonPhoneList)
                .Returns(mockPhoneModelList.Object);

            var mockPhoneModel = new Mock<IPersonPhone>();
            mockPhoneModel.SetupGet(x => x.PhoneNumber).Returns("1234567890");
            mockPhoneModel.SetupGet(x => x.ParentPerson).Returns(mockParentPerson.Object);

            var rule = new UniquePhoneNumberRule();

            // Act
            var result = await rule.RunRule(mockPhoneModel.Object);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneNumber) && r.Message == "Phone number must be unique");
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenParentPersonIsNull()
        {
            // Arrange
            var mockPhoneModel = new Mock<IPersonPhone>();
            mockPhoneModel.SetupGet(x => x.PhoneNumber).Returns("1234567890");
            mockPhoneModel.SetupGet(x => x.ParentPerson).Returns((IPerson?)null);

            var rule = new UniquePhoneNumberRule();

            // Act
            var result = await rule.RunRule(mockPhoneModel.Object);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneType) && r.Message == "Parent is null");
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneNumber) && r.Message == "Parent is null");
        }
    }
}
