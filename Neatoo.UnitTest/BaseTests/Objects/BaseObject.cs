namespace Neatoo.UnitTest.BaseTests.Objects;

public interface IA { }

public interface IB : IA { }

public class B : IB { }

public interface IBaseObject : IBase
{
    Guid? Id { get; set; }
    string? StringProperty { get; set; }
    string? PrivateProperty { get; }
    IA TestPropertyType { get; set; }
    void LoadPropertyTest(B propertyValue);
    IBaseObject? Child { get; set; }
    IBaseObjectList? ChildList { get; set; }
}

public class BaseObject : Base<BaseObject>, IBaseObject
{
    public BaseObject() : base(new BaseServices<BaseObject>()) { }

    public Guid? Id
    {
        get { return Getter<Guid>(); }
        set { Setter(value); }
    }

    public string? StringProperty { get => Getter<string>(); set => Setter(value); }

    public string? PrivateProperty
    {
        get { return Getter<string>(); }
        private set { Setter(value); }
    }

    public IBaseObject? Child
    {
        get { return Getter<IBaseObject>(); }
        set { Setter(value); }
    }

    public IA TestPropertyType
    {
        get { return Getter<IA>(); }
        set { Setter(value); }
    }

    public IBaseObjectList? ChildList
    {
        get { return Getter<IBaseObjectList>(); }
        set { Setter(value); }
    }

    /// <summary>
    /// For unit testing purposes only
    /// Do not expose the LoadProperty method like this
    /// </summary>
    /// <param name="propertyValue"></param>
    public void LoadPropertyTest(B propertyValue)
    {
        /// Example - If the types are different you need to explicitly define the type
        /// of the Property
        /// The <IA> in this case
        this[nameof(TestPropertyType)].SetValue(propertyValue);
    }
}
