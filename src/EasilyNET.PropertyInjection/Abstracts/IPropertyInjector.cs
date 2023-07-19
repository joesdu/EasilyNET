namespace EasilyNET.PropertyInjection.Abstracts;

/// <summary>
/// 注入器
/// </summary>
public interface IPropertyInjector
{
    object InjectProperties(object instance);
}