namespace AQ.Abstractions;

public interface IHasActiveFlag
{
    bool IsActive { get; }
    
    abstract void EvaluateIsActive();

}

// public interface IHasEffectiveExpression<T> where T : IEntity
// {
//     static abstract Expression<Func<T, bool>> IsEffectiveExpression { get; }
// }