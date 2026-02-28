# Dynamic Filtering System

This library provides a comprehensive, database-independent filtering system that works with Entity Framework Core, LINQ, and IQueryable. It supports complex filter expressions with nested properties, logical operators, and various comparison operations.

## Features

- **Database Independent**: Works with any EF Core provider
- **Nested Property Support**: Filter on nested objects (e.g., `User.Profile.Name`)
- **Multiple Operators**: Supports all common comparison operators
- **Logical Combinations**: Combine filters with AND/OR logic and parentheses
- **Type Safe**: Automatic type conversion and null handling
- **Fluent API**: Easy-to-use fluent interface
- **Request Integration**: Built-in support for API request models

## Supported Operators

| Operator | Aliases | Description | Example |
|----------|---------|-------------|---------|
| `Equal` | `eq`, `equal`, `=` | Equality check | `Name,eq,John` |
| `NotEqual` | `ne`, `notequal`, `!=` | Not equal | `Status,ne,Inactive` |
| `GreaterThan` | `gt`, `>` | Greater than | `Age,gt,25` |
| `GreaterThanOrEqual` | `gte`, `>=` | Greater than or equal | `Salary,gte,50000` |
| `LessThan` | `lt`, `<` | Less than | `Price,lt,100` |
| `LessThanOrEqual` | `lte`, `<=` | Less than or equal | `Quantity,lte,10` |
| `Contains` | `contains`, `like` | String contains | `Name,contains,John` |
| `NotContains` | `notcontains`, `notlike` | String not contains | `Email,notcontains,temp` |
| `StartsWith` | `startswith` | String starts with | `Code,startswith,ABC` |
| `EndsWith` | `endswith` | String ends with | `File,endswith,.pdf` |
| `IsNull` | `isnull`, `null` | Is null check | `DeletedAt,isnull` |
| `IsNotNull` | `isnotnull`, `notnull` | Is not null check | `Email,isnotnull` |
| `In` | `in` | Value in list | `Status,in,Active,Pending` |
| `NotIn` | `notin` | Value not in list | `Type,notin,Draft,Deleted` |
| `Between` | `between` | Between two values | `Date,between,2023-01-01,2023-12-31` |
| `NotBetween` | `notbetween` | Not between values | `Score,notbetween,0,50` |

## Usage Examples

### 1. Simple Filtering

```csharp
// Method 1: Using filter expression strings
var activeUsers = dbContext.Users
    .ApplyFilter("IsActive,eq,true")
    .ToList();

// Method 2: Using direct parameters (no parsing needed)
var activeUsers = dbContext.Users
    .ApplyFilter("IsActive", FilterOperator.Equal, true)
    .ToList();

// Method 3: Using strongly-typed convenience methods
var activeUsers = dbContext.Users
    .WhereEqual("IsActive", true)
    .ToList();

// String filtering with case sensitivity options
var products = dbContext.Products
    .WhereContains("Name", "Phone", caseSensitive: false)
    .ToList();
```

### 2. Multiple Methods for the Same Goal

```csharp
// All these methods achieve the same result - filtering by nested property
// Method 1: Filter expression string (requires parsing)
var result1 = dbContext.Employees
    .ApplyFilter("Department.Name,eq,Engineering")
    .ToList();

// Method 2: Direct parameters (no parsing needed)
var result2 = dbContext.Employees
    .ApplyFilter("Department.Name", FilterOperator.Equal, "Engineering")
    .ToList();

// Method 3: Strongly-typed convenience method
var result3 = dbContext.Employees
    .WhereEqual("Department.Name", "Engineering")
    .ToList();

// Special operators with convenience methods
var recentOrders = dbContext.Orders
    .WhereBetween("OrderDate", DateTime.Now.AddDays(-30), DateTime.Now)
    .ToList();

var inProgressTasks = dbContext.Tasks
    .WhereIn("Status", "Pending", "InProgress", "Review")
    .ToList();

var nonArchivedItems = dbContext.Items
    .WhereNotIn("Status", "Archived", "Deleted")
    .ToList();
```

### 3. Multiple Filter Conditions

