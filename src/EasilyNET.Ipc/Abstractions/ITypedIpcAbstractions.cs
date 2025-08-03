namespace EasilyNET.Ipc.Abstractions;

/// <summary>
/// 类型化 IPC 命令的基础接口
/// </summary>
public interface ITypedCommand
{
    /// <summary>
    /// 命令唯一标识符
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// 命令创建时间戳
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// 带有响应类型的类型化 IPC 命令接口
/// </summary>
/// <typeparam name="TResponse">响应数据类型</typeparam>
public interface ITypedCommand<TResponse> : ITypedCommand { }

/// <summary>
/// 带有响应的类型化 IPC 命令处理器接口
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
/// <typeparam name="TResponse">响应数据类型</typeparam>
public interface ITypedCommandHandler<in TCommand, TResponse>
    where TCommand : ITypedCommand<TResponse>
{
    /// <summary>
    /// 异步处理指定的命令并返回响应
    /// </summary>
    /// <param name="command">要处理的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令处理结果</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// 无响应的类型化 IPC 命令处理器接口
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
public interface ITypedCommandHandler<in TCommand>
    where TCommand : ITypedCommand
{
    /// <summary>
    /// 异步处理指定的命令（无返回值）
    /// </summary>
    /// <param name="command">要处理的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// 类型化 IPC 客户端接口，用于发送命令到远程服务
/// </summary>
public interface ITypedIpcClient
{
    /// <summary>
    /// 异步发送带有响应的命令
    /// </summary>
    /// <typeparam name="TResponse">响应数据类型</typeparam>
    /// <param name="command">要发送的命令</param>
    /// <param name="timeout">超时时间，默认为系统默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令执行结果</returns>
    Task<TResponse> SendAsync<TResponse>(ITypedCommand<TResponse> command, TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步发送无响应的命令
    /// </summary>
    /// <param name="command">要发送的命令</param>
    /// <param name="timeout">超时时间，默认为系统默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task SendAsync(ITypedCommand command, TimeSpan timeout = default, CancellationToken cancellationToken = default);
}

/// <summary>
/// 类型化命令分发器接口，用于在服务端分发命令到对应的处理器
/// </summary>
public interface ITypedCommandDispatcher
{
    /// <summary>
    /// 异步分发带有响应的命令到对应的处理器
    /// </summary>
    /// <typeparam name="TResponse">响应数据类型</typeparam>
    /// <param name="command">要分发的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令处理结果</returns>
    Task<TResponse> DispatchAsync<TResponse>(ITypedCommand<TResponse> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步分发无响应的命令到对应的处理器
    /// </summary>
    /// <param name="command">要分发的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    Task DispatchAsync(ITypedCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// 命令类型注册表接口，用于管理命令类型的注册和查找
/// </summary>
public interface ICommandTypeRegistry
{
    /// <summary>
    /// 注册指定的命令类型
    /// </summary>
    /// <typeparam name="TCommand">要注册的命令类型</typeparam>
    void Register<TCommand>() where TCommand : ITypedCommand;

    /// <summary>
    /// 根据类型哈希获取对应的命令类型
    /// </summary>
    /// <param name="typeHash">类型哈希值</param>
    /// <returns>对应的命令类型，如果未找到则返回 null</returns>
    Type? GetCommandType(string typeHash);

    /// <summary>
    /// 获取指定命令类型的哈希值
    /// </summary>
    /// <param name="commandType">命令类型</param>
    /// <returns>类型哈希值</returns>
    string GetTypeHash(Type commandType);
}