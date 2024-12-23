// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core;

/// <summary>
///     <para xml:lang="en">Operation information</para>
///     <para xml:lang="zh">操作信息</para>
/// </summary>
// ReSharper disable once UnusedType.Global
public class OperationInfo
{
    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    public OperationInfo() { }

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="done">
    ///     <para xml:lang="en">Whether the operation is completed</para>
    ///     <para xml:lang="zh">是否完成</para>
    /// </param>
    /// <param name="time">
    ///     <para xml:lang="en">Operation time</para>
    ///     <para xml:lang="zh">操作时间</para>
    /// </param>
    public OperationInfo(bool done, DateTime? time)
    {
        Done = done;
        Time = time;
    }

    /// <summary>
    ///     <para xml:lang="en">Time</para>
    ///     <para xml:lang="zh">时间</para>
    /// </summary>
    public DateTime? Time { get; set; } = DateTime.Now;

    /// <summary>
    ///     <para xml:lang="en">Whether the operation is completed</para>
    ///     <para xml:lang="zh">是否完成</para>
    /// </summary>
    public bool Done { get; set; }
}