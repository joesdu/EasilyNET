using EasilyNET.Core.Enums;

namespace WebApi.Test.Unit.Domain;

/// <summary>
/// 用枚举值作为字典的键
/// </summary>
public class EnumKeyDicTest
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 使用枚举值作为字典的键
    /// </summary>
    public Dictionary<EZodiac, object> Dic { get; set; } = [];
}