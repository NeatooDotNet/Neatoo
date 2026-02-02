using Neatoo;
using Neatoo.RemoteFactory;
using System.Security.Principal;

namespace Neatoo.Skills.Domain;

// =============================================================================
// AUTHORIZATION SAMPLES - Demonstrates factory-level authorization
// =============================================================================

// -----------------------------------------------------------------------------
// Basic Authorization Setup
// -----------------------------------------------------------------------------

#region skill-authorization
public interface IEmployeeAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanSave();
}

public class EmployeeAuthorization : IEmployeeAuthorization
{
    private readonly IPrincipal _principal;
    public EmployeeAuthorization(IPrincipal principal) => _principal = principal;

    public bool CanCreate() => _principal.IsInRole("HR");
    public bool CanFetch() => _principal.Identity?.IsAuthenticated ?? false;
    public bool CanSave() => _principal.IsInRole("HR");
}

[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class SkillDocEmployee : EntityBase<SkillDocEmployee>
{
    public SkillDocEmployee(IEntityBaseServices<SkillDocEmployee> services) : base(services) { }
    public partial int Id { get; set; }
    public partial string Name { get; set; }
    [Create] public void Create() { }
}
#endregion

#region auth-basic
/// <summary>
/// Authorization interface for Employee operations.
/// Methods return true if operation is allowed.
/// </summary>
public interface ISkillEmployeeAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanSave();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanDelete();
}

/// <summary>
/// Authorization implementation checking user roles.
/// </summary>
public class SkillEmployeeAuthorization : ISkillEmployeeAuthorization
{
    private readonly IPrincipal _principal;

    public SkillEmployeeAuthorization(IPrincipal principal)
    {
        _principal = principal;
    }

    public bool CanCreate() => _principal.IsInRole("Admin") || _principal.IsInRole("Manager");
    public bool CanFetch() => _principal.Identity?.IsAuthenticated ?? false;
    public bool CanSave() => _principal.IsInRole("Admin") || _principal.IsInRole("Manager");
    public bool CanDelete() => _principal.IsInRole("Admin");
}

/// <summary>
/// Employee entity with factory authorization.
/// </summary>
[Factory]
[AuthorizeFactory<ISkillEmployeeAuthorization>]
public partial class SkillAuthEmployee : EntityBase<SkillAuthEmployee>
{
    public SkillAuthEmployee(IEntityBaseServices<SkillAuthEmployee> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Name { get; set; }
    public partial decimal Salary { get; set; }

    [Create]
    public void Create() { }

    [Fetch]
    public void Fetch(int id, string name, decimal salary)
    {
        Id = id;
        Name = name;
        Salary = salary;
    }

    [Insert]
    public Task InsertAsync() => Task.CompletedTask;

    [Update]
    public Task UpdateAsync() => Task.CompletedTask;

    [Delete]
    public Task DeleteAsync() => Task.CompletedTask;
}
#endregion

// -----------------------------------------------------------------------------
// Authorization with Parameters
// -----------------------------------------------------------------------------

#region auth-with-parameters
/// <summary>
/// Authorization that receives factory method parameters.
/// </summary>
public interface ISkillDocumentAuthorization
{
    // Parameters match the Fetch method signature
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch(int documentId, string category);

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanSave();
}

public class SkillDocumentAuthorization : ISkillDocumentAuthorization
{
    private readonly IPrincipal _principal;

    public SkillDocumentAuthorization(IPrincipal principal)
    {
        _principal = principal;
    }

    // Check authorization based on document category
    public bool CanFetch(int documentId, string category)
    {
        // Confidential documents require Admin role
        if (category == "Confidential")
            return _principal.IsInRole("Admin");

        // Regular documents accessible to all authenticated users
        return _principal.Identity?.IsAuthenticated ?? false;
    }

    public bool CanSave() => _principal.IsInRole("Editor") || _principal.IsInRole("Admin");
}

[Factory]
[AuthorizeFactory<ISkillDocumentAuthorization>]
public partial class SkillAuthDocument : EntityBase<SkillAuthDocument>
{
    public SkillAuthDocument(IEntityBaseServices<SkillAuthDocument> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Title { get; set; }
    public partial string Category { get; set; }
    public partial string Content { get; set; }

    [Fetch]
    public void Fetch(int documentId, string category)
    {
        Id = documentId;
        Category = category;
        // Load document content...
    }
}
#endregion

// -----------------------------------------------------------------------------
// Authorization with Services
// -----------------------------------------------------------------------------

#region auth-with-services
/// <summary>
/// Authorization that uses injected services for complex logic.
/// </summary>
public interface ISkillOrderAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch(int orderId);
}

public class SkillOrderAuthorization : ISkillOrderAuthorization
{
    private readonly IPrincipal _principal;
    private readonly ISkillOrderAccessService _accessService;

