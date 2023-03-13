using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.Extension;

/// <summary>
/// 服务注册扩展类
/// </summary>
public static class RegisterSerializerExtension
{
    /// <summary>
    /// 添加常用MongoDB类型转化支持
    /// DateTime,Decimal,DateOnly,TimeOnly
    /// 默认将时间本地化
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterHoyoSerializer(this IServiceCollection services)
    {
        services.RegisterHoyoSerializer(new DateTimeSerializer(DateTimeKind.Local)); //to local time
        services.RegisterHoyoSerializer(new DecimalSerializer(BsonType.Decimal128)); //decimal to decimal default
        services.RegisterHoyoSerializer(new DateOnlySerializer());
        services.RegisterHoyoSerializer(new TimeOnlySerializer());
        return services;
    }

    /// <summary>
    /// 添加自定义序列化规则
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="services">IServiceCollection</param>
    /// <param name="serializer">自定义序列化类</param>
    /// <returns></returns>
    public static IServiceCollection RegisterHoyoSerializer<T>(this IServiceCollection services, IBsonSerializer<T> serializer) where T : struct
    {
        BsonSerializer.RegisterSerializer(serializer);
        return services;
    }
}