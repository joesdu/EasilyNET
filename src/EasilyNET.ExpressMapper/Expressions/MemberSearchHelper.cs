using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 成员搜索助手类，用于在类型中查找成员。
/// Static class for member search helper, used to find members in types.
/// </summary>
public static class MemberSearchHelper
{
    /// <summary>
    /// 生成成员访问表达式。
    /// Forms a member access expression.
    /// </summary>
    /// <param name="source">源参数表达式。Source parameter expression.</param>
    /// <param name="memberName">成员名称。Member name.</param>
    /// <param name="memberType">成员类型。Member type.</param>
    /// <returns>成员访问表达式。Member access expression.</returns>
    public static MemberExpression? FormMemberAccess(ParameterExpression source, string memberName, Type memberType)
    {
        var member = FindMember(source.Type, memberName, memberType, false);
        return member is null ? default : Expression.PropertyOrField(source, memberName);
    }

    /// <summary>
    /// 查找成员。
    /// Finds a member.
    /// </summary>
    /// <param name="source">源类型。Source type.</param>
    /// <param name="memberName">成员名称。Member name.</param>
    /// <param name="memberType">成员类型。Member type.</param>
    /// <param name="isWriteable">是否可写。Is writeable.</param>
    /// <returns>成员信息。Member information.</returns>
    public static MemberForMapping? FindMember(Type source, string memberName, Type memberType, bool isWriteable) => TryFindField(source, memberName, memberType, isWriteable) ?? TryFindProperty(source, memberName, memberType, isWriteable);

    /// <summary>
    /// 查找所有成员。
    /// Finds all members.
    /// </summary>
    /// <param name="type">类型。Type.</param>
    /// <param name="isWriteable">是否可写。Is writeable.</param>
    /// <returns>成员信息集合。Collection of member information.</returns>
    public static IEnumerable<MemberForMapping> FindAllMembers(Type type, bool isWriteable = false)
    {
        foreach (var prop in type.GetProperties())
        {
            switch (isWriteable)
            {
                case true when prop.CanWrite:
                    yield return MemberFromProp(prop);
                    break;
                case false:
                    yield return MemberFromProp(prop);
                    break;
            }
        }
        foreach (var field in type.GetFields())
        {
            switch (isWriteable)
            {
                case true when !field.IsInitOnly:
                    yield return MemberFromField(field);
                    break;
                case false:
                    yield return MemberFromField(field);
                    break;
            }
        }
    }

    /// <summary>
    /// 查找所有成员。
    /// Finds all members.
    /// </summary>
    /// <typeparam name="T">类型参数。Type parameter.</typeparam>
    /// <param name="isWriteable">是否可写。Is writeable.</param>
    /// <returns>成员信息集合。Collection of member information.</returns>
    public static IEnumerable<MappingMember> FindAllMembers<T>(bool isWriteable = false)
    {
        var type = typeof(T);
        foreach (var prop in type.GetProperties())
        {
            switch (isWriteable)
            {
                case true when prop.CanWrite:
                    yield return MemberFromPropV2(prop);
                    break;
                case false:
                    yield return MemberFromPropV2(prop);
                    break;
            }
        }
        foreach (var field in type.GetFields())
        {
            switch (isWriteable)
            {
                case true when !field.IsInitOnly:
                    yield return MemberFromFieldV2(field);
                    break;
                case false:
                    yield return MemberFromFieldV2(field);
                    break;
            }
        }
    }

    /// <summary>
    /// 尝试查找字段。
    /// Tries to find a field.
    /// </summary>
    /// <param name="source">源类型。Source type.</param>
    /// <param name="memberName">成员名称。Member name.</param>
    /// <param name="memberType">成员类型。Member type.</param>
    /// <param name="isWriteable">是否可写。Is writeable.</param>
    /// <returns>成员信息。Member information.</returns>
    private static MemberForMapping? TryFindField(Type source, string memberName, Type memberType, bool isWriteable)
    {
        var field = source.GetField(memberName);
        return field is not null && field.FieldType == memberType && (!isWriteable || !field.IsInitOnly)
                   ? MemberFromField(field)
                   : default;
    }

    /// <summary>
    /// 尝试查找属性。
    /// Tries to find a property.
    /// </summary>
    /// <param name="source">源类型。Source type.</param>
    /// <param name="memberName">成员名称。Member name.</param>
    /// <param name="memberType">成员类型。Member type.</param>
    /// <param name="isWriteable">是否可写。Is writeable.</param>
    /// <returns>成员信息。Member information.</returns>
    private static MemberForMapping? TryFindProperty(Type source, string memberName, Type memberType, bool isWriteable)
    {
        var prop = source.GetProperty(memberName);
        return prop is not null && prop.PropertyType == memberType && (!isWriteable || prop.CanWrite)
                   ? MemberFromProp(prop)
                   : default;
    }

    /// <summary>
    /// 从字段信息创建成员信息。
    /// Creates member information from field information.
    /// </summary>
    /// <param name="fieldInfo">字段信息。Field information.</param>
    /// <returns>成员信息。Member information.</returns>
    private static MemberForMapping MemberFromField(FieldInfo fieldInfo) =>
        new()
        {
            MemberInfo = fieldInfo,
            MemberName = fieldInfo.Name,
            MemberType = fieldInfo.FieldType
        };

    /// <summary>
    /// 从属性信息创建成员信息。
    /// Creates member information from property information.
    /// </summary>
    /// <param name="propertyInfo">属性信息。Property information.</param>
    /// <returns>成员信息。Member information.</returns>
    private static MemberForMapping MemberFromProp(PropertyInfo propertyInfo) =>
        new()
        {
            MemberInfo = propertyInfo,
            MemberName = propertyInfo.Name,
            MemberType = propertyInfo.PropertyType
        };

    /// <summary>
    /// 从字段信息创建映射成员信息。
    /// Creates mapping member information from field information.
    /// </summary>
    /// <param name="fieldInfo">字段信息。Field information.</param>
    /// <returns>映射成员信息。Mapping member information.</returns>
    private static MappingMember MemberFromFieldV2(FieldInfo fieldInfo) =>
        new()
        {
            Info = fieldInfo,
            Name = fieldInfo.Name,
            Type = fieldInfo.FieldType
        };

    /// <summary>
    /// 从属性信息创建映射成员信息。
    /// Creates mapping member information from property information.
    /// </summary>
    /// <param name="propertyInfo">属性信息。Property information.</param>
    /// <returns>映射成员信息。Mapping member information.</returns>
    private static MappingMember MemberFromPropV2(PropertyInfo propertyInfo) =>
        new()
        {
            Info = propertyInfo,
            Name = propertyInfo.Name,
            Type = propertyInfo.PropertyType
        };
}