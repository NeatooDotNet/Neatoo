using Neatoo;
using Neatoo.RemoteFactory;

namespace Neatoo.Samples.DomainModel.Installation;

#region source-gen-structure
// Required structure for source generation
public partial interface IMyAggregate : IEntityBase { }

[Factory]
internal partial class MyAggregate : EntityBase<MyAggregate>, IMyAggregate
{
    public MyAggregate(IEntityBaseServices<MyAggregate> services) : base(services) { }

    public partial string? Name { get; set; }  // Partial for state tracking
}
#endregion
