using KnockOff;
using Neatoo;
using Neatoo.Rules;

namespace DomainModel.Tests.UnitTests
{
    [KnockOff<UniqueName.IsUniqueName>]
    [KnockOff<IPerson>]
    [KnockOff<IEntityProperty>]
    public partial class UniqueNameRuleTests
    {
        private Stubs.IPerson CreatePersonStub(string firstName, string lastName, bool isModified)
        {
            var firstNameProp = new Stubs.IEntityProperty();
            firstNameProp.IsModified.OnGet = (ko) => isModified;

            var lastNameProp = new Stubs.IEntityProperty();
            lastNameProp.IsModified.OnGet = (ko) => isModified;

            var personStub = new Stubs.IPerson();
            personStub.FirstName.OnGet = (ko) => firstName;
            personStub.LastName.OnGet = (ko) => lastName;
            personStub.StringIndexer.OnGet = (ko, propName) => propName switch
            {
                nameof(IPerson.FirstName) => firstNameProp,
                nameof(IPerson.LastName) => lastNameProp,
                _ => new Stubs.IEntityProperty()
            };

            return personStub;
        }

        [Fact]
        public async Task Execute_ShouldReturnNone_WhenNameIsUnique()
        {
            // Arrange - KnockOff stubs
            var isUniqueStub = new Stubs.IsUniqueName();
            isUniqueStub.Interceptor.OnCall = (ko, id, firstName, lastName, token) => Task.FromResult(true);

            var rule = new UniqueNameRule(isUniqueStub);
            var personStub = CreatePersonStub("Jane", "Doe", isModified: true);

            // Act
            var result = await rule.RunRule(personStub);

            // Assert
            Assert.Equal(RuleMessages.None, result);
        }

        [Fact]
        public async Task Execute_ShouldReturnErrorMessages_WhenNameIsNotUnique()
        {
            // Arrange - KnockOff stubs
            var isUniqueStub = new Stubs.IsUniqueName();
            isUniqueStub.Interceptor.OnCall = (ko, id, firstName, lastName, token) => Task.FromResult(false);

            var rule = new UniqueNameRule(isUniqueStub);
            var personStub = CreatePersonStub("John", "Doe", isModified: true);

            // Act
            var result = await rule.RunRule(personStub);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPerson.FirstName) && r.Message == "First and Last name combination is not unique");
            Assert.Contains(result, r => r.PropertyName == nameof(IPerson.LastName) && r.Message == "First and Last name combination is not unique");
        }

        [Fact]
        public async Task Execute_ShouldReturnNone_WhenNameIsNotModified()
        {
            // Arrange - KnockOff stubs (no OnCall configured = will not be called)
            var isUniqueStub = new Stubs.IsUniqueName();
            var rule = new UniqueNameRule(isUniqueStub);
            var personStub = CreatePersonStub("Jane", "Doe", isModified: false);

            // Act
            var result = await rule.RunRule(personStub);

            // Assert
            Assert.Equal(RuleMessages.None, result);
            Assert.False(isUniqueStub.Interceptor.WasCalled);
            Assert.Equal(0, isUniqueStub.Interceptor.CallCount);
        }
    }
}
