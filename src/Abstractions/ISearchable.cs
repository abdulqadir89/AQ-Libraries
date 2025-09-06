namespace AQ.Abstractions;

public interface ISearchable
{
    public Guid Id { get; }
    public string EntityType => GetType().Name;
    string GetSearchTitle();
    string GetSearchContent();
}
