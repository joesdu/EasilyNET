// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core;

/// <summary>
/// 操作信息
/// </summary>
// ReSharper disable once UnusedType.Global
public class OperationInfo
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationInfo() { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="done">是否完成</param>
    /// <param name="time">操作时间</param>
    public OperationInfo(bool done, DateTime? time)
    {
        Done = done;
        Time = time;
    }

    /// <summary>
    /// 时间
    /// </summary>
    public DateTime? Time { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否完成
    /// </summary>
    public bool Done { get; set; }
}
