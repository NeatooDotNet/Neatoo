namespace Neatoo.UnitTest.EditBaseTests;


public interface IEditPersonList : IEditListBase<IEditPerson>
{
    int DeletedCount { get; }

}

public class EditPersonList : EditListBase<IEditPerson>, IEditPersonList
{
    public EditPersonList() : base()
    {
    }

    public int DeletedCount => DeletedList.Count;
}
