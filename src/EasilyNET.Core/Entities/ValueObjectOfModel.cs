namespace EasilyNET.Core.Entities;

/// <summary>
/// 值对象泛型
/// </summary>
/// <typeparam name="T">动态类型</typeparam>
public abstract class ValueObject<T>
    where T : ValueObject<T>
{
    public override bool Equals(object obj)
    {
        var valueObject = obj as T;
        if (ReferenceEquals(valueObject, null))
        {
            return false;
        }
        if (GetType() != obj.GetType())
        {
            return false;
        }
        return EqualsCore(valueObject);
    }

    abstract protected bool EqualsCore(T other);

    public override int GetHashCode() => GetHashCodeCore();

    abstract protected int GetHashCodeCore();

    public static bool operator ==(ValueObject<T> a, ValueObject<T> b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
        {
            return true;
        }
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        {
            return false;
        }
        return a.Equals(b);
    }

    public static bool operator !=(ValueObject<T> a, ValueObject<T> b) => !(a == b);
}