using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.WebCore.WebSocket;
using WebApi.Test.Unit.BackgroundServices;
using WebApi.Test.Unit.WebSocketHandlers;

namespace WebApi.Test.Unit.ServiceModules;

internal sealed class WebSocketServerModule : AppModule
{
    /// <inheritdoc />
    public override Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddHostedService<WebSocketClientTestService>();
        return Task.CompletedTask;
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