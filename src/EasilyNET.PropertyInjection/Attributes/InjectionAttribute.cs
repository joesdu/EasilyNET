namespace EasilyNET.PropertyInjection.Attributes
{
    /// <summary>
    /// 实现属性注入
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class InjectionAttribute : Attribute
    {
    }
}
