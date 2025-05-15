using Moq;

namespace DomainModel.Tests.UnitTests
{
    public class PersonAuthTests
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

            var auth = new PersonAuth(mockUser.Object);

            // Act
            var result = auth.HasAccess();

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

            var auth = new PersonAuth(mockUser.Object);

            // Act
            var result = auth.HasCreate();

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

            var auth = new PersonAuth(mockUser.Object);

            // Act
            var result = auth.HasFetch();

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

            var auth = new PersonAuth(mockUser.Object);

            // Act
            var result = auth.HasInsert();

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

            var auth = new PersonAuth(mockUser.Object);

            // Act
            var result = auth.HasUpdate();

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

            var auth = new PersonAuth(mockUser.Object);

            // Act
            var result = auth.HasDelete();

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
