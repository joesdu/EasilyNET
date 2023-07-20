using EasilyNET.AutoDependencyInjection.Abstracts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using System.Reflection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入提供者
/// </summary>
/// <param name="serviceProvider"></param>
internal sealed class PropertyInjectionServiceProvider(IServiceProvider serviceProvider) : IPropertyInjectionServiceProvider
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private static BindingFlags BindingFlags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>
    /// 得到服务
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object GetService(Type serviceType)
    {
        var instance = _serviceProvider.GetService(serviceType);
        IsInjectProperties(instance);
        return instance!;
    }

    /// <summary>
    /// 得到所需服务
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object GetRequiredService(Type serviceType) => GetService(serviceType);

    /// <summary>
    /// 判断是否需要属性注入
    /// </summary>
    /// <param name="instance"></param>
    public void IsInjectProperties(object? instance)
    {
        if (instance is null) return;
        var type = instance as Type ?? instance.GetType();
        //找到所有需要注入的成员，进行注入
        type.GetMembers(BindingFlags).Where(o => o.HasAttribute<InjectionAttribute>()).ToList().ForEach(member => InjectMember(instance, member));
    }

    /// <summary>
    /// 需要注入的成员（属性或字段）
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="member">成员信息</param>
    private void InjectMember(object instance, MemberInfo member)
    {
        if (member.MemberType == MemberTypes.Property)
        {
            InjectProperty(instance, (PropertyInfo)member);
            return;
        }
        InjectField(instance, (FieldInfo)member);
    }

    /// <summary>
    /// 属性注入
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="prop">属性信息</param>
    private void InjectProperty(object instance, PropertyInfo prop)
    {
        if (prop.CanWrite)
        {
            prop.SetValue(instance, GetService(prop.PropertyType));
        }
    }

    /// <summary>
    /// 字段注入
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="field">字段信息</param>
    private void InjectField(object instance, FieldInfo field) => field.SetValue(instance, GetService(field.FieldType));
}