namespace EasilyNET.Core.Domains;

//Inspired from https://docs.microsoft.com/zh-cn/dotnet/standard/microservices-architecture/microservice-ddd-cqrs-patterns/implement-value-objects
/// <summary>
/// 值对象
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// </summary>
    /// <returns></returns>
    abstract protected IEnumerable<object> GetAtomicValues();

    /// <summary>
    /// 值是否相等
    /// </summary>
    /// <param name="obj">对象</param>
    /// <returns></returns>
    public bool ValueEquals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }
        var other = (ValueObject)obj;
        using var thisValues = GetAtomicValues().GetEnumerator();
        using var otherValues = other.GetAtomicValues().GetEnumerator();
        var thisMoveNext = thisValues.MoveNext();
        var otherMoveNext = otherValues.MoveNext();
        while (thisMoveNext && otherMoveNext)
        {
            if (thisValues is { Current: null } ^ otherValues is { Current: null })
            {
                return false;
            }
            if (thisValues.Current != null && !thisValues.Current.Equals(otherValues.Current))
            {
                return false;
            }
            thisMoveNext = thisValues.MoveNext();
            otherMoveNext = otherValues.MoveNext();
            if (thisMoveNext != otherMoveNext)
            {
                return false;
            }
        }
        return !thisMoveNext && !otherMoveNext;
    }
}