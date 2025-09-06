namespace AQ.Utilities.Results;

/// <summary>
/// Represents a result containing data and pagination metadata
/// Supports both traditional pagination and virtual scrolling scenarios
/// </summary>
/// <typeparam name="T">The type of data items</typeparam>
public class DataSet<T>
{
    /// <summary>
    /// The data items for the current batch
    /// </summary>
    public IEnumerable<T> Data { get; set; } = [];

    /// <summary>
    /// The number of items to skip from the beginning (0-based offset)
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// The maximum number of items to return in this batch
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// The total number of items across all batches
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Indicates whether there are more items available after the current batch
    /// Calculated as: (Skip + Data.Count()) < TotalCount
    /// </summary>
    public bool HasMore => Skip + Data.Count() < TotalCount;

    /// <summary>
    /// The number of items remaining after the current batch
    /// </summary>
    public int RemainingCount => Math.Max(0, TotalCount - Skip - Data.Count());

    /// <summary>
    /// Creates a data result
    /// </summary>
    /// <param name="data">The data items for the current batch</param>
    /// <param name="skip">The number of items skipped (0-based offset)</param>
    /// <param name="take">The maximum number of items requested</param>
    /// <param name="totalCount">The total number of items across all batches</param>
    /// <returns>A new DataResult instance</returns>
    public static DataSet<T> Create(IEnumerable<T> data, int skip, int take, int totalCount)
    {
        return new DataSet<T>
        {
            Data = data,
            Skip = skip,
            Take = take,
            TotalCount = totalCount
        };
    }
}
