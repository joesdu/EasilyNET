using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// GridFS
/// </summary>
internal sealed class MongoFSModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddMongoGridFS();
        await Task.CompletedTask;
    }
}