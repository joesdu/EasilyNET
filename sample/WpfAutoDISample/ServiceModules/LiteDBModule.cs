using System.IO;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using LiteDB;
using LiteDB.Engine;
using Microsoft.Extensions.DependencyInjection;
using WpfAutoDISample.Common;

namespace WpfAutoDISample.ServiceModules;

internal sealed class LiteDBModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var cache_db = Path.Combine(Constant.GetUserDataPath(), "cache");
        // 确保数据目录存在
        if (!Directory.Exists(cache_db)) Directory.CreateDirectory(cache_db);
        context.Services.AddKeyedSingleton<ILiteDatabase>(Constant.AppCacheServiceKey, (_, _) => new LiteDatabase(new LiteEngine(new EngineSettings
        {
            AutoRebuild = true,
            Filename = Path.Combine(cache_db, Constant.AppCacheDB)
        })));
        var ui_db = Path.Combine(Constant.GetUserDataPath(), "ui");
        // 确保数据目录存在
        if (!Directory.Exists(ui_db)) Directory.CreateDirectory(ui_db);
        context.Services.AddKeyedSingleton<ILiteDatabase>(Constant.UiConfigServiceKey, (_, _) => new LiteDatabase(new LiteEngine(new EngineSettings
        {
            AutoRebuild = true,
            Filename = Path.Combine(ui_db, Constant.UiConfigDB)
        })));
    }
}