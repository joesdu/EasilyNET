namespace EasilyNET.Core.Domains;

/// <summary>
/// 值对象泛型
/// </summary>
/// <typeparam name="T">动态类型</typeparam>
public abstract class ValueObject<T> where T : ValueObject<T>
{
    /// <summary>
    /// Equals
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is not T valueObject)
        {
            return false;
        }
        return GetType() == obj.GetType() && EqualsCore(valueObject);
    }

    /// <summary>
    /// EqualsCore
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    abstract protected bool EqualsCore(T other);

    /// <inheritdoc />
    public override int GetHashCode() => GetHashCodeCore();

    /// <summary>
    /// 获取HashCode
    /// </summary>
    /// <returns></returns>
    abstract protected int GetHashCodeCore();

    /// <summary>
    /// 等于
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator ==(ValueObject<T>? a, ValueObject<T>? b)
    {
        if (a is null && b is null)
        {
            return true;
        }
        if (a is null || b is null)
        {
            return false;
        }
        return a.Equals(b);
    }

    /// <summary>
    /// 不等于
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator !=(ValueObject<T> a, ValueObject<T> b) => !(a == b);
}