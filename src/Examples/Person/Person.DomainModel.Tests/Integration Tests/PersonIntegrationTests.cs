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
                serviceCollection.AddNeatooServices(Neatoo.RemoteFactory.NeatooFactory.StandAlone, typeof(Person).Assembly);
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
            var person = factory.Create();

            person.FirstName = "John";
            person.LastName = "Smith";
            person.Email = "a@a.com";
            person.Notes = "Some notes";

            var phoneNumber = person.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "1234567890";
            phoneNumber = person.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Work;
            phoneNumber.PhoneNumber = "0987654321";

            return person;
        }

        [Fact]
        public async Task PersonTests_Create()
        {
            var person = create();

            Assert.True(person.IsValid);
        }

        [Fact]
        public async Task PersonTests_Fetch()
        {
            var person = create();

            // Act
            var saved = await person.Save();
            var result = await factory.Fetch();

            // Assert
            var personEntity = personContext.Persons.Single();
            Assert.Equal(personEntity.FirstName, result.FirstName);
            Assert.Equal(personEntity.LastName, result.LastName);
            Assert.Equal(personEntity.Email, result.Email);
            Assert.Equal(personEntity.Notes, result.Notes);
            Assert.Equal(2, result.PersonPhoneList.Count);
            Assert.Equal("1234567890", result.PersonPhoneList[0].PhoneNumber);
            Assert.Equal(PhoneType.Home, (PhoneType)result.PersonPhoneList[0].PhoneType);
            Assert.Equal("0987654321", result.PersonPhoneList[1].PhoneNumber);
            Assert.Equal(PhoneType.Work, (PhoneType)result.PersonPhoneList[1].PhoneType);
        }

        [Fact]
        public async Task PersonTests_Insert()
        {
            var person = create();

            // Act
            var result = await person.Save();

            // Assert
            var personEntity = personContext.Persons.Single();
            Assert.Equal(personEntity.FirstName, personEntity.FirstName);
            Assert.Equal(personEntity.LastName, personEntity.LastName);
            Assert.Equal(personEntity.Email, personEntity.Email);
            Assert.Equal(personEntity.Notes, personEntity.Notes);
            Assert.Equal(2, personEntity.Phones.Count);
            var phones = personEntity.Phones.ToList();
            Assert.Equal("1234567890", phones[0].PhoneNumber);
            Assert.Equal(PhoneType.Home, (PhoneType) phones[0].PhoneType);
            Assert.Equal("0987654321", phones[1].PhoneNumber);
            Assert.Equal(PhoneType.Work, (PhoneType) phones[1].PhoneType);
        }

        [Fact]
        public async Task PersonTests_Update()
        {
            var person = create();

            // Act
            var result = (IPerson) await person.Save();

            result.FirstName = "Jane";
            result.LastName = "Doe";
            result.Email = "1234567890";
            result.PersonPhoneList[0].PhoneNumber = "1111111111";
            result.PersonPhoneList[0].PhoneType = PhoneType.Mobile;
            result.PersonPhoneList[1].Delete();
            result.Notes = "Updated notes";

            result = (IPerson)await result.Save();

            // Assert
            var personEntity = personContext.Persons.Single();
            Assert.Equal(result.FirstName, personEntity.FirstName);
            Assert.Equal(result.LastName, personEntity.LastName);
            Assert.Equal(result.Email, personEntity.Email);
            Assert.Equal(result.Notes, personEntity.Notes);
            Assert.Equal(1, personEntity.Phones.Count);
            var phones = personEntity.Phones.ToList();
            Assert.Equal("1111111111", phones[0].PhoneNumber);
            Assert.Equal(PhoneType.Mobile, (PhoneType)phones[0].PhoneType);
        }

        [Fact]
        public async Task UniqueNameRule_ShouldReturnError_WhenNameIsNotUnique()
        {
            // Seed data
            personContext.Persons.Add(new PersonEntity { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe" });
            await personContext.SaveChangesAsync();

            var person = factory.Create();
            person.FirstName = "John";
            person.LastName = "Doe";

            await person.WaitForTasks();

            // Assert
            Assert.False(person.IsValid);
            Assert.False(person.IsSavable);
            Assert.Equal("First and Last name combination is not unique", person.PropertyMessages.Select(_ => _.Message).Distinct().Single());
        }

        [Fact]
        public async Task UniquePhoneTypeRule_ShouldReturnError_WhenPhoneTypeIsNotUnique()
        {
            var person = factory.Create();

            var phoneNumber = person.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "1234567890";

            await person.WaitForTasks();

            Assert.True(person.IsValid);

            phoneNumber = person.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "0987654321";

            Assert.False(person.IsValid);

            Assert.Contains("Phone type must be unique", person.PropertyMessages.Select(m => m.Message));
        }

        [Fact]
        public async Task UniquePhoneNumberRule_ShouldReturnError_WhenPhoneNumberIsNotUnique()
        {
            var person = factory.Create();

            var phoneNumber = person.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Home;
            phoneNumber.PhoneNumber = "1234567890";

            await person.WaitForTasks();

            Assert.True(person.IsValid);

            phoneNumber = person.PersonPhoneList.AddPhoneNumber();
            phoneNumber.PhoneType = PhoneType.Mobile;
            phoneNumber.PhoneNumber = "1234567890";

            Assert.False(person.IsValid);

            Assert.Contains("Phone number must be unique", person.PropertyMessages.Select(m => m.Message));
        }
    }
}
