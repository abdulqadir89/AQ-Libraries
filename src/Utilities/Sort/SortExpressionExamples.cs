namespace AQ.Utilities.Sort;

/// <summary>
/// Example usage and test cases for the sorting system
/// </summary>
public static class SortExpressionExamples
{
    /// <summary>
    /// Simple single-field sorting examples
    /// </summary>
    public static class Simple
    {
        // Sort by name ascending (default)
        public const string ByNameAscending = "Name,asc";
        public const string ByNameDefault = "Name"; // defaults to ascending

        // Sort by name descending
        public const string ByNameDescending = "Name,desc";

        // Sort by date
        public const string ByDateAscending = "CreatedDate,asc";
        public const string ByDateDescending = "CreatedDate,desc";
    }

    /// <summary>
    /// Complex multi-field sorting examples
    /// </summary>
    public static class Complex
    {
        // Sort by multiple fields
        public const string ByLastNameThenFirstName = "LastName,asc;FirstName,asc";

        // Mixed directions
        public const string ByPriorityDescThenDueDateAsc = "Priority,desc;DueDate,asc";

        // Nested properties
        public const string ByParentNameThenDescription = "Parent.Name,asc;Description,desc";

        // Complex business sorting
        public const string CustomerOrderSorting = "Customer.Company,asc;Priority,desc;DueDate,asc;OrderNumber,asc";

        // User management sorting
        public const string UserSorting = "Department.Name,asc;Role.Level,desc;User.LastName,asc;User.FirstName,asc";
    }

    /// <summary>
    /// Real-world business examples
    /// </summary>
    public static class BusinessExamples
    {
        // E-commerce product listing
        public const string ProductCatalog = "Category.Name,asc;Brand,asc;Price,asc;Rating,desc";

        // Task management
        public const string TaskList = "Status,asc;Priority,desc;DueDate,asc;AssignedTo.Name,asc";

        // Employee directory
        public const string EmployeeDirectory = "Department.Name,asc;Position.Level,desc;LastName,asc;FirstName,asc";

        // Financial transactions
        public const string TransactionHistory = "TransactionDate,desc;Amount,desc;Account.Name,asc";

        // Project management
        public const string ProjectList = "Status,asc;Priority,desc;StartDate,desc;Client.Company,asc";
    }

    /// <summary>
    /// Demonstrates programmatic building of sort expressions
    /// </summary>
    public static class ProgrammaticExamples
    {
        public static SortSpecification BuildUserSorting()
        {
            return SortExpressionBuilder.Create()
                .OrderByAscending("Department.Name")
                .OrderByDescending("Role.Level")
                .OrderByAscending("LastName")
                .OrderByAscending("FirstName")
                .Build();
        }

        public static SortSpecification BuildProductSorting(bool sortByPrice = false)
        {
            var builder = SortExpressionBuilder.Create()
                .OrderByAscending("Category.Name")
                .OrderByAscending("Brand");

            if (sortByPrice)
            {
                builder.OrderByAscending("Price");
            }

            return builder
                .OrderByDescending("Rating")
                .OrderByDescending("CreatedDate")
                .Build();
        }

        public static string BuildDynamicSort(string primaryField, bool ascending = true, params string[] additionalFields)
        {
            var builder = SortExpressionBuilder.Create()
                .OrderBy(primaryField, ascending ? SortDirection.Ascending : SortDirection.Descending);

            foreach (var field in additionalFields)
            {
                builder.OrderByAscending(field);
            }

            return builder.BuildExpression();
        }
    }

    /// <summary>
    /// Validation examples
    /// </summary>
    public static class ValidationExamples
    {
        public static bool ValidateUserSortExpression(string expression)
        {
            // Check basic validity
            if (!SortExpressionParser.IsValidExpression(expression))
                return false;

            // Parse and validate against User type (example)
            var specification = SortExpressionParser.Parse(expression);

            // Custom validation logic can be added here
            var propertyPaths = SortExpressionParser.GetPropertyPaths(expression);

            // Example: Ensure only allowed properties are used
            var allowedProperties = new[] { "FirstName", "LastName", "Email", "Department.Name", "CreatedDate" };

            return propertyPaths.All(path => allowedProperties.Contains(path));
        }

        public static string[] GetAllowedSortFields<T>()
        {
            return SortExtensions.GetSortableProperties<T>(includeNestedProperties: true, maxDepth: 3);
        }
    }
}
