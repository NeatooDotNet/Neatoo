using Neatoo;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Samples;

// Mock external service for async validation samples
public interface IEmailValidationService
{
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken token = default);
}

public class MockEmailValidationService : IEmailValidationService
{
    public Task<bool> IsEmailUniqueAsync(string email, CancellationToken token = default)
    {
        // Simulate async call - emails starting with "taken" are not unique
        return Task.FromResult(!email.StartsWith("taken"));
    }
}

// Mock repository for save samples
public interface IContactRepository
{
    Task InsertAsync(AsyncContact contact);
    Task UpdateAsync(AsyncContact contact);
}

public class MockContactRepository : IContactRepository
{
    public Task InsertAsync(AsyncContact contact) => Task.CompletedTask;
    public Task UpdateAsync(AsyncContact contact) => Task.CompletedTask;
}

#region async-validation-rule
public class UniqueEmailRule : AsyncRuleBase<AsyncContact>
{
    private readonly IEmailValidationService _emailService;

    public UniqueEmailRule(IEmailValidationService emailService)
        : base(c => c.Email) // Trigger when Email changes
    {
        _emailService = emailService;
    }

    protected override async Task<IRuleMessages> Execute(
        AsyncContact target,
        CancellationToken? token = null)
    {
        // Perform async validation
        var isUnique = await _emailService.IsEmailUniqueAsync(
            target.Email ?? "",
            token ?? CancellationToken.None);

        if (!isUnique)
        {
            return (nameof(AsyncContact.Email), "Email is already in use").AsRuleMessages();
        }

        return None;
    }
}
#endregion

// Entity for async samples with multiple rules
[Factory]
public partial class AsyncContact : EntityBase<AsyncContact>
{
    public AsyncContact(
        IEntityBaseServices<AsyncContact> services,
        UniqueEmailRule? emailRule = null) : base(services)
    {
        // Rules can be injected via constructor for dependency injection
        if (emailRule != null)
        {
            RuleManager.AddRule(emailRule);
        }
    }

    public partial int Id { get; set; }

    [Required]
    public partial string Name { get; set; }

    [EmailAddress]
    public partial string Email { get; set; }

    public partial string ZipCode { get; set; }

    public partial decimal TaxRate { get; set; }

    public partial string Status { get; set; }

    public partial string ComputedValue { get; set; }

    public partial int OperationCount { get; set; }

    // Child collection for list samples
    public partial IAsyncContactItemList Items { get; set; }

    [Create]
    public void Create() { }
}

public interface IAsyncContactItem : IEntityBase
{
    string Description { get; set; }
}

[Factory]
public partial class AsyncContactItem : EntityBase<AsyncContactItem>, IAsyncContactItem
{
    public AsyncContactItem(IEntityBaseServices<AsyncContactItem> services) : base(services)
    {
    }

    public partial string Description { get; set; }

    public partial string AsyncValue { get; set; }

    [Create]
    public void Create() { }
}

public interface IAsyncContactItemList : IEntityListBase<IAsyncContactItem> { }

public class AsyncContactItemList : EntityListBase<IAsyncContactItem>, IAsyncContactItemList { }

// Contact with inline async action rules for demonstration
[Factory]
public partial class AsyncActionContact : ValidateBase<AsyncActionContact>
{
    #region async-action-rule
    public AsyncActionContact(IValidateBaseServices<AsyncActionContact> services)
        : base(services)
    {
        // Register an async action that fires when ZipCode changes
        RuleManager.AddActionAsync(
            async contact =>
            {
                // Simulate async lookup (e.g., tax service API call)
                await Task.Delay(10);
                contact.TaxRate = contact.ZipCode?.StartsWith("9") == true ? 0.0825m : 0.07m;
            },
            c => c.ZipCode);
    }
    #endregion

    public partial string ZipCode { get; set; }

    public partial decimal TaxRate { get; set; }

    [Create]
    public void Create() { }
}

// Contact with cancellation token support
[Factory]
public partial class AsyncCancellableContact : ValidateBase<AsyncCancellableContact>
{
    #region async-action-rule-with-token
    public AsyncCancellableContact(IValidateBaseServices<AsyncCancellableContact> services)
        : base(services)
    {
        // Async action with CancellationToken support
        RuleManager.AddActionAsync(
            async (contact, token) =>
            {
                // Check cancellation before expensive operation
                token.ThrowIfCancellationRequested();

                await Task.Delay(50, token); // Honors cancellation
                contact.Status = "Validated";
            },
            c => c.Email);
    }
    #endregion

    public partial string Email { get; set; }

    public partial string Status { get; set; }

    [Create]
    public void Create() { }
}

// Contact with ordered rules for demonstration
[Factory]
public partial class AsyncOrderedRulesContact : ValidateBase<AsyncOrderedRulesContact>
{
    public int FirstRuleExecutionOrder { get; private set; }
    public int SecondRuleExecutionOrder { get; private set; }
    private int _executionCounter;

