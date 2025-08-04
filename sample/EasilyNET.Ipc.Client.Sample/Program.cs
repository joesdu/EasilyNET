using System.Diagnostics;
using EasilyNET.Ipc;
using EasilyNET.Ipc.Client.Sample.Commands;
using EasilyNET.Ipc.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Client.Sample;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== EasilyNET.Ipc Client Sample ===");
        Console.WriteLine("正在启动 IPC 客户端测试...");

        try
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // 注册 IPC 客户端
                    services.AddIpcClient(context.Configuration);

                    // 注册命令类型
                    services.RegisterIpcCommandsFromAssembly(typeof(Program).Assembly);
                });

            var host = builder.Build();
            var client = host.Services.GetRequiredService<IIpcClient>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("✅ 客户端已启动，开始测试...");
            Console.WriteLine("✅ 客户端已启动，开始测试...");
            Console.WriteLine();

            var testRunner = new IpcTestRunner(client, logger);

            // 执行所有测试
            await testRunner.RunAllTestsAsync();

            Console.WriteLine();
            Console.WriteLine("🎯 所有测试完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败: {ex.Message}");
            Console.WriteLine($"详细错误: {ex}");
        }

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}