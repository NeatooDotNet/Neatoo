namespace Neatoo.UnitTest.PersonObjects;

public interface IPersonEdit : IPersonBase, IEditBase
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
    void FillFromDto(PersonDto dto);
}