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

    /// <summary>
    /// 创建
    /// </summary>
    /// <returns></returns>
    public static EFCoreOptions Create(Action<IServiceProvider, EFCoreOptions> action, IServiceProvider serviceProvider)
    {
        var options = new EFCoreOptions();
        action(serviceProvider, options);
        options.Validate();
        return options;
    }

    /// <summary>
    /// 验证
    /// </summary>
    private void Validate()
    {
        if (ConfigureDbContextBuilder is null)
        {
            throw new InvalidOperationException("ConfigureDbContextBuilder未配置。");
        }
    }
}