using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization.EntityTests;

public interface IEntityObject : IEntityBase
{
    Guid ID { get; set; }
    string Name { get; set; }

    IEntityObject Child { get; set; }
    IEntityObjectList ChildList { get; set; }
    void MarkAsChild();

    void MarkNew();

    void MarkOld();

    void MarkUnmodified();

    void MarkDeleted();
}

[Factory]
public partial class EntityObject : EntityBase<EntityObject>, IEntityObject
{
    public EntityObject(IEntityBaseServices<EntityObject> services) : base(services)
    {
        Required = 1;
    }

    public partial Guid ID { get; set; }
    public partial string Name { get; set; }
    public partial IEntityObject Child { get; set; }
    public partial IEntityObjectList ChildList { get; set; }
    [Required]
    public partial int? Required { get; set; }

    void IEntityObject.MarkAsChild()
    {
        this.MarkAsChild();
    }

    void IEntityObject.MarkDeleted()
    {
        this.MarkDeleted();
    }

    void IEntityObject.MarkNew()
    {
        this.MarkNew();
    }

    void IEntityObject.MarkOld()
    {
        this.MarkOld();
    }

    void IEntityObject.MarkUnmodified()
    {
        this.MarkUnmodified();
    }

    [Create]
    public Task Create(Guid ID, string Name)
    {
        this.ID = ID;
        this.Name = Name;
        return Task.CompletedTask;
    }

    [Update]
    [Insert]
    public Task Update()
    {
        this.Name = "Updated";
        return Task.CompletedTask;
    }
}

public interface IEntityObjectList : IEntityListBase<IEntityObject>
{
    List<IEntityObject> DeletedList { get; }
}

public class EntityObjectList : EntityListBase<IEntityObject>, IEntityObjectList
{
    public EntityObjectList() : base()
    {

    }

    List<IEntityObject> IEntityObjectList.DeletedList => DeletedList;
}
