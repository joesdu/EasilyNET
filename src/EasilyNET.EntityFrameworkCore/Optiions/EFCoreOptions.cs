namespace EasilyNET.EntityFrameworkCore.Optiions;

/// <summary>
/// 上下文配置
/// </summary>
public class EFCoreOptions
{
    /// <summary>
    /// 链接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 默认DB上下文选项
    /// </summary>

    public Action<DbContextOptionsBuilder> ConfigureDbContextBuilder { get; set; } = default!;
}