namespace Neatoo.UnitTest.RemoteFactory;

public partial interface IEditObject : IEditBase
{
    bool FetchCalled { get; set; }
    bool DeleteCalled { get; set; }
    bool UpdateCalled { get; set; }
    bool InsertCalled { get; set; }
    void MarkAsChild();
    void MarkNew();
    void MarkOld();
    void MarkUnmodified();
    void MarkDeleted();
}