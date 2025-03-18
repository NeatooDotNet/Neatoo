namespace Neatoo.UnitTest.ObjectPortal;

public interface IEditObject : IEditBase
{
    Guid? ID { get; set; }
    int IntCriteria { get; }
    Guid GuidCriteria { get; }
    bool CreateCalled { get; set; }
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