// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core;

/// <summary>
/// 操作人
/// </summary>
/// <param name="rid">相关ID</param>
/// <param name="name">名称</param>
// ReSharper disable once UnusedType.Global
public class Operator(string rid, string name) : ReferenceItem(rid, name)
{
    /// <summary>
    /// 时间
    /// </summary>
    public DateTime? Time { get; set; }
}