```csharp
// Method 1: Complex expression string
var complexResult1 = dbContext.Employees
    .ApplyFilter("Department.Name,eq,Engineering && Salary,gte,50000 && IsActive,eq,true")
    .ToList();

// Method 2: Multiple expression strings with AND logic
var complexResult2 = dbContext.Employees
    .ApplyAndFilters("Department.Name,eq,Engineering", "Salary,gte,50000", "IsActive,eq,true")
    .ToList();

// Method 3: Multiple expression strings with OR logic
var complexResult3 = dbContext.Employees
    .ApplyOrFilters("Department.Name,eq,Engineering", "Department.Name,eq,Sales", "Level,gte,5")
    .ToList();

// Method 4: FilterCondition objects with AND logic
var complexResult4 = dbContext.Employees
    .ApplyAndFilters(
        new FilterCondition { PropertyPath = "Department.Name", Operator = FilterOperator.Equal, Value = "Engineering" },
        new FilterCondition { PropertyPath = "Salary", Operator = FilterOperator.GreaterThanOrEqual, Value = 50000 },
        new FilterCondition { PropertyPath = "IsActive", Operator = FilterOperator.Equal, Value = true }
    )
    .ToList();

// Method 5: Chaining strongly-typed methods
var complexResult5 = dbContext.Employees
    .WhereEqual("Department.Name", "Engineering")
    .WhereGreaterThanOrEqual("Salary", 50000)
    .WhereEqual("IsActive", true)
    .ToList();
```

### 4. Complex Expressions

```csharp
// Complex filter with logical operators and parentheses
var complexFilter = "(Name,contains,John && Age,gt,25) || (Department.Name,eq,IT && IsActive,eq,true)";
var filteredUsers = dbContext.Users
    .ApplyFilter(complexFilter)  // Direct string application - no need for FilterComplex
    .ToList();
```

### 5. Fluent API

```csharp
var results = dbContext.Orders
    .CreateFilter()
    .Equal("Status", "Active")
    .And()
    .GreaterThan("Total", 100)
    .Or()
    .BeginGroup()
        .Contains("Customer.Name", "VIP")
        .And()
        .LessThan("Days", 30)
    .EndGroup()
    .Build()
    .ToList();
```

### 4. Nested Property Filtering

```csharp
// Filter on nested properties
var filter = FilterSpecification.Create("User.Profile.Department.Name", FilterOperator.Equal, "Engineering");
var employees = dbContext.Employees
    .ApplyFilter(filter)
    .ToList();

// Multiple levels of nesting
var deepFilter = "Order.Customer.Address.City,eq,New York";
var orders = dbContext.Orders
    .FilterFromString(deepFilter)
    .ToList();
```

### 5. API Request Integration

```csharp
// In your API endpoint
public class GetUsersRequest : IFilterableRequest
{
    // Implement the interface
    public string? FilterExpression { get; set; }
    
    // Additional properties can be added here
    public bool? IncludeInactive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// Or inherit from PagedFilterableRequest which already implements IFilterableRequest
public class GetUsersRequest : PagedFilterableRequest
{
    // Additional properties can be added here
    public bool? IncludeInactive { get; set; }
}

// In your handler/controller
public async Task<PagedResult<UserDto>> GetUsers(GetUsersRequest request)
{
    var query = _context.Users.AsQueryable();
    
    // Apply custom filters first
    if (!request.IncludeInactive.GetValueOrDefault())
    {
        query = query.Where(u => u.IsActive);
    }
    
    // Apply dynamic filters from request
    query = query.ApplyFilters(request);
    
    // Get total count and apply pagination
    var totalCount = await query.CountAsync();
    var users = await query
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync();
    
    return PagedResult<UserDto>.Create(users, request.PageNumber, request.PageSize, totalCount);
}
```

### 6. Type Conversion Examples

```csharp
// Automatic type conversion works for common types
var filters = new[]
{
    "Id,eq,123",                    // Converts to int/long/Guid as needed
    "CreatedAt,gte,2023-01-01",    // Converts to DateTime
    "IsActive,eq,true",            // Converts to bool
    "Price,between,10.50,99.99",   // Converts to decimal/double
    "Tags,in,important,urgent"      // Supports multiple values for In operator
};

var results = dbContext.Items
    .FilterAnd(filters)  // Combines with AND logic
    .ToList();
```

### 7. Case Sensitivity

```csharp
// Case-sensitive search
var caseSensitive = FilterSpecification.Create("Name", FilterOperator.Contains, "John", caseSensitive: true);

// Case-insensitive search (default)
var caseInsensitive = FilterSpecification.Create("Name", FilterOperator.Contains, "john", caseSensitive: false);
```

### 8. Multiple Filter Conditions

```csharp
// Using array of filter expressions
var filterExpressions = new[]
{
    "Department.Name,eq,Engineering",
    "Salary,gte,50000",
    "IsActive,eq,true",
    "HireDate,gte,2020-01-01"
};

// Combine with AND
var andResults = dbContext.Employees
    .FilterAnd(filterExpressions)
    .ToList();

// Combine with OR
var orResults = dbContext.Employees
    .FilterOr(filterExpressions)
    .ToList();
```

## Filter Expression Format

### Simple Format
```
PropertyPath,Operator,Value
```

