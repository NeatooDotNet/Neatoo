using Neatoo.RemoteFactory;

namespace Neatoo.UnitTest.SystemTextJson.EditTests;

public interface IEditObject : IEditBase
{
    Guid ID { get; set; }
    string Name { get; set; }

    IEditObject Child { get; set; }
    IEditObjectList ChildList { get; set; }
    void MarkAsChild();

    void MarkNew();

    void MarkOld();

    void MarkUnmodified();

    void MarkDeleted();
}


public class EditObject : EditBase<EditObject>, IEditObject
{
    public EditObject(IEditBaseServices<EditObject> services) : base(services)
    {
    }

    public Guid ID { get => Getter<Guid>(); set => Setter(value); }
    public string Name { get => Getter<string>(); set => Setter(value); }
    public IEditObject Child { get => Getter<IEditObject>(); set => Setter(value); }
    public IEditObjectList ChildList { get => Getter<IEditObjectList>(); set => Setter(value); }

    void IEditObject.MarkAsChild()
    {
        this.MarkAsChild();
    }

    void IEditObject.MarkDeleted()
    {
        this.MarkDeleted();
    }

    void IEditObject.MarkNew()
    {
        this.MarkNew();
    }

    void IEditObject.MarkOld()
    {
        this.MarkOld();
    }

    void IEditObject.MarkUnmodified()
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

public interface IEditObjectList : IEditListBase<IEditObject>
{

}

public class EditObjectList : EditListBase<IEditObject>, IEditObjectList
{
    public EditObjectList() : base()
    {

    }

}
