namespace Neatoo.UnitTest.ObjectPortal;

/// <summary>
/// I don't know that this is really neccessary
/// Testing that the portal logic works on Base<> should be enough
/// </summary>
public class BaseObjectList : ListBase<IBaseObject>, IBaseObjectList
{

    public BaseObjectList() : base()
    {
    }
}
