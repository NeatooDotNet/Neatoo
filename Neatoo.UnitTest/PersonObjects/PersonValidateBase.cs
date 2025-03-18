namespace Neatoo.UnitTest.PersonObjects;


public abstract class PersonValidateBase<T> : ValidateBase<T>, IPersonBase
    where T : PersonValidateBase<T>
{

    public PersonValidateBase(IValidateBaseServices<T> services) : base(services)
    {
    }

    public Guid Id { get { return Getter<Guid>(); } }

    public string FirstName
    {
        get { return Getter<string>(); }
        set { Setter(value); }
    }

    public string LastName
    {
        get { return Getter<string>(); }
        set { Setter(value); }
    }

    public string ShortName
    {
        get { return Getter<string>(); }
        set { Setter(value); }
    }

    public string Title
    {
        get { return Getter<string>(); }
        set { Setter(value); }
    }

    public string FullName
    {
        get { return Getter<string>(); }
        set { Setter(value); }
    }

    public uint? Age
    {
        get => Getter<uint?>(); set => Setter(value);
    }

    public void FillFromDto(PersonDto dto)
    {
        this[nameof(Id)].SetValue(dto.PersonId);

        FirstName = dto.FirstName;
        LastName = dto.LastName;
        Title = dto.Title;
    }
}
