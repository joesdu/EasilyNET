using EasilyNET.Ipc;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using EasilyNET.Core.Essentials;
using EasilyNET.Ipc.Server.Sample.Commands;
using EasilyNET.Ipc.Server.Sample.Models;
using EasilyNET.Ipc.Server.Sample.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Server.Sample;

/// <summary>
/// 简单的Echo命令
/// </summary>
public class EchoCommand : IIpcCommand<string>
{
    /// <summary>
    /// 命令负载数据
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// 命令唯一标识符
    /// </summary>
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>
    /// 目标标识符
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// 命令创建时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Echo命令处理器
/// </summary>
public class EchoHandler : IIpcCommandHandler<EchoCommand, string, string>
{
    private readonly ILogger<EchoHandler> _logger;

    /// <summary>
    /// 初始化Echo处理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public EchoHandler(ILogger<EchoHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理Echo命令
    /// </summary>
    /// <param name="command">要处理的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令处理结果</returns>
    public Task<IpcCommandResponse<string>> HandleAsync(EchoCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("处理Echo命令: {Payload}", command.Payload);

        // 简单的回显逻辑
        var response = $"Echo: {command.Payload} (处理时间: {DateTime.Now:HH:mm:ss.fff})";
        return Task.FromResult(IpcCommandResponse<string>.CreateSuccess(command.CommandId, response, "Echo处理成功"));
    }
}

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== EasilyNET.Ipc Server Sample ===");
        Console.WriteLine("正在启动 IPC 服务端...");

        try
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // 注册 IPC 服务端
                    services.AddIpcServer(context.Configuration);

                    // 注册存储服务
                    services.AddSingleton<IUserStorage, UserStorage>();

                    // 注册Echo命令处理器
                    services.AddIpcCommandHandler<EchoCommand, string, string, EchoHandler>();

                    // 注册用户管理命令处理器
                    services.AddScoped<CreateUserHandler>();
                    services.AddScoped<GetUserHandler>();
                    services.AddScoped<UpdateUserHandler>();
                    services.AddScoped<DeleteUserHandler>();
                    services.AddScoped<GetAllUsersHandler>();

                    // 注册测试命令处理器
                    services.AddIpcCommandHandler<MathCalculationCommand, MathCalculationPayload, MathResult, MathCalculationHandler>();
                    services.AddIpcCommandHandler<DelayProcessCommand, DelayProcessPayload, DelayResult, DelayProcessHandler>();
                    services.AddIpcCommandHandler<ErrorTestCommand, ErrorTestPayload, string, ErrorTestHandler>();
                    services.AddIpcCommandHandler<GetServerStatusCommand, object?, ServerStatus, GetServerStatusHandler>();

                    // 批量注册命令类型
                    services.RegisterIpcCommandsFromAssembly(typeof(Program).Assembly);
                });

            var host = builder.Build();

            // 添加优雅关闭处理
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
            {
                Console.WriteLine("✅ IPC 服务端已启动");
                Console.WriteLine();
                Console.WriteLine("📝 支持的命令:");
                Console.WriteLine("   📢 Echo: 回显测试命令");
                Console.WriteLine("   👤 用户管理: 创建、查询、更新、删除用户");
                Console.WriteLine("   🧮 数学计算: 基本四则运算");
                Console.WriteLine("   ⏱️  延时处理: 模拟长时间运行的任务");
                Console.WriteLine("   ❌ 错误测试: 测试各种异常情况");
                Console.WriteLine("   📊 状态查询: 获取服务器运行状态");
                Console.WriteLine();
                Console.WriteLine("🌐 IPC 通信信息:");
                Console.WriteLine($"   📍 管道名称: EasilyNET_IPC_Test");
                Console.WriteLine($"   📁 Unix Socket: /tmp/easilynet_ipc_test.sock");
                Console.WriteLine();
                Console.WriteLine("⏹️  按 Ctrl+C 停止服务");
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine("🛑 正在停止 IPC 服务端...");
            });

            lifetime.ApplicationStopped.Register(() =>
            {
                Console.WriteLine("✅ IPC 服务端已停止");
            });

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 服务启动失败: {ex.Message}");
            Console.WriteLine($"详细错误信息: {ex}");
            Console.WriteLine();
            Console.WriteLine("💡 常见问题排查:");
            Console.WriteLine("   1. 检查端口是否被占用");
            Console.WriteLine("   2. 确认配置文件格式正确");
            Console.WriteLine("   3. 检查是否有权限访问管道/套接字路径");
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}