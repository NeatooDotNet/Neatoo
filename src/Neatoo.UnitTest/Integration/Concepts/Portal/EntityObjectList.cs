
using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.Integration.Concepts.Portal;

public interface IEntityObjectList : IEntityListBase<IEntityObject>
{

}

[Factory]
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
