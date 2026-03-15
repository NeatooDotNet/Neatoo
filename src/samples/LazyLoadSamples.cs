using Neatoo;
using Neatoo.RemoteFactory;
using Xunit;

namespace Samples;

// =============================================================================
// LAZY LOAD SAMPLES - Correct patterns for LazyLoad<T> with serialization
// =============================================================================

// -- Interfaces ---------------------------------------------------------------

public interface ISkillLazyChild : IEntityBase
{
    Guid Id { get; }
    string Data { get; set; }
}

public interface ISkillLazyParent : IEntityRoot
{
    Guid Id { get; set; }
    string Trigger { get; set; }
    string LoadedData { get; }
    LazyLoad<ISkillLazyChild> LazyChild { get; }
}

// -- Child entity (fetched via [Remote]) --------------------------------------

[Factory]
public partial class SkillLazyChild : EntityBase<SkillLazyChild>, ISkillLazyChild
{
    public SkillLazyChild(IEntityBaseServices<SkillLazyChild> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string Data { get; set; }

    [Remote]
    [Fetch]
    internal Task Fetch(Guid parentId)
    {
        using (PauseAllActions())
        {
            this["Id"].LoadValue(parentId);
            this["Data"].LoadValue($"Child data for {parentId}");
        }
        return Task.CompletedTask;
    }
}

// -- Parent entity: CORRECT pattern -------------------------------------------

#region skill-lazyload-constructor-pattern
[Factory]
public partial class SkillLazyParent : EntityBase<SkillLazyParent>, ISkillLazyParent
{
    public SkillLazyParent(
        IEntityBaseServices<SkillLazyParent> services,
        ISkillLazyChildFactory childFactory,
        ILazyLoadFactory lazyLoadFactory) : base(services)
    {
        // Create LazyLoad in the constructor.
        // The loader lambda captures the factory from DI and references this.Id,
        // which is resolved at load-time (not capture-time).
        // This instance survives serialization because the converter merges
        // deserialized state into it instead of replacing it.
        LazyChild = lazyLoadFactory.Create<ISkillLazyChild>(async () =>
        {
            return await childFactory.Fetch(this.Id);
        });

        // AddActionAsync: when Trigger changes, await the lazy-loaded child
        RuleManager.AddActionAsync(async parent =>
        {
            var child = await parent.LazyChild.LoadAsync();
            if (child != null)
            {
                parent.LoadedData = child.Data;
            }
        }, p => p.Trigger);
    }

    public partial string Trigger { get; set; }
    public partial string LoadedData { get; set; }
    public partial Guid Id { get; set; }

    // LazyLoad property -- partial, just like every other Neatoo property.
    // The generator handles backing field, setter (LoadValue), and registration.
    // Meta properties (IsValid, IsModified, etc.) propagate from the loaded child.
    public partial LazyLoad<ISkillLazyChild> LazyChild { get; set; }

    [Remote]
    [Fetch]
    internal Task Fetch(Guid id)
    {
        using (PauseAllActions())
        {
            this["Id"].LoadValue(id);
        }
        // LazyChild already created in the constructor with a loader
        // that uses this.Id — no need to create it here.
        return Task.CompletedTask;
    }
}
#endregion

// -- Anti-pattern: LazyLoad created in [Fetch] (WRONG) -----------------------

#region skill-lazyload-antipattern-fetch
// WRONG: Creating LazyLoad in [Fetch].
// [Fetch] only runs on the server. During serialization the loader delegate
// is [JsonIgnore] and lost. The client receives a LazyLoad with no loader,
// and any attempt to await it throws InvalidOperationException.
//
// [Fetch]
// internal Task Fetch(Guid id, [Service] ILazyLoadFactory lazyLoadFactory,
//     [Service] IChildFactory childFactory)
// {
//     this["Id"].LoadValue(id);
//     // BAD: This LazyLoad instance is created server-side only.
//     // After serialization to client, the loader delegate is gone.
//     LazyChild = lazyLoadFactory.Create<IChild>(async () =>
//     {
//         return await childFactory.Fetch(id);
//     });
// }
#endregion

// -- Anti-pattern: OnDeserialized workaround (WRONG) --------------------------

#region skill-lazyload-antipattern-ondeserialized
// WRONG: Recreating LazyLoad in OnDeserialized as a workaround.
// Before the converter fix, developers worked around the lost loader by
// overriding OnDeserialized to reinitialize LazyLoad instances.
// This is unnecessary — the converter now preserves constructor-created
// instances. Move LazyLoad creation to the constructor instead.
//
// // In the constructor — workaround creates an initializer method:
// InitializeLazyLoaders();
//
// internal void InitializeLazyLoaders()
// {
//     if (existingChild != null)
//         LazyChild = _lazyLoadFactory.Create<IChild>(existingChild);
//     else
//         LazyChild = _lazyLoadFactory.Create<IChild>(LoadChildAsync);
// }
//
// public override void OnDeserialized()
// {
//     base.OnDeserialized();
//     InitializeLazyLoaders(); // BAD: Unnecessary complexity
// }
//
// // Even worse — reinitialize after save:
// public void ReinitializeLazyLoaders() { InitializeLazyLoaders(); }
#endregion

// =============================================================================
// TESTS
// =============================================================================

public class LazyLoadPatternTests : SkillTestBase
{
    [Fact]
    public async Task LazyLoad_ConstructorPattern_LoadsChild()
    {
        var factory = GetRequiredService<ISkillLazyParentFactory>();
        var parent = await factory.Fetch(Guid.NewGuid());

        // LazyChild was created in the constructor with a loader
        Assert.NotNull(parent.LazyChild);
        Assert.False(parent.LazyChild.IsLoaded);

        // Trigger the AddActionAsync by setting Trigger
        parent.Trigger = "go";
        await parent.WaitForTasks();

        // Verify the lazy load completed
        Assert.True(parent.LazyChild.IsLoaded);
        Assert.NotNull(parent.LazyChild.Value);
        Assert.Equal($"Child data for {parent.Id}", parent.LoadedData);
    }
}
