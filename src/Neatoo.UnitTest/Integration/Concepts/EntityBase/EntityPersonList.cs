namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;


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
