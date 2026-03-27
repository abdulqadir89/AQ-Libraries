# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is a monorepo containing reusable domain-driven design (DDD) and utility libraries for .NET, plus a React component library. The focus is on providing well-designed, composable building blocks for applications.

### Directory Structure

- **dotnet/** - Main C# library suite
  - `src/` - Library implementations
  - `tests/` - Unit tests organized to mirror source structure
  - `AQ.sln` - Solution file for the entire .NET ecosystem

- **react/** - TypeScript React component library
  - Built on Mantine v8+ with TypeScript
  - Requires React 19+, React DOM 19+, TanStack Query v5+
  - Type definitions via TypeScript

## Quick Commands

### .NET (run from `dotnet/` directory)

**Build & Lint**
```bash
# Restore dependencies
dotnet restore AQ.sln

# Format code (required by CI, will fix most violations)
dotnet format AQ.sln

# Build the entire solution
dotnet build AQ.sln

# Build with Release configuration
dotnet build AQ.sln --configuration Release
```

**Testing**
```bash
# Run all unit tests
dotnet test AQ.sln

# Run tests in Release configuration
dotnet test AQ.sln --configuration Release

# Run specific test project
dotnet test tests/StateMachine/Services/AQ.StateMachine.Services.Tests.csproj

# Run specific test class or method
dotnet test AQ.sln --filter "ClassName=MyClass"
dotnet test AQ.sln --filter "FullyQualifiedName~MyNamespace.MyClass.MyMethod"
```

**Packaging**
```bash
# Pack all libraries
dotnet pack AQ.sln --configuration Release
```

### React (run from `react/` directory)

The React library currently has no build or test scripts. It's a type-definition package with Mantine components as peer dependencies.

## Architecture & Key Patterns

### Domain-Driven Design Focus

The library suite is organized around DDD principles:

1. **AQ.Entities** - Base `Entity<TId>` class for aggregate roots and domain entities
   - Automatically assigned incremental IDs by default
   - Provides common entity patterns
   - Foundation for other domain libraries

2. **AQ.ValueObjects** - Support for value object patterns
   - Often used with Markdig (Markdown) and HtmlSanitizer for content handling

3. **AQ.CQRS** - Command/Query Responsibility Segregation
   - Depends on AQ.Utilities.Results and AQ.Utilities.Sort
   - Provides patterns for separating read/write operations

4. **AQ.Extensions** - Utility extension methods
   - Integrates with Microsoft.Extensions.Logging for consistency

5. **AQ.Events*** - Event-driven architecture support
   - **AQ.Events.Domain** - Domain event interfaces and types
   - **AQ.Events.Integration** - Integration event handling
   - **AQ.Events.Dispatchers** - Event dispatching infrastructure
   - **AQ.Events.Outbox** - Outbox pattern for reliable event publishing

### Utilities (organized in separate folder)

The `Utilities` folder contains cross-cutting libraries:

- **AQ.Utilities.Filter** - Building block for query filtering
- **AQ.Utilities.Search** - Search query utilities
- **AQ.Utilities.Sort** - Sorting/ordering utilities
- **AQ.Utilities.Results** - Result pattern for error handling (used by CQRS)

### State Machine System

Located in `src/StateMachine/`, this is a sophisticated system for modeling complex business workflows:

**Key Components:**
- **AQ.StateMachine.Entities** - Core state machine entities and abstraction layer
- **AQ.StateMachine.Services** - Service layer for executing state transitions

**Core Concepts:**
- **Requirements System**: Transitions can declare requirements that must be evaluated before execution
  - **Specific Handlers**: `IStateMachineTransitionRequirementHandler<TRequirement>` - typed handlers for specific requirement types
  - **Generic Handlers**: `IStateMachineTransitionHandler` - process all requirements with status tracking (multiple handlers supported)
  - Requirements use `typeof(T).Name` for type discovery - no manual type strings
  - **User Data Collection**: Requirements can specify what data entities must be collected; handlers fetch directly from database

- **Entity-Based Operations**: All transitions use `StateMachineState` and `StateMachineTrigger` entities, not strings

- **Two-Phase Evaluation**: Specific handlers first, then all registered generic handlers process with status

- See [src/StateMachine/Entities/README.md](dotnet/src/StateMachine/Entities/README.md) for detailed usage patterns

### Data Seeding

**AQ.DataSeeding** provides reusable EF Core data seeding with:
- Multiple seeder types (test data, configuration, migration)
- Automatic dependency resolution
- Priority-based ordering within dependency levels
- See [src/DataSeeding/README.md](dotnet/src/DataSeeding/README.md) for usage

## Dependency Management

- **Central Package Management**: All NuGet versions are managed in `Directory.Packages.props`
- **.NET Version**: Net10.0 target framework (see Directory.Packages.props)
- **Testing**: xUnit with NSubstitute for mocking, FluentAssertions for readable assertions, Coverlet for code coverage
- **EF Core**: Version 10.0.5 - used across data seeding and domain event libraries

## Testing Conventions

- Test projects located in `dotnet/tests/` mirror the source structure
- Test project names follow pattern: `AQ.[Feature].Tests` or `AQ.[Namespace].Tests`
- Use `dotnet test` with `--filter` parameter to run specific tests
- Tests automatically collect code coverage data (XPlat Code Coverage format)

## CI/CD & Code Quality

### Automated Checks (GitHub Actions)

The repository enforces:
- Code formatting via `dotnet format` (Required - will reject unformatted code)
- Full test suite must pass (Release configuration)
- Package metadata validation
- Merge conflict detection
- No direct merges from main/master to main/master branches

### Running Locally Before Push

Always run locally to catch issues before CI:
```bash
cd dotnet/
dotnet format AQ.sln  # Fix formatting
dotnet build AQ.sln --configuration Release
dotnet test AQ.sln --configuration Release
```

## Project Assumptions

- **Implicit Usings**: Enabled in all projects - no need for explicit using statements for standard namespaces
- **Nullable Reference Types**: Enabled - handle nullability explicitly
- **No Service Abstractions for Data**: Handlers fetch required data directly from database via DbContext, not through services
- **Type Safety Over Runtime Types**: Use generic constraints and reflection via `typeof(T).Name` rather than manual type string constants
