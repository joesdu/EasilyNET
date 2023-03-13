namespace EasilyNET.Extensions.Attributes;

/// <summary>
/// 基础特性
/// </summary>
public abstract class AttributeBase : Attribute
{
    /// <summary>
    /// 获取描述
    /// </summary>
    /// <returns></returns>
    public abstract string Description();
}