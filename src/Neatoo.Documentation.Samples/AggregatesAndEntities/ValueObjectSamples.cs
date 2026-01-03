/// <summary>
/// Code samples for docs/aggregates-and-entities.md - Value Objects section
///
/// Snippets in this file:
/// - docs:aggregates-and-entities:value-object
///
/// Corresponding tests: ValueObjectSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;

namespace Neatoo.Documentation.Samples.AggregatesAndEntities;

#region docs:aggregates-and-entities:value-object
/// <summary>
/// Value Object - simple POCO class with [Factory] attribute.
/// No Neatoo base class inheritance. RemoteFactory generates fetch operations.
/// Typical Use: Lookup data, dropdown options, reference data.
/// </summary>
public interface IStateProvince
{
    string? Code { get; set; }
    string? Name { get; set; }
}

[Factory]
internal partial class StateProvince : IStateProvince
{
    public string? Code { get; set; }
    public string? Name { get; set; }

    [Fetch]
    public void Fetch(StateProvinceDto dto)
    {
        Code = dto.Code;
        Name = dto.Name;
    }
}

// DTO for demonstration
public class StateProvinceDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
}
#endregion
