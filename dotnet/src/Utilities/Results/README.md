# AQ.Utilities.Results

This library provides types and utilities for representing operation results, errors, and data sets in a consistent manner.

## Features
- **Result**: Represents the outcome of an operation, indicating success or failure and containing error details if any.
- **Error**: Encapsulates error information, including type, code, and message. Includes factory methods for common error scenarios.
- **DataSet<T>**: Wraps a collection of items with a result status.
- **ResultExtensions**: Extension methods for working with results, such as error checks.

## Usage
Reference this project to standardize result and error handling across your application logic.

---

**Example:**
```csharp
var result = Result.Success();
if (!result.IsSuccess) {
    // handle error
}
```
