using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Mongo.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// GridFS
/// </summary>
internal sealed class MongoFSModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.Configure<FormOptions>(c =>
               {
                   c.MultipartHeadersLengthLimit = int.MaxValue;
                   c.MultipartBodyLengthLimit = long.MaxValue;
                   c.ValueLengthLimit = int.MaxValue;
               })
               .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
               .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);
        context.Services.AddMongoGridFS();
        await Task.CompletedTask;
    }
}