namespace Neatoo.UnitTest.EntityBaseTests;


public interface IEntityPersonList : IEntityListBase<IEntityPerson>
{
    int DeletedCount { get; }

}

public class EntityPersonList : EntityListBase<IEntityPerson>, IEntityPersonList
{
    public EntityPersonList() : base()
    {
    }

    public int DeletedCount => DeletedList.Count;
}
