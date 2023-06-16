﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.GridFS;

/// <summary>
/// 服务注册于配置扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    internal static string BusinessApp { get; private set; } = string.Empty;

    /// <summary>
    /// 注册GridFS服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="db">IMongoDatabase,为空情况下使用默认数据库easilyfs</param>
    /// <param name="fsOptions"></param>
    /// <returns></returns>
    public static void AddEasilyNETGridFS(this IServiceCollection services, IMongoDatabase? db = null, Action<EasilyNETGridFSOptions>? fsOptions = null)
    {
        var client = services.BuildServiceProvider().GetService<IMongoClient>();
        var options = new EasilyNETGridFSOptions();
        fsOptions?.Invoke(options);
        if (db is null)
        {
            options.DefaultDB = true;
            if (client is null) throw new("无法从容器中获取服务依赖,请传入db参数.");
        }
        BusinessApp = options.BusinessApp;
        var easily_db = options.DefaultDB ? client!.GetDatabase("easilyfs") : db;
        _ = services.Configure<FormOptions>(c =>
                    {
                        c.MultipartHeadersLengthLimit = int.MaxValue;
                        c.MultipartBodyLengthLimit = long.MaxValue;
                        c.ValueLengthLimit = int.MaxValue;
                    })
                    .AddSingleton(new GridFSBucket(easily_db, options.Options))
                    .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
                    .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null)
                    .AddSingleton(easily_db!.GetCollection<GridFSItemInfo>(options.ItemInfo));
    }
}