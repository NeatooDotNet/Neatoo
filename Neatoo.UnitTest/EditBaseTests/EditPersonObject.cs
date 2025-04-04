using Neatoo.RemoteFactory;
using Neatoo.UnitTest.PersonObjects;
using System.Reflection.Metadata.Ecma335;

namespace Neatoo.UnitTest.EditBaseTests;
public partial interface IEditPerson : IPersonEdit
{
    void MarkAsChild();
    void MarkNew();
    void MarkOld();
    void MarkUnmodified();
    void MarkDeleted();
}

public partial class EditPerson : PersonEditBase<EditPerson>, IEditPerson
{
    public EditPerson() : base(new EditBaseServices<EditPerson>(null))
    {
        using var paused = PauseAllActions();
        RuleManager.AddRules(new ShortNameRule(), new FullNameRule());
        InitiallyDefined = new List<int>() { 1, 2, 3 };
        RuleManager.AddValidation(person => person.FirstName == "Error" ? "Error" : string.Empty, ep => ep.FirstName);
    }

    public partial List<int> InitiallyNull { get; set; }
    public partial List<int> InitiallyDefined { get; set; }
    public partial IEditPerson Child { get; set; }
    public partial IEditPersonList ChildList { get; set; }

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
