using Neatoo.Documentation.Samples.ValidationAndRules;
using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Documentation.Samples.SampleDomain;

/// <summary>
/// Sample person entity for documentation examples.
/// Demonstrates EntityBase, validation attributes, and rule registration.
/// </summary>
[Factory]
internal partial class Person : EntityBase<Person>, IPerson
{
    public Person(IEntityBaseServices<Person> services,
                  IAgeValidationRule ageRule,
                  IUniqueEmailRule uniqueEmailRule) : base(services)
    {
        RuleManager.AddRule(ageRule);
        RuleManager.AddRule(uniqueEmailRule);
    }

    public partial Guid? Id { get; set; }

    [DisplayName("First Name*")]
    [Required(ErrorMessage = "First Name is required")]
    public partial string? FirstName { get; set; }

    [DisplayName("Last Name*")]
    [Required(ErrorMessage = "Last Name is required")]
    public partial string? LastName { get; set; }

    [DisplayName("Email")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public partial string? Email { get; set; }

    [Range(0, 150, ErrorMessage = "Age must be between 0 and 150")]
    public partial int Age { get; set; }

    public partial string? FullName { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }
}
