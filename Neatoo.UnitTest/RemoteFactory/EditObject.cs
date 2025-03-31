using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory;
using Neatoo.UnitTest.Objects;

namespace Neatoo.UnitTest.RemoteFactory;

public partial class EditObject : EditBase<EditObject>, IEditObject
{
    [Create]
    public EditObject([Service] IEditBaseServices<EditObject> baseServices) : base(baseServices)
    {
        GetProperty(nameof(CreateCalled)).LoadValue(true);
        GetProperty(nameof(ID)).LoadValue(Guid.NewGuid()); // Don't run rules or mark as Modified
    }

    public partial Guid? ID { get; set; }
    public partial Guid GuidCriteria { get; set; }
    public partial int IntCriteria { get; set; }
    public partial object[] MultipleCriteria { get; set; }

    public partial bool CreateCalled { get; set; }

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
    public async Task CreateAsync(int criteria)
    {
        await Task.Delay(2);
        IntCriteria = criteria;
        CreateCalled = true;
    }


    [Create]
    public void Create(Guid criteria, [Service] IDisposableDependency dependency)
    {
        Assert.IsNotNull(dependency);
        GuidCriteria = criteria;
        CreateCalled = true;
    }

    [Remote]
    [Create]
    public void CreateRemote(Guid criteria, [Service] IDisposableDependency dependency)
    {
        Assert.IsNotNull(dependency);
        GuidCriteria = criteria;
        CreateCalled = true;
    }

    public bool FetchCalled { get; set; } = false;

    [Fetch]
    public void Fetch()
    {
        ID = Guid.NewGuid();
        FetchCalled = true;
    }

    [Fetch]
    public async Task Fetch(int criteria)
    {
        await Task.Delay(2);
        IntCriteria = criteria;
        FetchCalled = true;
    }

    [Fetch]
    public void Fetch(Guid criteria, [Service] IDisposableDependency dependency)
    {
        Assert.IsNotNull(dependency);
        GuidCriteria = criteria;
        FetchCalled = true;
    }

    [Fetch]
    [Remote]
    public void FetchRemote(Guid criteria, [Service] IDisposableDependency dependency)
    {
        Assert.IsNotNull(dependency);
        GuidCriteria = criteria;
        FetchCalled = true;
    }


    [Fetch]
    public bool FetchFail()
    {
        // returns null to the client
        return false;
    }

    [Fetch]
    public async Task<bool> FetchFailAsync()
    {
        await Task.Delay(2);
        // returns null to the client
        return false;
    }

    [Remote]
    [Fetch]
    public bool FetchFailDependency([Service] IDisposableDependency dependency)
    {
        // returns null to the client
        return false;
    }

    [Remote]
    [Fetch]
    public async Task<bool> FetchFailAsyncDependency([Service] IDisposableDependency dependency)
    {
        await Task.Delay(2);
        // returns null to the client
        return false;
    }

    public bool InsertCalled { get => Getter<bool>(); set => Setter(value); }

    [Insert]
    public void Insert()
    {
        ID = Guid.NewGuid();
        InsertCalled = true;
    }

    [Insert]
    public async Task Insert(int criteriaA)
    {
        await Task.Delay(2);
        InsertCalled = true;
        IntCriteria = criteriaA;
    }

    [Insert]
    public void Insert(Guid criteria, [Service] IDisposableDependency dependency)
    {
        Assert.IsNotNull(dependency);
        InsertCalled = true;
        GuidCriteria = criteria;
    }

    public bool UpdateCalled { get => Getter<bool>(); set => Setter(value); }

    [Update]
    public async Task Update()
    {
        await Task.Delay(2);
        ID = Guid.NewGuid();
        UpdateCalled = true;
    }

    [Update]
    public async Task Update(int criteriaB)
    {
        await Task.Delay(2);
        IntCriteria = criteriaB;
        UpdateCalled = true;
    }

    [Remote]
    [Update]
    public async Task Update(Guid criteria, [Service] IDisposableDependency dependency)
    {
        await Task.Delay(2);
        Assert.IsNotNull(dependency);
        GuidCriteria = criteria;
        UpdateCalled = true;
    }

    public bool DeleteCalled { get => Getter<bool>(); set => Setter(value); }

    [Delete]
    public void Delete()
    {
        DeleteCalled = true;
    }

    [Delete]
    public async Task Delete(int criteriaC)
    {
        await Task.Delay(2);
        IntCriteria = criteriaC;
        DeleteCalled = true;
    }

    [Delete]
    public void Delete(Guid criteria, [Service] IDisposableDependency dependency)
    {
        Assert.IsNotNull(dependency);
        GuidCriteria = criteria;
        DeleteCalled = true;
    }
}
