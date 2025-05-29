using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neatoo;
using Neatoo.Internal;
using Person.Ef;
using System;
using System.Threading.Tasks;

namespace DomainModel.Tests.IntegrationTests
{
    public class ContainerContext : IDisposable
    {
        public ContainerContext()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDbContext<PersonDbContext>(options => options.UseSqlite(new SqliteConnection("Filename=:memory:")));
            serviceCollection.AddScoped<IPersonDbContext>(cc => cc.GetRequiredService<PersonDbContext>());
            serviceCollection.AddNeatooServices(Neatoo.RemoteFactory.NeatooFactory.Logical, typeof(Person).Assembly);
            serviceCollection.AddTransient<IPersonAuth, PersonAuth>();
            var user = new User();
            user.Role = Role.Delete;
            serviceCollection.AddTransient<IUniqueNameRule, UniqueNameRule>();
            serviceCollection.AddTransient<IUniquePhoneNumberRule, UniquePhoneNumberRule>();
            serviceCollection.AddTransient<IUniquePhoneTypeRule, UniquePhoneTypeRule>();
            serviceCollection.AddSingleton<IUser>(user);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }


        public ServiceProvider ServiceProvider { get; private set; }

        public void Dispose()
        {
            ServiceProvider.Dispose();
        }
    }

    public class PersonIntegrationTests : IClassFixture<ContainerContext>
    {
        private readonly PersonDbContext personContext;
        private IPersonFactory factory;
        private string firstName = Guid.NewGuid().ToString();
        private string lastName = Guid.NewGuid().ToString();

        public PersonIntegrationTests(ContainerContext containerContext)
        {
            ArgumentNullException.ThrowIfNull(containerContext, nameof(containerContext));

            var scope = containerContext.ServiceProvider.CreateScope();

            this.personContext = scope.ServiceProvider.GetRequiredService<PersonDbContext>();
            this.personContext.Database.OpenConnection();
            this.personContext.Database.EnsureCreated();
            this.factory = scope.ServiceProvider.GetRequiredService<IPersonFactory>();
        }

        private IPerson create()
        {
            var person = factory.Create()!;

            person.FirstName = this.firstName;
            person.LastName = this.lastName;
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
        public async Task PersonTests_End_To_End()
        {
            IPerson? person = null;
            IPersonPhone? personPhone = null;

            async Task CheckInvalid()
            {
                Assert.NotNull(person);
                await person.RunRules();
                Assert.False(person.IsValid);
                Assert.False(personPhone?.IsValid ?? false);
            }

            void CheckValid()
            {
                Assert.NotNull(person);
                Assert.True(person.IsValid);
                Assert.True(personPhone?.IsValid ?? true);
            }

            person = factory.Create();
            Assert.NotNull(person);
            await CheckInvalid();

            person.FirstName = this.firstName;
            person.LastName = this.lastName;
            person.Email = "a@a.com";
            person.Notes = "Some notes";
            CheckValid();

            // Check phone number required fields
            personPhone = person.PersonPhoneList.AddPhoneNumber();

            await CheckInvalid();

            personPhone.PhoneType = PhoneType.Home;
            personPhone.PhoneNumber = "1";
            CheckValid();

            // Add another phone number with a duplicate phone type
            personPhone = person.PersonPhoneList.AddPhoneNumber();
            await CheckInvalid();

            personPhone.PhoneType = PhoneType.Home;
            personPhone.PhoneNumber = "0";
            await CheckInvalid();

            personPhone.PhoneType = PhoneType.Work;
            CheckValid();

            // Add another phone number with a duplicate phone number
            personPhone = person.PersonPhoneList.AddPhoneNumber();
            await CheckInvalid();

            personPhone.PhoneType = PhoneType.Mobile;
            personPhone.PhoneNumber = "1";
            await CheckInvalid();

            personPhone.PhoneNumber = "2";
            CheckValid();

            // Check duplicate name rule
            await person.Save(); // Need a record in the database to check for duplicates

            person = factory.Create();
            Assert.NotNull(person);

            person.FirstName = this.firstName; // Duplicate name
            person.LastName = this.lastName;

            Assert.False(person.IsValid);
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
            Assert.Equal(PhoneType.Home, (PhoneType)phones[0].PhoneType);
            Assert.Equal("0987654321", phones[1].PhoneNumber);
            Assert.Equal(PhoneType.Work, (PhoneType)phones[1].PhoneType);
        }

        [Fact]
        public async Task PersonTests_Update()
        {
            var person = create();

            // Act
            var result = (IPerson)await person.Save();

            result.FirstName = this.firstName;
            result.LastName = this.lastName;
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
            personContext.Persons.Add(new PersonEntity { Id = Guid.NewGuid(), FirstName = this.firstName, LastName = this.lastName });
            await personContext.SaveChangesAsync();

            var person = factory.Create()!;
            person.FirstName = this.firstName;
            person.LastName = this.lastName;

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
