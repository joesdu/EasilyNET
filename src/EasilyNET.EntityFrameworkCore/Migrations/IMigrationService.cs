namespace EasilyNET.EntityFrameworkCore.Migrations;

/// <summary>
/// 迁移
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// 安装 dotnet-ef 全局工具,执行命令: dotnet tool install -g dotnet-ef
    /// </summary>
    public Task InstallEfToolAsync();

    /// <summary>
    /// 更新 dotnet-ef 全局工具,执行命令: dotnet tool update -g dotnet-ef
    /// </summary>
    /// <returns></returns>
    public Task UpdateEfToolAsync();

    /// <summary>
    /// 添加迁移,执行命令: dotnet ef migrations add migrationName
    /// </summary>
    /// <param name="name">迁移的名称</param>
    /// <param name="dbContextRootPath">上下文根目录</param>
    /// <param name="isVerbose">是否显示详细输出</param>
    public Task AddMigrationAsync(string name, string dbContextRootPath, bool isVerbose = false);

    /// <summary>
    /// 将数据库更新到上一次迁移或指定的迁移。
    /// </summary>
    public Task UpdateDatabaseAsync(string dbContextRootPath);
}