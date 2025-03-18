using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.PersonObjects;


public abstract class PersonEditBase<T> : EditBase<T>, IPersonBase
    where T : PersonEditBase<T>
{

    public PersonEditBase(IEditBaseServices<T> services) : base(services)
    {
    }

    public Guid Id { get { return Getter<Guid>(); } }

    public string FirstName { get { return Getter<string>(); } set { Setter(value); } }

    public string LastName { get { return Getter<string>(); } set { Setter(value); } }

    public string ShortName { get { return Getter<string>(); } set { Setter(value); } }

    public string Title { get { return Getter<string>(); } set { Setter(value); } }

    public string FullName { get { return Getter<string>(); } set { Setter(value); } }

    public uint? Age { get => Getter<uint?>(); set => Setter(value); }
    string IPersonBase.FirstName { get => FirstName; set => FirstName = value; }

    [Fetch]
    public void FillFromDto(PersonDto dto)
    {
         this[nameof(Id)].LoadValue(dto.PersonId);

        // These will not mark IsModified to true
        // as long as within ObjectPortal operation
        FirstName = dto.FirstName;
        LastName = dto.LastName;
        Title = dto.Title;
    }
}
