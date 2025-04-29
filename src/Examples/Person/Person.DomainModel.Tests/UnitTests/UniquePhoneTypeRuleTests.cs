using System.Linq;
using Moq;
using Neatoo.Rules;
using Person.DomainModel;
using Xunit;

namespace Person.DomainModel.Tests.UnitTests
{
    public class UniquePhoneTypeRuleTests
    {
        [Fact]
        public async Task Execute_ShouldReturnNone_WhenPhoneTypeIsUnique()
        {
            // Arrange
            var mockParentPersonModel = new Mock<IPersonModel>();
            var mockPhoneModelList = new Mock<IPersonPhoneModelList>();

            mockPhoneModelList
                .Setup(x => x.GetEnumerator())
                .Returns((Array.Empty<IPersonPhoneModel>()).AsEnumerable().GetEnumerator());

            mockParentPersonModel
                .SetupGet(x => x.PersonPhoneModelList)
                .Returns(mockPhoneModelList.Object);

            var mockPhoneModel = new Mock<IPersonPhoneModel>();
            mockPhoneModel.SetupGet(x => x.PhoneType).Returns(PhoneType.Mobile);
            mockPhoneModel.SetupGet(x => x.ParentPersonModel).Returns(mockParentPersonModel.Object);

            var rule = new UniquePhoneTypeRule();

            // Act
            var result = await rule.RunRule(mockPhoneModel.Object);

            // Assert
            Assert.Equal(RuleMessages.None, result);
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenPhoneTypeIsNotUnique()
        {
            // Arrange
            var mockParentPersonModel = new Mock<IPersonModel>();
            var mockPhoneModelList = new Mock<IPersonPhoneModelList>();

            var existingPhoneModel = new Mock<IPersonPhoneModel>();
            existingPhoneModel.SetupGet(x => x.PhoneType).Returns(PhoneType.Mobile);

            mockPhoneModelList
                .Setup(x => x.GetEnumerator())
                .Returns((new[] { existingPhoneModel.Object }).AsEnumerable().GetEnumerator());

            mockParentPersonModel
                .SetupGet(x => x.PersonPhoneModelList)
                .Returns(mockPhoneModelList.Object);

            var mockPhoneModel = new Mock<IPersonPhoneModel>();
            mockPhoneModel.SetupGet(x => x.PhoneType).Returns(PhoneType.Mobile);
            mockPhoneModel.SetupGet(x => x.ParentPersonModel).Returns(mockParentPersonModel.Object);

            var rule = new UniquePhoneTypeRule();

            // Act
            var result = await rule.RunRule(mockPhoneModel.Object);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhoneModel.PhoneType) && r.Message == "Phone type must be unique");
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenParentPersonModelIsNull()
        {
            // Arrange
            var mockPhoneModel = new Mock<IPersonPhoneModel>();
            mockPhoneModel.SetupGet(x => x.PhoneType).Returns(PhoneType.Mobile);
            mockPhoneModel.SetupGet(x => x.ParentPersonModel).Returns((IPersonModel?)null);

            var rule = new UniquePhoneTypeRule();

            // Act
            var result = await rule.RunRule(mockPhoneModel.Object);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhoneModel.PhoneType) && r.Message == "Parent is null");
        }
    }
}
