namespace AQ.Entities;

public interface IHasActiveFlag
{
    bool IsActive { get; }

    void EvaluateIsActive();
}
