using System.Diagnostics;
using EasilyNET.Ipc.Client.Sample.Commands;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Client.Sample;

/// <summary>
/// IPC 测试运行器
/// </summary>
public class IpcTestRunner
{
    private readonly IIpcClient _client;
    private readonly ILogger _logger;

    /// <summary>
    /// 初始化测试运行器
    /// </summary>
    /// <param name="client">IPC 客户端</param>
    /// <param name="logger">日志记录器</param>
    public IpcTestRunner(IIpcClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public async Task RunAllTestsAsync()
    {
        await TestBasicConnection();
        await TestEchoCommand();
        await TestUserManagement();
        await TestMathCalculation();
        await TestDelayCommand();
        await TestErrorHandling();
        await TestServerStatus();
        await TestPerformance();
    }

    private async Task TestBasicConnection()
    {
        await RunTestAsync("🔗 基本连接测试", async () =>
        {
            var echo = new EchoCommand { Payload = "Hello IPC!" };
            var response = await _client.SendCommandAsync(echo, TimeSpan.FromSeconds(5));
            return response?.Success == true;
        });
    }

    private async Task TestEchoCommand()
    {
        await RunTestAsync("📢 Echo命令测试", async () =>
        {
            var testMessages = new[] { "Hello", "World", "IPC", "测试中文", "🚀 Emoji Test" };
            var allSuccess = true;
            foreach (var message in testMessages)
            {
                try
                {
                    var echo = new EchoCommand { Payload = message };
                    var response = await _client.SendCommandAsync(echo, TimeSpan.FromSeconds(5));
                    if (response?.Success != true)
                    {
                        allSuccess = false;
                        _logger.LogError("   ❌ Echo '{Message}' -> 失败: {ResponseMessage}", message, response?.Message);
                    }
                    else
                    {
                        _logger.LogInformation("   ✅ Echo '{Message}' -> 成功: {Data}", message, response.Data);
                    }
                }
                catch (Exception ex)
                {
                    allSuccess = false;
                    _logger.LogError(ex, "   ❌ Echo '{Message}' -> 异常", message);
                }
            }
            return allSuccess;
        });
    }

    private async Task TestUserManagement()
    {
        await RunTestAsync("👤 用户管理测试", async () =>
        {
            try
            {
                // 1. 创建用户
                var createUserCmd = new CreateUserCommand
                {
                    Payload = new() { Name = "Test User", Email = "test.user@example.com", Phone = "1234567890" }
                };
                var createResponse = await _client.SendCommandAsync(createUserCmd);
                if (createResponse?.Success != true)
                {
                    _logger.LogError("   ❌ 创建用户失败: {Message}", createResponse?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 创建用户成功");

                // 2. 获取用户
                var getUserCmd = new GetUserCommand { Payload = new() { UserId = 1 } };
                var getUserResponse = await _client.SendCommandAsync(getUserCmd);
                if (getUserResponse?.Success != true)
                {
                    _logger.LogError("   ❌ 获取用户失败: {Message}", getUserResponse?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 获取用户成功");

                // 3. 更新用户
                var updateUserCmd = new UpdateUserCommand
                {
                    Payload = new() { UserId = 1, Name = "Updated User" }
                };
                var updateResponse = await _client.SendCommandAsync(updateUserCmd);
                if (updateResponse?.Success != true)
                {
                    _logger.LogError("   ❌ 更新用户失败: {Message}", updateResponse?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 更新用户成功");

                // 4. 获取所有用户
                var getAllCmd = new GetAllUsersCommand();
                var getAllResponse = await _client.SendCommandAsync(getAllCmd);
                if (getAllResponse?.Success != true)
                {
                    _logger.LogError("   ❌ 获取所有用户失败: {Message}", getAllResponse?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 获取所有用户成功");

                // 5. 删除用户
                var deleteCmd = new DeleteUserCommand { Payload = new() { UserId = 1 } };
                var deleteResponse = await _client.SendCommandAsync(deleteCmd);
                if (deleteResponse?.Success != true)
                {
                    _logger.LogError("   ❌ 删除用户失败: {Message}", deleteResponse?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 删除用户成功");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "   ❌ 用户管理测试异常");
                return false;
            }
        });
    }

    private async Task TestMathCalculation()
    {
        await RunTestAsync("🧮 数学计算测试", async () =>
        {
            try
            {
                var cmd = new MathCalculationCommand { Payload = new() { Number1 = 10, Number2 = 5, Operation = "+" } };
                var response = await _client.SendCommandAsync(cmd);
                if (response?.Success != true)
                {
                    _logger.LogError("   ❌ 加法失败: {Message}", response?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 加法成功: {Data}", response.Data);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "   ❌ 数学计算测试异常");
                return false;
            }
        });
    }

    private async Task TestDelayCommand()
    {
        await RunTestAsync("⏱️ 延时命令测试", async () =>
        {
            try
            {
                var cmd = new DelayProcessCommand { Payload = new() { DelaySeconds = 2, Message = "Delay Test" } };
                var response = await _client.SendCommandAsync(cmd, TimeSpan.FromSeconds(5));
                if (response?.Success != true)
                {
                    _logger.LogError("   ❌ 延时命令失败: {Message}", response?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 延时命令成功: {Data}", response.Data);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "   ❌ 延时命令测试异常");
                return false;
            }
        });
    }

    private async Task TestErrorHandling()
    {
        await RunTestAsync("❌ 错误处理测试", async () =>
        {
            try
            {
                var cmd = new ErrorTestCommand { Payload = new() { ErrorType = "ArgumentException" } };
                var response = await _client.SendCommandAsync(cmd);
                if (response?.Success == true)
                {
                    _logger.LogError("   ❌ 错误处理测试失败: 未按预期返回错误");
                    return false;
                }
                _logger.LogInformation("   ✅ 错误处理测试成功: {Message}", response?.Message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("   ✅ 错误处理测试成功（异常）: {Message}", ex.Message);
                return true;
            }
        });
    }

    private async Task TestServerStatus()
    {
        await RunTestAsync("📊 服务器状态测试", async () =>
        {
            try
            {
                var cmd = new GetServerStatusCommand();
                var response = await _client.SendCommandAsync(cmd);
                if (response?.Success != true)
                {
                    _logger.LogError("   ❌ 获取服务器状态失败: {Message}", response?.Message);
                    return false;
                }
                _logger.LogInformation("   ✅ 获取服务器状态成功: {Data}", response.Data);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "   ❌ 服务器状态测试异常");
                return false;
            }
        });
    }

    private async Task TestPerformance()
    {
        Console.WriteLine("⚡ 性能测试");
        const int testCount = 100;
        var tasks = new List<Task<IpcCommandResponse<object>?>>();
        var sw = Stopwatch.StartNew();
        try
        {
            for (var i = 0; i < testCount; i++)
            {
                var echo = new EchoCommand { Payload = $"Performance Test {i + 1}" };
                tasks.Add(_client.SendCommandAsync(echo, TimeSpan.FromSeconds(10)));
            }
            var results = await Task.WhenAll(tasks);
            sw.Stop();
            var successful = results.Count(r => r?.Success == true);
            var failed = testCount - successful;
            Console.WriteLine("   📊 性能测试结果:");
            Console.WriteLine($"      总数: {testCount}");
            Console.WriteLine($"      成功: {successful}");
            Console.WriteLine($"      失败: {failed}");
            Console.WriteLine($"      总耗时: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"      平均耗时: {(double)sw.ElapsedMilliseconds / testCount:F2}ms");
            Console.WriteLine($"      吞吐量: {testCount / sw.Elapsed.TotalSeconds:F2} req/s");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 性能测试异常: {ex.Message}");
        }
        Console.WriteLine();
    }

    private async Task RunTestAsync(string testName, Func<Task<bool>> testFunc)
    {
        Console.WriteLine(testName);
        try
        {
            var success = await testFunc();
            Console.WriteLine(success ? "   ✅ 测试通过" : "   ❌ 测试失败");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ 测试异常: {ex.Message}");
            _logger.LogError(ex, "测试 '{TestName}' 期间发生异常", testName);
        }
        Console.WriteLine();
    }
}
