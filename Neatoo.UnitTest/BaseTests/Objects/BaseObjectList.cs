namespace Neatoo.UnitTest.BaseTests.Objects;


public interface IBaseObjectList : IListBase<IBaseObject>
{ 

}
public class BaseObjectList : ListBase<IBaseObject>, IBaseObjectList
{

    public BaseObjectList() : base() { }

}