    public AsyncOrderedRulesContact(IValidateBaseServices<AsyncOrderedRulesContact> services)
        : base(services)
    {
        // Rule execution order is controlled by registration order
        // and the RuleOrder property on the rule

        // This rule registers first
        RuleManager.AddActionAsync(
            async contact =>
            {
                await Task.Delay(5);
                contact.FirstRuleExecutionOrder = ++_executionCounter;
            },
            c => c.Value);

        // This rule registers second, executes after the first
        RuleManager.AddActionAsync(
            async contact =>
            {
                await Task.Delay(5);
                contact.SecondRuleExecutionOrder = ++_executionCounter;
            },
            c => c.Value);
    }

    public partial string Value { get; set; }

    [Create]
    public void Create() { }
}

// Contact with recursive rule chain
[Factory]
public partial class AsyncRecursiveContact : ValidateBase<AsyncRecursiveContact>
{
    public AsyncRecursiveContact(IValidateBaseServices<AsyncRecursiveContact> services)
        : base(services)
    {
        // First rule: triggered by FirstName, sets FullName
        RuleManager.AddActionAsync(
            async contact =>
            {
                await Task.Delay(5);
                contact.FullName = $"{contact.FirstName} {contact.LastName}";
            },
            c => c.FirstName, c => c.LastName);

        // Second rule: triggered by FullName change from first rule
        RuleManager.AddActionAsync(
            async contact =>
            {
                await Task.Delay(5);
                contact.Initials = contact.FullName?.Length > 0
                    ? string.Join("", contact.FullName.Split(' ').Select(n => n.FirstOrDefault()))
                    : "";
            },
            c => c.FullName);
    }

    public partial string FirstName { get; set; }

    public partial string LastName { get; set; }

    public partial string FullName { get; set; }

    public partial string Initials { get; set; }

    [Create]
    public void Create() { }
}

// Contact for error handling demonstration
[Factory]
public partial class AsyncErrorContact : ValidateBase<AsyncErrorContact>
{
    public AsyncErrorContact(IValidateBaseServices<AsyncErrorContact> services)
        : base(services)
    {
        RuleManager.AddActionAsync(
            async contact =>
            {
                await Task.Delay(5);
                if (contact.Value == "error")
                {
                    throw new InvalidOperationException("Async rule failed");
                }
                contact.ProcessedValue = contact.Value?.ToUpper();
            },
            c => c.Value);
    }

    public partial string Value { get; set; }

    public partial string ProcessedValue { get; set; }

    [Create]
    public void Create() { }
}

/// <summary>
/// Tests for async.md snippets demonstrating DI-based factory usage.
/// </summary>
public class AsyncSamplesTests : SamplesTestBase
{
    [Fact]
    public async Task AsyncValidationRule_ValidatesEmailUniqueness()
    {
        // Factory resolves AsyncContact with UniqueEmailRule injected from DI
        var factory = GetRequiredService<IAsyncContactFactory>();
        var contact = factory.Create();

        // Set a unique email
        contact.Email = "unique@example.com";
        await contact.WaitForTasks();

        Assert.True(contact.IsValid);

        // Set an email that's taken
        contact.Email = "taken@example.com";
        await contact.WaitForTasks();

        Assert.False(contact.IsValid);
        Assert.False(contact["Email"].IsValid);
    }

    [Fact]
    public async Task AsyncActionRule_UpdatesTaxRateFromZipCode()
    {
        var factory = GetRequiredService<IAsyncActionContactFactory>();
        var contact = factory.Create();

        contact.ZipCode = "90210"; // California
        await contact.WaitForTasks();

        Assert.Equal(0.0825m, contact.TaxRate);

        contact.ZipCode = "10001"; // New York
        await contact.WaitForTasks();

        Assert.Equal(0.07m, contact.TaxRate);
    }

    #region async-wait-for-tasks
    [Fact]
    public async Task WaitForTasks_EnsuresAsyncRulesComplete()
    {
        var factory = GetRequiredService<IAsyncActionContactFactory>();
        var contact = factory.Create();

        // Setting ZipCode triggers an async rule
        contact.ZipCode = "90210";

        // WaitForTasks ensures the async rule completes
        await contact.WaitForTasks();

        // Now the TaxRate is guaranteed to be set
        Assert.Equal(0.0825m, contact.TaxRate);
    }
    #endregion

    #region async-check-busy
    [Fact]
    public async Task IsBusy_TracksAsyncOperationState()
    {
        var factory = GetRequiredService<IAsyncActionContactFactory>();
        var contact = factory.Create();

        // Trigger async rule
        contact.ZipCode = "90210";

        // Object is busy while async rule executes
        Assert.True(contact.IsBusy);

        // Wait for completion
        await contact.WaitForTasks();

        // No longer busy
        Assert.False(contact.IsBusy);
    }
    #endregion

    #region async-cancellation-token
    [Fact]
    public async Task CancellationToken_CancelsWaitForTasks()
    {
        var factory = GetRequiredService<IAsyncCancellableContactFactory>();
        var contact = factory.Create();

        contact.Email = "test@example.com";

        // Create a token that cancels quickly
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

        // WaitForTasks with cancellation
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await contact.WaitForTasks(cts.Token));

