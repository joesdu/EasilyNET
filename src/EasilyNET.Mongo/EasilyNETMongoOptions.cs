using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo;

/// <summary>
/// Mongodb配置选项
/// </summary>
public sealed class EasilyNETMongoOptions
{
    /// <summary>
    /// ObjectId到String转换的类型[该列表中的对象,不会将Id,ID字段转化为ObjectId类型.在数据库中存为字符串格式]
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public static List<Type> ObjIdToStringTypes { get; private set; } = new();

    /// <summary>
    /// ObjectId到String转换的类型[该列表中的对象,不会将Id,ID字段转化为ObjectId类型.在数据库中存为字符串格式]
    /// </summary>
    public List<Type> ObjectIdToStringTypes
    {
        get => ObjIdToStringTypes;
        set => ObjIdToStringTypes = value;
    }

    /// <summary>
    /// 是否使用本库提供的默认转换,默认:true
    /// 1.驼峰名称格式
    /// 2.忽略代码中未定义的字段
    /// 3._id映射为实体中的ID或者Id,反之亦然
    /// 4.将枚举类型存储为字符串格式
    /// </summary>
    public bool DefaultConventionRegistry { get; set; } = true;

    internal Dictionary<string, ConventionPack> ConventionRegistry { get; set; } = new()
    {
        {
            $"{EasilyNETConstant.Pack}-{ObjectId.GenerateNewId()}", new()
            {
                new CamelCaseElementNameConvention(),             // 驼峰名称格式
                new IgnoreExtraElementsConvention(true),          //
                new NamedIdMemberConvention("Id", "ID"),          // _id映射为实体中的ID或者Id
                new EnumRepresentationConvention(BsonType.String) // 将枚举类型存储为字符串格式
            }
        }
    };

    /// <summary>
    /// 添加自己的一些Convention配置,用于设置mongodb序列化反序列化的一些表现.
    /// </summary>
    /// <param name="dic"></param>
    // ReSharper disable once UnusedMember.Global
    public void AppendConventionRegistry(Dictionary<string, ConventionPack> dic)
    {
        foreach (var item in dic) ConventionRegistry.Add(item.Key, item.Value);
    }
}