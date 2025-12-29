# Exception Handling

Neatoo provides a structured exception hierarchy for handling framework-specific errors. Understanding these exceptions enables proper error handling in your application.

## Exception Hierarchy

```
NeatooException (abstract)
    |
    +-- PropertyException (abstract)
    |       |
    |       +-- PropertyReadOnlyException
    |       +-- PropertyMissingException
    |       +-- ChildObjectBusyException
    |
    +-- RuleException (abstract)
    |       |
    |       +-- RuleNotAddedException
    |
    +-- EntityException (abstract)
    |       |
    |       +-- SaveOperationException
    |
    +-- ConfigurationException (abstract)
            |
            +-- TypeNotRegisteredException
```

## Base Exception Classes

### NeatooException

The root exception for all Neatoo framework exceptions. Catch this to handle any Neatoo-specific error.

```csharp
try
{
    await personFactory.Save(person);
}
catch (NeatooException ex)
{
    // Handle any Neatoo framework error
    logger.LogError(ex, "Neatoo operation failed");
}
```

### PropertyException

Base exception for property-related errors such as read-only violations or missing properties.

### RuleException

Base exception for rule-related errors during validation.

### EntityException

Base exception for entity-related errors during persistence operations.

### ConfigurationException

Base exception for setup and configuration errors.

## Specific Exceptions

### SaveOperationException

Thrown when a save operation fails on an entity. The `Reason` property indicates why the save failed.

```csharp
try
{
    person = await personFactory.Save(person);
}
catch (SaveOperationException ex)
{
    switch (ex.Reason)
    {
        case SaveFailureReason.IsChildObject:
            // Child entities cannot be saved directly
            message = "Save the parent entity instead.";
            break;

        case SaveFailureReason.IsInvalid:
            // Validation errors exist
            message = "Please fix validation errors before saving.";
            break;

        case SaveFailureReason.NotModified:
            // No changes to save
            message = "No changes have been made.";
            break;

        case SaveFailureReason.IsBusy:
            // Async operations in progress
            message = "Please wait for operations to complete.";
            break;

        case SaveFailureReason.NoFactoryMethod:
            // Missing factory configuration
            message = "Factory method not configured.";
            break;
    }
}
```

#### SaveFailureReason Enum

| Value | Description |
|-------|-------------|
| `IsChildObject` | Child entities cannot be saved directly; save the parent instead |
| `IsInvalid` | The entity has validation errors |
| `NotModified` | No changes to save |
| `IsBusy` | Async operations are in progress |
| `NoFactoryMethod` | No `[Insert]`, `[Update]`, or `[Delete]` method defined |

### ChildObjectBusyException

Thrown when attempting to add or remove a child object that is currently busy with an async operation.

```csharp
try
{
    personPhoneList.Add(newPhone);
}
catch (ChildObjectBusyException ex)
{
    if (ex.IsAddOperation)
    {
        message = "Cannot add a phone that is being validated.";
    }
    else
    {
        message = "Cannot remove a phone that is being validated.";
    }

    // Wait for the operation to complete, then retry
    await newPhone.WaitForTasks();
    personPhoneList.Add(newPhone);
}
```

### TypeNotRegisteredException

Thrown when a required type is not registered in the dependency injection container.

```csharp
catch (TypeNotRegisteredException ex)
{
    // ex.UnregisteredType contains the missing type
    logger.LogError($"Missing registration for {ex.UnregisteredType?.FullName}");
    message = "Application configuration error. Please contact support.";
}
```

**Common causes:**
- Forgot to call `AddNeatooServices()` in Program.cs
- Missing rule registration
- Missing factory assembly in `AddNeatooServices()`

### RuleNotAddedException

Thrown when attempting to run a rule that hasn't been added to the RuleManager.

```csharp
catch (RuleNotAddedException ex)
{
    logger.LogError(ex, "Rule not registered");
    // Ensure rule is added in constructor:
    // RuleManager.AddRule(uniqueNameRule);
}
```

### PropertyReadOnlyException

Thrown when attempting to set a read-only property.

```csharp
catch (PropertyReadOnlyException ex)
{
    message = "This field cannot be modified.";
}
```

### PropertyMissingException

Thrown when a required property cannot be found on an entity.

```csharp
catch (PropertyMissingException ex)
{
    logger.LogError(ex, "Property not found");
    // Check property name matches partial property declaration
}
```

## Best Practices

### 1. Use Specific Exception Types

Catch specific exceptions for targeted error handling:

