namespace Neatoo.UnitTest.Integration.Aggregates.Person;

public interface IPersonEntity : IPersonBase, IEntityBase
{

}

public interface IPersonBase : IValidateBase
{
    Guid Id { get; }
    string FirstName { get; set; }
    string FullName { get; set; }
    string LastName { get; set; }
    string ShortName { get; set; }
    string Title { get; set; }
    uint? Age { get; set; }
    void FromDto(PersonDto dto);
}