        // After cancellation, must re-run rules to clear invalid state
        await contact.RunRules(RunRulesFlag.All);
    }
    #endregion

    #region async-list-wait-tasks
    [Fact]
    public async Task ListWaitForTasks_WaitsForAllItems()
    {
        var list = new AsyncContactItemList();

        var itemFactory = GetRequiredService<IAsyncContactItemFactory>();
        var item1 = itemFactory.Create();
        var item2 = itemFactory.Create();

        list.Add(item1);
        list.Add(item2);

        // Trigger async operations on items
        item1.Description = "First";
        item2.Description = "Second";

        // WaitForTasks on list waits for all child items
        await list.WaitForTasks();

        // All items are no longer busy
        Assert.False(list.IsBusy);
        Assert.False(item1.IsBusy);
        Assert.False(item2.IsBusy);
    }
    #endregion

    #region async-run-rules
    [Fact]
    public async Task RunRules_ExecutesAllValidationRules()
    {
        var factory = GetRequiredService<IAsyncActionContactFactory>();
        var contact = factory.Create();

        // Set properties without waiting
        contact.ZipCode = "90210";

        // RunRules executes all rules and waits for completion
        await contact.RunRules(RunRulesFlag.All);

        // Validation state is now current
        Assert.True(contact.IsValid);
        Assert.Equal(0.0825m, contact.TaxRate);
    }
    #endregion

    #region async-rule-order
    [Fact]
    public async Task RuleOrder_ControlsExecutionSequence()
    {
        var factory = GetRequiredService<IAsyncOrderedRulesContactFactory>();
        var contact = factory.Create();

        contact.Value = "test";
        await contact.WaitForTasks();

        // Rules execute in registration order
        Assert.Equal(1, contact.FirstRuleExecutionOrder);
        Assert.Equal(2, contact.SecondRuleExecutionOrder);
    }
    #endregion

    #region async-error-handling
    [Fact]
    public async Task AsyncRule_ExceptionsAreCaptured()
    {
        var factory = GetRequiredService<IAsyncErrorContactFactory>();
        var contact = factory.Create();

        // This value causes the rule to throw
        contact.Value = "error";

        // Exception is surfaced when waiting (wrapped in AggregateException)
        var exception = await Assert.ThrowsAsync<AggregateException>(
            async () => await contact.WaitForTasks());

        // The original exception is available via InnerException
        Assert.IsType<InvalidOperationException>(exception.InnerException);

        // Property is marked invalid with exception message
        Assert.False(contact["Value"].IsValid);
    }
    #endregion

    #region async-recursive-rules
    [Fact]
    public async Task RecursiveRules_ChainedRulesExecute()
    {
        var factory = GetRequiredService<IAsyncRecursiveContactFactory>();
        var contact = factory.Create();

        // Setting FirstName triggers FullName rule
        contact.FirstName = "John";
        contact.LastName = "Doe";

        // WaitForTasks waits for the entire chain
        await contact.WaitForTasks();

        // First rule set FullName
        Assert.Equal("John Doe", contact.FullName);

        // Second rule (triggered by FullName) set Initials
        Assert.Equal("JD", contact.Initials);
    }
    #endregion

    #region async-pause-actions
    [Fact]
    public async Task PauseAllActions_BatchesPropertyChanges()
    {
        var factory = GetRequiredService<IAsyncRecursiveContactFactory>();
        var contact = factory.Create();

        // Pause to batch changes
        using (contact.PauseAllActions())
        {
            // These changes don't trigger rules yet
            contact.FirstName = "Jane";
            contact.LastName = "Smith";
        }
        // After resume, manually run rules for all properties
        await contact.RunRules(RunRulesFlag.All);

        // Wait for async rules to complete
        await contact.WaitForTasks();

        // Rules executed once for all changes
        Assert.Equal("Jane Smith", contact.FullName);
        Assert.Equal("JS", contact.Initials);
    }
    #endregion

    #region async-save-entity
    [Fact]
    public async Task Save_WaitsForAsyncValidation()
    {
        // Factory resolves AsyncContact with UniqueEmailRule injected from DI
        var factory = GetRequiredService<IAsyncContactFactory>();
        var contact = factory.Create();

        contact.Name = "Test Contact";
        contact.Email = "valid@example.com";

        // Save automatically calls WaitForTasks before persistence
        // Here we verify the pattern by waiting first
        await contact.WaitForTasks();

        Assert.True(contact.IsValid);
        Assert.True(contact.IsSavable);
        Assert.False(contact.IsBusy);
    }
    #endregion

    [Fact]
    public async Task AsyncCancellableContact_WithToken_Completes()
    {
        var factory = GetRequiredService<IAsyncCancellableContactFactory>();
        var contact = factory.Create();

        contact.Email = "test@example.com";

        // Wait with a long timeout - should complete normally
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await contact.WaitForTasks(cts.Token);

        Assert.Equal("Validated", contact.Status);
        Assert.False(contact.IsBusy);
    }
}
