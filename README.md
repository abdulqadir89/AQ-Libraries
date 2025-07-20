# AQ Libraries

A collection of reusable .NET libraries following Clean Architecture principles and Domain-Driven Design patterns.

## Packages

- **AQ.Common.Domain** - Domain layer containing business entities, value objects, and domain services
- **AQ.Common.Application** - Application layer with use cases, DTOs, and application services
- **AQ.Common.Infrastructure** - Infrastructure layer with data access, external services, and configurations
- **AQ.Common.Presentation** - Presentation layer with API controllers, view models, and web-specific logic

## Installation

Install the packages you need via NuGet Package Manager or .NET CLI:

```bash
dotnet add package AQ.Common.Domain
dotnet add package AQ.Common.Application
dotnet add package AQ.Common.Infrastructure
dotnet add package AQ.Common.Presentation
```

## Features

- Clean Architecture implementation
- Domain-Driven Design patterns
- CQRS support
- Event-driven architecture
- Multi-targeting (.NET 8.0 and .NET 9.0)
- Comprehensive unit tests
- Continuous integration and deployment

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.