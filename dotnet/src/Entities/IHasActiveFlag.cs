namespace AQ.Abstractions;

public interface IHasActiveFlag
{
    bool IsActive { get; }

    void EvaluateIsActive();
}
