using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.PersonObjects;


public abstract partial class PersonEditBase<T> : EditBase<T>, IPersonBase
    where T : PersonEditBase<T>
{

    public PersonEditBase(IEditBaseServices<T> services) : base(services)
    {
    }

    public Guid Id { get { return Getter<Guid>(); } }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string ShortName { get; set; }

    public partial string Title { get; set; }

    public partial string FullName { get; set; }

    public partial uint? Age { get; set; }

    //string IPersonBase.FirstName { get => FirstName; set => FirstName = value; }

    public partial void MapFrom(PersonDto dto);

    public void FromDto(PersonDto dto)
    {
        using var pause = this.PauseAllActions();
        this[nameof(Id)].LoadValue(dto.PersonId);
        MapFrom(dto);
    }

    [Fetch]
    public void FillFromDto(PersonDto dto)
    {
        FromDto(dto);
    }
}
