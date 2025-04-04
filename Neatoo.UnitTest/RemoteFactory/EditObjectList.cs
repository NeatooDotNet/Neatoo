
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.RemoteFactory;

public interface IEditObjectList : IEditListBase<IEditObject>
{

}

public class EditObjectList : EditListBase<IEditObject>, IEditObjectList
{

    public EditObjectList() : base()
    {
    }

    [Fetch]
    public void Fetch([Service] IEditObjectFactory editObjectFactory)
    {
        Add(editObjectFactory.Fetch());
        Add(editObjectFactory.Fetch());
        Add(editObjectFactory.Fetch());
    }

    [Update]
    public void Update([Service] IEditObjectFactory editObjectFactory)
    {
        foreach (var item in this.Union(DeletedList))
        {
            editObjectFactory.Save(item);
        }
    }
}
