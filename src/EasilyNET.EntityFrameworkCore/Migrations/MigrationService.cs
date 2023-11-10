using System.Diagnostics;
using System.Text;

namespace EasilyNET.EntityFrameworkCore.Migrations;

/// <summary>
/// 自动迁移
/// </summary>
public class MigrationService : IMigrationService
{
    /// <summary>
    /// 安装dotnet-ef工具
    /// </summary>
    public void InstallEfTool()
    {
        var startInfo = new ProcessStartInfo("dotnet tool", "install --global dotnet-ef")
        {
            UseShellExecute = false, // 使用操作系统的外壳来启动
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        var process = new Process();
        process.StartInfo = startInfo;
        process.Start();
    }
}