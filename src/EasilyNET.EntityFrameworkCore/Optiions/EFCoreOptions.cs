using System.Diagnostics.CodeAnalysis;

namespace EasilyNET.EntityFrameworkCore.Optiions;

/// <summary>
/// 上下文配置
/// </summary>
public class EFCoreOptions
{

    /// <summary>
    /// 链接字符串
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    /// 默认DB上下文选项
    /// </summary>
    internal Action<IServiceProvider, DbContextOptionsBuilder> DefaultDbContextOptionsAction { get; private set; } = default!;
    
    /// <summary>
    /// 添加上下文选项
    /// </summary>
    /// <param name="action"></param>
    public void AddContextOptions([NotNull] Action<IServiceProvider,DbContextOptionsBuilder> action)
    {
        action.NotNull(nameof(action));

        DefaultDbContextOptionsAction = action;
    }

}