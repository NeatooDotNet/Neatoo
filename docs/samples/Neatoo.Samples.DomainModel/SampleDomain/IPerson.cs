namespace Neatoo.Samples.DomainModel.SampleDomain;

/// <summary>
/// Sample person interface for documentation examples.
/// </summary>
public partial interface IPerson : IEntityBase
{
    Guid? Id { get; set; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    int Age { get; set; }
    string? FullName { get; set; }
}
