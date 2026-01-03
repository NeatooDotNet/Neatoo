namespace Neatoo.Documentation.Samples.SampleDomain;

/// <summary>
/// Service interface for email-related operations.
/// Used by async validation rules to check email uniqueness.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Checks if an email address already exists in the system.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates).</param>
    /// <returns>True if the email exists; false otherwise.</returns>
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null);
}

/// <summary>
/// Mock implementation of IEmailService for testing.
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly HashSet<string> _existingEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "taken@example.com",
        "existing@test.com"
    };

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
    {
        return Task.FromResult(_existingEmails.Contains(email));
    }
}