    public SkillOrderAuthorization(IPrincipal principal, ISkillOrderAccessService accessService)
    {
        _principal = principal;
        _accessService = accessService;
    }

    public bool CanCreate() => _principal.Identity?.IsAuthenticated ?? false;

    // Use service to check if user owns the order
    public bool CanFetch(int orderId)
    {
        var userId = _principal.Identity?.Name;
        if (string.IsNullOrEmpty(userId)) return false;

        // Admin can fetch any order
        if (_principal.IsInRole("Admin")) return true;

        // Regular users can only fetch their own orders
        return _accessService.IsOrderOwner(orderId, userId);
    }
}

public interface ISkillOrderAccessService
{
    bool IsOrderOwner(int orderId, string userId);
}
#endregion

// -----------------------------------------------------------------------------
// Async Authorization
// -----------------------------------------------------------------------------

#region auth-async
/// <summary>
/// Async authorization for operations requiring database lookups.
/// </summary>
public interface ISkillProjectAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    Task<bool> CanFetchAsync(int projectId);

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    Task<bool> CanSaveAsync();
}

public class SkillProjectAuthorization : ISkillProjectAuthorization
{
    private readonly IPrincipal _principal;
    private readonly ISkillProjectMembershipService _membershipService;

    public SkillProjectAuthorization(
        IPrincipal principal,
        ISkillProjectMembershipService membershipService)
    {
        _principal = principal;
        _membershipService = membershipService;
    }

    public async Task<bool> CanFetchAsync(int projectId)
    {
        var userId = _principal.Identity?.Name;
        if (string.IsNullOrEmpty(userId)) return false;

        // Check project membership asynchronously
        return await _membershipService.IsMemberAsync(projectId, userId);
    }

    public async Task<bool> CanSaveAsync()
    {
        var userId = _principal.Identity?.Name;
        if (string.IsNullOrEmpty(userId)) return false;

        // Check user's write permissions
        return await _membershipService.HasWriteAccessAsync(userId);
    }
}

public interface ISkillProjectMembershipService
{
    Task<bool> IsMemberAsync(int projectId, string userId);
    Task<bool> HasWriteAccessAsync(string userId);
}
#endregion

// -----------------------------------------------------------------------------
// Combined Authorization Checks
// -----------------------------------------------------------------------------

#region auth-combined-checks
/// <summary>
/// Authorization interface that combines multiple concerns.
/// A single authorization class can check permissions, feature flags, and more.
/// </summary>
public interface ISkillSecureEntityAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanSave();
}

/// <summary>
/// Combined authorization checking both user permissions and feature flags.
/// </summary>
public class SkillSecureEntityAuthorization : ISkillSecureEntityAuthorization
{
    private readonly IPrincipal _principal;
    private readonly ISkillFeatureFlagService _featureFlags;

    public SkillSecureEntityAuthorization(
        IPrincipal principal,
        ISkillFeatureFlagService featureFlags)
    {
        _principal = principal;
        _featureFlags = featureFlags;
    }

    public bool CanFetch()
    {
        // Check user is authenticated AND feature flag is enabled
        var isAuthenticated = _principal.Identity?.IsAuthenticated ?? false;
        var featureEnabled = _featureFlags.IsEnabled("SecureEntityAccess");

        return isAuthenticated && featureEnabled;
    }

    public bool CanSave()
    {
        // Check user has write role AND feature flag is enabled
        var hasWriteRole = _principal.IsInRole("Admin") || _principal.IsInRole("Editor");
        var featureEnabled = _featureFlags.IsEnabled("SecureEntityAccess");

        return hasWriteRole && featureEnabled;
    }
}

/// <summary>
/// Entity with combined authorization handler.
/// </summary>
[Factory]
[AuthorizeFactory<ISkillSecureEntityAuthorization>]
public partial class SkillAuthSecureEntity : EntityBase<SkillAuthSecureEntity>
{
    public SkillAuthSecureEntity(IEntityBaseServices<SkillAuthSecureEntity> services) : base(services) { }

    public partial int Id { get; set; }
    public partial string Data { get; set; }

    [Fetch]
    public void Fetch(int id) => Id = id;
}

public interface ISkillFeatureFlagService
{
    bool IsEnabled(string featureName);
}
#endregion
