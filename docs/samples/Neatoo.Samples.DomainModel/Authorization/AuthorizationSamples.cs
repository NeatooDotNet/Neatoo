/// <summary>
/// Code samples for authorization skill - Authorization patterns
///
/// Full snippets:
/// - docs:authorization:auth-interface
/// - docs:authorization:auth-implementation
/// - docs:authorization:entity-with-auth
/// - docs:authorization:operation-specific
/// - docs:authorization:role-based
///
/// Corresponding tests: AuthorizationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.Authorization;

#region docs:authorization:auth-interface
/// <summary>
/// Authorization interface defines which operations require authorization.
/// Methods return bool: true = authorized, false = denied.
/// </summary>
public interface IDocumentAuth
{
    /// <summary>
    /// Authorize Read and Write operations (general access).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    /// <summary>
    /// Authorize Create operation specifically.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Authorize Fetch operation specifically.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    /// <summary>
    /// Authorize Insert operation (new records).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
    bool CanInsert();

    /// <summary>
    /// Authorize Update operation (modify existing).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool CanUpdate();

    /// <summary>
    /// Authorize Delete operation.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
#endregion

#region docs:authorization:auth-implementation
/// <summary>
/// Authorization implementation checks user permissions.
/// Injected via DI with current user context.
/// </summary>
public class DocumentAuth : IDocumentAuth
{
    private readonly ICurrentUser _currentUser;

    public DocumentAuth(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public bool HasAccess() => _currentUser.IsAuthenticated;

    public bool CanCreate() => _currentUser.HasPermission(Permission.Create);

    public bool CanFetch() => _currentUser.HasPermission(Permission.Read);

    public bool CanInsert() => _currentUser.HasPermission(Permission.Create);

    public bool CanUpdate() => _currentUser.HasPermission(Permission.Update);

    public bool CanDelete() => _currentUser.HasPermission(Permission.Delete);
}
#endregion

#region docs:authorization:entity-with-auth
/// <summary>
/// Entity with authorization - all factory operations are protected.
/// </summary>
public partial interface IDocument : IEntityBase
{
    Guid Id { get; }
    string? Title { get; set; }
    string? Content { get; set; }
}

[Factory]
[AuthorizeFactory<IDocumentAuth>]
internal partial class Document : EntityBase<Document>, IDocument
{
    public Document(IEntityBaseServices<Document> services) : base(services) { }

    public partial Guid Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    public partial string? Title { get; set; }

    public partial string? Content { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    public void Fetch(Guid id, string title, string content)
    {
        Id = id;
        Title = title;
        Content = content;
    }

    [Insert]
    public Task Insert()
    {
        // In real code: persist to database
        return Task.CompletedTask;
    }

    [Update]
    public Task Update()
    {
        // In real code: update database record
        return Task.CompletedTask;
    }

    [Delete]
    public Task Delete()
    {
        // In real code: delete from database
        return Task.CompletedTask;
    }
}
#endregion

#region docs:authorization:operation-specific
/// <summary>
/// Different authorization rules for different operations.
/// Read operations are public, write operations require admin.
/// </summary>
public interface IPublicReadAuth
{
    /// <summary>
    /// Anyone can read/fetch.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch | AuthorizeFactoryOperation.Read)]
    bool CanRead();

    /// <summary>
    /// Only admins can create/modify.
    /// </summary>
    [AuthorizeFactory(
        AuthorizeFactoryOperation.Create |
        AuthorizeFactoryOperation.Insert |
        AuthorizeFactoryOperation.Update |
        AuthorizeFactoryOperation.Delete |
        AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

public class PublicReadAuth : IPublicReadAuth
{
    private readonly ICurrentUser _currentUser;

    public PublicReadAuth(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Read is always allowed (public access).
    /// </summary>
    public bool CanRead() => true;

    /// <summary>
    /// Write requires Admin role.
    /// </summary>
    public bool CanWrite() => _currentUser.IsInRole("Admin");
}
#endregion

#region docs:authorization:role-based
/// <summary>
/// Role-based authorization with hierarchical permissions.
/// </summary>
public interface IRoleBasedAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Fetch)]
    bool HasReadAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Insert)]
    bool HasCreateAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool HasUpdateAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool HasDeleteAccess();
}

public class RoleBasedAuth : IRoleBasedAuth
{
    private readonly ICurrentUser _currentUser;

    public RoleBasedAuth(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Users, Editors, and Admins can read.
    /// </summary>
    public bool HasReadAccess() =>
        _currentUser.IsInRole("User") ||
        _currentUser.IsInRole("Editor") ||
        _currentUser.IsInRole("Admin");

    /// <summary>
    /// Editors and Admins can create.
    /// </summary>
    public bool HasCreateAccess() =>
        _currentUser.IsInRole("Editor") ||
        _currentUser.IsInRole("Admin");

    /// <summary>
    /// Editors and Admins can update.
    /// </summary>
    public bool HasUpdateAccess() =>
        _currentUser.IsInRole("Editor") ||
        _currentUser.IsInRole("Admin");

    /// <summary>
    /// Only Admins can delete.
    /// </summary>
    public bool HasDeleteAccess() =>
        _currentUser.IsInRole("Admin");
}

/// <summary>
/// Entity with role-based authorization.
/// </summary>
public partial interface IArticle : IEntityBase
{
    string? Title { get; set; }
}

[Factory]
[AuthorizeFactory<IRoleBasedAuth>]
internal partial class Article : EntityBase<Article>, IArticle
{
    public Article(IEntityBaseServices<Article> services) : base(services) { }

    public partial string? Title { get; set; }

    [Create]
    public void Create() { }
}
#endregion

// Supporting types for authorization samples

/// <summary>
/// Represents the current user context for authorization checks.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    bool HasPermission(Permission permission);
    bool IsInRole(string role);
}

/// <summary>
/// Permission flags for fine-grained access control.
/// </summary>
[Flags]
public enum Permission
{
    None = 0,
    Read = 1,
    Create = 2,
    Update = 4,
    Delete = 8,
    All = Read | Create | Update | Delete
}

/// <summary>
/// Mock current user for testing authorization.
/// </summary>
public class MockCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; }
    public Permission Permissions { get; set; }
    public HashSet<string> Roles { get; } = new();

    public bool HasPermission(Permission permission) =>
        (Permissions & permission) == permission;

    public bool IsInRole(string role) =>
        Roles.Contains(role);
}
