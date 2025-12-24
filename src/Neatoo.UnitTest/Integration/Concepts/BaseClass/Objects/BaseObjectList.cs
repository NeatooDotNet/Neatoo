namespace Neatoo.UnitTest.Integration.Concepts.BaseClass.Objects;


public interface IBaseObjectList : IListBase<IBaseObject>
{

}
public class BaseObjectList : ListBase<IBaseObject>, IBaseObjectList
{

    public BaseObjectList() : base() { }

}