Examples:
- `Name,eq,John`
- `Age,gt,25`
- `User.Department.Name,contains,Engineering`

### Multiple Values (for In/NotIn operators)
```
PropertyPath,in,Value1,Value2,Value3
```

Example:
- `Status,in,Active,Pending,Review`

### Between Operator
```
PropertyPath,between,MinValue,MaxValue
```

Example:
- `Salary,between,50000,100000`

### Complex Expressions
Use logical operators (`&&`, `||`) and parentheses for grouping:

```
(Name,contains,John && Age,gt,25) || (Department,eq,IT && IsActive,eq,true)
```

## Best Practices

1. **Use Indexed Properties**: Ensure frequently filtered properties have database indexes
2. **Validate Input**: Always validate filter expressions in your API endpoints
3. **Limit Complexity**: Consider limiting the complexity of filter expressions for performance
4. **Test Performance**: Test filter performance with realistic data volumes
5. **Use Pagination**: Always use pagination with filtering to avoid large result sets

## Error Handling

The filtering system gracefully handles various error scenarios:

- **Invalid Property Names**: Returns empty results for non-existent properties
- **Type Conversion Errors**: Skips conditions that cannot be converted
- **Malformed Expressions**: Ignores invalid parts of complex expressions
- **Null Values**: Properly handles null comparisons

## Integration with Existing Code

This filtering system can be easily integrated into your existing query handlers. For example, updating the `GetAllUnits` handler:

```csharp
public async Task<Result<PagedResult<UnitSummaryDto>>> Handle(GetAllUnits query, CancellationToken cancellationToken = default)
{
    var unitsQuery = _context.Set<Unit>()
        .Include(u => u.Parent)
        .Include(u => u.Manager)
        .Include(u => u.Children)
        .AsQueryable();

    // Apply dynamic filters if provided
    if (query.Filters != null)
    {
        var filterSpec = FilterExpressionParser.ParseConditions(query.Filters, LogicalOperator.And);
        unitsQuery = unitsQuery.ApplyFilter(filterSpec);
    }

    // Apply existing filters
    if (!query.IncludeInactive)
    {
        unitsQuery = unitsQuery.Where(u => u.IsActive);
    }

    // ... rest of the existing logic
}
```

## New ApplyFilter Method Variations

The system now provides multiple ways to apply filters without manual parsing:

### Direct Parameter Methods
```csharp
// Single condition - no parsing needed
query.ApplyFilter("Name", FilterOperator.Equal, "John");
query.ApplyFilter("Age", FilterOperator.GreaterThan, 25);
query.ApplyFilter("Email", FilterOperator.Contains, "@gmail.com", caseSensitive: false);

// Special operators
query.ApplyBetweenFilter("Price", 100, 500);
query.ApplyInFilter("Status", new[] { "Active", "Pending", "Review" });
```

### Multiple Conditions
```csharp
// Multiple conditions with AND logic
query.ApplyAndFilters("Name,eq,John", "Age,gt,25", "IsActive,eq,true");

// Multiple conditions with OR logic  
query.ApplyOrFilters("Department,eq,IT", "Department,eq,Engineering", "Level,gte,5");

// Using FilterCondition objects
query.ApplyAndFilters(
    new FilterCondition { PropertyPath = "Name", Operator = FilterOperator.Equal, Value = "John" },
    new FilterCondition { PropertyPath = "Age", Operator = FilterOperator.GreaterThan, Value = 25 }
);
```

### Strongly-Typed Convenience Methods
```csharp
// Comparison operators
query.WhereEqual("Status", "Active");
query.WhereNotEqual("Type", "Deleted");
query.WhereGreaterThan("Price", 100);
query.WhereLessThanOrEqual("Quantity", 50);

// String operators
query.WhereContains("Name", "John", caseSensitive: false);
query.WhereStartsWith("Code", "ABC");
query.WhereEndsWith("Email", "@company.com");

// Null checks
query.WhereIsNull("DeletedAt");
query.WhereIsNotNull("Email");

// Collection operators
query.WhereIn("Status", "Active", "Pending", "Review");
query.WhereNotIn("Type", "Draft", "Archived");
query.WhereBetween("CreatedAt", startDate, endDate);

// Chaining multiple conditions
var results = dbContext.Users
    .WhereEqual("IsActive", true)
    .WhereContains("Email", "@company.com")
    .WhereGreaterThan("LastLoginDate", DateTime.Now.AddDays(-30))
    .WhereBetween("Age", 18, 65)
    .ToList();
```

### Performance Benefits
- **No Parsing Overhead**: Direct parameter methods bypass string parsing
- **Compile-Time Safety**: Strongly-typed methods provide better IntelliSense and error checking
- **Flexible Options**: Choose the method that best fits your use case
