namespace Neatoo.Documentation.Samples.SampleDomain;

/// <summary>
/// Sample event interface for documentation examples.
/// Used to demonstrate cross-property validation (StartDate before EndDate).
/// </summary>
public partial interface IEvent : IEntityBase
{
    Guid? Id { get; set; }
    string? Name { get; set; }
    DateTime StartDate { get; set; }
    DateTime EndDate { get; set; }
}
