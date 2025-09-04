namespace EasilyNET.Consensus.Raft;

/// <summary>
/// Raft 日志条目
/// </summary>
public class LogEntry
{
    /// <summary>
    /// 任期号
    /// </summary>
    public int Term { get; set; }

    /// <summary>
    /// 日志索引
    /// </summary>
    public long Index { get; set; }

    /// <summary>
    /// 命令数据
    /// </summary>
    public byte[]? Command { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="term">任期号</param>
    /// <param name="index">日志索引</param>
    /// <param name="command">命令数据</param>
    public LogEntry(int term, long index, byte[]? command)
    {
        Term = term;
        Index = index;
        Command = command;
    }
}