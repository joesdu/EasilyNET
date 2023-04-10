// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Initialization;

/// <summary>
/// Ini帮助类
/// </summary>
public static class IniHelper
{
    /// <summary>
    /// 读取或者创建ini
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns></returns>
    public static async Task<IniContext> ReadOrCreateAsync(string path)
    {
        var fileInfo = new FileInfo(path);
        var iniContext = new IniContext(fileInfo);
        string[] lines;
        if (File.Exists(path))
        {
#if !NETSTANDARD2_0
            lines = await File.ReadAllLinesAsync(path);
#else
            lines = File.ReadAllLines(path);
#endif
        }
        else
        {
#if !NETSTANDARD2_0
            await File.WriteAllTextAsync(path, string.Empty);
#else
            File.WriteAllText(path, string.Empty);
#endif
            return iniContext;
        }
        Section? tempSection = null;
        for (var i = 0; i < lines.Length; i++)
        {
            var item = lines[i].TrimStart();
            //为注解
            if (item.StartsWith(";") || item.StartsWith("；") || string.IsNullOrEmpty(item))
            {
                continue;
            }
        NextSection:
            if (tempSection is null)
            {
                var sectionStart = item.IndexOf('[');
                var sectionEnd = item.LastIndexOf(']');
                if (sectionStart == 0 && sectionEnd >= 1)
                {
                    tempSection = new()
                    {
#if !NETSTANDARD2_0
                        Name = item[(sectionStart + 1)..sectionEnd],
#else
                        Name = item.Substring(sectionStart + 1, sectionEnd - sectionStart + 1),
#endif
                        Line = i,
                        Args = new()
                    };
                    iniContext.Sections.Items.Add(tempSection);
                }
                continue;
            }
            var ksIndex = item.IndexOf('=');
            if (ksIndex == -1)
            {
                tempSection = null;
                goto NextSection;
            }
#if !NETSTANDARD2_0
            var k = item[..ksIndex];
            var v = item[(ksIndex + 1)..item.Length];
#else
            var k = item.Substring(0, ksIndex);
            var v = item.Substring(ksIndex + 1, item.Length - ksIndex + 1);
#endif
            if (string.IsNullOrWhiteSpace(k)) continue;
            tempSection.Args?.Add(k, v);
        }
        return iniContext;
    }

    /// <summary>
    /// 读取或者创建ini
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static IniContext ReadOrCreate(string path)
    {
        var iniTask = ReadOrCreateAsync(path);
        iniTask.Wait();
        return iniTask.Result;
    }

    /// <summary>
    /// 更新ini
    /// </summary>
    /// <param name="iniContext"></param>
    public static async Task UpgradeAsync(IniContext iniContext)
    {
        if (iniContext.File is null) throw new("文件路径不能为空");
        string? sb = null;
        _ = await Task.Run(() => sb = iniContext.ToString());
#if !NETSTANDARD2_0
        await File.WriteAllTextAsync(iniContext.File.FullName, sb);
#else
        File.WriteAllText(iniContext.File.FullName, sb);
#endif
    }

    /// <summary>
    /// 更新ini
    /// </summary>
    /// <param name="iniContext"></param>
    public static void Upgrade(IniContext iniContext)
    {
        var iniTask = UpgradeAsync(iniContext);
        iniTask.Wait();
    }
}