namespace EasilyNET.WebCore.Attributes;

/// <summary>
/// 防抖特性
/// </summary>
/// <param name="interval">请求间隔时间(默认:1000ms)</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RepeatSubmitAttribute(int interval = 1000) : Attribute
{
    /// <summary>
    /// 请求间隔
    /// </summary>
    public int Interval { get; } = interval;
}