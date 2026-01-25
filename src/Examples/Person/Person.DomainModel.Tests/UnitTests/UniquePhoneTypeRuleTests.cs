using KnockOff;
using Neatoo.Rules;

namespace DomainModel.Tests.UnitTests
{
    [KnockOff<IPerson>]
    [KnockOff<IPersonPhoneList>]
    [KnockOff<IPersonPhone>]
    public partial class UniquePhoneTypeRuleTests
    {
        [Fact]
        public async Task Execute_ShouldReturnNone_WhenPhoneTypeIsUnique()
        {
            // Arrange - KnockOff stubs
            var phoneListStub = new Stubs.IPersonPhoneList();
            phoneListStub.GetEnumerator.OnCall(() => ((IEnumerable<IPersonPhone>)Array.Empty<IPersonPhone>()).GetEnumerator());

            var personStub = new Stubs.IPerson();
            personStub.PersonPhoneList.OnGet = () => phoneListStub;

            var phoneStub = new Stubs.IPersonPhone();
            phoneStub.PhoneType.OnGet = () => PhoneType.Mobile;
            phoneStub.ParentPerson.OnGet = () => personStub;

            var rule = new UniquePhoneTypeRule();

            // Act
            var result = await rule.RunRule(phoneStub);

            // Assert
            Assert.Equal(RuleMessages.None, result);
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenPhoneTypeIsNotUnique()
        {
            // Arrange - KnockOff stubs
            var existingPhoneStub = new Stubs.IPersonPhone();
            existingPhoneStub.PhoneType.OnGet = () => PhoneType.Mobile;

            var phoneListStub = new Stubs.IPersonPhoneList();
            phoneListStub.GetEnumerator.OnCall(() => ((IEnumerable<IPersonPhone>)new[] { (IPersonPhone)existingPhoneStub }).GetEnumerator());

            var personStub = new Stubs.IPerson();
            personStub.PersonPhoneList.OnGet = () => phoneListStub;

            var phoneStub = new Stubs.IPersonPhone();
            phoneStub.PhoneType.OnGet = () => PhoneType.Mobile;
            phoneStub.ParentPerson.OnGet = () => personStub;

            var rule = new UniquePhoneTypeRule();

            // Act
            var result = await rule.RunRule(phoneStub);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneType) && r.Message == "Phone type must be unique");
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenParentPersonIsNull()
        {
            // Arrange - KnockOff stub
            var phoneStub = new Stubs.IPersonPhone();
            phoneStub.PhoneType.OnGet = () => PhoneType.Mobile;
            phoneStub.ParentPerson.OnGet = () => null;

            var rule = new UniquePhoneTypeRule();

            // Act
            var result = await rule.RunRule(phoneStub);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneType) && r.Message == "Parent is null");
        }
    }
}
