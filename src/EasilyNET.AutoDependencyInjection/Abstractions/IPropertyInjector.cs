namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// 属性注入接口
/// </summary>
internal interface IPropertyInjector
{
    /// <summary>
    /// 注入属性
    /// </summary>
    /// <param name="instance">要注入的实例</param>
    /// <returns></returns>
    object InjectProperties(object instance);
}