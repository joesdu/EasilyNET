using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;

namespace EasilyNET.Raft.AspNetCore.Runtime;

/// <summary>
///     <para xml:lang="en">Runtime facade for processing raft messages</para>
///     <para xml:lang="zh">Raft 运行时消息处理门面</para>
/// </summary>
public interface IRaftRuntime
{
    /// <summary>
    ///     <para xml:lang="en">Initializes runtime state from persistent stores</para>
    ///     <para xml:lang="zh">从持久化存储初始化运行时状态</para>
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Handles inbound raft message</para>
    ///     <para xml:lang="zh">处理入站 Raft 消息</para>
    /// </summary>
    Task HandleAsync(RaftMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Handles inbound raft RPC message and returns direct response</para>
    ///     <para xml:lang="zh">处理入站 Raft RPC 消息并返回直接响应</para>
    /// </summary>
    Task<TResponse> HandleRpcAsync<TResponse>(RaftMessage message, CancellationToken cancellationToken = default) where TResponse : RaftMessage;

    /// <summary>
    ///     <para xml:lang="en">Gets whether runtime has completed recovery initialization</para>
    ///     <para xml:lang="zh">运行时是否完成恢复初始化</para>
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets current raft state snapshot</para>
    ///     <para xml:lang="zh">获取当前状态快照</para>
    /// </summary>
    RaftNodeState GetState();
}
