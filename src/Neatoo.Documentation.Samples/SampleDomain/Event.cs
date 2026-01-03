using Neatoo.Documentation.Samples.ValidationAndRules;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Documentation.Samples.SampleDomain;

/// <summary>
/// Sample event entity for documentation examples.
/// Demonstrates cross-property validation with DateRangeRule.
/// </summary>
[Factory]
internal partial class Event : EntityBase<Event>, IEvent
{
    public Event(IEntityBaseServices<Event> services,
                 IDateRangeRule dateRangeRule) : base(services)
    {
        RuleManager.AddRule(dateRangeRule);
    }

    public partial Guid? Id { get; set; }

    [Required]
    public partial string? Name { get; set; }

    public partial DateTime StartDate { get; set; }

    public partial DateTime EndDate { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
        StartDate = DateTime.Today;
        EndDate = DateTime.Today.AddDays(1);
    }
}
