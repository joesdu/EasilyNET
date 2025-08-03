using EasilyNET.Core.Essentials;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Examples;

/// <summary>
/// 示例：用户创建命令处理器
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class CreateUserCommandHandler : IIpcCommandHandler<CreateUserCommand, CreateUserPayload, CreateUserResponse>
{
    private readonly ILogger<CreateUserCommandHandler> _logger;

    /// <summary>
    /// 初始化新的处理器实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public CreateUserCommandHandler(ILogger<CreateUserCommandHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IpcCommandResponseT<CreateUserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("处理用户创建命令: {UserName}, {Email}",
                command.Payload.UserName, command.Payload.Email);

            // 模拟异步操作
            await Task.Delay(100, cancellationToken);

            // 验证输入
            if (string.IsNullOrWhiteSpace(command.Payload.UserName))
            {
                return IpcCommandResponseT<CreateUserResponse>.CreateFailure(command.CommandId, "用户名不能为空");
            }
            if (string.IsNullOrWhiteSpace(command.Payload.Email))
            {
                return IpcCommandResponseT<CreateUserResponse>.CreateFailure(command.CommandId, "邮箱不能为空");
            }
            if (command.Payload.Age is < 0 or > 120)
            {
                return IpcCommandResponseT<CreateUserResponse>.CreateFailure(command.CommandId, "年龄必须在 0-120 之间");
            }

            // 模拟创建用户
            var response = new CreateUserResponse
            {
                UserId = Ulid.NewUlid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _logger.LogInformation("用户创建成功: {UserId}", response.UserId);
            return IpcCommandResponseT<CreateUserResponse>.CreateSuccess(command.CommandId, response, "用户创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理用户创建命令时发生错误");
            return IpcCommandResponseT<CreateUserResponse>.CreateFailure(command.CommandId, $"处理命令时发生错误: {ex.Message}");
        }
    }
}