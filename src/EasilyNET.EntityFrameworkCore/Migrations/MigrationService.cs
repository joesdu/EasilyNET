using System.Diagnostics;
using System.Text;

namespace EasilyNET.EntityFrameworkCore.Migrations;

/// <summary>
/// 自动迁移
/// </summary>
/// <remarks>
/// </remarks>
/// <param name="logger"></param>
public class MigrationService(ILogger<MigrationService>? logger) : IMigrationService
{
    private readonly ILogger<MigrationService> _logger = logger ?? NullLogger<MigrationService>.Instance;

    /// <summary>
    /// 安装dotnet-ef工具
    /// </summary>
    public void InstallEfTool()
    {
        _logger.LogTrace("准备install dotnet-ef 全局工具.");
        var process = new Process
        {
            StartInfo = new("dotnet", "tool install --global dotnet-ef")
            {
                UseShellExecute = false, // 使用操作系统的外壳来启动
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("{msg}", eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("失败:{msg}", eventArgs.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public void UpdateEfTool()
    {
        _logger.LogTrace("准备update dotnet-ef 全局工具.");
        var process = new Process
        {
            StartInfo = new("dotnet", "tool update --global dotnet-ef")
            {
                UseShellExecute = false, // 使用操作系统的外壳来启动
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("{msg}", eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("失败:{msg}", eventArgs.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="dbContextRootPath"></param>
    public void AddMigration(string name, string dbContextRootPath)
    {
        name.NotNullOrEmpty(" 请设置迁移名称");
        dbContextRootPath.NotNullOrEmpty("请设置上下根目录");
        _logger.LogTrace("准备执行添加迁移:{name}，上下文根路径为:{dbContextRootPath}", name, dbContextRootPath);
        var process = new Process
        {
            StartInfo = new("dotnet", $"ef migrations add {name} -v")
            {
                UseShellExecute = false, // 使用操作系统的外壳来启动
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = dbContextRootPath
            }
        };
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("{msg}", eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("失败:{msg}", eventArgs.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.Close(); // 关闭进程
    }

    /// <summary>
    /// </summary>
    public void UpdateDatabase(string dbContextRootPath)
    {
        _logger.LogTrace("准备更新数据库");
        dbContextRootPath.NotNullOrEmpty("请设置上下根目录");
        var process = new Process
        {
            StartInfo = new("dotnet", "ef database update")
            {
                UseShellExecute = false, // 使用操作系统的外壳来启动
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = dbContextRootPath
            }
        };
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("{msg}", eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.LogTrace("失败:{msg}", eventArgs.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        process.Close(); // 关闭进程
    }
}