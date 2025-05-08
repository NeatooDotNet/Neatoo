
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.RemoteFactory;

public interface IEntityObjectList : IEntityListBase<IEntityObject>
{

}

public class EntityObjectList : EntityListBase<IEntityObject>, IEntityObjectList
{

    public EntityObjectList() : base()
    {
    }

    [Fetch]
    public void Fetch([Service] IEntityObjectFactory editObjectFactory)
    {
        Add(editObjectFactory.Fetch());
        Add(editObjectFactory.Fetch());
        Add(editObjectFactory.Fetch());
    }

    [Update]
    public void Update([Service] IEntityObjectFactory editObjectFactory)
    {
        foreach (var item in this.Union(DeletedList))
        {
            editObjectFactory.Save(item);
        }
    }
}
