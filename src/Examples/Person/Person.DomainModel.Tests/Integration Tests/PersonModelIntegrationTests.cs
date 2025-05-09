using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Person.Ef;

namespace DomainModel.Tests.IntegrationTests
{
    public class PersonIntegrationTests : IDisposable
    {

        private readonly PersonDbContext personContext;
        private static IServiceCollection serviceCollection;
        private IPersonFactory factory;
        private readonly IServiceProvider serviceProvider;

        public PersonIntegrationTests()
        {
            if (serviceCollection == null)
            {
                serviceCollection = new ServiceCollection();
                serviceCollection.AddDbContext<PersonDbContext>(options => options.UseSqlite(new SqliteConnection("Filename=:memory:")));
                serviceCollection.AddScoped<IPersonDbContext>(cc => cc.GetRequiredService<PersonDbContext>());
                serviceCollection.AddNeatooServices(Neatoo.RemoteFactory.NeatooFactory.Local, typeof(Person).Assembly);
                serviceCollection.AddTransient<IPersonAuth, PersonAuth>();
                var user = new User();
                user.Role = Role.Delete;
                serviceCollection.AddTransient<IUniqueNameRule, UniqueNameRule>();
                serviceCollection.AddTransient<IUniquePhoneNumberRule, UniquePhoneNumberRule>();
                serviceCollection.AddTransient<IUniquePhoneTypeRule, UniquePhoneTypeRule>();
                serviceCollection.AddSingleton<IUser>(user);
            }

            serviceProvider = serviceCollection.BuildServiceProvider();
            personContext = serviceProvider.GetRequiredService<PersonDbContext>();
            personContext.Database.OpenConnection();
            personContext.Database.EnsureCreated();
            factory = serviceProvider.GetRequiredService<IPersonFactory>();
        }

        public void Dispose()
        {
            personContext.Database.CloseConnection();
        }

        private IPerson create()
        {
            var personModel = factory.Create();

            personModel.FirstName = "John";
            personModel.LastName = "Smith";
            personModel.Email = "a@a.com";
            personModel.Notes = "Some notes";

            var phoneNumber = personModel.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "1234567890";
            phoneNumber = personModel.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Work;
            phoneNumber.PhoneNumber = "0987654321";

            return personModel;
        }

        [Fact]
        public async Task PersonTests_Create()
        {
            var personModel = create();

            Assert.True(personModel.IsValid);
        }

        [Fact]
        public async Task PersonTests_Fetch()
        {
            var personModel = create();

            // Act
            var saved = await personModel.Save();
            var result = await factory.Fetch();

            // Assert
            var person = personContext.Persons.Single();
            Assert.Equal(personModel.FirstName, result.FirstName);
            Assert.Equal(personModel.LastName, result.LastName);
            Assert.Equal(personModel.Email, result.Email);
            Assert.Equal(personModel.Notes, result.Notes);
            Assert.Equal(2, result.PersonPhoneList.Count);
            Assert.Equal("1234567890", result.PersonPhoneList[0].PhoneNumber);
            Assert.Equal(PhoneType.Home, (PhoneType)result.PersonPhoneList[0].PhoneType);
            Assert.Equal("0987654321", result.PersonPhoneList[1].PhoneNumber);
            Assert.Equal(PhoneType.Work, (PhoneType)result.PersonPhoneList[1].PhoneType);
        }

        [Fact]
        public async Task PersonTests_Insert()
        {
            var personModel = create();

            // Act
            var result = await personModel.Save();

            // Assert
            var person = personContext.Persons.Single();
            Assert.Equal(personModel.FirstName, person.FirstName);
            Assert.Equal(personModel.LastName, person.LastName);
            Assert.Equal(personModel.Email, person.Email);
            Assert.Equal(personModel.Notes, person.Notes);
            Assert.Equal(2, person.Phones.Count);
            Assert.Equal("1234567890", person.Phones[0].PhoneNumber);
            Assert.Equal(PhoneType.Home, (PhoneType) person.Phones[0].PhoneType);
            Assert.Equal("0987654321", person.Phones[1].PhoneNumber);
            Assert.Equal(PhoneType.Work, (PhoneType) person.Phones[1].PhoneType);
        }

        [Fact]
        public async Task PersonTests_Update()
        {
            var personModel = create();

            // Act
            var result = (IPerson) await personModel.Save();

            result.FirstName = "Jane";
            result.LastName = "Doe";
            result.Email = "1234567890";
            result.PersonPhoneList[0].PhoneNumber = "1111111111";
            result.PersonPhoneList[0].PhoneType = PhoneType.Mobile;
            result.PersonPhoneList[1].Delete();
            result.Notes = "Updated notes";

            result = (IPerson)await result.Save();

            // Assert
            var person = personContext.Persons.Single();
            Assert.Equal(result.FirstName, person.FirstName);
            Assert.Equal(result.LastName, person.LastName);
            Assert.Equal(result.Email, person.Email);
            Assert.Equal(result.Notes, person.Notes);
            Assert.Equal(1, person.Phones.Count);
            Assert.Equal("1111111111", person.Phones[0].PhoneNumber);
            Assert.Equal(PhoneType.Mobile, (PhoneType)person.Phones[0].PhoneType);
        }

        [Fact]
        public async Task UniqueNameRule_ShouldReturnError_WhenNameIsNotUnique()
        {
            // Seed data
            personContext.Persons.Add(new PersonEntity { FirstName = "John", LastName = "Doe" });
            await personContext.SaveChangesAsync();

            var personModel = factory.Create();
            personModel.FirstName = "John";
            personModel.LastName = "Doe";

            await personModel.WaitForTasks();

            // Assert
            Assert.False(personModel.IsValid);
            Assert.False(personModel.IsSavable);
            Assert.Equal("First and Last name combination is not unique", personModel.PropertyMessages.Select(_ => _.Message).Distinct().Single());
        }

        [Fact]
        public async Task UniquePhoneTypeRule_ShouldReturnError_WhenPhoneTypeIsNotUnique()
        {
            var personModel = factory.Create();

            var phoneNumber = personModel.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "1234567890";

            await personModel.WaitForTasks();

            Assert.True(personModel.IsValid);

            phoneNumber = personModel.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "0987654321";

            Assert.False(personModel.IsValid);

            Assert.Contains("Phone type must be unique", personModel.PropertyMessages.Select(m => m.Message));
        }

        [Fact]
        public async Task UniquePhoneNumberRule_ShouldReturnError_WhenPhoneNumberIsNotUnique()
        {
            var personModel = factory.Create();

            var phoneNumber = personModel.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "1234567890";

            await personModel.WaitForTasks();

            Assert.True(personModel.IsValid);

            phoneNumber = personModel.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Mobile;
            phoneNumber.PhoneNumber = "1234567890";

            Assert.False(personModel.IsValid);

            Assert.Contains("Phone number must be unique", personModel.PropertyMessages.Select(m => m.Message));
        }
    }
}
