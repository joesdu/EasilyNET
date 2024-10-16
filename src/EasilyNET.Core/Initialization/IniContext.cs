using System.Text;

namespace EasilyNET.Core.Initialization;

/// <summary>
/// ini 文件内容
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="file">文件信息</param>
public sealed class IniContext(FileInfo file)
{
    /// <summary>
    /// 所有节
    /// </summary>
    public Content Sections { get; set; } = new();

    /// <summary>
    /// 文件路径
    /// </summary>
    public FileInfo File { get; } = file;

    /// <summary>
    /// 转字符串
    /// </summary>
    /// <returns>返回ini文件内容的字符串表示</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var section in Sections.Items)
        {
            sb.AppendLine($"[{section.Name}]");
            foreach (var kvp in section.Args)
            {
                sb.AppendLine($"{kvp.Key}={kvp.Value}");
            }
        }
        return sb.ToString();
    }
}