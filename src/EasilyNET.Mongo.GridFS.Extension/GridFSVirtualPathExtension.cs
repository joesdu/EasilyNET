using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.GridFS.Extension;

/// <summary>
/// 配置虚拟文件路径扩展
/// </summary>
public static class GridFSVirtualPathExtension
{
    /// <summary>
    /// 注册虚拟文件路径中间件.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static void UseEasilyNETGridFSVirtualPath(this IApplicationBuilder app, IConfiguration config)
    {
        var hoyoFile = config.GetSection(EasilyNETStaticFileSettings.Position).Get<EasilyNETStaticFileSettings>();
        if (!Directory.Exists(hoyoFile.PhysicalPath)) _ = Directory.CreateDirectory(hoyoFile.PhysicalPath);
        _ = app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(hoyoFile.PhysicalPath), RequestPath = hoyoFile.VirtualPath
        });
    }
}