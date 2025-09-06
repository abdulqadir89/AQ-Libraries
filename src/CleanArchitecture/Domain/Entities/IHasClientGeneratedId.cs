namespace AQ.Domain.Entities;

public interface IHasClientGeneratedId
{
    public void SetClientGeneratedId(Guid id);
}
