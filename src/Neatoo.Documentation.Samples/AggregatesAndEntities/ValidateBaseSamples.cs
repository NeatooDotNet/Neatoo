/// <summary>
/// Code samples for docs/aggregates-and-entities.md - ValidateBase section
///
/// Snippets in this file:
/// - docs:aggregates-and-entities:validatebase-criteria
/// - docs:aggregates-and-entities:validatebase-order-criteria
///
/// Corresponding tests: ValidateBaseSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Documentation.Samples.AggregatesAndEntities;

/// <summary>
/// Date range validation rule for search criteria.
/// </summary>
public interface IDateRangeSearchRule : IRule<IPersonSearchCriteria> { }

public class DateRangeSearchRule : RuleBase<IPersonSearchCriteria>, IDateRangeSearchRule
{
    public DateRangeSearchRule() : base(t => t.FromDate, t => t.ToDate) { }

    protected override IRuleMessages Execute(IPersonSearchCriteria target)
    {
        if (target.FromDate.HasValue && target.ToDate.HasValue && target.FromDate > target.ToDate)
        {
            return (nameof(target.FromDate), "From date must be before To date").AsRuleMessages();
        }
        return None;
    }
}

#region docs:aggregates-and-entities:validatebase-criteria
/// <summary>
/// Criteria object - has validation but no persistence.
/// Use ValidateBase for objects that need validation but are NOT persisted.
/// </summary>
public partial interface IPersonSearchCriteria : IValidateBase
{
    string? SearchTerm { get; set; }
    DateTime? FromDate { get; set; }
    DateTime? ToDate { get; set; }
}

[Factory]
internal partial class PersonSearchCriteria : ValidateBase<PersonSearchCriteria>, IPersonSearchCriteria
{
    public PersonSearchCriteria(IValidateBaseServices<PersonSearchCriteria> services,
                                 IDateRangeSearchRule dateRangeRule) : base(services)
    {
        // Add custom date range validation rule
        RuleManager.AddRule(dateRangeRule);
    }

    [Required(ErrorMessage = "At least one search term required")]
    public partial string? SearchTerm { get; set; }

    public partial DateTime? FromDate { get; set; }
    public partial DateTime? ToDate { get; set; }

    [Create]
    public void Create() { }
}
#endregion

#region docs:aggregates-and-entities:validatebase-order-criteria
/// <summary>
/// Another ValidateBase example - order search criteria.
/// </summary>
public partial interface IOrderSearchCriteria : IValidateBase
{
    string? CustomerName { get; set; }
    DateTime? OrderDate { get; set; }
}

[Factory]
internal partial class OrderSearchCriteria : ValidateBase<OrderSearchCriteria>, IOrderSearchCriteria
{
    public OrderSearchCriteria(IValidateBaseServices<OrderSearchCriteria> services) : base(services) { }

    public partial string? CustomerName { get; set; }
    public partial DateTime? OrderDate { get; set; }

    [Create]
    public void Create() { }
}
#endregion
