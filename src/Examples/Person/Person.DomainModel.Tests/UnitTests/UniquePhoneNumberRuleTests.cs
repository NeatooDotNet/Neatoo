using KnockOff;
using Neatoo.Rules;

namespace DomainModel.Tests.UnitTests
{
    [KnockOff<IPerson>]
    [KnockOff<IPersonPhoneList>]
    [KnockOff<IPersonPhone>]
    public partial class UniquePhoneNumberRuleTests
    {
        [Fact]
        public async Task Execute_ShouldReturnNone_WhenPhoneNumberIsUnique()
        {
            // Arrange - KnockOff stubs
            var phoneListStub = new Stubs.IPersonPhoneList();
            phoneListStub.GetEnumerator.OnCall(() => ((IEnumerable<IPersonPhone>)Array.Empty<IPersonPhone>()).GetEnumerator());

            var personStub = new Stubs.IPerson();
            personStub.PersonPhoneList.OnGet = () => phoneListStub;

            var phoneStub = new Stubs.IPersonPhone();
            phoneStub.PhoneNumber.OnGet = () => "1234567890";
            phoneStub.ParentPerson.OnGet = () => personStub;

            var rule = new UniquePhoneNumberRule();

            // Act
            var result = await rule.RunRule(phoneStub);

            // Assert
            Assert.Equal(RuleMessages.None, result);
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenPhoneNumberIsNotUnique()
        {
            // Arrange - KnockOff stubs
            var existingPhoneStub = new Stubs.IPersonPhone();
            existingPhoneStub.PhoneNumber.OnGet = () => "1234567890";

            var phoneListStub = new Stubs.IPersonPhoneList();
            phoneListStub.GetEnumerator.OnCall(() => ((IEnumerable<IPersonPhone>)new[] { (IPersonPhone)existingPhoneStub }).GetEnumerator());

            var personStub = new Stubs.IPerson();
            personStub.PersonPhoneList.OnGet = () => phoneListStub;

            var phoneStub = new Stubs.IPersonPhone();
            phoneStub.PhoneNumber.OnGet = () => "1234567890";
            phoneStub.ParentPerson.OnGet = () => personStub;

            var rule = new UniquePhoneNumberRule();

            // Act
            var result = await rule.RunRule(phoneStub);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneNumber) && r.Message == "Phone number must be unique");
        }

        [Fact]
        public async Task Execute_ShouldReturnError_WhenParentPersonIsNull()
        {
            // Arrange - KnockOff stub
            var phoneStub = new Stubs.IPersonPhone();
            phoneStub.PhoneNumber.OnGet = () => "1234567890";
            phoneStub.ParentPerson.OnGet = () => null;

            var rule = new UniquePhoneNumberRule();

            // Act
            var result = await rule.RunRule(phoneStub);

            // Assert
            Assert.NotEqual(RuleMessages.None, result);
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneType) && r.Message == "Parent is null");
            Assert.Contains(result, r => r.PropertyName == nameof(IPersonPhone.PhoneNumber) && r.Message == "Parent is null");
        }
    }
}
