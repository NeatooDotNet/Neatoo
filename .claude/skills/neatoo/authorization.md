# Neatoo Authorization Reference

## Overview

Neatoo's authorization system controls access to factory operations. Authorization checks run both client-side (for UI decisions) and server-side (for enforcement), ensuring security through the `[AuthorizeFactory]` attribute.

## Key Concepts

| Concept | Purpose |
|---------|---------|
| `[AuthorizeFactory]` | Marks authorization methods |
| `IDocumentAuth` interface | Defines authorization rules for an entity |
| `AuthorizeFactoryOperation` flags | Specifies which operations require authorization |

## AuthorizeFactoryOperation Enum

| Flag | Value | Purpose |
|------|-------|---------|
| `Create` | 1 | Creating new instances |
| `Fetch` | 2 | Reading/fetching instances |
| `Insert` | 4 | Inserting new records |
| `Update` | 8 | Modifying existing records |
| `Delete` | 16 | Removing records |
| `Read` | 32 | General read access |
| `Write` | 64 | General write access |
| `Execute` | 128 | Executing special operations |

## Authorization Interface

Define an interface with methods marked by `[AuthorizeFactory]`:

<!-- snippet: auth-interface -->
```csharp
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
```
<!-- /snippet -->

## Authorization Implementation

Implement the interface with your authorization logic:

<!-- snippet: auth-implementation -->
```csharp
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
```
<!-- /snippet -->

## Entity with Authorization

Apply `[AuthorizeFactory<TAuth>]` to protect an entity:

<!-- snippet: entity-with-auth -->
```csharp
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
```
<!-- /snippet -->

## Operation-Specific Authorization

Different operations can have different requirements:

<!-- snippet: operation-specific -->
```csharp
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
```
<!-- /snippet -->

## Role-Based Authorization

Hierarchical permissions based on user roles:

<!-- snippet: role-based -->
```csharp
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
```
<!-- /snippet -->

## Best Practices

1. **Always check on server** - Client checks can be bypassed
2. **Use meaningful messages** - Help users understand why access was denied
3. **Combine with role checks** - Use ASP.NET Core's authentication middleware
4. **Test authorization rules** - Ensure coverage of all paths
