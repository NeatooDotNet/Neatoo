namespace Person.Dal;

public interface IPersonDbContext
{
	Task<PersonEntity?> FindPerson(CancellationToken cancellationToken = default);
	Task<PersonEntity?> FindPerson(Guid? id, CancellationToken cancellationToken = default);
	void AddPerson(PersonEntity personEntity);
	Task DeleteAllPersons(CancellationToken cancellationToken = default);
	void DeletePerson(PersonEntity person);
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
	Task<bool> PersonNameExists(Guid? excludeId, string firstName, string lastName);
	Task<ICollection<PersonPhoneEntity>> FindPersonPhones(Guid personId, CancellationToken cancellationToken = default);
}
