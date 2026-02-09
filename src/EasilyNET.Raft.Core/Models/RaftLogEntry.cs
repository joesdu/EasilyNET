namespace EasilyNET.Raft.Core.Models;

/// <summary>
///     <para xml:lang="en">Replicated log entry</para>
///     <para xml:lang="zh">复制日志条目</para>
/// </summary>
/// <param name="Index">
///     <para xml:lang="en">Log index (1-based)</para>
///     <para xml:lang="zh">日志索引（从 1 开始）</para>
/// </param>
/// <param name="Term">
///     <para xml:lang="en">Term of this entry</para>
///     <para xml:lang="zh">条目所属任期</para>
/// </param>
/// <param name="Command">
///     <para xml:lang="en">Opaque command payload</para>
///     <para xml:lang="zh">命令负载（不透明字节）</para>
/// </param>
public sealed record RaftLogEntry(long Index, long Term, byte[] Command);
