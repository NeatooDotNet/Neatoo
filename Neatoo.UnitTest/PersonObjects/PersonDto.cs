namespace Neatoo.UnitTest.PersonObjects;

public class PersonDto
{
    public PersonDto()
    {
        PersonId = Guid.NewGuid();
    }
    public Guid PersonId { get; }
    public string Title { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public Guid? FatherId { get; set; }
    public Guid? MotherId { get; set; }
    public Guid? MateId { get; set; }

    public static IReadOnlyList<PersonDto> Data()
    {
        List<PersonObjects.PersonDto> result = new List<PersonDto>();

        PersonDto father;
        PersonDto mother;

        result.Add(father = new PersonDto() { FirstName = "A", LastName = "Smith", Title = "Mr." });
        result.Add(mother = new PersonDto() { FirstName = "B", LastName = "Smith", Title = "Mrs." });

        father.MateId = mother.PersonId;
        mother.MateId = father.PersonId;

        List<PersonDto> children = new List<PersonDto>();

        children.Add(new PersonDto() { FirstName = "AB1", LastName = "Smith", Title = "Mr." });
        children.Add(new PersonDto() { FirstName = "AB2", LastName = "Smith", Title = "Mr." });
        children.Add(new PersonDto() { FirstName = "AB3", LastName = "Smith", Title = "Mr." });

        children.ForEach(c => { c.FatherId = father.PersonId; c.MotherId = mother.PersonId; });

        result.AddRange(children);

        father = children.First();
        result.Add(mother = new PersonDto() { FirstName = "C", LastName = "Johnson", Title = "Mrs." });
        children.Clear();

        children.Add(new PersonDto() { FirstName = "AB1C1", LastName = "Smith", Title = "Mr." });
        children.Add(new PersonDto() { FirstName = "AB1C2", LastName = "Smith", Title = "Mr." });
        children.Add(new PersonDto() { FirstName = "AB1C3", LastName = "Smith", Title = "Mr." });

        children.ForEach(c => { c.FatherId = father.PersonId; c.MotherId = mother.PersonId; });

        result.AddRange(children);

        return result.AsReadOnly();

    }
}
