using Neatoo;
using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Skills.Domain;

// =============================================================================
// QUICKSTART SAMPLE - Simple entity demonstrating core Neatoo patterns
// =============================================================================

#region skill-quickstart
[Factory]
public partial class Product : EntityBase<Product>
{
    public Product(IEntityBaseServices<Product> services) : base(services) { }

    [Required]
    public partial string Name { get; set; }
    public partial decimal Price { get; set; }

    [Create] public void Create() { }
}
#endregion

// -----------------------------------------------------------------------------
// Properties Basic Sample - demonstrates partial properties without attributes
// This class exists to provide a compilable snippet showing basic property syntax
// -----------------------------------------------------------------------------

[Factory]
public partial class SkillPropertiesBasic : ValidateBase<SkillPropertiesBasic>
{
    public SkillPropertiesBasic(IValidateBaseServices<SkillPropertiesBasic> services) : base(services) { }

    #region skill-properties-basic
    public partial string Name { get; set; }
    public partial decimal Price { get; set; }
    #endregion

    [Create]
    public void Create() { }
}
