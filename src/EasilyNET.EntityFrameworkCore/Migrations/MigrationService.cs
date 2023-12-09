namespace EasilyNET.EntityFrameworkCore.Migrations;

/// <summary>
/// 自动迁移
/// </summary>
/// <remarks>
/// </remarks>
/// <param name="logger"></param>
public class MigrationService(ILogger<MigrationService>? logger) : IMigrationService
{
    private const string FileName = "dotnet";

    /// <summary>
    /// 工具参数
    /// </summary>
    private const string Tool = "tool";

    /// <summary>
    /// 工具安装
    /// </summary>
    private const string ToolInstall = "install";

    /// <summary>
    /// 工具更新
    /// </summary>
    private const string ToolUpdate = "update";

    /// <summary>
    /// 全局参数
    /// </summary>
    private const string Global = "--global";

    /// <summary>
    /// dotnet ef 参数
    /// </summary>
    private const string Dotnet_EF = "dotnet-ef";

    /// <summary>
    /// ef migrations
    /// </summary>
    private const string Ef_Migrations = "ef migrations";

    /// <summary>
    /// ef migrations add
    /// </summary>
    private const string Add = "add";

    /// <summary>
    /// -v显示详细输出。
    /// </summary>
    private const string Verbose = "-v";

    /// <summary>
    /// ef database update
    /// </summary>
    private const string Ef_Database_Update = "ef database update";

    private readonly ILogger<MigrationService> _logger = logger ?? NullLogger<MigrationService>.Instance;

    /// <summary>
    /// 安装dotnet-ef工具
    /// </summary>
    public async Task InstallEfToolAsync()
    {
        _logger.LogInformation("准备install dotnet-ef 全局工具.");
        await ExecuteBufferedAsync($"{Tool} {ToolInstall} {Global} {Dotnet_EF}");
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public async Task UpdateEfToolAsync()
    {
        _logger.LogInformation("准备update dotnet-ef 全局工具.");
        await ExecuteBufferedAsync($"{Tool} {ToolUpdate} {Global} {Dotnet_EF}");
    }

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="dbContextRootPath"></param>
    /// <param name="isVerbose">是否显示详细输出</param>
    public async Task AddMigrationAsync(string name, string dbContextRootPath, bool isVerbose = false)
    {
        name.NotNullOrEmpty(" 请设置迁移名称");
        dbContextRootPath.NotNullOrEmpty("请设置上下根目录");
        _logger.LogTrace("准备执行添加迁移:{name}，上下文根路径为:{dbContextRootPath}", name, dbContextRootPath);
        await ExecuteBufferedAsync($"{Ef_Migrations} {Add} {name} {(isVerbose ? Verbose : string.Empty)}", dbContextRootPath);
    }

    /// <summary>
    /// </summary>
    public async Task UpdateDatabaseAsync(string dbContextRootPath)
    {
        _logger.LogInformation("准备更新数据库");
        dbContextRootPath.NotNullOrEmpty("请设置上下根目录");
        await ExecuteBufferedAsync($"{Ef_Database_Update}", dbContextRootPath);
    }

    /// <summary>
    /// 异步执行
    /// </summary>
    /// <param name="arguments">参数</param>
    /// <param name="workingDirPath">工作路径</param>
    /// <returns></returns>
    private async Task ExecuteBufferedAsync(string arguments, string? workingDirPath = default)
    {
        arguments.NotNullOrEmpty(nameof(arguments));
        var command = Cli.Wrap(FileName).WithValidation(CommandResultValidation.None)
                         .WithStandardOutputPipe(PipeTarget.ToDelegate(msg => { _logger.LogTrace(msg); }, Encoding.UTF8))
                         .WithStandardErrorPipe(PipeTarget.ToDelegate(msg => { _logger.LogError(msg); }, Encoding.UTF8))
                         .WithArguments(arguments);
        if (!string.IsNullOrWhiteSpace(workingDirPath))
        {
            command = command.WithWorkingDirectory(workingDirPath);
        }
        await command.ExecuteBufferedAsync(Encoding.UTF8, Encoding.UTF8);
    }
}