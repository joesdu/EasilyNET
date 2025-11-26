using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// GridFS
/// </summary>
internal sealed class MongoFSModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddMongoGridFS(serverConfigure: options =>
        {
            options.EnableController = true;
#if !DEBUG
            options.AuthorizeData.Add(new AuthorizeAttribute());
#endif
        });
        await Task.CompletedTask;
    }
}