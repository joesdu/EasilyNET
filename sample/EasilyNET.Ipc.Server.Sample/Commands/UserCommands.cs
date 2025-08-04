using EasilyNET.Core.Essentials;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using EasilyNET.Ipc.Server.Sample.Models;
using EasilyNET.Ipc.Server.Sample.Services;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Server.Sample.Commands;

// ==================== 用户命令定义 ====================

/// <summary>
/// 创建用户命令
/// </summary>
public class CreateUserCommand : IIpcCommand<CreateUserPayload>
{
    public CreateUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 获取用户命令
/// </summary>
public class GetUserCommand : IIpcCommand<GetUserPayload>
{
    public GetUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 更新用户命令
/// </summary>
public class UpdateUserCommand : IIpcCommand<UpdateUserPayload>
{
    public UpdateUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 删除用户命令
/// </summary>
public class DeleteUserCommand : IIpcCommand<DeleteUserPayload>
{
    public DeleteUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 获取所有用户命令
/// </summary>
public class GetAllUsersCommand : IIpcCommand<GetAllUsersPayload>
{
    public GetAllUsersPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// ==================== 命令处理器 ====================

/// <summary>
/// 创建用户处理器
/// </summary>
public class CreateUserHandler : IIpcCommandHandler<CreateUserCommand, CreateUserPayload, User>
{
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly IUserStorage _userStorage;

    public CreateUserHandler(ILogger<CreateUserHandler> logger, IUserStorage userStorage)
    {
        _logger = logger;
        _userStorage = userStorage;
    }

    public async Task<IpcCommandResponse<User>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = command.Payload;

            _logger.LogInformation("创建用户: {Name}, Email: {Email}", payload.Name, payload.Email);

            // 验证输入
            if (string.IsNullOrWhiteSpace(payload.Name))
                throw new ArgumentException("用户名不能为空", nameof(payload.Name));

            if (string.IsNullOrWhiteSpace(payload.Email))
                throw new ArgumentException("邮箱不能为空", nameof(payload.Email));

            // 创建用户
            var user = new User
            {
                Name = payload.Name,
                Email = payload.Email,
                Phone = payload.Phone ?? string.Empty
            };

            var createdUser = _userStorage.AddUser(user);

            _logger.LogInformation("用户创建成功: {UserId}", createdUser.Id);

            return await Task.FromResult(IpcCommandResponse<User>.CreateSuccess(command.CommandId, createdUser, "用户创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败");
            return await Task.FromResult(IpcCommandResponse<User>.CreateFailure(command.CommandId, ex.Message));
        }
    }
}

/// <summary>
/// 获取用户处理器
/// </summary>
public class GetUserHandler : IIpcCommandHandler<GetUserCommand, GetUserPayload, User?>
{
    private readonly ILogger<GetUserHandler> _logger;
    private readonly IUserStorage _userStorage;

    public GetUserHandler(ILogger<GetUserHandler> logger, IUserStorage userStorage)
    {
        _logger = logger;
        _userStorage = userStorage;
    }

