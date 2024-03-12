using System.Text;

namespace EasilyNET.Core.Initialization;

/// <summary>
/// ini 文件内容
/// </summary>
/// <param name="file"></param>
public sealed class IniContext(FileInfo file)
{
    /// <summary>
    /// 所有节
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public Content Sections { get; set; } = new();

    /// <summary>
    /// 文件路径
    /// </summary>
    public FileInfo File { get; } = file;

    /// <summary>
    /// 转字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (Sections is not { Items.Count: > 0 }) return "";
        StringBuilder sb = new();
        foreach (var item in Sections.Items)
        {
            _ = sb.AppendLine($"[{item.Name}]");
            var args = item.Args.Select(x => $"{x.Key}={x.Value}");
            foreach (var argItem in args)
            {
                _ = sb.AppendLine(argItem);
            }
        }
        return sb.ToString();
    }
}
