using System.Collections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo.AspNetCore.Conventions;

/// <summary>
/// Id映射处理器
/// <list type="number">
///     <item>映射 [BsonRepresentation(BsonType.ObjectId)]</item>
///     <item>将 <see cref="string" /> 和 <see cref="ObjectId" /> 相互转换.</item>
///     <item>当 <see langword="ID" /> 或者 <see langword="Id" /> 字段为空值时添加默认 <see cref="ObjectId" /></item>
/// </list>
/// </summary>
internal sealed class StringToObjectIdIdGeneratorConvention : ConventionBase, IPostProcessingConvention
{
    /// <summary>
    /// 在类映射完成后进行处理
    /// </summary>
    /// <param name="classMap">Bson类映射</param>
    public void PostProcess(BsonClassMap classMap)
    {
        ProcessClassMap(classMap);
    }

    /// <summary>
    /// 递归处理类映射，确保所有子对象的Id字段都被正确配置
    /// </summary>
    /// <param name="classMap">Bson类映射</param>
    private static void ProcessClassMap(BsonClassMap classMap)
    {
        // 获取Id成员映射
        var idMemberMap = classMap.IdMemberMap;
        // 如果Id生成器为空且成员类型为string，则设置自定义Id生成器和序列化器
        if (idMemberMap is { IdGenerator: null } && idMemberMap.MemberType == typeof(string))
        {
            idMemberMap.SetIdGenerator(new CustomStringObjectIdGenerator()).SetSerializer(new StringSerializer(BsonType.ObjectId));
        }
        // 递归处理所有成员映射
        foreach (var memberMap in classMap.AllMemberMaps)
        {
            var memberType = memberMap.MemberType;
            // 如果成员类型是泛型集合，则处理集合中的项
            if (typeof(IEnumerable).IsAssignableFrom(memberType) && memberType.IsGenericType)
            {
                var itemType = memberType.GetGenericArguments().FirstOrDefault();
                if (itemType is null) continue;
                if (!BsonClassMap.IsClassMapRegistered(itemType))
                {
                    BsonClassMap.RegisterClassMap(new(itemType));
                }
                var itemClassMap = BsonClassMap.LookupClassMap(itemType);
                ProcessClassMap(itemClassMap);
            }
            // 如果成员类型已经注册，则递归处理
            else if (BsonClassMap.IsClassMapRegistered(memberType))
            {
                var nestedClassMap = BsonClassMap.LookupClassMap(memberType);
                ProcessClassMap(nestedClassMap);
            }
            // 否则，注册成员类型并递归处理
            else
            {
                BsonClassMap.RegisterClassMap(new(memberType));
                var nestedClassMap = BsonClassMap.LookupClassMap(memberType);
                ProcessClassMap(nestedClassMap);
            }
        }
    }
}

/// <summary>
/// 自定义的 StringObjectIdGenerator，用于在插入数据时自动生成 ObjectId
/// </summary>
internal class CustomStringObjectIdGenerator : IIdGenerator
{
    /// <summary>
    /// 生成新的Id
    /// </summary>
    /// <param name="container">容器对象</param>
    /// <param name="document">文档对象</param>
    /// <returns>生成的Id</returns>
    public object GenerateId(object container, object document)
    {
        var classMap = BsonClassMap.LookupClassMap(document.GetType());
        // 递归处理所有成员映射
        foreach (var memberMap in classMap.AllMemberMaps)
        {
            // 如果成员类型是泛型集合，则处理集合中的项
            if (!typeof(IEnumerable).IsAssignableFrom(memberMap.MemberType) || !memberMap.MemberType.IsGenericType) continue;
            var itemType = memberMap.MemberType.GetGenericArguments().FirstOrDefault();
            if (itemType is null) continue;
            if (memberMap.Getter(document) is not IEnumerable items) continue;
            foreach (var item in items)
            {
                var itemClassMap = BsonClassMap.LookupClassMap(item.GetType());
                var itemIdMemberMap = itemClassMap.IdMemberMap;
                // 如果子对象的Id字段为空，则为其生成新的ObjectId
                if (itemIdMemberMap is not null && itemIdMemberMap.MemberType == typeof(string) && string.IsNullOrWhiteSpace(itemIdMemberMap.Getter(item)?.ToString()))
                {
                    itemIdMemberMap.Setter(item, ObjectId.GenerateNewId().ToString());
                }
            }
        }
        // 返回生成的ObjectId
        return ObjectId.GenerateNewId().ToString();
    }

    /// <summary>
    /// 检查Id是否为空
    /// </summary>
    /// <param name="id">Id对象</param>
    /// <returns>如果Id为空则返回true，否则返回false</returns>
    public bool IsEmpty(object? id) => string.IsNullOrWhiteSpace(id?.ToString());
}