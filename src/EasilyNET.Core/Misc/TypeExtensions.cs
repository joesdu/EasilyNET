using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

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
    ///     <para xml:lang="en">Type extensions</para>
    ///     <para xml:lang="zh">Type 扩展</para>
    /// </summary>
    extension(Type type)
    {
        /// <summary>
        ///     <para xml:lang="en">Checks if the type is a Nullable type</para>
        ///     <para xml:lang="zh">判断类型是否为 Nullable 类型</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">True if the type is Nullable, otherwise false</para>
        ///     <para xml:lang="zh">如果是 Nullable 类型则返回 True，否则返回 False</para>
        /// </returns>
        public bool IsNullable => Nullable.GetUnderlyingType(type) != null;

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
        /// <returns>
        ///     <para xml:lang="en">True if the type is a primitive type, otherwise false</para>
        ///     <para xml:lang="zh">如果是基元类型则返回 True，否则返回 False</para>
        /// </returns>
        public bool IsPrimitiveType => (Nullable.GetUnderlyingType(type) ?? type).IsPrimitive;

        /// <summary>
        ///     <para xml:lang="en">Gets the collection of interfaces implemented by the current type</para>
        ///     <para xml:lang="zh">获取当前类型实现的接口的集合</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The collection of interfaces implemented by the current type</para>
        ///     <para xml:lang="zh">当前类型实现的接口的集合</para>
        /// </returns>
        public IEnumerable<Type> ImplementedInterfaces => type.GetInterfaces();

        /// <summary>
        ///     <para xml:lang="en">Checks if the current type can be derived from the specified base type</para>
        ///     <para xml:lang="zh">判断当前类型是否可由指定基类型派生</para>
        /// </summary>
        /// <typeparam name="TBaseType">
        ///     <para xml:lang="en">The base type</para>
        ///     <para xml:lang="zh">基类型</para>
        /// </typeparam>
        /// <param name="canAbstract">
        ///     <para xml:lang="en">Whether abstract classes are allowed</para>
        ///     <para xml:lang="zh">是否允许抽象类</para>
        /// </param>
        public bool IsDeriveClassFrom<TBaseType>(bool canAbstract = false) => type.IsDeriveClassFrom(typeof(TBaseType), canAbstract);

        /// <summary>
        ///     <para xml:lang="en">Checks if the current type can be derived from the specified base type</para>
        ///     <para xml:lang="zh">判断当前类型是否可由指定基类型派生</para>
        /// </summary>
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
        public bool IsDeriveClassFrom(Type baseType, bool canAbstract = false)
        {
            type.NotNull(nameof(type));
            baseType.NotNull(nameof(baseType));
            return type.IsClass && (canAbstract || !type.IsAbstract) && type.IsBaseOn(baseType);
        }

        /// <summary>
        ///     <para xml:lang="en">Checks if the current type is a derived class of the specified base type</para>
        ///     <para xml:lang="zh">返回当前类型是否是指定基类的派生类</para>
        /// </summary>
        /// <param name="baseType">
        ///     <para xml:lang="en">The base type to check</para>
        ///     <para xml:lang="zh">要判断的基类型</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">True if the type is a derived class of the base type, otherwise false</para>
        ///     <para xml:lang="zh">如果是派生类则返回 True，否则返回 False</para>
        /// </returns>
        public bool IsBaseOn(Type baseType) => baseType.IsGenericTypeDefinition ? type.HasImplementedRawGeneric(baseType) : baseType.IsAssignableFrom(type);

        /// <summary>
        ///     <para xml:lang="en">
        ///     Checks if the specified type is a subtype of the specified generic type, or implements the specified
        ///     generic interface
        ///     </para>
        ///     <para xml:lang="zh">判断指定的类型是否是指定泛型类型的子类型，或实现了指定泛型接口</para>
        /// </summary>
        /// <param name="generic">
        ///     <para xml:lang="en">The generic interface type, pass typeof(IXxx&lt;&gt;)</para>
        ///     <para xml:lang="zh">泛型接口类型，传入 typeof(IXxx&lt;&gt;)</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">True if the type is a subtype of the generic type, otherwise false</para>
        ///     <para xml:lang="zh">如果是泛型接口的子类型，则返回 true，否则返回 false</para>
        /// </returns>
        public bool HasImplementedRawGeneric(Type generic)
        {
            type.NotNull(nameof(type));
            generic.NotNull(nameof(generic));
            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == generic) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == generic) ||
                   (type.BaseType != null && type.BaseType.HasImplementedRawGeneric(generic));
        }

        /// <summary>
        ///     <para xml:lang="en">Checks if the interface type has a matching generic type</para>
        ///     <para xml:lang="zh">判断接口类型是否具有相匹配的通用类型</para>
        /// </summary>
        /// <param name="typeInfo">
        ///     <para xml:lang="en">The type info</para>
        ///     <para xml:lang="zh">类型信息</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">True if the interface type has a matching generic type, otherwise false</para>
        ///     <para xml:lang="zh">如果具有相匹配的通用类型则返回 True，否则返回 False</para>
        /// </returns>
        public bool HasMatchingGenericArity(TypeInfo typeInfo)
        {
            if (!typeInfo.IsGenericType) return true;
            var interfaceTypeInfo = type.GetTypeInfo();
            if (!interfaceTypeInfo.IsGenericType) return false;
            var argumentCount = type.GenericTypeArguments.Length;
            var parameterCount = typeInfo.GenericTypeParameters.Length;
            return argumentCount == parameterCount;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets the registration type</para>
        ///     <para xml:lang="zh">获取注册类型</para>
        /// </summary>
        /// <param name="typeInfo">
        ///     <para xml:lang="en">The type info</para>
        ///     <para xml:lang="zh">类型信息</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The registration type</para>
        ///     <para xml:lang="zh">注册类型</para>
        /// </returns>
        public Type GetRegistrationType(TypeInfo typeInfo)
        {
            if (!typeInfo.IsGenericTypeDefinition) return type;
            var interfaceTypeInfo = type.GetTypeInfo();
            return interfaceTypeInfo.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }

        /// <summary>
        ///     <para xml:lang="en">Gets a friendly type name suitable for use in error messages</para>
        ///     <para xml:lang="zh">获取适合在错误消息中使用的友好类名</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The friendly type name</para>
        ///     <para xml:lang="zh">友好的类名</para>
        /// </returns>
        public string GetFriendlyTypeName()
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
    }

    [GeneratedRegex(@"\`\d+$")]
    private static partial Regex FriendlyTypeNameRegex();
}