// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core;

/// <summary>
/// <see cref="ReferenceItem" />, 通常用来保存关联的一些业务信息
/// </summary>
public class ReferenceItem : IEquatable<ReferenceItem>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="rid"></param>
    public ReferenceItem(string rid)
    {
        Rid = rid;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="rid"></param>
    /// <param name="name"></param>
    public ReferenceItem(string rid, string name)
    {
        Rid = rid;
        Name = name;
    }

    /// <summary>
    /// 标识(引用Id)
    /// </summary>
    public string Rid { get; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; } = string.Empty;

    /// <summary>
    /// 对比两个对象是否相同,通常理解为数据一致就相等
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ReferenceItem? other) => Equals(this, other!);

    /// <summary>
    /// 比较对象是否相等
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((ReferenceItem)obj)));

    /// <summary>
    /// GetHashCode
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Rid.GetHashCode() * 397) ^ Name.GetHashCode();
        }
    }

    /// <summary>
    /// 输出名称字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;

    /// <summary>
    /// 对比两个对象值是否相同
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool Equals(ReferenceItem x, ReferenceItem y) => x.Rid == y.Rid && x.Name == y.Name;

    /// <summary>
    /// 对比两个对象值是否相同(会比较对象中所有成员)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool operator ==(ReferenceItem x, ReferenceItem y) => Equals(x, y);

    /// <summary>
    /// 对比两个对象值是否不同(会比较对象中所有成员)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool operator !=(ReferenceItem x, ReferenceItem y) => !Equals(x, y);
}
