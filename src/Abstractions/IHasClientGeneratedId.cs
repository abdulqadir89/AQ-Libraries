namespace AQ.Abstractions;

public interface IHasClientGeneratedId
{
    public void SetClientGeneratedId(Guid id);
}
