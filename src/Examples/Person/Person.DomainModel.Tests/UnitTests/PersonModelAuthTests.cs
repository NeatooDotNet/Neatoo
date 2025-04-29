using Moq;
using Person.DomainModel;
using Xunit;

namespace Person.DomainModel.Tests.UnitTests
{
    public class PersonModelAuthTests
    {
        [Theory]
        [InlineData(Role.None, false)]
        [InlineData(Role.Create, true)]
        [InlineData(Role.Fetch, true)]
        [InlineData(Role.Update, true)]
        [InlineData(Role.Delete, true)]
        public void CanAccess_ShouldReturnExpectedResult(Role userRole, bool expectedResult)
        {
            // Arrange
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet(u => u.Role).Returns(userRole);

            var auth = new PersonModelAuth(mockUser.Object);

            // Act
            var result = auth.CanAccess();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(Role.None, false)]
        [InlineData(Role.Create, true)]
        [InlineData(Role.Fetch, true)]
        [InlineData(Role.Update, true)]
        [InlineData(Role.Delete, true)]
        public void CanCreate_ShouldReturnExpectedResult(Role userRole, bool expectedResult)
        {
            // Arrange
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet(u => u.Role).Returns(userRole);

            var auth = new PersonModelAuth(mockUser.Object);

            // Act
            var result = auth.CanCreate();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(Role.None, false)]
        [InlineData(Role.Create, false)]
        [InlineData(Role.Fetch, true)]
        [InlineData(Role.Update, true)]
        [InlineData(Role.Delete, true)]
        public void CanFetch_ShouldReturnExpectedResult(Role userRole, bool expectedResult)
        {
            // Arrange
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet(u => u.Role).Returns(userRole);

            var auth = new PersonModelAuth(mockUser.Object);

            // Act
            var result = auth.CanFetch();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(Role.None, false)]
        [InlineData(Role.Create, false)]
        [InlineData(Role.Fetch, false)]
        [InlineData(Role.Update, true)]
        [InlineData(Role.Delete, true)]
        public void CanInsert_ShouldReturnExpectedResult(Role userRole, bool expectedResult)
        {
            // Arrange
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet(u => u.Role).Returns(userRole);

            var auth = new PersonModelAuth(mockUser.Object);

            // Act
            var result = auth.CanInsert();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(Role.None, false)]
        [InlineData(Role.Create, false)]
        [InlineData(Role.Fetch, false)]
        [InlineData(Role.Update, true)]
        [InlineData(Role.Delete, true)]
        public void CanUpdate_ShouldReturnExpectedResult(Role userRole, bool expectedResult)
        {
            // Arrange
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet(u => u.Role).Returns(userRole);

            var auth = new PersonModelAuth(mockUser.Object);

            // Act
            var result = auth.CanUpdate();

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(Role.None, false)]
        [InlineData(Role.Create, false)]
        [InlineData(Role.Fetch, false)]
        [InlineData(Role.Update, false)]
        [InlineData(Role.Delete, true)]
        public void CanDelete_ShouldReturnExpectedResult(Role userRole, bool expectedResult)
        {
            // Arrange
            var mockUser = new Mock<IUser>();
            mockUser.SetupGet(u => u.Role).Returns(userRole);

            var auth = new PersonModelAuth(mockUser.Object);

            // Act
            var result = auth.CanDelete();

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
