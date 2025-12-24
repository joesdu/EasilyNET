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
        context.Services.AddMongoGridFS(serverConfigure: s =>
        {
            s.EnableController = true;
            s.AuthorizeData.Add(new AuthorizeAttribute());
            // 或者添加带策略的授权 (相当于 [Authorize(Policy = "MyPolicy")])
            // s.AuthorizeData.Add(new AuthorizeAttribute("MyPolicy"));
        });
        await Task.CompletedTask;
    }
}