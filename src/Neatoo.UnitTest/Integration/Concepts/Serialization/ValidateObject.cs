using Neatoo.RemoteFactory;
using Neatoo.Rules;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization.ValidateTests;

public interface IValidateObject : IValidateBase
{
    Guid ID { get; set; }
    string Name { get; set; }
    int RuleRunCount { get; internal set; }

    IValidateObject Child { get; set; }
    IEnumerable<IRule> Rules { get; }
    void MarkInvalid(string message);

    string ObjectInvalid { get; }

    //new IValidateProperty this[string propertyName] { get => GetProperty(propertyName); }
}

public partial class ValidateObject : ValidateBase<ValidateObject>, IValidateObject
{
    public ValidateObject(IValidateBaseServices<ValidateObject> services) : base(services)
    {
        RuleManager.AddValidation(t =>
        {
            t.RuleRunCount++;
            if (t.Name == "Error") { return "Error"; }
            return string.Empty;
        }, _ => _.Name);
    }

    public partial int RuleRunCount { get; set; }
    public partial Guid ID { get; set; }
    public partial string Name { get; set; }
    public partial IValidateObject Child { get; set; }

    public IEnumerable<IRule> Rules => RuleManager.Rules;
    void IValidateObject.MarkInvalid(string message)
    {
        base.MarkInvalid(message);
    }

    [Create]
    public void Create(Guid ID, string Name)
    {
        this.ID = ID;
        this.Name = Name;
    }
}

public interface IValidateObjectList : IValidateListBase<IValidateObject>
{

}

public class ValidateObjectList : ValidateListBase<IValidateObject>, IValidateObjectList
{
    public ValidateObjectList() : base()
    {
    }


}
