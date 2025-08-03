using EasilyNET.Ipc.Extensions;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Examples;

/// <summary>
/// 完整的使用示例
/// </summary>
public class Program
{
    /// <summary>
    /// 应用程序的主入口点，演示类型化IPC系统的使用
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>异步任务</returns>
    public static async Task Main(string[] args)
    {
        // 创建主机
        var host = Host.CreateDefaultBuilder(args)
                       .ConfigureServices((context, services) =>
                       {
                           // 添加 IPC 服务
                           services.AddAdvancedIpc();

                           // 注册命令处理器
                           services.AddIpcCommandHandler<CreateUserCommand, CreateUserPayload, CreateUserResponse, CreateUserCommandHandler>();

                           // 可以注册更多命令处理器
                           // services.AddIpcCommandHandler<UpdateUserCommand, UpdateUserPayload, UpdateUserResponse, UpdateUserCommandHandler>();
                           // services.AddIpcCommandHandler<DeleteUserCommand, DeleteUserPayload, DeleteUserResponse, DeleteUserCommandHandler>();
                       })
                       .Build();

        // 初始化 IPC 服务
        host.InitializeIpc();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        try
        {
            // 演示命令的创建、序列化、反序列化和处理
            await DemonstrateCommandHandling(host.Services, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "程序执行出错");
        }
        await host.StopAsync();
    }

    private static async Task DemonstrateCommandHandling(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("开始演示 IPC 命令处理");

        // 获取服务
        var serializer = services.GetRequiredService<IIpcGenericSerializer>();
        var registry = services.GetRequiredService<IpcCommandRegistry>();
        var dispatcher = services.GetRequiredService<IpcCommandDispatcher>();

        // 1. 创建命令
        var payload = new CreateUserPayload
        {
            UserName = "张三",
            Email = "zhangsan@example.com",
            Age = 25
        };
        var command = new CreateUserCommand(payload);
        logger.LogInformation("创建了命令: {CommandId}, 用户: {UserName}",
            command.CommandId, command.Payload.UserName);

        // 2. 序列化命令
        var commandData = serializer.SerializeCommand(command, registry);
        logger.LogInformation("命令序列化完成，数据大小: {Size} 字节", commandData.Length);

        // 3. 反序列化命令（模拟传输后的接收端）
        var deserializedCommand = serializer.DeserializeCommand(commandData, registry);
        if (deserializedCommand == null)
        {
            logger.LogError("命令反序列化失败");
            return;
        }
        logger.LogInformation("命令反序列化成功: {CommandId}", deserializedCommand.CommandId);

        // 4. 分发并处理命令
        var response = await dispatcher.DispatchAsync(deserializedCommand);
        logger.LogInformation("命令处理完成: 成功={Success}, 消息={Message}",
            response.Success, response.Message);

        // 5. 如果是泛型响应，可以获取具体的响应数据
        if (response.Success)
        {
            // 这里需要知道响应的具体类型才能反序列化
            // 在实际应用中，可能需要在响应中包含类型信息
            logger.LogInformation("处理完成，响应时间: {Timestamp}", response.Timestamp);
        }
        logger.LogInformation("演示完成");
    }
}