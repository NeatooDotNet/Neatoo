/// <summary>
/// Code samples for docs/aggregates-and-entities.md - ValidateBase section
///
/// Full snippets (for complete examples):
/// - docs:aggregates-and-entities:validatebase-criteria
/// - docs:aggregates-and-entities:validatebase-order-criteria
///
/// Micro-snippets (for focused inline examples):
/// - docs:aggregates-and-entities:validatebase-declaration
/// - docs:aggregates-and-entities:criteria-inline-rule
/// - docs:aggregates-and-entities:criteria-date-properties
///
/// Corresponding tests: ValidateBaseSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.AggregatesAndEntities;

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

#region docs:aggregates-and-entities:validatebase-declaration
[Factory]
internal partial class PersonSearchCriteria : ValidateBase<PersonSearchCriteria>, IPersonSearchCriteria
#endregion
{
    public PersonSearchCriteria(IValidateBaseServices<PersonSearchCriteria> services) : base(services)
    {
        #region docs:aggregates-and-entities:criteria-inline-rule
        // Inline date range validation - validates when either date changes
        RuleManager.AddValidation(
            t => t.FromDate.HasValue && t.ToDate.HasValue && t.FromDate > t.ToDate
                ? "From date must be before To date" : "",
            t => t.FromDate);

        RuleManager.AddValidation(
            t => t.FromDate.HasValue && t.ToDate.HasValue && t.FromDate > t.ToDate
                ? "To date must be after From date" : "",
            t => t.ToDate);
        #endregion
    }

    [Required(ErrorMessage = "At least one search term required")]
    public partial string? SearchTerm { get; set; }

    #region docs:aggregates-and-entities:criteria-date-properties
    public partial DateTime? FromDate { get; set; }
    public partial DateTime? ToDate { get; set; }
    #endregion

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
