using Neatoo.RemoteFactory;
using Neatoo.UnitTest.PersonObjects;

namespace Neatoo.UnitTest.EditBaseTests;


public partial interface IEditPerson : IPersonEdit
{

    IEditPerson Child { get; set; }

    void MarkAsChild();

    void MarkNew();

    void MarkOld();

    void MarkUnmodified();

    void MarkDeleted();

}

public partial class EditPerson : PersonEditBase<EditPerson>, IEditPerson
{
    public EditPerson(IEditBaseServices<EditPerson> services,
        IShortNameRule shortNameRule,
        IFullNameRule fullNameRule) : base(services)
    {
        using var paused = PauseAllActions();
        RuleManager.AddRules(shortNameRule, fullNameRule);
        InitiallyDefined = new List<int>() { 1, 2, 3 };
    }

    public partial List<int> InitiallyNull { get; set; }
    public partial List<int> InitiallyDefined { get; set; }

    public partial IEditPerson Child { get; set; }

    void IEditPerson.MarkAsChild()
    {
        this.MarkAsChild();
    }
    
    void IEditPerson.MarkDeleted()
    {
        this.MarkDeleted();
    }

    void IEditPerson.MarkNew()
    {
        this.MarkNew();
    }

    void IEditPerson.MarkOld()
    {
        this.MarkOld();
    }

    void IEditPerson.MarkUnmodified()
    {
        this.MarkUnmodified();
    }
    [Insert]
    public void Insert()
    {

    }
}
