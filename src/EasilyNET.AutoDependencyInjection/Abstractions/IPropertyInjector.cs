namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// 属性注入注射器接口
/// </summary>
public interface IPropertyInjector
{
    /// <summary>
    /// 把属性注入
    /// </summary>
    /// <param name="instance">要注入的实例</param>
    /// <returns></returns>
    object InjectProperties(object instance);
}