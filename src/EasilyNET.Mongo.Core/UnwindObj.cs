using MongoDB.Bson.Serialization.Attributes;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core;

/// <summary>
/// Unwind 操作符使用的类型
/// </summary>
/// <typeparam name="T"></typeparam>
public class UnwindObj<T>
{
    /// <summary>
    /// 1.T as List,use for Projection,
    /// 2.T as single Object,use for MongoDB array field Unwind result
    /// </summary>
    [BsonElement("Obj")]
    public T? Obj { get; set; }

    /// <summary>
    /// when T as List,record Count
    /// </summary>
    [BsonElement("Count")]
    public int Count { get; set; }

    /// <summary>
    /// record array field element's index before Unwinds
    /// </summary>
    [BsonElement("Index")]
    public int Index { get; set; }
}