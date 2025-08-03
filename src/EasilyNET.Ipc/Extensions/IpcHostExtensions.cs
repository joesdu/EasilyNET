using EasilyNET.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EasilyNET.Ipc.Extensions;

/// <summary>
/// IPC 服务初始化扩展
/// </summary>
public static class IpcHostExtensions
{
    /// <summary>
    /// 初始化 IPC 服务
    /// </summary>
    /// <param name="host">应用主机</param>
    /// <returns>应用主机</returns>
    public static IHost InitializeIpc(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IpcCommandRegistry>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IpcCommandDispatcher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IOptions<IpcConfiguration>>().Value;

        // 注册所有命令类型
        foreach (var registration in configuration.CommandRegistrations)
        {
            // 使用反射调用泛型方法注册命令
            var registerMethod = typeof(IpcCommandRegistry)
                                 .GetMethod(nameof(IpcCommandRegistry.RegisterCommand))
                                 ?.MakeGenericMethod(registration.CommandType, registration.PayloadType);
            registerMethod?.Invoke(registry, [registration.CommandTypeName, registration.Version]);

            // 注册处理器映射
            var registerHandlerMethod = typeof(IpcCommandDispatcher)
                                        .GetMethod(nameof(IpcCommandDispatcher.RegisterHandler))
                                        ?.MakeGenericMethod(registration.CommandType, registration.PayloadType,
                                            registration.ResponseType, registration.HandlerType);
            registerHandlerMethod?.Invoke(dispatcher, [registration.CommandTypeName]);
        }
        return host;
    }
}