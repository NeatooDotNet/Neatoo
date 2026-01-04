using Neatoo.RemoteFactory;
using Neatoo.Internal;
using Neatoo.UnitTest.Integration.Aggregates.Person;

namespace Neatoo.UnitTest.Integration.Concepts.EntityBase;
public partial interface IEntityPerson : IPersonEntity
{
    void MarkAsChild();
    void MarkNew();
    void MarkOld();
    void MarkUnmodified();
    void MarkDeleted();

    /// <summary>
    /// Test helper: Makes the entity busy by adding a pending task.
    /// Call the returned action to release the busy state.
    /// </summary>
    Action MarkBusyForTest();
}

[Factory]
public partial class EntityPerson : PersonEntityBase<EntityPerson>, IEntityPerson
{
    public EntityPerson() : base(new EntityBaseServices<EntityPerson>(null))
    {
        using var paused = PauseAllActions();
        RuleManager.AddRules(new ShortNameRule(), new FullNameRule());
        InitiallyDefined = new List<int>() { 1, 2, 3 };
        RuleManager.AddValidation(person => person.FirstName == "Error" ? "Error" : string.Empty, ep => ep.FirstName);
    }

    public partial List<int> InitiallyNull { get; set; }
    public partial List<int> InitiallyDefined { get; set; }
    public partial IEntityPerson Child { get; set; }
    public partial IEntityPersonList ChildList { get; set; }

    void IEntityPerson.MarkAsChild()
    {
        this.MarkAsChild();
    }

    void IEntityPerson.MarkDeleted()
    {
        this.MarkDeleted();
    }

    void IEntityPerson.MarkNew()
    {
        this.MarkNew();
    }

    void IEntityPerson.MarkOld()
    {
        this.MarkOld();
    }

    void IEntityPerson.MarkUnmodified()
    {
        this.MarkUnmodified();
    }

    Action IEntityPerson.MarkBusyForTest()
    {
        var tcs = new TaskCompletionSource<bool>();
        this.RunningTasks.AddTask(tcs.Task);
        return () => tcs.SetResult(true);
    }

    [Insert]
    public void Insert()
    {

    }
}
