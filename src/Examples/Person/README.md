# Person Example

This example demonstrates using Neatoo with Entity Framework Core for a simple Person domain model.

## Projects

- **Person.DomainModel** - Domain objects using Neatoo base classes
- **Person.Ef** - Entity Framework Core entities and DbContext
- **Person.App** - Application entry point and configuration
- **Person.DomainModel.Tests** - Unit and integration tests

## Entity Framework Migrations

### Package Manager Console

Run these commands from the Package Manager Console in Visual Studio:

```powershell
# Apply migrations to create/update database
Update-Database -Project Person.Ef -StartupProject Person.App

# Add a new migration
Add-Migration MigrationName -Project Person.Ef -StartupProject Person.App

# Remove last migration (if not applied)
Remove-Migration -Project Person.Ef -StartupProject Person.App

# Generate SQL script instead of applying directly
Script-Migration -Project Person.Ef -StartupProject Person.App

# Update to a specific migration
Update-Database -Migration MigrationName -Project Person.Ef -StartupProject Person.App

# Revert all migrations (empty database)
Update-Database -Migration 0 -Project Person.Ef -StartupProject Person.App
```

### .NET CLI

Alternatively, use the .NET CLI from the repository root:

```bash
# Apply migrations
dotnet ef database update --project src/Examples/Person/Person.Ef --startup-project src/Examples/Person/Person.App

# Add a new migration
dotnet ef migrations add MigrationName --project src/Examples/Person/Person.Ef --startup-project src/Examples/Person/Person.App

# Remove last migration
dotnet ef migrations remove --project src/Examples/Person/Person.Ef --startup-project src/Examples/Person/Person.App

# Generate SQL script
dotnet ef migrations script --project src/Examples/Person/Person.Ef --startup-project src/Examples/Person/Person.App
```

## Prerequisites

- .NET 8.0 or later
- SQL Server (LocalDB or full instance)
- `Microsoft.EntityFrameworkCore.Tools` package (for Package Manager Console commands)
