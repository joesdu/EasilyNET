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