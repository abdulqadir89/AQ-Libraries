# AQ.DataSeeding

Reusable data seeding library for Entity Framework Core with support for multiple seeder types and dependency resolution.

## Features

- **Multiple Seeder Types**: Built-in support for test data, configuration data, and migration data
- **Dependency Resolution**: Automatically resolves and executes seeders in the correct order based on dependencies
- **Priority Ordering**: Control execution order within the same dependency level
- **Generic Design**: Works with any `DbContext` type
- **Type-Safe**: Strongly typed with generic constraints
- **Extensible**: Easy to add custom seeder types

## Installation

Add a project reference to `AQ.DataSeeding`:

```xml
<ProjectReference Include="path\to\AQ.DataSeeding\AQ.DataSeeding.csproj" />
```

## Usage

### 1. Create a Seeder

Implement `IDataSeeder<TSeederType>` with a seeder type marker interface:

```csharp
using AQ.DataSeeding;
using AQ.DataSeeding.Types;

public class UserSeeder(MyDbContext dbContext) : IDataSeeder<ITestDataSeeder>
{
    public async Task SeedAsync()
    {
        if (await dbContext.Users.AnyAsync()) return;
        
        dbContext.Users.Add(new User { Name = "John Doe" });
        await dbContext.SaveChangesAsync();
    }

    public IEnumerable<Type> Dependencies => Array.Empty<Type>();
    
    public int Priority => 0; // Optional: Lower numbers run first
}
```

### 2. Create a Seeder with Dependencies

```csharp
public class CourseSeeder(MyDbContext dbContext) : IDataSeeder<ITestDataSeeder>
{
    public async Task SeedAsync()
    {
        if (await dbContext.Courses.AnyAsync()) return;
        
        dbContext.Courses.Add(new Course { Title = "Introduction to C#" });
        await dbContext.SaveChangesAsync();
    }

    // This seeder depends on UserSeeder
    public IEnumerable<Type> Dependencies => new[] { typeof(UserSeeder) };
}
```

### 3. Register Seeders in Dependency Injection

```csharp
using AQ.DataSeeding;

// Option 1: Register all test data seeders
services.AddTestDataSeeders<MyDbContext>();

// Option 2: Register specific seeder type
services.AddDataSeeders<ITestDataSeeder, MyDbContext>();

// Option 3: Scan specific assemblies
services.AddTestDataSeeders<MyDbContext>(Assembly.GetExecutingAssembly());

// Option 4: Register multiple seeder types
services.AddTestDataSeeders<MyDbContext>();
services.AddConfigurationSeeders<MyDbContext>();
services.AddMigrationSeeders<MyDbContext>();
```

### 4. Use the Seeding Service

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<MyDbContext>();
                services.AddTestDataSeeders<MyDbContext>();
            })
            .Build();

        // Get the seeding service
        using var scope = host.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeedingService<ITestDataSeeder, MyDbContext>>();
        
        // Seed all data
        await seeder.SeedAllAsync();
        
        // Or reset database (drop, migrate, seed)
        await seeder.ResetAllAsync();
        
        await host.RunAsync();
    }
}
```

## Built-in Seeder Types

### Test Data Seeder
Use `ITestDataSeeder` for test/demo data:
```csharp
public class MySeeder : IDataSeeder<ITestDataSeeder> { ... }
```

### Configuration Seeder
Use `IConfigurationSeeder` for configuration data:
```csharp
public class MySeeder : IDataSeeder<IConfigurationSeeder> { ... }
```

### Migration Seeder
Use `IMigrationSeeder` for migration data:
```csharp
public class MySeeder : IDataSeeder<IMigrationSeeder> { ... }
```

## Creating Custom Seeder Types

1. Create a marker interface:
```csharp
public interface IMyCustomSeeder : ISeederType { }
```

2. Implement seeders:
```csharp
public class MySeeder : IDataSeeder<IMyCustomSeeder> { ... }
```

3. Register and use:
```csharp
services.AddDataSeeders<IMyCustomSeeder, MyDbContext>();
var seeder = services.GetRequiredService<DataSeedingService<IMyCustomSeeder, MyDbContext>>();
await seeder.SeedAllAsync();
```

## Advanced Features

### Priority Ordering
Control execution order within the same dependency level:
```csharp
public class FirstSeeder : IDataSeeder<ITestDataSeeder>
{
    public int Priority => 1; // Runs before lower priority seeders
}

public class SecondSeeder : IDataSeeder<ITestDataSeeder>
{
    public int Priority => 10; // Runs after higher priority seeders
}
```

### Database Operations
The `DataSeedingService` provides utility methods:
```csharp
// Drop database
await seeder.DropDatabaseAsync();

// Apply migrations
await seeder.ApplyMigrationsAsync();

// Full reset (drop + migrate + seed)
await seeder.ResetAllAsync();

// Get seeder count
int count = seeder.GetSeederCount();
```

## Best Practices

1. **Idempotent Seeders**: Always check if data exists before seeding
2. **Dependency Order**: Declare dependencies to ensure correct execution order
3. **Use Priority**: Use priority for explicit ordering when dependencies aren't enough
4. **Separate Concerns**: Use different seeder types for different purposes
5. **Clean Data**: Use `ResetAllAsync()` for testing scenarios

## License

See the LICENSE file in the repository root.
