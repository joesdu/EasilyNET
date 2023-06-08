using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.GridFS.Extension;

/// <summary>
/// 配置虚拟文件路径扩展
/// </summary>
public static class ApplicationBuilderExtension
{
    /// <summary>
    /// 注册虚拟文件路径中间件.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static void UseGridFSVirtualPath(this IApplicationBuilder app, IConfiguration config)
    {
        var setting = config.GetSection(EasilyFSSettings.Position).Get<EasilyFSSettings>() ?? throw new($"不存在 {EasilyFSSettings.Position}配置,请添加配置");
        if (!Directory.Exists(setting.PhysicalPath)) _ = Directory.CreateDirectory(setting.PhysicalPath);
        _ = app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(setting.PhysicalPath),
            RequestPath = setting.VirtualPath
        });
    }
}