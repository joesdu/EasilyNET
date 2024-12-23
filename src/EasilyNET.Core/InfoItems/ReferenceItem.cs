// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core;

/// <summary>
///     <para xml:lang="en"><see cref="ReferenceItem" />, usually used to store some related business information</para>
///     <para xml:lang="zh"><see cref="ReferenceItem" />, 通常用来保存关联的一些业务信息</para>
/// </summary>
public class ReferenceItem : IEquatable<ReferenceItem>
{
    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="rid"></param>
    public ReferenceItem(string rid)
    {
        Rid = rid;
    }

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="rid"></param>
    /// <param name="name"></param>
    public ReferenceItem(string rid, string name)
    {
        Rid = rid;
        Name = name;
    }

    /// <summary>
    ///     <para xml:lang="en">Identifier (reference Id)</para>
    ///     <para xml:lang="zh">标识(引用Id)</para>
    /// </summary>
    public string Rid { get; }

    /// <summary>
    ///     <para xml:lang="en">Name</para>
    ///     <para xml:lang="zh">名称</para>
    /// </summary>
    public string Name { get; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Compare whether two objects are the same, usually understood as data consistency is equal</para>
    ///     <para xml:lang="zh">对比两个对象是否相同,通常理解为数据一致就相等</para>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ReferenceItem? other) => Equals(this, other!);

    /// <summary>
    ///     <para xml:lang="en">Compare whether the objects are equal</para>
    ///     <para xml:lang="zh">比较对象是否相等</para>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) => obj is not null && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((ReferenceItem)obj)));

    /// <summary>
    ///     <para xml:lang="en">GetHashCode</para>
    ///     <para xml:lang="zh">GetHashCode</para>
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
    ///     <para xml:lang="en">Output name string</para>
    ///     <para xml:lang="zh">输出名称字符串</para>
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;

    /// <summary>
    ///     <para xml:lang="en">Compare whether the values of two objects are the same</para>
    ///     <para xml:lang="zh">对比两个对象值是否相同</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool Equals(ReferenceItem x, ReferenceItem y) => x.Rid == y.Rid && x.Name == y.Name;

    /// <summary>
    ///     <para xml:lang="en">Compare whether the values of two objects are the same (will compare all members in the object)</para>
    ///     <para xml:lang="zh">对比两个对象值是否相同(会比较对象中所有成员)</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool operator ==(ReferenceItem x, ReferenceItem y) => Equals(x, y);

    /// <summary>
    ///     <para xml:lang="en">Compare whether the values of two objects are different (will compare all members in the object)</para>
    ///     <para xml:lang="zh">对比两个对象值是否不同(会比较对象中所有成员)</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static bool operator !=(ReferenceItem x, ReferenceItem y) => !Equals(x, y);
}