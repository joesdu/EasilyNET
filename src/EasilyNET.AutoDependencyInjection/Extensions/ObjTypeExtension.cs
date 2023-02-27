using System.Reflection;

namespace EasilyNET.AutoDependencyInjection.Extensions;

/// <summary>
/// 一些扩展方法,用来实现一些类型的检测和Object数据类型的扩展.
/// </summary>
internal static class ObjTypeExtension
{
    /// <summary>
    /// 判断特性相应是否存在
    /// </summary>
    /// <typeparam name="T">动态类型要判断的特性</typeparam>
    /// <param name="memberInfo"></param>
    /// <param name="inherit"></param>
    /// <returns>如果存在还在返回true，否则返回false</returns>
    internal static bool HasAttribute<T>(this ICustomAttributeProvider memberInfo, bool inherit = true)
        where T : Attribute =>
        memberInfo.IsDefined(typeof(T), inherit);

    /// <summary>
    /// 获取注册类型
    /// </summary>
    /// <param name="interfaceType">接口类型.</param>
    /// <param name="typeInfo">对象类型</param>
    /// <returns></returns>
    internal static Type GetRegistrationType(this Type interfaceType, Type typeInfo)
    {
        if (!typeInfo.IsGenericTypeDefinition) return interfaceType;
        var interfaceTypeInfo = interfaceType.GetTypeInfo();
        return interfaceTypeInfo.IsGenericType
                   ? interfaceType.GetGenericTypeDefinition()
                   : interfaceType;
    }

    /// <summary>
    /// 具有相匹配的通用类型
    /// </summary>
    /// <param name="interfaceType">接口类型</param>
    /// <param name="typeInfo">对象类型</param>
    /// <returns></returns>
    internal static bool HasMatchingGenericArity(this Type interfaceType, TypeInfo typeInfo)
    {
        if (!typeInfo.IsGenericType) return true;
        var interfaceTypeInfo = interfaceType.GetTypeInfo();
        if (!interfaceTypeInfo.IsGenericType) return false;
        var argumentCount = interfaceType.GenericTypeArguments.Length;
        var parameterCount = typeInfo.GenericTypeParameters.Length;
        return argumentCount == parameterCount;
    }
}