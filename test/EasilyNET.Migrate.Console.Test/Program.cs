// See https://aka.ms/new-console-template for more information

using EasilyNET.AutoDependencyInjection;
using EasilyNET.EntityFrameworkCore.Migrations;
using EasilyNET.Migrate.Console.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable ClassNeverInstantiated.Global

using (var application = ApplicationFactory.Create<TestAppModule>())
{
    var serviceProvider = application.ServiceProvider!;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var migrationService = serviceProvider.GetService<IMigrationService>();
    var dir = AppDomain.CurrentDomain.BaseDirectory.Split(Path.DirectorySeparatorChar);
    var slice = new ArraySegment<string>(dir, 0, dir.Length - 5);
    var slice1 = new ArraySegment<string>(dir, 0, dir.Length);
    var path = Path.Combine([.. slice]);
    var curPath = Path.Combine([.. slice1]);
    var rootPath = Path.Combine(path, "EasilyNET.Migrate.Console.Test.Model");
    var testDbContext = serviceProvider.GetService<TestDbContext>()!;
    const string name = "Init_123";
    var isMigration = testDbContext.Database.GetPendingMigrations().Where(o => o.Contains(name))!.Any();

    //暂时不更新，因为8.0问题
    try
    {
        //await migrationService?.InstallEfToolAsync()!;
        //await migrationService?.UpdateEfToolAsync()!;
        if (!isMigration)
        {
            await migrationService?.AddMigrationAsync(name, rootPath)!;
        }
        //await migrationService?.UpdateDatabaseAsync(rootPath)!;
        const string dbName = "My.db";
        //拷贝文件
        logger?.LogInformation("拷贝数据库文件");
        var dbFilePath = Path.Combine(rootPath, dbName);
        if (File.Exists(dbFilePath)) //说明存在
        {
            var rootDbFilePathShm = Path.Combine(curPath, dbName + "-shm");
            var rootDbFilePathWal = Path.Combine(curPath, dbName + "-wal");
            var rootDbFilePath = Path.Combine(curPath, dbName);
            //File.Delete(rootDbFilePathShm);
            //File.Delete(rootDbFilePathWal);
            //File.Delete(rootDbFilePath);
            //File.Copy(dbFilePath, rootDbFilePath, true);
        }
        logger?.LogInformation("开始添加种子数据");
        var dbSets = serviceProvider.GetServices<IDbSetup>();
        foreach (var dbSet in dbSets)
        {
            await dbSet.Init();
        }
        logger?.LogInformation("种子数据添加成功");
    }
    catch (Exception e)
    {
        logger?.LogError("{msg}", e.Message);
    }
}
Console.ReadKey();