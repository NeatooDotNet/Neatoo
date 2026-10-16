using MudBlazor;
using Neatoo.Internal;
using Neatoo.RemoteFactory;

namespace Neatoo.Blazor.MudNeatoo.Tests;

/// <summary>
/// An entity with one property per MudNeatoo input type, built directly (no factory).
/// </summary>
[Factory]
public partial class SetterTarget : EntityBase<SetterTarget>
{
    public SetterTarget() : base(new EntityBaseServices<SetterTarget>())
    {
    }

    public partial int Minutes { get; set; }
    public partial string? Name { get; set; }
    public partial bool Locked { get; set; }
    public partial DateTime? When { get; set; }
    public partial TimeSpan? At { get; set; }
    public partial DateRange? Range { get; set; }
}
