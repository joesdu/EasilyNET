using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EasilyNET.Core.Attributes;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Type extensions</para>
///     <para xml:lang="zh">Type 扩展</para>
/// </summary>
public static partial class TypeExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Checks if the type is a Nullable type</para>
    ///     <para xml:lang="zh">判断类型是否为 Nullable 类型</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type to check</para>
    ///     <para xml:lang="zh">要检查的类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type is Nullable, otherwise false</para>
    ///     <para xml:lang="zh">如果是 Nullable 类型则返回 True，否则返回 False</para>
    /// </returns>
    public static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

    /// <summary>
    ///     <para xml:lang="en">Checks if the current type can be derived from the specified base type</para>
    ///     <para xml:lang="zh">判断当前类型是否可由指定基类型派生</para>
    /// </summary>
    /// <typeparam name="TBaseType">
    ///     <para xml:lang="en">The base type</para>
    ///     <para xml:lang="zh">基类型</para>
    /// </typeparam>
    /// <param name="type">
    ///     <para xml:lang="en">The current type</para>
    ///     <para xml:lang="zh">当前类型</para>
    /// </param>
    /// <param name="canAbstract">
    ///     <para xml:lang="en">Whether abstract classes are allowed</para>
    ///     <para xml:lang="zh">是否允许抽象类</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type can be derived from the base type, otherwise false</para>
    ///     <para xml:lang="zh">如果是派生类则返回 True，否则返回 False</para>
    /// </returns>
    public static bool IsDeriveClassFrom<TBaseType>(this Type type, bool canAbstract = false) => type.IsDeriveClassFrom(typeof(TBaseType), canAbstract);

    /// <summary>
    ///     <para xml:lang="en">Checks if the current type can be derived from the specified base type</para>
    ///     <para xml:lang="zh">判断当前类型是否可由指定基类型派生</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The current type</para>
    ///     <para xml:lang="zh">当前类型</para>
    /// </param>
    /// <param name="baseType">
    ///     <para xml:lang="en">The base type</para>
    ///     <para xml:lang="zh">基类型</para>
    /// </param>
    /// <param name="canAbstract">
    ///     <para xml:lang="en">Whether abstract classes are allowed</para>
    ///     <para xml:lang="zh">是否允许抽象类</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type can be derived from the base type, otherwise false</para>
    ///     <para xml:lang="zh">如果是派生类则返回 True，否则返回 False</para>
    /// </returns>
    public static bool IsDeriveClassFrom(this Type type, Type baseType, bool canAbstract = false)
    {
        type.NotNull(nameof(type));
        baseType.NotNull(nameof(baseType));
        return type.IsClass && (canAbstract || !type.IsAbstract) && type.IsBaseOn(baseType);
    }

    /// <summary>
    ///     <para xml:lang="en">Checks if the current type is a derived class of the specified base type</para>
    ///     <para xml:lang="zh">返回当前类型是否是指定基类的派生类</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The current type</para>
    ///     <para xml:lang="zh">当前类型</para>
    /// </param>
    /// <param name="baseType">
    ///     <para xml:lang="en">The base type to check</para>
    ///     <para xml:lang="zh">要判断的基类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type is a derived class of the base type, otherwise false</para>
    ///     <para xml:lang="zh">如果是派生类则返回 True，否则返回 False</para>
    /// </returns>
    public static bool IsBaseOn(this Type type, Type baseType) => baseType.IsGenericTypeDefinition ? type.HasImplementedRawGeneric(baseType) : baseType.IsAssignableFrom(type);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Checks if the specified type <paramref name="type" /> is a subtype of the specified generic type, or implements the specified
    ///     generic interface
    ///     </para>
    ///     <para xml:lang="zh">判断指定的类型 <paramref name="type" /> 是否是指定泛型类型的子类型，或实现了指定泛型接口</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type to check</para>
    ///     <para xml:lang="zh">需要测试的类型</para>
    /// </param>
    /// <param name="generic">
    ///     <para xml:lang="en">The generic interface type, pass typeof(IXxx&lt;&gt;)</para>
    ///     <para xml:lang="zh">泛型接口类型，传入 typeof(IXxx&lt;&gt;)</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type is a subtype of the generic type, otherwise false</para>
    ///     <para xml:lang="zh">如果是泛型接口的子类型，则返回 true，否则返回 false</para>
    /// </returns>
    public static bool HasImplementedRawGeneric(this Type type, Type generic)
    {
        type.NotNull(nameof(type));
        generic.NotNull(nameof(generic));
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == generic) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == generic) ||
               (type.BaseType != null && type.BaseType.HasImplementedRawGeneric(generic));
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the underlying type of Nullable type using a type converter</para>
    ///     <para xml:lang="zh">通过类型转换器获取 Nullable 类型的基础类型</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type to process</para>
    ///     <para xml:lang="zh">要处理的类型对象</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The underlying type</para>
    ///     <para xml:lang="zh">基础类型</para>
    /// </returns>
    public static Type GetUnNullableType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>
    ///     <para xml:lang="en">Checks if the type is a ValueTuple</para>
    ///     <para xml:lang="zh">是否是 ValueTuple</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type to check</para>
    ///     <para xml:lang="zh">要检查的类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type is a ValueTuple, otherwise false</para>
    ///     <para xml:lang="zh">如果是 ValueTuple 则返回 True，否则返回 False</para>
    /// </returns>
    public static bool IsValueTuple(this Type type) => type.IsValueType && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Checks if the type is a primitive type. If it is a nullable type, it first gets the underlying type. For example, int? is
    ///     also a primitive type.
    ///     </para>
    ///     <para xml:lang="zh">判断是否基元类型，如果是可空类型会先获取里面的类型，如 int? 也是基元类型</para>
    ///     <para xml:lang="en">
    ///     The primitive types are Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and
    ///     Single.
    ///     </para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type to check</para>
    ///     <para xml:lang="zh">要检查的类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type is a primitive type, otherwise false</para>
    ///     <para xml:lang="zh">如果是基元类型则返回 True，否则返回 False</para>
    /// </returns>
    public static bool IsPrimitiveType(this Type type) => (Nullable.GetUnderlyingType(type) ?? type).IsPrimitive;

    /// <summary>
    ///     <para xml:lang="en">Gets the collection of interfaces implemented by the current type</para>
    ///     <para xml:lang="zh">获取当前类型实现的接口的集合</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type to check</para>
    ///     <para xml:lang="zh">要检查的类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The collection of interfaces implemented by the current type</para>
    ///     <para xml:lang="zh">当前类型实现的接口的集合</para>
    /// </returns>
    public static IEnumerable<Type> GetImplementedInterfaces(this Type type) => type.GetInterfaces();

    /// <summary>
    ///     <para xml:lang="en">Gets the description of the type</para>
    ///     <para xml:lang="zh">获取类型的描述信息</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type to check</para>
    ///     <para xml:lang="zh">要检查的类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The description of the type</para>
    ///     <para xml:lang="zh">类型的描述信息</para>
    /// </returns>
    public static string ToDescription(this Type type) => type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Gets the description of the member</para>
    ///     <para xml:lang="zh">获取成员的描述信息</para>
    /// </summary>
    /// <typeparam name="TAttribute">
    ///     <para xml:lang="en">The type of the attribute</para>
    ///     <para xml:lang="zh">特性的类型</para>
    /// </typeparam>
    /// <param name="member">
    ///     <para xml:lang="en">The member info</para>
    ///     <para xml:lang="zh">成员信息</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The description of the member</para>
    ///     <para xml:lang="zh">成员的描述信息</para>
    /// </returns>
    public static string ToDescription<TAttribute>(this MemberInfo member) where TAttribute : AttributeBase =>
        member.GetCustomAttribute<TAttribute>() is AttributeBase attributeBase
            ? attributeBase.Description()
            : member.Name;

    /// <summary>
    ///     <para xml:lang="en">Gets the description or display name of the member</para>
    ///     <para xml:lang="zh">获取成员的描述信息或显示名称</para>
    /// </summary>
    /// <param name="member">
    ///     <para xml:lang="en">The member info</para>
    ///     <para xml:lang="zh">成员信息</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The description or display name of the member</para>
    ///     <para xml:lang="zh">成员的描述信息或显示名称</para>
    /// </returns>
    public static string ToDescription(this MemberInfo member)
    {
        var desc = member.GetCustomAttribute<DescriptionAttribute>();
        if (desc is not null) return desc.Description;
        var display = member.GetCustomAttribute<DisplayNameAttribute>();
        return display is not null ? display.DisplayName : member.Name;
    }

    /// <summary>
    ///     <para xml:lang="en">Checks if the interface type has a matching generic type</para>
    ///     <para xml:lang="zh">判断接口类型是否具有相匹配的通用类型</para>
    /// </summary>
    /// <param name="interfaceType">
    ///     <para xml:lang="en">The interface type</para>
    ///     <para xml:lang="zh">接口类型</para>
    /// </param>
    /// <param name="typeInfo">
    ///     <para xml:lang="en">The type info</para>
    ///     <para xml:lang="zh">类型信息</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the interface type has a matching generic type, otherwise false</para>
    ///     <para xml:lang="zh">如果具有相匹配的通用类型则返回 True，否则返回 False</para>
    /// </returns>
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
    ///     <para xml:lang="en">Gets the registration type</para>
    ///     <para xml:lang="zh">获取注册类型</para>
    /// </summary>
    /// <param name="interfaceType">
    ///     <para xml:lang="en">The interface type</para>
    ///     <para xml:lang="zh">接口类型</para>
    /// </param>
    /// <param name="typeInfo">
    ///     <para xml:lang="en">The type info</para>
    ///     <para xml:lang="zh">类型信息</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The registration type</para>
    ///     <para xml:lang="zh">注册类型</para>
    /// </returns>
    public static Type GetRegistrationType(this Type interfaceType, TypeInfo typeInfo)
    {
        if (!typeInfo.IsGenericTypeDefinition) return interfaceType;
        var interfaceTypeInfo = interfaceType.GetTypeInfo();
        return interfaceTypeInfo.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets a friendly type name suitable for use in error messages</para>
    ///     <para xml:lang="zh">获取适合在错误消息中使用的友好类名</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The type</para>
    ///     <para xml:lang="zh">类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The friendly type name</para>
    ///     <para xml:lang="zh">友好的类名</para>
    /// </returns>
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
        buffer[offset++] = 0x3C; // '<'
        // 复制泛型参数
        for (var i = 0; i < genericArguments.Length; i++)
        {
            if (i > 0)
            {
                buffer[offset++] = 0x2C; // ','
                buffer[offset++] = 0x20; // ' '
            }
            var argName = genericArguments[i].GetFriendlyTypeName();
            var argNameBytes = Encoding.UTF8.GetBytes(argName);
            argNameBytes.CopyTo(buffer[offset..]);
            offset += argNameBytes.Length;
        }
        // 添加 '>'
        // ReSharper disable once RedundantAssignment
        buffer[offset++] = 0x3E; // 这行代码中[offset++]是必要的
        // 返回结果
        return Encoding.UTF8.GetString(buffer);
    }

    [GeneratedRegex(@"\`\d+$")]
    private static partial Regex FriendlyTypeNameRegex();
}