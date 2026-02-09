using EasilyNET.Raft.Core.Actions;

namespace EasilyNET.Raft.Core.Models;

/// <summary>
///     <para xml:lang="en">Raft state machine result</para>
///     <para xml:lang="zh">Raft 状态机处理结果</para>
/// </summary>
/// <param name="State">
///     <para xml:lang="en">Updated state</para>
///     <para xml:lang="zh">更新后的状态</para>
/// </param>
/// <param name="Actions">
///     <para xml:lang="en">Side-effect actions to execute by shell</para>
///     <para xml:lang="zh">由外壳执行的副作用动作</para>
/// </param>
public sealed record RaftResult(RaftNodeState State, IReadOnlyList<RaftAction> Actions);
