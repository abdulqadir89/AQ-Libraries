# Sorting System

A comprehensive sorting system for dynamic sorting of collections with support for complex expressions, nested properties, and multiple sort conditions.

## Features

- **Simple Sorting**: Single field sorting with direction
- **Complex Sorting**: Multiple fields with individual directions
- **Nested Properties**: Support for deep property paths (e.g., `Parent.Name`)
- **Multiple Directions**: Mix ascending and descending in same query
- **Type Safety**: Compile-time validation of property paths
- **Case Sensitivity**: Optional case-sensitive sorting for strings
- **Null Handling**: Control where null values appear in results

## Usage Examples

### Simple Sorting

```csharp
// Single field, ascending (default)
var spec = SortExpressionParser.Parse("Name");

// Single field with explicit direction
var spec = SortExpressionParser.Parse("Name,desc");
```

### Complex Multi-Field Sorting

```csharp
// Multiple fields with different directions
var spec = SortExpressionParser.Parse("Parent.Name,asc;Description,desc;CreatedDate,desc");

// Using the builder
var spec = SortExpressionBuilder.Create()
    .OrderByAscending("Parent.Name")
    .OrderByDescending("Description")
    .OrderByDescending("CreatedDate")
    .Build();
```

### Apply to IQueryable

```csharp
// Apply sort specification
var sortedQuery = query.ApplySort(sortSpecification);

// Apply sort expression string directly
var sortedQuery = query.ApplySort("Parent.Name,asc;Description,desc");

// Apply with default fallback
var sortedQuery = query.ApplySortWithDefault(sortSpecification, "Id", SortDirection.Ascending);
```

### Using in Queries

```csharp
public record GetUsersQuery : BaseGetAllQuery<PagedResult<UserDto>>
{
    // SortExpression is inherited from BaseGetAllQuery
}

// In query handler
public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
{
    var query = dbContext.Users.AsQueryable();
    
    // Apply sorting
    query = query.ApplySort(request.SortExpression);
    
    // Continue with filtering, pagination, etc.
    return await query.ToPagedResultAsync(request.PageNumber, request.PageSize);
}
```

## Expression Format

### Basic Format
```
PropertyPath,Direction
```

### Multiple Conditions
```
PropertyPath1,Direction1;PropertyPath2,Direction2;PropertyPathN,DirectionN
```

### Supported Directions
- `asc`, `ascending` - Ascending order
- `desc`, `descending` - Descending order

### Examples

```
// Simple
"Name,asc"
"CreatedDate,desc"

// Nested properties
"User.Profile.FirstName,asc"
"Department.Manager.Name,desc"

// Multiple conditions
"LastName,asc;FirstName,asc"
"Priority,desc;DueDate,asc;Title,asc"
"Parent.Name,asc;Description,desc;CreatedDate,desc"
```

## Validation

```csharp
// Check if expression is valid
bool isValid = SortExpressionParser.IsValidExpression("Name,asc;Age,desc");

// Check if valid for specific type
bool isValidForType = specification.IsValidForType<User>();

// Get available sortable properties
string[] properties = SortExtensions.GetSortableProperties<User>(includeNestedProperties: true);
```

## Builder Pattern

```csharp
var specification = SortExpressionBuilder.Create()
    .OrderByAscending("LastName")
    .OrderByAscending("FirstName")
    .OrderByDescending("CreatedDate")
    .Build();

// From existing expression
var builder = SortExpressionBuilder.FromExpression("Name,asc;Age,desc")
    .OrderByDescending("CreatedDate")
    .Build();
```

## Integration with BaseQuery

The sorting system integrates seamlessly with the existing `BaseGetAllQuery`:

```csharp
public record GetUsersQuery : BaseGetAllQuery<PagedResult<UserDto>>
{
    // SortExpression is available from base class
    // Can be used alongside FilterExpression, SearchTerm, etc.
}
```

## Error Handling

- Invalid property paths are skipped silently
- Invalid directions throw `ArgumentException`
- Malformed expressions return empty specifications
- Type validation can be performed before applying sorts

## Performance Considerations

- Property paths are resolved at runtime using reflection
- Consider caching sort expressions for frequently used queries
- Nested property sorting may impact performance on large datasets
- Use indexes on commonly sorted fields

## Thread Safety

All components are thread-safe and can be used in concurrent environments.
