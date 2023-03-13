namespace EasilyNET.RabbitBus.Extensions;

/// <summary>
/// 扩展枚举
/// </summary>
internal static class EnumExtensions
{
    /// <summary>
    /// 转成显示名字
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static string? ToDescription(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.ToDescription();
    }
}