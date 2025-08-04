using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// 泛型 IPC 命令处理器接口
/// </summary>
/// <typeparam name="TCommand">命令类型</typeparam>
/// <typeparam name="TPayload">负载数据类型</typeparam>
/// <typeparam name="TResponse">响应数据类型</typeparam>
public interface IIpcCommandHandler<in TCommand, TPayload, TResponse>
    where TCommand : class, IIpcCommand<TPayload>
{
    /// <summary>
    /// 处理指定的 IPC 命令
    /// </summary>
    /// <param name="command">要处理的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令处理结果</returns>
    Task<IpcCommandResponse<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}