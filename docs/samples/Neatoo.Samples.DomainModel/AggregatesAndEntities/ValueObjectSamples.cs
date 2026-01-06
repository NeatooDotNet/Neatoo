/// <summary>
/// Code samples for docs/aggregates-and-entities.md - Value Objects section
///
/// Full snippets (for complete examples):
/// - docs:aggregates-and-entities:value-object
///
/// Micro-snippets (for focused inline examples):
/// - docs:aggregates-and-entities:value-object-declaration
/// - docs:aggregates-and-entities:value-object-properties
/// - docs:aggregates-and-entities:value-object-fetch
///
/// Corresponding tests: ValueObjectSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.AggregatesAndEntities;

#region docs:aggregates-and-entities:value-object
/// <summary>
/// Value Object - simple POCO class with [Factory] attribute.
/// No Neatoo base class inheritance. RemoteFactory generates fetch operations.
/// Typical Use: Lookup data, dropdown options, reference data.
/// </summary>
public interface IStateProvince
{
    string Code { get; }
    string Name { get; }
}

#region docs:aggregates-and-entities:value-object-declaration
[Factory]
internal partial class StateProvince : IStateProvince
#endregion
{
    #region docs:aggregates-and-entities:value-object-properties
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    #endregion

    #region docs:aggregates-and-entities:value-object-fetch
    [Fetch]
    public void Fetch(string code, string name)
    {
        Code = code;
        Name = name;
    }
    #endregion
}
#endregion
