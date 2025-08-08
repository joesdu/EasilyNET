using System.Text.Json.Serialization;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApi.Test.Unit;

/// <summary>
/// Mongo测试数据类型
/// </summary>
[MongoCompoundIndex(["timeSpan", "dateOnly"],
    [EIndexType.Ascending, EIndexType.Descending],
    Name = "MongoCompoundIndexTest",
    Unique = false)]
public class MongoTest
{
    /// <summary>
    /// DateTimeDescending
    /// </summary>
    [BsonIgnore]
    [JsonIgnore]
    private const string DateTimeDescending = "datetime_desc";

    /// <summary>
    /// ID
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 完整DateTime
    /// </summary>
    [MongoIndex(EIndexType.Descending, Name = DateTimeDescending)]
    public DateTime DateTime { get; set; }

    /// <summary>
    /// 测试UTC时间
    /// </summary>
    public DateTime DateTimeUtc { get; set; }

    /// <summary>
    /// TimeSpan类型
    /// </summary>
    public TimeSpan TimeSpan { get; set; }

    /// <summary>
    /// DateOnly类型
    /// </summary>
    public DateOnly DateOnly { get; set; }

    /// <summary>
    /// TimeOnly类型
    /// </summary>
    public TimeOnly TimeOnly { get; set; }

    /// <summary>
    /// DateOnly类型
    /// </summary>
    public DateOnly? NullableDateOnly { get; set; }

    /// <summary>
    /// TimeOnly类型
    /// </summary>
    public TimeOnly? NullableTimeOnly { get; set; }
}