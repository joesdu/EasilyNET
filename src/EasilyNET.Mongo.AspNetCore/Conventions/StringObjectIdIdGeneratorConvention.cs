using System.Collections;
using System.Collections.Concurrent;
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
    ///     <para xml:lang="en">
    ///     Tracks types that have already been processed to prevent infinite recursion
    ///     when circular references exist (e.g., Type A references Type B, Type B references Type A).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     追踪已处理的类型，防止存在循环引用时（如类型 A 引用类型 B，类型 B 引用类型 A）导致无限递归。
    ///     </para>
    /// </summary>
    private static readonly ConcurrentDictionary<Type, byte> ProcessedTypes = new();

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
        var classType = classMap.ClassType;
        // 跳过已处理的类型，防止循环引用导致无限递归
        if (!ProcessedTypes.TryAdd(classType, 0))
        {
            return;
        }
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
            // 跳过基础类型、字符串、枚举和值类型，避免不必要递归
            if (memberType.IsPrimitive || memberType == typeof(string) || memberType.IsEnum || memberType.IsValueType)
            {
                continue;
            }
            // 跳过已处理的类型
            if (ProcessedTypes.ContainsKey(memberType))
            {
                continue;
            }
            // 如果成员类型是泛型集合，则处理集合中的项
            if (typeof(IEnumerable).IsAssignableFrom(memberType) && memberType.IsGenericType)
            {
                var itemType = memberType.GetGenericArguments().FirstOrDefault();
                if (itemType is null || itemType.IsPrimitive || itemType == typeof(string) || itemType.IsEnum || itemType.IsValueType)
                {
                    continue;
                }
                if (ProcessedTypes.ContainsKey(itemType))
                {
                    continue;
                }
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
    ///     <para xml:lang="en">
    ///     Cache for document types and their collection members that may contain items requiring Id processing.
    ///     The key is the document <see cref="Type" />, and the value is the list of <see cref="BsonMemberMap" />
    ///     representing enumerable members (e.g. child collections) on that document.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     文档类型与其需要处理的集合成员的缓存。
    ///     键为文档的 <see cref="Type" />，值为该文档上表示可枚举成员（如子集合）的 <see cref="BsonMemberMap" /> 列表。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     Backed by <see cref="ConcurrentDictionary{TKey,TValue}" /> and declared as <c>static</c>, this cache is
    ///     shared across all instances of <see cref="CustomStringObjectIdGenerator" /> and is safe for concurrent
    ///     reads and writes from multiple threads. It avoids repeatedly inspecting class maps and reflection for
    ///     each generated Id.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     使用 <see cref="ConcurrentDictionary{TKey,TValue}" /> 实现，并声明为 <c>static</c>，
    ///     在所有 <see cref="CustomStringObjectIdGenerator" /> 实例之间共享，支持多线程并发读写。
    ///     通过缓存集合成员映射，避免在每次生成 Id 时重复执行 ClassMap 分析和反射操作，从而提升性能。
    ///     </para>
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, List<BsonMemberMap>> _documentCollectionMembersCache = new();

    /// <summary>
    ///     <para xml:lang="en">
    ///     Cache for item types and their Id member map used when generating Ids for elements inside collections.
    ///     The key is the item <see cref="Type" />, and the value is the <see cref="BsonMemberMap" /> of the
    ///     string-typed Id member, or <see langword="null" /> when the type has no applicable Id member.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     子项类型与其 Id 成员映射的缓存，用于为集合中的元素生成 Id。
    ///     键为子项的 <see cref="Type" />，值为其字符串类型 Id 成员对应的 <see cref="BsonMemberMap" />；
    ///     如果该类型不存在可用的 Id 成员，则缓存为 <see langword="null" />。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     Implemented with <see cref="ConcurrentDictionary{TKey,TValue}" /> as a <c>static</c> field, this cache is
    ///     thread-safe and shared globally for all uses of <see cref="CustomStringObjectIdGenerator" />. It prevents
    ///     repeated lookup of the Id member map for the same item type while ensuring correctness under concurrent
    ///     access.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     该缓存由 <see cref="ConcurrentDictionary{TKey,TValue}" /> 实现，并作为 <c>static</c> 字段在全局共享，
    ///     可安全地在多线程环境下并发访问。通过缓存子项类型对应的 Id 成员映射，避免对同一类型重复查询，
    ///     在保证正确性的同时降低运行时开销。
    ///     </para>
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, BsonMemberMap?> _itemIdMemberMapCache = new();

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
    public object GenerateId(object container, object document)
    {
        var docType = document.GetType();

        // 获取或计算该文档类型的集合成员列表
        var collectionMembers = _documentCollectionMembersCache.GetOrAdd(docType, type =>
        {
            var classMap = BsonClassMap.LookupClassMap(type);
            return [.. classMap.AllMemberMaps.Where(memberMap => typeof(IEnumerable).IsAssignableFrom(memberMap.MemberType) && memberMap.MemberType.IsGenericType && memberMap.MemberType.GetGenericArguments().Length > 0)];
        });
        foreach (var memberMap in collectionMembers)
        {
            if (memberMap.Getter(document) is not IEnumerable items)
            {
                continue;
            }
            foreach (var item in items)
            {
                if (item is null)
                {
                    continue;
                }
                var itemType = item.GetType();
                // 获取或计算该项类型的Id成员映射
                var itemIdMemberMap = _itemIdMemberMapCache.GetOrAdd(itemType, type =>
                {
                    var itemClassMap = BsonClassMap.LookupClassMap(type);
                    var idMap = itemClassMap.IdMemberMap;
                    // 仅当Id类型为string时才缓存
                    return idMap is { MemberType: not null } && idMap.MemberType == typeof(string) ? idMap : null;
                });
                // 如果子对象的Id字段为空，则为其生成新的ObjectId
                if (itemIdMemberMap is not null && IsEmpty(itemIdMemberMap.Getter(item)))
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
    public bool IsEmpty(object? id) => string.IsNullOrWhiteSpace(id?.ToString());
}