// -----------------------------------------------------------------------------
// Design.Domain - Domain Registration
// -----------------------------------------------------------------------------
// Registers the domain types that come from DI but are not factories.
// AddNeatooServices registers the generated factories; it does not register
// rules. Rule types are internal, so the domain assembly registers them.
// -----------------------------------------------------------------------------

using Design.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Domain.DI;

#region skill-rules-di-registration
/// <summary>
/// Registration for the domain's DI-provided rules. Call on BOTH tiers, after
/// AddNeatooServices: an entity that takes a rule in its constructor is built
/// on the client too.
/// </summary>
public static class DomainRegistration
{
    public static IServiceCollection AddDesignDomainRules(this IServiceCollection services)
    {
        // Transient: each entity instance gets its own rule instance, because
        // a rule tracks execution state
        services.AddTransient<ICheckUsernameAvailabilityRule, CheckUsernameAvailabilityRule>();
        services.AddTransient<IUniqueCodeRule, UniqueCodeRule>();
        return services;
    }
}
#endregion
