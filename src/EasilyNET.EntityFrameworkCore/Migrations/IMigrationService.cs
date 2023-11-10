namespace EasilyNET.EntityFrameworkCore.Migrations;

/// <summary>
/// 迁移
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// 安装dotnet-ef
    /// </summary>
    public void InstallEfTool();
}