using EasilyNET.Mongo.AspNetCore.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Service registration extension class</para>
///     <para xml:lang="zh">服务注册扩展类</para>
/// </summary>
public static class SerializersCollectionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Add custom serialization rules</para>
    ///     <para xml:lang="zh">添加自定义序列化规则</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Add custom serializers for MongoDB, such as types not supported by MongoDB.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         为MongoDB添加自定义序列化器,比如MongoDB不支持的类型。
    ///         </para>
    ///         <example>
    ///             <para>Usage:</para>
    ///             <code>
    /// <![CDATA[
    /// builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());
    /// ]]>
    /// </code>
    ///         </example>
    ///     </remarks>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">Data type</para>
    ///     <para xml:lang="zh">数据类型</para>
    /// </typeparam>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    /// <param name="serializer">
    ///     <para xml:lang="en">Custom serializer class</para>
    ///     <para xml:lang="zh">自定义序列化类</para>
    /// </param>
    public static IServiceCollection RegisterSerializer<T>(this IServiceCollection services, IBsonSerializer<T> serializer)
    {
        BsonSerializer.RegisterSerializer(serializer);
        return services;
    }

    /// <summary>
    ///     <para xml:lang="en">Register dynamic type [<see langword="dynamic" /> | <see langword="object" />] serialization support</para>
    ///     <para xml:lang="zh">注册动态类型 [<see langword="dynamic" /> | <see langword="object" />] 序列化支持</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Add dynamic type support for MongoDB, supporting anonymous types, making it convenient to quickly verify some functions without declaring
    ///         entity objects.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         为MongoDB添加动态类型支持,支持匿名类型,方便某些时候进行快速验证一些功能使用,不用声明实体对象。
    ///         </para>
    ///         <example>
    ///             <para>Usage:</para>
    ///             <code>
    /// <![CDATA[
    /// builder.Services.RegisterDynamicSerializer();
    /// ]]>
    /// </code>
    ///         </example>
    ///     </remarks>
    /// </summary>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    public static IServiceCollection RegisterDynamicSerializer(this IServiceCollection services)
    {
        var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || (type.FullName is not null && type.FullName.StartsWith("<>f__AnonymousType")));
        BsonSerializer.RegisterSerializer(objectSerializer);
        return services;
    }

    /// <summary>
    ///     <para xml:lang="en">Register global dictionary serializer for handling dictionaries with enum keys</para>
    ///     <para xml:lang="zh">注册全局字典序列化器，用于处理使用枚举值作为键的字典类型数据</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Add a global dictionary serializer for MongoDB to handle dictionaries with enum keys.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         为MongoDB添加全局字典序列化器，以处理使用枚举值作为键的字典类型数据。
    ///         </para>
    ///         <example>
    ///             <para>Usage:</para>
    ///             <code>
    /// <![CDATA[
    /// builder.Services.RegisterGlobalEnumKeyDictionarySerializer();
    /// ]]>
    /// </code>
    ///         </example>
    ///     </remarks>
    /// </summary>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    public static IServiceCollection RegisterGlobalEnumKeyDictionarySerializer(this IServiceCollection services)
    {
        BsonSerializer.RegisterGenericSerializerDefinition(typeof(Dictionary<,>), typeof(EnumKeyDictionarySerializer<,>));
        return services;
    }
}