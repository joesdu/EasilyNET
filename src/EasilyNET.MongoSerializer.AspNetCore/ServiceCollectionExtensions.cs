using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务注册扩展类
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加自定义序列化规则
    /// <remarks>
    ///     <para>
    ///     为MongoDB添加自定义序列化器,比如MongoDB不支持的类型
    ///     </para>
    ///     <example>
    ///     使用方法:
    ///     <code>
    ///  <![CDATA[
    ///  builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());
    ///   ]]>
    ///  </code>
    ///     </example>
    /// </remarks>
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="services">IServiceCollection</param>
    /// <param name="serializer">自定义序列化类</param>
    /// <returns></returns>
    public static IServiceCollection RegisterSerializer<T>(this IServiceCollection services, IBsonSerializer<T> serializer)
    {
        BsonSerializer.RegisterSerializer(serializer);
        return services;
    }

    /// <summary>
    /// 注册动态类型 [<see langword="dynamic" /> | <see langword="object" />] 序列化支持
    /// <remarks>
    ///     <para>
    ///     为MongoDB添加动态类型支持,支持匿名类型,方便某些时候进行快速验证一些功能使用,不用声明实体对象.
    ///     </para>
    ///     <example>
    ///     使用方法:
    ///     <code>
    ///  <![CDATA[
    ///  builder.Services.RegisterDynamicSerializer();
    ///   ]]>
    ///  </code>
    ///     </example>
    /// </remarks>
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterDynamicSerializer(this IServiceCollection services)
    {
        var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || (type.FullName is not null && type.FullName.StartsWith("<>f__AnonymousType")));
        BsonSerializer.RegisterSerializer(objectSerializer);
        return services;
    }
}