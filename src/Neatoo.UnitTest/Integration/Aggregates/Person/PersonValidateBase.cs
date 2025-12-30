using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Aggregates.Person;

[Factory]
public abstract partial class PersonValidateBase<T> : ValidateBase<T>, IPersonBase
    where T : PersonValidateBase<T>
{

    public PersonValidateBase(IValidateBaseServices<T> services) : base(services)
    {
    }

    public Guid Id { get { return Getter<Guid>(); } }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string ShortName { get; set; }

    public partial string Title { get; set; }

    public partial string FullName { get; set; }

    public partial uint? Age { get; set; }

    public void MapFrom(PersonDto dto)
    {
        this.Title = dto.Title;
        this.FirstName = dto.FirstName;
        this.LastName = dto.LastName;
    }

    public void FromDto(PersonDto dto)
    {
        using var pause = this.PauseAllActions();
        this[nameof(Id)].LoadValue(dto.PersonId);
        MapFrom(dto);
    }

}
