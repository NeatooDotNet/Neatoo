/// <summary>
/// Code samples for docs/database-dependent-validation.md
///
/// Snippets in this file:
/// - docs:database-dependent-validation:anti-pattern (DON'T DO THIS)
/// - docs:database-dependent-validation:command-pattern
/// - docs:database-dependent-validation:async-rule
/// - docs:database-dependent-validation:clean-factory
///
/// Corresponding tests: AsyncValidationSamplesTests.cs
/// </summary>

using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.Samples.DomainModel.DatabaseValidation;

/// <summary>
/// Mock repository for email uniqueness checks.
/// </summary>
public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, Guid? excludeId);
}

public class MockUserRepository : IUserRepository
{
    private readonly HashSet<string> _existingEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "existing@example.com",
        "taken@example.com",
        "admin@example.com"
    };

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId)
    {
        return Task.FromResult(_existingEmails.Contains(email));
    }
}

#region anti-pattern
/// <summary>
/// ANTI-PATTERN: Do NOT put validation in factory methods.
/// This example shows what NOT to do.
/// </summary>
/// <remarks>
/// Problems with this approach:
/// 1. Poor UX - Users only see errors after clicking Save
/// 2. Throws exceptions instead of validation messages
/// 3. Bypasses rule system - no IsBusy, no UI integration
/// 4. Returns HTTP 500 instead of validation error
/// </remarks>
public static class AntiPatternExample
{
    /// <summary>
    /// BAD: Validation logic inside Insert method.
    /// </summary>
    public static async Task Insert_AntiPattern(
        IUserWithEmail user,
        IUserRepository repo)
    {
        // DON'T DO THIS - validation only runs at save time!
        if (await repo.EmailExistsAsync(user.Email!, null))
            throw new InvalidOperationException("Email already in use");

        // ... persistence would go here
    }
}
#endregion

#region command-pattern
/// <summary>
/// Command for checking email uniqueness.
/// The source generator creates a delegate that can be injected and executed remotely.
/// </summary>
[Factory]
public static partial class CheckEmailUnique
{
    [Execute]
    internal static async Task<bool> _IsUnique(
        string email,
        Guid? excludeId,
        [Service] IUserRepository repo)
    {
        return !await repo.EmailExistsAsync(email, excludeId);
    }
}
#endregion

#region async-rule
/// <summary>
/// Async rule that validates email uniqueness using the command.
/// </summary>
public interface IAsyncUniqueEmailRule : IRule<IUserWithEmail> { }

public class AsyncUniqueEmailRule : AsyncRuleBase<IUserWithEmail>, IAsyncUniqueEmailRule
{
    private readonly CheckEmailUnique.IsUnique _isUnique;

    public AsyncUniqueEmailRule(CheckEmailUnique.IsUnique isUnique)
    {
        _isUnique = isUnique;
        AddTriggerProperties(u => u.Email);
    }

    protected override async Task<IRuleMessages> Execute(
        IUserWithEmail target, CancellationToken? token = null)
    {
        if (string.IsNullOrEmpty(target.Email))
            return None;

        // Skip if property not modified (optimization)
        if (!target.IsNew && !target[nameof(target.Email)].IsModified)
            return None;

        var excludeId = target.IsNew ? null : (Guid?)target.Id;

        if (!await _isUnique(target.Email, excludeId))
        {
            return (nameof(target.Email), "Email already in use").AsRuleMessages();
        }

        return None;
    }
}
#endregion

/// <summary>
/// Entity demonstrating async validation pattern.
/// </summary>
public partial interface IUserWithEmail : IEntityBase
{
    Guid Id { get; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string? Email { get; set; }

    string? Name { get; set; }
}

#region clean-factory
/// <summary>
/// Entity with clean factory methods - validation is in rules, not here.
/// </summary>
[Factory]
internal partial class UserWithEmail : EntityBase<UserWithEmail>, IUserWithEmail
{
    public UserWithEmail(
        IEntityBaseServices<UserWithEmail> services,
        IAsyncUniqueEmailRule uniqueEmailRule) : base(services)
    {
        // Register the async validation rule
        RuleManager.AddRule(uniqueEmailRule);
    }

    public partial Guid Id { get; set; }
    public partial string? Email { get; set; }
    public partial string? Name { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Clean Insert - only persistence logic, no validation.
    /// Validation is handled by rules during editing.
    /// </summary>
    [Insert]
    public async Task Insert()
    {
        await RunRules();
        if (!IsSavable)
            return;

        // Only persistence - validation already handled by rules
        // In real code: await repository.InsertAsync(entity);
    }
}
#endregion