    public async Task<IpcCommandResponse<User?>> HandleAsync(GetUserCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = command.Payload;

            _logger.LogInformation("获取用户: {UserId}", payload.UserId);

            if (payload.UserId <= 0)
                throw new ArgumentException("用户ID必须大于0", nameof(payload.UserId));

            var user = _userStorage.GetUser(payload.UserId);

            if (user == null)
            {
                _logger.LogWarning("用户不存在: {UserId}", payload.UserId);
                return await Task.FromResult(IpcCommandResponse<User?>.CreateSuccess(command.CommandId, null, "用户不存在"));
            }
            else
            {
                _logger.LogInformation("用户获取成功: {UserId}, Name: {Name}", user.Id, user.Name);
                return await Task.FromResult(IpcCommandResponse<User?>.CreateSuccess(command.CommandId, user, "用户获取成功"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户失败");
            return await Task.FromResult(IpcCommandResponse<User?>.CreateFailure(command.CommandId, ex.Message));
        }
    }
}

/// <summary>
/// 更新用户处理器
/// </summary>
public class UpdateUserHandler : IIpcCommandHandler<UpdateUserCommand, UpdateUserPayload, User?>
{
    private readonly ILogger<UpdateUserHandler> _logger;
    private readonly IUserStorage _userStorage;

    public UpdateUserHandler(ILogger<UpdateUserHandler> logger, IUserStorage userStorage)
    {
        _logger = logger;
        _userStorage = userStorage;
    }

    public async Task<IpcCommandResponse<User?>> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = command.Payload;

            _logger.LogInformation("更新用户: {UserId}", payload.UserId);

            if (payload.UserId <= 0)
                throw new ArgumentException("用户ID必须大于0", nameof(payload.UserId));

            var existingUser = _userStorage.GetUser(payload.UserId);
            if (existingUser == null)
            {
                _logger.LogWarning("用户不存在: {UserId}", payload.UserId);
                return await Task.FromResult(IpcCommandResponse<User?>.CreateSuccess(command.CommandId, null, "用户不存在"));
            }

            // 更新字段
            if (!string.IsNullOrWhiteSpace(payload.Name))
                existingUser.Name = payload.Name;

            if (!string.IsNullOrWhiteSpace(payload.Email))
                existingUser.Email = payload.Email;

            if (!string.IsNullOrWhiteSpace(payload.Phone))
                existingUser.Phone = payload.Phone;

            var updatedUser = _userStorage.UpdateUser(existingUser);

            _logger.LogInformation("用户更新成功: {UserId}", payload.UserId);

            return await Task.FromResult(IpcCommandResponse<User?>.CreateSuccess(command.CommandId, updatedUser, "用户更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败");
            return await Task.FromResult(IpcCommandResponse<User?>.CreateFailure(command.CommandId, ex.Message));
        }
    }
}

/// <summary>
/// 删除用户处理器
/// </summary>
public class DeleteUserHandler : IIpcCommandHandler<DeleteUserCommand, DeleteUserPayload, bool>
{
    private readonly ILogger<DeleteUserHandler> _logger;
    private readonly IUserStorage _userStorage;

    public DeleteUserHandler(ILogger<DeleteUserHandler> logger, IUserStorage userStorage)
    {
        _logger = logger;
        _userStorage = userStorage;
    }

    public async Task<IpcCommandResponse<bool>> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = command.Payload;

            _logger.LogInformation("删除用户: {UserId}, 原因: {Reason}", payload.UserId, payload.Reason);

            if (payload.UserId <= 0)
                throw new ArgumentException("用户ID必须大于0", nameof(payload.UserId));

            var result = _userStorage.DeleteUser(payload.UserId);

            if (result)
            {
                _logger.LogInformation("用户删除成功: {UserId}", payload.UserId);
                return await Task.FromResult(IpcCommandResponse<bool>.CreateSuccess(command.CommandId, true, "用户删除成功"));
            }
            else
            {
                _logger.LogWarning("用户删除失败，用户不存在: {UserId}", payload.UserId);
                return await Task.FromResult(IpcCommandResponse<bool>.CreateSuccess(command.CommandId, false, "用户不存在"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户失败");
            return await Task.FromResult(IpcCommandResponse<bool>.CreateFailure(command.CommandId, ex.Message));
        }
    }
}

/// <summary>
/// 获取所有用户处理器
/// </summary>
public class GetAllUsersHandler : IIpcCommandHandler<GetAllUsersCommand, GetAllUsersPayload, List<User>>
{
    private readonly ILogger<GetAllUsersHandler> _logger;
    private readonly IUserStorage _userStorage;

    public GetAllUsersHandler(ILogger<GetAllUsersHandler> logger, IUserStorage userStorage)
    {
        _logger = logger;
        _userStorage = userStorage;
    }

    public async Task<IpcCommandResponse<List<User>>> HandleAsync(GetAllUsersCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("获取所有用户列表");

            var users = _userStorage.GetAllUsers();

            _logger.LogInformation("获取到 {Count} 个用户", users.Count);

            return await Task.FromResult(IpcCommandResponse<List<User>>.CreateSuccess(command.CommandId, users, $"获取到 {users.Count} 个用户"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return await Task.FromResult(IpcCommandResponse<List<User>>.CreateFailure(command.CommandId, ex.Message));
        }
    }
}
