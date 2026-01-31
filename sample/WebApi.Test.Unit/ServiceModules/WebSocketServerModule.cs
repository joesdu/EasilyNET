using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using WebApi.Test.Unit.BackgroundServices;
using WebApi.Test.Unit.WebSocketHandlers;

namespace WebApi.Test.Unit.ServiceModules;

internal sealed class WebSocketServerModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddHostedService<WebSocketClientTestService>();
    }

    /// <inheritdoc />
    public override Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as WebApplication;
        app?.UseWebSockets();
        app?.MapWebSocketHandler<ChatHandler>("/ws/chat");
        return Task.CompletedTask;
    }
}