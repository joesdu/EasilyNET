using System.Collections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo.AspNetCore.Conventions;

/// <summary>
///     <para xml:lang="en">Id mapping processor</para>
///     <para xml:lang="zh">Id映射处理器</para>
///     <list type="number">
///         <item>
///             <description>
///                 <para xml:lang="en">Map [BsonRepresentation(BsonType.ObjectId)]</para>
///                 <para xml:lang="zh">映射 [BsonRepresentation(BsonType.ObjectId)]</para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <para xml:lang="en">Convert between <see cref="string" /> and <see cref="ObjectId" /></para>
///                 <para xml:lang="zh">将 <see cref="string" /> 和 <see cref="ObjectId" /> 相互转换</para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <para xml:lang="en">Add default <see cref="ObjectId" /> when <see langword="ID" /> or <see langword="Id" /> field is null</para>
///                 <para xml:lang="zh">当 <see langword="ID" /> 或者 <see langword="Id" /> 字段为空值时添加默认 <see cref="ObjectId" /></para>
///             </description>
///         </item>
///     </list>
/// </summary>
internal sealed class StringToObjectIdIdGeneratorConvention : ConventionBase, IPostProcessingConvention
{
    /// <summary>
    ///     <para xml:lang="en">Process after class mapping is completed</para>
    ///     <para xml:lang="zh">在类映射完成后进行处理</para>
    /// </summary>
    /// <param name="classMap">
    ///     <para xml:lang="en">Bson class map</para>
    ///     <para xml:lang="zh">Bson类映射</para>
    /// </param>
    public void PostProcess(BsonClassMap classMap)
    {
        ProcessClassMap(classMap);
    }

    /// <summary>
    ///     <para xml:lang="en">Recursively process class mapping to ensure that the Id fields of all sub-objects are correctly configured</para>
    ///     <para xml:lang="zh">递归处理类映射，确保所有子对象的Id字段都被正确配置</para>
    /// </summary>
    /// <param name="classMap">
    ///     <para xml:lang="en">Bson class map</para>
    ///     <para xml:lang="zh">Bson类映射</para>
    /// </param>
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
///     <para xml:lang="en">Custom StringObjectIdGenerator for automatically generating ObjectId when inserting data</para>
///     <para xml:lang="zh">自定义的 StringObjectIdGenerator，用于在插入数据时自动生成 ObjectId</para>
/// </summary>
file class CustomStringObjectIdGenerator : IIdGenerator
{
    /// <summary>
    ///     <para xml:lang="en">Generate new Id</para>
    ///     <para xml:lang="zh">生成新的Id</para>
    /// </summary>
    /// <param name="container">
    ///     <para xml:lang="en">Container object</para>
    ///     <para xml:lang="zh">容器对象</para>
    /// </param>
    /// <param name="document">
    ///     <para xml:lang="en">Document object</para>
    ///     <para xml:lang="zh">文档对象</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Generated Id</para>
    ///     <para xml:lang="zh">生成的Id</para>
    /// </returns>
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
                if (itemIdMemberMap is not null && itemIdMemberMap.MemberType == typeof(string) && IsEmpty(itemIdMemberMap.Getter(item)))
                {
                    itemIdMemberMap.Setter(item, ObjectId.GenerateNewId().ToString());
                }
            }
        }
        // 返回生成的ObjectId
        return ObjectId.GenerateNewId().ToString();
    }

    /// <summary>
    ///     <para xml:lang="en">Check if Id is empty</para>
    ///     <para xml:lang="zh">检查Id是否为空</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">Id object</para>
    ///     <para xml:lang="zh">Id对象</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if Id is empty, otherwise false</para>
    ///     <para xml:lang="zh">如果Id为空则返回true，否则返回false</para>
    /// </returns>
    public bool IsEmpty(object? id) => string.IsNullOrWhiteSpace(id?.ToString());
}