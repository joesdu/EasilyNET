using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EasilyNET.Core.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// Type扩展.
/// </summary>
public static partial class TypeExtension
{
    /// <summary>
    /// 判断类型是否为Nullable类型
    /// </summary>
    /// <param name="type">要处理的类型</param>
    /// <returns>是返回True，不是返回False</returns>
    public static bool IsNullable(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    /// <summary>
    /// 判断当前类型是否可由指定类型派生
    /// </summary>
    /// <typeparam name="TBaseType">基类型</typeparam>
    /// <param name="type">当前类型</param>
    /// <param name="canAbstract">是否允许抽象类</param>
    /// <returns>如果是派生类则返回True，否则返回False</returns>
    public static bool IsDeriveClassFrom<TBaseType>(this Type type, bool canAbstract = false) => type.IsDeriveClassFrom(typeof(TBaseType), canAbstract);

    /// <summary>
    /// 判断当前类型是否可由指定类型派生
    /// </summary>
    /// <param name="type">当前类型</param>
    /// <param name="baseType">基类型</param>
    /// <param name="canAbstract">是否允许抽象类</param>
    /// <returns>如果是派生类则返回True，否则返回False</returns>
    public static bool IsDeriveClassFrom(this Type type, Type baseType, bool canAbstract = false)
    {
        type.NotNull(nameof(type));
        baseType.NotNull(nameof(baseType));
        return type.IsClass && (canAbstract || !type.IsAbstract) && type.IsBaseOn(baseType);
    }

    /// <summary>
    /// 返回当前类型是否是指定基类的派生类
    /// </summary>
    /// <param name="type">当前类型</param>
    /// <param name="baseType">要判断的基类型</param>
    /// <returns>如果是派生类则返回True，否则返回False</returns>
    public static bool IsBaseOn(this Type type, Type baseType) => baseType.IsGenericTypeDefinition ? type.HasImplementedRawGeneric(baseType) : baseType.IsAssignableFrom(type);

    /// <summary>
    /// 判断指定的类型 <paramref name="type" /> 是否是指定泛型类型的子类型，或实现了指定泛型接口。
    /// </summary>
    /// <param name="type">需要测试的类型。</param>
    /// <param name="generic">泛型接口类型，传入 typeof(IXxx&lt;&gt;)</param>
    /// <returns>如果是泛型接口的子类型，则返回 true，否则返回 false。</returns>
    public static bool HasImplementedRawGeneric(this Type type, Type generic)
    {
        type.NotNull(nameof(type));
        generic.NotNull(nameof(generic));
        // 测试接口。
        var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
        if (isTheRawGenericType) return true;
        // 测试类型。
        while (type != typeof(object))
        {
            isTheRawGenericType = IsTheRawGenericType(type);
            if (isTheRawGenericType) return true;
            type = type.BaseType!;
        }
        // 没有找到任何匹配的接口或类型。
        return false;

        // 测试某个类型是否是指定的原始接口。
        bool IsTheRawGenericType(Type test) => generic == (test.IsGenericType ? test.GetGenericTypeDefinition() : test);
    }

    /// <summary>
    /// 通过类型转换器获取Nullable类型的基础类型
    /// </summary>
    /// <param name="type"> 要处理的类型对象 </param>
    /// <returns> 基础类型 </returns>
    public static Type GetUnNullableType(this Type type)
    {
        if (!type.IsNullable()) return type;
        NullableConverter nullableConverter = new(type);
        return nullableConverter.UnderlyingType;
    }

    /// <summary>
    /// 是否是 ValueTuple
    /// </summary>
    /// <param name="type">type</param>
    /// <returns>如果是 ValueTuple 则返回True，否则返回False</returns>
    public static bool IsValueTuple(this Type type) => type.IsValueType && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    /// <summary>
    /// 判断是否基元类型，如果是可空类型会先获取里面的类型，如 int? 也是基元类型
    /// The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
    /// </summary>
    /// <param name="type">type</param>
    /// <returns>如果是基元类型则返回True，否则返回False</returns>
    public static bool IsPrimitiveType(this Type type) => (Nullable.GetUnderlyingType(type) ?? type).IsPrimitive;

    /// <summary>
    /// 获取当前类型实现的接口的集合。
    /// </summary>
    /// <param name="type">type</param>
    /// <returns>当前类型实现的接口的集合。</returns>
    public static IEnumerable<Type> GetImplementedInterfaces(this Type type) => type.GetTypeInfo().ImplementedInterfaces;

    /// <summary>
    /// 获取类型的描述信息
    /// </summary>
    /// <param name="type">type</param>
    /// <returns>描述信息</returns>
    public static string ToDescription(this Type type) => type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

    /// <summary>
    /// 获取成员的描述信息
    /// </summary>
    /// <typeparam name="TAttribute">动态特性</typeparam>
    /// <param name="member">成员信息</param>
    /// <returns>描述信息</returns>
    public static string ToDescription<TAttribute>(this MemberInfo member) where TAttribute : AttributeBase =>
        member.GetCustomAttribute<TAttribute>() is AttributeBase attributeBase
            ? attributeBase.Description()
            : member.Name;

    /// <summary>
    /// 获取成员的描述信息或显示名称
    /// </summary>
    /// <param name="member">成员信息</param>
    /// <returns>描述信息或显示名称</returns>
    public static string ToDescription(this MemberInfo member)
    {
        var desc = member.GetCustomAttribute<DescriptionAttribute>();
        if (desc is not null) return desc.Description;
        //显示名
        var display = member.GetCustomAttribute<DisplayNameAttribute>();
        return display is not null ? display.DisplayName : member.Name;
    }

    /// <summary>
    /// 判断接口类型是否具有相匹配的通用类型
    /// </summary>
    /// <param name="interfaceType">接口类型</param>
    /// <param name="typeInfo">对象类型</param>
    /// <returns>如果具有相匹配的通用类型则返回True，否则返回False</returns>
    public static bool HasMatchingGenericArity(this Type interfaceType, TypeInfo typeInfo)
    {
        if (!typeInfo.IsGenericType) return true;
        var interfaceTypeInfo = interfaceType.GetTypeInfo();
        if (!interfaceTypeInfo.IsGenericType) return false;
        var argumentCount = interfaceType.GenericTypeArguments.Length;
        var parameterCount = typeInfo.GenericTypeParameters.Length;
        return argumentCount == parameterCount;
    }

    /// <summary>
    /// 获取注册类型
    /// </summary>
    /// <param name="interfaceType">接口类型</param>
    /// <param name="typeInfo">对象类型</param>
    /// <returns>注册类型</returns>
    public static Type GetRegistrationType(this Type interfaceType, TypeInfo typeInfo)
    {
        if (!typeInfo.IsGenericTypeDefinition) return interfaceType;
        var interfaceTypeInfo = interfaceType.GetTypeInfo();
        return interfaceTypeInfo.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
    }

    /// <summary>
    /// 获取适合在错误消息中使用的友好类名.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>友好的类名.</returns>
    public static string GetFriendlyTypeName(this Type type)
    {
        var typeInfo = type.GetTypeInfo();
        if (!typeInfo.IsGenericType)
        {
            return type.Name;
        }
        // 计算所需的字节数
        var typeName = FriendlyTypeNameRegex().Replace(type.Name, "");
        var typeNameBytes = Encoding.UTF8.GetBytes(typeName);
        var genericArguments = typeInfo.GetGenericArguments();
        var totalLength = typeNameBytes.Length + 2 + genericArguments.Sum(arg => arg.GetFriendlyTypeName().Length + 2); // 包括 '<' 和 '>'
        totalLength -= 2;                                                                                               // 去掉最后一个 ", "
        // 使用 Span<byte> 进行二进制处理
        var buffer = totalLength <= 256 ? stackalloc byte[totalLength] : new byte[totalLength];
        var offset = 0;
        // 复制类型名称
        typeNameBytes.CopyTo(buffer[offset..]);
        offset += typeNameBytes.Length;
        // 添加 '<'
        buffer[offset++] = (byte)'<';
        // 复制泛型参数
        for (var i = 0; i < genericArguments.Length; i++)
        {
            if (i > 0)
            {
                buffer[offset++] = (byte)',';
                buffer[offset++] = (byte)' ';
            }
            var argName = genericArguments[i].GetFriendlyTypeName();
            var argNameBytes = Encoding.UTF8.GetBytes(argName);
            argNameBytes.CopyTo(buffer[offset..]);
            offset += argNameBytes.Length;
        }
        // 添加 '>'
        // ReSharper disable once RedundantAssignment
        buffer[offset++] = (byte)'>'; // 这行代码中[offset++]是必要的
        // 返回结果
        return Encoding.UTF8.GetString(buffer);
    }

    [GeneratedRegex(@"\`\d+$")]
    private static partial Regex FriendlyTypeNameRegex();
}