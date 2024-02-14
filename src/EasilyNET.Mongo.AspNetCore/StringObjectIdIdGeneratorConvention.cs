using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo.AspNetCore;

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
    public void PostProcess(BsonClassMap classMap)
    {
        var idMemberMap = classMap.IdMemberMap;
        if (idMemberMap is null || idMemberMap.IdGenerator is not null) return;
        if (idMemberMap.MemberType == typeof(string)) _ = idMemberMap.SetIdGenerator(StringObjectIdGenerator.Instance).SetSerializer(new StringSerializer(BsonType.ObjectId));
    }
}
