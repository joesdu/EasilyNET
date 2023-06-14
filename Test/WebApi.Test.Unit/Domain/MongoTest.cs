namespace WebApi.Test.Unit;

/// <summary>
/// Mongo测试数据类型
/// </summary>
public class MongoTest
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 完整DateTime
    /// </summary>
    public DateTime DateTime { get; set; }

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
}