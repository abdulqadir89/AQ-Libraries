using Xunit;

namespace AQ.Utilities.Tests.Search;

/// <summary>
/// SearchDialect.Current is global mutable state. Every test class that sets it
/// must belong to this collection so they never run in parallel with each other.
/// </summary>
[CollectionDefinition("SearchDialect")]
public class SearchDialectCollection
{
}