```csharp
try
{
    person = await personFactory.Save(person);
}
catch (SaveOperationException ex) when (ex.Reason == SaveFailureReason.IsInvalid)
{
    // Show validation errors
    foreach (var msg in person.PropertyMessages)
    {
        errors.Add($"{msg.Property.DisplayName}: {msg.Message}");
    }
}
catch (SaveOperationException ex) when (ex.Reason == SaveFailureReason.IsBusy)
{
    // Wait and retry
    await person.WaitForTasks();
    person = await personFactory.Save(person);
}
catch (NeatooException ex)
{
    // Log unexpected framework errors
    logger.LogError(ex, "Unexpected Neatoo error");
    throw;
}
```

### 2. Check IsSavable Before Save

Avoid exceptions by checking state before saving:

```csharp
async Task SavePerson()
{
    // Wait for async validation
    await person.WaitForTasks();

    // Check if save is possible
    if (!person.IsSavable)
    {
        if (!person.IsValid)
        {
            ShowValidationErrors(person.PropertyMessages);
            return;
        }
        if (person.IsChild)
        {
            ShowMessage("Save through parent entity.");
            return;
        }
        if (!person.IsModified)
        {
            ShowMessage("No changes to save.");
            return;
        }
    }

    // Safe to save
    person = await personFactory.Save(person);
}
```

### 3. Handle Remote Factory Errors

For client-server scenarios, handle network and server errors:

```csharp
try
{
    person = await personFactory.Fetch(id);
}
catch (HttpRequestException ex)
{
    // Network error
    message = "Unable to connect to server.";
}
catch (SaveOperationException ex)
{
    // Server-side validation or save error
    message = ex.Message;
}
catch (NeatooException ex)
{
    // Framework error
    logger.LogError(ex, "Remote factory error");
    message = "An error occurred. Please try again.";
}
```

### 4. Blazor Error Handling Pattern

Complete error handling in a Blazor component:

```razor
@inject IPersonFactory PersonFactory
@inject ILogger<PersonPage> Logger

@if (!string.IsNullOrEmpty(errorMessage))
{
    <MudAlert Severity="Severity.Error" Class="mb-4">@errorMessage</MudAlert>
}

<MudButton Disabled="@(!person.IsSavable)" OnClick="Save">
    @if (saving)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
    }
    Save
</MudButton>

@code {
    private IPerson person = default!;
    private string? errorMessage;
    private bool saving;

    private async Task Save()
    {
        errorMessage = null;
        saving = true;

        try
        {
            await person.WaitForTasks();

            if (!person.IsSavable)
            {
                if (!person.IsValid)
                {
                    errorMessage = "Please correct validation errors.";
                    return;
                }
                return;
            }

            person = await PersonFactory.Save(person);
        }
        catch (SaveOperationException ex)
        {
            errorMessage = ex.Reason switch
            {
                SaveFailureReason.IsInvalid => "Validation failed on server.",
                SaveFailureReason.IsBusy => "Please wait for operations to complete.",
                _ => ex.Message
            };
            Logger.LogWarning(ex, "Save failed: {Reason}", ex.Reason);
        }
        catch (HttpRequestException)
        {
            errorMessage = "Unable to connect to server. Please check your connection.";
        }
        catch (Exception ex)
        {
            errorMessage = "An unexpected error occurred.";
            Logger.LogError(ex, "Unexpected save error");
        }
        finally
        {
            saving = false;
        }
    }
}
```

## Debugging Tips

### Common Exception Scenarios

| Exception | Common Cause | Solution |
|-----------|--------------|----------|
| `TypeNotRegisteredException` | Missing `AddNeatooServices()` | Add to Program.cs |
| `RuleNotAddedException` | Rule not added in constructor | Add `RuleManager.AddRule()` |
| `SaveOperationException(IsInvalid)` | Validation errors | Check `PropertyMessages` |
| `SaveOperationException(NoFactoryMethod)` | Missing `[Insert]`/`[Update]` | Add factory methods |
| `ChildObjectBusyException` | Modifying list during validation | Use `WaitForTasks()` first |

### Logging Recommendations

```csharp
catch (NeatooException ex)
{
    logger.LogError(ex,
        "Neatoo error: Type={ExceptionType}, Message={Message}",
        ex.GetType().Name,
        ex.Message);
}
```

## See Also

- [Factory Operations](factory-operations.md) - Save operation lifecycle
- [Validation and Rules](validation-and-rules.md) - Validation error handling
- [Meta-Properties Reference](meta-properties.md) - IsSavable, IsValid states
