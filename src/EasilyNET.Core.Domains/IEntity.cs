namespace EasilyNET.Core.Domains;

/// <summary>
/// 接口实体
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 得到主键
    /// </summary>
    /// <returns>返回主键对象</returns>
    public object[] GetKeys();

}