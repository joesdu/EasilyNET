namespace EasilyNET.Core.Initialization;

/// <summary>
/// 节
/// </summary>
public sealed class Section
{
    /// <summary>
    /// 节名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节行号
    /// </summary>
    public long Line { get; set; }

    /// <summary>
    /// 键值对
    /// </summary>
    public Dictionary<string, string> Args { get; set; } = new();
}